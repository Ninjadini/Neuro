using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    public class NeuroRefIdTests
    {
        [SetUp]
        public void SetUp()
        {
            UberTestClass.RegisterAll();
            RegisterAll();
        }

        [TestCase(0u, "0")]
        [TestCase(1u, "1")]
        [TestCase(9u, "9")]
        [TestCase(10u, "a")]
        [TestCase(20u, "k")]
        [TestCase(35u, "z")]
        [TestCase(36u, "10")]
        [TestCase(46656u, "1000")]
        [TestCase(87123u, "1v83")]
        [TestCase(1679615u, "zzzz")]
        [TestCase(uint.MaxValue, "1z141z3")]
        public void ToStringCases(uint id, string expected)
        {
            Assert.That(NeuroRefId.ToString(id), Is.EqualTo(expected));
        }

        [Test]
        public void EverySpellingIsOneIdAndEveryIdOneSpelling()
        {
            // the point of dropping the decimal form: no id has two spellings and no spelling has two ids
            var seen = new Dictionary<string, uint>();
            for (var id = 0u; id < 200000; id++)
            {
                var text = NeuroRefId.ToString(id);
                Assert.IsFalse(seen.ContainsKey(text), $"`{text}` is two different ids");
                seen[text] = id;
                Assert.That(NeuroRefId.Parse(text), Is.EqualTo(id));
            }
        }

        [Test]
        public void EveryIdRoundTrips()
        {
            // exhaustive over the generated range plus the boundaries around it
            for (var id = 0u; id < 60000; id++)
            {
                Assert.That(NeuroRefId.Parse(NeuroRefId.ToString(id)), Is.EqualTo(id), "id " + id);
            }
            for (var id = NeuroRefId.GeneratedMaxValue - 10000; id <= NeuroRefId.GeneratedMaxValue + 10; id++)
            {
                Assert.That(NeuroRefId.Parse(NeuroRefId.ToString(id)), Is.EqualTo(id), "id " + id);
            }
            Assert.That(NeuroRefId.Parse(NeuroRefId.ToString(uint.MaxValue)), Is.EqualTo(uint.MaxValue));
        }

        [Test]
        public void GeneratedRangeIsAlways4Chars()
        {
            // this is the whole point of the range - every generated id is exactly 4 chars, no exceptions
            for (var id = NeuroRefId.GeneratedMinValue; id <= NeuroRefId.GeneratedMaxValue; id++)
            {
                Assert.That(NeuroRefId.ToString(id).Length, Is.EqualTo(4), "id " + id);
            }
        }

        [Test]
        public void GeneratedRangeFitsIn3ByteVarint()
        {
            Assert.LessOrEqual(NeuroRefId.GeneratedMaxValue, (1u << 21) - 1);
        }

        [Test]
        public void EncodingHasNoUpperBound()
        {
            // GeneratedMaxValue bounds where new ids come from, not what the encoding can represent -
            // an id set by hand above it still writes as base36.
            Assert.That(NeuroRefId.ToString(NeuroRefId.GeneratedMaxValue), Is.EqualTo("zzzz"));
            Assert.That(NeuroRefId.ToString(NeuroRefId.GeneratedMaxValue + 1), Is.EqualTo("10000"));
            Assert.That(NeuroRefId.ToString(uint.MaxValue), Is.EqualTo("1z141z3"));
            Assert.That(NeuroRefId.Parse("1z141z3"), Is.EqualTo(uint.MaxValue));
        }

        [Test]
        public void EverythingIsReadAsBase36()
        {
            Assert.That(NeuroRefId.Parse("1"), Is.EqualTo(1u));
            Assert.That(NeuroRefId.Parse("20"), Is.EqualTo(72u), "`20` is base36 now, not the decimal 20");
            Assert.That(NeuroRefId.Parse("100"), Is.EqualTo(1296u));
        }

        [TestCase("1", 1u)]
        [TestCase("20", 20u)]
        [TestCase("100", 100u)]
        [TestCase("87123", 87123u)]
        [TestCase("4294967295", uint.MaxValue)]
        [TestCase("k", 20u)]
        [TestCase("1v83", 87123u)]
        public void LegacyParseReadsTheOldDecimalSpelling(string text, uint expected)
        {
            // what the migration uses to work out what an old file name or id actually meant
            Assert.IsTrue(NeuroRefId.TryParseLegacy(text, out var id));
            Assert.That(id, Is.EqualTo(expected));
        }

        [Test]
        public void MigrationFixupRecoversTheOldId()
        {
            // an old id read by the new base36 reader is recoverable, which is what lets the migration run
            // without a legacy mode in the reader: legacy(base36Text(misread)) == the original id.
            for (var original = 0u; original < 200000; original++)
            {
                var oldText = original.ToString();
                Assert.IsTrue(NeuroRefId.TryParse(oldText, out var misread));
                Assert.IsTrue(NeuroRefId.TryParseLegacy(NeuroRefId.ToString(misread), out var recovered));
                Assert.That(recovered, Is.EqualTo(original), "old text " + oldText);
            }
        }

        [Test]
        public void ParsesUpperCase()
        {
            Assert.That(NeuroRefId.Parse("1V83"), Is.EqualTo(NeuroRefId.Parse("1v83")));
        }

        [TestCase("")]
        [TestCase("-1")]
        [TestCase("1.5")]
        [TestCase("hello world")]
        [TestCase("4294967296")] // one over uint.MaxValue
        [TestCase("zzzzzzzz")] // way over uint.MaxValue in base36
        public void RejectsInvalid(string str)
        {
            Assert.IsFalse(NeuroRefId.TryParse(str, out _));
        }

        [Test]
        public void JsonWritesBase36AsAQuotedString()
        {
            var refs = new NeuroReferences();
            var testObj = new RefIdTestClass();
            testObj.Ref.RefId = 87123;

            var json = new NeuroJsonWriter().Write(testObj, refs);
            Assert.IsTrue(json.Contains("\"1v83\""), json);
        }

        [Test]
        public void JsonAlwaysWritesRefIdsAsStrings()
        {
            var refs = new NeuroReferences();
            var testObj = new RefIdTestClass();
            testObj.Ref.RefId = 20;

            var json = new NeuroJsonWriter().Write(testObj, refs);
            Assert.IsTrue(json.Contains("\"Ref\": \"k\""), json);
        }

        [Test]
        public void JsonRoundTripsBothForms()
        {
            var refs = new NeuroReferences();
            var testObj = new RefIdTestClass();
            testObj.Ref.RefId = 87123;
            testObj.Refs.Add(new Reference<ReferencableClass>() { RefId = 20 });
            testObj.Refs.Add(new Reference<ReferencableClass>() { RefId = 1679615 });
            testObj.Refs.Add(new Reference<ReferencableClass>() { RefId = 46656 });

            var json = new NeuroJsonWriter().Write(testObj, refs);
            var result = new NeuroJsonReader().Read<RefIdTestClass>(json);

            Assert.That(result.Ref.RefId, Is.EqualTo(87123u));
            Assert.That(result.Refs[0].RefId, Is.EqualTo(20u));
            Assert.That(result.Refs[1].RefId, Is.EqualTo(1679615u));
            Assert.That(result.Refs[2].RefId, Is.EqualTo(46656u));
        }

        [Test]
        public void JsonReadsBase36AlongsideTheRefName()
        {
            var refs = new NeuroReferences();
            refs.Register(new ReferencableClass() { RefId = 87123, RefName = "my_item", Name = "n" });

            var testObj = new RefIdTestClass();
            testObj.Ref.RefId = 87123;

            var json = new NeuroJsonWriter().Write(testObj, refs);
            Assert.IsTrue(json.Contains("\"1v83:my_item\""), json);
            Assert.That(new NeuroJsonReader().Read<RefIdTestClass>(json).Ref.RefId, Is.EqualTo(87123u));
        }

        [Test]
        public void JsonReadsUnquotedRefIdsTheSameAsQuotedOnes()
        {
            // The writer quotes every RefId, but hand written json may not. Whenever an unquoted token gets
            // past the json tokenizer at all, it has to mean the same id as the quoted form.
            Assert.That(ReadRef("20"), Is.EqualTo(ReadRef("\"20\"")), "all digits");
            Assert.That(ReadRef("20"), Is.EqualTo(72u), "`20` is base36, so 72 - quoted or not");
            Assert.That(ReadRef("1e83"), Is.EqualTo(ReadRef("\"1e83\"")), "`e` is both a base36 digit and a json exponent");
        }

        [TestCase("k", Description = "leading letter")]
        [TestCase("1v83", Description = "letter inside")]
        public void UnquotedRefIdsWithLettersAreNotValidJson(string unquoted)
        {
            // Not a regression to fix in the reader - `1v83` bare is simply not json. It is worth pinning down
            // because RefIds usually have letters in them now, so hand editing without quotes will hit this.
            Assert.Throws<Exception>(() => ReadRef(unquoted));
            Assert.That(ReadRef("\"" + unquoted + "\""), Is.EqualTo(NeuroRefId.Parse(unquoted)), "quoted is fine");
        }

        static uint ReadRef(string jsonToken)
        {
            return new NeuroJsonReader().Read<RefIdTestClass>("{\"Ref\": " + jsonToken + "}").Ref.RefId;
        }

        static bool _registered;
        static void RegisterAll()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref RefIdTestClass value)
            {
                value ??= new RefIdTestClass();
                neuro.Sync(1, nameof(value.Ref), ref value.Ref);
                neuro.Sync(2, nameof(value.Refs), ref value.Refs);
            });
        }

        class RefIdTestClass
        {
            public Reference<ReferencableClass> Ref;
            public List<Reference<ReferencableClass>> Refs = new List<Reference<ReferencableClass>>();
        }
    }
}
