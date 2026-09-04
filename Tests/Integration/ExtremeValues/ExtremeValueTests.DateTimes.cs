using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
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
            Assert.That(Bin(new DateTimeBox { Value = withTicks }).Value, Is.EqualTo(expected));
            Assert.That(Jsn(new DateTimeBox { Value = withTicks }).Value, Is.EqualTo(expected));
        }

        [Test]
        public void TimeSpan_SubMillisecondIsIntentionallyDropped()
        {
            Assert.That(Bin(new TimeSpanBox { Value = new TimeSpan(9999) }).Value, Is.EqualTo(TimeSpan.Zero));
            Assert.That(Jsn(new TimeSpanBox { Value = new TimeSpan(9999) }).Value, Is.EqualTo(TimeSpan.Zero));
            Assert.That(Bin(new TimeSpanBox { Value = new TimeSpan(10001) }).Value, Is.EqualTo(TimeSpan.FromMilliseconds(1)));
        }

        [TestCaseSource(nameof(DateTimeValues))]
        public void DateTime_Kind_Binary(DateTime v) => Assert.That(Bin(new DateTimeBox { Value = v }).Value.Kind, Is.EqualTo(v.Kind));

        [TestCaseSource(nameof(DateTimeValues))]
        public void DateTime_Kind_Json(DateTime v) => Assert.That(Jsn(new DateTimeBox { Value = v }).Value.Kind, Is.EqualTo(v.Kind));

// ---------------------------------------------------------------------------------------------------- DateTimeOffset

        public partial class DateTimeOffsetBox
        {
            [Neuro(1)] public DateTimeOffset Value;
        }

        static IEnumerable<TestCaseData> DateTimeOffsetValues()
        {
            yield return new TestCaseData(default(DateTimeOffset)).SetName("{m}_default");
            yield return new TestCaseData(DateTimeOffset.MinValue).SetName("{m}_min");
            yield return new TestCaseData(DateTimeOffset.MaxValue).SetName("{m}_max");
            yield return new TestCaseData(new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, TimeSpan.Zero)).SetName("{m}_utc");
            yield return new TestCaseData(new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, TimeSpan.FromHours(14))).SetName("{m}_maxPositiveOffset");
            yield return new TestCaseData(new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, TimeSpan.FromHours(-14))).SetName("{m}_maxNegativeOffset");
            yield return new TestCaseData(new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, new TimeSpan(5, 30, 0))).SetName("{m}_indiaHalfHour");
            yield return new TestCaseData(new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, new TimeSpan(5, 45, 0))).SetName("{m}_nepalQuarterHour");
            yield return new TestCaseData(new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, new TimeSpan(0, -1, 0))).SetName("{m}_oneMinuteWest");
            yield return new TestCaseData(new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, new TimeSpan(0, 1, 0))).SetName("{m}_oneMinuteEast");
            yield return new TestCaseData(new DateTimeOffset(1969, 7, 20, 20, 17, 40, TimeSpan.FromHours(-5))).SetName("{m}_beforeEpochWithOffset");
            yield return new TestCaseData(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetName("{m}_neuroZeroPoint");
            yield return new TestCaseData(DateTimeOffset.MaxValue.AddHours(-14).ToOffset(TimeSpan.FromHours(14))).SetName("{m}_maxWithMaxOffset");
            yield return new TestCaseData(DateTimeOffset.MinValue.AddHours(14).ToOffset(TimeSpan.FromHours(-14))).SetName("{m}_minWithMinOffset");
        }

        static void AssertStoredResolution(DateTimeOffset expected, DateTimeOffset actual)
        {
            Assert.That(actual.Offset, Is.EqualTo(expected.Offset), $"offset of {expected:O} was not preserved (got {actual:O})");
            var lost = Math.Abs((expected - actual).Ticks);
            Assert.Less(lost, TimeSpan.TicksPerMillisecond,
                $"expected {expected:O} to survive to the millisecond, got {actual:O}");
        }

        [TestCaseSource(nameof(DateTimeOffsetValues))]
        public void DateTimeOffset_Binary(DateTimeOffset v) => AssertStoredResolution(v, Bin(new DateTimeOffsetBox { Value = v }).Value);

        [TestCaseSource(nameof(DateTimeOffsetValues))]
        public void DateTimeOffset_Json(DateTimeOffset v) => AssertStoredResolution(v, Jsn(new DateTimeOffsetBox { Value = v }).Value);

        [Test]
        public void DateTimeOffset_KeepsOffsetWhenInstantMatchesDefault()
        {
            // DateTimeOffset.Equals only compares the utc instant, so this one is "equal" to default and would
            // be skipped by the writer's default check if that were the only test.
            var sameInstantAsDefault = new DateTimeOffset(1, 1, 1, 5, 0, 0, TimeSpan.FromHours(5));
            Assert.IsTrue(sameInstantAsDefault.Equals(default(DateTimeOffset)), "premise of this test changed");
            Assert.That(Bin(new DateTimeOffsetBox { Value = sameInstantAsDefault }).Value.Offset, Is.EqualTo(TimeSpan.FromHours(5)));
            Assert.That(Jsn(new DateTimeOffsetBox { Value = sameInstantAsDefault }).Value.Offset, Is.EqualTo(TimeSpan.FromHours(5)));
        }

        [Test]
        public void DateTimeOffset_SubMillisecondIsIntentionallyDropped()
        {
            var withTicks = new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, TimeSpan.FromHours(2)).AddTicks(1234);
            var expected = new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, TimeSpan.FromHours(2));
            Assert.That(Bin(new DateTimeOffsetBox { Value = withTicks }).Value, Is.EqualTo(expected));
            Assert.That(Jsn(new DateTimeOffsetBox { Value = withTicks }).Value, Is.EqualTo(expected));
        }

        [Test]
        public void DateTimeOffset_JsonIsReadable()
        {
            var json = new NeuroJsonWriter().Write(new DateTimeOffsetBox
            {
                Value = new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, new TimeSpan(5, 30, 0))
            });
            TestContext.WriteLine(json);
            Assert.IsTrue(json.Contains("\"2024-06-05T04:03:02:001+05:30\""), json);
        }

        [Test]
        public void DateTimeOffset_JsonNegativeOffsetIsReadable()
        {
            var json = new NeuroJsonWriter().Write(new DateTimeOffsetBox
            {
                Value = new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, new TimeSpan(-5, -45, 0))
            });
            Assert.IsTrue(json.Contains("\"2024-06-05T04:03:02:001-05:45\""), json);
        }

        [Test]
        public void DateTimeOffset_ReadsIsoFromElsewhere()
        {
            // anything not in our own 29 char shape falls through to the framework parser.
            var copy = ReadJson<DateTimeOffsetBox>(@"{""Value"": ""2024-06-05T04:03:02.0010000+05:30""}");
            Assert.That(copy.Value, Is.EqualTo(new DateTimeOffset(2024, 6, 5, 4, 3, 2, 1, new TimeSpan(5, 30, 0))));
        }

        [Test]
        public void DateTimeOffset_WriteDoesNotMutateSource()
        {
            var src = new DateTimeOffsetBox { Value = DateTimeOffset.MaxValue };
            new NeuroBytesWriter().Write(src);
            Assert.That(src.Value, Is.EqualTo(DateTimeOffset.MaxValue), "writing changed the object that was written");
        }

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
    }
}
