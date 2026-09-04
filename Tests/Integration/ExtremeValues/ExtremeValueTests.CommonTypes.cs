using System;
using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
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
        public void Guid_Binary(Guid v) => Assert.That(Bin(new GuidBox { Value = v }).Value, Is.EqualTo(v));

        [TestCaseSource(nameof(GuidValues))]
        public void Guid_Json(Guid v) => Assert.That(Jsn(new GuidBox { Value = v }).Value, Is.EqualTo(v));

// ---------------------------------------------------------------------------------------------------- Uri / Version

        public partial class UriBox
        {
            [Neuro(1)] public Uri Value;
        }

        public partial class VersionBox
        {
            [Neuro(1)] public Version Value;
        }

        static IEnumerable<TestCaseData> UriValues()
        {
            yield return new TestCaseData(new Uri("https://example.com/a/b?q=1&r=2#frag")).SetName("{m}_full");
            yield return new TestCaseData(new Uri("https://example.com")).SetName("{m}_bare");
            yield return new TestCaseData(new Uri("file:///Users/x/y.json")).SetName("{m}_file");
            yield return new TestCaseData(new Uri("relative/path.json", UriKind.Relative)).SetName("{m}_relative");
            yield return new TestCaseData(new Uri("https://example.com/a%20b")).SetName("{m}_percentEncoded");
            yield return new TestCaseData(new Uri("https://example.com/\u4e2d\u6587")).SetName("{m}_unicodePath");
        }

        [TestCaseSource(nameof(UriValues))]
        public void Uri_Binary(Uri v) => Assert.That(Bin(new UriBox { Value = v }).Value, Is.EqualTo(v));

        [TestCaseSource(nameof(UriValues))]
        public void Uri_Json(Uri v) => Assert.That(Jsn(new UriBox { Value = v }).Value, Is.EqualTo(v));

        [Test]
        public void Uri_Null_RoundTrips()
        {
            Assert.IsNull(Bin(new UriBox()).Value);
            Assert.IsNull(Jsn(new UriBox()).Value);
        }

        [Test]
        public void Uri_KeepsTheOriginalSpelling()
        {
            // ToString() would normalise the escaping, OriginalString does not.
            var src = new Uri("https://example.com/a%20b%2Fc");
            Assert.That(Bin(new UriBox { Value = src }).Value.OriginalString, Is.EqualTo(src.OriginalString));
            Assert.That(Jsn(new UriBox { Value = src }).Value.OriginalString, Is.EqualTo(src.OriginalString));
        }

        static IEnumerable<TestCaseData> VersionValues()
        {
            yield return new TestCaseData(new Version(1, 2)).SetName("{m}_majorMinor");
            yield return new TestCaseData(new Version(1, 2, 3)).SetName("{m}_withBuild");
            yield return new TestCaseData(new Version(1, 2, 3, 4)).SetName("{m}_full");
            yield return new TestCaseData(new Version(0, 0)).SetName("{m}_zero");
            yield return new TestCaseData(new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)).SetName("{m}_max");
        }

        [TestCaseSource(nameof(VersionValues))]
        public void Version_Binary(Version v) => Assert.That(Bin(new VersionBox { Value = v }).Value, Is.EqualTo(v));

        [TestCaseSource(nameof(VersionValues))]
        public void Version_Json(Version v) => Assert.That(Jsn(new VersionBox { Value = v }).Value, Is.EqualTo(v));

        [Test]
        public void Version_KeepsComponentCount()
        {
            // "1.2" must not come back as "1.2.0.0" - Build and Revision stay unset at -1.
            var copy = Bin(new VersionBox { Value = new Version(1, 2) }).Value;
            Assert.That(copy.Build, Is.EqualTo(-1));
            Assert.That(copy.Revision, Is.EqualTo(-1));
            Assert.That(copy.ToString(), Is.EqualTo("1.2"));
        }

        [Test]
        public void Version_ExpandsToComponents()
        {
            var json = new NeuroJsonWriter().Write(new VersionBox { Value = new Version(1, 2, 3, 4) });
            TestContext.WriteLine(json);
            foreach (var part in new[] { "Major", "Minor", "Build", "Revision" })
            {
                Assert.IsTrue(json.Contains(part), $"expected {part} in the json: {json}");
            }

            // an unset Build/Revision is -1, which is the registered default, so it costs nothing at all.
            var twoParts = new NeuroBytesWriter().Write(new VersionBox { Value = new Version(1, 2) }).ToArray().Length;
            var fourParts = new NeuroBytesWriter().Write(new VersionBox { Value = new Version(1, 2, 3, 4) }).ToArray().Length;
            Assert.Less(twoParts, fourParts, "an unset Build and Revision should be skipped entirely");

            // and big components stay varints rather than becoming a long run of digits.
            var big = new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
            var bigBytes = new NeuroBytesWriter().Write(new VersionBox { Value = big }).ToArray().Length;
            Assert.Less(bigBytes, big.ToString().Length, "component form should beat the string form here");
        }

        [Test]
        public void Version_UnsetComponentsAreOmittedFromJson()
        {
            var json = new NeuroJsonWriter().Write(new VersionBox { Value = new Version(1, 2) });
            Assert.IsFalse(json.Contains("Build"), "an unset Build should not be written: " + json);
            Assert.IsFalse(json.Contains("Revision"), "an unset Revision should not be written: " + json);
        }

        [Test]
        public void Version_Null_RoundTrips()
        {
            Assert.IsNull(Bin(new VersionBox()).Value);
            Assert.IsNull(Jsn(new VersionBox()).Value);
        }


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
        public void Color_Binary(Color v) => Assert.That(Bin(new ColorBox { Value = v }).Value.ToArgb(), Is.EqualTo(v.ToArgb()));

        [TestCaseSource(nameof(ColorValues))]
        public void Color_Json(Color v) => Assert.That(Jsn(new ColorBox { Value = v }).Value.ToArgb(), Is.EqualTo(v.ToArgb()));
    }
}
