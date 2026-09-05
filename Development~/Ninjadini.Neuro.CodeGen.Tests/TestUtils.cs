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
        public const string FastCodeGenDefine = "NEURO_FAST_CODEGEN";

        /// Fast code gen only looks at assemblies that asked to be looked at. Includes the using directive
        /// because assembly attributes have to come after those.
        public const string AssemblyOptIn = "using Ninjadini.Neuro;\n[assembly:Neuro]\n";

        public static string GenerateSource(string source, params string[] defines)
        {
            var compilation = CreateCompilation(source + GetStandardSrc(), defines);
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
        
        public static void GenerateSourceExpectingError(string source, string expectedPartialErrorString, params string[] defines)
        {
            var compilation = CreateCompilation(source + GetStandardSrc(), defines);
            var walker = new AnalyzerWrappedCodeWalker();
            walker.Walk(compilation, new NeuroSourceAnalyzer());
            var errors = walker.GetErrorsString();
            
            if (string.IsNullOrEmpty(errors))
            {
                var visitor = new NeuroSourceGenerator();
                errors = "";
                visitor.Generate(compilation, diagnostic =>
                {
                    errors += diagnostic.GetMessage() + "\n";
                });
                if (string.IsNullOrEmpty(errors))
                {
                    Assert.Fail("Error is expected");
                }
            }
            Console.WriteLine("ERROR: " + errors);
            if (!string.IsNullOrEmpty(expectedPartialErrorString) && !errors.Contains(expectedPartialErrorString))
            {
                Assert.Fail($"Expected error string `{expectedPartialErrorString}` not found. Resulting error: {errors}");
            }
        }

        /// Analyzer diagnostics only, as one string. Unlike the Expecting/Generates helpers this does
        /// not fail on the first error - some things are only worth asserting about by their absence.
        public static string CollectAnalyzerErrors(Compilation compilation)
        {
            var walker = new AnalyzerWrappedCodeWalker();
            walker.Walk(compilation, new NeuroSourceAnalyzer());
            return walker.GetErrorsString();
        }

        /// Generator diagnostics only, as one string.
        public static string CollectGeneratorErrors(string source, params string[] defines)
        {
            var compilation = CreateCompilation(source + GetStandardSrc(), defines);
            var errors = "";
            new NeuroSourceGenerator().Generate(compilation, diagnostic => errors += diagnostic.GetMessage() + "\n");
            Console.WriteLine("ERRORS: " + errors);
            return errors;
        }

        public static Compilation CreateCompilation(string source, params string[] defines)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: defines));
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

        /// Same as <see cref="TestSourceGenerates"/> but with NEURO_FAST_CODEGEN on and the assembly opted in.
        public static void TestFastCodeGenSourceGenerates(string source, params string[] partialExpectedResults)
        {
            var generatedSrc = GenerateSource(AssemblyOptIn + source, FastCodeGenDefine);
            foreach (var partialExpectedResult in partialExpectedResults)
            {
                CompareSource(generatedSrc, partialExpectedResult);
            }
        }

        public static string StandardSrc => GetStandardSrc();

        static string GetStandardSrc()
        {
            return
                @"
namespace Ninjadini.Neuro
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Interface | System.AttributeTargets.Assembly)]
    public class NeuroAttribute : System.Attribute
    {
        public uint Tag;
        public NeuroAttribute(uint tag = 0)
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
                if (errors.Count == 0)
                {
                    var ctx = new CompilationAnalysisContext(compilation, options, ReportDiagnostic,
                        IsSupportedDiagnostic, new CancellationToken());
                }
            }

            public string GetErrorsString()
            {
                return string.Join("\n", errors);
            }

            public override void Visit(SyntaxNode syntaxNode)
            {
                // The real analyzer registers a symbol action for every named type, interfaces included,
                // so the walker standing in for it here has to reach them too.
                if (syntaxNode is ClassDeclarationSyntax || syntaxNode is StructDeclarationSyntax
                    || syntaxNode is InterfaceDeclarationSyntax)
                {
                    VisitClassOrStructNode(syntaxNode);
                }
                else if (syntaxNode is AttributeSyntax attributeSyntax
                         && NeuroSourceAnalyzer.GetScanMode(compilation) == NeuroScanMode.Fast)
                {
                    analyzer.ProcessFieldAttribute(attributeSyntax, compilation.GetSemanticModel(syntaxNode.SyntaxTree),
                        ReportDiagnostic, CancellationToken.None);
                }

                base.Visit(syntaxNode);
            }

            void VisitClassOrStructNode(SyntaxNode syntaxNode)
            {
                var model = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(syntaxNode);
                var ctx = new SymbolAnalysisContext(classSymbol, compilation, options, ReportDiagnostic,
                    IsSupportedDiagnostic, new CancellationToken());
                analyzer.ProcessClassOrStruct(ctx, NeuroSourceAnalyzer.GetScanMode(compilation));
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