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
                    }
                    , new SubTestClass1()
                    {
                        NumValue = 456
                    }
                }
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

        void TestClone(UberTestClass src, NeuroReferences references = null, bool testBinary = true, bool testJson = true)
        {
            if (testBinary)
            {
                var writer = NeuroBytesWriter.Shared;
                writer.Write(src);
                Console.WriteLine(writer.GetDebugString());
                Console.WriteLine(new NeuroBytesDebugWalker().Walk(writer.GetCurrentBytesChunk()));
                var target = NeuroBytesReader.Shared.Read<UberTestClass>(writer.GetCurrentBytesChunk());
                UberTestClass.TestAllValuesMatch(src, target);
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
        public void ErrorOnNullItemsInLists()
        {
            var testObj = new UberTestClass();
            testObj.ListTexts.Add(null);
            
            const string errMsg = "Null list item is not supported";
            ExpectException(() =>
            {
                NeuroBytesWriter.Shared.Write(testObj);
            }, errMsg);
            ExpectException(() =>
            {
                NeuroJsonWriter.Shared.Write(testObj);
            }, errMsg);
            
            testObj = new UberTestClass();
            testObj.ListClass = new List<TestChildClass>() { null };
            ExpectException(() =>
            {
                NeuroBytesWriter.Shared.Write(testObj);
            }, errMsg);
            ExpectException(() =>
            {
                NeuroJsonWriter.Shared.Write(testObj);
            }, errMsg);
        }
        
        [Test]
        public void ErrorOnNullItemsInDictionary()
        {
            
            var testObj = new UberTestClass();
            
            testObj.DictionaryIntStr = new Dictionary<int, string>()
            {
                {2, null},
            };
            
            const string errMsg = "Null dictionary item is not supported";
            
            ExpectException(() =>
            {
                NeuroBytesWriter.Shared.Write(testObj);
            }, errMsg);
            ExpectException(() =>
            {
                NeuroJsonWriter.Shared.Write(testObj);
            }, errMsg);
            
            testObj = new UberTestClass();
            testObj.DictionaryIntObj.Add(1, null);
            ExpectException(() =>
            {
                NeuroBytesWriter.Shared.Write(testObj);
            }, errMsg);
            ExpectException(() =>
            {
                NeuroJsonWriter.Shared.Write(testObj);
            }, errMsg);
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
        
        [Test]
        public void WIP()
        {
            var testObj = new UberTestClass();
            
            testObj.DictionaryIntStr = new Dictionary<int, string>()
            {
                {1, "a"},
                {2, "b"},
                {3, "c"},
            };
            /*
            testObj.DictionaryRefObj = new Dictionary<Reference<ReferencableClass>, BaseTestClass1>()
            {
                {
                    new Reference<ReferencableClass>() { RefId = 1 }, new SubTestClass1()
                    {
                        NumValue = 123,
                        Value = "Value1",
                        Name = "Name1"
                    }
                }
                ,
                {
                new Reference<ReferencableClass>() { RefId = 2 }, new SubTestClass1()
                    {
                        NumValue = 234,
                        Value = "Value2",
                        Name = "Name2"
                    }
                }
            };

            testObj.DictionaryStringObj = new Dictionary<string, BaseTestClass1>()
            {
                { "1", new BaseTestClass1() { Id = 1 } },
                { "2", new SubTestClass1() { Id = 1, Value = "1" } }
            };
            
            testObj.DictionaryIntObj.Add(1, new TestChildClass() { Id = 1 });
            testObj.DictionaryIntObj.Add(2, new TestChildClass() { Id = 2 });
            */
            //TestClone(testObj, testJson:false);
            var references = new NeuroReferences();
            var ref1 = new ReferencableClass()
            {
                RefId = 1,
                Name = "ref1",
                RefName = "ref1",
            };
            references.Register(ref1);
            var ref2 = new ReferencableClass()
            {
                RefId = 2,
                Name = "ref2",
                RefName = "ref2",
            };
            references.Register(ref2);

            //var jsonStr = NeuroJsonWriter.Shared.Write(testObj, references);
            //Console.WriteLine("JSON:\n"+jsonStr);

            TestClone(testObj);
        }
    }
}