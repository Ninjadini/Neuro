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
        class CodeWalker
        {
            Compilation compilation;
            NeuroScanMode scanMode;
            Action<Diagnostic> onError;
            List<string> registryHooks;
            List<string> referencableTypes;
            HashSet<string> referencableTypesAdded;
            List<string> enumTypes;
            HashSet<string> enumTypesAdded;
            Dictionary<string, ClassToGenerate> classesToGenerate;
            HashSet<ISymbol> processedSymbols;
            Dictionary<string, List<TagNameLocation>> _baseClasses;
            List<TagNameLocation> _globalClasses;
            List<string> _fatalMessages;
            SyntaxTree cachedModelTree;
            SemanticModel cachedModel;

            public GenerationResult Walk(Compilation compilation_, Action<Diagnostic> onError_ = null)
            {
                compilation = compilation_;
                onError = onError_;
                scanMode = NeuroCodeGenUtils.GetScanMode(compilation_);
                registryHooks = new List<string>();
                referencableTypes = new List<string>();
                referencableTypesAdded = new HashSet<string>();
                enumTypes = new List<string>();
                enumTypesAdded = new HashSet<string>();
                classesToGenerate = new Dictionary<string, ClassToGenerate>();
                processedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                _globalClasses = new List<TagNameLocation>();
                _baseClasses = new Dictionary<string, List<TagNameLocation>>();
                _fatalMessages = new List<string>();
                cachedModelTree = null;
                cachedModel = null;
                if (scanMode == NeuroScanMode.Skip)
                {
                    return new GenerationResult();
                }
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    VisitTypeContainer(syntaxTree.GetRoot());
                }

                if (!ValidateConflicts())
                {
                    return new GenerationResult()
                    {
                        FatalMessages = _fatalMessages
                    };
                }
                return new GenerationResult()
                {
                    Classes = classesToGenerate.Values.ToList(),
                    ReferencableTypes = referencableTypes,
                    EnumTypes = enumTypes,
                    RegistryHooks = registryHooks,
                    FatalMessages = _fatalMessages,
                    TagsByRootClass = ToEntriesByRootClass(_baseClasses),
                    GlobalTypeIds = ToEntries(_globalClasses)
                };
            }

            bool ValidateConflicts()
            {
                var allPass = true;
                foreach (var rootNameAndClasses in _baseClasses)
                {
                    var classes = rootNameAndClasses.Value;
                    allPass &= ValidateConflicts(classes, NeuroSourceAnalyzer.ClassTagConflictRule, NeuroSourceAnalyzer.ClassTagReservedRule, NeuroSourceAnalyzer.ClassTagNotSetRule, "tags");
                }
                // A [ReservedNeuroTag] never lands in _globalClasses, so the reserved branch is unreachable
                // here - but it used to be handed ClassTagConflictRule, whose message wants one more
                // argument than that branch supplies.
                allPass &= ValidateConflicts(_globalClasses, NeuroSourceAnalyzer.GlobalTypeConflictRule, NeuroSourceAnalyzer.ClassTagReservedRule, NeuroSourceAnalyzer.GlobalTypeIdNotSetRule, "global type ids");
                return allPass;
            }

            bool ValidateConflicts(List<TagNameLocation> classes, DiagnosticDescriptor tagConflict, DiagnosticDescriptor tagReserved, DiagnosticDescriptor tagNotSet, string noun)
            {
                var allPass = true;
                classes.Sort((a, b) => a.Tag.CompareTo(b.Tag));
                ReportUnsetTags(classes, tagNotSet, noun);
                var numClasses = classes.Count;
                for (var index1 = 0; index1 < numClasses; index1++)
                {
                    var item1 = classes[index1];
                    // Tag 0 is 'not decided yet', reported above. Two of them are not a conflict with
                    // each other, and saying so would bury the message that actually helps.
                    if (string.IsNullOrEmpty(item1.Name) || item1.Tag == 0)
                    {
                        continue;
                    }
                    for (var index2 = 0; index2 < numClasses; index2++)
                    {
                        var item2 = classes[index2];
                        if (item1.Tag == item2.Tag && item1.Name != item2.Name)
                        {
                            var diagnostic = string.IsNullOrEmpty(item2.Name)
                                ? Diagnostic.Create(tagReserved,
                                    item1.Location, item1.Tag,
                                    item1.Name, CreateTagsList(classes, noun))
                                : Diagnostic.Create(tagConflict,
                                    item1.Location, item1.Tag,
                                    item1.Name, item2.Name, CreateTagsList(classes, noun));
                            if (onError != null)
                            {
                                onError(diagnostic);
                            }
                            // Kept as text as well: unity does not surface ReportDiagnostic from the
                            // generation step, so this gets written into the generated source instead.
                            _fatalMessages.Add(diagnostic.Id + ": " + diagnostic.GetMessage());
                            allPass = false;
                            break;
                        }
                    }
                }
                return allPass;
            }

            /// `[Neuro(0)]` / `[NeuroGlobalType(0)]` means the tag has not been picked yet. The analyzer
            /// already reports that it is out of range, but only here is the whole hierarchy known, so
            /// this is the one place that can say which number to use instead.
            void ReportUnsetTags(List<TagNameLocation> classes, DiagnosticDescriptor tagNotSet, string noun)
            {
                string report = null;
                foreach (var item in classes)
                {
                    // An interface is allowed to sit at tag 0 - it is the root, it never gets written.
                    if (item.Tag != 0 || item.IsInterface || string.IsNullOrEmpty(item.Name))
                    {
                        continue;
                    }
                    if (report == null)
                    {
                        report = CreateTagsList(classes, noun);
                    }
                    var diagnostic = Diagnostic.Create(tagNotSet, item.Location, item.Name, report);
                    if (onError != null)
                    {
                        onError(diagnostic);
                    }
                }
            }

            static string CreateTagsList(List<TagNameLocation> list, string noun)
            {
                return NeuroTagReport.Describe(ToEntries(list), noun);
            }

            static Dictionary<string, List<NeuroTagReport.Entry>> ToEntriesByRootClass(Dictionary<string, List<TagNameLocation>> byRootClass)
            {
                var result = new Dictionary<string, List<NeuroTagReport.Entry>>(byRootClass.Count);
                foreach (var rootNameAndClasses in byRootClass)
                {
                    result.Add(rootNameAndClasses.Key, ToEntries(rootNameAndClasses.Value));
                }
                return result;
            }

            static List<NeuroTagReport.Entry> ToEntries(List<TagNameLocation> list)
            {
                var result = new List<NeuroTagReport.Entry>(list.Count);
                foreach (var item in list)
                {
                    result.Add(new NeuroTagReport.Entry(item.Tag, item.Name));
                }
                return result;
            }

            struct TagNameLocation
            {
                public uint Tag;
                public string Name;
                public Location Location;
                /// Interfaces are the one thing allowed to stay at tag 0.
                public bool IsInterface;

                public TagNameLocation(uint tag, string name, Location location, bool isInterface = false)
                {
                    Tag = tag;
                    Name = name;
                    Location = location;
                    IsInterface = isInterface;
                }
            }

            /// Walks a compilation unit or a namespace looking for type declarations. Deliberately not a
            /// CSharpSyntaxWalker: that visits every node in the tree, method bodies and expressions included,
            /// none of which can hold a type declaration.
            void VisitTypeContainer(SyntaxNode container)
            {
                foreach (var child in container.ChildNodes())
                {
                    var typeDeclaration = child as TypeDeclarationSyntax;
                    if (typeDeclaration != null)
                    {
                        VisitTypeDeclaration(typeDeclaration);
                        VisitNestedTypes(typeDeclaration);
                    }
                    else if (child is MemberDeclarationSyntax
                             && !(child is EnumDeclarationSyntax)
                             && !(child is DelegateDeclarationSyntax)
                             && !(child is GlobalStatementSyntax))
                    {
                        // A namespace. Matched by elimination rather than by type so that file scoped
                        // namespaces are picked up too without needing a newer Roslyn to compile against.
                        VisitTypeContainer(child);
                    }
                }
            }

            void VisitNestedTypes(TypeDeclarationSyntax typeDeclaration)
            {
                foreach (var member in typeDeclaration.Members)
                {
                    var nested = member as TypeDeclarationSyntax;
                    if (nested != null)
                    {
                        VisitTypeDeclaration(nested);
                        VisitNestedTypes(nested);
                    }
                }
            }

            void VisitTypeDeclaration(TypeDeclarationSyntax typeDeclaration)
            {
                var isStruct = typeDeclaration is StructDeclarationSyntax;
                if (!isStruct && !(typeDeclaration is ClassDeclarationSyntax))
                {
                    // Interfaces and records are only ever reached through a class or struct that uses them.
                    return;
                }
                // Decide from the source text whether this is worth binding. Binding is by far the most
                // expensive thing here and most types in an assembly are of no interest.
                var isRegistryHook = false;
                if (scanMode == NeuroScanMode.Fast)
                {
                    isRegistryHook = NeuroCodeGenUtils.HasRegistryHookBaseSyntax(typeDeclaration);
                    if (!isRegistryHook && !NeuroCodeGenUtils.HasNeuroTypeAttributeSyntax(typeDeclaration))
                    {
                        return;
                    }
                }
                else if (!NeuroCodeGenUtils.CouldBeNeuroTypeSyntax(typeDeclaration))
                {
                    return;
                }

                var classSymbol = GetSemanticModel(typeDeclaration.SyntaxTree).GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
                if (classSymbol == null)
                {
                    return;
                }
                if (!processedSymbols.Add(classSymbol))
                {
                    // A partial type is declared more than once but must only be generated once.
                    return;
                }
                if (NeuroSourceGenerator.Verbose)
                {
                    if (isStruct) Console.WriteLine("struct: " + classSymbol.Name);
                    else Console.WriteLine("class: " + classSymbol.Name);
                }

                // isRegistryHook only decided whether this was worth binding; the real answer is the semantic one.
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
                        if (referencableTypesAdded.Add(fullName))
                        {
                            referencableTypes.Add(fullName);
                        }
                    }
                }
            }
            
            SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
            {
                if (cachedModelTree != syntaxTree)
                {
                    cachedModelTree = syntaxTree;
                    cachedModel = compilation.GetSemanticModel(syntaxTree);
                }
                return cachedModel;
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
                if (classToGenerate == null && scanMode == NeuroScanMode.Fast)
                {
                    // Attributed fields alone no longer opt a type in, so there is nothing to read its
                    // members for. NeuroSourceAnalyzer reports the missing class level attribute.
                    return;
                }
                foreach (var member in classSymbol.GetMembers())
                {
                    var fieldSymbol = member as IFieldSymbol;
                    if (fieldSymbol == null || fieldSymbol.IsStatic)
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
                        if (enumTypesAdded.Add(fullName))
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
                    if (IsNeuroType(baseSymbol))
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
                        new TagNameLocation(classToGenerate.Tag, classToGenerate.Name, NeuroCodeGenUtils.GetLocation(classAttribute), classSymbol.TypeKind == TypeKind.Interface));
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

            /// Under fast code gen a class level attribute is the only way in, which also means the base chain
            /// can be answered without reading anyone's members.
            bool IsNeuroType(INamedTypeSymbol symbol)
            {
                if (NeuroCodeGenUtils.FindNeuroAttribute(symbol) != null)
                {
                    return true;
                }
                if (scanMode == NeuroScanMode.Fast)
                {
                    return false;
                }
                foreach (var member in symbol.GetMembers())
                {
                    var fieldSymbol = member as IFieldSymbol;
                    if (fieldSymbol != null && NeuroCodeGenUtils.FindNeuroAttribute(fieldSymbol) != null)
                    {
                        return true;
                    }
                }
                return false;
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
                        var model = GetSemanticModel(initializerValue.SyntaxTree);
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

            /// Every subtype tag, keyed by root class, and every global type id - kept so the generated
            /// file can carry a tag map you can read without provoking a conflict first.
            public Dictionary<string, List<NeuroTagReport.Entry>> TagsByRootClass;
            public List<NeuroTagReport.Entry> GlobalTypeIds;

            /// Conflicts that stopped generation, as text. Unity does not show a diagnostic reported
            /// from the generation step, so these get written into the generated source too.
            public List<string> FatalMessages;
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