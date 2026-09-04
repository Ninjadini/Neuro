using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
// ---------------------------------------------------------------------------------------------------- string

        public partial class StringBox
        {
            [Neuro(1)] public string Value;
        }

        static IEnumerable<TestCaseData> StringValues()
        {
            yield return new TestCaseData(null).SetName("String_null");
            yield return new TestCaseData("").SetName("String_empty");
            yield return new TestCaseData(" ").SetName("String_space");
            yield return new TestCaseData("plain").SetName("String_plain");
            yield return new TestCaseData("null").SetName("String_literalNullWord");
            yield return new TestCaseData("true").SetName("String_literalTrueWord");
            yield return new TestCaseData("123").SetName("String_literalNumber");
            yield return new TestCaseData("has \"quotes\"").SetName("String_quotes");
            yield return new TestCaseData("has \\ backslash").SetName("String_backslash");
            yield return new TestCaseData("trailing backslash \\").SetName("String_trailingBackslash");
            yield return new TestCaseData("back\\nslash-n").SetName("String_backslashThenN");
            yield return new TestCaseData("line\nbreak").SetName("String_newline");
            yield return new TestCaseData("carriage\rreturn").SetName("String_carriageReturn");
            yield return new TestCaseData("tab\there").SetName("String_tab");
            yield return new TestCaseData("\b\f\v").SetName("String_backspaceFormfeedVtab");
            yield return new TestCaseData("nul\0char").SetName("String_nulChar");
            yield return new TestCaseData("json{\"a\":1,\"b\":[2]}").SetName("String_looksLikeJson");
            yield return new TestCaseData("unicode: é中文Ж").SetName("String_unicode");
            yield return new TestCaseData("emoji: \U0001F600\U0001F1F3\U0001F1FF").SetName("String_surrogatePairs");
            yield return new TestCaseData("rtl: ‮reversed‬").SetName("String_bidiControl");
            yield return new TestCaseData(new string('x', 200000)).SetName("String_200k");
        }

        [Test]
        public void String_LoneSurrogate_IsReplacedInBinaryButKeptInJson()
        {
            // A lone surrogate is not valid text and has no UTF-8 encoding, so the binary format replaces it
            // with U+FFFD. Json keeps it because it never leaves UTF-16. Switching the wire format to UTF-16
            // would preserve it, at the cost of doubling every ascii string - not worth it for malformed input.
            var src = new StringBox { Value = "lone surrogate: \ud800" };
            Assert.AreEqual("lone surrogate: \ufffd", Bin(src).Value, "binary");
            Assert.AreEqual("lone surrogate: \ud800", Jsn(src).Value, "json");
        }

        [TestCaseSource(nameof(StringValues))]
        public void String_Binary(string v) => Assert.AreEqual(v, Bin(new StringBox { Value = v }).Value);

        [TestCaseSource(nameof(StringValues))]
        public void String_Json(string v) => Assert.AreEqual(v, Jsn(new StringBox { Value = v }).Value);
    }
}
