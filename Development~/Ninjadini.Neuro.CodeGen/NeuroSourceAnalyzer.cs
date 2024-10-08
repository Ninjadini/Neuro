using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ninjadini.Neuro.CodeGen
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NeuroSourceAnalyzer : DiagnosticAnalyzer
    {
        public const string InvalidTagDiagnosticID = "Neuro301";
        public const string FieldTagConflictDiagnosticID = "Neuro300";
        
        static readonly DiagnosticDescriptor ReadOnlyFieldRule = new DiagnosticDescriptor("Neuro022", "Readonly Neuro field on primitive types", "Neuro attributed field with readonly keyword found @ {0}, which is not a class type", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor ReadOnlyWithoutInitializerFieldRule = new DiagnosticDescriptor("Neuro023", "Readonly Neuro fields without an initializer", "Neuro attribute field that is readonly must have a 'new' initializer assignment @ {0}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor UnsupportedTypeRule = new DiagnosticDescriptor("Neuro101", "Unsupported type", "Unsupported type `{0}` found @ {1}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor InvalidDictionaryKeyTypeRule = new DiagnosticDescriptor("Neuro101", "Invalid dictionary key type", "Unsupported dictionary key type `{0}` found @ {1}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor InvalidTagRangeRule = new DiagnosticDescriptor(InvalidTagDiagnosticID, "Invalid field neuro tag", "Neuro field attribute tag must be between 0 and "+int.MaxValue+" @ {1}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor FieldTagConflictRule = new DiagnosticDescriptor(FieldTagConflictDiagnosticID, "Field attribute tag already used", "Neuro field attribute tag {0} of `{1}` is already used by another field `{2}`", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor MissingClassAttributeRule = new DiagnosticDescriptor("Neuro404", "Missing neuro class attribute", "`{0}` needs neuro class attribute because it's base class `{1}` is a Neuro class.", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor InvalidClassTagRangeRule = new DiagnosticDescriptor("Neuro002", "Invalid class neuro tag",  "Neuro class attribute tag must be between 0 and "+int.MaxValue+" @ {0}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor PartialClassRule = new DiagnosticDescriptor("Neuro101", "Non-partial Neuro class",  "{0} is not a partial class. It is required so Neuro can write to private fields without reflection.", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor ClassTagConflictRule = new DiagnosticDescriptor("Neuro303", "Class attribute tag already used", "Neuro class attribute tag {0} of `{1}` is already used by another class `{2}`", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor ClassTagReservedRule = new DiagnosticDescriptor("Neuro304", "Class attribute tag reserved", "Neuro class attribute tag {0} of `{1}` is marked as reserved at {2}", "Syntax", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor GlobalTypeConflictRule = new DiagnosticDescriptor("Neuro310", "Global type id already used", "Neuro global type id {0} of `{1}` is already used by another class `{2}`", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor GlobalTypeRangeRule = new DiagnosticDescriptor("Neuro311", "Invalid global neuro type id",  "Neuro global type id must be between 0 and "+int.MaxValue+" @ {0}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor RefsGlobalTypeRule = new DiagnosticDescriptor("Neuro312", "Global neuro type attribute missing",  "Neuro global type attribute `[NeuroGlobalType(#)]` is required in `{0}` because it is an IReferencable", "Syntax", DiagnosticSeverity.Error, true);

        Dictionary<uint, string> globalTypeNames = new Dictionary<uint, string>();
        Dictionary<ISymbol, List<SymbolAndTag>> classTags = new Dictionary<ISymbol, List<SymbolAndTag>>();
        private Dictionary<uint, string> tempTagDict = new Dictionary<uint, string>();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            UnsupportedTypeRule,
            InvalidDictionaryKeyTypeRule,
            ReadOnlyFieldRule, 
            ReadOnlyWithoutInitializerFieldRule,
            InvalidTagRangeRule, 
            FieldTagConflictRule, 
            MissingClassAttributeRule, 
            InvalidClassTagRangeRule, 
            PartialClassRule, 
            ClassTagConflictRule,
            ClassTagReservedRule,
            GlobalTypeConflictRule,
            GlobalTypeRangeRule,
            RefsGlobalTypeRule);

        public override void Initialize(AnalysisContext context)
        {
            //context.EnableConcurrentExecution();
            classTags.Clear();
            globalTypeNames.Clear();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSymbolAction(ProcessClassOrStruct, SymbolKind.NamedType);
        }

        public void ProcessClassOrStruct(SymbolAnalysisContext context)
        {
            var classSymbol = context.Symbol as INamedTypeSymbol;
            if (classSymbol == null)
            {
                return;
            }
            var fieldsInfo = ProcessFields(classSymbol, context);
            ProcessNeuroBaseClass(classSymbol, fieldsInfo, context);
        }

        private ClassFieldsInfo ProcessFields(INamedTypeSymbol classSymbol, SymbolAnalysisContext context)
        {
            tempTagDict.Clear();
            var result = ClassFieldsInfo.NoNeuro;
            foreach (var fieldSymbol in classSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                if (fieldSymbol.IsStatic)
                {
                    continue;
                }
                foreach (var attributeData in fieldSymbol.GetAttributes())
                {
                    if (NeuroCodeGenUtils.IsReservedNeuroTagAttribute(attributeData.AttributeClass))
                    {
                        var tag = NeuroCodeGenUtils.GetNeuroTag(attributeData);
                        if (tempTagDict.TryGetValue(tag, out var otherField))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(FieldTagConflictRule, NeuroCodeGenUtils.GetLocation(attributeData), tag, fieldSymbol.ToString(), otherField));
                        }
                        else
                        {
                            tempTagDict.Add(tag, "* reserved or deprecated *");
                        }
                    }
                }
                var fieldAttribute = NeuroCodeGenUtils.FindNeuroAttribute(fieldSymbol);
                if (fieldAttribute != null)
                {
                    if(fieldSymbol.IsReadOnly)
                    {
                        if (fieldSymbol.Type.TypeKind != TypeKind.Class)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(ReadOnlyFieldRule, fieldSymbol.Locations.FirstOrDefault(), fieldSymbol.ToString()));
                            continue;
                        }
                        if (!HasFieldInitializer(fieldSymbol))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(ReadOnlyWithoutInitializerFieldRule, fieldSymbol.Locations.FirstOrDefault(), fieldSymbol.ToString()));
                            continue;
                        }
                    }
                    var typeProblem = GetTypeProblem(fieldSymbol.Type);
                    if(typeProblem != null)
                    {
                        var syntaxReference = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                        var fieldDeclarationSyntax = syntaxReference?.GetSyntax() as VariableDeclaratorSyntax;
                        var variableDeclaration = fieldDeclarationSyntax?.Parent as VariableDeclarationSyntax;
                        var typeSyntax = variableDeclaration?.Type;
                        var location = typeSyntax?.GetLocation() ?? fieldSymbol.Locations.FirstOrDefault();
                        context.ReportDiagnostic(Diagnostic.Create(typeProblem, location, fieldSymbol.Type.ToString(), fieldSymbol.ToString()));
                        continue;
                    }
                    if (fieldSymbol.DeclaredAccessibility != Accessibility.Public)
                    {
                        result = ClassFieldsInfo.NeuroWithPrivateFields;
                    }
                    else if (result != ClassFieldsInfo.NeuroWithPrivateFields)
                    {
                        result = ClassFieldsInfo.NeuroWithPublicOnly;
                    }
                    var tag = NeuroCodeGenUtils.GetNeuroTag(fieldAttribute);
                    if(tag == 0 || tag >= int.MaxValue)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidTagRangeRule, NeuroCodeGenUtils.GetLocation(fieldAttribute), fieldSymbol.ToString()));
                    }
                    if (tempTagDict.TryGetValue(tag, out var otherField))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(FieldTagConflictRule, NeuroCodeGenUtils.GetLocation(fieldAttribute), tag, fieldSymbol.ToString(), otherField));
                    }
                    else
                    {
                        tempTagDict.Add(tag, fieldSymbol.Name);
                    }
                }
            }
            return result;
        }
        
        public static bool HasFieldInitializer(IFieldSymbol fieldSymbol)
        {
            var declaringSyntaxReference = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (declaringSyntaxReference != null)
            {
                var fieldDeclarationSyntax = declaringSyntaxReference.GetSyntax() as VariableDeclaratorSyntax;
                if (fieldDeclarationSyntax?.Initializer != null)
                {
                    var initializerExpression = fieldDeclarationSyntax.Initializer.Value;
                    if (initializerExpression is BaseObjectCreationExpressionSyntax)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        enum ClassFieldsInfo
        {
            NoNeuro,
            NeuroWithPublicOnly,
            NeuroWithPrivateFields
        }

        DiagnosticDescriptor GetTypeProblem(ITypeSymbol classSymbol)
        {
            var typeKind = classSymbol.TypeKind;
            if (typeKind != TypeKind.Class 
                && typeKind != TypeKind.Struct 
                && typeKind != TypeKind.Interface 
                && typeKind != TypeKind.Enum)
            {
                return UnsupportedTypeRule;
            }
            if (classSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
            {
                if (NeuroCodeGenUtils.IsSupportedGenericType(namedTypeSymbol))
                {
                    var typeArguments = namedTypeSymbol.TypeArguments;
                    foreach (var typeArgument in typeArguments)
                    {
                        if(typeArgument is INamedTypeSymbol namedTypeArg && namedTypeArg.IsGenericType && !NeuroCodeGenUtils.IsReferenceType(namedTypeArg))
                        {
                            return UnsupportedTypeRule;
                        }
                        var argProblem = GetTypeProblem(typeArgument);
                        if(argProblem != null)
                        {
                            return argProblem;
                        }
                    }
                    if (namedTypeSymbol.Name == "Dictionary")
                    {
                        var keyArg = typeArguments[0];
                        if(!(keyArg is INamedTypeSymbol namedTypeArg) 
                           || (namedTypeArg.TypeKind != TypeKind.Struct &&namedTypeArg.TypeKind != TypeKind.Enum && namedTypeArg.SpecialType != SpecialType.System_String))
                        {
                            return InvalidDictionaryKeyTypeRule;
                        }
                    }
                    return null;
                }
                return UnsupportedTypeRule;
            }
            return null;

        }

        private void ProcessNeuroBaseClass(INamedTypeSymbol classSymbol, ClassFieldsInfo fieldsInfo, SymbolAnalysisContext context)
        {
            var classAttribute = NeuroCodeGenUtils.FindNeuroAttribute(classSymbol);
            if (classAttribute == null && fieldsInfo == ClassFieldsInfo.NoNeuro)
            {
                return;
            }
            if (fieldsInfo == ClassFieldsInfo.NeuroWithPrivateFields)
            {
                var syntax = classSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as TypeDeclarationSyntax;
                if(syntax != null && !syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(PartialClassRule, classSymbol.Locations.FirstOrDefault(), classSymbol.ToString()));
                }
            }
            uint tag = 0;
            if(classAttribute != null)
            {
                tag = NeuroCodeGenUtils.GetNeuroTag(classAttribute);
                if ((tag == 0 && classSymbol.TypeKind != TypeKind.Interface) || tag >= int.MaxValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidClassTagRangeRule, NeuroCodeGenUtils.GetLocation(classAttribute), classSymbol.ToString()));
                    return;
                }
            }
            var globalAttribute = NeuroCodeGenUtils.FindNeuroGlobalTypeAttribute(classSymbol);
            if (globalAttribute != null)
            {
                var globalId = NeuroCodeGenUtils.GetNeuroGlobalTypeId(globalAttribute);
                if (globalId == 0 || globalId >= int.MaxValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GlobalTypeRangeRule, NeuroCodeGenUtils.GetLocation(globalAttribute), classSymbol.ToString()));
                }
                else if (globalTypeNames.TryGetValue(globalId, out var otherName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(GlobalTypeConflictRule, NeuroCodeGenUtils.GetLocation(globalAttribute), globalId, classSymbol.ToString(), otherName));
                }
                else
                {
                    globalTypeNames[globalId] = classSymbol.ToString();
                }
            }
            else if (NeuroCodeGenUtils.IsReferencableType(classSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(RefsGlobalTypeRule, classSymbol.Locations.FirstOrDefault(), classSymbol.ToString()));
            }
            ISymbol baseClassSymbol = null;
            var baseSymbol = classSymbol.BaseType;
            while (baseSymbol != null)
            {
                if (NeuroCodeGenUtils.FindNeuroAttribute(baseSymbol) != null 
                    || baseSymbol.GetMembers()
                        .Where(m => m.Kind == SymbolKind.Field).Cast<IFieldSymbol>()
                        .Any(s => NeuroCodeGenUtils.FindNeuroAttribute(s) != null))
                {
                    baseClassSymbol = baseSymbol;
                    break;
                }
                baseSymbol = baseSymbol.BaseType;
            }
            if (baseClassSymbol != null)
            {
                if (classAttribute == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(MissingClassAttributeRule, classSymbol.Locations.FirstOrDefault(), classSymbol.ToString(), baseSymbol.ToString()));
                }
                else
                {
                    ValidateBaseClass(classSymbol, classAttribute, tag, baseClassSymbol, context);
                }
            }
            else if(tag > 0)
            {
                ValidateBaseClass(classSymbol, classAttribute, tag, classSymbol, context);
            }
        }

        void ValidateBaseClass(ISymbol classSymbol, AttributeData classAttribute, uint tag, ISymbol baseSymbol, SymbolAnalysisContext context)
        {
            if (!classTags.TryGetValue(baseSymbol, out var subList))
            {
                subList = new List<SymbolAndTag>();
                classTags[baseSymbol] = subList;
            }
            foreach (var attributeData in classSymbol.GetAttributes())
            {
                if (NeuroCodeGenUtils.IsReservedNeuroTagAttribute(attributeData.AttributeClass))
                {
                    var reservedTag = NeuroCodeGenUtils.GetNeuroTag(attributeData);
                    foreach (var symbolAndTag in subList)
                    {
                        if (symbolAndTag.Tag == reservedTag && symbolAndTag.Symbol != null)
                        {
                            ReportClassTagConflict(null, attributeData, symbolAndTag, context);
                            break;
                        }
                    }
                    subList.Add(new SymbolAndTag()
                    {
                        Attribute = attributeData,
                        Tag = reservedTag
                    });
                }
            }
            
            var selfIndex = -1;
            for (int index = 0, l = subList.Count; index < l; index++)
            {
                var symbolAndTag = subList[index];
                if (symbolAndTag.Tag == tag && !SymbolEqualityComparer.Default.Equals(symbolAndTag.Symbol, classSymbol))
                {
                    ReportClassTagConflict(classSymbol, classAttribute, symbolAndTag, context);
                }
                if (SymbolEqualityComparer.Default.Equals(symbolAndTag.Symbol, classSymbol))
                {
                    selfIndex = index;
                }
            }

            if(selfIndex < 0)
            {
                subList.Add(new SymbolAndTag()
                {
                    Symbol = classSymbol,
                    Attribute = classAttribute,
                    Tag = tag
                });
            }
            else
            {
                var existing = subList[selfIndex];
                existing.Tag = tag;
                subList[selfIndex] = existing;
            }
        }

        void ReportClassTagConflict(ISymbol classSymbol, AttributeData classAttribute, SymbolAndTag symbolAndTag, SymbolAnalysisContext context)
        {
            var otherSymbol = symbolAndTag.Symbol;
            var classSymbolName = classSymbol != null ? classSymbol.Name : "[ReservedTag]";
            var otherSymbolName = otherSymbol != null ? otherSymbol.Name : "[ReservedTag]";

            if (classSymbol != null)
            {
                if (otherSymbol != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClassTagConflictRule,
                        NeuroCodeGenUtils.GetLocation(classAttribute), symbolAndTag.Tag,
                        classSymbolName, otherSymbolName));
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClassTagReservedRule,
                        NeuroCodeGenUtils.GetLocation(classAttribute), symbolAndTag.Tag,
                        classSymbolName, NeuroCodeGenUtils.GetLocation(symbolAndTag.Attribute)));
                }
            }
            if (otherSymbol != null)
            {
                if (classSymbol != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClassTagConflictRule,
                        NeuroCodeGenUtils.GetLocation(symbolAndTag.Attribute), symbolAndTag.Tag,
                        otherSymbolName, classSymbolName));
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClassTagReservedRule,
                        NeuroCodeGenUtils.GetLocation(classAttribute), symbolAndTag.Tag,
                        otherSymbolName, NeuroCodeGenUtils.GetLocation(classAttribute)));
                }
            }
        }

        struct SymbolAndTag
        {
            public ISymbol Symbol;
            public AttributeData Attribute;
            public uint Tag;
        }
    }
}