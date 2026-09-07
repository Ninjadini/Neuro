using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;
using UnityEngine;

namespace Ninjadini.Neuro.UnityTests
{
    /// <summary>
    /// Round trips UnityEngine.Color and Color32 through binary and json.
    ///
    /// Both pack their channels into a single integer rather than writing an object, so a mistake in
    /// the packing does not fail loudly - it quietly returns a different colour, or bleeds one channel
    /// into the next. Color did exactly that until 0.2.1: the decoder did not invert the encoder, so
    /// every colour read back wrong. ChannelsAreIndependent and the exact-value cases are the guards
    /// against that coming back.
    /// </summary>
    public class UnityColorTests
    {
        [OneTimeSetUp]
        public void RegisterTypes()
        {
            NeuroSyncTypes.TryRegisterAssemblyOf<ColorBox>();   // this test assembly
            NeuroSyncTypes.TryRegisterAssemblyOf<AssetAddress>(); // Ninjadini.Neuro.Unity - the Color hooks
        }

        public partial class ColorBox
        {
            [Neuro(1)] public Color Value;
        }

        public partial class Color32Box
        {
            [Neuro(1)] public Color32 Value;
        }

        static T Bin<T>(T src) where T : class, new()
            => new NeuroBytesReader().Read<T>(new NeuroBytesWriter().Write(src).ToArray(), new ReaderOptions());

        static T Jsn<T>(T src) where T : class, new()
            => new NeuroJsonReader().Read<T>(new NeuroJsonWriter().Write(src), new ReaderOptions());

        // Binary keeps 12 bits per channel, json writes 8 bit hex - so they quantise differently.
        // One quantisation step each: half a step is the true worst case, but a value landing exactly
        // on a tie (0.5 * 4095 = 2047.5, which rounds up) sits exactly on that bound and float
        // comparison tips it over. Still tight - a real packing fault is off by 0.1, not 0.0002.
        const float BinTolerance = 1f / 4095f;
        const float JsonTolerance = 1f / 255f;

        static void AssertClose(Color actual, Color expected, float tolerance, string what)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), what + " .r");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), what + " .g");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), what + " .b");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance), what + " .a");
        }

// ---------------------------------------------------------------------------------------------------- Color

        static IEnumerable<TestCaseData> ColorValues()
        {
            yield return new TestCaseData(new Color(0f, 0f, 0f, 0f)).SetName("Color_allZero");
            yield return new TestCaseData(new Color(1f, 1f, 1f, 1f)).SetName("Color_allOne");
            yield return new TestCaseData(new Color(1f, 0f, 0f, 0f)).SetName("Color_onlyRed");
            yield return new TestCaseData(new Color(0f, 1f, 0f, 0f)).SetName("Color_onlyGreen");
            yield return new TestCaseData(new Color(0f, 0f, 1f, 0f)).SetName("Color_onlyBlue");
            yield return new TestCaseData(new Color(0f, 0f, 0f, 1f)).SetName("Color_onlyAlpha");
            yield return new TestCaseData(new Color(1f, 0.8f, 0.2f, 1f)).SetName("Color_ffcc33");
            yield return new TestCaseData(new Color(0.5f, 0.5f, 0.5f, 0.5f)).SetName("Color_mid");
            yield return new TestCaseData(new Color(1f / 4095f, 2f / 4095f, 3f / 4095f, 4f / 4095f)).SetName("Color_smallestSteps");
            yield return new TestCaseData(new Color(0.03529412f, 0.8666667f, 0.21176471f, 1f)).SetName("Color_arbitrary");
        }

        [TestCaseSource(nameof(ColorValues))]
        public void Color_Binary(Color v) => AssertClose(Bin(new ColorBox { Value = v }).Value, v, BinTolerance, "binary");

        [TestCaseSource(nameof(ColorValues))]
        public void Color_Json(Color v) => AssertClose(Jsn(new ColorBox { Value = v }).Value, v, JsonTolerance, "json");

        /// The original bug: r = 1.0 overflowed its 12 bit slot and landed in green, and the decode
        /// mask was a single bit, so channels bled into each other. Each channel is swept on its own
        /// with the others held at zero - anything leaking shows up immediately.
        [Test]
        public void Color_ChannelsAreIndependent()
        {
            for (var step = 0; step <= 16; step++)
            {
                var v = step / 16f;
                AssertClose(Bin(new ColorBox { Value = new Color(v, 0, 0, 0) }).Value, new Color(v, 0, 0, 0), BinTolerance, $"r={v}");
                AssertClose(Bin(new ColorBox { Value = new Color(0, v, 0, 0) }).Value, new Color(0, v, 0, 0), BinTolerance, $"g={v}");
                AssertClose(Bin(new ColorBox { Value = new Color(0, 0, v, 0) }).Value, new Color(0, 0, v, 0), BinTolerance, $"b={v}");
                AssertClose(Bin(new ColorBox { Value = new Color(0, 0, 0, v) }).Value, new Color(0, 0, 0, v), BinTolerance, $"a={v}");
                AssertClose(Jsn(new ColorBox { Value = new Color(v, 0, 0, 0) }).Value, new Color(v, 0, 0, 0), JsonTolerance, $"json r={v}");
                AssertClose(Jsn(new ColorBox { Value = new Color(0, 0, 0, v) }).Value, new Color(0, 0, 0, v), JsonTolerance, $"json a={v}");
            }
        }

        [Test]
        public void Color_RandomValuesSurviveBothFormats()
        {
            var rnd = new System.Random(12345);
            for (var i = 0; i < 500; i++)
            {
                var v = new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(),
                                  (float)rnd.NextDouble(), (float)rnd.NextDouble());
                AssertClose(Bin(new ColorBox { Value = v }).Value, v, BinTolerance, "binary #" + i);
                AssertClose(Jsn(new ColorBox { Value = v }).Value, v, JsonTolerance, "json #" + i);
            }
        }

        /// Color is LDR by design - out of range channels clamp rather than corrupting their neighbours.
        [Test]
        public void Color_OutOfRangeChannelsClamp()
        {
            AssertClose(Bin(new ColorBox { Value = new Color(2.5f, -0.3f, 0.5f, 1f) }).Value,
                new Color(1f, 0f, 0.5f, 1f), BinTolerance, "clamped binary");
            AssertClose(Jsn(new ColorBox { Value = new Color(2.5f, -0.3f, 0.5f, 1f) }).Value,
                new Color(1f, 0f, 0.5f, 1f), JsonTolerance, "clamped json");
        }

        /// Locks the binary packing. If this changes, data already on disk silently means something
        /// else, so it needs a changelog entry and a migration note, not just a green test.
        /// Read through the json number fallback, which uses the same layout.
        [Test]
        public void Color_BinaryPackingIsStable()
        {
            var result = new NeuroJsonReader().Read<ColorBox>("{\"Value\": 281420011196415}");
            AssertClose(result.Value, new Color(1f, 0.8f, 0.2f, 1f), BinTolerance,
                "packing changed - r | g<<12 | b<<24 | a<<36, each channel scaled by 4095");
        }

