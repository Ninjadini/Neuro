using Ninjadini.Neuro;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    public partial class SubClassTests
    {
        [SetUp]
        public void SetUp()
        {
            RegisterTypes();
        }

        [Test]
        public void EmptyBaseClass_HavingNull()
        {
            TestEmptyBaseClassWith(null);
        }

        [Test]
        public void EmptyBaseClass_HavingBaseObj()
        {
            TestEmptyBaseClassWith(new EmptyBaseClass());
        }

        [Test]
        public void EmptyBaseClass_HavingEmptySubClass()
        {
            TestEmptyBaseClassWith(new EmptySubClassOf_EmptyBaseClass());
        }

        [Test]
        public void EmptyBaseClass_HavingSubClassButNoData()
        {
            TestEmptyBaseClassWith(new SubClassOf_EmptyBaseClass());
        }

        [Test]
        public void EmptyBaseClass_HavingSubClassWithData()
        {
            var result = TestEmptyBaseClassWith(new SubClassOf_EmptyBaseClass()
            {
                Id = 1234
            }) as SubClassOf_EmptyBaseClass;
            Assert.AreEqual(1234, result.Id);
        }

        EmptyBaseClass TestEmptyBaseClassWith(EmptyBaseClass obj)
        {
            var testObj = new ClassHoldingEmptyBaseClass();
            testObj.Before = 123;
            testObj.EmptyBaseClass = obj;
            testObj.After = 456;
            var result = TestUtils.CloneViaBinary(testObj, true);
            Assert.AreEqual(testObj.Before, result.Before);
            Assert.AreEqual(obj?.GetType(), result.EmptyBaseClass?.GetType());
            Assert.AreEqual(testObj.After, result.After);
            return result.EmptyBaseClass;
        }


        [Test]
        public void BaseClass_HavingNull()
        {
            TestBaseClassWith(null);
        }
        
        [Test]
        public void BaseClass_HavingBaseObj()
        {
            TestBaseClassWith(new BaseClass());
        }

        [Test]
        public void BaseClass_HavingEmptySubClass()
        {
            TestBaseClassWith(new EmptySubClass());
        }

        [Test]
        public void BaseClass_HavingSubClassButNoData()
        {
            TestBaseClassWith(new SubClass());
        }

        [Test]
        public void BaseClass_SubClassButBaseDataOnly()
        {
            var result = TestBaseClassWith(new SubClass()
            {
                Id = 123
            }) as SubClass;
            Assert.AreEqual(123, result.Id);
            Assert.AreEqual(0, result.SubId);
        }

        [Test]
        public void BaseClass_SubClassButSubDataOnly()
        {
            var result = TestBaseClassWith(new SubClass()
            {
                SubId = 123
            }) as SubClass;
            Assert.AreEqual(123, result.SubId);
            Assert.AreEqual(0, result.Id);
        }

        [Test]
        public void BaseClass_SubClassWithBothData()
        {
            var result = TestBaseClassWith(new SubClass()
            {
                Id = 1234,
                SubId = 2345
            }) as SubClass;
            Assert.AreEqual(1234, result.Id);
            Assert.AreEqual(2345, result.SubId);
        }

        [Test]
        public void BaseClass_AndSubSubClass()
        {
            var result = TestBaseClassWith(new SubSubClass()
            {
                Id = 11,
                SubId = 22,
                SubSubId = 33
            }) as SubSubClass;
            Assert.AreEqual(11, result.Id);
            Assert.AreEqual(22, result.SubId);
            Assert.AreEqual(33, result.SubSubId);
        }
        
        BaseClass TestBaseClassWith(BaseClass obj, bool testSkipping = true)
        {
            var testObj = new ClassHoldingBaseClass();
            testObj.Before = 123;
            testObj.BaseClass = obj;
            testObj.After = 456;
            var result = TestUtils.CloneViaBinary(testObj, out var bytes, true);
            Assert.AreEqual(testObj.Before, result.Before);
            Assert.AreEqual(obj?.GetType(), result.BaseClass?.GetType());
            Assert.AreEqual(testObj.After, result.After);

            if (testSkipping)
            {
                var skippedObj = NeuroBytesReader.Shared.Read<ClassForSkipping>(bytes, new ReaderOptions());
                Assert.AreEqual(testObj.After, skippedObj.After);
            }
            return result.BaseClass;
        }

        [Test]
        public void Interface_Null()
        {
            TestInterfaceClassWith(null);
        }

        [Test]
        public void Interface_EmptyClass()
        {
            TestInterfaceClassWith(new EmptyClassInterface());
        }

        [Test]
        public void Interface_SubClassWithNoData()
        {
            var result = TestInterfaceClassWith(new ClassInterface()) as ClassInterface;
            Assert.AreEqual(0, result.SubId);
        }

        [Test]
        public void Interface_SubClassWithData()
        {
            var result = TestInterfaceClassWith(new ClassInterface()
            {
                SubId = 1234
            }) as ClassInterface;
            Assert.AreEqual(1234, result.SubId);
        }

        [Test]
        public void SubClassReadWriteDirectly()
        {
            var srcObj = new SubClass()
            {
                Id = 123,
                SubId = 234
            };
            var resultObj = TestUtils.CloneViaBinary(srcObj, true);
            Assert.AreEqual(srcObj.GetType(), resultObj.GetType());
            Assert.AreEqual(srcObj.Id, resultObj.Id);
            Assert.AreEqual(srcObj.SubId, resultObj.SubId);
        }
        
        IInterface TestInterfaceClassWith(IInterface obj, bool testSkipping = true)
        {
            var testObj = new ClassHoldingInterface();
            testObj.Before = 123;
            testObj.Interface = obj;
            testObj.After = 456;
            var result = TestUtils.CloneViaBinary(testObj, out var bytes, true);
            Assert.AreEqual(testObj.Before, result.Before);
            Assert.AreEqual(obj?.GetType(), result.Interface?.GetType());
            Assert.AreEqual(testObj.After, result.After);

            if (testSkipping)
            {
                var skippedObj = NeuroBytesReader.Shared.Read<ClassForSkipping>(bytes, new ReaderOptions());
                Assert.AreEqual(testObj.After, skippedObj.After);
            }
            return result.Interface;
        }

        void RegisterTypes()
        {
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref ClassHoldingEmptyBaseClass value)
            {
                value ??= new ClassHoldingEmptyBaseClass();
                neuro.Sync(1, nameof(value.Before), ref value.Before, default);
                neuro.Sync(2, nameof(value.EmptyBaseClass), ref value.EmptyBaseClass);
                neuro.Sync(3, nameof(value.After), ref value.After, default);
            });
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref EmptyBaseClass value)
            {
                value ??= new EmptyBaseClass();
            });
            NeuroSyncTypes.RegisterSubClass<EmptyBaseClass, EmptySubClassOf_EmptyBaseClass>(2, delegate(INeuroSync neuro, ref EmptySubClassOf_EmptyBaseClass value)
            {
                value ??= new EmptySubClassOf_EmptyBaseClass();
            });
            NeuroSyncTypes.RegisterSubClass<EmptyBaseClass, SubClassOf_EmptyBaseClass>(3, delegate(INeuroSync neuro, ref SubClassOf_EmptyBaseClass value)
            {
                value ??= new SubClassOf_EmptyBaseClass();
                neuro.Sync(1, nameof(value.Id), ref value.Id, default);
            });

            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref ClassHoldingBaseClass value)
            {
                value ??= new ClassHoldingBaseClass();
                neuro.Sync(1, nameof(value.Before), ref value.Before, default);
                neuro.Sync(2, nameof(value.BaseClass), ref value.BaseClass);
                neuro.Sync(3, nameof(value.After), ref value.After, default);
            });
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref BaseClass value)
            {
                value ??= new BaseClass();
                neuro.Sync(1, nameof(value.Id), ref value.Id, default);
            });
            NeuroSyncTypes.RegisterSubClass<BaseClass, EmptySubClass>(1, delegate(INeuroSync neuro, ref EmptySubClass value)
            {
                value ??= new EmptySubClass();
            });
            NeuroSyncTypes.RegisterSubClass<BaseClass, SubClass>(2, delegate(INeuroSync neuro, ref SubClass value)
            {
                value ??= new SubClass();
                neuro.Sync(1, nameof(value.SubId), ref value.SubId, default);
            });
            NeuroSyncTypes.RegisterSubClass<BaseClass, SubClass, SubSubClass>(3, delegate(INeuroSync neuro, ref SubSubClass value)
            {
                value ??= new SubSubClass();
                neuro.Sync(1, nameof(value.SubSubId), ref value.SubSubId, default);
            });
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref ClassForSkipping value)
            {
                value ??= new ClassForSkipping();
                neuro.Sync(3, nameof(value.After), ref value.After, default);
            });
            
            
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref ClassHoldingInterface value)
            {
                value ??= new ClassHoldingInterface();
                neuro.Sync(1, nameof(value.Before), ref value.Before, default);
                neuro.Sync(2, nameof(value.Interface), ref value.Interface);
                neuro.Sync(3, nameof(value.After), ref value.After, default);
            });
            NeuroSyncTypes.RegisterSubClass<IInterface, EmptyClassInterface>(1, delegate(INeuroSync neuro, ref EmptyClassInterface value)
            {
                value ??= new EmptyClassInterface();
            });
            NeuroSyncTypes.RegisterSubClass<IInterface, ClassInterface>(2, delegate(INeuroSync neuro, ref ClassInterface value)
            {
                value ??= new ClassInterface();
                neuro.Sync(1, nameof(value.SubId), ref value.SubId, default);
            });
        }
        public partial class ClassHoldingEmptyBaseClass
        {
            public int Before;
            public EmptyBaseClass EmptyBaseClass;
            public int After;
        }
    
        public partial class EmptyBaseClass
        {
        }
    
        public partial class EmptySubClassOf_EmptyBaseClass : EmptyBaseClass
        {
        }
        public partial class SubClassOf_EmptyBaseClass : EmptyBaseClass
        {
            public int Id;
        }
        
        public partial class ClassHoldingBaseClass
        {
            public int Before;
            public BaseClass BaseClass;
            public int After;
        }
    
        public partial class BaseClass
        {
            public uint Id;
        }
        public partial class EmptySubClass : BaseClass
        {
        }
        public partial class SubClass : BaseClass
        {
            public uint SubId;
        }
        public partial class SubSubClass : SubClass
        {
            public uint SubSubId;
        }
        
        public partial class ClassForSkipping
        {
            public int After;
        }
        
        
        
        public partial class ClassHoldingInterface
        {
            public int Before;
            public IInterface Interface;
            public int After;
        }
        public interface IInterface
        {
            
        }

        public partial class EmptyClassInterface : IInterface
        {
        }
        public partial class ClassInterface : IInterface
        {
            public int SubId;
        }
    }
}