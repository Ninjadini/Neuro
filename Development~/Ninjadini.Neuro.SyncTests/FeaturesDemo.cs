using System;
using System.Collections.Generic;
using System.Numerics;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro.SyncTests
{
    //
    // No Schema
    //
    public class MyObject
    {
        [Neuro(1)] public int MyInt;
        [Neuro(2)] public List<string> MyList;
        
    }








    
    
    
    
    
    
    
    
    //
    // Multi layer polymorphic
    //
    [Neuro(1)]
    public interface IMyInterface
    {
        int Id { get; }
    }
    
    [Neuro(2)]
    public class MyImplement : IMyInterface
    {
        int IMyInterface.Id => id;
        
        [Neuro(1)] public int id;
    }
    
    [Neuro(3)]
    public class MySubImplement : MyImplement
    {
        [Neuro(1)] public int someValue;
    }
    
    [Neuro(4)]
    public class MyOtherImplement : IMyInterface
    {
        int IMyInterface.Id => id;
        
        [Neuro(1)] public int id;
        [Neuro(2)] public int otherValue;
    }








    
    
    
    
    
    
    
    
    //
    // Default values
    //
    public class MyObjectWithDefaults
    {
        [Neuro(1)] public int TheNumber = 123;
        [Neuro(2)] public string TheString = "abcd";
    }








    
    
    
    
    
    
    
    
    //
    // Structs
    //
    public class MyClassObj
    {
        [Neuro(1)] public int ANumber;
        [Neuro(2)] public MyStruct Struct1;
        [Neuro(3)] public List<MyStruct> Structs;
    }
    public struct MyStruct
    {
        [Neuro(1)] public int SomeNumber;
        [Neuro(2)] public MyOtherStruct OtherStruct;
    }
    public struct MyOtherStruct
    {
        [Neuro(1)] public int SomeNumber;
    }








    
    
    
    
    
    
    
    
    //
    // Nullable primitives/structs
    //
    public class MyObjWithNullables
    {
        [Neuro(1)] public int? TheNumber;
        [Neuro(2)] public MyStruct? Struct;
    }








    
    
    
    
    
    
    
    
    //
    // Private fields / simulate readonly
    //
    public partial class MyPrivateObject
    {
        [Neuro(1)] private int _myValue;
        [Neuro(2)] private List<int> _myList;
        
        public int MyValue => _myValue;
        public IReadOnlyList<int> MyList => _myList;
    }








    
    
    
    
    
    
    
    
    //
    // Deprecating tags
    //
    public class MyObjectWithOldFields
    {
        [ReservedNeuroTag(1), ReservedNeuroTag(2)]
        
        [Neuro(3)] public int MyValue;
    }







    
    
    
    
    
    
    
    
    //
    // References
    //
    [NeuroGlobalType(123)]
    public partial class MyReferencableObj : IReferencable
    {
        [Neuro(2)] string Name;
        
        [Neuro(3)] string MyValues;

        public uint RefId { get; set; }
        public string RefName { get; set; }
    }

    public partial class MyObjectWithReferences
    {
        [Neuro(1)] Reference<MyReferencableObj> AReference;
        [Neuro(2)] List<Reference<MyReferencableObj>> SomeReferences;

        void Test()
        {

            NeuroReferences refs = null;
            
            MyReferencableObj myObj = AReference.GetValue(refs);
            foreach (var obj in SomeReferences)
            {
                var myObj2 = obj.GetValue(refs);
            }
        }
    }







    
    
    
    
    
    
    
    
    //
    // References can be on demand deserialisation
    //
    public partial class OnDemandReferences : INeuroReferencedItemLoader
    {
        private NeuroReferences neuroReferences;
        
        void Test()
        {
            neuroReferences = new NeuroReferences();
            
            neuroReferences.GetTable<MyReferencableObj>().Register(123, this);

            var myObj123 = neuroReferences.Get<MyReferencableObj>(123u);
        }

        IReferencable INeuroReferencedItemLoader.Load(uint refId)
        {
            var someBytes = new byte[0];
            return (MyReferencableObj)NeuroBytesReader.Shared.ReadGlobalTyped(someBytes, default);
        }

        string INeuroReferencedItemLoader.GetRefName(uint refId)
        {
            return "";
        }
    }







    
    
    
    
    
    
    
    
    //
    // Third-party types
    //
    public partial class MyObjectWithThirdPartyValues
    {
        [Neuro(1)] private Vector3 myVector3;
    }

    public struct MyCustomRegisters : INeuroCustomTypesRegistryHook
    {
        public void Register()
        {
            NeuroSyncTypes.Register((INeuroSync neuro, ref Vector3 value) =>
            {
                neuro.Sync(1, "x", ref value.X);
                neuro.Sync(2, "y", ref value.Y);
                neuro.Sync(3, "z", ref value.Z);
            });
            // ...
        }
    }






    
    
    
    
    
    
    
    
    //
    // Pooling
    //
    public class MyPoolableObj : INeuroPoolable
    {
        [Neuro(1)] public MyPoolableObj ChildPoolable;
        [Neuro(2)] public List<MyPoolableObj> PoolableChildren;

        public static void Test(NeuroPoolCollector poolCollector, INeuroObjectPool pool, MyPoolableObj poolableObj)
        {
            poolCollector.ReturnAllToPool(poolableObj, pool);
            // auto returns ChildPoolable and PoolableChildren and all children inside it to pool automatically.
        }
    }







    
    
    
    
    
    
    
    
    //
    // Reusable objects (combined with pooling)
    //
    public class MyObj : INeuroPoolable
    {
        [Neuro(1)] public int MyInt;
        [Neuro(2)] public int MyOtherInt;
        [Neuro(3)] public MyObj Child;
        
        public static void Test()
        {
            var srcObj = new MyObj()
            {
                MyInt = 123,
                Child = new MyObj()
                {
                    MyInt = 234
                }
            };
            var bytes = NeuroBytesWriter.Shared.Write(srcObj).ToArray();

            srcObj.MyInt = 0;
            srcObj.MyOtherInt = 234;
            NeuroBytesReader.Shared.Read(bytes, ref srcObj, new ReaderOptions());
            // srcObj.MyInt == 123
            // srcObj.MyOtherInt == 0
            // srcObj is same instance
            // srcObj.Child not changed or newed up.
            
            
            // pooling example:
            
            var pool = new NeuroPoolCollector.BasicPool();
            NeuroPoolCollector.Shared.ReturnAllToPool(srcObj, pool);
            var copiedObj = NeuroBytesReader.Shared.Read<MyObj>(bytes, new ReaderOptions(objectPool:pool));
            // copiedObj - used pooling
            // copiedObj.Child used pooling.
            // no allocations from reading above ^
        }
    }







    
    
    
    
    
    
    
    
    //
    // Binary/JSON serialise/deserialise
    //
    public class MyObjectToSerialise
    {
        [Neuro(1)] public int MyInt;
        [Neuro(2)] public List<string> MyList;

        
        public static void TestBytes()
        {
            var srcObj = new MyObjectToSerialise()
            {
                MyInt = 123,
                MyList = new List<string>() { "h", "e", "l", "l", "o" }
            };

            var bytes = NeuroBytesWriter.Shared.Write(srcObj).ToArray();

            var copiedObj = NeuroBytesReader.Shared.Read<MyObjectToSerialise>(bytes, new ReaderOptions());
            // srcObj == copiedObj
        }
        
        public static void TestJSON()
        {
            var srcObj = new MyObjectToSerialise()
            {
                MyInt = 123,
                MyList = new List<string>() { "h", "e", "l", "l", "o" }
            };

            var jsonString = NeuroJsonWriter.Shared.Write(srcObj);

            var copiedObj = NeuroJsonReader.Shared.Read<MyObjectToSerialise>(jsonString, new ReaderOptions());
            // srcObj == copiedObj
        }
    }
    
}