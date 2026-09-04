using System;
using System.Drawing;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
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
            [Neuro(17)] public DateTimeOffset DateOffset;
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
            Color = Color.FromArgb(255, 255, 255, 255),
            DateOffset = new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, new TimeSpan(5, 45, 0))
        };

        static void AssertSink(KitchenSink a, KitchenSink b)
        {
            Assert.That(b.Bool, Is.EqualTo(a.Bool), nameof(a.Bool));
            Assert.That(b.Int, Is.EqualTo(a.Int), nameof(a.Int));
            Assert.That(b.UInt, Is.EqualTo(a.UInt), nameof(a.UInt));
            Assert.That(b.Long, Is.EqualTo(a.Long), nameof(a.Long));
            Assert.That(b.ULong, Is.EqualTo(a.ULong), nameof(a.ULong));
            Assert.That(b.Float, Is.EqualTo(a.Float), nameof(a.Float));
            Assert.That(b.Double, Is.EqualTo(a.Double), nameof(a.Double));
            Assert.That(b.String, Is.EqualTo(a.String), nameof(a.String));
            AssertStoredResolution(a.Date, b.Date);
            AssertStoredResolution(a.Time, b.Time);
            Assert.That(b.Guid, Is.EqualTo(a.Guid), nameof(a.Guid));
            Assert.That(b.Enum, Is.EqualTo(a.Enum), nameof(a.Enum));
            Assert.That(b.FlagEnum, Is.EqualTo(a.FlagEnum), nameof(a.FlagEnum));
            Assert.That(b.Struct.Id, Is.EqualTo(a.Struct.Id), nameof(a.Struct));
            Assert.That(b.Struct.Name, Is.EqualTo(a.Struct.Name), nameof(a.Struct));
            Assert.That(b.Ref.RefId, Is.EqualTo(a.Ref.RefId), nameof(a.Ref));
            Assert.That(b.Color.ToArgb(), Is.EqualTo(a.Color.ToArgb()), nameof(a.Color));
            AssertStoredResolution(a.DateOffset, b.DateOffset);
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
            Assert.That(src.Value, Is.EqualTo(DateTime.MaxValue), "writing changed the object that was written");
        }

        [Test]
        public void Write_DoesNotMutateSource_DateTime_Json()
        {
            var src = new DateTimeBox { Value = DateTime.MaxValue };
            new NeuroJsonWriter().Write(src);
            Assert.That(src.Value, Is.EqualTo(DateTime.MaxValue), "writing changed the object that was written");
        }

        [Test]
        public void Write_DoesNotMutateSource_TimeSpan_Binary()
        {
            var src = new TimeSpanBox { Value = TimeSpan.MinValue };
            new NeuroBytesWriter().Write(src);
            Assert.That(src.Value, Is.EqualTo(TimeSpan.MinValue), "writing changed the object that was written");
        }

        [Test]
        public void Write_DoesNotMutateSource_TimeSpan_Json()
        {
            var src = new TimeSpanBox { Value = TimeSpan.MinValue };
            new NeuroJsonWriter().Write(src);
            Assert.That(src.Value, Is.EqualTo(TimeSpan.MinValue), "writing changed the object that was written");
        }

        [Test]
        public void Write_Twice_ProducesSameBytes()
        {
            var src = MaxedOutSink();
            var first = new NeuroBytesWriter().Write(src).ToArray();
            var second = new NeuroBytesWriter().Write(src).ToArray();
            Assert.That(second, Is.EqualTo(first).AsCollection, "writing the same object twice gave different bytes");
        }

        [Test]
        public void Write_Twice_ProducesSameJson()
        {
            var src = MaxedOutSink();
            var first = new NeuroJsonWriter().Write(src);
            var second = new NeuroJsonWriter().Write(src);
            Assert.That(second, Is.EqualTo(first), "writing the same object twice gave different json");
        }
    }
}
