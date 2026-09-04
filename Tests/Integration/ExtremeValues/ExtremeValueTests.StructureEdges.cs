using System;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
// ---------------------------------------------------------------------------------------------------- structure edges

        public partial class DeepBox
        {
            [Neuro(1)] public DeepBox Child;
            [Neuro(2)] public int Depth;
        }

        static DeepBox BuildDeep(int depth)
        {
            var root = new DeepBox { Depth = 0 };
            var cursor = root;
            for (var i = 1; i < depth; i++)
            {
                cursor.Child = new DeepBox { Depth = i };
                cursor = cursor.Child;
            }
            return root;
        }

        static void AssertDeep(DeepBox copy, int depth)
        {
            var cursor = copy;
            for (var i = 0; i < depth; i++)
            {
                Assert.IsNotNull(cursor, $"ran out of nesting at depth {i}");
                Assert.AreEqual(i, cursor.Depth);
                cursor = cursor.Child;
            }
            Assert.IsNull(cursor, "more nesting than expected");
        }

        [TestCase(1), TestCase(2), TestCase(50), TestCase(500)]
        public void Deep_Binary(int depth) => AssertDeep(Bin(BuildDeep(depth)), depth);

        [TestCase(1), TestCase(2), TestCase(50), TestCase(500)]
        public void Deep_Json(int depth) => AssertDeep(Jsn(BuildDeep(depth)), depth);

        public partial class EmptyBox
        {
        }

        public partial class HolderOfEmpty
        {
            [Neuro(1)] public EmptyBox Empty;
            [Neuro(2)] public int After;
        }

        // A class with no [Neuro] members is not a serializable type, and is meant to be rejected rather than
        // silently written as an empty object. These pin that it fails loudly, and says which type is at fault.

        [Test]
        public void EmptyObject_FailsToSerialize_Binary()
        {
            var e = Assert.Throws<Exception>(() => Bin(new HolderOfEmpty { Empty = new EmptyBox(), After = 7 }));
            Assert.IsTrue(e.Message.Contains(nameof(EmptyBox)), e.Message);
        }

        [Test]
        public void EmptyObject_FailsToSerialize_Json()
        {
            var e = Assert.Throws<Exception>(() => Jsn(new HolderOfEmpty { Empty = new EmptyBox(), After = 7 }));
            Assert.IsTrue(e.Message.Contains(nameof(EmptyBox)), e.Message);
        }

        [Test]
        public void EmptySubClassOfNeuroBase_IsStillSerializable()
        {
            // a subclass that declares [Neuro(tag)] is opting in, so it works even with no members of its own.
            var copy = Bin(new PolyBox { Value = new PolyNoFields { Id = 5 } });
            Assert.IsInstanceOf<PolyNoFields>(copy.Value);
            Assert.AreEqual(5, copy.Value.Id);
        }

        public partial class BigTagBox
        {
            [Neuro(1)] public int First;
            [Neuro(127)] public int Tag127;
            [Neuro(128)] public int Tag128;
            [Neuro(16383)] public int Tag16383;
            [Neuro(16384)] public int Tag16384;
            [Neuro(1048576)] public int TagBig;
        }

        [Test]
        public void BigFieldTags_Binary()
        {
            var src = new BigTagBox { First = 1, Tag127 = 2, Tag128 = 3, Tag16383 = 4, Tag16384 = 5, TagBig = 6 };
            var copy = Bin(src);
            Assert.AreEqual(1, copy.First);
            Assert.AreEqual(2, copy.Tag127);
            Assert.AreEqual(3, copy.Tag128);
            Assert.AreEqual(4, copy.Tag16383);
            Assert.AreEqual(5, copy.Tag16384);
            Assert.AreEqual(6, copy.TagBig);
        }

        [Test]
        public void BigFieldTags_Json()
        {
            var src = new BigTagBox { First = 1, Tag127 = 2, Tag128 = 3, Tag16383 = 4, Tag16384 = 5, TagBig = 6 };
            var copy = Jsn(src);
            Assert.AreEqual(1, copy.First);
            Assert.AreEqual(2, copy.Tag127);
            Assert.AreEqual(3, copy.Tag128);
            Assert.AreEqual(4, copy.Tag16383);
            Assert.AreEqual(5, copy.Tag16384);
            Assert.AreEqual(6, copy.TagBig);
        }
    }
}
