using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests;

/// <summary>
/// The other tests call into the analyzer directly, which skips Initialize() and so proves nothing about how
/// the diagnostics are registered. These run the real analyzer driver - the same one the compiler and the IDE
/// use - so that a diagnostic that never fires in practice can't pass.
/// </summary>
public class CodeGen_AnalyzerDriverTests
{
    const string FieldWithoutClassAttribute = TestUtils.AssemblyOptIn + @"
public partial class Data
{
    [Neuro(1)] public int Id;
}
";

    static ImmutableArray<Diagnostic> Analyze(string source, params string[] defines)
    {
        var compilation = TestUtils.CreateCompilation(source + TestUtils.StandardSrc, defines);
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new NeuroSourceAnalyzer()));
        return withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    [Test]
    public void FieldWithoutClassAttribute_IsReportedByTheDriver()
    {
        var diagnostics = Analyze(FieldWithoutClassAttribute, TestUtils.FastCodeGenDefine);
        Assert.That(diagnostics.Select(d => d.Id), Does.Contain("Neuro406"), "reported: " + string.Join(", ", diagnostics));
    }

    [Test]
    public void FieldWithoutClassAttribute_PointsAtTheAttribute()
    {
        // The squiggle should land on the [Neuro(1)] the developer just typed, not somewhere else in the file.
        var diagnostic = Analyze(FieldWithoutClassAttribute, TestUtils.FastCodeGenDefine).First(d => d.Id == "Neuro406");
        var span = diagnostic.Location.SourceSpan;
        var text = diagnostic.Location.SourceTree!.ToString().Substring(span.Start, span.Length);
        Assert.AreEqual("Neuro(1)", text);
    }

    [Test]
    public void FieldWithoutClassAttribute_IsSilentWhenModeIsOff()
    {
        var diagnostics = Analyze(FieldWithoutClassAttribute);
        Assert.That(diagnostics.Select(d => d.Id), Does.Not.Contain("Neuro406"), "reported: " + string.Join(", ", diagnostics));
    }

    [Test]
    public void ClassThatOptedIn_IsSilent()
    {
        var src = TestUtils.AssemblyOptIn + @"
[Neuro(1)]
public partial class Data
{
    [Neuro(1)] public int Id;
}
";
        var diagnostics = Analyze(src, TestUtils.FastCodeGenDefine);
        Assert.That(diagnostics, Is.Empty, "reported: " + string.Join(", ", diagnostics));
    }

    [Test]
    public void SubClassMissingItsAttribute_IsReportedByTheDriver()
    {
        var src = TestUtils.AssemblyOptIn + @"
[Neuro(1)]
public partial class BaseData
{
    [Neuro(1)] public int Id;
}
public partial class SubData : BaseData
{
}
";
        var diagnostics = Analyze(src, TestUtils.FastCodeGenDefine);
        Assert.That(diagnostics.Select(d => d.Id), Does.Contain("Neuro404"), "reported: " + string.Join(", ", diagnostics));
    }
}
