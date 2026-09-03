using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests;

/// <summary>
/// A class with no [Neuro] members is not serializable. At runtime that surfaces as
/// "NeuroSyncTypes of type X is not registered" when something tries to write it. Where the codegen can tell
/// the class was meant to take part - it derives from a Neuro class - it says so at compile time instead.
/// </summary>
public class CodeGen_FieldlessClassTests
{
    [Test]
    public void SubClassOfNeuroClass_WithoutClassAttribute_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class BaseClass
        {
            [Neuro(1)] public int Id;
        }
        partial class SubClass : BaseClass
        {
            [Neuro(1)] public int Other;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "needs neuro class attribute");
    }

    [Test]
    public void FieldlessSubClassOfNeuroClass_WithoutClassAttribute_Fails()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class BaseClass
        {
            [Neuro(1)] public int Id;
        }
        partial class SubClass : BaseClass
        {
        }
";
        TestUtils.GenerateSourceExpectingError(src, "needs neuro class attribute");
    }

    [Test]
    public void FieldlessSubClassOfNeuroClass_WithClassAttribute_Works()
    {
        // opting in with [Neuro(tag)] is enough, members of its own are not required.
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class BaseClass
        {
            [Neuro(1)] public int Id;
        }
        [Neuro(2)]
        partial class SubClass : BaseClass
        {
        }
";
        TestUtils.GenerateSource(src);
    }

    [Test]
    public void FieldlessStandaloneClass_IsNotDetectedAtCompileTime()
    {
        // Nothing marks this one as intended for Neuro, so codegen stays out of it and it fails at
        // serialization time instead - see ExtremeValueTests.EmptyObject_FailsToSerialize_*.
        var src = @"
using Ninjadini.Neuro;
        partial class Fieldless
        {
        }
        partial class Holder
        {
            [Neuro(1)] public Fieldless Value;
        }
";
        TestUtils.GenerateSource(src);
    }
}
