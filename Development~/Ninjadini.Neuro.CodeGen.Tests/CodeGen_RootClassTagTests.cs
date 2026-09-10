using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests;

/// <summary>
/// The tag on a root type is never written - a root registers with `Register&lt;T&gt;(sync)`, no tag - so a bare
/// `[Neuro]` is enough to opt a plain class or struct in. Only a subtype's tag is wire format.
/// </summary>
public class CodeGen_RootClassTagTests
{
    [Test]
    public void BareAttributeOnRootClass_Generates()
    {
        var src = @"
using Ninjadini.Neuro;
[Neuro]
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        var generated = TestUtils.GenerateSource(src);
        TestUtils.CompareSource(generated, "_NeuroSyncTypes.Register<Data>(");
        TestUtils.CompareSource(generated, "neuro.Sync(1, nameof(value.Id), ref value.Id, default);");
    }

    [Test]
    public void BareAttributeOnRootStruct_Generates()
    {
        var src = @"
using Ninjadini.Neuro;
[Neuro]
public partial struct Data
{
    [Neuro(1)] public int Id;
}
";
        TestUtils.CompareSource(TestUtils.GenerateSource(src), "_NeuroSyncTypes.Register<Data>(");
    }

    [Test]
    public void BareAttributeOnRootClass_GeneratesUnderFastCodeGen()
    {
        var src = TestUtils.AssemblyOptIn + @"
[Neuro]
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        TestUtils.CompareSource(TestUtils.GenerateSource(src, TestUtils.FastCodeGenDefine), "_NeuroSyncTypes.Register<Data>(");
    }

    [Test]
    public void BareAttributeOnSubClass_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
[Neuro]
public partial class BaseData
{
    [Neuro(1)] public int Id;
}
[Neuro]
public partial class SubData : BaseData
{
}
";
        TestUtils.GenerateSourceExpectingError(src, "`SubData` is a subtype of the Neuro type `BaseData`");
    }

    [Test]
    public void BareAttributeOnInterfaceImplementation_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
[Neuro]
public interface IThing
{
}
[Neuro]
public partial class Thing : IThing
{
    [Neuro(1)] public int Id;
}
";
        TestUtils.GenerateSourceExpectingError(src, "`Thing` is a subtype of the Neuro type `IThing`");
    }

    [Test]
    public void TooLargeClassTag_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
[Neuro(uint.MaxValue)]
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        TestUtils.GenerateSourceExpectingError(src, "must be below");
    }
}
