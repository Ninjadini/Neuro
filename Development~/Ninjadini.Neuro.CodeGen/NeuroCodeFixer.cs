using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
/*
 doesn't work yet :(
namespace Ninjadini.Neuro.CodeGen
{
    //https://dennistretyakov.com/writing-first-roslyn-analyzer-and-codefix-provider
    //dotnet build -p:Configuration=Release .\Ninjadini.Neuro.CodeGen.csproj
    //nuget pack Ninjadini.Neuro.CodeGen.nuspec -p Configuration=Release -outputDirectory packages
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "NeuroCodeFixer"), Shared]
    public class NeuroCodeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => new ImmutableArray<string>()
        {
            NeuroSourceAnalyzer.InvalidTagDiagnosticID,
            NeuroSourceAnalyzer.FieldTagConflictDiagnosticID
        };
        
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var node = root.FindNode(diagnosticSpan);
                if (node is AttributeSyntax attributeSyntax)
                {
                    // Get the symbol for the method being called
                    
                    var codeAction = CodeAction.Create(
                        "NOICE",
                        cancellationToken => AddParameterAsync(context.Document, attributeSyntax, cancellationToken));

                    context.RegisterCodeFix(codeAction, diagnostic);
                }
                else
                {
                    var codeAction = CodeAction.Create(
                        "NOPE",
                        cancellationToken => AddParameterAsync(context.Document, null, cancellationToken));

                    context.RegisterCodeFix(codeAction, diagnostic);
                }
            }
        }

        private async Task<Document> AddParameterAsync(Document document, AttributeSyntax attributeSyntax, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(attributeSyntax, SyntaxFactory.ParseStatement("HELLO"));
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}
*/