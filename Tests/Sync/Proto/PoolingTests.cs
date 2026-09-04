using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    public class PoolingTests
    {
        private static bool _registered;

        [SetUp]
        public void SetUp()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            NeuroSyncTypes.Register<ContainerObj>((INeuroSync neuro, ref ContainerObj value) =>
            {
                value ??= neuro.GetPooled<ContainerObj>() ?? new ContainerObj();
                neuro.Sync(1, nameof(value.Child1), ref value.Child1);
                neuro.Sync(2, nameof(value.Child2), ref value.Child2);
                neuro.Sync(3, nameof(value.Children), ref value.Children);
            });
            NeuroSyncTypes.Register<ChildObj>((INeuroSync neuro, ref ChildObj value) =>
            {
                value ??= neuro.GetPooled<ChildObj>() ?? new ChildObj();
                neuro.Sync(1, nameof(value.Container), ref value.Container);
            });
            NeuroSyncTypes.RegisterSubClass<ChildObj, SubClassChildObj>(2, delegate(INeuroSync neuro, ref SubClassChildObj value)
            {
                value ??= neuro.GetPooled<SubClassChildObj>() ?? new SubClassChildObj();
                neuro.Sync(1, nameof(value.Container2), ref value.Container2);
            });
        }

        [Test]
        public void TestBasic()
        {
            var child1 = new ChildObj();
            var child2 = new ChildObj();
            var child3 = new ChildObj();
            var container = new ContainerObj();
            container.Child1 = child1;
            container.Children = new List<ChildObj>(){ child2 };
            
            var container2 = new ContainerObj();
            container2.Child1 = child3;
            child2.Container = container2;

            var pool = new TestPool();
            new NeuroPoolCollector().ReturnAllToPool(container, pool);
            Assert.AreEqual(5, pool.AllObjects.Count);
            Assert.IsTrue(pool.AllObjects.Contains(child1));
            Assert.IsTrue(pool.AllObjects.Contains(child2));
            Assert.IsTrue(pool.AllObjects.Contains(container));
            Assert.IsTrue(pool.AllObjects.Contains(container2));
            Assert.IsTrue(pool.AllObjects.Contains(child3));
            Assert.AreEqual(0, container.Children.Count);
            Assert.IsNull(container.Child1);
            Assert.IsNull(container.Child2);
            Assert.IsNull(container2.Child1);
        }

        [Test]
        public void TestSubclasses()
        {
            var child1 = new SubClassChildObj();
            var child2 = new ChildObj();
            var child3 = new SubClassChildObj();
            var child4 = new SubClassChildObj();
            var container = new ContainerObj();
            container.Child1 = child1;
            container.Children = new List<ChildObj>(){ child2, child3 };

            var container2 = new ContainerObj();
            container2.Child1 = child4;
            child2.Container = container2;
            
            var pool = new TestPool();
            new NeuroPoolCollector().ReturnAllToPool(container, pool);
            Assert.AreEqual(6, pool.AllObjects.Count);
            Assert.IsTrue(pool.AllObjects.Contains(child1));
            Assert.IsTrue(pool.AllObjects.Contains(child2));
            Assert.IsTrue(pool.AllObjects.Contains(container));
            Assert.IsTrue(pool.AllObjects.Contains(container2));
            Assert.IsTrue(pool.AllObjects.Contains(child3));
            Assert.IsTrue(pool.AllObjects.Contains(child4));
            Assert.AreEqual(0, container.Children.Count);
            Assert.IsNull(container.Child1);
            Assert.IsNull(container.Child2);
            Assert.IsNull(container2.Child1);
        }

        class TestPool : INeuroObjectPool
        {
            public List<object> AllObjects = new List<object>();
            public T Borrow<T>() where T : class
            {
                // this is just a very inefficent pool
                var index = AllObjects.FindIndex(o => o.GetType() == typeof(T));
                if (index >= 0)
                {
                    var obj = AllObjects[index];
                    AllObjects.RemoveAt(index);
                    return (T)obj;
                }
                return null;
            }

            public void Return(object obj)
            {
                if (AllObjects.Contains(obj))
                {
                    throw new Exception("Object already in list " + obj);
                }
                AllObjects.Add(obj);
            }
        }

        class ContainerObj : INeuroPoolable
        {
            public ChildObj Child1;
            public ChildObj Child2;
            public List<ChildObj> Children = new List<ChildObj>();
        }
        
        class ChildObj : INeuroPoolable
        {
            public ContainerObj Container;
        }
        
        class SubClassChildObj : ChildObj
        {
            public ContainerObj Container2;
        }
    }
}