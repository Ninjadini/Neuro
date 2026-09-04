using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Linq;
using System.Text;
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
        static readonly DiagnosticDescriptor UnsupportedNumberTypeRule = new DiagnosticDescriptor("Neuro102", "Unsupported number type", "Unsupported number type `{0}` found @ {1}. Whole numbers are stored as variable length ints, so a narrow type saves nothing - use int, uint, long or ulong. For char use string, for decimal use double or a long of scaled units.", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor InvalidDictionaryKeyTypeRule = new DiagnosticDescriptor("Neuro101", "Invalid dictionary key type", "Unsupported dictionary key type `{0}` found @ {1}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor InvalidTagRangeRule = new DiagnosticDescriptor(InvalidTagDiagnosticID, "Invalid field neuro tag", "Neuro field attribute tag of `{0}` must be between 1 and "+int.MaxValue+". {1}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor FieldTagConflictRule = new DiagnosticDescriptor(FieldTagConflictDiagnosticID, "Field attribute tag already used", "Neuro field attribute tag {0} of `{1}` is already used by another field `{2}`. {3}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor MissingClassAttributeRule = new DiagnosticDescriptor("Neuro404", "Missing neuro class attribute", "`{0}` needs neuro class attribute `[Neuro(#)]` because it's base class `{1}` is a Neuro class.", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor FastCodeGenClassAttributeRule = new DiagnosticDescriptor("Neuro406", "Missing neuro class attribute", "`{0}` has [Neuro] field(s) but no class level [Neuro(#)] attribute. " + NeuroCodeGenUtils.DefineSymbol_FastCodeGen + " is on, which requires every Neuro type to declare itself with a class level attribute.", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor MultipleBaseClassRootsRule = new DiagnosticDescriptor("Neuro405", "Multiple inheritance paths not supported", "`{0}` extends from multiple inheritance paths: `{1}` and `{2}`. This is not supported for now.", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor InvalidClassTagRangeRule = new DiagnosticDescriptor("Neuro002", "Invalid class neuro tag",  "Neuro class attribute tag must be between 0 and "+int.MaxValue+" @ {0}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor PartialClassRule = new DiagnosticDescriptor("Neuro101", "Non-partial Neuro class",  "{0} is not a partial class. It is required so Neuro can write to private fields without reflection.", "Syntax", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor ClassTagConflictRule = new DiagnosticDescriptor("Neuro303", "Class attribute tag already used", "Neuro class attribute tag {0} of `{1}` is already used by another class `{2}`. {3}", "Syntax", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor ClassTagReservedRule = new DiagnosticDescriptor("Neuro304", "Class attribute tag reserved", "Neuro class attribute tag {0} of `{1}` is marked as reserved `[ReservedNeuroTag({0})]`. {2}", "Syntax", DiagnosticSeverity.Error, true);
        /// `[Neuro(0)]` is not a tag, it is a question - answer it with the numbers this hierarchy has
        /// already spent so the next one can be typed straight in.
        public static readonly DiagnosticDescriptor ClassTagNotSetRule = new DiagnosticDescriptor("Neuro305", "Class attribute tag not set", "Neuro class attribute tag of `{0}` is not set. {1}", "Syntax", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor GlobalTypeIdNotSetRule = new DiagnosticDescriptor("Neuro313", "Global type id not set", "Neuro global type id of `{0}` is not set. {1}", "Syntax", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor GlobalTypeConflictRule = new DiagnosticDescriptor("Neuro310", "Global type id already used", "Neuro global type id {0} of `{1}` is already used by another class `{2}`. {3}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor GlobalTypeRangeRule = new DiagnosticDescriptor("Neuro311", "Invalid global neuro type id",  "Neuro global type id must be between 0 and "+int.MaxValue+" @ {0}", "Syntax", DiagnosticSeverity.Error, true);
        static readonly DiagnosticDescriptor RefsGlobalTypeRule = new DiagnosticDescriptor("Neuro312", "Global neuro type attribute missing",  "Neuro global type attribute `[NeuroGlobalType(#)]` is required in `{0}` because it is an IReferencable", "Syntax", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor ExceptionThrown = new DiagnosticDescriptor("Neuro911", "Exception was thrown while generating Neuro source", "Neuro codegen exception: {0}", "Syntax", DiagnosticSeverity.Error, true);

        [ThreadStatic]
        static Dictionary<uint, string> tempTagDict;
        [ThreadStatic]
        static List<NeuroTagReport.Entry> tempTagEntries;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            UnsupportedTypeRule,
            UnsupportedNumberTypeRule,
            InvalidDictionaryKeyTypeRule,
            ReadOnlyFieldRule, 
            ReadOnlyWithoutInitializerFieldRule,
            InvalidTagRangeRule, 
            FieldTagConflictRule, 
            MissingClassAttributeRule,
            FastCodeGenClassAttributeRule,
            MultipleBaseClassRootsRule,
            InvalidClassTagRangeRule, 
            PartialClassRule, 
            ClassTagConflictRule,
            ClassTagReservedRule,
            ClassTagNotSetRule,
            GlobalTypeConflictRule,
            GlobalTypeIdNotSetRule,
            GlobalTypeRangeRule,
            RefsGlobalTypeRule,
            ExceptionThrown);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            
            context.RegisterCompilationStartAction(compilationStart =>
            {
                var scanMode = GetScanMode(compilationStart.Compilation);
                if (scanMode == NeuroScanMode.Skip)
                {
                    return;
                }
                compilationStart.RegisterSymbolAction(symbolContext => ProcessClassOrStruct(symbolContext, scanMode), SymbolKind.NamedType);
                if (scanMode == NeuroScanMode.Fast)
                {
                    // Finding the fields that should have opted their type in by reading every type's members
                    // costs more than everything else this analyzer does put together, and almost every type
                    // it reads has no Neuro field at all. Asking for the attributes instead turns it into work
                    // proportional to the number of [Neuro] attributes actually written in the assembly.
                    compilationStart.RegisterSyntaxNodeAction(
                        nodeContext => ProcessFieldAttribute((AttributeSyntax)nodeContext.Node, nodeContext.SemanticModel, nodeContext.ReportDiagnostic, nodeContext.CancellationToken),
                        SyntaxKind.Attribute);
                }
            });
            
        }

        public static NeuroScanMode GetScanMode(Compilation compilation)
        {
            return NeuroCodeGenUtils.GetScanMode(compilation);
        }

        public void ProcessClassOrStruct(SymbolAnalysisContext context, NeuroScanMode scanMode)
        {
            var classSymbol = context.Symbol as INamedTypeSymbol;
            if (classSymbol == null)
            {
                return;
            }
            if (scanMode == NeuroScanMode.Fast && !ProcessFastCodeGenOptIn(classSymbol, context))
            {
                return;
            }
            var fieldsInfo = ProcessFields(classSymbol, context);
            ProcessNeuroBaseClass(classSymbol, fieldsInfo, context, scanMode);
        }

        /// A [Neuro] attribute on a field of a type that never declared itself a Neuro type. Reached from the
        /// attribute itself, so the check costs nothing for the types that have no Neuro attributes at all.
        public void ProcessFieldAttribute(AttributeSyntax attribute, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            if (!NeuroCodeGenUtils.IsNeuroAttributeNameSyntax(attribute))
            {
                return;
            }
            var attributeList = attribute.Parent as AttributeListSyntax;
            var fieldDeclaration = attributeList == null ? null : attributeList.Parent as FieldDeclarationSyntax;
            if (fieldDeclaration == null)
            {
                return;
            }
            var typeDeclaration = fieldDeclaration.Parent as TypeDeclarationSyntax;
            if (typeDeclaration == null)
            {
                return;
            }
            foreach (var modifier in fieldDeclaration.Modifiers)
            {
                // Static and const fields take no part in Neuro either way, so they can't be the mistake
                // this rule is looking for.
                if (modifier.IsKind(SyntaxKind.StaticKeyword) || modifier.IsKind(SyntaxKind.ConstKeyword))
                {
                    return;
                }
            }
            var classSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) as INamedTypeSymbol;
            if (classSymbol == null || HasNeuroTypeAttribute(classSymbol))
            {
                return;
            }
            reportDiagnostic(Diagnostic.Create(FastCodeGenClassAttributeRule, attribute.GetLocation(), classSymbol.ToString()));
        }

        static bool HasNeuroTypeAttribute(INamedTypeSymbol classSymbol)
        {
            return NeuroCodeGenUtils.FindNeuroAttribute(classSymbol) != null
                   || NeuroCodeGenUtils.FindNeuroGlobalTypeAttribute(classSymbol) != null;
        }

        /// Did this type opt in to Neuro? If it didn't, reports the subclass that should have and returns
        /// false so the caller can stop.
        bool ProcessFastCodeGenOptIn(INamedTypeSymbol classSymbol, SymbolAnalysisContext context)
        {
            if (HasNeuroTypeAttribute(classSymbol))
            {
                return true;
            }
            // The [Neuro] field with no class level attribute is reported by ProcessFieldAttribute, which the
            // driver reaches straight from the attribute rather than by reading this type's members.
            // A subclass of a Neuro class takes part in serialization whether or not it declares anything of
            // its own, so it has to opt in as well. Only class level attributes count here, which means no
            // one's members need reading to answer it.
            var baseClassSymbol = FindNeuroBaseType(classSymbol, NeuroScanMode.Fast);
            if (baseClassSymbol != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingClassAttributeRule, classSymbol.Locations.FirstOrDefault(), classSymbol.ToString(), baseClassSymbol.ToString()));
            }
            return false;
        }

        private ClassFieldsInfo ProcessFields(INamedTypeSymbol classSymbol, SymbolAnalysisContext context)
        {
            if (tempTagDict == null)
            {
                tempTagDict = new Dictionary<uint, string>();
                tempTagEntries = new List<NeuroTagReport.Entry>();
            }
            else
            {
                tempTagDict.Clear();
                tempTagEntries.Clear();
            }
            // Tag problems are held back until the whole class has been read: the message quotes every
            // tag in the class, and the fields after this one have not been looked at yet. Only
            // allocated when there is something to report, so a clean class pays nothing.
            List<PendingTagDiagnostic> pending = null;
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
                            //context.ReportDiagnostic(Diagnostic.Create(FieldTagConflictRule, NeuroCodeGenUtils.GetLocation(attributeData), tag, fieldSymbol.ToString(), otherField));
                        }
                        else
                        {
                            tempTagDict.Add(tag, "* reserved or deprecated *");
                            tempTagEntries.Add(new NeuroTagReport.Entry(tag, null));
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
                        // `[Neuro(0)]` is how you ask what is free - the report tells you. Nothing is
                        // added to the tag set, so it can't go on to look like a conflict as well.
                        Add(ref pending, new PendingTagDiagnostic(InvalidTagRangeRule, NeuroCodeGenUtils.GetLocation(fieldAttribute), fieldSymbol.ToString()));
                        continue;
                    }
                    if (tempTagDict.TryGetValue(tag, out var otherField))
                    {
                        Add(ref pending, new PendingTagDiagnostic(FieldTagConflictRule, NeuroCodeGenUtils.GetLocation(fieldAttribute), tag, fieldSymbol.ToString(), otherField));
                    }
                    else
                    {
                        tempTagDict.Add(tag, fieldSymbol.Name);
                        tempTagEntries.Add(new NeuroTagReport.Entry(tag, fieldSymbol.Name));
                    }
                }
            }
            if (pending != null)
            {
                var report = NeuroTagReport.Describe(tempTagEntries);
                foreach (var item in pending)
                {
                    context.ReportDiagnostic(item.Create(report));
                }
            }
            tempTagDict.Clear();
            tempTagEntries.Clear();
            return result;
        }

        static void Add(ref List<PendingTagDiagnostic> pending, PendingTagDiagnostic diagnostic)
        {
            if (pending == null)
            {
                pending = new List<PendingTagDiagnostic>();
            }
            pending.Add(diagnostic);
        }

        /// A tag diagnostic waiting on the used-tag report, which is only complete once every field in
        /// the class has been read. The report is always the last message argument.
        struct PendingTagDiagnostic
        {
            readonly DiagnosticDescriptor rule;
            readonly Location location;
            readonly object[] args;

            public PendingTagDiagnostic(DiagnosticDescriptor rule_, Location location_, params object[] args_)
            {
                rule = rule_;
                location = location_;
                args = args_;
            }

            public Diagnostic Create(string report)
            {
                var allArgs = new object[args.Length + 1];
                args.CopyTo(allArgs, 0);
                allArgs[args.Length] = report;
                return Diagnostic.Create(rule, location, allArgs);
            }
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
            switch (classSymbol.SpecialType)
            {
                // These would pass codegen and then fail at runtime with "type is not registered", so reject
                // them here instead. Enums backed by these are fine - the field type there is the enum.
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Char:
                case SpecialType.System_Decimal:
                    return UnsupportedNumberTypeRule;
            }
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

        static bool IsNeuroType(INamedTypeSymbol symbol, NeuroScanMode scanMode)
        {
            if (NeuroCodeGenUtils.FindNeuroAttribute(symbol) != null)
            {
                return true;
            }
            if (scanMode == NeuroScanMode.Fast)
            {
                // A class level attribute is the only way in, so members don't need reading.
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

        /// A class deriving from a Neuro class takes part in serialization whether or not it declares members
        /// of its own, so it still has to be checked for its `[Neuro(#)]` tag. Without this a field-less
        /// subclass would slip through and silently serialize as its base type.
        static ISymbol FindNeuroBaseType(INamedTypeSymbol classSymbol, NeuroScanMode scanMode)
        {
            var baseSymbol = classSymbol.BaseType;
            while (baseSymbol != null)
            {
                if (IsNeuroType(baseSymbol, scanMode))
                {
                    return baseSymbol;
                }
                baseSymbol = baseSymbol.BaseType;
            }
            foreach (var interfaceSymbol in classSymbol.AllInterfaces)
            {
                if (NeuroCodeGenUtils.FindNeuroAttribute(interfaceSymbol) != null)
                {
                    return interfaceSymbol;
                }
            }
            return null;
        }

        private void ProcessNeuroBaseClass(INamedTypeSymbol classSymbol, ClassFieldsInfo fieldsInfo, SymbolAnalysisContext context, NeuroScanMode scanMode)
        {
            var classAttribute = NeuroCodeGenUtils.FindNeuroAttribute(classSymbol);
            if (classAttribute == null && fieldsInfo == ClassFieldsInfo.NoNeuro && FindNeuroBaseType(classSymbol, scanMode) == null)
            {
                // nothing marks this type as taking part in Neuro, so leave it alone.
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
            }
            else if (NeuroCodeGenUtils.IsReferencableType(classSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(RefsGlobalTypeRule, classSymbol.Locations.FirstOrDefault(), classSymbol.ToString()));
            }
            ISymbol baseClassSymbol = null;
            var baseSymbol = classSymbol.BaseType;
            while (baseSymbol != null)
            {
                if (IsNeuroType(baseSymbol, scanMode))
                {
                    baseClassSymbol = baseSymbol;
                    break;
                }
                baseSymbol = baseSymbol.BaseType;
            }
            foreach (var interfaceSymbol in  classSymbol.AllInterfaces)
            {
                if (NeuroCodeGenUtils.FindNeuroAttribute(interfaceSymbol) != null)
                {
                    if (baseClassSymbol != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(MultipleBaseClassRootsRule, classSymbol.Locations.FirstOrDefault(), classSymbol.ToString(), baseClassSymbol.ToString(), interfaceSymbol.ToString()));
                    }
                    else
                    {
                        baseClassSymbol = interfaceSymbol;
                    }
                }
            }
            if (baseClassSymbol != null)
            {
                if (classAttribute == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(MissingClassAttributeRule, classSymbol.Locations.FirstOrDefault(), classSymbol.ToString(), baseClassSymbol.ToString()));
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
        }

        void ReportTagConflicts(List<SymbolAndTag> tags, CompilationAnalysisContext context, DiagnosticDescriptor tagConflict, DiagnosticDescriptor tagReserved)
        {
            for (var indexA = tags.Count - 1; indexA >= 0; indexA--)
            {
                var tagA = tags[indexA];
                for (var indexB = tags.Count - 1; indexB >= 0; indexB--)
                {
                    var tagB = tags[indexB];
                    if (tagA.Tag == tagB.Tag && !SymbolEqualityComparer.Default.Equals(tagA.Symbol, tagB.Symbol))
                    {
                        ReportTagConflict(tagA, tagB, tags, context, tagConflict, tagReserved);
                        break;
                    }
                }
            }
        }
        
        void ReportTagConflict(SymbolAndTag symbolAndTag1, SymbolAndTag symbolAndTag2, List<SymbolAndTag> tags, CompilationAnalysisContext context, DiagnosticDescriptor tagConflict, DiagnosticDescriptor tagReserved)
        {
            var classSymbol = symbolAndTag1.Symbol;
            if (classSymbol == null)
            {
                return;
            }
            var otherSymbol = symbolAndTag2.Symbol;
            if (otherSymbol != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(tagConflict,
                    NeuroCodeGenUtils.GetLocation(symbolAndTag1.Attribute), symbolAndTag2.Tag,
                    classSymbol.Name, otherSymbol.Name, CreateTagsList(tags)));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(tagReserved,
                    NeuroCodeGenUtils.GetLocation(symbolAndTag1.Attribute), symbolAndTag2.Tag,
                    classSymbol.Name, NeuroCodeGenUtils.GetLocation(symbolAndTag2.Attribute), CreateTagsList(tags)));
            }
        }

        string CreateTagsList(List<SymbolAndTag> tags)
        {
            var stringBuilder = new StringBuilder();
            foreach (var tag in tags)
            {
                var classSymbol = tag.Symbol;
                var classSymbolName = classSymbol != null ? classSymbol.Name : "[ReservedTag]";
                stringBuilder.Append(tag.Tag).Append(": ").AppendLine(classSymbolName);
            }
            return stringBuilder.ToString();
        }

        struct SymbolAndTag
        {
            public ISymbol Symbol;
            public AttributeData Attribute;
            public uint Tag;
        }
    }
}