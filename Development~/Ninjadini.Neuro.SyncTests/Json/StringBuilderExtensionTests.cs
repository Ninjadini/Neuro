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
            Assert.AreEqual("1234", new StringBuilder().AppendNum(1234u, false).ToString());
            Assert.AreEqual("0", new StringBuilder().AppendNum(0u).ToString());
            Assert.AreEqual(uint.MaxValue.ToString(), new StringBuilder().AppendNum(uint.MaxValue, false).ToString());
            Assert.AreEqual(uint.MinValue.ToString(), new StringBuilder().AppendNum(uint.MinValue, false).ToString());
        }
        
        [Test]
        public void TestInt()
        {
            Assert.AreEqual("1234", new StringBuilder().AppendNum(1234, false).ToString());
            Assert.AreEqual("0", new StringBuilder().AppendNum(0, false).ToString());
            Assert.AreEqual("-1", new StringBuilder().AppendNum(-1, false).ToString());
            Assert.AreEqual(int.MaxValue.ToString(), new StringBuilder().AppendNum(int.MaxValue, false).ToString());
            Assert.AreEqual(int.MinValue.ToString(), new StringBuilder().AppendNum(int.MinValue, false).ToString());
        }

        [Test]
        public void TestIntGrouping()
        {
            Assert.AreEqual("123", new StringBuilder().AppendNum(123, true).ToString());
            Assert.AreEqual("1,234", new StringBuilder().AppendNum(1234, true).ToString());
            Assert.AreEqual("12,345", new StringBuilder().AppendNum(12345, true).ToString());
            Assert.AreEqual("123,456", new StringBuilder().AppendNum(123456, true).ToString());
            Assert.AreEqual("1,234,567", new StringBuilder().AppendNum(1234567, true).ToString());
        }

        [Test]
        public void TestIntPadding()
        {
            Assert.AreEqual("123", new StringBuilder().AppendNumWithZeroPadding(123, 1).ToString());
            Assert.AreEqual("123", new StringBuilder().AppendNumWithZeroPadding(123, 3).ToString());
            Assert.AreEqual("0123", new StringBuilder().AppendNumWithZeroPadding(123, 4).ToString());
            Assert.AreEqual("001", new StringBuilder().AppendNumWithZeroPadding(1, 3).ToString());
            Assert.AreEqual("000", new StringBuilder().AppendNumWithZeroPadding(0, 3).ToString());
            Assert.AreEqual("-003", new StringBuilder().AppendNumWithZeroPadding(-3, 3).ToString());
        }

        [Test]
        public void TestFloat()
        {
            Assert.AreEqual("1234", new StringBuilder().AppendNum(1234f, group:false).ToString());
            Assert.AreEqual("1,234", new StringBuilder().AppendNum(1234f, group:true).ToString());
            Assert.AreEqual("0", new StringBuilder().AppendNum(0f).ToString());
            Assert.AreEqual("-1", new StringBuilder().AppendNum(-1f).ToString());
            Assert.AreEqual("123.45", new StringBuilder().AppendNum(123.45f).ToString());
            Assert.AreEqual("123.4567", new StringBuilder().AppendNum(123.4567f).ToString());
            Assert.AreEqual("-123.4567", new StringBuilder().AppendNum(-123.4567f).ToString());
            
            Assert.AreEqual("123.45", new StringBuilder().AppendNum(123.4567f, 2).ToString());
            
            Assert.AreEqual("123.4000", new StringBuilder().AppendNum(123.4f, 2, 4).ToString());
            Assert.AreEqual("123.40", new StringBuilder().AppendNum(123.4f, 4, 2).ToString());
            Assert.AreEqual("123.45", new StringBuilder().AppendNum(123.456f, 2, 1).ToString());
            Assert.AreEqual("123.00", new StringBuilder().AppendNum(123f, 2, 2).ToString());
            
            Assert.AreEqual("12,345.67", new StringBuilder().AppendNum(12345.6789f, 2, group:true).ToString());
            Assert.AreEqual("12,345.50", new StringBuilder().AppendNum(12345.50f, minDecimalPlaces:2, group:true).ToString());
            Assert.AreEqual("1,234,567.5", new StringBuilder().AppendNum(1234567.5f, group:true).ToString());
        }
    }
}