using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ninjadini.Neuro.CodeGen
{
    /// A field initialiser is the serialization default: the readers write it back when the data does not
    /// carry the field, so it has to be handed to `neuro.Sync(...)` as an expression the generated file can
    /// compile on its own. That file has no `using` directives of the declaring file, hence the fully
    /// qualified names, and it is a different file to the one being read, hence rebuilding the initialiser
    /// rather than copying its text.
    /// Only the forms below are rebuilt. Anything else is `Neuro024` rather than a guess - the field would
    /// otherwise read back as `default` without saying so, and rewriting the initialiser as one of these is
    /// the author's call to make.
    internal static class NeuroDefaultValues
    {
        static readonly SymbolDisplayFormat FullTypeNameFormat = SymbolDisplayFormat.FullyQualifiedFormat;

        /// Same as <see cref="FullTypeNameFormat"/> but a member prints with the type that holds it -
        /// `global::UnityEngine.Vector2Int.one` rather than `one`.
        static readonly SymbolDisplayFormat FullMemberNameFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType);

        /// Whether the type can be handed a default at all. The defaulted `Sync` overload is constrained to
        /// `IEquatable<T>` - that is how a writer knows the field still holds its default and can be
        /// skipped - so a struct without it, `Nullable<>` included, goes to the overload that resets the
        /// field instead. A class typed field is read as null when the data omits it.
        public static bool CanCarryDefault(ITypeSymbol symbol)
        {
            if (symbol.TypeKind == TypeKind.Class || symbol.TypeKind == TypeKind.Interface)
            {
                return false;
            }
            if (symbol.TypeKind == TypeKind.Struct)
            {
                return symbol.Interfaces
                    .Any(i =>
                        i.IsGenericType
                        && i.Name == "IEquatable"
                        && i.ContainingNamespace?.Name == "System"
                        && (i.ContainingNamespace?.ContainingNamespace?.IsGlobalNamespace ?? false)
                        && i.TypeArguments.Length == 1
                        && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], symbol)
                    );
            }
            return true;
        }

        /// `= null` and `= default` ask for what a field with no default reads back as, so they are not
        /// the mistake <see cref="CanCarryDefault"/> is worth reporting.
        public static bool IsNullOrDefault(ExpressionSyntax expression)
        {
            return expression is LiteralExpressionSyntax literal
                   && (literal.IsKind(SyntaxKind.NullLiteralExpression) || literal.IsKind(SyntaxKind.DefaultLiteralExpression));
        }

        /// The rendered expression, or null when this initialiser is not one of the forms we rebuild.
        public static string Render(SemanticModel model, ExpressionSyntax expression)
        {
            switch (expression)
            {
                case LiteralExpressionSyntax literal:
                    // Written out as typed, so a `1.5f` stays a float and a `1u` stays unsigned.
                    return literal.ToString();
                case PrefixUnaryExpressionSyntax unary when unary.Operand is LiteralExpressionSyntax
                                                            && (unary.IsKind(SyntaxKind.UnaryMinusExpression)
                                                                || unary.IsKind(SyntaxKind.UnaryPlusExpression)):
                    return unary.ToString();
                case IdentifierNameSyntax _:
                case MemberAccessExpressionSyntax _:
                    // A static field or get-property - `Vector2Int.one`, `float.NaN`, an enum member, a const.
                    switch (model?.GetSymbolInfo(expression).Symbol)
                    {
                        case IFieldSymbol field when field.IsStatic || field.IsConst:
                            return field.ToDisplayString(FullMemberNameFormat);
                        case IPropertySymbol property when property.IsStatic && property.GetMethod != null:
                            return property.ToDisplayString(FullMemberNameFormat);
                    }
                    return null;
                case ObjectCreationExpressionSyntax creation:
                    return RenderCreation(model, creation);
            }
            return null;
        }

        /// `new Vector2Int(1, 1)`, where every argument is itself one of the rebuilt forms.
        static string RenderCreation(SemanticModel model, ObjectCreationExpressionSyntax creation)
        {
            var createdType = model?.GetTypeInfo(creation).Type;
            if (createdType == null || createdType.TypeKind == TypeKind.Error || creation.Initializer != null)
            {
                return null;
            }
            var result = new System.Text.StringBuilder("new ");
            result.Append(createdType.ToDisplayString(FullTypeNameFormat));
            result.Append("(");
            var arguments = creation.ArgumentList?.Arguments;
            for (var i = 0; arguments != null && i < arguments.Value.Count; i++)
            {
                var argument = arguments.Value[i];
                if (argument.NameColon != null || !argument.RefKindKeyword.IsKind(SyntaxKind.None))
                {
                    return null;
                }
                var rendered = Render(model, argument.Expression);
                if (rendered == null)
                {
                    return null;
                }
                if (i > 0)
                {
                    result.Append(", ");
                }
                result.Append(rendered);
            }
            result.Append(")");
            return result.ToString();
        }
    }
}
