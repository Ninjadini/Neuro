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

    [TestCase("byte"), TestCase("sbyte"), TestCase("short"), TestCase("ushort"), TestCase("char"), TestCase("decimal")]
    public void NarrowNumberTypes_Fail(string type)
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public " + type + @" obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, type);
    }

    [TestCase("byte"), TestCase("char")]
    public void NarrowNumberTypes_InsideCollections_Fail(string type)
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public System.Collections.Generic.List<" + type + @"> obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, type);
    }

    [Test]
    public void EnumBackedByNarrowType_Works()
    {
        // the field type is the enum, not its underlying type, so this stays supported.
        var src = @"
using Ninjadini.Neuro;
        enum SmallEnum : byte { A = 1, B = 2 }
        partial class TestClass
        {
            [Neuro(1)] public SmallEnum obj;
        }
";
        TestUtils.GenerateSource(src);
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

    [Test]
    public void ListWithInvalidType_Fails2()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>> obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "Dictionary<string, string>");
    }

    [Test]
    public void DictionaryWithInvalidType_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "List<string>");
    }

    [Test]
    public void ListWithReadonlyDoesntWorkIfNoInitializer()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public readonly System.Collections.Generic.List<string> obj = null;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "readonly");
    }

    [Test]
    public void ListWithReadonlyDefaultWorks()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public readonly System.Collections.Generic.List<string> obj = new System.Collections.Generic.List<string>();
        }
";
        TestUtils.GenerateSource(src);
    }

    /// A dictionary key is one value - a json object name, and in binary a key with no terminator after it -
    /// so a type built out of [Neuro] fields has nowhere to put its second field.
    [Test]
    public void DictionaryKeyedByNeuroStruct_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        partial struct KeyStruct
        {
            [Neuro(1)] public int A;
            [Neuro(2)] public int B;
        }
        partial class TestClass
        {
            [Neuro(1)] public System.Collections.Generic.Dictionary<KeyStruct, string> obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "dictionary key must be a single value");
    }

    [Test]
    public void DictionaryKeyedByNeuroTaggedStruct_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial struct KeyStruct
        {
            [Neuro(1)] public int A;
        }
        partial class TestClass
        {
            [Neuro(1)] public System.Collections.Generic.Dictionary<KeyStruct, string> obj;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "dictionary key must be a single value");
    }

    [TestCase("string"), TestCase("int"), TestCase("long"), TestCase("System.DateTime")]
    public void DictionaryKeyedBySingleValueTypes_Work(string type)
    {
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public System.Collections.Generic.Dictionary<" + type + @", string> obj;
        }
";
        TestUtils.GenerateSource(src);
    }

    /// The generator only reaches an interface through a class that implements it, so a global type id
    /// written on the interface is never emitted - it has to be a compile error, not a runtime surprise.
    [Test]
    public void GlobalTypeIdOnInterface_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        [NeuroGlobalType(500)]
        partial interface IShape
        {
        }
";
        TestUtils.GenerateSourceExpectingError(src, "it can not carry `[NeuroGlobalType(#)]`");
    }

    [Test]
    public void NeuroTagOnInterface_Works()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial interface IShape
        {
        }
        [Neuro(2)]
        partial class Circle : IShape
        {
            [Neuro(1)] public int R;
        }
";
        TestUtils.GenerateSource(src);
    }
}
