using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
// ---------------------------------------------------------------------------------------------------- lists

        public partial class ListBox
        {
            [Neuro(1)] public List<int> Ints;
            [Neuro(2)] public List<string> Strings;
            [Neuro(3)] public List<float> Floats;
            [Neuro(4)] public List<ChildBox> Children;
        }

        public partial class ChildBox
        {
            [Neuro(1)] public int Id;
            [Neuro(2)] public string Name;
        }

        [Test]
        public void List_Null_Binary() => Assert.IsNull(Bin(new ListBox()).Ints);

        [Test]
        public void List_Null_Json() => Assert.IsNull(Jsn(new ListBox()).Ints);

        [Test]
        public void List_Empty_Binary()
        {
            var copy = Bin(new ListBox { Ints = new List<int>() });
            Assert.IsNotNull(copy.Ints, "empty list came back null");
            Assert.AreEqual(0, copy.Ints.Count);
        }

        [Test]
        public void List_Empty_Json()
        {
            var copy = Jsn(new ListBox { Ints = new List<int>() });
            Assert.IsNotNull(copy.Ints, "empty list came back null");
            Assert.AreEqual(0, copy.Ints.Count);
        }

        [Test]
        public void List_ExtremeInts_Binary()
        {
            var src = new ListBox { Ints = new List<int> { int.MinValue, -1, 0, 1, int.MaxValue } };
            CollectionAssert.AreEqual(src.Ints, Bin(src).Ints);
        }

        [Test]
        public void List_ExtremeInts_Json()
        {
            var src = new ListBox { Ints = new List<int> { int.MinValue, -1, 0, 1, int.MaxValue } };
            CollectionAssert.AreEqual(src.Ints, Jsn(src).Ints);
        }

        [Test]
        public void List_ExtremeFloats_Binary()
        {
            var src = new ListBox { Floats = new List<float> { float.MinValue, -0f, 0f, float.Epsilon, float.MaxValue, float.NaN, float.PositiveInfinity, float.NegativeInfinity } };
            CollectionAssert.AreEqual(src.Floats, Bin(src).Floats);
        }

        [Test]
        public void List_ExtremeFloats_Json()
        {
            var src = new ListBox { Floats = new List<float> { float.MinValue, -0f, 0f, float.Epsilon, float.MaxValue, float.NaN, float.PositiveInfinity, float.NegativeInfinity } };
            CollectionAssert.AreEqual(src.Floats, Jsn(src).Floats);
        }

        [Test]
        public void List_StringsWithNullsAndEmpties_Binary()
        {
            var src = new ListBox { Strings = new List<string> { null, "", "a", "with \"quote\"", "line\nbreak", null } };
            CollectionAssert.AreEqual(src.Strings, Bin(src).Strings);
        }

        [Test]
        public void List_StringsWithNullsAndEmpties_Json()
        {
            var src = new ListBox { Strings = new List<string> { null, "", "a", "with \"quote\"", "line\nbreak", null } };
            CollectionAssert.AreEqual(src.Strings, Jsn(src).Strings);
        }

        [Test]
        public void List_WithNullChildren_Binary()
        {
            var src = new ListBox { Children = new List<ChildBox> { null, new ChildBox { Id = 1 }, null } };
            var copy = Bin(src);
            Assert.AreEqual(3, copy.Children.Count);
            Assert.IsNull(copy.Children[0]);
            Assert.AreEqual(1, copy.Children[1].Id);
            Assert.IsNull(copy.Children[2]);
        }

        [Test]
        public void List_WithNullChildren_Json()
        {
            var src = new ListBox { Children = new List<ChildBox> { null, new ChildBox { Id = 1 }, null } };
            var copy = Jsn(src);
            Assert.AreEqual(3, copy.Children.Count);
            Assert.IsNull(copy.Children[0]);
            Assert.AreEqual(1, copy.Children[1].Id);
            Assert.IsNull(copy.Children[2]);
        }

        [Test]
        public void List_Large_Binary()
        {
            var src = new ListBox { Ints = new List<int>() };
            for (var i = 0; i < 100000; i++) src.Ints.Add(i);
            CollectionAssert.AreEqual(src.Ints, Bin(src).Ints);
        }

        [Test]
        public void List_Large_Json()
        {
            var src = new ListBox { Ints = new List<int>() };
            for (var i = 0; i < 100000; i++) src.Ints.Add(i);
            CollectionAssert.AreEqual(src.Ints, Jsn(src).Ints);
        }

