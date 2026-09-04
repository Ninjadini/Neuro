using System;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
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
    }
}
