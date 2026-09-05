using System.Collections.Generic;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    /// Returning an object tree to the pool has to reach every poolable in it. An item that is not poolable
    /// itself is not a dead end - it can be holding poolable objects further down.
    public class PoolCollectorTests
    {
        public partial class Poolable : INeuroPoolable
        {
            [Neuro(1)] public int V;
            public void OnReturnedToPool() { }
        }

        public partial class Wrapper
        {
            [Neuro(1)] public Poolable Item;
        }

        public partial struct WrapperStruct
        {
            [Neuro(1)] public Poolable Item;
        }

        public partial class Root
        {
            [Neuro(1)] public Poolable Direct;
            [Neuro(2)] public Wrapper Nested;
            [Neuro(3)] public List<Poolable> PoolableList;
            [Neuro(4)] public List<Wrapper> WrapperList;
            [Neuro(5)] public Dictionary<int, Wrapper> WrapperMap;
            [Neuro(6)] public List<int> Numbers;
            [Neuro(7)] public WrapperStruct? NullableWrapper;
        }

        static NeuroPoolCollector.BasicPool Collect(Root root)
        {
            var pool = new NeuroPoolCollector.BasicPool();
            NeuroPoolCollector.Shared.ReturnAllToPool(root, pool);
            return pool;
        }

        [Test]
        public void DirectAndNestedPoolables_AreReturned()
        {
            var pool = Collect(new Root { Direct = new Poolable(), Nested = new Wrapper { Item = new Poolable() } });
            Assert.That(pool.AllObjects.Count, Is.EqualTo(2));
        }

        [Test]
        public void PoolablesInsideListItems_AreReturned()
        {
            var root = new Root
            {
                PoolableList = new List<Poolable> { new Poolable(), new Poolable() },
                WrapperList = new List<Wrapper> { new Wrapper { Item = new Poolable() }, new Wrapper { Item = new Poolable() } },
            };
            var pool = Collect(root);
            Assert.That(pool.AllObjects.Count, Is.EqualTo(4));
            Assert.That(root.PoolableList, Is.Empty);
            Assert.That(root.WrapperList, Is.Empty);
        }

        [Test]
        public void PoolablesInsideDictionaryValues_AreReturned()
        {
            var root = new Root
            {
                WrapperMap = new Dictionary<int, Wrapper>
                {
                    { 1, new Wrapper { Item = new Poolable() } },
                    { 2, new Wrapper { Item = new Poolable() } },
                }
            };
            var pool = Collect(root);
            Assert.That(pool.AllObjects.Count, Is.EqualTo(2));
            Assert.That(root.WrapperMap, Is.Empty);
        }

        [Test]
        public void PoolablesInsideANullableStruct_AreReturned()
        {
            var pool = Collect(new Root { NullableWrapper = new WrapperStruct { Item = new Poolable() } });
            Assert.That(pool.AllObjects.Count, Is.EqualTo(1));
        }

        [Test]
        public void ListsWithNoPoolablesInThem_AreJustCleared()
        {
            var root = new Root { Numbers = new List<int> { 1, 2, 3 }, WrapperList = new List<Wrapper> { null, null } };
            var pool = Collect(root);
            Assert.That(pool.AllObjects, Is.Empty);
            Assert.That(root.Numbers, Is.Empty);
            Assert.That(root.WrapperList, Is.Empty);
        }

        [Test]
        public void NothingToReturn_IsFine()
        {
            Assert.That(Collect(new Root()).AllObjects, Is.Empty);
        }
    }
}
