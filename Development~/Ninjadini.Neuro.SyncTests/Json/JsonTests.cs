using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Sync;
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

            Assert.AreEqual("{\n    \"-globalType\": \"11:ReferencableClass\",\n}", json);
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

            Assert.AreEqual("{\n    \"-globalType\": \"11:ReferencableClass\",\n    \"Name\": \"HELLO\"\n}", json);
            
            var copy = NeuroJsonReader.Shared.Read<object>(json, new ReaderOptions()) as ReferencableClass;
            Assert.AreEqual(obj.Name, copy.Name);
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

            var copy = NeuroJsonReader.Shared.Read<object>(json, new ReaderOptions()) as SubTestClass1;
            Assert.AreEqual(obj.Id, copy.Id);
            Assert.AreEqual(obj.Name, copy.Name);
            Assert.AreEqual(obj.NumValue, copy.NumValue);
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
            Assert.AreEqual(12.345f, copy.SingleNumber.Number);
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

        void TestStringOutput(StringTest obj)
        {
            var neuroJson = NeuroJsonWriter.Shared.Write(obj);
            var copy = NeuroJsonReader.Shared.Read<StringTest>(neuroJson);
            Console.WriteLine(copy.String);
            Assert.AreEqual(obj.String, copy.String);
            
            var referenceJson = JsonConvert.SerializeObject(obj, Formatting.Indented);
            neuroJson = neuroJson.Replace(NeuroJsonWriter.SingleIndent, "  ");
            Assert.AreEqual(referenceJson, neuroJson);
        }

        void Test(UberTestClass testObj, NeuroReferences references = null)
        {
            var json = NeuroJsonWriter.Shared.Write(testObj);
            Console.WriteLine(json);
            var reader = new NeuroJsonReader();
            var result = reader.Read<UberTestClass>(json);
            UberTestClass.TestAllValuesMatch(testObj, result);
        }
    }
}