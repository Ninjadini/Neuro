using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests;

public class BinaryReadWriteTests
{
    NeuroBytesReader Reader => NeuroBytesReader.Shared;
    NeuroBytesWriter Writer => NeuroBytesWriter.Shared;
    
    void EnsureNotGlobal(ReadOnlySpan<byte> bytes)
    {
        Console.WriteLine(RawProtoReader.GetDebugString(bytes));
        var reader = new RawProtoReader();
        reader.Set(bytes.ToArray());
        var header = reader.ReadUint();
        Assert.AreNotEqual(1 << NeuroConstants.HeaderShift | NeuroConstants.VarInt, header);
    }
    void EnsureGlobal(ReadOnlySpan<byte> bytes, uint tag)
    {
        Console.WriteLine(RawProtoReader.GetDebugString(bytes));
        var reader = new RawProtoReader();
        reader.Set(bytes.ToArray());
        var header = reader.ReadUint();
        Assert.AreEqual(1 << NeuroConstants.HeaderShift | NeuroConstants.VarInt, header);
        var typeId = reader.ReadUint();
        Assert.AreEqual(tag, typeId);
    }

    byte[] GetDataForReading(ReadOnlySpan<byte> bytes)
    {
        return bytes.ToArray();
    }

    byte[] GetAnyTestData()
    {
        return new byte[] {1};
    }

    const uint NormalGlobalClassId = 106;
    const uint GlobalBaseClassId = 107;


    [NeuroGlobalType(108)]
    public class RefA : Referencable
    {
        [Neuro(1)] public string Value;
    }
    
    [NeuroGlobalType(109)]
    public class RefB : Referencable
    {
        [Neuro(1)] public string Value;
    }

    [Test]
    public void ReferencesReadWrite()
    {
        var bytes = Writer.WriteReferencesList(new IReferencable[]
        {
            new RefA(){ RefId = 1, RefName = "rA-1",Value = "rA-1-value"},
            new RefA(){ RefId = 2, RefName = "rA-2",Value = "rA-2-value"},
            new RefA(){ RefId = 3, RefName = "rA-3",Value = "rA-3-value"},
            new RefB(){ RefId = 1, RefName = "rB-1",Value = "rB-1-value"},
            new RefB(){ RefId = 2, RefName = "rB-2",Value = "rB-2-value"}
        }).ToArray();
        
        var references = new NeuroReferences();
        Reader.ReadReferencesListInto(references, bytes);
        var tableA = references.GetTable<RefA>();
        Assert.AreEqual(3, tableA.Count);
        AssertRefA(tableA.Get(1), 1);
        AssertRefA(tableA.Get(2), 2);
        AssertRefA(tableA.Get(3), 3);
        var tableB = references.GetTable<RefB>();
        Assert.AreEqual(2, tableB.Count);
        AssertRefB(tableB.Get(1), 1);
        AssertRefB(tableB.Get(2), 2);
    }

    void AssertRefA(RefA refA, uint num)
    {
        Assert.IsNotNull(refA);
        Assert.AreEqual(num, refA.RefId);
        Assert.AreEqual("rA-" + num, refA.RefName);
        Assert.AreEqual($"rA-{num}-value", refA.Value);
    }

    void AssertRefB(RefB refB, uint num)
    {
        Assert.IsNotNull(refB);
        Assert.AreEqual(num, refB.RefId);
        Assert.AreEqual("rB-" + num, refB.RefName);
        Assert.AreEqual($"rB-{num}-value", refB.Value);
    }

//
//
// CODE BELOW IS COPIED BETWEEN JsonReadWriteTests and BinaryReadWriteTests

    public class NormalClass
    {
        [Neuro(1)] public string Value;
    }

    [Test]
    public void ErrorIfWriteGenericIsObject()
    {
        var obj = new NormalClass();
        Exception exception = null;
        try
        {
            Writer.Write<object>(obj);
        }
        catch (Exception e)
        {
            exception = e;
        }
        Assert.IsTrue(exception?.Message.Contains("ambiguous"));
    }

