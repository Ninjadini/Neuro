using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests;

public class CodeGen_UnsupportedTypesTests
{
    [Test]
    public void Array_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public int[] obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "int[]");
    }
    
    [Test]
    public void Tuple_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public (int, int) obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "(int, int)");
    }

    [Test]
    public void Generic_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public System.Collections.Generic.HashSet<int> obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "HashSet<int>");
    }

    [Test]
    public void List_Works()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public System.Collections.Generic.List<int> obj;
        }
";
        TestUtils.GenerateSource(src);
    }


    [Test]
    public void ListWithRefs_Works()
    {
        var src = @"
using Ninjadini.Neuro;
[NeuroGlobalType(1)]
        partial class TestClass : Referencable
        {
            [Neuro(1)] public System.Collections.Generic.List<Reference<TestClass>> obj;
        }
";
        TestUtils.GenerateSource(src);
    }

    [Test]
    public void ListWithInvalidType_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public System.Collections.Generic.List<(int, int)> obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "(int, int)");
    }
}