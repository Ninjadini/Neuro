using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    public class UberTestClassTests
    {
        [SetUp]
        public void set()
        {
            UberTestClass.RegisterAll();
        }

        [Test]
        public void TestUberClass()
        {
            var refs = new NeuroReferences();
            var testObj = UberTestClass.CreateTestClass(refs);

            TestClone(testObj, refs);
        }
        
        [Test]
        public void TestBaseClass()
        {
            var testObj = new UberTestClass()
            {
                BaseClassObj = new BaseTestClass1()
            };
            TestClone(testObj);
        }
        
        [Test]
        public void TestBaseClassList()
        {
            var testObj = new UberTestClass()
            {
                ListBaseClasses = new List<BaseTestClass1>()
                {
                    new BaseTestClass1()
                    {
                        Id = 1,
                        Name = "one"
                    }
                    , new SubTestClass1()
                    {
                        Id = 2,
                        Name = "two",
                        NumValue = 22,
                        Value = "two2"
                    },
                    new BaseTestClass1()
                    {},
                    new BaseTestClass1()
                    {
                        Id = 6443
                    }
                    , new SubTestClass1()
                    {
                    }
                    , new SubTestClass1()
                    {
                        Id = 22
                    },
                    null,
                    new SubTestClass1()
                    {
                        NumValue = 456
                    }
                }
            };
            testObj.BaseClassObj = new SubTestClass1()
            {
                Id = 123,
                Name = "one",
                NumValue = 456,
                Value = "one2"
            };
            TestClone(testObj);
        }
        
        [Test]
        public void TestSubClass()
        {
            TestClone(new UberTestClass()
            {
                BaseClassObj = new SubTestClass1()
                {
                    NumValue = 1,
                    Id = 1,
                },
                LastItem = 123
            });
            TestClone(new UberTestClass()
            {
                BaseClassObj = new SubTestClass1()
                {
                    Id = 1
                },
                LastItem = 123
            });
            TestClone(new UberTestClass()
            {
                BaseClassObj = new SubTestClass1()
                {
                    NumValue = 1,
                },
                LastItem = 123
            });
        }
        
        [Test]
        public void TestBlank()
        {
            var testObj = new UberTestClass();
            testObj.ListTexts.Clear();
            testObj.DictionaryIntObj.Clear();
            TestClone(testObj);
            var bytes = NeuroBytesWriter.Shared.Write(testObj);
            Assert.AreEqual(2, bytes.Length);
        }
        
        [Test]
        public void TestSkipping()
        {
            var refs = new NeuroReferences();
            var src = UberTestClass.CreateTestClass(refs);
            src.LastItem = 12345;
            var writer = NeuroBytesWriter.Shared;
            var bytes = writer.Write(src);
            Console.WriteLine(RawProtoReader.GetDebugString(bytes));
            
            var target = NeuroBytesReader.Shared.Read<UberTestClassWithJustLastItem>(writer.GetCurrentBytesChunk(), new ReaderOptions());
            Assert.AreEqual(src.LastItem, target.LastItem);
        }
        
        [Test]
        public void TestStruct()
        {
            TestClone(new UberTestClass()
            {
                Struct = new TestStruct()
                {
                    Id = 9,
                    Name = "abcd"
                }
            });
            TestClone(new UberTestClass()
            {
                Struct = new TestStruct()
                {
                    Id = 9,
                }
            });
            TestClone(new UberTestClass()
            {
                Struct = new TestStruct()
                {
                    Name = "asdfsf"
                }
            });
            TestClone(new UberTestClass()
            {
                Struct = new TestStruct()
            });
        }

        void TestClone(UberTestClass src, NeuroReferences references = null, bool testBinary = true, bool testJson = true, bool testSkipping = true)
        {
            if (testBinary)
            {
                var writer = NeuroBytesWriter.Shared;
                writer.Write(src);
                Console.WriteLine(writer.GetDebugString());
                Console.WriteLine(new NeuroBytesDebugWalker().Walk(writer.GetCurrentBytesChunk()));
                var target = NeuroBytesReader.Shared.Read<UberTestClass>(writer.GetCurrentBytesChunk());
                UberTestClass.TestAllValuesMatch(src, target);

                if (testSkipping)
                {
                    var skipTarget = NeuroBytesReader.Shared.Read<UberTestClassWithJustLastItem>(writer.GetCurrentBytesChunk(), new ReaderOptions());
                    Assert.AreEqual(src.LastItem, skipTarget.LastItem);
                }
            }

            if (testJson)
            {
                var jsonStr = NeuroJsonWriter.Shared.Write(src);
                Console.WriteLine("JSON:\n"+jsonStr);
                
                var token = new NeuroJsonTokenizer();
                token.Visit(jsonStr);
                token.PrintNodes(jsonStr);
                
                var target = NeuroJsonReader.Shared.Read<UberTestClass>(jsonStr);
                UberTestClass.TestAllValuesMatch(src, target);
            }
        }
        
        
        [Test]
        public void NullItemsInLists()
        {
            var testObj = new UberTestClass();
            testObj.ListTexts.Add("a");
            testObj.ListTexts.Add(null);
            testObj.ListTexts.Add("c");
            TestClone(testObj);
            
            testObj = new UberTestClass();
            testObj.ListClass = new List<TestChildClass>() { null , new TestChildClass()};
            TestClone(testObj);
        }
        
        [Test]
        public void NullItemsInDictionary()
        {
            var testObj = new UberTestClass();
            
            testObj.DictionaryIntStr = new Dictionary<int, string>()
            {
                {1, null},
                {2, "hello"},
            };
            TestClone(testObj);
            
            testObj = new UberTestClass();
            testObj.DictionaryIntObj.Add(1, new TestChildClass());
            testObj.DictionaryIntObj.Add(2, null);
            TestClone(testObj);
        }

        static void ExpectException(Action action, string exceptionMessage = null)
        {
            Exception caughtException = null;
            try
            {
                action();
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            if (caughtException == null)
            {
                Assert.Fail("Exception was not thrown");
            }
            else if (!string.IsNullOrEmpty(exceptionMessage) && !caughtException.Message.Contains(exceptionMessage))
            {
                Assert.Fail("Expected exception type was not thrown");
            }
        }
        
        [Test]
        public void EmptyListAndDictionaries()
        {
            var testObj = new UberTestClass();
            testObj.ListClass = new List<TestChildClass>();
            testObj.DictionaryIntStr = new Dictionary<int, string>();
            
            TestClone(testObj);
        }
    }
}