    [Test]
    public void ErrorIfReadGenericIsObject()
    {
        Exception exception = null;
        try
        {
            Reader.Read<object>(GetAnyTestData());
        }
        catch (Exception e)
        {
            exception = e;
        }
        Assert.IsTrue(exception?.Message.Contains("ambiguous"));
    }
    
    [Test]
    public void NormalReadWrite()
    {
        var obj = new NormalClass()
        {
            Value = "123"
        };
        var data = GetDataForReading(Writer.Write(obj));
        EnsureNotGlobal(data);
        var copy = Reader.Read<NormalClass>(data);
        Assert.AreEqual("123", copy.Value);
        
        var copy2 = new NormalClass()
        {
            Value = "abc"
        };
        var copy2Instance = copy2;
        Reader.Read(data, ref copy2Instance);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
    }
    
    [Test]
    public void NormalReadWriteNonGeneric()
    {
        var obj = new NormalClass()
        {
            Value = "123"
        };
        var data = GetDataForReading(Writer.WriteObject(obj));
        EnsureNotGlobal(data);
        var copy = (NormalClass)Reader.ReadObject(data, typeof(NormalClass));
        Assert.AreEqual("123", copy.Value);
        
        var copy2 = new NormalClass()
        {
            Value = "abc"
        };
        var copy2Instance = copy2;
        Reader.Read(data, ref copy2Instance);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
    }
    
    public class NormalStruct
    {
        [Neuro(1)] public string Value;
    }
    
    [Test]
    public void StructReadWrite()
    {
        var obj = new NormalStruct()
        {
            Value = "123"
        };
        var data = GetDataForReading(Writer.Write(obj));
        EnsureNotGlobal(data);
        var copy = Reader.Read<NormalStruct>(data);
        Assert.AreEqual("123", copy.Value);
        
        var copy2 = new NormalStruct()
        {
            Value = "abc"
        };
        var copy2Instance = copy2;
        Reader.Read(data, ref copy2Instance);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
    }
    
    [Test]
    public void StructReadWriteNonGeneric()
    {
        var obj = new NormalStruct()
        {
            Value = "123"
        };
        var data = GetDataForReading(Writer.WriteObject(obj));
        EnsureNotGlobal(data);
        var copy = (NormalStruct)Reader.ReadObject(data, typeof(NormalStruct));
        Assert.AreEqual("123", copy.Value);
        
        var copy2 = new NormalStruct()
        {
            Value = "abc"
        };
        var copy2Instance = copy2;
        Reader.Read(data, ref copy2Instance);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
    }
    
    public class BaseClass
    {
        [Neuro(1)] public string Value;
    }
    
    
    [Test]
    public void BaseReadWrite()
    {
        var obj = new BaseClass()
        {
            Value = "123"
        };
        var data = GetDataForReading(Writer.Write(obj));
        EnsureNotGlobal(data);
        var copy = Reader.Read<BaseClass>(data);
        Assert.AreEqual("123", copy.Value);
        
        var copy2 = new BaseClass()
        {
            Value = "abc"
        };
        var copy2Instance = copy2;
        Reader.Read(data, ref copy2Instance);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
    }
    
    [Test]
    public void BaseReadWriteNonGeneric()
    {
        var obj = new BaseClass()
        {
            Value = "123"
        };
        var data = GetDataForReading(Writer.WriteObject(obj));
        EnsureNotGlobal(data);
        var copy = (BaseClass)Reader.ReadObject(data, typeof(BaseClass));
        Assert.AreEqual("123", copy.Value);
        
        var copy2 = new BaseClass()
        {
            Value = "abc"
        };
        var copy2Instance = copy2;
        Reader.Read(data, ref copy2Instance);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
    }
    
    
    [Neuro(2)]
    public class SubClass1 : BaseClass
    {
        [Neuro(1)] public string SubValue1;
    }