// ---------------------------------------------------------------------------------------------------- Color32

        static IEnumerable<TestCaseData> Color32Values()
        {
            yield return new TestCaseData(new Color32(0, 0, 0, 0)).SetName("Color32_allZero");
            yield return new TestCaseData(new Color32(255, 255, 255, 255)).SetName("Color32_allMax");
            yield return new TestCaseData(new Color32(255, 0, 0, 0)).SetName("Color32_onlyRed");
            yield return new TestCaseData(new Color32(0, 255, 0, 0)).SetName("Color32_onlyGreen");
            yield return new TestCaseData(new Color32(0, 0, 255, 0)).SetName("Color32_onlyBlue");
            // a << 24 with a = 255 overflows a signed int before the cast to uint - the case most
            // likely to have been wrong.
            yield return new TestCaseData(new Color32(0, 0, 0, 255)).SetName("Color32_onlyAlpha_highBit");
            yield return new TestCaseData(new Color32(255, 204, 51, 255)).SetName("Color32_ffcc33");
            yield return new TestCaseData(new Color32(1, 2, 3, 4)).SetName("Color32_lowValues");
            yield return new TestCaseData(new Color32(128, 128, 128, 128)).SetName("Color32_mid");
        }

        static void AssertExact(Color32 actual, Color32 expected, string what)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r), what + " .r");
            Assert.That(actual.g, Is.EqualTo(expected.g), what + " .g");
            Assert.That(actual.b, Is.EqualTo(expected.b), what + " .b");
            Assert.That(actual.a, Is.EqualTo(expected.a), what + " .a");
        }

        [TestCaseSource(nameof(Color32Values))]
        public void Color32_Binary(Color32 v) => AssertExact(Bin(new Color32Box { Value = v }).Value, v, "binary");

        [TestCaseSource(nameof(Color32Values))]
        public void Color32_Json(Color32 v) => AssertExact(Jsn(new Color32Box { Value = v }).Value, v, "json");

        /// Color32 is 8 bits per channel with no scaling, so unlike Color it must be exactly lossless.
        [Test]
        public void Color32_IsLossless()
        {
            var rnd = new System.Random(999);
            for (var i = 0; i < 500; i++)
            {
                var v = new Color32((byte)rnd.Next(256), (byte)rnd.Next(256),
                                    (byte)rnd.Next(256), (byte)rnd.Next(256));
                AssertExact(Bin(new Color32Box { Value = v }).Value, v, "binary #" + i);
                AssertExact(Jsn(new Color32Box { Value = v }).Value, v, "json #" + i);
            }
        }

        [Test]
        public void Color32_ChannelsAreIndependent()
        {
            for (var i = 0; i < 256; i++)
            {
                var b = (byte)i;
                AssertExact(Bin(new Color32Box { Value = new Color32(b, 0, 0, 0) }).Value, new Color32(b, 0, 0, 0), $"r={b}");
                AssertExact(Bin(new Color32Box { Value = new Color32(0, b, 0, 0) }).Value, new Color32(0, b, 0, 0), $"g={b}");
                AssertExact(Bin(new Color32Box { Value = new Color32(0, 0, b, 0) }).Value, new Color32(0, 0, b, 0), $"b={b}");
                AssertExact(Bin(new Color32Box { Value = new Color32(0, 0, 0, b) }).Value, new Color32(0, 0, 0, b), $"a={b}");
            }
        }

