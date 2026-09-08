using System;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    /// A field initialiser is the serialization default: the writers skip a field still holding it, so the
    /// readers have to put it back when the data does not carry it. Codegen rebuilds the initialiser into
    /// the generated Sync call, and these cover the shapes it has to rebuild - the ones it dropped used to
    /// read back as a zero without saying anything.
    public class DefaultValueTests
    {
        public struct Coord : IEquatable<Coord>
        {
            [Neuro(1)] public int X;
            [Neuro(2)] public int Y;

            public Coord(int x, int y)
            {
                X = x;
                Y = y;
            }

            /// The Vector2Int.one shape - a static property rather than a field.
            public static Coord One => new Coord(1, 1);

            public static readonly Coord Two = new Coord(2, 2);

            public bool Equals(Coord other) => X == other.X && Y == other.Y;

            public override string ToString() => $"({X}, {Y})";
        }

        public partial class Defaults
        {
            [Neuro(1)] public int Positive = 5;
            [Neuro(2)] public int Negative = -5;
            [Neuro(3)] public float Half = 0.5f;
            [Neuro(4)] public Coord FromProperty = Coord.One;
            [Neuro(5)] public Coord FromField = Coord.Two;
            [Neuro(6)] public Coord FromConstructor = new Coord(3, -4);
        }

        [Test]
        public void Json_MissingFields_ReadBackAsTheirInitialisers()
        {
            AssertDefaults(NeuroJsonReader.Shared.Read<Defaults>("{}"));
        }

        [Test]
        public void Json_RoundTripOfAnUntouchedObject_KeepsTheInitialisers()
        {
            // Nothing was changed from the initialisers, so the writer has nothing to write.
            var json = NeuroJsonWriter.Shared.Write(new Defaults());
            AssertDefaults(NeuroJsonReader.Shared.Read<Defaults>(json));
        }

        [Test]
        public void Binary_RoundTripOfAnUntouchedObject_KeepsTheInitialisers()
        {
            var bytes = NeuroBytesWriter.Shared.Write(new Defaults()).ToArray();
            AssertDefaults(NeuroBytesReader.Shared.Read<Defaults>(bytes));
        }

        [Test]
        public void ChangedValues_StillWinOverTheInitialisers()
        {
            var json = NeuroJsonWriter.Shared.Write(new Defaults()
            {
                Positive = 0,
                FromProperty = new Coord(9, 9)
            });
            var copy = NeuroJsonReader.Shared.Read<Defaults>(json);

            Assert.That(copy.Positive, Is.EqualTo(0));
            Assert.That(copy.FromProperty, Is.EqualTo(new Coord(9, 9)));
            Assert.That(copy.Negative, Is.EqualTo(-5));
        }

        static void AssertDefaults(Defaults value)
        {
            Assert.That(value.Positive, Is.EqualTo(5));
            Assert.That(value.Negative, Is.EqualTo(-5));
            Assert.That(value.Half, Is.EqualTo(0.5f));
            Assert.That(value.FromProperty, Is.EqualTo(new Coord(1, 1)));
            Assert.That(value.FromField, Is.EqualTo(new Coord(2, 2)));
            Assert.That(value.FromConstructor, Is.EqualTo(new Coord(3, -4)));
        }
    }
}
