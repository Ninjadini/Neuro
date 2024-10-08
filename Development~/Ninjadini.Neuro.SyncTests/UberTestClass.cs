using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using _NeuroSync = Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro.SyncTests
{
    public partial class UberTestClass
    {
        [Neuro(1)] public int Id;
        [Neuro(2)] public float Float;
        [Neuro(3)] public string Name;
        [Neuro(4)] public DateTime Date;
        [Neuro(5)] public TimeSpan TimeSpan;

        [Neuro(11)] public TestEnum1 Enum;
        [Neuro(12)] public TestFlagEnum1 FlagEnum;
        
        [Neuro(20)] public TestChildClass ClassObj;
        [Neuro(21)] public BaseTestClass1 BaseClassObj;
        [Neuro(22)] public ITestInterface Interface;
        
        [Neuro(50)] public TestStruct Struct;
        [Neuro(51)] public Reference<ReferencableClass> Referencable;
        [Neuro(52)] public SingleNumberStruct SingleNumber;
        
        [Neuro(200)] public List<int> ListInt;
        [Neuro(201)] public List<TestEnum1> ListEnum;
        [Neuro(202)] public List<TestChildClass> ListClass;
        [Neuro(203)] public List<TestStruct> ListStruct;
        [Neuro(204)] public readonly List<string> ListTexts = new List<string>();
        [Neuro(205)] public List<BaseTestClass1> ListBaseClasses;
        
        [Neuro(210)] public Dictionary<int, string> DictionaryIntStr;
        [Neuro(211)] public readonly Dictionary<int, TestChildClass> DictionaryIntObj = new Dictionary<int, TestChildClass>();
        [Neuro(212)] public Dictionary<string, BaseTestClass1> DictionaryStringObj;
        [Neuro(213)] public Dictionary<Reference<ReferencableClass>, BaseTestClass1> DictionaryRefObj;
        
        [Neuro(300)] public int? NullableId;
        [Neuro(301)] public TestEnum1? NullableEnum;
        [Neuro(302)] public DateTime? NullableDate;
        [Neuro(303)] public TestStruct? NullableStr;
        [Neuro(10000)] public int LastItem;
        
        public string NotSerializedString;


        private static bool _registered;
        public static void  RegisterAll()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            _NeuroSync.NeuroSyncTypes.Register<UberTestClass>(UberTestClass.Sync);
            _NeuroSync.NeuroSyncTypes.RegisterSubClass<ITestInterface, TestInterfaceImp1>(2, delegate(_NeuroSync.INeuroSync neuro, ref TestInterfaceImp1 value)
            {
                value ??= new TestInterfaceImp1();
                neuro.Sync(1, nameof(value.NumValue), ref value.NumValue, default);
                neuro.Sync(2, nameof(value.Value), ref value.Value, default);
            });
            
            _NeuroSync.NeuroSyncTypes.Register(delegate(_NeuroSync.INeuroSync neuro, ref UberTestClassWithJustLastItem value)
            {
                value ??= new UberTestClassWithJustLastItem();
                neuro.Sync(10000, nameof(value.LastItem), ref value.LastItem, 0);
            });
            _NeuroSync.NeuroSyncTypes.Register(delegate(_NeuroSync.INeuroSync neuro, ref TestChildClass value)
            {
                value ??= new TestChildClass();
                neuro.Sync(1, nameof(value.Id), ref value.Id, 0);
                neuro.Sync(2, nameof(value.Name), ref value.Name, defaultValue:null);
            });
            _NeuroSync.NeuroSyncTypes.Register(delegate(_NeuroSync.INeuroSync neuro, ref BaseTestClass1 value)
            {
                value ??= new BaseTestClass1();
                neuro.Sync(1, nameof(value.Id), ref value.Id, 0);
                neuro.Sync(2, nameof(value.Name), ref value.Name, null);
            }, globalTypeId:10);
            _NeuroSync.NeuroSyncTypes.RegisterSubClass<BaseTestClass1, SubTestClass1>(2, delegate(_NeuroSync.INeuroSync neuro, ref SubTestClass1 value)
            {
                value ??= new SubTestClass1();
                neuro.Sync(1, nameof(value.Value), ref value.Value, null);
                neuro.Sync(2, nameof(value.NumValue), ref value.NumValue, 0);
            });
            _NeuroSync.NeuroSyncTypes.Register(delegate(_NeuroSync.INeuroSync neuro, ref TestStruct value)
            {
                neuro.Sync(1, nameof(value.Id), ref value.Id, 0);
                neuro.Sync(2, nameof(value.Name), ref value.Name, defaultValue:null);
            });
            _NeuroSync.NeuroSyncTypes.Register(sizeType:_NeuroSync.FieldSizeType.VarInt, delegate(_NeuroSync.INeuroSync neuro, ref SingleNumberStruct value)
            {
                neuro.Sync(ref value.Number);
            });
            NeuroJsonSyncTypes.Register(sizeType:_NeuroSync.FieldSizeType.VarInt, delegate(_NeuroSync.INeuroSync neuro, ref SingleNumberStruct value)
            {
                var n = (int)(value.Number * 1000f);
                neuro.Sync(ref n);
                value.Number = n / 1000f;
            });
            _NeuroSync.NeuroSyncTypes.Register(delegate(_NeuroSync.INeuroSync neuro, ref StringTest value)
            {
                value ??= new StringTest();
                neuro.Sync(1, nameof(value.String), ref value.String);
            });
            _NeuroSync.NeuroSyncTypes.Register(delegate(_NeuroSync.INeuroSync neuro, ref ReferencableClass value)
            {
                value ??= new ReferencableClass();
                neuro.Sync(2, nameof(value.Name), ref value.Name, defaultValue:null);
            }, globalTypeId:11);
            
            _NeuroSync.NeuroSyncTypes.Register<Reference<ReferencableClass>>(_NeuroSync.FieldSizeType.VarInt, Reference<ReferencableClass>.Sync);
            _NeuroSync.NeuroSyncEnumTypes<TestEnum1>.Register((e) => (int)e, (i) => (TestEnum1)i);
            _NeuroSync.NeuroSyncEnumTypes<TestFlagEnum1>.Register((e) => (int)e, (i) => (TestFlagEnum1)i);
        }
        
        public static UberTestClass CreateTestClass(NeuroReferences references)
        {
            var ref1 = new ReferencableClass()
            {
                RefId = 123,
                Name = "ref1"
            };
            references.Register(ref1);
            var ref2 = new ReferencableClass()
            {
                RefId = 234,
                Name = "ref2"
            };
            references.Register(ref2);
            
            var result = new UberTestClass()
            {
                Id = 1234,
                Float = 456.78f,
                LastItem =  812,
                Name = "testing 123",
                Struct = new TestStruct()
                {
                    Id = 321,
                    Name = "str"
                },
                Date = new DateTime(2000, 4, 3, 2, 1, 9, 8, DateTimeKind.Utc),
                Enum = TestEnum1.A,
                FlagEnum = TestFlagEnum1.B | TestFlagEnum1.C,
                TimeSpan = TimeSpan.FromSeconds(123456),
                ListEnum = new List<TestEnum1> { TestEnum1.C , TestEnum1.B},
                ListStruct = new List<TestStruct>{new TestStruct(){ Id = 51}, new TestStruct(){ Id = 11, Name = "abcd"}},
                ListInt = new List<int>(){ 7, 6, 5, 3},
                ListClass = new List<TestChildClass>(){ new TestChildClass()
                {
                    Id = 123,
                    Name = "list1"
                }},
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
                },
                
                DictionaryIntStr = new Dictionary<int, string>()
                {
                    {1, "first"},
                    {2, "second"},
                    {3, "third"}
                },
                DictionaryStringObj = new Dictionary<string, BaseTestClass1>()
                {
                    {"first", new BaseTestClass1() { Id = 1, Name = "first's base"}},
                    {"second", new SubTestClass1() { Id = 2, NumValue = 2}},
                    {"third", new SubTestClass1() { Id = 3, Value = "VALUE"}}
                },
                DictionaryRefObj = new Dictionary<Reference<ReferencableClass>, BaseTestClass1>()
                {
                    {new Reference<ReferencableClass>() { RefId = 1 }, new BaseTestClass1()
                    {
                        Id = 1,
                        Name = "Name1"
                    }},
                    
                    {new Reference<ReferencableClass>() { RefId = 2 }, new SubTestClass1()
                    {
                        Id = 2,
                        NumValue = 234,
                        Value = "Value2",
                        Name = "Name3"
                    }}  
                },
                
                NullableId = 123,
                NullableDate = new DateTime(12340000),
                NullableStr = new TestStruct()
                {
                    Id = 77
                },
                BaseClassObj = new SubTestClass1()
                {
                    Id = 99,
                    Value = "hi"
                },
                Interface = new TestInterfaceImp1()
                {
                    NumValue = 88
                },
                ClassObj = new TestChildClass()
                {
                    Id = 9876766,
                    Name = "BaseClass1"
                },
                Referencable = ref1
            };
            result.ListTexts.Add("a");
            result.ListTexts.Add("b");
            result.ListTexts.Add("c");
            //result.ListTexts.Add(null);
            
            result.DictionaryIntObj.Add(1, new TestChildClass(){ Id = 1});
            result.DictionaryIntObj.Add(2, new TestChildClass(){ Id = 2, Name = "2"});
            //result.DictionaryIntObj.Add(3, null);

            return result;
        }
    }

    public partial class UberTestClassWithJustLastItem
    {
        [Neuro(10000)] public int LastItem;
    }

    public partial class TestChildClass
    {
        [Neuro(1)] public int Id;
        [Neuro(2)] public string Name;

    }
    
    [Neuro(1), NeuroGlobalType(10)]
    public partial class BaseTestClass1
    {
        [Neuro(1)] public int Id;
        [Neuro(2)] public string Name;
    }
    
    [Neuro(2)]
    public partial class SubTestClass1 : BaseTestClass1
    {
        [Neuro(1)] public int NumValue;
        [Neuro(2)] public string Value;
    }
    
    
    [Neuro(1)]
    public partial interface ITestInterface
    {
    }
    
    [Neuro(2)]
    public partial class TestInterfaceImp1 : ITestInterface
    {
        [Neuro(1)] public int NumValue;
        [Neuro(2)] public string Value;
    }
    
    public partial struct TestStruct : IEquatable<TestStruct>
    {
        [Neuro(1)] public int Id;
        [Neuro(2)] public string Name;

        public bool Equals(TestStruct other)
        {
            return Id == other.Id && Name == other.Name;
        }
    }
    
    public partial struct SingleNumberStruct : IEquatable<SingleNumberStruct>
    {
        [Neuro(1)] public float Number;

        public bool Equals(SingleNumberStruct other)
        {
            return Math.Abs(Number - other.Number) < 0.000001;
        }
    }
    
    [NeuroGlobalType(11)]
    public partial class ReferencableClass : IReferencable
    {
        [Neuro(2)] public string Name;
        
        public uint RefId { get; set; }
        public string RefName { get; set; }
    }

    public enum TestEnum1
    {
        A = 1,
        B = 2,
        C = 3,
    }
    [Flags]
    public enum TestFlagEnum1
    {
        None = 0,
        A = 1<<0,
        B = 1<<1,
        C = 1<<2,
    }
    
    public class StringTest
    {
        public string String;
    }
    
    public partial class UberTestClass
    {
        static void Sync(_NeuroSync.INeuroSync neuro, ref UberTestClass value)
        {
            value ??= neuro.GetPooled<UberTestClass>() ?? new UberTestClass();
            neuro.Sync(1, nameof(value.Id), ref value.Id, 0);
            neuro.Sync(2, nameof(value.Float), ref value.Float, default);
            neuro.Sync(3, nameof(value.Name), ref value.Name, defaultValue:null);
            neuro.Sync(4, nameof(value.Date), ref value.Date, default);
            neuro.Sync(5, nameof(value.TimeSpan), ref value.TimeSpan, default);
                
            neuro.SyncEnum(11, nameof(value.Enum), ref value.Enum, default);
            neuro.SyncEnum(12, nameof(value.FlagEnum), ref value.FlagEnum, default);

            neuro.Sync(20, nameof(ClassObj), ref value.ClassObj);
            neuro.Sync(21, nameof(BaseClassObj), ref value.BaseClassObj);
            //neuro.Sync(22, nameof(Interface), ref value.Interface);
            
            neuro.Sync(50, nameof(Struct), ref value.Struct, default);
            neuro.Sync(51, nameof(Referencable), ref value.Referencable, default);
            neuro.Sync(52, nameof(SingleNumber), ref value.SingleNumber, default);
            
            neuro.Sync(200, nameof(ListInt), ref value.ListInt);
            neuro.Sync(201, nameof(ListEnum), ref value.ListEnum);
            neuro.Sync(202, nameof(ListClass), ref value.ListClass);
            neuro.Sync(203, nameof(ListStruct), ref value.ListStruct);
            neuro.Sync(204, nameof(ListTexts), value.ListTexts);
            neuro.Sync(205, nameof(ListBaseClasses), ref value.ListBaseClasses);
            
            neuro.Sync(210, nameof(DictionaryIntStr), ref value.DictionaryIntStr);
            neuro.Sync(211, nameof(DictionaryIntObj), value.DictionaryIntObj);
            neuro.Sync(212, nameof(DictionaryStringObj), ref value.DictionaryStringObj);
            neuro.Sync(213, nameof(DictionaryRefObj), ref value.DictionaryRefObj);
            
            neuro.Sync(300, nameof(NullableId), ref value.NullableId);
            neuro.Sync(301, nameof(NullableEnum), ref value.NullableEnum);
            neuro.Sync(302, nameof(NullableDate), ref value.NullableDate);
            neuro.Sync(303, nameof(NullableStr), ref value.NullableStr);
                    
            neuro.Sync(10000, nameof(value.LastItem), ref value.LastItem, 0);
        }

        public static void TestAllValuesMatch(UberTestClass a, UberTestClass b)
        {
            Assert.AreEqual(a.Id, b.Id);
            Assert.AreEqual(a.Float, b.Float);
            Assert.AreEqual(a.Name, b.Name);
            Assert.AreEqual(a.Date, b.Date);
            Assert.AreEqual(a.TimeSpan, b.TimeSpan);
            Assert.AreEqual(a.Enum, b.Enum);
            Assert.AreEqual(a.FlagEnum, b.FlagEnum);
            
            Assert.AreEqual(a.ClassObj?.Id, b.ClassObj?.Id);
            Assert.AreEqual(a.ClassObj?.Name, b.ClassObj?.Name);
            //Assert.AreEqual(a.Interface is TestInterfaceImp1 imp1 ? imp1.NumValue : -1, b.Interface is TestInterfaceImp1 imp2 ? imp2.NumValue : -1);
            
            
            Assert.AreEqual(a.Struct.Id, b.Struct.Id);
            Assert.AreEqual(a.Struct.Name, b.Struct.Name);
            
            Assert.AreEqual(a.ListInt?.Count, b.ListInt?.Count);
            if (a.ListInt?.Count > 0)
            {
                Assert.AreEqual(a.ListInt[0], b.ListInt[0]);
            }
            
            Assert.AreEqual(a.ListEnum?.Count, b.ListEnum?.Count);
            if (a.ListEnum?.Count > 0)
            {
                Assert.AreEqual(a.ListEnum[0], b.ListEnum[0]);
            }
            Assert.AreEqual(a.ListStruct?.Count, b.ListStruct?.Count);
            if (a.ListStruct?.Count > 0)
            {
                Assert.AreEqual(a.ListStruct[0], b.ListStruct[0]);
            }
            Assert.AreEqual(a.ListClass?.Count, b.ListClass?.Count);
            if (a.ListClass?.Count > 0)
            {
                for(var i = 0; i < a.ListClass.Count; i++)
                {
                    Assert.AreEqual(a.ListClass[i]?.Id, b.ListClass[i]?.Id);
                    Assert.AreEqual(a.ListClass[i]?.Name, b.ListClass[i]?.Name);
                }
            }

            if (a.ListBaseClasses?.Count > 0)
            {
                Assert.AreEqual(a.ListBaseClasses.Count, b.ListBaseClasses.Count);
                for (int i = 0, l = a.ListBaseClasses.Count; i < l; i++)
                {
                    var ai = a.ListBaseClasses[i];
                    var bi = b.ListBaseClasses[i];
                    Assert.AreEqual(ai != null, bi != null);
                    if (ai != null)
                    {
                        Assert.AreEqual(ai.GetType(), bi.GetType());
                        Assert.AreEqual(ai.Id, bi.Id);
                        Assert.AreEqual(ai.Name, bi.Name);
                        var ais = ai as SubTestClass1;
                        if (ais != null)
                        {
                            var bis = ai as SubTestClass1;
                            Assert.AreEqual(ais.NumValue, bis.NumValue);
                            Assert.AreEqual(ais.Value, bis.Value);
                        }
                    }
                }
            }

            Assert.AreEqual(a.Referencable.RefId, b.Referencable.RefId);
            Assert.AreEqual(a.SingleNumber.Number, b.SingleNumber.Number);
            
            if (a.DictionaryIntStr != null)
            {
                CollectionAssert.AreEquivalent(a.DictionaryIntStr.ToList(), b.DictionaryIntStr.ToList());
            }
            
            Assert.AreEqual(a.DictionaryIntObj.Count, b.DictionaryIntObj.Count);
            foreach (var kv in a.DictionaryIntObj)
            {
                if (b.DictionaryIntObj.TryGetValue(kv.Key, out var bValue))
                {
                    if (kv.Value != null)
                    {
                        Assert.AreEqual(kv.Value.Id, bValue.Id);
                        Assert.AreEqual(kv.Value.Name, bValue.Name);
                    }
                    else
                    {
                        Assert.IsNull(bValue);
                    }
                }
                else
                {
                    Assert.Fail($"Key missing, {kv.Key}");
                }
            }

            if (a.DictionaryStringObj != null)
            {
                Assert.AreEqual(a.DictionaryStringObj.Count, b.DictionaryStringObj.Count);
                foreach (var kv in a.DictionaryStringObj)
                {
                    if (b.DictionaryStringObj.TryGetValue(kv.Key, out var bValue))
                    {
                        TestSame(kv.Value, bValue);
                    }
                    else
                    {
                        Assert.Fail($"Key missing, {kv.Key}");
                    }
                }
            }
            
            if (a.DictionaryRefObj != null)
            {
                Assert.AreEqual(a.DictionaryRefObj.Count, b.DictionaryRefObj.Count);
                foreach (var kv in a.DictionaryRefObj)
                {
                    if (b.DictionaryRefObj.TryGetValue(kv.Key, out var bValue))
                    {
                        TestSame(kv.Value, bValue);
                    }
                    else
                    {
                        Assert.Fail($"Key missing, {kv.Key}");
                    }
                }
            }
            
            Assert.AreEqual(a.BaseClassObj?.GetType(), b.BaseClassObj?.GetType());
            if (a.BaseClassObj != null)
            {
                Assert.AreEqual(a.BaseClassObj.Id, b.BaseClassObj.Id);
                Assert.AreEqual(a.BaseClassObj.Name, b.BaseClassObj.Name);

                if (a.BaseClassObj is SubTestClass1 asub)
                {
                    var bsub = (SubTestClass1)b.BaseClassObj;
                    Assert.AreEqual(asub.NumValue, bsub.NumValue);
                    Assert.AreEqual(asub.Value, bsub.Value);
                }
            }
            
            Assert.AreEqual(a.NullableId, b.NullableId);
            Assert.AreEqual(a.NullableDate, b.NullableDate);
            Assert.AreEqual(a.NullableStr, b.NullableStr);
            Assert.AreEqual(a.NullableEnum, b.NullableEnum);
            
            Assert.AreEqual(a.LastItem, b.LastItem);
        }

        static void TestSame(BaseTestClass1 a, BaseTestClass1 b)
        {
            Assert.AreEqual(a.Id, b.Id);
            Assert.AreEqual(a.Name, b.Name);
            if (a is SubTestClass1 subTestClassA && b is SubTestClass1 subTestClassB)
            {
                Assert.AreEqual(subTestClassA.NumValue, subTestClassB.NumValue);
                Assert.AreEqual(subTestClassA.Value, subTestClassB.Value);
            }
            else if(a.GetType() != b.GetType())
            {
                Assert.Fail($"Not matching type {a} vs {b}");
            }
        }
    }
}