// ---------------------------------------------------------------------------------------------------- dictionaries

        public partial class DictBox
        {
            [Neuro(1)] public Dictionary<int, string> IntKeys;
            [Neuro(2)] public Dictionary<string, string> StringKeys;
            [Neuro(3)] public Dictionary<ExtremeEnum, int> EnumKeys;
        }

        [Test]
        public void Dict_Null_Binary() => Assert.IsNull(Bin(new DictBox()).IntKeys);

        [Test]
        public void Dict_Null_Json() => Assert.IsNull(Jsn(new DictBox()).IntKeys);

        [Test]
        public void Dict_Empty_Binary()
        {
            var copy = Bin(new DictBox { IntKeys = new Dictionary<int, string>() });
            Assert.IsNotNull(copy.IntKeys, "empty dictionary came back null");
            Assert.AreEqual(0, copy.IntKeys.Count);
        }

        [Test]
        public void Dict_Empty_Json()
        {
            var copy = Jsn(new DictBox { IntKeys = new Dictionary<int, string>() });
            Assert.IsNotNull(copy.IntKeys, "empty dictionary came back null");
            Assert.AreEqual(0, copy.IntKeys.Count);
        }

        static Dictionary<int, string> ExtremeIntKeyed() => new Dictionary<int, string>
        {
            { int.MinValue, "min" }, { -1, "minus one" }, { 0, "zero" }, { 1, "one" }, { int.MaxValue, "max" }
        };

        [Test]
        public void Dict_ExtremeIntKeys_Binary()
        {
            var src = new DictBox { IntKeys = ExtremeIntKeyed() };
            CollectionAssert.AreEquivalent(src.IntKeys, Bin(src).IntKeys);
        }

        [Test]
        public void Dict_ExtremeIntKeys_Json()
        {
            var src = new DictBox { IntKeys = ExtremeIntKeyed() };
            CollectionAssert.AreEquivalent(src.IntKeys, Jsn(src).IntKeys);
        }

        static Dictionary<string, string> AwkwardStringKeyed() => new Dictionary<string, string>
        {
            { "plain", "a" },
            { "", "empty key" },
            { "with space", "b" },
            { "with \"quote\"", "c" },
            { "with\\backslash", "d" },
            { "with\nnewline", "e" },
            { "with\ttab", "e2" },
            { "with\rreturn", "e3" },
            { "with\u0000nul", "e4" },
            { "with:colon,comma{brace}", "f" },
            { "unicode 中文", "g" }
        };

        [Test]
        public void Dict_AwkwardStringKeys_Binary()
        {
            var src = new DictBox { StringKeys = AwkwardStringKeyed() };
            CollectionAssert.AreEquivalent(src.StringKeys, Bin(src).StringKeys);
        }

        [Test]
        public void Dict_AwkwardStringKeys_Json()
        {
            var src = new DictBox { StringKeys = AwkwardStringKeyed() };
            CollectionAssert.AreEquivalent(src.StringKeys, Jsn(src).StringKeys);
        }

        [Test]
        public void Dict_NullValues_Binary()
        {
            var src = new DictBox { IntKeys = new Dictionary<int, string> { { 1, null }, { 2, "" }, { 3, "x" } } };
            CollectionAssert.AreEquivalent(src.IntKeys, Bin(src).IntKeys);
        }

        [Test]
        public void Dict_NullValues_Json()
        {
            var src = new DictBox { IntKeys = new Dictionary<int, string> { { 1, null }, { 2, "" }, { 3, "x" } } };
            CollectionAssert.AreEquivalent(src.IntKeys, Jsn(src).IntKeys);
        }

        [Test]
        public void Dict_ExtremeEnumKeys_Binary()
        {
            var src = new DictBox { EnumKeys = new Dictionary<ExtremeEnum, int> { { ExtremeEnum.Min, 1 }, { ExtremeEnum.Max, 2 }, { ExtremeEnum.Zero, 3 } } };
            CollectionAssert.AreEquivalent(src.EnumKeys, Bin(src).EnumKeys);
        }

        [Test]
        public void Dict_ExtremeEnumKeys_Json()
        {
            var src = new DictBox { EnumKeys = new Dictionary<ExtremeEnum, int> { { ExtremeEnum.Min, 1 }, { ExtremeEnum.Max, 2 }, { ExtremeEnum.Zero, 3 } } };
            CollectionAssert.AreEquivalent(src.EnumKeys, Jsn(src).EnumKeys);
        }
    }
}
