using System.Linq;
using System.Text;
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
    /// An initialiser that can not be rebuilt is a data trap rather than a compile problem - the field would
    /// silently read back as `default` - so <see cref="NeuroSourceAnalyzer"/> reports those as `Neuro024`.
    internal static class NeuroDefaultValues
    {
        const int MaxDepth = 4;

        static readonly SymbolDisplayFormat FullTypeNameFormat = SymbolDisplayFormat.FullyQualifiedFormat;

        /// Same as <see cref="FullTypeNameFormat"/> but a member prints with the type that holds it -
        /// `global::UnityEngine.Vector2Int.one` rather than `one`.
        static readonly SymbolDisplayFormat FullMemberNameFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType);

        /// A rebuilt initialiser. <see cref="HasCall"/> says it runs something - a property getter, a method
        /// or a constructor - rather than naming a value, which is what makes it worth holding in a static
        /// readonly field instead of running it on every Sync of every object.
        public readonly struct Rendered
        {
            public readonly string Expression;
            public readonly bool HasCall;

            public Rendered(string expression, bool hasCall)
            {
                Expression = expression;
                HasCall = hasCall;
            }

            public bool IsValid => Expression != null;
        }

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
            var unwrapped = expression;
            while (unwrapped is ParenthesizedExpressionSyntax parenthesized)
            {
                unwrapped = parenthesized.Expression;
            }
            return unwrapped is DefaultExpressionSyntax
                   || (unwrapped is LiteralExpressionSyntax literal
                       && (literal.IsKind(SyntaxKind.NullLiteralExpression) || literal.IsKind(SyntaxKind.DefaultLiteralExpression)));
        }

        /// The rebuilt initialiser, or an invalid <see cref="Rendered"/> when it is not a form we rebuild.
        public static Rendered Render(SemanticModel model, ExpressionSyntax expression, ITypeSymbol type)
        {
            return Render(model, expression, type, 0);
        }

        static Rendered Render(SemanticModel model, ExpressionSyntax expression, ITypeSymbol type, int depth)
        {
            if (expression == null || depth > MaxDepth)
            {
                return default;
            }
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    return Render(model, parenthesized.Expression, type, depth + 1);
                case LiteralExpressionSyntax literal:
                    // Written out as typed, so a `1.5f` stays a float and a `1u` stays unsigned.
                    return new Rendered(literal.ToString(), false);
                case PrefixUnaryExpressionSyntax unary when unary.Operand is LiteralExpressionSyntax
                                                            && (unary.IsKind(SyntaxKind.UnaryMinusExpression)
                                                                || unary.IsKind(SyntaxKind.UnaryPlusExpression)):
                    return new Rendered(unary.ToString(), false);
                case DefaultExpressionSyntax _:
                    return new Rendered("default", false);
                case IdentifierNameSyntax _:
                case MemberAccessExpressionSyntax _:
                    var member = RenderMember(model, expression);
                    return member.IsValid ? member : RenderConstant(model, expression, type);
                case InvocationExpressionSyntax invocation:
                    var invoked = RenderInvocation(model, invocation, depth);
                    return invoked.IsValid ? invoked : RenderConstant(model, expression, type);
                case ObjectCreationExpressionSyntax creation:
                    return RenderCreation(model, expression, creation.ArgumentList, creation.Initializer, type, depth);
                case ImplicitObjectCreationExpressionSyntax implicitCreation:
                    return RenderCreation(model, expression, implicitCreation.ArgumentList, implicitCreation.Initializer, type, depth);
            }
            return RenderConstant(model, expression, type);
        }

        /// A static field or get-property - `Vector2Int.one`, `float.NaN`, an enum member, a const. A field
        /// is a value to load; a property is a call, however small it looks.
        static Rendered RenderMember(SemanticModel model, ExpressionSyntax expression)
        {
            switch (model?.GetSymbolInfo(expression).Symbol)
            {
                case IFieldSymbol field when (field.IsStatic || field.IsConst) && IsReachable(model, field):
                    return new Rendered(field.ToDisplayString(FullMemberNameFormat), false);
                case IPropertySymbol property when property.IsStatic && property.GetMethod != null && IsReachable(model, property):
                    return new Rendered(property.ToDisplayString(FullMemberNameFormat), true);
            }
            return default;
        }

        /// A static method - `FPUtils.FromInt(10)`, `TimeSpan.FromSeconds(0.25)`. It runs where the
        /// initialiser would have, so it is the same call the field was declared with.
        static Rendered RenderInvocation(SemanticModel model, InvocationExpressionSyntax invocation, int depth)
        {
            if (!(model?.GetSymbolInfo(invocation).Symbol is IMethodSymbol method)
                || !method.IsStatic
                || method.IsGenericMethod
                || method.MethodKind != MethodKind.Ordinary
                || !IsReachable(model, method))
            {
                return default;
            }
            var arguments = RenderArguments(model, invocation.ArgumentList, depth);
            return arguments == null ? default : new Rendered(method.ToDisplayString(FullMemberNameFormat) + arguments, true);
        }

        static Rendered RenderCreation(SemanticModel model, ExpressionSyntax expression, ArgumentListSyntax argumentList, InitializerExpressionSyntax initializer, ITypeSymbol type, int depth)
        {
            if (initializer != null)
            {
                // `new Thing { X = 1 }` - the member names would have to be resolved as well, and no one
                // has needed it. It reads as a supported initialiser, so the analyzer says otherwise.
                return default;
            }
            var createdType = model?.GetTypeInfo(expression).Type ?? type;
            if (createdType == null || createdType.TypeKind == TypeKind.Error)
            {
                return default;
            }
            var arguments = RenderArguments(model, argumentList, depth);
            return arguments == null
                ? default
                : new Rendered("new " + createdType.ToDisplayString(FullTypeNameFormat) + arguments, true);
        }

        /// The bracketed argument list, or null when one of the arguments is not renderable.
        static string RenderArguments(SemanticModel model, ArgumentListSyntax argumentList, int depth)
        {
            var result = new StringBuilder("(");
            var arguments = argumentList?.Arguments;
            for (var i = 0; arguments != null && i < arguments.Value.Count; i++)
            {
                var argument = arguments.Value[i];
                if (argument.NameColon != null || !argument.RefKindKeyword.IsKind(SyntaxKind.None))
                {
                    return null;
                }
                var argumentType = model?.GetTypeInfo(argument.Expression).ConvertedType;
                var rendered = Render(model, argument.Expression, argumentType, depth + 1);
                if (!rendered.IsValid)
                {
                    return null;
                }
                if (i > 0)
                {
                    result.Append(", ");
                }
                result.Append(rendered.Expression);
            }
            result.Append(")");
            return result.ToString();
        }

        /// Anything else the compiler can fold to a constant - `1 + 2`, `MaxHealth / 2`. The cast carries
        /// the type the literal alone would lose, e.g. `1.5` as a float.
        static Rendered RenderConstant(SemanticModel model, ExpressionSyntax expression, ITypeSymbol type)
        {
            if (type == null || type.TypeKind == TypeKind.Error || !IsReachable(model, type))
            {
                return default;
            }
            var constant = model?.GetConstantValue(expression) ?? default;
            if (!constant.HasValue || constant.Value == null)
            {
                return default;
            }
            switch (constant.Value)
            {
                // Not finite numbers have no literal form - `float.NaN` and friends arrive as members.
                case float floatValue when float.IsNaN(floatValue) || float.IsInfinity(floatValue):
                case double doubleValue when double.IsNaN(doubleValue) || double.IsInfinity(doubleValue):
                    return default;
            }
            var text = SymbolDisplay.FormatPrimitive(constant.Value, true, false);
            if (string.IsNullOrEmpty(text))
            {
                return default;
            }
            return new Rendered("(" + type.ToDisplayString(FullTypeNameFormat) + ")(" + text + ")", false);
        }

        /// Whether the generated file - a static class of its own in the same assembly - can name it.
        public static bool IsReachable(SemanticModel model, ISymbol symbol)
        {
            for (var current = symbol; current != null && current.Kind != SymbolKind.Namespace; current = current.ContainingSymbol)
            {
                switch (current.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                    case Accessibility.NotApplicable:
                        break;
                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                        if (!SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, model?.Compilation.Assembly))
                        {
                            return false;
                        }
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }
    }
}
