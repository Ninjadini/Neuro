using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    /// NeuroEditVisitor is the walk that lets a visitor rewrite what it is handed. The RefId change in the
    /// editor relies on that reaching every reference, not just the ones held in a plain field.
    public class NeuroEditVisitorTests
    {
        [SetUp]
        public void SetUp()
        {
            UberTestClass.RegisterAll();
            Register();
        }

        [Test]
        public void RewritesEveryKindOfReferenceHolder()
        {
            var obj = NewObjWithAllRefsPointingAt(1);

            new NeuroEditVisitor().Visit(obj, new Rewriter(1, 999));

            Assert.That(obj.Single.RefId, Is.EqualTo(999u), "plain field");
            Assert.That(obj.List[0].RefId, Is.EqualTo(999u), "list item");
            Assert.That(obj.Nullable.Value.RefId, Is.EqualTo(999u), "nullable");
            Assert.That(obj.Dict[5].RefId, Is.EqualTo(999u), "dictionary value");
            Assert.That(obj.Child.Single.RefId, Is.EqualTo(999u), "nested child field");
            Assert.That(obj.Child.List[0].RefId, Is.EqualTo(999u), "nested child list item");
            Assert.That(obj.ChildList[0].Single.RefId, Is.EqualTo(999u), "field of a child in a list");
            Assert.That(obj.ChildList[0].List[0].RefId, Is.EqualTo(999u), "list inside a child in a list");
        }

        [Test]
        public void LeavesOtherIdsAlone()
        {
            var obj = NewObjWithAllRefsPointingAt(2);

            new NeuroEditVisitor().Visit(obj, new Rewriter(1, 999));

            Assert.That(obj.Single.RefId, Is.EqualTo(2u));
            Assert.That(obj.List[0].RefId, Is.EqualTo(2u));
            Assert.That(obj.Nullable.Value.RefId, Is.EqualTo(2u));
            Assert.That(obj.Dict[5].RefId, Is.EqualTo(2u));
            Assert.That(obj.ChildList[0].List[0].RefId, Is.EqualTo(2u));
        }

        [Test]
        public void NeuroVisitorStaysReadOnly()
        {
            // NeuroVisitor is the pure walk - it hands out copies of what is inside lists, dictionaries and
            // nullables, so nothing a visitor writes there is kept. Anything that means to edit uses
            // NeuroEditVisitor instead.
            var obj = NewObjWithAllRefsPointingAt(1);

            new NeuroVisitor().Visit(obj, new ReadOnlyProbe());

            Assert.That(obj.List[0].RefId, Is.EqualTo(1u));
            Assert.That(obj.Nullable.Value.RefId, Is.EqualTo(1u));
            Assert.That(obj.Dict[5].RefId, Is.EqualTo(1u));
            Assert.That(obj.Child.List[0].RefId, Is.EqualTo(1u));
        }

        [Test]
        public void ReadOnlyVisitorLeavesTheObjectUntouched()
        {
            var obj = NewObjWithAllRefsPointingAt(1);
            var listBefore = obj.List;
            var dictBefore = obj.Dict;

            new NeuroEditVisitor().Visit(obj, new Rewriter(0, 0));

            Assert.That(obj.Single.RefId, Is.EqualTo(1u));
            Assert.That(obj.List[0].RefId, Is.EqualTo(1u));
            Assert.That(obj.Dict[5].RefId, Is.EqualTo(1u));
            Assert.That(obj.Dict.Count, Is.EqualTo(1));
            Assert.That(obj.List, Is.SameAs(listBefore));
            Assert.That(obj.Dict, Is.SameAs(dictBefore));
        }

        static RewriteTestClass NewObjWithAllRefsPointingAt(uint refId)
        {
            var obj = new RewriteTestClass();
            obj.Single.RefId = refId;
            obj.List.Add(new Reference<ReferencableClass>() { RefId = refId });
            obj.Nullable = new Reference<ReferencableClass>() { RefId = refId };
            obj.Dict[5] = new Reference<ReferencableClass>() { RefId = refId };
            obj.Child = NewChild(refId);
            obj.ChildList.Add(NewChild(refId));
            return obj;
        }

        static RewriteChildClass NewChild(uint refId)
        {
            var child = new RewriteChildClass();
            child.Single.RefId = refId;
            child.List.Add(new Reference<ReferencableClass>() { RefId = refId });
            return child;
        }

        class Rewriter : NeuroEditVisitor.IInterface
        {
            readonly uint from;
            readonly uint to;

            public Rewriter(uint from, uint to)
            {
                this.from = from;
                this.to = to;
            }

            public void BeginVisit<T>(ref T obj, string name, int? listIndex) { }
            public void EndVisit() { }

            public void VisitRef<T>(ref Reference<T> reference) where T : class, IReferencable
            {
                if (from != 0 && reference.RefId == from)
                {
                    reference.RefId = to;
                }
            }
        }

        /// Writes through NeuroVisitor's ref parameters, which that walk does not keep.
        class ReadOnlyProbe : NeuroVisitor.IInterface
        {
            public void BeginVisit<T>(ref T obj, string name, int? listIndex) { }
            public void EndVisit() { }

            public void VisitRef<T>(ref Reference<T> reference) where T : class, IReferencable
            {
                reference.RefId = 999;
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
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref RewriteChildClass value)
            {
                value ??= new RewriteChildClass();
                neuro.Sync(1, nameof(value.Single), ref value.Single);
                neuro.Sync(2, nameof(value.List), ref value.List);
            });
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref RewriteTestClass value)
            {
                value ??= new RewriteTestClass();
                neuro.Sync(1, nameof(value.Single), ref value.Single);
                neuro.Sync(2, nameof(value.List), ref value.List);
                neuro.Sync(3, nameof(value.Nullable), ref value.Nullable);
                neuro.Sync(4, nameof(value.Dict), ref value.Dict);
                neuro.Sync(5, nameof(value.Child), ref value.Child);
                neuro.Sync(6, nameof(value.ChildList), ref value.ChildList);
            });
        }

        class RewriteChildClass
        {
            public Reference<ReferencableClass> Single;
            public List<Reference<ReferencableClass>> List = new List<Reference<ReferencableClass>>();
        }

        class RewriteTestClass
        {
            public Reference<ReferencableClass> Single;
            public List<Reference<ReferencableClass>> List = new List<Reference<ReferencableClass>>();
            public Reference<ReferencableClass>? Nullable;
            public Dictionary<uint, Reference<ReferencableClass>> Dict = new Dictionary<uint, Reference<ReferencableClass>>();
            public RewriteChildClass Child;
            public List<RewriteChildClass> ChildList = new List<RewriteChildClass>();
        }
    }
}
