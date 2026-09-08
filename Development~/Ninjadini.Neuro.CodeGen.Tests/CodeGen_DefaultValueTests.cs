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
        public static Vec Make(int x_, int y_) => new Vec(x_, y_);
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
    public void StaticProperty_IsHeldInAStaticField()
    {
        // The Vector2Int.one shape. Only static fields used to be resolved, so this read back as (0, 0).
        // A property is a call, so the value is built once at class init rather than on every Sync.
        TestUtils.TestSourceGenerates(Wrap("public Zz.Vec A = Zz.Vec.One;"),
            "internal static readonly global::Zz.Vec _default_TestClass_A = global::Zz.Vec.One;",
            "neuro.Sync(1, nameof(value.A), ref value.A, global::NeuroCodeGen_NeuroRoslyn_Test_Assembly._default_TestClass_A);");
    }

    [Test]
    public void StaticField_IsFullyQualified()
    {
        // Qualified so the generated file can not pick up something else of that name.
        TestUtils.TestSourceGenerates(Wrap("public Zz.Vec A = Zz.Vec.Two;"),
            "neuro.Sync(1, nameof(value.A), ref value.A, global::Zz.Vec.Two);");
    }

    [Test]
    public void Constructor_IsHeldInAStaticField()
    {
        TestUtils.TestSourceGenerates(Wrap("public Zz.Vec A = new Zz.Vec(3, -4);"),
            "internal static readonly global::Zz.Vec _default_TestClass_A = new global::Zz.Vec(3, -4);",
            "neuro.Sync(1, nameof(value.A), ref value.A, global::NeuroCodeGen_NeuroRoslyn_Test_Assembly._default_TestClass_A);");
    }

    [Test]
    public void StaticMethod_IsHeldInAStaticField()
    {
        // The `fp Speed = FPUtils.FromInt(10)` and `TimeSpan.FromSeconds(0.25)` shape - the call runs once.
        TestUtils.TestSourceGenerates(Wrap("public Zz.Vec A = Zz.Vec.Make(3, 4);"),
            "internal static readonly global::Zz.Vec _default_TestClass_A = global::Zz.Vec.Make(3, 4);",
            "neuro.Sync(1, nameof(value.A), ref value.A, global::NeuroCodeGen_NeuroRoslyn_Test_Assembly._default_TestClass_A);");
    }

    [Test]
    public void ImplicitConstructor_IsHeldInAStaticField()
    {
        TestUtils.TestSourceGenerates(Wrap("public Zz.Vec A = new(3, 4);"),
            "internal static readonly global::Zz.Vec _default_TestClass_A = new global::Zz.Vec(3, 4);");
    }

    [Test]
    public void ValueNamingDefaults_StayInline()
    {
        // A literal or a static field is already just a value to load - a cache field would only add a
        // name to read past.
        var generated = TestUtils.GenerateSource(Wrap("public int A = 5;", "public Zz.Vec B = Zz.Vec.Two;"));
        TestUtils.CompareSource(generated, "neuro.Sync(1, nameof(value.A), ref value.A, 5);");
        TestUtils.CompareSource(generated, "neuro.Sync(2, nameof(value.B), ref value.B, global::Zz.Vec.Two);");
        Assert.That(generated, Does.Not.Contain("_default_"));
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
        // Silently reading back a 0 where the initialiser said otherwise is the outcome worth refusing.
        // Reaching into a static value is not one of the rebuilt forms - only naming or calling one is.
        TestUtils.GenerateSourceExpectingError(Wrap("public int A = Zz.Vec.Two.x;"),
            "can not be turned into a serialization default");
    }

    [Test]
    public void ConstantExpression_IsFoldedAndCast()
    {
        // The cast carries the type the folded literal alone would lose, e.g. a long or a float.
        TestUtils.TestSourceGenerates(Wrap("public long A = 1 + 2;", "public float B = 1 / 2f;"),
            "neuro.Sync(1, nameof(value.A), ref value.A, (long)(3));",
            "neuro.Sync(2, nameof(value.B), ref value.B, (float)(0.5));");
    }

    [Test]
    public void PrivateStaticMember_Fails()
    {
        // The generated code is a class of its own, so it can not reach a private member - better said
        // as a Neuro error than as a compile error inside generated source no one wrote.
        var src = @"
using Ninjadini.Neuro;
        partial class TestClass
        {
            [Neuro(1)] public int A = Secret;
            private static int Secret = 5;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "can not be turned into a serialization default");
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