// ---------------------------------------------------------------------------------------------------- hex json form

        static string JsonOf<T>(T box) where T : class => new NeuroJsonWriter().Write(box);

        [Test]
        public void Color_JsonWritesHex_RgbWhenOpaque()
        {
            StringAssert.Contains("\"FFCC00\"", JsonOf(new ColorBox { Value = new Color(1f, 0.8f, 0f, 1f) }));
        }

        [Test]
        public void Color_JsonWritesHex_RgbaWhenTranslucent()
        {
            StringAssert.Contains("\"FFCC0080\"", JsonOf(new ColorBox { Value = new Color(1f, 0.8f, 0f, 128f / 255f) }));
        }

        [Test]
        public void Color32_JsonWritesHex()
        {
            StringAssert.Contains("\"FFCC00\"", JsonOf(new Color32Box { Value = new Color32(255, 204, 0, 255) }));
            StringAssert.Contains("\"FFCC0080\"", JsonOf(new Color32Box { Value = new Color32(255, 204, 0, 128) }));
        }

        static IEnumerable<TestCaseData> HexForms()
        {
            yield return new TestCaseData("\"FFCC00\"", new Color32(255, 204, 0, 255)).SetName("hex_rgb");
            yield return new TestCaseData("\"#FFCC00\"", new Color32(255, 204, 0, 255)).SetName("hex_hashPrefix");
            yield return new TestCaseData("\"ffcc00\"", new Color32(255, 204, 0, 255)).SetName("hex_lowercase");
            yield return new TestCaseData("\"FFCC0080\"", new Color32(255, 204, 0, 128)).SetName("hex_rgba");
            yield return new TestCaseData("\"FC0\"", new Color32(255, 204, 0, 255)).SetName("hex_shorthand");
            yield return new TestCaseData("\"#FC08\"", new Color32(255, 204, 0, 136)).SetName("hex_shorthandAlpha");
            // The number form data was written with before hex, still has to load.
            yield return new TestCaseData("4278242559", new Color32(255, 204, 0, 255)).SetName("legacy_packedNumber");
        }

        [TestCaseSource(nameof(HexForms))]
        public void Color32_ReadsAllAcceptedForms(string jsonValue, Color32 expected)
        {
            var result = new NeuroJsonReader().Read<Color32Box>("{\"Value\": " + jsonValue + "}");
            AssertExact(result.Value, expected, jsonValue);
        }

        /// A quoted "281420" is hex, a bare 281420 is a legacy packed number. Same digits, different
        /// meaning - the reader has to go by the json token type, not the text.
        [Test]
        public void Color_QuotedAndBareDigitsMeanDifferentThings()
        {
            var asHex = new NeuroJsonReader().Read<ColorBox>("{\"Value\": \"281420\"}").Value;
            AssertClose(asHex, new Color(0x28 / 255f, 0x14 / 255f, 0x20 / 255f, 1f), JsonTolerance, "quoted -> hex");

            var asNumber = new NeuroJsonReader().Read<ColorBox>("{\"Value\": 281420}").Value;
            Assert.That(asNumber, Is.Not.EqualTo(asHex), "bare number should decode as the packed form");
        }

        /// A bad hand edit should cost you one colour, not the whole file.
        [TestCase("\"nothex\"")]
        [TestCase("\"\"")]
        [TestCase("\"FFCC0\"")]
        public void Color_MalformedHexDoesNotThrow(string jsonValue)
        {
            Assert.DoesNotThrow(() => new NeuroJsonReader().Read<ColorBox>("{\"Value\": " + jsonValue + "}"));
        }

        [Test]
        public void Color32_HexRoundTripsExactly()
        {
            var rnd = new System.Random(4242);
            for (var i = 0; i < 300; i++)
            {
                var v = new Color32((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));
                AssertExact(Jsn(new Color32Box { Value = v }).Value, v, "json #" + i);
            }
        }

// ---------------------------------------------------------------------------------------------------- Gradient

        public partial class GradientBox
        {
            [Neuro(1)] public Gradient Value;
        }

        /// Gradient colour stops go through the same Color codec, so it broke and was fixed with it.
        [Test]
        public void Gradient_ColorKeysSurvive()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f),
                    new GradientColorKey(new Color(0f, 0.5f, 1f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });

            var result = Bin(new GradientBox { Value = gradient }).Value;

            Assert.That(result.colorKeys.Length, Is.EqualTo(2));
            AssertClose(result.colorKeys[0].color, new Color(1f, 0.8f, 0.2f, 1f), BinTolerance, "key0");
            AssertClose(result.colorKeys[1].color, new Color(0f, 0.5f, 1f, 1f), BinTolerance, "key1");
        }
    }
}
