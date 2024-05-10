
using System;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class TypeTests
    {
        [Test]
        public void TestBool()
        {
            var src = new TypeWithBool();
            src.value1 = true;
            src.value2 = false;
            
            NeuroSyncTypes.TryRegisterAssemblyOf<TypeWithBool>();

            var bytes = new NeuroBytesWriter().Write(src);
            var json = new NeuroJsonWriter().Write(src);
        
            Console.WriteLine("Binary:" + RawProtoReader.GetDebugString(bytes));
            Console.WriteLine("Debug:" + new NeuroBytesDebugWalker().Walk(bytes.ToArray()));
            Console.WriteLine("Json:" + json);
        
            var copyBinary = new NeuroBytesReader().Read<TypeWithBool>(bytes.ToArray(), new ReaderOptions());
        
            Assert.AreEqual(src.value1, copyBinary.value1);
            Assert.AreEqual(src.value2, copyBinary.value2);
            
            var copyJson = new NeuroJsonReader().Read<TypeWithBool>(json, new ReaderOptions());
            Assert.AreEqual(src.value1, copyJson.value1);
            Assert.AreEqual(src.value2, copyJson.value2);
        }

        public partial class TypeWithBool
        {
            [Neuro(1)] public bool value1;
            [Neuro(2)] public bool value2;
        }
        
        [Test]
        public void TestInt()
        {
            var src = new TypeWithInt();
            src.value1 = 12;
            src.value2 = -123;
            src.value3 = int.MinValue;
            src.value4 = int.MaxValue;
            
            NeuroSyncTypes.TryRegisterAssemblyOf<TypeWithGUID>();

            var bytes = new NeuroBytesWriter().Write(src);
            var json = new NeuroJsonWriter().Write(src);
        
            Console.WriteLine("Binary:" + RawProtoReader.GetDebugString(bytes));
            Console.WriteLine("Debug:" + new NeuroBytesDebugWalker().Walk(bytes.ToArray()));
            Console.WriteLine("Json:" + json);
        
            var copyBinary = new NeuroBytesReader().Read<TypeWithInt>(bytes.ToArray(), new ReaderOptions());
        
            Assert.AreEqual(src.value1, copyBinary.value1);
            Assert.AreEqual(src.value2, copyBinary.value2);
            Assert.AreEqual(src.value3, copyBinary.value3);
            Assert.AreEqual(src.value4, copyBinary.value4);
            
            var copyJson = new NeuroJsonReader().Read<TypeWithInt>(json, new ReaderOptions());
            Assert.AreEqual(src.value1, copyJson.value1);
            Assert.AreEqual(src.value2, copyJson.value2);
            Assert.AreEqual(src.value3, copyJson.value3);
            Assert.AreEqual(src.value4, copyJson.value4);
        }

        public partial class TypeWithInt
        {
            [Neuro(1)] public int value1;
            [Neuro(2)] public int value2;
            [Neuro(3)] public int value3;
            [Neuro(4)] public int value4;
        }
        
        [Test]
        public void TestUint()
        {
            var src = new TypeWithUint();
            src.value1 = 123;
            src.value2 = 0;
            src.value3 = uint.MaxValue;
            
            NeuroSyncTypes.TryRegisterAssemblyOf<TypeWithUint>();

            var bytes = new NeuroBytesWriter().Write(src);
            var json = new NeuroJsonWriter().Write(src);
        
            Console.WriteLine("Binary:" + RawProtoReader.GetDebugString(bytes));
            Console.WriteLine("Debug:" + new NeuroBytesDebugWalker().Walk(bytes.ToArray()));
            Console.WriteLine("Json:" + json);
        
            var copyBinary = new NeuroBytesReader().Read<TypeWithUint>(bytes.ToArray(), new ReaderOptions());
        
            Assert.AreEqual(src.value1, copyBinary.value1);
            Assert.AreEqual(src.value2, copyBinary.value2);
            Assert.AreEqual(src.value3, copyBinary.value3);
            
            var copyJson = new NeuroJsonReader().Read<TypeWithUint>(json, new ReaderOptions());
            Assert.AreEqual(src.value1, copyJson.value1);
            Assert.AreEqual(src.value2, copyJson.value2);
            Assert.AreEqual(src.value3, copyJson.value3);
        }

        public partial class TypeWithUint
        {
            [Neuro(1)] public uint value1;
            [Neuro(2)] public uint value2;
            [Neuro(3)] public uint value3;
        }
        
        [Test]
        public void TestFloat()
        {
            var src = new TypeWithFloat();
            src.value1 = 123.456f;
            src.value2 = -1234.56f;
            src.value3 = float.MinValue;
            src.value4 = float.MaxValue;
            
            NeuroSyncTypes.TryRegisterAssemblyOf<TypeWithGUID>();

            var bytes = new NeuroBytesWriter().Write(src);
            var json = new NeuroJsonWriter().Write(src);
        
            Console.WriteLine("Binary:" + RawProtoReader.GetDebugString(bytes));
            Console.WriteLine("Debug:" + new NeuroBytesDebugWalker().Walk(bytes.ToArray()));
            Console.WriteLine("Json:" + json);
            
            var copyBinary = new NeuroBytesReader().Read<TypeWithFloat>(bytes.ToArray(), new ReaderOptions());
            Assert.AreEqual(src.value1, copyBinary.value1);
            Assert.AreEqual(src.value2, copyBinary.value2);
            Assert.AreEqual(src.value3, copyBinary.value3);
            Assert.AreEqual(src.value4, copyBinary.value4);
            
            var copyJson = new NeuroJsonReader().Read<TypeWithFloat>(json, new ReaderOptions());
            Assert.AreEqual(src.value1, copyJson.value1);
            Assert.AreEqual(src.value2, copyJson.value2);
            Assert.AreEqual(src.value3, copyJson.value3);
            Assert.AreEqual(src.value4, copyJson.value4);
        }

        public partial class TypeWithFloat
        {
            [Neuro(1)] public float value1;
            [Neuro(2)] public float value2;
            [Neuro(3)] public float value3;
            [Neuro(4)] public float value4;
        }
        
        [Test]
        public void TestDouble()
        {
            var src = new TypeWithDouble();
            src.value1 = 123.456;
            src.value2 = -1234.56;
            src.value3 = double.MinValue;
            src.value4 = double.MaxValue;
            
            NeuroSyncTypes.TryRegisterAssemblyOf<TypeWithDouble>();

            var bytes = new NeuroBytesWriter().Write(src);
            var json = new NeuroJsonWriter().Write(src);
        
            Console.WriteLine("Binary:" + RawProtoReader.GetDebugString(bytes));
            Console.WriteLine("Debug:" + new NeuroBytesDebugWalker().Walk(bytes.ToArray()));
            Console.WriteLine("Json:" + json);
            
            var copyBinary = new NeuroBytesReader().Read<TypeWithDouble>(bytes.ToArray(), new ReaderOptions());
            Assert.AreEqual(src.value1, copyBinary.value1);
            Assert.AreEqual(src.value2, copyBinary.value2);
            Assert.AreEqual(src.value3, copyBinary.value3);
            Assert.AreEqual(src.value4, copyBinary.value4);
            
            var copyJson = new NeuroJsonReader().Read<TypeWithDouble>(json, new ReaderOptions());
            Assert.AreEqual(src.value1, copyJson.value1);
            Assert.AreEqual(src.value2, copyJson.value2);
            Assert.AreEqual(src.value3, copyJson.value3);
            Assert.AreEqual(src.value4, copyJson.value4);
        }

        public partial class TypeWithDouble
        {
            [Neuro(1)] public double value1;
            [Neuro(2)] public double value2;
            [Neuro(3)] public double value3;
            [Neuro(4)] public double value4;
        }
        
        [Test]
        public void TestFlagEnum()
        {
            var src = new TypeWithFlagEnums();
            src.value2 = TypeWithFlagEnums.FlagEnum.A;
            src.value3 = TypeWithFlagEnums.FlagEnum.B | TypeWithFlagEnums.FlagEnum.C;
            
            NeuroSyncTypes.TryRegisterAssemblyOf<TypeWithFlagEnums>();

            var bytes = new NeuroBytesWriter().Write(src);
            var json = new NeuroJsonWriter().Write(src);
        
            Console.WriteLine("Binary:" + RawProtoReader.GetDebugString(bytes));
            Console.WriteLine("Debug:" + new NeuroBytesDebugWalker().Walk(bytes.ToArray()));
            Console.WriteLine("Json:" + json);
            
            var copyBinary = new NeuroBytesReader().Read<TypeWithFlagEnums>(bytes.ToArray(), new ReaderOptions());
            Assert.AreEqual(src.value1, copyBinary.value1);
            Assert.AreEqual(src.value2, copyBinary.value2);
            Assert.AreEqual(src.value3, copyBinary.value3);
            
            var copyJson = new NeuroJsonReader().Read<TypeWithFlagEnums>(json, new ReaderOptions());
            Assert.AreEqual(src.value1, copyJson.value1);
            Assert.AreEqual(src.value2, copyJson.value2);
            Assert.AreEqual(src.value3, copyJson.value3);
        }
        
        public partial class TypeWithFlagEnums
        {
            [Neuro(1)] public FlagEnum value1;
            [Neuro(2)] public FlagEnum value2;
            [Neuro(3)] public FlagEnum value3;
            [Flags]
            public enum FlagEnum
            {
                None = 0,
                A = 1<<0,
                B = 1<<1,
                C = 1<<2,
            }
        }

        [Test]
        public void TestGUID()
        {
            var src = new TypeWithGUID();
            src.value1  = Guid.NewGuid();
            src.value2  = Guid.NewGuid();
            src.value3  = Guid.NewGuid();
            src.value4  = Guid.NewGuid();
            
            NeuroSyncTypes.TryRegisterAssemblyOf<TypeWithGUID>();

            var bytes = new NeuroBytesWriter().Write(src);
            var json = new NeuroJsonWriter().Write(src);
        
            Console.WriteLine("Binary:" + RawProtoReader.GetDebugString(bytes));
            Console.WriteLine("Debug:" + new NeuroBytesDebugWalker().Walk(bytes.ToArray()));
            Console.WriteLine("Json:" + json);
        
            var copyBinary = new NeuroBytesReader().Read<TypeWithGUID>(bytes.ToArray(), new ReaderOptions());
        
            Assert.AreEqual(src.value1, copyBinary.value1);
            Assert.AreEqual(src.value2, copyBinary.value2);
            Assert.AreEqual(src.value3, copyBinary.value3);
            Assert.AreEqual(src.value4, copyBinary.value4);
            
            var copyJson = new NeuroJsonReader().Read<TypeWithGUID>(json, new ReaderOptions());
            Assert.AreEqual(src.value1, copyJson.value1);
            Assert.AreEqual(src.value2, copyJson.value2);
            Assert.AreEqual(src.value3, copyJson.value3);
            Assert.AreEqual(src.value4, copyJson.value4);
        }

        public partial class TypeWithGUID
        {
            [Neuro(1)] public Guid value1;
            [Neuro(2)] public Guid value2;
            [Neuro(3)] public Guid value3;
            [Neuro(4)] public Guid value4;
        }
        
        
        [Test]
        public void TestDateTime()
        {
            var src = new TypeWithDateTime();
            src.value1 = new DateTime(2010, 1, 2, 7, 7, 7, DateTimeKind.Utc);
            src.value2  = new DateTime(2024, 2, 3, 7, 7, 7, DateTimeKind.Local);
            src.value3  = new DateTime(2034, 3, 4, 7, 7, 7, DateTimeKind.Unspecified);
            src.value4  = new DateTime(2054, 4, 5, 7, 7, 7, DateTimeKind.Utc);
            
            NeuroSyncTypes.TryRegisterAssemblyOf<TypeWithDateTime>();

            var bytes = new NeuroBytesWriter().Write(src);
            var json = new NeuroJsonWriter().Write(src);
        
            Console.WriteLine("Binary:" + RawProtoReader.GetDebugString(bytes));
            Console.WriteLine("Debug:" + new NeuroBytesDebugWalker().Walk(bytes.ToArray()));
            Console.WriteLine("Json:" + json);
        
            var copyBinary = new NeuroBytesReader().Read<TypeWithDateTime>(bytes.ToArray(), new ReaderOptions());
        
            Assert.AreEqual(src.value1, copyBinary.value1);
            Assert.AreEqual(src.value2, copyBinary.value2);
            Assert.AreEqual(src.value3, copyBinary.value3);
            Assert.AreEqual(src.value4, copyBinary.value4);
            
            var copyJson = new NeuroJsonReader().Read<TypeWithDateTime>(json, new ReaderOptions());
            Assert.AreEqual(src.value1, copyJson.value1);
            Assert.AreEqual(src.value2, copyJson.value2);
            Assert.AreEqual(src.value3, copyJson.value3);
            Assert.AreEqual(src.value4, copyJson.value4);
        }

        public partial class TypeWithDateTime
        {
            [Neuro(1)] public DateTime value1;
            [Neuro(2)] public DateTime value2;
            [Neuro(3)] public DateTime value3;
            [Neuro(4)] public DateTime value4;
        }
    }
}