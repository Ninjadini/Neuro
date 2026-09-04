using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Sync;
using Ninjadini.Neuro.Utils;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    public class JsonTests
    {
        [SetUp]
        public void SetUp()
        {
            UberTestClass.RegisterAll();
        }
        
        [Test]
        public void TestJsonWrite()
        {
            var refs = new NeuroReferences();
            var testObj = UberTestClass.CreateTestClass(refs);

            Test(testObj, refs);
        }
        
        [Test]
        public void ReadWrite_UberTestClass()
        {
            var refs = new NeuroReferences();

            var testRef = new ReferencableClass()
            {
                RefId = 87123,
                Name = "MyReferencableClass"
            };
            refs.Register(testRef);

            var testObj = new UberTestClass()
            {
                Id = 123,
                Name = "Hello",
                Date = DateTime.UtcNow.StripMicroSeconds(),
                TimeSpan = TimeSpan.FromMilliseconds(12345678),
                Enum = TestEnum1.B,
                FlagEnum = TestFlagEnum1.B | TestFlagEnum1.C,
                ClassObj = new TestChildClass()
                {
                    Id = 234
                },
                Struct = new TestStruct()
                {
                    Id = 22,
                    Name = "StructName"
                },
                Referencable = testRef,
                NullableId = 1,
                NullableStr = new TestStruct()
                {
                    
                },
                NullableDate = new DateTime(),
                BaseClassObj = new SubTestClass1()
                {
                    Id = 3,
                    NumValue = 4
                },
                ListInt = new List<int>()
                {
                    5, 4, 3, 2
                },
                ListClass = new List<TestChildClass>()
                {
                    new TestChildClass(){ Id = 1 }
                },
                ListBaseClasses = new List<BaseTestClass1>()
                {
                    new BaseTestClass1(){ Id = 31},
                    new SubTestClass1() { Value = "ab", Id = 4}
                },
            };
            testObj.ListTexts.Add("Hi");
            Test(testObj, refs);
        }
        
        [Test]
        public void TestSubCLass()
        {
            var testObj = new UberTestClass()
            {
                BaseClassObj = new SubTestClass1()
                {
                    Id = 3,
                    NumValue = 4
                }
            };
            Test(testObj);
        }
        
        [Test]
        public void TestGlobalTypeEmpty()
        {
            var obj = new ReferencableClass()
            {
            };
            object globalTyped = obj;
            var json = NeuroJsonWriter.Shared.WriteGlobalTyped(globalTyped);
            Console.WriteLine(json);

            Assert.That(json, Is.EqualTo("{\n    \"-globalType\": \"11:ReferencableClass\",\n}"));
        }
        
        [Test]
        public void TestGlobalTypeBasic()
        {
            var obj = new ReferencableClass()
            {
                Name = "HELLO"
            };
            object globalTyped = obj;
            var json = NeuroJsonWriter.Shared.WriteGlobalTyped(globalTyped);
            Console.WriteLine(json);

            Assert.That(json, Is.EqualTo("{\n    \"-globalType\": \"11:ReferencableClass\",\n    \"Name\": \"HELLO\"\n}"));
            
            var copy = NeuroJsonReader.Shared.ReadGlobalTyped(json, new ReaderOptions()) as ReferencableClass;
            Assert.That(copy.Name, Is.EqualTo(obj.Name));
        }
        
        [Test]
        public void TestGlobalTypePolymorphic()
        {
            var obj = new SubTestClass1()
            {
                Id = 123,
                Name = "HELLO",
                NumValue = 234
            }; 
            object globalTyped = obj;
            var json = NeuroJsonWriter.Shared.WriteGlobalTyped(globalTyped);
            Console.WriteLine(json);

            var copy = NeuroJsonReader.Shared.ReadGlobalTyped(json, new ReaderOptions()) as SubTestClass1;
            Assert.That(copy.Id, Is.EqualTo(obj.Id));
            Assert.That(copy.Name, Is.EqualTo(obj.Name));
            Assert.That(copy.NumValue, Is.EqualTo(obj.NumValue));
        }

        [Test]
        public void CustomJson()
        {
            var refs = new NeuroReferences();
            var testObj = new UberTestClass()
            {
                SingleNumber = new SingleNumberStruct()
                {
                    Number = 12.3456f
                }
            };
            var json = NeuroJsonWriter.Shared.Write(testObj);
            Assert.IsTrue(json.Contains("12345"));

            Test(testObj, refs);

            var copyBytes = NeuroBytesWriter.Shared.Write(testObj).ToArray();
            var copy = NeuroBytesReader.Shared.Read<UberTestClass>(copyBytes, new ReaderOptions());
            Assert.That(copy.SingleNumber.Number, Is.EqualTo(12.345f));
            //Assert.IsTrue(Math.Abs(12.345f - copy.SingleNumber.Number) < 0.0001f);
        }

        [Test]
        public void TestStringLineBreaks()
        {
            var obj = new StringTest()
            {
                String = "Hello,\nLine 2 here\nLine 3"
            };
            TestStringOutput(obj);
            obj = new StringTest()
            {
                String = "Hello,\n"
            };
            TestStringOutput(obj);
        }

        [Test]
        public void TestSafeString1()
        {
            UberTestClass.RegisterAll();
            var obj = new StringTest()
            {
                String = "§±';\\|/.,`~?><}{][\"!@£$%^&*(\n)_+-="
            };
            TestStringOutput(obj);
        }

        [Test]
        public void TestSafeString2()
        {
            UberTestClass.RegisterAll();
            var obj = new StringTest()
            {
                String = "\"§±\n';\\|/.\"\",`~?><}{][\"!@£$%^\"&*()_+\n-=\""
            };
            TestStringOutput(obj);
        }

        [Test]
        public void TestControlCharacterEscapes()
        {
            UberTestClass.RegisterAll();
            // every character the json spec says must be escaped, checked against Newtonsoft's output.
            TestStringOutput(new StringTest() { String = "tab\there" });
            TestStringOutput(new StringTest() { String = "carriage\rreturn" });
            TestStringOutput(new StringTest() { String = "windows\r\nline" });
            TestStringOutput(new StringTest() { String = "backspace\bformfeed\fvtab\v" });
            TestStringOutput(new StringTest() { String = "nul\0char" });
            TestStringOutput(new StringTest() { String = "\u0001\u0002\u001e\u001f" });
            TestStringOutput(new StringTest() { String = "\t\r\n\\\"\b\f" });
            TestStringOutput(new StringTest() { String = "unicode stays raw: \u00e9\u4e2d\U0001F600" });
        }

        [Test]
        public void TestControlCharactersAreActuallyEscaped()
        {
            UberTestClass.RegisterAll();
            var json = NeuroJsonWriter.Shared.Write(new StringTest() { String = "a\tb\rc\u001fd" });
            Assert.IsTrue(json.Contains(@"a\tb\rc\u001fd"), json);
            Assert.IsFalse(json.Contains("\t"), "a raw tab was written into the json");
            Assert.IsFalse(json.Contains("\r"), "a raw carriage return was written into the json");
        }

        void TestStringOutput(StringTest obj)
        {
            var neuroJson = NeuroJsonWriter.Shared.Write(obj);
            var copy = NeuroJsonReader.Shared.Read<StringTest>(neuroJson);
            Console.WriteLine(copy.String);
            Assert.That(copy.String, Is.EqualTo(obj.String));
            
            var referenceJson = JsonConvert.SerializeObject(obj, Formatting.Indented);
            neuroJson = neuroJson.Replace(NeuroJsonWriter.SingleIndent, "  ");
            Assert.That(neuroJson, Is.EqualTo(referenceJson));
        }

        void Test(UberTestClass testObj, NeuroReferences references = null)
        {
            var json = NeuroJsonWriter.Shared.Write(testObj);
            Console.WriteLine(json);
            var reader = new NeuroJsonReader();
            var result = reader.Read<UberTestClass>(json);
            UberTestClass.TestAllValuesMatch(testObj, result);
        }
        
        
        
        [Test]
        public void FloatWriter()
        {
            Assert.That(new StringBuilder().AppendNum(0f).ToString(), Is.EqualTo("0"));
            Assert.That(new StringBuilder().AppendNum(123f).ToString(), Is.EqualTo("123"));
            Assert.That(new StringBuilder().AppendNum(0.1f).ToString(), Is.EqualTo("0.1"));
            Assert.That(new StringBuilder().AppendNum(0.0001f).ToString(), Is.EqualTo("0.0001"));
            Assert.That(new StringBuilder().AppendNum(0.00001f).ToString(), Is.EqualTo("0.00001"));
            Assert.That(new StringBuilder().AppendNum(0.0002f).ToString(), Is.EqualTo("0.0002"));
            Assert.That(new StringBuilder().AppendNum(0.00002f).ToString(), Is.EqualTo("0.00002"));
            Assert.That(new StringBuilder().AppendNum(0.123f).ToString(), Is.EqualTo("0.123"));
            Assert.That(new StringBuilder().AppendNum(0.0123f).ToString(), Is.EqualTo("0.0123"));
            Assert.That(new StringBuilder().AppendNum(0.00123f).ToString(), Is.EqualTo("0.00123"));
            Assert.That(new StringBuilder().AppendNum(123.456f).ToString(), Is.EqualTo("123.456"));
            // the fast path widens its decimal places as needed rather than truncating.
            Assert.That(new StringBuilder().AppendNum(0.000123456f).ToString(), Is.EqualTo("0.000123456"));
            Assert.That(new StringBuilder().AppendNum(1e-8f).ToString(), Is.EqualTo("0.00000001"));
            Assert.That(new StringBuilder().AppendNum(1e-9f).ToString(), Is.EqualTo("0.000000001"));
            // past the widest step it falls back to exact formatting.
            Assert.That(new StringBuilder().AppendNum(float.Epsilon).ToString(), Is.EqualTo("1E-45"));
            Assert.That(new StringBuilder().AppendNum(1e8f).ToString(), Is.EqualTo("100000000"));
            Assert.That(new StringBuilder().AppendNum(1e9f).ToString(), Is.EqualTo("1E+09"));
            Assert.That(new StringBuilder().AppendNum(1e10f).ToString(), Is.EqualTo("1E+10"));
        }
        
        [Test]
        public void FloatWriterRoundTripsRandomValues()
        {
            // the writer picks how many decimal places to use, so whatever it picks has to parse back to the
            // exact same value - including for the values it hands over to the framework's own formatting.
            var random = new Random(12345);
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < 200000; i++)
            {
                var value = BitConverter.Int32BitsToSingle((int)(uint)random.NextInt64(0, uint.MaxValue));
                if (!float.IsFinite(value))
                {
                    continue;
                }
                stringBuilder.Clear();
                stringBuilder.AppendNum(value);
                var written = stringBuilder.ToString();
                if (float.Parse(written, CultureInfo.InvariantCulture) != value)
                {
                    Assert.Fail($"{value:R} was written as '{written}'");
                }
            }
        }

        [Test]
        public void DoubleWriterRoundTripsRandomValues()
        {
            var random = new Random(12345);
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < 200000; i++)
            {
                var value = random.NextDouble() * Math.Pow(10, random.Next(-20, 20)) * (random.Next(2) == 0 ? 1 : -1);
                stringBuilder.Clear();
                stringBuilder.AppendNum(value);
                var written = stringBuilder.ToString();
                if (double.Parse(written, CultureInfo.InvariantCulture) != value)
                {
                    Assert.Fail($"{value:R} was written as '{written}'");
                }
            }
        }

        [Test]
        public void DecimalsThatRoundUpToAWholeCarryOver()
        {
            // exact mode just widens until it fits, no rounding up needed.
            Assert.That(new StringBuilder().AppendNum(0.999999f).ToString(), Is.EqualTo("0.999999"));
            // the fixed places of presentation mode do round up, and that has to carry into the whole part.
            Assert.That(new StringBuilder().AppendNum(0.999999f, 2).ToString(), Is.EqualTo("1"));
            Assert.That(new StringBuilder().AppendNum(0.999999f, 2, 2).ToString(), Is.EqualTo("1.00"));
        }

        [Test]
        public void DoubleWrite()
        {
            Assert.That(new StringBuilder().AppendNum(0).ToString(), Is.EqualTo("0"));
            Assert.That(new StringBuilder().AppendNum(123).ToString(), Is.EqualTo("123"));
            Assert.That(new StringBuilder().AppendNum(0.1).ToString(), Is.EqualTo("0.1"));
            Assert.That(new StringBuilder().AppendNum(0.00001).ToString(), Is.EqualTo("0.00001"));
            Assert.That(new StringBuilder().AppendNum(0.000001).ToString(), Is.EqualTo("0.000001"));
            Assert.That(new StringBuilder().AppendNum(0.00002).ToString(), Is.EqualTo("0.00002"));
            Assert.That(new StringBuilder().AppendNum(0.000002).ToString(), Is.EqualTo("0.000002"));
            // the fast path widens its decimal places as needed rather than truncating.
            Assert.That(new StringBuilder().AppendNum(0.0000002).ToString(), Is.EqualTo("0.0000002"));
            Assert.That(new StringBuilder().AppendNum(1E-15).ToString(), Is.EqualTo("0.000000000000001"));
            // past the widest step it falls back to exact formatting.
            Assert.That(new StringBuilder().AppendNum(1E-300).ToString(), Is.EqualTo("1E-300"));
            Assert.That(new StringBuilder().AppendNum(0.123).ToString(), Is.EqualTo("0.123"));
            Assert.That(new StringBuilder().AppendNum(0.0123).ToString(), Is.EqualTo("0.0123"));
            Assert.That(new StringBuilder().AppendNum(0.00123).ToString(), Is.EqualTo("0.00123"));
            Assert.That(new StringBuilder().AppendNum(10000.00123).ToString(), Is.EqualTo("10000.00123"));
            Assert.That(new StringBuilder().AppendNum(27788615228878552).ToString(), Is.EqualTo("27788615228878552"));
            Assert.That(new StringBuilder().AppendNum(2.7788615228878553E+17).ToString(), Is.EqualTo("277886152288785536"));
            Assert.That(new StringBuilder().AppendNum(2.7788615228878553E+18).ToString(), Is.EqualTo("2.778861522887855E+18"));
            Assert.That(new StringBuilder().AppendNum(2.7788615228878553E+19).ToString(), Is.EqualTo("2.7788615228878553E+19"));
        }
    }
}