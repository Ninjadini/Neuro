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
        public void TestBinaryWrite()
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
            TestClone(testObj);
            var bytes = NeuroBytesWriter.Shared.Write(testObj);
            Assert.AreEqual(2, bytes.Length);
        }
        
        [Test]
        public void TestSkipping()
        {
            var refs = new NeuroReferences();
            var src = UberTestClass.CreateTestClass(refs);
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
    }
}