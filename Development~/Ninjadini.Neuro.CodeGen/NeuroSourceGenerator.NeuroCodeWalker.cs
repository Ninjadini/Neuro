using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Action<Diagnostic> onError;
            List<string> registryHooks;
            List<string> referencableTypes;
            List<string> enumTypes;
            Dictionary<string, ClassToGenerate> classesToGenerate;
            Dictionary<string, List<TagNameLocation>> _baseClasses;
            List<TagNameLocation> _globalClasses;

            public GenerationResult Walk(Compilation compilation_, Action<Diagnostic> onError_ = null)
            {
                compilation = compilation_;
                onError = onError_;
                registryHooks = new List<string>();
                referencableTypes = new List<string>();
                enumTypes = new List<string>();
                classesToGenerate = new Dictionary<string, ClassToGenerate>();
                _globalClasses = new List<TagNameLocation>();
                _baseClasses = new Dictionary<string, List<TagNameLocation>>();
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    Visit(syntaxTree.GetRoot());
                }
                
                foreach (var attr in compilation.Assembly.GetAttributes())
                {
                    if (attr.AttributeClass?.Name == "FieldOffsetToNeuro")
                    {
                        foreach (var arg in attr.ConstructorArguments)
                        {
                            if (arg.Value is INamedTypeSymbol typeSym)
                            {
                                ProcessFieldOffsetStruct(typeSym);
                            }
                        }
                    }
                }

                if (!ValidateConflicts())
                {
                    return new GenerationResult();
                }
                return new GenerationResult()
                {
                    Classes = classesToGenerate.Values.ToList(),
                    ReferencableTypes = referencableTypes,
                    EnumTypes = enumTypes,
                    RegistryHooks = registryHooks
                };
            }

            bool ValidateConflicts()
            {
                var allPass = true;
                foreach (var rootNameAndClasses in _baseClasses)
                {
                    var classes = rootNameAndClasses.Value;
                    allPass &= ValidateConflicts(classes, NeuroSourceAnalyzer.ClassTagConflictRule, NeuroSourceAnalyzer.ClassTagReservedRule);
                }
                allPass &= ValidateConflicts(_globalClasses, NeuroSourceAnalyzer.GlobalTypeConflictRule, NeuroSourceAnalyzer.ClassTagConflictRule);
                return allPass;
            }

            bool ValidateConflicts(List<TagNameLocation> classes, DiagnosticDescriptor tagConflict, DiagnosticDescriptor tagReserved)
            {
                var allPass = true;
                classes.Sort((a, b) => a.Tag.CompareTo(b.Tag));
                var numClasses = classes.Count;
                for (var index1 = 0; index1 < numClasses; index1++)
                {
                    var item1 = classes[index1];
                    if (string.IsNullOrEmpty(item1.Name))
                    {
                        continue;
                    }
                    for (var index2 = 0; index2 < numClasses; index2++)
                    {
                        var item2 = classes[index2];
                        if (item1.Tag == item2.Tag && item1.Name != item2.Name)
                        {
                            if (onError == null)
                            {
                                allPass = false;
                                break;
                            }
                            if (string.IsNullOrEmpty(item2.Name))
                            {
                                onError(Diagnostic.Create(tagReserved,
                                    item1.Location, item1.Tag,
                                    item1.Name, CreateTagsList(classes)));
                            }
                            else
                            {
                                onError(Diagnostic.Create(tagConflict,
                                    item1.Location, item1.Tag,
                                    item1.Name, item2.Name, CreateTagsList(classes)));
                            }
                            allPass = false;
                            break;
                        }
                    }
                }
                return allPass;
            }

            string CreateTagsList(List<TagNameLocation> list)
            {
                var stringBuilder = new StringBuilder();
                foreach (var tag in list)
                {
                    var classSymbolName = string.IsNullOrEmpty(tag.Name) ? "[Reserved]" : tag.Name;
                    stringBuilder.Append(classSymbolName).Append("=>").Append(tag.Tag).Append("; ");
                    //  ^ unity doesn't show multiline errors :(
                }
                return stringBuilder.ToString();
            }

            struct TagNameLocation
            {
                public uint Tag;
                public string Name;
                public Location Location;

                public TagNameLocation(uint tag, string name, Location location)
                {
                    Tag = tag;
                    Name = name;
                    Location = location;
                }
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
                    
                    _globalClasses.Add(new TagNameLocation(classToGenerate.GlobalTypeId, classToGenerate.Name, NeuroCodeGenUtils.GetLocation(globalAttribute)));
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
                    ProcessNeuroClass(classSymbol, classToGenerate, classAttribute);
                }
            }

            void ProcessFieldOffsetStruct(INamedTypeSymbol classSymbol)
            {
                ClassToGenerate classToGenerate = null;
                foreach (var fieldSymbol in classSymbol.GetMembers().OfType<IFieldSymbol>())
                {
                    if (fieldSymbol.IsStatic)
                    {
                        continue;
                    }

                    var fieldOffset = TryGetFieldOffset(fieldSymbol);
                    Console.WriteLine(fieldOffset);
                    if (fieldOffset == null)
                    {
                        continue;
                    }
                    
                    var fieldType = fieldSymbol.Type;
                    EnsureClassToGenerate(classSymbol, ref classToGenerate);

                    var defaultValue = GetDefaultValue(fieldSymbol, fieldType);
                    classToGenerate.Fields.Add(new FieldToGenerate()
                    {
                        Name = fieldSymbol.Name,
                        Tag = (uint)fieldOffset.Value + 1,
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
                    ProcessNeuroClass(classSymbol, classToGenerate, null);
                }
            }
            
            INamedTypeSymbol _fieldOffsetSymbol;

            INamedTypeSymbol FieldOffsetSymbol
            {
                get
                {
                    if (_fieldOffsetSymbol == null)
                    {
                        _fieldOffsetSymbol = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.FieldOffsetAttribute");
                    }
                    return _fieldOffsetSymbol;
                }
            }
            
            int? TryGetFieldOffset(IFieldSymbol field)
            {
                foreach (var attr in field.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, FieldOffsetSymbol))
                    {
                        continue;
                    }
                    if (attr.ConstructorArguments.Length >= 1)
                    {
                        var arg = attr.ConstructorArguments[0];
                        if (arg.Value is int i) return i;
                    }
                }
                return null;
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
            
            private void ProcessNeuroClass(INamedTypeSymbol classSymbol, ClassToGenerate classToGenerate, AttributeData classAttribute)
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
                if (!string.IsNullOrEmpty(classToGenerate.RootClassName))
                {
                    AddToBaseClass(classToGenerate.RootClassName, 
                        new TagNameLocation(classToGenerate.Tag, classToGenerate.Name, NeuroCodeGenUtils.GetLocation(classAttribute)));
                }
                foreach (var attributeData in classSymbol.GetAttributes())
                {
                    if (NeuroCodeGenUtils.IsReservedNeuroTagAttribute(attributeData.AttributeClass))
                    {
                        var tag = NeuroCodeGenUtils.GetNeuroTag(attributeData);
                        AddToBaseClass(string.IsNullOrEmpty(classToGenerate.RootClassName) ? classToGenerate.Name : classToGenerate.RootClassName, 
                            new TagNameLocation(tag, null, NeuroCodeGenUtils.GetLocation(attributeData)));
                    }
                }
            }

            void AddToBaseClass(string rootClassName, TagNameLocation tagNameLocation)
            {
                if (!_baseClasses.TryGetValue(rootClassName, out var list))
                {
                    list = new List<TagNameLocation>();
                    _baseClasses.Add(rootClassName, list);
                }
                list.Add(tagNameLocation);
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
            public Location Location;
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