    [Test]
    public void SubClassReadWrite()
    {
        var obj = new SubClass1()
        {
            Value = "123",
            SubValue1 = "234",
        };
        var data = GetDataForReading(Writer.Write(obj));
        EnsureNotGlobal(data);
        var copy = (SubClass1)Reader.Read<BaseClass>(data);
        Assert.AreEqual("123", copy.Value);
        Assert.AreEqual("234", copy.SubValue1);
        
        var copy2 = new SubClass1()
        {
            Value = "abc",
            SubValue1 = "def"
        };
        var copy2Instance = copy2;
        Reader.Read(data, ref copy2Instance);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
        Assert.AreEqual("234", copy2.SubValue1);

        copy2.Value = copy2.SubValue1 = "xx";
        var copyBase = (BaseClass)copy2;
        Reader.Read(data, ref copyBase);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
        Assert.AreEqual("234", copy2.SubValue1);
        
        
        var copy3 = Reader.Read<SubClass1>(data);
        Assert.AreEqual("123", copy3.Value);
        Assert.AreEqual("234", copy3.SubValue1);
    }
    
    [Test]
    public void SubClassReadWriteNonGeneric()
    {
        var obj = new SubClass1()
        {
            Value = "123",
            SubValue1 = "234"
        };
        var data = GetDataForReading(Writer.WriteObject(obj));
        EnsureNotGlobal(data);
        var copy = (SubClass1)Reader.ReadObject(data, typeof(BaseClass));
        Assert.AreEqual("123", copy.Value);
        Assert.AreEqual("234", copy.SubValue1);
        
        var copy2 = new SubClass1()
        {
            Value = "abc",
            SubValue1 = "def"
        };
        var copy2Instance = (object)copy2;
        Reader.ReadObject(data, typeof(BaseClass), ref copy2Instance);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
        Assert.AreEqual("234", copy2.SubValue1);

        copy2.Value = copy2.SubValue1 = "xx";
        Reader.ReadObject(data, typeof(SubClass1), ref copy2Instance);
        Assert.AreEqual(copy2Instance, copy2);
        Assert.AreEqual("123", copy2.Value);
        Assert.AreEqual("234", copy2.SubValue1);
        
        
        var copy3 = (SubClass1)Reader.ReadObject(data, typeof(SubClass1));
        Assert.AreEqual("123", copy3.Value);
        Assert.AreEqual("234", copy3.SubValue1);
        
    }

    [NeuroGlobalType(NormalGlobalClassId)]
    public class NormalGlobalClass
    {
        [Neuro(1)] public string Value;
    }
    
    [Test]
    public void GlobalReadWrite()
    {
        var obj = new NormalGlobalClass()
        {
            Value = "123"
        };
        var data = GetDataForReading(Writer.Write(obj));
        EnsureNotGlobal(data);

        data = GetDataForReading(Writer.WriteGlobalTyped(obj));
        EnsureGlobal(data, NormalGlobalClassId);

        var copy = (NormalGlobalClass) Reader.ReadGlobalTyped(data);
        Assert.AreEqual("123", copy.Value);
    }
    

    [NeuroGlobalType(GlobalBaseClassId)]
    public class GlobalBaseClass
    {
        [Neuro(1)] public string Value;
    }
    
    [Test]
    public void GlobalBaseReadWrite()
    {
        var obj = new GlobalBaseClass()
        {
            Value = "123"
        };
        var data = GetDataForReading(Writer.Write(obj));
        EnsureNotGlobal(data);

        data = GetDataForReading(Writer.WriteGlobalTyped(obj));
        EnsureGlobal(data, GlobalBaseClassId);

        var copy = (GlobalBaseClass) Reader.ReadGlobalTyped(data);
        Assert.AreEqual("123", copy.Value);
    }

    [Neuro(2)]
    public class GlobalSubClass : GlobalBaseClass
    {
        [Neuro(1)] public string SubValue1;
    }
    
    [Test]
    public void GlobalSubReadWrite()
    {
        var obj = new GlobalSubClass()
        {
            Value = "123",
            SubValue1 = "234"
        };
        var data = GetDataForReading(Writer.Write(obj));
        EnsureNotGlobal(data);

        data = GetDataForReading(Writer.WriteGlobalTyped(obj));
        EnsureGlobal(data, GlobalBaseClassId);

        var copy = (GlobalSubClass) Reader.ReadGlobalTyped(data);
        Assert.AreEqual("123", copy.Value);
        Assert.AreEqual("234", copy.SubValue1);
    }
}