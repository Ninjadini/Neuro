using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
// ---------------------------------------------------------------------------------------------------- json we write is valid json

        static void AssertNoRawControlCharacters(string json)
        {
            for (var i = 0; i < json.Length; i++)
            {
                var c = json[i];
                // the writer's own pretty printing newlines are the only control characters allowed through.
                if (c < ' ' && c != '\n')
                {
                    Assert.Fail($"raw U+{(int)c:X4} at index {i} of: {json}");
                }
            }
        }

        [Test]
        public void Write_EscapesControlCharactersInValues()
        {
            var json = new NeuroJsonWriter().Write(new StringBox { Value = "tab\there\rand\u0000nul\bback\ffeed" });
            TestContext.WriteLine(json);
            AssertNoRawControlCharacters(json);
            Assert.IsTrue(json.Contains(@"tab\there\rand\u0000nul\bback\ffeed"), json);
        }

        [Test]
        public void Write_EscapesControlCharactersInDictionaryKeys()
        {
            var json = new NeuroJsonWriter().Write(new DictBox { StringKeys = AwkwardStringKeyed() });
            TestContext.WriteLine(json);
            AssertNoRawControlCharacters(json);
        }

        [Test]
        public void Write_EscapesControlCharactersInLists()
        {
            var json = new NeuroJsonWriter().Write(new ListBox { Strings = new List<string> { "a\tb", "c\rd", null, "" } });
            TestContext.WriteLine(json);
            AssertNoRawControlCharacters(json);
        }

        [Test]
        public void Write_LeavesUnicodeAlone()
        {
            var json = new NeuroJsonWriter().Write(new StringBox { Value = "\u00e9\u4e2d\U0001F600" });
            Assert.IsTrue(json.Contains("\u00e9\u4e2d\U0001F600"), json);
        }

// ---------------------------------------------------------------------------------------------------- reading json we did not write

        // The writer only ever emits \n, \" and \\, so nothing above covers the rest of the escapes.
        // Anything else that produced the file - a hand edit, another serializer - can use all of them.

        static T ReadJson<T>(string json) where T : class, new()
        {
            TestContext.WriteLine(json);
            return new NeuroJsonReader().Read<T>(json, new ReaderOptions());
        }

        [Test]
        public void Read_StandardEscapes()
        {
            var copy = ReadJson<StringBox>(@"{""Value"": ""a\tb\rc\bd\fe\/f\""g\\h""}");
            Assert.That(copy.Value, Is.EqualTo("a\tb\rc\bd\fe/f\"g\\h"));
        }

        [Test]
        public void Read_UnicodeEscapes()
        {
            var copy = ReadJson<StringBox>(@"{""Value"": ""\u0041\u00e9\u4E2D""}");
            Assert.That(copy.Value, Is.EqualTo("A\u00e9\u4e2d"));
        }

        [Test]
        public void Read_SurrogatePairEscape()
        {
            var copy = ReadJson<StringBox>(@"{""Value"": ""\ud83d\ude00""}");
            Assert.That(copy.Value, Is.EqualTo("\U0001F600"));
        }

        [Test]
        public void Read_EscapedQuoteAtEndOfString()
        {
            Assert.That(ReadJson<StringBox>(@"{""Value"": ""say \""hi\""""}").Value, Is.EqualTo("say \"hi\""));
        }

        [Test]
        public void Read_BackslashAtEndOfString()
        {
            Assert.That(ReadJson<StringBox>(@"{""Value"": ""ends with \\""}").Value, Is.EqualTo("ends with \\"));
            Assert.That(ReadJson<StringBox>(@"{""Value"": ""\\""}").Value, Is.EqualTo("\\"));
        }

        [Test]
        public void Read_QuotedNullIsAString()
        {
            Assert.That(ReadJson<StringBox>(@"{""Value"": ""null""}").Value, Is.EqualTo("null"));
        }

        [Test]
        public void Read_BareNullIsNull()
        {
            Assert.IsNull(ReadJson<StringBox>(@"{""Value"": null}").Value);
        }

        [Test]
        public void Read_MissingFieldKeepsDefault()
        {
            Assert.IsNull(ReadJson<StringBox>(@"{}").Value);
        }

        [TestCase("NaN", float.NaN)]
        [TestCase("Infinity", float.PositiveInfinity)]
        [TestCase("-Infinity", float.NegativeInfinity)]
        public void Read_FloatSpecialValues(string written, float expected)
        {
            Assert.That(ReadJson<FloatBox>(@"{""Value"": " + written + "}").Value, Is.EqualTo(expected));
        }

        [TestCase("NaN", double.NaN)]
        [TestCase("Infinity", double.PositiveInfinity)]
        [TestCase("-Infinity", double.NegativeInfinity)]
        public void Read_DoubleSpecialValues(string written, double expected)
        {
            Assert.That(ReadJson<DoubleBox>(@"{""Value"": " + written + "}").Value, Is.EqualTo(expected));
        }

        [TestCase("1e3", 1000f)]
        [TestCase("1.5E-8", 1.5e-8f)]
        [TestCase("-2.5e+2", -250f)]
        [TestCase("0.0000001", 0.0000001f)]
        public void Read_ExponentNotation(string written, float expected)
        {
            Assert.That(ReadJson<FloatBox>(@"{""Value"": " + written + "}").Value, Is.EqualTo(expected));
        }

        [Test]
        public void Read_NumbersAreCultureIndependent()
        {
            // a comma decimal separator culture must not turn 1.5 into 15.
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.That(ReadJson<FloatBox>(@"{""Value"": 1.5}").Value, Is.EqualTo(1.5f));
                Assert.That(ReadJson<DoubleBox>(@"{""Value"": -1234.5678}").Value, Is.EqualTo(-1234.5678d));
                Assert.That(Jsn(new FloatBox { Value = 1.5f }).Value, Is.EqualTo(1.5f));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
