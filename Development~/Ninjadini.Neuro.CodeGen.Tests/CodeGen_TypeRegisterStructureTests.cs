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

    [Test]
    public void PartialClassSplitOverTwoDeclarations_SyncsEachFieldOnce()
    {
        // Each declaration of a partial type resolves to the same symbol, and that symbol reports every field
        // of every part. Generating from both declarations used to emit each field's Sync call twice.
        var src = @"
using Ninjadini.Neuro;
[Neuro(1)]
partial class Split
{
    [Neuro(1)] public int A;
}
partial class Split
{
    [Neuro(2)] public int B;
}
";
        var generatedSrc = TestUtils.GenerateSource(src);
        Console.WriteLine(generatedSrc);
        Assert.AreEqual(1, CountOccurrences(generatedSrc, "nameof(value.A)"), "field A should be synced exactly once");
        Assert.AreEqual(1, CountOccurrences(generatedSrc, "nameof(value.B)"), "field B should be synced exactly once");
    }

    static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var index = haystack.IndexOf(needle, StringComparison.Ordinal); index >= 0; index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }
        return count;
    }
}
