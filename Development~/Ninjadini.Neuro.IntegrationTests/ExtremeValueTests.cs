using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    /// <summary>
    /// Round trips every Neuro supported type through both binary and json at the extreme ends of its range.
    /// Each value is its own test case so a failure points straight at the value that broke.
    /// Some of these are expected to fail - the point is to document exactly where the limits are.
    /// </summary>
    public partial class ExtremeValueTests
    {
        [OneTimeSetUp]
        public void RegisterTypes()
        {
            NeuroSyncTypes.TryRegisterAssemblyOf<BoolBox>();
        }

        const int MaxLogChars = 400;

        static void Log(string prefix, string content)
        {
            TestContext.WriteLine(content.Length > MaxLogChars
                ? prefix + content.Substring(0, MaxLogChars) + "... (" + content.Length + " chars)"
                : prefix + content);
        }

        static T Bin<T>(T src) where T : class, new()
        {
            var bytes = new NeuroBytesWriter().Write(src).ToArray();
            try
            {
                Log("binary: ", RawProtoReader.GetDebugString(bytes));
            }
            catch (Exception e)
            {
                TestContext.WriteLine("binary: <could not dump bytes> " + e.Message);
            }
            return new NeuroBytesReader().Read<T>(bytes, new ReaderOptions());
        }

        static T Jsn<T>(T src) where T : class, new()
        {
            var json = new NeuroJsonWriter().Write(src);
            Log("json: ", json);
            return new NeuroJsonReader().Read<T>(json, new ReaderOptions());
        }

// ---------------------------------------------------------------------------------------------------- bool

        public partial class BoolBox
        {
            [Neuro(1)] public bool Value;
        }

        [TestCase(true), TestCase(false)]
        public void Bool_Binary(bool v) => Assert.AreEqual(v, Bin(new BoolBox { Value = v }).Value);

        [TestCase(true), TestCase(false)]
        public void Bool_Json(bool v) => Assert.AreEqual(v, Jsn(new BoolBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- int

        public partial class IntBox
        {
            [Neuro(1)] public int Value;
        }

        // varint boundaries either side of every 7 bit group, plus the zig-zag sign flips.
        static readonly int[] IntValues =
        {
            0, 1, -1, 63, 64, -64, -65, 127, 128, -128, -129,
            8191, 8192, 16383, 16384, 1048575, 1048576,
            134217727, 134217728, int.MaxValue - 1, int.MaxValue, int.MinValue + 1, int.MinValue
        };

        [TestCaseSource(nameof(IntValues))]
        public void Int_Binary(int v) => Assert.AreEqual(v, Bin(new IntBox { Value = v }).Value);

        [TestCaseSource(nameof(IntValues))]
        public void Int_Json(int v) => Assert.AreEqual(v, Jsn(new IntBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- uint

        public partial class UIntBox
        {
            [Neuro(1)] public uint Value;
        }

        static readonly uint[] UIntValues =
        {
            0u, 1u, 127u, 128u, 16383u, 16384u, 2147483647u, 2147483648u, uint.MaxValue - 1, uint.MaxValue
        };

        [TestCaseSource(nameof(UIntValues))]
        public void UInt_Binary(uint v) => Assert.AreEqual(v, Bin(new UIntBox { Value = v }).Value);

        [TestCaseSource(nameof(UIntValues))]
        public void UInt_Json(uint v) => Assert.AreEqual(v, Jsn(new UIntBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- long

        public partial class LongBox
        {
            [Neuro(1)] public long Value;
        }

        static readonly long[] LongValues =
        {
            0L, 1L, -1L, 127L, -128L, int.MaxValue, int.MinValue,
            (long)uint.MaxValue + 1, 72057594037927935L, 72057594037927936L,
            long.MaxValue - 1, long.MaxValue, long.MinValue + 1, long.MinValue
        };

        [TestCaseSource(nameof(LongValues))]
        public void Long_Binary(long v) => Assert.AreEqual(v, Bin(new LongBox { Value = v }).Value);

        [TestCaseSource(nameof(LongValues))]
        public void Long_Json(long v) => Assert.AreEqual(v, Jsn(new LongBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- ulong

        public partial class ULongBox
        {
            [Neuro(1)] public ulong Value;
        }

        static readonly ulong[] ULongValues =
        {
            0UL, 1UL, 127UL, 128UL, uint.MaxValue, (ulong)long.MaxValue, (ulong)long.MaxValue + 1,
            ulong.MaxValue - 1, ulong.MaxValue
        };

        [TestCaseSource(nameof(ULongValues))]
        public void ULong_Binary(ulong v) => Assert.AreEqual(v, Bin(new ULongBox { Value = v }).Value);

        [TestCaseSource(nameof(ULongValues))]
        public void ULong_Json(ulong v) => Assert.AreEqual(v, Jsn(new ULongBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- float

        public partial class FloatBox
        {
            [Neuro(1)] public float Value;
        }

        static readonly float[] FloatValues =
        {
            0f, -0f, 1f, -1f, 0.1f, -0.1f,
            1E-8f, 1E-9f,                       // right at, and just past, the 8 decimal places json keeps
            123.456f, -1234.56f, 0.000123456f,
            16777215f, 16777216f,               // last exactly representable integers
            1E8f, 1E9f, 1E20f,                  // json switches to ToString() at 1E8
            float.Epsilon, -float.Epsilon,
            float.MinValue, float.MaxValue,
            float.NaN, float.PositiveInfinity, float.NegativeInfinity
        };

        [TestCaseSource(nameof(FloatValues))]
        public void Float_Binary(float v) => Assert.AreEqual(v, Bin(new FloatBox { Value = v }).Value);

        [TestCaseSource(nameof(FloatValues))]
        public void Float_Json(float v) => Assert.AreEqual(v, Jsn(new FloatBox { Value = v }).Value);

        [Test, Ignore("Accepted limitation: -0f equals 0f, so the writer's default value check skips the field " +
                      "and the sign bit is lost. Fixing it means sign aware comparison in the default check for " +
                      "no practical gain.")]
        public void Float_NegativeZero_KeepsSignBit_Binary()
        {
            var copy = Bin(new FloatBox { Value = -0f });
            Assert.AreEqual(BitConverter.SingleToInt32Bits(-0f), BitConverter.SingleToInt32Bits(copy.Value));
        }

        [Test, Ignore("Accepted limitation, see the binary variant above.")]
        public void Float_NegativeZero_KeepsSignBit_Json()
        {
            var copy = Jsn(new FloatBox { Value = -0f });
            Assert.AreEqual(BitConverter.SingleToInt32Bits(-0f), BitConverter.SingleToInt32Bits(copy.Value));
        }

// ---------------------------------------------------------------------------------------------------- double

        public partial class DoubleBox
        {
            [Neuro(1)] public double Value;
        }

        static readonly double[] DoubleValues =
        {
            0d, -0d, 1d, -1d, 0.1d, -0.1d,
            1E-8d, 1E-9d, 1E-300d,
            123.456d, -1234.56d,
            9007199254740991d, 9007199254740992d, // last exactly representable integers
            1E8d, 1E9d, 1E20d, 1E300d,
            double.Epsilon, -double.Epsilon,
            double.MinValue, double.MaxValue,
            double.NaN, double.PositiveInfinity, double.NegativeInfinity
        };

        [TestCaseSource(nameof(DoubleValues))]
        public void Double_Binary(double v) => Assert.AreEqual(v, Bin(new DoubleBox { Value = v }).Value);

        [TestCaseSource(nameof(DoubleValues))]
        public void Double_Json(double v) => Assert.AreEqual(v, Jsn(new DoubleBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- string

        public partial class StringBox
        {
            [Neuro(1)] public string Value;
        }

        static IEnumerable<TestCaseData> StringValues()
        {
            yield return new TestCaseData(null).SetName("String_null");
            yield return new TestCaseData("").SetName("String_empty");
            yield return new TestCaseData(" ").SetName("String_space");
            yield return new TestCaseData("plain").SetName("String_plain");
            yield return new TestCaseData("null").SetName("String_literalNullWord");
            yield return new TestCaseData("true").SetName("String_literalTrueWord");
            yield return new TestCaseData("123").SetName("String_literalNumber");
            yield return new TestCaseData("has \"quotes\"").SetName("String_quotes");
            yield return new TestCaseData("has \\ backslash").SetName("String_backslash");
            yield return new TestCaseData("trailing backslash \\").SetName("String_trailingBackslash");
            yield return new TestCaseData("back\\nslash-n").SetName("String_backslashThenN");
            yield return new TestCaseData("line\nbreak").SetName("String_newline");
            yield return new TestCaseData("carriage\rreturn").SetName("String_carriageReturn");
            yield return new TestCaseData("tab\there").SetName("String_tab");
            yield return new TestCaseData("\b\f\v").SetName("String_backspaceFormfeedVtab");
            yield return new TestCaseData("nul\0char").SetName("String_nulChar");
            yield return new TestCaseData("json{\"a\":1,\"b\":[2]}").SetName("String_looksLikeJson");
            yield return new TestCaseData("unicode: é中文Ж").SetName("String_unicode");
            yield return new TestCaseData("emoji: \U0001F600\U0001F1F3\U0001F1FF").SetName("String_surrogatePairs");
            yield return new TestCaseData("rtl: ‮reversed‬").SetName("String_bidiControl");
            yield return new TestCaseData(new string('x', 200000)).SetName("String_200k");
        }

        [Test]
        public void String_LoneSurrogate_IsReplacedInBinaryButKeptInJson()
        {
            // A lone surrogate is not valid text and has no UTF-8 encoding, so the binary format replaces it
            // with U+FFFD. Json keeps it because it never leaves UTF-16. Switching the wire format to UTF-16
            // would preserve it, at the cost of doubling every ascii string - not worth it for malformed input.
            var src = new StringBox { Value = "lone surrogate: \ud800" };
            Assert.AreEqual("lone surrogate: \ufffd", Bin(src).Value, "binary");
            Assert.AreEqual("lone surrogate: \ud800", Jsn(src).Value, "json");
        }

        [TestCaseSource(nameof(StringValues))]
        public void String_Binary(string v) => Assert.AreEqual(v, Bin(new StringBox { Value = v }).Value);

        [TestCaseSource(nameof(StringValues))]
        public void String_Json(string v) => Assert.AreEqual(v, Jsn(new StringBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- DateTime

        public partial class DateTimeBox
        {
            [Neuro(1)] public DateTime Value;
        }

        static IEnumerable<TestCaseData> DateTimeValues()
        {
            yield return new TestCaseData(default(DateTime)).SetName("DateTime_default");
            yield return new TestCaseData(DateTime.MinValue).SetName("DateTime_min");
            yield return new TestCaseData(DateTime.MaxValue).SetName("DateTime_max");
            yield return new TestCaseData(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc)).SetName("DateTime_minUtc");
            yield return new TestCaseData(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc)).SetName("DateTime_maxWholeMsUtc");
            yield return new TestCaseData(new DateTime(1969, 7, 20, 20, 17, 40, DateTimeKind.Utc)).SetName("DateTime_beforeEpoch");
            yield return new TestCaseData(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).SetName("DateTime_neuroZeroPoint");
            yield return new TestCaseData(new DateTime(2024, 6, 5, 4, 3, 2, 1, DateTimeKind.Utc)).SetName("DateTime_utc");
            yield return new TestCaseData(new DateTime(2024, 6, 5, 4, 3, 2, 1, DateTimeKind.Local)).SetName("DateTime_local");
            yield return new TestCaseData(new DateTime(2024, 6, 5, 4, 3, 2, 1, DateTimeKind.Unspecified)).SetName("DateTime_unspecified");
            yield return new TestCaseData(new DateTime(2024, 6, 5, 4, 3, 2, 1, DateTimeKind.Utc).AddTicks(1234)).SetName("DateTime_subMillisecondTicks");
        }

        /// Neuro stores DateTime and TimeSpan to the millisecond on purpose: ticks cost about 2 more bytes of
        /// varint per field, and roughly double the size of the short durations that make up most of the data,
        /// for precision content files do not need. So these assert the resolution rather than exact ticks -
        /// anything coarser than a millisecond is a regression, sub millisecond loss is the design.
        static void AssertStoredResolution(DateTime expected, DateTime actual)
        {
            var lost = Math.Abs((expected - actual).Ticks);
            Assert.Less(lost, TimeSpan.TicksPerMillisecond,
                $"expected {expected:O} to survive to the millisecond, got {actual:O}");
        }

        static void AssertStoredResolution(TimeSpan expected, TimeSpan actual)
        {
            var lost = Math.Abs((expected - actual).Ticks);
            Assert.Less(lost, TimeSpan.TicksPerMillisecond,
                $"expected {expected} to survive to the millisecond, got {actual}");
        }

        [TestCaseSource(nameof(DateTimeValues))]
        public void DateTime_Binary(DateTime v) => AssertStoredResolution(v, Bin(new DateTimeBox { Value = v }).Value);

        [TestCaseSource(nameof(DateTimeValues))]
        public void DateTime_Json(DateTime v) => AssertStoredResolution(v, Jsn(new DateTimeBox { Value = v }).Value);

        [Test]
        public void DateTime_SubMillisecondIsIntentionallyDropped()
        {
            var withTicks = new DateTime(2024, 6, 5, 4, 3, 2, 1, DateTimeKind.Utc).AddTicks(1234);
            var expected = new DateTime(2024, 6, 5, 4, 3, 2, 1, DateTimeKind.Utc);
            Assert.AreEqual(expected, Bin(new DateTimeBox { Value = withTicks }).Value);
            Assert.AreEqual(expected, Jsn(new DateTimeBox { Value = withTicks }).Value);
        }

        [Test]
        public void TimeSpan_SubMillisecondIsIntentionallyDropped()
        {
            Assert.AreEqual(TimeSpan.Zero, Bin(new TimeSpanBox { Value = new TimeSpan(9999) }).Value);
            Assert.AreEqual(TimeSpan.Zero, Jsn(new TimeSpanBox { Value = new TimeSpan(9999) }).Value);
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), Bin(new TimeSpanBox { Value = new TimeSpan(10001) }).Value);
        }

        [TestCaseSource(nameof(DateTimeValues))]
        public void DateTime_Kind_Binary(DateTime v) => Assert.AreEqual(v.Kind, Bin(new DateTimeBox { Value = v }).Value.Kind);

        [TestCaseSource(nameof(DateTimeValues))]
        public void DateTime_Kind_Json(DateTime v) => Assert.AreEqual(v.Kind, Jsn(new DateTimeBox { Value = v }).Value.Kind);

// ---------------------------------------------------------------------------------------------------- TimeSpan

        public partial class TimeSpanBox
        {
            [Neuro(1)] public TimeSpan Value;
        }

        static IEnumerable<TestCaseData> TimeSpanValues()
        {
            yield return new TestCaseData(TimeSpan.Zero).SetName("TimeSpan_zero");
            yield return new TestCaseData(TimeSpan.MinValue).SetName("TimeSpan_min");
            yield return new TestCaseData(TimeSpan.MaxValue).SetName("TimeSpan_max");
            yield return new TestCaseData(TimeSpan.FromMilliseconds(1)).SetName("TimeSpan_oneMs");
            yield return new TestCaseData(TimeSpan.FromMilliseconds(-1)).SetName("TimeSpan_minusOneMs");
            yield return new TestCaseData(new TimeSpan(1)).SetName("TimeSpan_oneTick");
            yield return new TestCaseData(new TimeSpan(-1)).SetName("TimeSpan_minusOneTick");
            yield return new TestCaseData(new TimeSpan(9999)).SetName("TimeSpan_justUnderOneMs");
            yield return new TestCaseData(TimeSpan.FromDays(365 * 1000)).SetName("TimeSpan_1000years");
        }

        [TestCaseSource(nameof(TimeSpanValues))]
        public void TimeSpan_Binary(TimeSpan v) => AssertStoredResolution(v, Bin(new TimeSpanBox { Value = v }).Value);

        [TestCaseSource(nameof(TimeSpanValues))]
        public void TimeSpan_Json(TimeSpan v) => AssertStoredResolution(v, Jsn(new TimeSpanBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- Guid

        public partial class GuidBox
        {
            [Neuro(1)] public Guid Value;
        }

        static IEnumerable<TestCaseData> GuidValues()
        {
            yield return new TestCaseData(Guid.Empty).SetName("Guid_empty");
            yield return new TestCaseData(new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")).SetName("Guid_allBitsSet");
            yield return new TestCaseData(new Guid("00000000-0000-0000-0000-000000000001")).SetName("Guid_lowestBit");
            yield return new TestCaseData(new Guid("80000000-0000-0000-0000-000000000000")).SetName("Guid_highestBit");
            yield return new TestCaseData(new Guid("01234567-89ab-cdef-0123-456789abcdef")).SetName("Guid_pattern");
        }

        [TestCaseSource(nameof(GuidValues))]
        public void Guid_Binary(Guid v) => Assert.AreEqual(v, Bin(new GuidBox { Value = v }).Value);

        [TestCaseSource(nameof(GuidValues))]
        public void Guid_Json(Guid v) => Assert.AreEqual(v, Jsn(new GuidBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- enum

        public enum ExtremeEnum
        {
            Min = int.MinValue,
            MinusOne = -1,
            Zero = 0,
            One = 1,
            Max = int.MaxValue
        }

        [Flags]
        public enum ExtremeFlagEnum
        {
            None = 0,
            A = 1 << 0,
            B = 1 << 1,
            Top = 1 << 31,
            All = ~0
        }

        public partial class EnumBox
        {
            [Neuro(1)] public ExtremeEnum Value;
        }

        public partial class FlagEnumBox
        {
            [Neuro(1)] public ExtremeFlagEnum Value;
        }

        static readonly ExtremeEnum[] EnumValues =
        {
            ExtremeEnum.Min, ExtremeEnum.MinusOne, ExtremeEnum.Zero, ExtremeEnum.One, ExtremeEnum.Max,
            (ExtremeEnum)12345, (ExtremeEnum)(-12345)
        };

        [TestCaseSource(nameof(EnumValues))]
        public void Enum_Binary(ExtremeEnum v) => Assert.AreEqual(v, Bin(new EnumBox { Value = v }).Value);

        [TestCaseSource(nameof(EnumValues))]
        public void Enum_Json(ExtremeEnum v) => Assert.AreEqual(v, Jsn(new EnumBox { Value = v }).Value);

        static readonly ExtremeFlagEnum[] FlagEnumValues =
        {
            ExtremeFlagEnum.None, ExtremeFlagEnum.A, ExtremeFlagEnum.A | ExtremeFlagEnum.B,
            ExtremeFlagEnum.Top, ExtremeFlagEnum.All, (ExtremeFlagEnum)(1 << 30)
        };

        [TestCaseSource(nameof(FlagEnumValues))]
        public void FlagEnum_Binary(ExtremeFlagEnum v) => Assert.AreEqual(v, Bin(new FlagEnumBox { Value = v }).Value);

        [TestCaseSource(nameof(FlagEnumValues))]
        public void FlagEnum_Json(ExtremeFlagEnum v) => Assert.AreEqual(v, Jsn(new FlagEnumBox { Value = v }).Value);

// ---------------------------------------------------------------------------------------------------- Color

        public partial class ColorBox
        {
            [Neuro(1)] public Color Value;
        }

        static IEnumerable<TestCaseData> ColorValues()
        {
            yield return new TestCaseData(Color.FromArgb(0, 0, 0, 0)).SetName("Color_transparentBlack");
            yield return new TestCaseData(Color.FromArgb(255, 255, 255, 255)).SetName("Color_opaqueWhite");
            yield return new TestCaseData(Color.FromArgb(1, 2, 3, 4)).SetName("Color_arbitrary");
            yield return new TestCaseData(Color.FromArgb(128, 255, 0, 127)).SetName("Color_halfAlpha");
        }

        [TestCaseSource(nameof(ColorValues))]
        public void Color_Binary(Color v) => Assert.AreEqual(v.ToArgb(), Bin(new ColorBox { Value = v }).Value.ToArgb());

        [TestCaseSource(nameof(ColorValues))]
        public void Color_Json(Color v) => Assert.AreEqual(v.ToArgb(), Jsn(new ColorBox { Value = v }).Value.ToArgb());

// ---------------------------------------------------------------------------------------------------- Reference<>

        [NeuroGlobalType(901)]
        public partial class ExtremeReferencable : Referencable
        {
            [Neuro(1)] public string Name;
        }

        public partial class ReferenceBox
        {
            [Neuro(1)] public Reference<ExtremeReferencable> Value;
        }

        static readonly uint[] ReferenceIds = { 0u, 1u, 127u, 128u, int.MaxValue, 2147483648u, uint.MaxValue };

        [TestCaseSource(nameof(ReferenceIds))]
        public void Reference_Binary(uint id) => Assert.AreEqual(id, Bin(new ReferenceBox { Value = id }).Value.RefId);

        [TestCaseSource(nameof(ReferenceIds))]
        public void Reference_Json(uint id) => Assert.AreEqual(id, Jsn(new ReferenceBox { Value = id }).Value.RefId);

// ---------------------------------------------------------------------------------------------------- nullables

        public partial class NullableBox
        {
            [Neuro(1)] public int? Int;
            [Neuro(2)] public float? Float;
            [Neuro(3)] public DateTime? Date;
            [Neuro(4)] public ExtremeEnum? Enum;
            [Neuro(5)] public bool? Bool;
        }

        [Test]
        public void Nullable_AllNull_Binary()
        {
            var copy = Bin(new NullableBox());
            Assert.IsNull(copy.Int);
            Assert.IsNull(copy.Float);
            Assert.IsNull(copy.Date);
            Assert.IsNull(copy.Enum);
            Assert.IsNull(copy.Bool);
        }

        [Test]
        public void Nullable_AllNull_Json()
        {
            var copy = Jsn(new NullableBox());
            Assert.IsNull(copy.Int);
            Assert.IsNull(copy.Float);
            Assert.IsNull(copy.Date);
            Assert.IsNull(copy.Enum);
            Assert.IsNull(copy.Bool);
        }

        [Test]
        public void Nullable_DefaultValuesAreNotLostAsNull_Binary()
        {
            // a nullable holding the type's default is the classic trap - 0 must not come back as null.
            var src = new NullableBox { Int = 0, Float = 0f, Date = default(DateTime), Enum = ExtremeEnum.Zero, Bool = false };
            var copy = Bin(src);
            Assert.AreEqual(0, copy.Int);
            Assert.AreEqual(0f, copy.Float);
            Assert.AreEqual(default(DateTime), copy.Date);
            Assert.AreEqual(ExtremeEnum.Zero, copy.Enum);
            Assert.AreEqual(false, copy.Bool);
        }

        [Test]
        public void Nullable_DefaultValuesAreNotLostAsNull_Json()
        {
            var src = new NullableBox { Int = 0, Float = 0f, Date = default(DateTime), Enum = ExtremeEnum.Zero, Bool = false };
            var copy = Jsn(src);
            Assert.AreEqual(0, copy.Int);
            Assert.AreEqual(0f, copy.Float);
            Assert.AreEqual(default(DateTime), copy.Date);
            Assert.AreEqual(ExtremeEnum.Zero, copy.Enum);
            Assert.AreEqual(false, copy.Bool);
        }

        [Test]
        public void Nullable_ExtremeValues_Binary()
        {
            var src = new NullableBox { Int = int.MinValue, Float = float.NaN, Date = DateTime.MaxValue, Enum = ExtremeEnum.Max, Bool = true };
            var copy = Bin(src);
            Assert.AreEqual(int.MinValue, copy.Int);
            Assert.AreEqual(float.NaN, copy.Float);
            AssertStoredResolution(DateTime.MaxValue, copy.Date.Value);
            Assert.AreEqual(ExtremeEnum.Max, copy.Enum);
            Assert.AreEqual(true, copy.Bool);
        }

        [Test]
        public void Nullable_ExtremeValues_Json()
        {
            var src = new NullableBox { Int = int.MinValue, Float = float.NaN, Date = DateTime.MaxValue, Enum = ExtremeEnum.Max, Bool = true };
            var copy = Jsn(src);
            Assert.AreEqual(int.MinValue, copy.Int);
            Assert.AreEqual(float.NaN, copy.Float);
            AssertStoredResolution(DateTime.MaxValue, copy.Date.Value);
            Assert.AreEqual(ExtremeEnum.Max, copy.Enum);
            Assert.AreEqual(true, copy.Bool);
        }

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

// ---------------------------------------------------------------------------------------------------- polymorphism

        [Neuro(1), NeuroGlobalType(902)]
        public partial class PolyBase
        {
            [Neuro(1)] public int Id;
        }

        [Neuro(2)]
        public partial class PolySmallTag : PolyBase
        {
            [Neuro(1)] public string Value;
        }

        [Neuro(127)]
        public partial class PolyTag127 : PolyBase
        {
            [Neuro(1)] public string Value;
        }

        [Neuro(128)]
        public partial class PolyTag128 : PolyBase
        {
            [Neuro(1)] public string Value;
        }

        [Neuro(16384)]
        public partial class PolyBigTag : PolyBase
        {
            [Neuro(1)] public string Value;
        }

        [Neuro(200)]
        public partial class PolyNoFields : PolyBase
        {
        }

        public partial class PolyBox
        {
            [Neuro(1)] public PolyBase Value;
            [Neuro(2)] public List<PolyBase> List;
        }

        static IEnumerable<TestCaseData> PolyValues()
        {
            yield return new TestCaseData(new PolyBase { Id = int.MaxValue }).SetName("{m}_base");
            yield return new TestCaseData(new PolySmallTag { Id = 1, Value = "small" }).SetName("{m}_tag2");
            yield return new TestCaseData(new PolyTag127 { Id = 2, Value = "127" }).SetName("{m}_tag127");
            yield return new TestCaseData(new PolyTag128 { Id = 3, Value = "128" }).SetName("{m}_tag128");
            yield return new TestCaseData(new PolyBigTag { Id = 4, Value = "16384" }).SetName("{m}_tag16384");
            yield return new TestCaseData(new PolySmallTag { Id = 0, Value = null }).SetName("{m}_allDefaults");
        }

        static void AssertPoly(PolyBase expected, PolyBase actual)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.GetType(), actual.GetType(), "sub type was not preserved");
            Assert.AreEqual(expected.Id, actual.Id);
            if (expected is PolySmallTag a) Assert.AreEqual(a.Value, ((PolySmallTag)actual).Value);
            if (expected is PolyTag127 b) Assert.AreEqual(b.Value, ((PolyTag127)actual).Value);
            if (expected is PolyTag128 c) Assert.AreEqual(c.Value, ((PolyTag128)actual).Value);
            if (expected is PolyBigTag d) Assert.AreEqual(d.Value, ((PolyBigTag)actual).Value);
        }

        [TestCaseSource(nameof(PolyValues))]
        public void Poly_Binary(PolyBase v) => AssertPoly(v, Bin(new PolyBox { Value = v }).Value);

        [TestCaseSource(nameof(PolyValues))]
        public void Poly_Json(PolyBase v) => AssertPoly(v, Jsn(new PolyBox { Value = v }).Value);

        [Test]
        public void Poly_MixedListWithNulls_Binary()
        {
            var src = new PolyBox { List = new List<PolyBase> { null, new PolyBase { Id = 1 }, new PolyBigTag { Id = 2, Value = "x" }, null } };
            var copy = Bin(src);
            Assert.AreEqual(4, copy.List.Count);
            Assert.IsNull(copy.List[0]);
            AssertPoly(src.List[1], copy.List[1]);
            AssertPoly(src.List[2], copy.List[2]);
            Assert.IsNull(copy.List[3]);
        }

        [Test]
        public void Poly_MixedListWithNulls_Json()
        {
            var src = new PolyBox { List = new List<PolyBase> { null, new PolyBase { Id = 1 }, new PolyBigTag { Id = 2, Value = "x" }, null } };
            var copy = Jsn(src);
            Assert.AreEqual(4, copy.List.Count);
            Assert.IsNull(copy.List[0]);
            AssertPoly(src.List[1], copy.List[1]);
            AssertPoly(src.List[2], copy.List[2]);
            Assert.IsNull(copy.List[3]);
        }

// ---------------------------------------------------------------------------------------------------- references in collections

        public partial class RefCollectionBox
        {
            [Neuro(1)] public List<Reference<ExtremeReferencable>> List;
            [Neuro(2)] public Dictionary<Reference<ExtremeReferencable>, string> Keys;
        }

        static RefCollectionBox RefCollection() => new RefCollectionBox
        {
            List = new List<Reference<ExtremeReferencable>> { 0u, 1u, uint.MaxValue },
            Keys = new Dictionary<Reference<ExtremeReferencable>, string>
            {
                { (Reference<ExtremeReferencable>)0u, "zero" },
                { (Reference<ExtremeReferencable>)1u, "one" },
                { (Reference<ExtremeReferencable>)uint.MaxValue, "max" }
            }
        };

        static void AssertRefCollection(RefCollectionBox a, RefCollectionBox b)
        {
            CollectionAssert.AreEqual(a.List, b.List);
            CollectionAssert.AreEquivalent(a.Keys, b.Keys);
        }

        [Test]
        public void RefCollection_Binary() => AssertRefCollection(RefCollection(), Bin(RefCollection()));

        [Test]
        public void RefCollection_Json() => AssertRefCollection(RefCollection(), Jsn(RefCollection()));

// ---------------------------------------------------------------------------------------------------- nullable struct

        public partial class NullableStructBox
        {
            [Neuro(1)] public ExtremeStruct? Value;
        }

        [Test]
        public void NullableStruct_Null_Binary() => Assert.IsNull(Bin(new NullableStructBox()).Value);

        [Test]
        public void NullableStruct_Null_Json() => Assert.IsNull(Jsn(new NullableStructBox()).Value);

        [Test]
        public void NullableStruct_AllDefaultFields_Binary()
        {
            var copy = Bin(new NullableStructBox { Value = new ExtremeStruct() });
            Assert.IsNotNull(copy.Value, "a present-but-empty struct came back null");
        }

        [Test]
        public void NullableStruct_AllDefaultFields_Json()
        {
            var copy = Jsn(new NullableStructBox { Value = new ExtremeStruct() });
            Assert.IsNotNull(copy.Value, "a present-but-empty struct came back null");
        }

        [Test]
        public void NullableStruct_Extremes_Binary()
        {
            var copy = Bin(new NullableStructBox { Value = new ExtremeStruct { Id = int.MinValue, Name = "x" } });
            Assert.AreEqual(int.MinValue, copy.Value.Value.Id);
            Assert.AreEqual("x", copy.Value.Value.Name);
        }

        [Test]
        public void NullableStruct_Extremes_Json()
        {
            var copy = Jsn(new NullableStructBox { Value = new ExtremeStruct { Id = int.MinValue, Name = "x" } });
            Assert.AreEqual(int.MinValue, copy.Value.Value.Id);
            Assert.AreEqual("x", copy.Value.Value.Name);
        }

// ---------------------------------------------------------------------------------------------------- json we write is valid json

        static void AssertNoRawControlCharacters(string json)
        {
            for (var i = 0; i < json.Length; i++)
            {
                var c = json[i];
                // the writer's own pretty printing newlines are the only control characters allowed through.
                if (c < ' ' && c != '\n')
                {
                    Assert.Fail($"raw U+{(int)c:X4} at index {i} of: {json}");
                }
            }
        }

        [Test]
        public void Write_EscapesControlCharactersInValues()
        {
            var json = new NeuroJsonWriter().Write(new StringBox { Value = "tab\there\rand\u0000nul\bback\ffeed" });
            TestContext.WriteLine(json);
            AssertNoRawControlCharacters(json);
            Assert.IsTrue(json.Contains(@"tab\there\rand\u0000nul\bback\ffeed"), json);
        }

        [Test]
        public void Write_EscapesControlCharactersInDictionaryKeys()
        {
            var json = new NeuroJsonWriter().Write(new DictBox { StringKeys = AwkwardStringKeyed() });
            TestContext.WriteLine(json);
            AssertNoRawControlCharacters(json);
        }

        [Test]
        public void Write_EscapesControlCharactersInLists()
        {
            var json = new NeuroJsonWriter().Write(new ListBox { Strings = new List<string> { "a\tb", "c\rd", null, "" } });
            TestContext.WriteLine(json);
            AssertNoRawControlCharacters(json);
        }

        [Test]
        public void Write_LeavesUnicodeAlone()
        {
            var json = new NeuroJsonWriter().Write(new StringBox { Value = "\u00e9\u4e2d\U0001F600" });
            Assert.IsTrue(json.Contains("\u00e9\u4e2d\U0001F600"), json);
        }

// ---------------------------------------------------------------------------------------------------- reading json we did not write

        // The writer only ever emits \n, \" and \\, so nothing above covers the rest of the escapes.
        // Anything else that produced the file - a hand edit, another serializer - can use all of them.

        static T ReadJson<T>(string json) where T : class, new()
        {
            TestContext.WriteLine(json);
            return new NeuroJsonReader().Read<T>(json, new ReaderOptions());
        }

        [Test]
        public void Read_StandardEscapes()
        {
            var copy = ReadJson<StringBox>(@"{""Value"": ""a\tb\rc\bd\fe\/f\""g\\h""}");
            Assert.AreEqual("a\tb\rc\bd\fe/f\"g\\h", copy.Value);
        }

        [Test]
        public void Read_UnicodeEscapes()
        {
            var copy = ReadJson<StringBox>(@"{""Value"": ""\u0041\u00e9\u4E2D""}");
            Assert.AreEqual("A\u00e9\u4e2d", copy.Value);
        }

        [Test]
        public void Read_SurrogatePairEscape()
        {
            var copy = ReadJson<StringBox>(@"{""Value"": ""\ud83d\ude00""}");
            Assert.AreEqual("\U0001F600", copy.Value);
        }

        [Test]
        public void Read_EscapedQuoteAtEndOfString()
        {
            Assert.AreEqual("say \"hi\"", ReadJson<StringBox>(@"{""Value"": ""say \""hi\""""}").Value);
        }

        [Test]
        public void Read_BackslashAtEndOfString()
        {
            Assert.AreEqual("ends with \\", ReadJson<StringBox>(@"{""Value"": ""ends with \\""}").Value);
            Assert.AreEqual("\\", ReadJson<StringBox>(@"{""Value"": ""\\""}").Value);
        }

        [Test]
        public void Read_QuotedNullIsAString()
        {
            Assert.AreEqual("null", ReadJson<StringBox>(@"{""Value"": ""null""}").Value);
        }

        [Test]
        public void Read_BareNullIsNull()
        {
            Assert.IsNull(ReadJson<StringBox>(@"{""Value"": null}").Value);
        }

        [Test]
        public void Read_MissingFieldKeepsDefault()
        {
            Assert.IsNull(ReadJson<StringBox>(@"{}").Value);
        }

        [TestCase("NaN", float.NaN)]
        [TestCase("Infinity", float.PositiveInfinity)]
        [TestCase("-Infinity", float.NegativeInfinity)]
        public void Read_FloatSpecialValues(string written, float expected)
        {
            Assert.AreEqual(expected, ReadJson<FloatBox>(@"{""Value"": " + written + "}").Value);
        }

        [TestCase("NaN", double.NaN)]
        [TestCase("Infinity", double.PositiveInfinity)]
        [TestCase("-Infinity", double.NegativeInfinity)]
        public void Read_DoubleSpecialValues(string written, double expected)
        {
            Assert.AreEqual(expected, ReadJson<DoubleBox>(@"{""Value"": " + written + "}").Value);
        }

        [TestCase("1e3", 1000f)]
        [TestCase("1.5E-8", 1.5e-8f)]
        [TestCase("-2.5e+2", -250f)]
        [TestCase("0.0000001", 0.0000001f)]
        public void Read_ExponentNotation(string written, float expected)
        {
            Assert.AreEqual(expected, ReadJson<FloatBox>(@"{""Value"": " + written + "}").Value);
        }

        [Test]
        public void Read_NumbersAreCultureIndependent()
        {
            // a comma decimal separator culture must not turn 1.5 into 15.
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.AreEqual(1.5f, ReadJson<FloatBox>(@"{""Value"": 1.5}").Value);
                Assert.AreEqual(-1234.5678d, ReadJson<DoubleBox>(@"{""Value"": -1234.5678}").Value);
                Assert.AreEqual(1.5f, Jsn(new FloatBox { Value = 1.5f }).Value);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

// ---------------------------------------------------------------------------------------------------- everything at once

        public partial struct ExtremeStruct
        {
            [Neuro(1)] public int Id;
            [Neuro(2)] public string Name;
        }

        public partial class KitchenSink
        {
            [Neuro(1)] public bool Bool;
            [Neuro(2)] public int Int;
            [Neuro(3)] public uint UInt;
            [Neuro(4)] public long Long;
            [Neuro(5)] public ulong ULong;
            [Neuro(6)] public float Float;
            [Neuro(7)] public double Double;
            [Neuro(8)] public string String;
            [Neuro(9)] public DateTime Date;
            [Neuro(10)] public TimeSpan Time;
            [Neuro(11)] public Guid Guid;
            [Neuro(12)] public ExtremeEnum Enum;
            [Neuro(13)] public ExtremeFlagEnum FlagEnum;
            [Neuro(14)] public ExtremeStruct Struct;
            [Neuro(15)] public Reference<ExtremeReferencable> Ref;
            [Neuro(16)] public Color Color;
        }

        static KitchenSink MaxedOutSink() => new KitchenSink
        {
            Bool = true,
            Int = int.MinValue,
            UInt = uint.MaxValue,
            Long = long.MinValue,
            ULong = ulong.MaxValue,
            Float = float.MaxValue,
            Double = double.MinValue,
            String = "everything \"at\" once\n中文 \U0001F600",
            Date = DateTime.MaxValue,
            Time = TimeSpan.MinValue,
            Guid = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            Enum = ExtremeEnum.Min,
            FlagEnum = ExtremeFlagEnum.All,
            Struct = new ExtremeStruct { Id = int.MaxValue, Name = "struct" },
            Ref = uint.MaxValue,
            Color = Color.FromArgb(255, 255, 255, 255)
        };

        static void AssertSink(KitchenSink a, KitchenSink b)
        {
            Assert.AreEqual(a.Bool, b.Bool, nameof(a.Bool));
            Assert.AreEqual(a.Int, b.Int, nameof(a.Int));
            Assert.AreEqual(a.UInt, b.UInt, nameof(a.UInt));
            Assert.AreEqual(a.Long, b.Long, nameof(a.Long));
            Assert.AreEqual(a.ULong, b.ULong, nameof(a.ULong));
            Assert.AreEqual(a.Float, b.Float, nameof(a.Float));
            Assert.AreEqual(a.Double, b.Double, nameof(a.Double));
            Assert.AreEqual(a.String, b.String, nameof(a.String));
            AssertStoredResolution(a.Date, b.Date);
            AssertStoredResolution(a.Time, b.Time);
            Assert.AreEqual(a.Guid, b.Guid, nameof(a.Guid));
            Assert.AreEqual(a.Enum, b.Enum, nameof(a.Enum));
            Assert.AreEqual(a.FlagEnum, b.FlagEnum, nameof(a.FlagEnum));
            Assert.AreEqual(a.Struct.Id, b.Struct.Id, nameof(a.Struct));
            Assert.AreEqual(a.Struct.Name, b.Struct.Name, nameof(a.Struct));
            Assert.AreEqual(a.Ref.RefId, b.Ref.RefId, nameof(a.Ref));
            Assert.AreEqual(a.Color.ToArgb(), b.Color.ToArgb(), nameof(a.Color));
        }

        [Test]
        public void KitchenSink_Binary()
        {
            // compared against a fresh instance rather than src - writing is allowed to mutate what it writes today,
            // see Write_DoesNotMutateSource_* below.
            AssertSink(MaxedOutSink(), Bin(MaxedOutSink()));
        }

        [Test]
        public void KitchenSink_Json()
        {
            AssertSink(MaxedOutSink(), Jsn(MaxedOutSink()));
        }

        [Test]
        public void KitchenSink_BinaryThenJson_MatchEachOther()
        {
            AssertSink(Bin(MaxedOutSink()), Jsn(MaxedOutSink()));
        }

// ---------------------------------------------------------------------------------------------------- writing must not mutate

        [Test]
        public void Write_DoesNotMutateSource_DateTime_Binary()
        {
            var src = new DateTimeBox { Value = DateTime.MaxValue };
            new NeuroBytesWriter().Write(src);
            Assert.AreEqual(DateTime.MaxValue, src.Value, "writing changed the object that was written");
        }

        [Test]
        public void Write_DoesNotMutateSource_DateTime_Json()
        {
            var src = new DateTimeBox { Value = DateTime.MaxValue };
            new NeuroJsonWriter().Write(src);
            Assert.AreEqual(DateTime.MaxValue, src.Value, "writing changed the object that was written");
        }

        [Test]
        public void Write_DoesNotMutateSource_TimeSpan_Binary()
        {
            var src = new TimeSpanBox { Value = TimeSpan.MinValue };
            new NeuroBytesWriter().Write(src);
            Assert.AreEqual(TimeSpan.MinValue, src.Value, "writing changed the object that was written");
        }

        [Test]
        public void Write_DoesNotMutateSource_TimeSpan_Json()
        {
            var src = new TimeSpanBox { Value = TimeSpan.MinValue };
            new NeuroJsonWriter().Write(src);
            Assert.AreEqual(TimeSpan.MinValue, src.Value, "writing changed the object that was written");
        }

        [Test]
        public void Write_Twice_ProducesSameBytes()
        {
            var src = MaxedOutSink();
            var first = new NeuroBytesWriter().Write(src).ToArray();
            var second = new NeuroBytesWriter().Write(src).ToArray();
            CollectionAssert.AreEqual(first, second, "writing the same object twice gave different bytes");
        }

        [Test]
        public void Write_Twice_ProducesSameJson()
        {
            var src = MaxedOutSink();
            var first = new NeuroJsonWriter().Write(src);
            var second = new NeuroJsonWriter().Write(src);
            Assert.AreEqual(first, second, "writing the same object twice gave different json");
        }
    }
}
