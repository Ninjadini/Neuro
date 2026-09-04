using System;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
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
        public void Enum_Binary(ExtremeEnum v) => Assert.That(Bin(new EnumBox { Value = v }).Value, Is.EqualTo(v));

        [TestCaseSource(nameof(EnumValues))]
        public void Enum_Json(ExtremeEnum v) => Assert.That(Jsn(new EnumBox { Value = v }).Value, Is.EqualTo(v));

        static readonly ExtremeFlagEnum[] FlagEnumValues =
        {
            ExtremeFlagEnum.None, ExtremeFlagEnum.A, ExtremeFlagEnum.A | ExtremeFlagEnum.B,
            ExtremeFlagEnum.Top, ExtremeFlagEnum.All, (ExtremeFlagEnum)(1 << 30)
        };

        [TestCaseSource(nameof(FlagEnumValues))]
        public void FlagEnum_Binary(ExtremeFlagEnum v) => Assert.That(Bin(new FlagEnumBox { Value = v }).Value, Is.EqualTo(v));

        [TestCaseSource(nameof(FlagEnumValues))]
        public void FlagEnum_Json(ExtremeFlagEnum v) => Assert.That(Jsn(new FlagEnumBox { Value = v }).Value, Is.EqualTo(v));
    }
}
