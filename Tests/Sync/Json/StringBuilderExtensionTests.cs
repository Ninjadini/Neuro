using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Ninjadini.Neuro.Utils;

namespace Ninjadini.Neuro.SyncTests
{
    public class StringBuilderExtensionTests
    {
        [Test]
        public void TestUint()
        {
            Assert.That(new StringBuilder().AppendNum(1234u, false).ToString(), Is.EqualTo("1234"));
            Assert.That(new StringBuilder().AppendNum(0u).ToString(), Is.EqualTo("0"));
            Assert.That(new StringBuilder().AppendNum(uint.MaxValue, false).ToString(), Is.EqualTo(uint.MaxValue.ToString()));
            Assert.That(new StringBuilder().AppendNum(uint.MinValue, false).ToString(), Is.EqualTo(uint.MinValue.ToString()));
        }
        
        [Test]
        public void TestInt()
        {
            Assert.That(new StringBuilder().AppendNum(1234, false).ToString(), Is.EqualTo("1234"));
            Assert.That(new StringBuilder().AppendNum(0, false).ToString(), Is.EqualTo("0"));
            Assert.That(new StringBuilder().AppendNum(-1, false).ToString(), Is.EqualTo("-1"));
            Assert.That(new StringBuilder().AppendNum(int.MaxValue, false).ToString(), Is.EqualTo(int.MaxValue.ToString()));
            Assert.That(new StringBuilder().AppendNum(int.MinValue, false).ToString(), Is.EqualTo(int.MinValue.ToString()));
        }

        [Test]
        public void TestIntGrouping()
        {
            Assert.That(new StringBuilder().AppendNum(123, true).ToString(), Is.EqualTo("123"));
            Assert.That(new StringBuilder().AppendNum(1234, true).ToString(), Is.EqualTo("1,234"));
            Assert.That(new StringBuilder().AppendNum(12345, true).ToString(), Is.EqualTo("12,345"));
            Assert.That(new StringBuilder().AppendNum(123456, true).ToString(), Is.EqualTo("123,456"));
            Assert.That(new StringBuilder().AppendNum(1234567, true).ToString(), Is.EqualTo("1,234,567"));
        }

        [Test]
        public void TestIntPadding()
        {
            Assert.That(new StringBuilder().AppendNumWithZeroPadding(123, 1).ToString(), Is.EqualTo("123"));
            Assert.That(new StringBuilder().AppendNumWithZeroPadding(123, 3).ToString(), Is.EqualTo("123"));
            Assert.That(new StringBuilder().AppendNumWithZeroPadding(123, 4).ToString(), Is.EqualTo("0123"));
            Assert.That(new StringBuilder().AppendNumWithZeroPadding(1, 3).ToString(), Is.EqualTo("001"));
            Assert.That(new StringBuilder().AppendNumWithZeroPadding(0, 3).ToString(), Is.EqualTo("000"));
            Assert.That(new StringBuilder().AppendNumWithZeroPadding(-3, 3).ToString(), Is.EqualTo("-003"));
        }

        [Test]
        public void TestFloat()
        {
            Assert.That(new StringBuilder().AppendNum(1234f, group:false).ToString(), Is.EqualTo("1234"));
            Assert.That(new StringBuilder().AppendNum(1234f, group:true).ToString(), Is.EqualTo("1,234"));
            Assert.That(new StringBuilder().AppendNum(0f).ToString(), Is.EqualTo("0"));
            Assert.That(new StringBuilder().AppendNum(-1f).ToString(), Is.EqualTo("-1"));
            Assert.That(new StringBuilder().AppendNum(123.45f).ToString(), Is.EqualTo("123.45"));
            Assert.That(new StringBuilder().AppendNum(123.4567f).ToString(), Is.EqualTo("123.4567"));
            Assert.That(new StringBuilder().AppendNum(-123.4567f).ToString(), Is.EqualTo("-123.4567"));

            Assert.That(new StringBuilder().AppendNum(123.4567f, 2).ToString(), Is.EqualTo("123.45"));

            Assert.That(new StringBuilder().AppendNum(123.4f, 2, 4).ToString(), Is.EqualTo("123.4000"));
            Assert.That(new StringBuilder().AppendNum(123.4f, 4, 2).ToString(), Is.EqualTo("123.40"));
            Assert.That(new StringBuilder().AppendNum(123.456f, 2, 1).ToString(), Is.EqualTo("123.45"));
            Assert.That(new StringBuilder().AppendNum(123f, 2, 2).ToString(), Is.EqualTo("123.00"));

            Assert.That(new StringBuilder().AppendNum(12345.6789f, 2, group:true).ToString(), Is.EqualTo("12,345.67"));
            Assert.That(new StringBuilder().AppendNum(12345.50f, minDecimalPlaces:2, group:true).ToString(), Is.EqualTo("12,345.50"));
            Assert.That(new StringBuilder().AppendNum(1234567.5f, group:true).ToString(), Is.EqualTo("1,234,567.5"));
        }
    }
}