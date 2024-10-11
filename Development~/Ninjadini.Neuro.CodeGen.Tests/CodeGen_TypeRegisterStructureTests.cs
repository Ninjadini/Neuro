using System;
using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests;

public class CodeGen_TypeRegisterStructureTests
{
    [Test]
    public void NotGeneratedIfNoNeuro()
    {
        var src = @"
        class TestClass
        {
            public int Id;
        }
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Assert.AreEqual("", generatedSrc);
    }
    
    [Test]
    public void GeneratedIfNeuro()
    {
        var src = @"
partial class TestChildClass
{
    [Ninjadini.Neuro.Neuro(1)] public int Id;
}
";
        
        TestUtils.TestSourceGenerates(src, 
@"[assembly:Ninjadini.Neuro.NeuroAssemblyAttribute(typeof(NeuroCodeGen_NeuroRoslyn_Test_Assembly), ""RegisterTypes"")]
public static class NeuroCodeGen_NeuroRoslyn_Test_Assembly
{
    static bool registered;
    public static void RegisterTypes()
    {
        if (registered) return;
        registered = true;"
, 
@"if(_NeuroSyncTypes.IsEmpty<TestChildClass>())
         _NeuroSyncTypes.Register<TestChildClass>((_NeuroSyncNS.INeuroSync neuro, ref TestChildClass value) => {
           value ??= new TestChildClass();
           neuro.Sync(1, nameof(value.Id), ref value.Id, default);
         });
    }
}"
            );
    }
        
    [Test]
    public void TestPolymorphics1()
    {
        var src = @"
using Ninjadini.Neuro;
[Neuro(1)]
        partial class BaseClass
        {

        }
[Neuro(2)]
        partial class SubClass1 : BaseClass
        {

        }
";
        TestUtils.TestSourceGenerates(src, 
            @"RegisterSubClass<BaseClass, SubClass1>(2, 
"
        );
    }

}