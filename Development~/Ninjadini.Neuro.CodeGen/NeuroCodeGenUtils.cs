using System.Linq;
using Microsoft.CodeAnalysis;

namespace Ninjadini.Neuro.CodeGen
{
    internal static class NeuroCodeGenUtils
    {
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