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

            Assert.AreEqual(999u, obj.Single.RefId, "plain field");
            Assert.AreEqual(999u, obj.List[0].RefId, "list item");
            Assert.AreEqual(999u, obj.Nullable.Value.RefId, "nullable");
            Assert.AreEqual(999u, obj.Dict[5].RefId, "dictionary value");
            Assert.AreEqual(999u, obj.Child.Single.RefId, "nested child field");
            Assert.AreEqual(999u, obj.Child.List[0].RefId, "nested child list item");
            Assert.AreEqual(999u, obj.ChildList[0].Single.RefId, "field of a child in a list");
            Assert.AreEqual(999u, obj.ChildList[0].List[0].RefId, "list inside a child in a list");
        }

        [Test]
        public void LeavesOtherIdsAlone()
        {
            var obj = NewObjWithAllRefsPointingAt(2);

            new NeuroEditVisitor().Visit(obj, new Rewriter(1, 999));

            Assert.AreEqual(2u, obj.Single.RefId);
            Assert.AreEqual(2u, obj.List[0].RefId);
            Assert.AreEqual(2u, obj.Nullable.Value.RefId);
            Assert.AreEqual(2u, obj.Dict[5].RefId);
            Assert.AreEqual(2u, obj.ChildList[0].List[0].RefId);
        }

        [Test]
        public void NeuroVisitorStaysReadOnly()
        {
            // NeuroVisitor is the pure walk - it hands out copies of what is inside lists, dictionaries and
            // nullables, so nothing a visitor writes there is kept. Anything that means to edit uses
            // NeuroEditVisitor instead.
            var obj = NewObjWithAllRefsPointingAt(1);

            new NeuroVisitor().Visit(obj, new ReadOnlyProbe());

            Assert.AreEqual(1u, obj.List[0].RefId);
            Assert.AreEqual(1u, obj.Nullable.Value.RefId);
            Assert.AreEqual(1u, obj.Dict[5].RefId);
            Assert.AreEqual(1u, obj.Child.List[0].RefId);
        }

        [Test]
        public void ReadOnlyVisitorLeavesTheObjectUntouched()
        {
            var obj = NewObjWithAllRefsPointingAt(1);
            var listBefore = obj.List;
            var dictBefore = obj.Dict;

            new NeuroEditVisitor().Visit(obj, new Rewriter(0, 0));

            Assert.AreEqual(1u, obj.Single.RefId);
            Assert.AreEqual(1u, obj.List[0].RefId);
            Assert.AreEqual(1u, obj.Dict[5].RefId);
            Assert.AreEqual(1, obj.Dict.Count);
            Assert.AreSame(listBefore, obj.List);
            Assert.AreSame(dictBefore, obj.Dict);
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
