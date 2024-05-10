using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace Ninjadini.Neuro.CodeGen.Tests
{
    public static class TestUtils
    {
        public static string GenerateSource(string source)
        {
            var compilation = CreateCompilation(source + GetStandardSrc());
            var walker = new AnalyzerWrappedCodeWalker();
            walker.Walk(compilation, new NeuroSourceAnalyzer());
            var errors = walker.GetErrorsString();
            if (!string.IsNullOrEmpty(errors))
            {
                Assert.Fail(errors);
            }
            var visitor = new NeuroSourceGenerator();
            var result = visitor.Generate(compilation, diagnostic =>
            {
                errors += diagnostic.GetMessage() + "\n";
            });
            if (!string.IsNullOrEmpty(errors))
            {
                Assert.Fail(errors);
            }
            return result;
        }
        
        public static void GenerateSourceExpectingError(string source, string expectedPartialErrorString)
        {
            var compilation = CreateCompilation(source + GetStandardSrc());
            var walker = new AnalyzerWrappedCodeWalker();
            walker.Walk(compilation, new NeuroSourceAnalyzer());
            var errors = walker.GetErrorsString();
            if (string.IsNullOrEmpty(errors))
            {
                Assert.Fail("Error is expected");
            }
            else if (!string.IsNullOrEmpty(expectedPartialErrorString) && !errors.Contains(expectedPartialErrorString))
            {
                Assert.Fail($"Expected error string `{expectedPartialErrorString}` not found. Resulting error: {errors}");
            }
        }

        public static Compilation CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
            foreach (var diagnostic in syntaxTree.GetDiagnostics())
            {
                Console.WriteLine(diagnostic);
            }
            var syntaxTrees = new[] { syntaxTree };
            var references = new List<PortableExecutableReference>();
            references.Add(MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location));

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            return CSharpCompilation.Create("NeuroRoslyn_Test-Assembly", syntaxTrees, references, options);
        }

        public static void CompareSource(string actualSource, string expectedSource)
        {
            Console.WriteLine(actualSource);
            actualSource = Regex.Replace(actualSource, @"\s", " ");
            expectedSource = Regex.Replace(expectedSource.Trim(), @"\s", " ");
            Assert.That(actualSource, Does.Contain(expectedSource));
        }
        
        public static void TestSourceGenerates(string source, params string[] partialExpectedResults)
        {
            var generatedSrc = GenerateSource(source);
            foreach (var partialExpectedResult in partialExpectedResults)
            {
                CompareSource(generatedSrc, partialExpectedResult);
            }
        }

        static string GetStandardSrc()
        {
            return
                @"
namespace Ninjadini.Neuro
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class NeuroAttribute : System.Attribute
    {
        public uint Tag;
        public NeuroAttribute(uint tag)
        {
            Tag = tag;
        }
    }

    public interface INeuroPoolable
    {

    }

    public interface INeuroCustomTypesRegistryHook
    {
        void Register();
    }

    public interface IReferencable
    {
        uint RefId { get; set; }
        string RefName { get; set; }
    }

    public abstract class Referencable : IReferencable
    {
        uint RefId { get; set; }
        string RefName { get; set; }
    }

    public struct Reference<T> where T : IReferencable
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface)]
    public class NeuroGlobalTypeAttribute : System.Attribute
    {
        public uint Id;

        public NeuroGlobalTypeAttribute(uint id)
        {
            Id = id;
        }
    }

    [AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Struct, AllowMultiple = true)]
    public class ReservedNeuroTagAttribute : System.Attribute
    {
        public uint Tag;

        public ReservedNeuroTagAttribute(uint tag)
        {
            Tag = tag;
        }
    }
}";
        }
        
        class AnalyzerWrappedCodeWalker : CSharpSyntaxWalker
        {
            Compilation compilation;
            NeuroSourceAnalyzer analyzer;
            AnalyzerOptions options;
            private List<string> errors = new List<string>();

            public void Walk(Compilation compilation_, NeuroSourceAnalyzer analyzer_)
            {
                errors.Clear();
                compilation = compilation_;
                analyzer = analyzer_;
                options = new AnalyzerOptions(new ImmutableArray<AdditionalText>());

                foreach (var syntaxTree in this.compilation.SyntaxTrees)
                {
                    Visit(syntaxTree.GetRoot());
                }
            }

            public string GetErrorsString()
            {
                return string.Join("\n", errors);
            }

            public override void Visit(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax || syntaxNode is StructDeclarationSyntax)
                {
                    VisitClassOrStructNode(syntaxNode);
                }

                base.Visit(syntaxNode);
            }

            void VisitClassOrStructNode(SyntaxNode syntaxNode)
            {
                var model = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(syntaxNode);
                var ctx = new SymbolAnalysisContext(classSymbol, compilation, options, ReportDiagnostic,
                    IsSupportedDiagnostic, new CancellationToken());
                analyzer.ProcessClassOrStruct(ctx);
            }

            private void ReportDiagnostic(Diagnostic obj)
            {
                errors.Add(obj.Id + ": " + obj.GetMessage());
            }

            private bool IsSupportedDiagnostic(Diagnostic arg)
            {
                return true;
            }
        }
    }
}