using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ninjadini.Neuro.CodeGen
{
    internal static class NeuroCodeGenUtils
    {
        public const string DefineSymbol_FastCodeGen = "NEURO_FAST_CODEGEN";
        /// The original name of <see cref="DefineSymbol_FastCodeGen"/>, still honoured so existing projects keep working.
        public const string DefineSymbol_SelectiveAssemblies = "NEURO_SELECTIVE_ASSEMBLIES";
        public const string Name_NeuroAttribute = "NeuroAttribute";
        public const string Name_NeuroAttribute_Tag = "Tag";
        public const string Name_INeuroCustomTypesRegistryHook = "INeuroCustomTypesRegistryHook";
        public const string Name_INeuroPoolable = "INeuroPoolable";
        public const string Name_ReservedNeuroTagAttribute = "ReservedNeuroTagAttribute";
        public const string Name_NeuroGlobalTypeAttribute = "NeuroGlobalTypeAttribute";
        public const string Name_NeuroGlobalTypeAttribute_Id = "Id";
        public const string Name_IReferencable = "IReferencable";
        public const string Name_Referencable = "Referencable";
        public const string Name_ISingletonReferencable = "ISingletonReferencable";
        
        static readonly SymbolDisplayFormat fullNameFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        public static NeuroScanMode GetScanMode(Compilation compilation)
        {
            var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options;
            if (parseOptions == null)
            {
                return NeuroScanMode.Full;
            }
            var fastCodeGen = false;
            foreach (var define in parseOptions.PreprocessorSymbolNames)
            {
                if (define == DefineSymbol_FastCodeGen || define == DefineSymbol_SelectiveAssemblies)
                {
                    fastCodeGen = true;
                    break;
                }
            }
            if (!fastCodeGen)
            {
                return NeuroScanMode.Full;
            }
            foreach (var attributeData in compilation.Assembly.GetAttributes())
            {
                if (IsNeuroAttribute(attributeData.AttributeClass))
                {
                    return NeuroScanMode.Fast;
                }
            }
            return NeuroScanMode.Skip;
        }

        // --- Syntax only checks -------------------------------------------------------------------------
        // Asking the semantic model about a type forces Roslyn to bind it, and in a real project almost every
        // type has nothing to do with Neuro. These read the source text instead, so the expensive question is
        // only asked about types that could plausibly answer yes.
        // The trade off is that an attribute reached through a `using` alias is not recognised here.

        /// Does the declaration carry an attribute that could make this a Neuro type?
        public static bool HasNeuroTypeAttributeSyntax(TypeDeclarationSyntax typeDeclaration)
        {
            foreach (var attributeList in typeDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = GetRightMostName(attribute.Name);
                    if (name == "Neuro" || name == Name_NeuroAttribute
                        || name == "NeuroGlobalType" || name == Name_NeuroGlobalTypeAttribute
                        || name == "ReservedNeuroTag" || name == Name_ReservedNeuroTagAttribute)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// Does any field of the declaration carry a [Neuro] attribute?
        public static bool HasNeuroFieldAttributeSyntax(TypeDeclarationSyntax typeDeclaration)
        {
            foreach (var member in typeDeclaration.Members)
            {
                var fieldDeclaration = member as FieldDeclarationSyntax;
                if (fieldDeclaration == null)
                {
                    continue;
                }
                foreach (var attributeList in fieldDeclaration.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var name = GetRightMostName(attribute.Name);
                        if (name == "Neuro" || name == Name_NeuroAttribute)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// Is this declaration a <see cref="Name_INeuroCustomTypesRegistryHook"/>? Hooks carry no attribute,
        /// so the base list is the only thing that can give them away without binding.
        public static bool HasRegistryHookBaseSyntax(TypeDeclarationSyntax typeDeclaration)
        {
            if (typeDeclaration.BaseList == null)
            {
                return false;
            }
            foreach (var baseType in typeDeclaration.BaseList.Types)
            {
                if (GetRightMostName(baseType.Type) == Name_INeuroCustomTypesRegistryHook)
                {
                    return true;
                }
            }
            return false;
        }

        /// The widest possible net, for when fast code gen is off: anything attributed, anything with a base
        /// list (it could inherit its Neuro-ness, or be a registry hook or a referencable), is worth binding.
        public static bool CouldBeNeuroTypeSyntax(TypeDeclarationSyntax typeDeclaration)
        {
            if (typeDeclaration.AttributeLists.Count > 0 || typeDeclaration.BaseList != null)
            {
                return true;
            }
            foreach (var member in typeDeclaration.Members)
            {
                var fieldDeclaration = member as FieldDeclarationSyntax;
                if (fieldDeclaration != null && fieldDeclaration.AttributeLists.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// Is this attribute written as [Neuro...]? Name only, nothing is bound.
        public static bool IsNeuroAttributeNameSyntax(AttributeSyntax attribute)
        {
            var name = GetRightMostName(attribute.Name);
            return name == "Neuro" || name == Name_NeuroAttribute;
        }

        /// `Ninjadini.Neuro.Neuro` -> `Neuro`. Null for anything that isn't a plain name.
        static string GetRightMostName(TypeSyntax typeSyntax)
        {
            while (true)
            {
                var qualifiedName = typeSyntax as QualifiedNameSyntax;
                if (qualifiedName != null)
                {
                    typeSyntax = qualifiedName.Right;
                    continue;
                }
                var aliasQualifiedName = typeSyntax as AliasQualifiedNameSyntax;
                if (aliasQualifiedName != null)
                {
                    typeSyntax = aliasQualifiedName.Name;
                    continue;
                }
                var simpleName = typeSyntax as SimpleNameSyntax;
                return simpleName?.Identifier.ValueText;
            }
        }

        public static string GetFullName(ITypeSymbol symbol)
        {
            return symbol.IsValueType ? symbol.ToString() : symbol.ToDisplayString(fullNameFormat);
        }

        public static Location GetLocation(AttributeData attributeData)
        {
            return attributeData?.ApplicationSyntaxReference.GetSyntax().GetLocation();
        }
        
        public static AttributeData FindNeuroAttribute(ISymbol symbol)
        {
            foreach (var attributeData in symbol.GetAttributes())
            {
                if (IsNeuroAttribute(attributeData.AttributeClass))
                {
                    return attributeData;
                }
            }
            return null;
        }

        public static uint GetNeuroTag(AttributeData attributeData)
        {
            return GetAttributeUintWithKey(attributeData, Name_NeuroAttribute_Tag);
        }

        public static uint GetAttributeUintWithKey(AttributeData attributeData, string key)
        {
            if (attributeData == null)
            {
                return 0;
            }
            var constructorArguments = attributeData.ConstructorArguments;
            object result = null;
            if (constructorArguments.Length > 0)
            {
                result = constructorArguments[0].Value;
            }
            else
            {
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.Key == key)
                    {
                        result = namedArgument.Value.Value;
                    }
                }
            }
            return result is uint u ? u : 0;
        }

        public static bool IsNeuroAttribute(INamedTypeSymbol symbol)
        {
            return symbol?.Name == Name_NeuroAttribute && IsNeuroNameSpace(symbol.ContainingNamespace);
        }

        public static bool IsReservedNeuroTagAttribute(INamedTypeSymbol symbol)
        {
            return symbol?.Name == Name_ReservedNeuroTagAttribute && IsNeuroNameSpace(symbol.ContainingNamespace);
        }
        

        public static bool IsNeuroGlobalTypeAttribute(INamedTypeSymbol symbol)
        {
            return symbol?.Name == Name_NeuroGlobalTypeAttribute && IsNeuroNameSpace(symbol.ContainingNamespace);
        }
        public static AttributeData FindNeuroGlobalTypeAttribute(ISymbol symbol)
        {
            foreach (var attributeData in symbol.GetAttributes())
            {
                if (IsNeuroGlobalTypeAttribute(attributeData.AttributeClass))
                {
                    return attributeData;
                }
            }
            return null;
        }
        public static uint GetNeuroGlobalTypeId(AttributeData attributeData)
        {
            return GetAttributeUintWithKey(attributeData, Name_NeuroGlobalTypeAttribute_Id);
        }
        
        public static bool IsNeuroCustomTypesRegisteryHook(INamedTypeSymbol symbol)
        {
            return symbol.Interfaces
                .Any(i =>
                    i.Name == Name_INeuroCustomTypesRegistryHook &&
                    IsNeuroNameSpace(i.ContainingNamespace)
                );
        }
            
        public static bool IsPoolableNeuroType(INamedTypeSymbol symbol)
        {
            return symbol.Interfaces
                .Any(i =>
                    i.Name == Name_INeuroPoolable &&
                    IsNeuroNameSpace(i.ContainingNamespace)
                );
        }
            
        public static bool IsReferencableType(INamedTypeSymbol symbol)
        {
            var baseType = symbol.BaseType;
            if (baseType != null && baseType.Name == Name_Referencable && IsNeuroNameSpace(baseType.ContainingNamespace))
            {
                return true;
            }
            if (symbol.Interfaces
                .Any(i =>
                    IsNeuroNameSpace(i.ContainingNamespace) &&
                    (i.Name == Name_IReferencable || i.Name == Name_ISingletonReferencable)
                ))
            {
                return !(symbol.Name == Name_Referencable && IsNeuroNameSpace(symbol.ContainingNamespace));
            }
            return false;
        }
        
        public static bool IsNeuroNameSpace(INamespaceSymbol ns)
        {
            if (ns?.Name == "Neuro")
            {
                ns = ns.ContainingNamespace;
                if (ns?.Name == "Ninjadini")
                {
                    return ns.ContainingNamespace?.IsGlobalNamespace ?? true;
                }
            }
            return false;
        }
        
        public static bool IsSupportedGenericType(INamedTypeSymbol typeSymbol)
        {
            if ((typeSymbol.Name == "List" || typeSymbol.Name == "Dictionary") && IsNameSpaceReversed(typeSymbol.ContainingNamespace, "Generic", "Collections", "System"))
            {
                return true;
            }
            if (IsReferenceType(typeSymbol))
            {
                return true;
            }
            if (typeSymbol.Name == "Nullable" && IsNameSpaceReversed(typeSymbol.ContainingNamespace, "System"))
            {
                return true;
            }
            return false;
        }

        public static bool IsReferenceType(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.Name == "Reference" && IsNeuroNameSpace(typeSymbol.ContainingNamespace);
        }

        public static bool IsNameSpaceReversed(INamespaceSymbol ns, string part1, string part2 = null, string part3 = null)
        {
            if(ns?.Name == part1)
            {
                ns = ns.ContainingNamespace;
                if (string.IsNullOrEmpty(part2))
                {
                    return ns.ContainingNamespace?.IsGlobalNamespace ?? true;
                }
                if (ns?.Name == part2)
                {
                    ns = ns.ContainingNamespace;
                    if (string.IsNullOrEmpty(part3))
                    {
                        return ns.ContainingNamespace?.IsGlobalNamespace ?? true;
                    }
                    if (ns?.Name == part3)
                    {
                        return ns.ContainingNamespace?.IsGlobalNamespace ?? true;
                    }
                }
            }
            return false;
        }
    }
}