using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests;

public class ReadOutTests
{
    [Test]
    public void Test1()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class TestClass
        {
            [Neuro(1)] public int Id;
            [Neuro(2)] public string Name;
            [Neuro(3)] public Test.NameSpace.TestSubClass Child;
            [Neuro(44)] public TestStruct Str;
            [Neuro(55)] public System.DateTimeKind Enum;
            [Neuro(6)] public System.DateTimeKind? NullableEnum;
        }
namespace Test.NameSpace
{
        [Neuro(2)]
        partial class TestSubClass : TestClass
        {
            [Neuro(1)] int Id2 = 1;
            [Neuro(2)] string Name2 = ""HI!"";
        }
}

        partial struct TestStruct
        {
const uint KEY1 = 1;
            [Neuro(KEY1)] public int Id;
            [Neuro(2)] public string Name;
        }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
    }
    
    
    [Test]
    public void TestInterface()
    {
        var src = @"
using Ninjadini.Neuro;
namespace Test.NameSpace
{
        [Neuro(1)]
        partial interface MyInterface
        {
        }

        [Neuro(2)]
        partial class TestSubClass : MyInterface
        {
            [Neuro(1)] int Id;
        }
}
";
        TestUtils.TestSourceGenerates(src, 
@"
_NeuroSyncTypes.RegisterSubClass<Test.NameSpace.MyInterface, Test.NameSpace.TestSubClass>(2, Test.NameSpace.TestSubClass.Sync);
",
@"
public partial class TestSubClass {
    internal static void Sync(_NeuroSyncNS.INeuroSync neuro, ref TestSubClass value) {
           value ??= new Test.NameSpace.TestSubClass();
           neuro.Sync(1, nameof(value.Id), ref value.Id, default);
}}
"
);
    }
    
    [Test]
    public void TestDefaultValues()
    {
        var src = @"
using Ninjadini.Neuro;
using Test.NameSpace;
        [Neuro(1)]
        partial class TestClass
        {
const int DEFAULT_INT = 1;
const string DEFAULT_STRING = ""HELLO"";

            [Neuro(1)] public int Id = 123;
            [Neuro(2)] public int Id2 = DEFAULT_INT;
            [Neuro(3)] public string Name = DEFAULT_STRING;
            [Neuro(4)] public string Name2 = TestConsts.DEFAULT_STRING;
        }
namespace Test.NameSpace
{
        partial class TestConsts
        {
public const int DEFAULT_INT = 1;
public const string DEFAULT_STRING = ""WORLD"";
        }
}
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
    }
        
    [Test]
    public void TestContainedClass()
    {
        var src = @"
using Ninjadini.Neuro;
namespace Test.NameSpace
{
        partial class TestClass
        {
            [Neuro(1)]
            partial class TestChildClass
            {
                [Neuro(1)] public int Id;
            }
        }
}
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
    }
        
    [Test]
    public void TestNullables()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class TestClass
        {
            [Neuro(1)] public int? Id;
            [Neuro(2)] public System.DateTimeKind? NullableEnum;
            [Neuro(3)] public TestStruct? Str;
        }

        partial struct TestStruct
        {
            [Neuro(1)] public int Id;
            [Neuro(2)] public string Name;
        }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
    }
        
    [Test]
    public void TestStructDefault()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class TestClass
        {
            [Neuro(1)] public TestStruct Str;
        }
        partial struct TestStruct : System.IEquatable<TestStruct>
        {
            [Neuro(1)] public int Id;
            [Neuro(2)] public int Name;

            public bool Equals(TestStruct other)
            {
                return Id == other.Id && Name == other.Name;
            }
        }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
    }
    
        
        
    [Test]
    public void TestCustomRegistry()
    {
        NeuroSourceGenerator.Verbose = true;
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class TestClass
        {
            [Neuro(1)] public int Id;
        }
    

    struct CustomReg : INeuroCustomTypesRegistryHook
    {
        public void Register()
        {
            
        }
    }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
        Assert.IsTrue(generatedSrc.Contains("new CustomReg().Register()"));
    }
    
    [Test]
    public void TestCustomRegistryOnly()
    {
        NeuroSourceGenerator.Verbose = true;
        var src = @"
using Ninjadini.Neuro;
    struct CustomReg : INeuroCustomTypesRegistryHook
    {
        public void Register()
        {
            
        }
    }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
        Assert.IsTrue(generatedSrc.Contains("new CustomReg().Register()"));
    }

    [Test]
    public void TestPoolableDetection()
    {
        NeuroSourceGenerator.Verbose = true;
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class TestClass : INeuroPoolable
        {
            [Neuro(1)] public int Id;
        }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
    }

    [Test]
    public void TestReferencableViaInterface()
    {
        NeuroSourceGenerator.Verbose = true;
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        [NeuroGlobalType(123)]
        partial class TestClass : IReferencable
        {
        }
        partial class OtherClass
        {
            [Neuro(1)] public Reference<TestClass> Ref;
        }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
    }

    [Test]
    public void TestReferencableViaClass()
    {
        NeuroSourceGenerator.Verbose = true;
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        [NeuroGlobalType(123)]
        partial class TestClass : IReferencable
        {
        }

        partial class OtherClass
        {
            [Neuro(1)] public Reference<TestClass> Ref;
        }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
    }

    [Test]
    [Ignore("Doesn't work yet")]
    public void TestReadOnlyList()
    {
        NeuroSourceGenerator.Verbose = true;
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        [NeuroGlobalType(123)]
        partial class TestClass : IReferencable
        {
            [Neuro(1)] readonly System.Collections.Generic.List<int> list = new System.Collections.Generic.List<int>();
        }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
    }
    
        
    [Test]
    public void TestGlobalTag()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        [NeuroGlobalType(33)]
        partial class TestClass
        {
            [Neuro(1)] public int? Id;
        }
";
        TestUtils.TestSourceGenerates(src, 
            @"
_NeuroSyncTypes.Register<TestClass>((_NeuroSyncNS.INeuroSync neuro, ref TestClass value) => {
           value ??= new TestClass();
           neuro.Sync(1, nameof(value.Id), ref value.Id);
         }, globalTypeId:33);
"
        );
    }
        
    [Test]
    public void TestEnumTag()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public MyEnum e;
        }
        enum MyEnum
        {
            A,
            B,
            C
        }
";
        TestUtils.TestSourceGenerates(src, 
            @"
if(_NeuroSyncNS.NeuroSyncEnumTypes<MyEnum>.IsEmpty())
         _NeuroSyncNS.NeuroSyncEnumTypes<MyEnum>.Register((e) => (int)e, (i) => (MyEnum)i);
"
        );
    }
    


// TestSketches
    [Neuro(1)]
    class TestClass
    {
        [Neuro(1)] public int Id;
        [Neuro(2)] public int Name;
    }
    public class NeuroAttribute : System.Attribute
    {
        public uint Tag;
        public NeuroAttribute(uint tag)
        {
            Tag = tag;
        }
    }
}