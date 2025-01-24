using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests;

public class CodeGen_TagConflictTests
{
    [Test]
    public void NeedsClassTag()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class BaseClass
        {
            [Neuro(1)] public string str;
        }
        partial class SubClass1 : BaseClass
        {
            [Neuro(1)] public int num;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "needs neuro class attribute");
    }
    
    [Test]
    public void NeedsClassTagFromInterface()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial interface IBaseInterface
        {
        }
        partial class SubClass1 : IBaseInterface
        {
            [Neuro(1)] public int num;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "needs neuro class attribute");
    }
    
    [Test]
    public void FailOnMultipleInheritancePaths()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial interface IBaseInterface1
        {
        }
        [Neuro(1)]
        partial interface IBaseInterface2
        {
        }
        [Neuro(2)]
        partial class SubClass1 : IBaseInterface1, IBaseInterface2
        {
            [Neuro(1)] public int num;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "extends from multiple inheritance paths");
    }
    
    [Test]
    public void ClassTagConflictReporting()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class BaseClass
        {
            [Neuro(1)] public string str;
        }
        [Neuro(1)]
        partial class SubClass1 : BaseClass
        {
            [Neuro(1)] public int num;
        }
        [Neuro(1)]
        [ReservedNeuroTag(1)]
        partial class SubClass2 : BaseClass
        {
            [Neuro(1)] public int num;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "is already used by another class");
    }
    
    [Test]
    public void ClassTagConflictWithReservedReporting1()
    {
        var src = @"
using Ninjadini.Neuro;
        [ReservedNeuroTag(1)]
        partial class BaseClass
        {
            [Neuro(1)] public string str;
        }
        [Neuro(1)]
        partial class SubClass1 : BaseClass
        {
            [Neuro(1)] public int num;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "is marked as reserved");
    }
    
    [Test]
    public void ClassTagConflictWithReservedReporting2()
    {
        var src = @"
using Ninjadini.Neuro;
        partial class BaseClass
        {
            [Neuro(1)] public string str;
        }
        [Neuro(1)]
        [ReservedNeuroTag(1)]
        partial class SubClass1 : BaseClass
        {
            [Neuro(1)] public int num;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "is marked as reserved");
    }
    
    
    [Test]
    public void GlobalTagConflictReporting()
    {
        var src = @"
using Ninjadini.Neuro;
        [NeuroGlobalType(1)]
        partial class Class1
        {
            [Neuro(1)] public string str;
        }
        [NeuroGlobalType(1)]
        partial class Class2
        {
            [Neuro(1)] public string str;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "is already used by another c");
    }
}