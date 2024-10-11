using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ninjadini.Neuro.CodeGen
{
    public partial class NeuroSourceGenerator
    {
        class CodeWalker : CSharpSyntaxWalker
        {
            Compilation compilation;
            private Action<Diagnostic> onError;
            List<string> registryHooks;
            List<string> referencableTypes;
            List<string> enumTypes;
            Dictionary<string, ClassToGenerate> classesToGenerate;
            Dictionary<uint, string> globalTypeNames;

            public GenerationResult Walk(Compilation compilation_, Action<Diagnostic> onError_ = null)
            {
                compilation = compilation_;
                onError = onError_;
                registryHooks = new List<string>();
                referencableTypes = new List<string>();
                enumTypes = new List<string>();
                classesToGenerate = new Dictionary<string, ClassToGenerate>();
                globalTypeNames = new Dictionary<uint, string>();
                foreach (var syntaxTree in this.compilation.SyntaxTrees)
                {
                    Visit(syntaxTree.GetRoot());
                }
                return new GenerationResult()
                {
                    Classes = classesToGenerate.Values.ToList(),
                    ReferencableTypes = referencableTypes,
                    EnumTypes = enumTypes,
                    RegistryHooks = registryHooks
                };
            }

            public override void Visit(SyntaxNode node)
            {
                //stringBuilder.AppendLine(node.GetType().Name);
                
                if (node is ClassDeclarationSyntax classDeclarationSyntax)
                {
                    VisitClassOrStruct(classDeclarationSyntax);
                }
                if (node is StructDeclarationSyntax structDeclarationSyntax)
                {
                    VisitClassOrStruct(structDeclarationSyntax);
                }
                base.Visit(node);
            }
            
            void VisitClassOrStruct(SyntaxNode syntaxNode)
            {
                var model = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(syntaxNode) as INamedTypeSymbol;
                if (classSymbol == null)
                {
                    return;
                }

                var isStruct = syntaxNode is StructDeclarationSyntax;
                if (NeuroSourceGenerator.Verbose)
                {
                    if (isStruct) Console.WriteLine("struct: " + classSymbol.Name);
                    else Console.WriteLine("class: " + classSymbol.Name);
                }

                if (NeuroCodeGenUtils.IsNeuroCustomTypesRegisteryHook(classSymbol))
                {
                    registryHooks.Add(NeuroCodeGenUtils.GetFullName(classSymbol));
                }
                else
                {
                    ProcessAnyClassOrStruct(classSymbol);
                    if (!isStruct && NeuroCodeGenUtils.IsReferencableType(classSymbol))
                    {
                        var fullName = NeuroCodeGenUtils.GetFullName(classSymbol);
                        if (!referencableTypes.Contains(fullName))
                        {
                            referencableTypes.Add(fullName);
                        }
                    }
                }
            }
            
            private void ProcessAnyClassOrStruct(INamedTypeSymbol classSymbol)
            { 
                ClassToGenerate classToGenerate = null;
                var classAttribute = NeuroCodeGenUtils.FindNeuroAttribute(classSymbol);
                if (classAttribute != null)
                {
                    EnsureClassToGenerate(classSymbol, ref classToGenerate);
                    classToGenerate.Tag = NeuroCodeGenUtils.GetNeuroTag(classAttribute);
                }
                var globalAttribute = NeuroCodeGenUtils.FindNeuroGlobalTypeAttribute(classSymbol);
                if (globalAttribute != null)
                {
                    EnsureClassToGenerate(classSymbol, ref classToGenerate);
                    classToGenerate.GlobalTypeId = NeuroCodeGenUtils.GetNeuroGlobalTypeId(globalAttribute);

                    if (onError != null && globalTypeNames.TryGetValue(classToGenerate.GlobalTypeId, out var otherName))
                    {
                        onError(Diagnostic.Create(NeuroSourceAnalyzer.GlobalTypeConflictRule,
                            NeuroCodeGenUtils.GetLocation(globalAttribute), classToGenerate.GlobalTypeId,
                            classToGenerate.Name, otherName));
                    }
                    globalTypeNames[classToGenerate.GlobalTypeId] = classToGenerate.Name;
                }
                foreach (var fieldSymbol in classSymbol.GetMembers().OfType<IFieldSymbol>())
                {
                    if (fieldSymbol.IsStatic)
                    {
                        continue;
                    }
                    var fieldAttribute = NeuroCodeGenUtils.FindNeuroAttribute(fieldSymbol);
                    if (fieldAttribute == null)
                    {
                        continue;
                    }
                    var fieldType = fieldSymbol.Type;
                    EnsureClassToGenerate(classSymbol, ref classToGenerate);

                    var defaultValue = GetDefaultValue(fieldSymbol, fieldType);
                    classToGenerate.Fields.Add(new FieldToGenerate()
                    {
                        Name = fieldSymbol.Name,
                        Tag = NeuroCodeGenUtils.GetNeuroTag(fieldAttribute),
                        DefaultValue = defaultValue,
                        IsEnum = fieldType.TypeKind == TypeKind.Enum,
                        IsReadonly = fieldSymbol.IsReadOnly
                    });
                    if (fieldType.TypeKind == TypeKind.Enum)
                    {
                        var fullName = NeuroCodeGenUtils.GetFullName(fieldType);
                        if (!enumTypes.Contains(fullName))
                        {
                            enumTypes.Add(fullName);
                        }
                    }
                    classToGenerate.HasPrivateFields |= fieldSymbol.DeclaredAccessibility != Accessibility.Public;
                }
                if (classToGenerate != null)
                {
                    ProcessNeuroClass(classSymbol, classToGenerate);
                }
            }

            void EnsureClassToGenerate(INamedTypeSymbol symbol, ref ClassToGenerate classToGenerate)
            {
                if (classToGenerate == null)
                {
                    var fullName = NeuroCodeGenUtils.GetFullName(symbol);

                    if (!classesToGenerate.TryGetValue(fullName, out classToGenerate))
                    {
                        classToGenerate = new ClassToGenerate();
                        classToGenerate.Name = symbol.ToDisplayString(nameFormat);
                        classToGenerate.NameSpace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
                        classToGenerate.IsStructOrAbstract = symbol.IsValueType || symbol.IsAbstract;
                        classToGenerate.IsPoolable = NeuroCodeGenUtils.IsPoolableNeuroType(symbol);
                        classesToGenerate[fullName] = classToGenerate;
                    }
                }
            }
            
            private void ProcessNeuroClass(INamedTypeSymbol classSymbol, ClassToGenerate classToGenerate)
            {
                var baseSymbol = classSymbol.BaseType;
                while (baseSymbol != null)
                {
                    if (NeuroCodeGenUtils.FindNeuroAttribute(baseSymbol) != null 
                        || baseSymbol.GetMembers()
                            .Where(m => m.Kind == SymbolKind.Field).Cast<IFieldSymbol>()
                            .Any(s => NeuroCodeGenUtils.FindNeuroAttribute(s) != null))
                    {
                        var baseClass = NeuroCodeGenUtils.GetFullName(baseSymbol);
                        if (string.IsNullOrEmpty(classToGenerate.BaseClassName))
                        {
                            classToGenerate.BaseClassName = baseClass;
                        }
                        classToGenerate.RootClassName = baseClass;
                    }
                    baseSymbol = baseSymbol.BaseType;
                }
                foreach (var symbolInterface in classSymbol.Interfaces)
                {
                    if (NeuroCodeGenUtils.FindNeuroAttribute(symbolInterface) != null)
                    {
                        var baseClass = NeuroCodeGenUtils.GetFullName(symbolInterface);
                        if (string.IsNullOrEmpty(classToGenerate.BaseClassName))
                        {
                            classToGenerate.BaseClassName = baseClass;
                        }
                        classToGenerate.RootClassName = baseClass;
                    }
                }
            }
            
            static SymbolDisplayFormat nameFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);
            

            private string GetDefaultValue(IFieldSymbol fieldSymbol, ITypeSymbol fieldType)
            {
                var syntax = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
                var initializerValue = syntax?.Initializer?.Value;
                if (initializerValue != null && fieldType.TypeKind != TypeKind.Class)
                {
                    if (initializerValue is LiteralExpressionSyntax)
                    {
                        return initializerValue.ToString();
                    }
                    if (initializerValue is IdentifierNameSyntax || initializerValue is MemberAccessExpressionSyntax)
                    {
                        var model = compilation.GetSemanticModel(initializerValue.SyntaxTree);
                        var symbol = model?.GetSymbolInfo(initializerValue).Symbol as IFieldSymbol;
                        if (symbol != null)
                        {
                            return symbol.ToString();
                        }
                    }
                    //throw new System.Exception($"Unsupported initializer `{initializerValue.GetText()}` @ `{fieldSymbol}`");
                }
                return ShouldHaveDefault(fieldType) ? "default" : null;
            }
            
            static bool ShouldHaveDefault(ITypeSymbol symbol)
            {
                if (symbol.TypeKind == TypeKind.Class)
                {
                    return false;
                }
                if (symbol.TypeKind == TypeKind.Interface)
                {
                    return false;
                }
                if (symbol.TypeKind == TypeKind.Struct)
                {
                    if (symbol.Interfaces
                        .Any(i => 
                            i.IsGenericType 
                            && i.Name == "IEquatable" 
                            && i.ContainingNamespace?.Name == "System"
                            && (i.ContainingNamespace?.ContainingNamespace?.IsGlobalNamespace ?? false)
                            && i.TypeArguments.Length == 1
                            && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], symbol)
                            )
                        )
                    {
                        return true;
                    }
                    return false;
                }
                return true;
            }
        }

        class GenerationResult
        {
            public List<ClassToGenerate> Classes;
            public List<string> RegistryHooks;
            public List<string> ReferencableTypes;
            public List<string> EnumTypes;
        }

        class ClassToGenerate
        {
            public string NameSpace;
            public string Name;
            public string BaseClassName;
            public string RootClassName;
            public uint Tag;
            public bool IsStructOrAbstract;
            public bool HasPrivateFields;
            public bool IsPoolable;
            public uint GlobalTypeId;
            public List<FieldToGenerate> Fields = new List<FieldToGenerate>();
        }
            
        class FieldToGenerate
        {
            public string Name;
            public uint Tag;

            public string DefaultValue;
            public bool IsEnum;
            public bool IsReadonly;
        }
    }
}