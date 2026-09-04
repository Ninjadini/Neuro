using System.Collections.Generic;
using System.Linq;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    /// The scan the editor's "change RefId" runs over the whole database. This covers the algorithm - walking
    /// every registered table and repointing every Reference<> that held the old id - without the file IO,
    /// which is what NeuroEditorDataProvider.ChangeRefId adds on top.
    public class RefIdRewriteAcrossDatabaseTests
    {
        NeuroReferences references;

        [SetUp]
        public void SetUp()
        {
            UberTestClass.RegisterAll();
            Register();
            references = new NeuroReferences();
        }

        [Test]
        public void RepointsEveryItemThatReferencedTheOldId()
        {
            var target = NewTarget(5);
            var other = NewTarget(6);
            var holderA = NewHolder(1, pointsAt: 5);
            var holderB = NewHolder(2, pointsAt: 5);
            var holderC = NewHolder(3, pointsAt: 6);

            var updated = RewriteAcrossDatabase(typeof(ReferencableClass), 5, 4000);

            Assert.That(holderA.Single.RefId, Is.EqualTo(4000u));
            Assert.That(holderA.List[0].RefId, Is.EqualTo(4000u));
            Assert.That(holderB.Single.RefId, Is.EqualTo(4000u));
            Assert.That(holderC.Single.RefId, Is.EqualTo(6u), "an item pointing at a different id must be left alone");
            Assert.That(updated, Is.EquivalentTo(new object[] { holderA, holderB }));
            Assert.NotNull(target);
            Assert.NotNull(other);
        }

        [Test]
        public void RepointsSelfReferences()
        {
            // the item being moved can reference itself, and the finder in the editor skips the item itself,
            // so this is the case most likely to be missed.
            var selfRef = new RefHolder() { RefId = 7 };
            selfRef.OtherTypeRef.RefId = 7;
            references.Register(selfRef);

            var updated = RewriteAcrossDatabase(typeof(RefHolder), 7, 4000);

            Assert.That(selfRef.OtherTypeRef.RefId, Is.EqualTo(4000u));
            Assert.That(updated, Is.EquivalentTo(new object[] { selfRef }));
        }

        [Test]
        public void OnlyRepointsReferencesOfTheMatchingType()
        {
            // two different referencable types can both have an item with id 5 - moving one must not touch
            // references aimed at the other.
            var holder = NewHolder(1, pointsAt: 5);
            holder.OtherTypeRef.RefId = 5;

            RewriteAcrossDatabase(typeof(ReferencableClass), 5, 4000);

            Assert.That(holder.Single.RefId, Is.EqualTo(4000u));
            Assert.That(holder.OtherTypeRef.RefId, Is.EqualTo(5u), "a Reference<> to a different type must be left alone");
        }

        [Test]
        public void ChangesNothingWhenNoOneReferencedTheId()
        {
            var holder = NewHolder(1, pointsAt: 5);

            var updated = RewriteAcrossDatabase(typeof(ReferencableClass), 999, 4000);

            Assert.IsEmpty(updated);
            Assert.That(holder.Single.RefId, Is.EqualTo(5u));
        }

        List<IReferencable> RewriteAcrossDatabase(System.Type rootType, uint oldRefId, uint newRefId)
        {
            var visitor = new NeuroEditVisitor();
            var rewriter = new Rewriter(rootType, oldRefId, newRefId);
            var updated = new List<IReferencable>();
            foreach (var baseType in references.GetRegisteredBaseTypes().ToArray())
            {
                foreach (var referencable in references.GetTable(baseType).SelectAll().ToArray())
                {
                    rewriter.Changes = 0;
                    visitor.Visit(referencable, rewriter);
                    if (rewriter.Changes > 0)
                    {
                        updated.Add(referencable);
                    }
                }
            }
            return updated;
        }

        ReferencableClass NewTarget(uint refId)
        {
            var obj = new ReferencableClass() { RefId = refId, Name = "target" + refId };
            references.Register(obj);
            return obj;
        }

        RefHolder NewHolder(uint refId, uint pointsAt)
        {
            var holder = new RefHolder() { RefId = refId };
            holder.Single.RefId = pointsAt;
            holder.List.Add(new Reference<ReferencableClass>() { RefId = pointsAt });
            references.Register(holder);
            return holder;
        }

        class Rewriter : NeuroEditVisitor.IInterface
        {
            readonly System.Type rootType;
            readonly uint oldRefId;
            readonly uint newRefId;
            public int Changes;

            public Rewriter(System.Type rootType, uint oldRefId, uint newRefId)
            {
                this.rootType = rootType;
                this.oldRefId = oldRefId;
                this.newRefId = newRefId;
            }

            public void BeginVisit<T>(ref T obj, string name, int? listIndex) { }
            public void EndVisit() { }

            public void VisitRef<T>(ref Reference<T> reference) where T : class, IReferencable
            {
                if (reference.RefId == oldRefId && typeof(T) == rootType)
                {
                    reference.RefId = newRefId;
                    Changes++;
                }
            }
        }

        static bool _registered;
        static void Register()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            // the visitor enters through the global type path when the static type is only IReferencable
            NeuroGlobalTypes.Register<RefHolder>(3311);
            NeuroSyncTypes.Register<Reference<RefHolder>>(FieldSizeType.VarInt, Reference<RefHolder>.Sync);
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref RefHolder value)
            {
                value ??= new RefHolder();
                neuro.Sync(1, nameof(value.Single), ref value.Single);
                neuro.Sync(2, nameof(value.List), ref value.List);
                neuro.Sync(3, nameof(value.OtherTypeRef), ref value.OtherTypeRef);
            });
        }

        public class RefHolder : IReferencable
        {
            public Reference<ReferencableClass> Single;
            public List<Reference<ReferencableClass>> List = new List<Reference<ReferencableClass>>();
            public Reference<RefHolder> OtherTypeRef;

            public uint RefId { get; set; }
            public string RefName { get; set; }
        }
    }
}
