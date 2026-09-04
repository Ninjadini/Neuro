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

    [Test]
    public void FieldTagConflictListsEveryTagInTheClass()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class Class1
        {
            [Neuro(1)] public string a;
            [Neuro(1)] public string b;
            [Neuro(2)] public string c;
            [Neuro(5)] public string d;
        }
";
        // the report has to include tag 5, which is only read after the conflict on tag 1 is found.
        TestUtils.GenerateSourceExpectingError(src, "is already used by another field");
        TestUtils.GenerateSourceExpectingError(src, "Used tags: 1-2, 5. Next free: 3.");
        TestUtils.GenerateSourceExpectingError(src, "Full list: 1=a; 2=c; 5=d");
    }

    [Test]
    public void FieldTagReportCountsReservedTagsAsTaken()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class Class1
        {
            [Neuro(1)] public string a;
            [ReservedNeuroTag(2)]
            [ReservedNeuroTag(3)]
            [Neuro(1)] public string b;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "Used tags: 1, 2-3(reserved). Next free: 4.");
        TestUtils.GenerateSourceExpectingError(src, "Full list: 1=a; 2=[reserved]; 3=[reserved]");
    }

    [Test]
    public void ClassTagConflictListsEveryTagInTheHierarchy()
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
        partial class SubClass2 : BaseClass
        {
            [Neuro(1)] public int num;
        }
        [Neuro(4)]
        partial class SubClass3 : BaseClass
        {
            [Neuro(1)] public int num;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "Used tags: 1, 4. Next free: 2.");
        TestUtils.GenerateSourceExpectingError(src, "Full list: 1=SubClass1; 1=SubClass2; 4=SubClass3");
    }

    [Test]
    public void GlobalTypeConflictReportsAsGlobalTypeIds()
    {
        var src = @"
using Ninjadini.Neuro;
        [NeuroGlobalType(3)]
        partial class Class1
        {
            [Neuro(1)] public string str;
        }
        [NeuroGlobalType(3)]
        partial class Class2
        {
            [Neuro(1)] public string str;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "Used global type ids: 3. Next free: 1.");
    }

    /// Unity does not show a diagnostic reported during the generation step, so the reason has to reach
    /// it through the generated source or a tag conflict just looks like a pile of unregistered types.
    [Test]
    public void ConflictStillGeneratesSourceCarryingTheReason()
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
        }
        [Neuro(1)]
        partial class SubClass2 : BaseClass
        {
        }
";
        var compilation = TestUtils.CreateCompilation(src + TestUtils.StandardSrc);
        var generated = new NeuroSourceGenerator().Generate(compilation, _ => { });
        Assert.That(generated, Does.Contain("Neuro303"));
        Assert.That(generated, Does.Contain("is already used by another class"));
        Assert.That(generated, Does.Contain("NeuroTagConflict"));
    }

    /// `[Neuro(0)]` on a field is already an error, so it doubles as a way to ask what is free.
    [Test]
    public void UnsetFieldTagReportsTheNextFreeOne()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class Class1
        {
            [Neuro(1)] public string a;
            [Neuro(2)] public string b;
            [Neuro(4)] public string c;
            [Neuro(0)] public string pickOneForMe;
        }
";
        TestUtils.GenerateSourceExpectingError(src, "must be between 1 and 2147483647");
        TestUtils.GenerateSourceExpectingError(src, "Used tags: 1-2, 4. Next free: 3.");
    }

    /// The unset field must not also be reported as conflicting with another unset one.
    [Test]
    public void TwoUnsetFieldTagsAreNotAConflict()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial class Class1
        {
            [Neuro(1)] public string a;
            [Neuro(0)] public string b;
            [Neuro(0)] public string c;
        }
";
        var compilation = TestUtils.CreateCompilation(src + TestUtils.StandardSrc);
        var errors = TestUtils.CollectAnalyzerErrors(compilation);
        Assert.That(errors, Does.Not.Contain("is already used by another field"));
        Assert.That(errors, Does.Contain("Next free: 2."));
    }

    [Test]
    public void UnsetClassTagReportsTheNextFreeOneForTheHierarchy()
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
        }
        [Neuro(3)]
        partial class SubClass2 : BaseClass
        {
        }
        [Neuro(0)]
        partial class SubClass3 : BaseClass
        {
        }
";
        var errors = TestUtils.CollectGeneratorErrors(src);
        Assert.That(errors, Does.Contain("Neuro class attribute tag of `SubClass3` is not set."));
        Assert.That(errors, Does.Contain("Used tags: 1, 3. Next free: 2."));
        Assert.That(errors, Does.Not.Contain("is already used by another class"));
    }

    [Test]
    public void UnsetGlobalTypeIdReportsTheNextFreeOne()
    {
        var src = @"
using Ninjadini.Neuro;
        [NeuroGlobalType(1)]
        partial class Class1
        {
            [Neuro(1)] public string str;
        }
        [NeuroGlobalType(2)]
        partial class Class2
        {
            [Neuro(1)] public string str;
        }
        [NeuroGlobalType(0)]
        partial class Class3
        {
            [Neuro(1)] public string str;
        }
";
        var errors = TestUtils.CollectGeneratorErrors(src);
        Assert.That(errors, Does.Contain("Neuro global type id of `Class3` is not set."));
        Assert.That(errors, Does.Contain("Used global type ids: 1-2. Next free: 3."));
    }

    /// An interface is the root of its hierarchy and never gets written, so tag 0 is legal there.
    [Test]
    public void UnsetTagOnAnInterfaceIsNotReported()
    {
        var src = @"
using Ninjadini.Neuro;
        [Neuro(1)]
        partial interface IRoot
        {
        }
        [Neuro(0)]
        partial interface IMiddle : IRoot
        {
        }
        [Neuro(2)]
        partial class SubClass1 : IMiddle
        {
        }
";
        var errors = TestUtils.CollectGeneratorErrors(src);
        Assert.That(errors, Does.Not.Contain("is not set"));
    }

    [Test]
    public void GeneratedSourceCarriesATagMap()
    {
        var src = @"
using Ninjadini.Neuro;
        [NeuroGlobalType(7)]
        partial class BaseClass
        {
            [Neuro(1)] public string str;
        }
        [Neuro(1)]
        partial class SubClass1 : BaseClass
        {
        }
        [Neuro(3)]
        partial class SubClass2 : BaseClass
        {
        }
";
        var generated = TestUtils.GenerateSource(src);
        Assert.That(generated, Does.Contain("Neuro tag map"));
        Assert.That(generated, Does.Contain("used 1, 3 | next free 2"));
        Assert.That(generated, Does.Contain("1 = SubClass1"));
        Assert.That(generated, Does.Contain("used 7 | next free 1"));
    }
}