using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests;

/// <summary>
/// A field initialiser is the serialization default: the readers write it back into the field when the data
/// does not carry it, so an initialiser the code gen can not render would read back as a zero instead.
/// </summary>
public class CodeGen_DefaultValueTests
{
    /// A struct with the IEquatable&lt;&gt; the defaulted Sync overload asks for, plus the shapes a
    /// Unity struct like Vector2Int offers - a static property, a static readonly field, a constructor.
    const string VecSrc = @"
namespace Zz
{
    public struct Vec : System.IEquatable<Vec>
    {
        public int x;
        public int y;
        public Vec(int x_, int y_) { x = x_; y = y_; }
        public static Vec One => new Vec(1, 1);
        public static readonly Vec Two = new Vec(2, 2);
        public bool Equals(Vec other) => other.x == x && other.y == y;
    }
}
";

    [Test]
    public void Literal_IsWrittenAsTyped()
    {
        TestUtils.TestSourceGenerates(Wrap("public int A = 5;", "public float B = 1.5f;", "public bool C = true;"),
            "neuro.Sync(1, nameof(value.A), ref value.A, 5);",
            "neuro.Sync(2, nameof(value.B), ref value.B, 1.5f);",
            "neuro.Sync(3, nameof(value.C), ref value.C, true);");
    }

    [Test]
    public void NegativeNumber_KeepsItsSign()
    {
        // A `-5` is an operator over a literal rather than a literal, which used to read back as 0.
        TestUtils.TestSourceGenerates(Wrap("public int A = -5;", "public float B = -1.5f;"),
            "neuro.Sync(1, nameof(value.A), ref value.A, -5);",
            "neuro.Sync(2, nameof(value.B), ref value.B, -1.5f);");
    }

    [Test]
    public void StaticProperty_IsFullyQualified()
    {
        // The Vector2Int.one shape. Only static fields used to be resolved, so this read back as (0, 0).
        TestUtils.TestSourceGenerates(Wrap("public Zz.Vec A = Zz.Vec.One;"),
            "neuro.Sync(1, nameof(value.A), ref value.A, global::Zz.Vec.One);");
    }

    [Test]
    public void StaticField_IsFullyQualified()
    {
        // Qualified so the generated file can not pick up something else of that name.
        TestUtils.TestSourceGenerates(Wrap("public Zz.Vec A = Zz.Vec.Two;"),
            "neuro.Sync(1, nameof(value.A), ref value.A, global::Zz.Vec.Two);");
    }

    [Test]
    public void Constructor_IsRebuilt()
    {
        TestUtils.TestSourceGenerates(Wrap("public Zz.Vec A = new Zz.Vec(3, -4);"),
            "neuro.Sync(1, nameof(value.A), ref value.A, new global::Zz.Vec(3, -4));");
    }

    [Test]
    public void NotFiniteFloat_ComesFromItsMember()
    {
        // NaN and the infinities have no literal form, so only the member path can carry them.
        TestUtils.TestSourceGenerates(Wrap("public float A = float.NaN;"),
            "neuro.Sync(1, nameof(value.A), ref value.A, float.NaN);");
    }

    [Test]
    public void ClassField_HasNoDefault()
    {
        // A class typed field reads back as null when the data omits it, initialiser or not.
        TestUtils.TestSourceGenerates(Wrap("public string A = \"\"hi\"\";"),
            "neuro.Sync(1, nameof(value.A), ref value.A);");
    }

    [Test]
    public void UnrenderableInitializer_Fails()
    {
        // Silently reading back a 0 where the initialiser said 5 is the outcome worth refusing.
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public int A = Compute();
            static int Compute() => 5;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "can not be turned into a serialization default");
    }

    [Test]
    public void FoldedConstantExpression_Fails()
    {
        // `1 + 2` is a constant to the compiler, but not one of the forms that get rebuilt - write `3`.
        TestUtils.GenerateSourceExpectingError(Wrap("public long A = 1 + 2;"), "can not be turned into a serialization default");
    }

    [Test]
    public void ObjectInitializer_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
" + VecSrc + @"
        partial class TestClass
        {
            [Neuro(1)] public Zz.Vec A = new Zz.Vec { x = 1 };
        }
";
        TestUtils.GenerateSourceExpectingError(src, "can not be turned into a serialization default");
    }

    [Test]
    public void NullableField_WithAnInitializer_Fails()
    {
        // `int?` implements no interfaces, so it can not go to the Sync overload that carries a default -
        // the field reads back as null whatever the initialiser says.
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public int? A = 5;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "can not carry a serialization default");
    }

    [Test]
    public void NullableField_WithoutAnInitializer_IsFine()
    {
        TestUtils.TestSourceGenerates(Wrap("public int? A;", "public int? B = null;"),
            "neuro.Sync(1, nameof(value.A), ref value.A);",
            "neuro.Sync(2, nameof(value.B), ref value.B);");
    }


    static string Wrap(params string[] fields)
    {
        var src = @"
using Ninjadini.Neuro;
" + VecSrc + @"
        partial class TestClass
        {
";
        for (var i = 0; i < fields.Length; i++)
        {
            src += $"            [Neuro({i + 1})] {fields[i]}\n";
        }
        return src + @"        }
";
    }
}
