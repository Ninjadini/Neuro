using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests;

/// <summary>
/// NEURO_FAST_CODEGEN narrows what the code gen has to look at, so that it can decide from the source text
/// alone whether a type is worth binding. Two rules make that possible:
/// the assembly opts in with [assembly:Neuro(0)], and every Neuro type opts in with a class level [Neuro(#)].
/// </summary>
public class CodeGen_FastCodeGenTests
{
    const string Define = TestUtils.FastCodeGenDefine;
    const string OptIn = TestUtils.AssemblyOptIn;

    [Test]
    public void ClassAttribute_IsGenerated()
    {
        var src = OptIn + @"
[Neuro(1)]
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        TestUtils.CompareSource(TestUtils.GenerateSource(src, Define), "neuro.Sync(1, nameof(value.Id), ref value.Id, default);");
    }

    [Test]
    public void FieldAttributeWithoutClassAttribute_Fails()
    {
        // The whole point of the mode: fields alone no longer opt a type in, so say so rather than
        // silently leaving the type unregistered.
        var src = OptIn + @"
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        TestUtils.GenerateSourceExpectingError(src, "no class level [Neuro(#)] attribute", Define);
    }

    [Test]
    public void StaticFieldAttributeWithoutClassAttribute_IsIgnored()
    {
        // Static and const fields are never serialized, so they can't be the mistake Neuro406 looks for.
        var src = OptIn + @"
public class NotNeuro
{
    [Neuro(1)] public static int Id;
    [Neuro(2)] public const int Other = 3;
}
";
        Assert.That(TestUtils.GenerateSource(src, Define), Is.Empty);
    }

    [Test]
    public void FieldAttributeWithoutClassAttribute_IsFineWhenModeIsOff()
    {
        var src = @"
using Ninjadini.Neuro;
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        TestUtils.CompareSource(TestUtils.GenerateSource(src), "neuro.Sync(1, nameof(value.Id), ref value.Id, default);");
    }

    [Test]
    public void SubClassWithoutClassAttribute_StillFails()
    {
        // Reported without reading anyone's members - only class level attributes count in this mode.
        var src = OptIn + @"
[Neuro(1)]
public partial class BaseData
{
    [Neuro(1)] public int Id;
}
public partial class SubData : BaseData
{
}
";
        TestUtils.GenerateSourceExpectingError(src, "needs neuro class attribute", Define);
    }

    [Test]
    public void PartialClass_AttributeOnOnePart_FieldsOnAnother()
    {
        // The part carrying the fields has no attribute of its own, but the type opted in, so it counts.
        var src = OptIn + @"
[Neuro(1)]
public partial class Data
{
    [Neuro(1)] public int Id;
}
public partial class Data
{
    [Neuro(2)] public int Other;
}
";
        var generated = TestUtils.GenerateSource(src, Define);
        TestUtils.CompareSource(generated, "neuro.Sync(1, nameof(value.Id), ref value.Id, default);");
        TestUtils.CompareSource(generated, "neuro.Sync(2, nameof(value.Other), ref value.Other, default);");
    }

    [Test]
    public void RegistryHook_IsFoundWithoutAnyAttribute()
    {
        var src = OptIn + @"
public class MyHook : INeuroCustomTypesRegistryHook
{
    public void Register() {}
}
";
        TestUtils.CompareSource(TestUtils.GenerateSource(src, Define), "new MyHook().Register();");
    }

    [Test]
    public void AssemblyThatDidNotOptIn_GeneratesNothing()
    {
        var src = @"
using Ninjadini.Neuro;
[Neuro(1)]
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        Assert.That(TestUtils.GenerateSource(src, Define), Is.Empty);
    }

    [Test]
    public void LegacyDefineName_StillWorks()
    {
        var src = OptIn + @"
[Neuro(1)]
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        TestUtils.CompareSource(TestUtils.GenerateSource(src, "NEURO_SELECTIVE_ASSEMBLIES"), "neuro.Sync(1, nameof(value.Id), ref value.Id, default);");
    }

    [Test]
    public void AssemblyOptInWithExplicitTag_AlsoWorks()
    {
        var src = @"
using Ninjadini.Neuro;
[assembly:Neuro(0)]
[Neuro(1)]
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        TestUtils.CompareSource(TestUtils.GenerateSource(src, Define), "neuro.Sync(1, nameof(value.Id), ref value.Id, default);");
    }

    [Test]
    public void FullyQualifiedAttribute_IsRecognised()
    {
        // The pre-filter reads the source text, so it has to cope with the attribute being written out in full.
        var src = OptIn + @"
[Ninjadini.Neuro.Neuro(1)]
public partial class Data
{
    [Ninjadini.Neuro.NeuroAttribute(1)] public int Id;
}
";
        TestUtils.CompareSource(TestUtils.GenerateSource(src, Define), "neuro.Sync(1, nameof(value.Id), ref value.Id, default);");
    }

    [Test]
    public void NestedTypes_AreStillFound()
    {
        var src = OptIn + @"
public static class Outer
{
    [Neuro(1)]
    public partial class Inner
    {
        [Neuro(1)] public int Id;
    }
}
";
        TestUtils.CompareSource(TestUtils.GenerateSource(src, Define), "neuro.Sync(1, nameof(value.Id), ref value.Id, default);");
    }
}
