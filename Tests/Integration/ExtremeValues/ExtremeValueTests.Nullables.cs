using System;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
// ---------------------------------------------------------------------------------------------------- nullables

        public partial class NullableBox
        {
            [Neuro(1)] public int? Int;
            [Neuro(2)] public float? Float;
            [Neuro(3)] public DateTime? Date;
            [Neuro(4)] public ExtremeEnum? Enum;
            [Neuro(5)] public bool? Bool;
            [Neuro(6)] public DateTimeOffset? DateOffset;
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
            Assert.IsNull(copy.DateOffset);
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
            Assert.That(copy.Int, Is.EqualTo(0));
            Assert.That(copy.Float, Is.EqualTo(0f));
            Assert.That(copy.Date, Is.EqualTo(default(DateTime)));
            Assert.That(copy.Enum, Is.EqualTo(ExtremeEnum.Zero));
            Assert.That(copy.Bool, Is.EqualTo(false));
        }

        [Test]
        public void Nullable_DefaultValuesAreNotLostAsNull_Json()
        {
            var src = new NullableBox { Int = 0, Float = 0f, Date = default(DateTime), Enum = ExtremeEnum.Zero, Bool = false };
            var copy = Jsn(src);
            Assert.That(copy.Int, Is.EqualTo(0));
            Assert.That(copy.Float, Is.EqualTo(0f));
            Assert.That(copy.Date, Is.EqualTo(default(DateTime)));
            Assert.That(copy.Enum, Is.EqualTo(ExtremeEnum.Zero));
            Assert.That(copy.Bool, Is.EqualTo(false));
        }

        [Test]
        public void Nullable_ExtremeValues_Binary()
        {
            var src = new NullableBox { Int = int.MinValue, Float = float.NaN, Date = DateTime.MaxValue, Enum = ExtremeEnum.Max, Bool = true };
            var copy = Bin(src);
            Assert.That(copy.Int, Is.EqualTo(int.MinValue));
            Assert.That(copy.Float, Is.EqualTo(float.NaN));
            AssertStoredResolution(DateTime.MaxValue, copy.Date.Value);
            Assert.That(copy.Enum, Is.EqualTo(ExtremeEnum.Max));
            Assert.That(copy.Bool, Is.EqualTo(true));
        }

        [Test]
        public void Nullable_ExtremeValues_Json()
        {
            var src = new NullableBox { Int = int.MinValue, Float = float.NaN, Date = DateTime.MaxValue, Enum = ExtremeEnum.Max, Bool = true };
            var copy = Jsn(src);
            Assert.That(copy.Int, Is.EqualTo(int.MinValue));
            Assert.That(copy.Float, Is.EqualTo(float.NaN));
            AssertStoredResolution(DateTime.MaxValue, copy.Date.Value);
            Assert.That(copy.Enum, Is.EqualTo(ExtremeEnum.Max));
            Assert.That(copy.Bool, Is.EqualTo(true));
        }


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
            Assert.That(copy.Value.Value.Id, Is.EqualTo(int.MinValue));
            Assert.That(copy.Value.Value.Name, Is.EqualTo("x"));
        }

        [Test]
        public void NullableStruct_Extremes_Json()
        {
            var copy = Jsn(new NullableStructBox { Value = new ExtremeStruct { Id = int.MinValue, Name = "x" } });
            Assert.That(copy.Value.Value.Id, Is.EqualTo(int.MinValue));
            Assert.That(copy.Value.Value.Name, Is.EqualTo("x"));
        }
    }
}
