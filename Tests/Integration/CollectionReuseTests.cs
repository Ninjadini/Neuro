using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    /// Reading into an object that already holds a populated list is the pooling / no-allocation path.
    /// The items that are already there get reused, so every slot has to end up with the item the data
    /// actually describes - not whatever happened to be sitting in that slot before.
    public class CollectionReuseTests
    {
        public partial class Item
        {
            [Neuro(1)] public string Name;
            [Neuro(2)] public int Extra;
        }

        public partial class Holder
        {
            [Neuro(1)] public List<int> Nums;
            [Neuro(2)] public List<Item> Items;
        }

        static Holder MakeTarget()
        {
            return new Holder
            {
                Nums = new List<int> { 9, 9, 9 },
                Items = new List<Item>
                {
                    new Item { Name = "old0", Extra = 100 },
                    new Item { Name = "old1", Extra = 200 },
                    new Item { Name = "old2", Extra = 300 },
                }
            };
        }

        [Test]
        public void Binary_ListOfObjects_ReusesTheItemAtTheSameIndex()
        {
            var src = new Holder { Items = new List<Item> { new Item { Name = "a" }, new Item { Name = "b" } } };
            var target = MakeTarget();
            var item0 = target.Items[0];
            var item1 = target.Items[1];
            NeuroBytesReader.Shared.Read(NeuroBytesWriter.Shared.Write(src).ToArray(), ref target);

            Assert.That(target.Items.Count, Is.EqualTo(2));
            Assert.That(target.Items[0], Is.SameAs(item0));
            Assert.That(target.Items[1], Is.SameAs(item1));
            Assert.That(target.Items[0].Name, Is.EqualTo("a"));
            Assert.That(target.Items[1].Name, Is.EqualTo("b"));
        }

        [Test]
        public void Json_ListOfObjects_ReusesTheItemAtTheSameIndex()
        {
            var src = new Holder { Nums = new List<int> { 1, 2, 3 }, Items = new List<Item> { new Item { Name = "a" }, new Item { Name = "b" } } };
            var target = MakeTarget();
            var item0 = target.Items[0];
            var item1 = target.Items[1];
            NeuroJsonReader.Shared.Read(NeuroJsonWriter.Shared.Write(src), ref target);

            Assert.That(target.Items.Count, Is.EqualTo(2));
            // The list items live further down the node array than their own index, so reusing by node index
            // reads item 0 into item 1's instance - or allocates a fresh one and drops the reuse entirely.
            Assert.That(target.Items[0], Is.SameAs(item0));
            Assert.That(target.Items[1], Is.SameAs(item1));
            Assert.That(target.Items[0].Name, Is.EqualTo("a"));
            Assert.That(target.Items[1].Name, Is.EqualTo("b"));
        }

        [Test]
        public void Json_ListOfObjects_ReuseStillResetsAbsentFields()
        {
            var target = MakeTarget();
            var item0 = target.Items[0];
            // Reuse is about the allocation, not about merging - "Extra" is absent from the json so it goes
            // back to its default on every item, exactly as it would on a freshly allocated one.
            NeuroJsonReader.Shared.Read("{\"Items\":[{\"Name\":\"a\"},{\"Name\":\"b\"},{\"Name\":\"c\"}]}", ref target);

            Assert.That(target.Items[0], Is.SameAs(item0));
            Assert.That(target.Items[0].Name, Is.EqualTo("a"));
            Assert.That(target.Items[1].Name, Is.EqualTo("b"));
            Assert.That(target.Items[2].Name, Is.EqualTo("c"));
            Assert.That(target.Items[0].Extra, Is.EqualTo(0));
            Assert.That(target.Items[1].Extra, Is.EqualTo(0));
            Assert.That(target.Items[2].Extra, Is.EqualTo(0));
        }

        [Test]
        public void Binary_ListWithNulls_ClearsTheReusedItem()
        {
            var src = new Holder { Items = new List<Item> { new Item { Name = "a" }, null, new Item { Name = "c" } } };
            var target = MakeTarget();
            NeuroBytesReader.Shared.Read(NeuroBytesWriter.Shared.Write(src).ToArray(), ref target);

            Assert.That(target.Items[0].Name, Is.EqualTo("a"));
            Assert.That(target.Items[1], Is.Null);
            Assert.That(target.Items[2].Name, Is.EqualTo("c"));
        }

        public partial class BaseItem { [Neuro(1)] public int A; }
        [Neuro(1)] public partial class SubItem : BaseItem { [Neuro(1)] public int B; }
        public partial class PolyHolder { [Neuro(1)] public List<BaseItem> Items; }

        [Test]
        public void Binary_PolymorphicListWithNulls_ClearsTheReusedItem()
        {
            var src = new PolyHolder { Items = new List<BaseItem> { new SubItem { B = 1 }, null, new SubItem { B = 3 } } };
            var target = new PolyHolder { Items = new List<BaseItem> { new SubItem(), new SubItem(), new SubItem() } };
            NeuroBytesReader.Shared.Read(NeuroBytesWriter.Shared.Write(src).ToArray(), ref target);

            Assert.That(((SubItem)target.Items[0]).B, Is.EqualTo(1));
            Assert.That(target.Items[1], Is.Null);
            Assert.That(((SubItem)target.Items[2]).B, Is.EqualTo(3));
        }

        [Test]
        public void Json_ListWithNulls_ClearsTheReusedItem()
        {
            var target = MakeTarget();
            NeuroJsonReader.Shared.Read("{\"Items\":[{\"Name\":\"a\"},null,{\"Name\":\"c\"}]}", ref target);

            Assert.That(target.Items[0].Name, Is.EqualTo("a"));
            Assert.That(target.Items[1], Is.Null);
            Assert.That(target.Items[2].Name, Is.EqualTo("c"));
        }
    }
}
