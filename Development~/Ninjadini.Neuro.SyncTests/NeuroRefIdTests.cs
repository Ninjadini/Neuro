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
            Assert.AreEqual(expected, NeuroRefId.ToString(id));
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
                Assert.AreEqual(id, NeuroRefId.Parse(text));
            }
        }

        [Test]
        public void EveryIdRoundTrips()
        {
            // exhaustive over the generated range plus the boundaries around it
            for (var id = 0u; id < 60000; id++)
            {
                Assert.AreEqual(id, NeuroRefId.Parse(NeuroRefId.ToString(id)), "id " + id);
            }
            for (var id = NeuroRefId.GeneratedMaxValue - 10000; id <= NeuroRefId.GeneratedMaxValue + 10; id++)
            {
                Assert.AreEqual(id, NeuroRefId.Parse(NeuroRefId.ToString(id)), "id " + id);
            }
            Assert.AreEqual(uint.MaxValue, NeuroRefId.Parse(NeuroRefId.ToString(uint.MaxValue)));
        }

        [Test]
        public void GeneratedRangeIsAlways4Chars()
        {
            // this is the whole point of the range - every generated id is exactly 4 chars, no exceptions
            for (var id = NeuroRefId.GeneratedMinValue; id <= NeuroRefId.GeneratedMaxValue; id++)
            {
                Assert.AreEqual(4, NeuroRefId.ToString(id).Length, "id " + id);
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
            Assert.AreEqual("zzzz", NeuroRefId.ToString(NeuroRefId.GeneratedMaxValue));
            Assert.AreEqual("10000", NeuroRefId.ToString(NeuroRefId.GeneratedMaxValue + 1));
            Assert.AreEqual("1z141z3", NeuroRefId.ToString(uint.MaxValue));
            Assert.AreEqual(uint.MaxValue, NeuroRefId.Parse("1z141z3"));
        }

        [Test]
        public void EverythingIsReadAsBase36()
        {
            Assert.AreEqual(1u, NeuroRefId.Parse("1"));
            Assert.AreEqual(72u, NeuroRefId.Parse("20"), "`20` is base36 now, not the decimal 20");
            Assert.AreEqual(1296u, NeuroRefId.Parse("100"));
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
            Assert.AreEqual(expected, id);
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
                Assert.AreEqual(original, recovered, "old text " + oldText);
            }
        }

        [Test]
        public void ParsesUpperCase()
        {
            Assert.AreEqual(NeuroRefId.Parse("1v83"), NeuroRefId.Parse("1V83"));
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

            Assert.AreEqual(87123u, result.Ref.RefId);
            Assert.AreEqual(20u, result.Refs[0].RefId);
            Assert.AreEqual(1679615u, result.Refs[1].RefId);
            Assert.AreEqual(46656u, result.Refs[2].RefId);
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
            Assert.AreEqual(87123u, new NeuroJsonReader().Read<RefIdTestClass>(json).Ref.RefId);
        }

        [Test]
        public void JsonReadsUnquotedRefIdsTheSameAsQuotedOnes()
        {
            // The writer quotes every RefId, but hand written json may not. Whenever an unquoted token gets
            // past the json tokenizer at all, it has to mean the same id as the quoted form.
            Assert.AreEqual(ReadRef("\"20\""), ReadRef("20"), "all digits");
            Assert.AreEqual(72u, ReadRef("20"), "`20` is base36, so 72 - quoted or not");
            Assert.AreEqual(ReadRef("\"1e83\""), ReadRef("1e83"), "`e` is both a base36 digit and a json exponent");
        }

        [TestCase("k", Description = "leading letter")]
        [TestCase("1v83", Description = "letter inside")]
        public void UnquotedRefIdsWithLettersAreNotValidJson(string unquoted)
        {
            // Not a regression to fix in the reader - `1v83` bare is simply not json. It is worth pinning down
            // because RefIds usually have letters in them now, so hand editing without quotes will hit this.
            Assert.Throws<Exception>(() => ReadRef(unquoted));
            Assert.AreEqual(NeuroRefId.Parse(unquoted), ReadRef("\"" + unquoted + "\""), "quoted is fine");
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
