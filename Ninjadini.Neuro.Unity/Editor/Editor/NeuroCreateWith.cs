using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    /// <summary>
    /// Runs a field's <see cref="NeuroCreateWithAttribute"/> method for the editor's creation paths, and checks
    /// every such attribute resolves (<see cref="FindProblems"/>, run by <see cref="NeuroContentTestsRunner"/>).
    /// </summary>
    public static class NeuroCreateWith
    {
        /// <summary>
        /// A new value for a slot of <paramref name="slotType"/> under <paramref name="data"/>'s field - the field
        /// itself, or one of its elements. False when the field has no attribute, or its method makes something
        /// that does not go in this slot (an element's method, asked for the list). A method that cannot be found
        /// or that throws is logged and also false, so the caller's plain creation still happens.
        /// </summary>
        public static bool TryCreate(ObjectInspector.Data data, Type slotType, out object result)
        {
            result = null;
            var attribute = data.MemberInfo?.GetCustomAttribute<NeuroCreateWithAttribute>(true);
            if (attribute == null)
            {
                return false;
            }
            var method = Resolve(data.MemberInfo, attribute, out var problem);
            if (method == null)
            {
                Debug.LogError(problem);
                return false;
            }
            if (!Fits(method.ReturnType, slotType))
            {
                return false;
            }
            try
            {
                var parameters = method.GetParameters();
                var args = parameters.Length == 0
                    ? null
                    : new[] { parameters[0].ParameterType.IsInstanceOfType(data.Owner) ? data.Owner : null };
                result = method.Invoke(null, args);
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException ?? e);
                return false;
            }
            return result != null;
        }

        /// <summary>One line per [NeuroCreateWith] in the loaded assemblies that names no usable method.</summary>
        public static List<string> FindProblems()
        {
            var result = new List<string>();
            foreach (var field in TypeCache.GetFieldsWithAttribute<NeuroCreateWithAttribute>())
            {
                var attribute = field.GetCustomAttribute<NeuroCreateWithAttribute>(true);
                var method = Resolve(field, attribute, out var problem);
                if (method == null)
                {
                    result.Add(problem);
                }
                else if (!Fits(method.ReturnType, field.FieldType) && !Fits(method.ReturnType, ElementType(field.FieldType)))
                {
                    result.Add($"{Describe(field)}: {method.Name} returns {method.ReturnType.Name}, which is neither " +
                               $"a {field.FieldType.Name} nor one of its elements.");
                }
            }
            return result;
        }

        static MethodInfo Resolve(MemberInfo member, NeuroCreateWithAttribute attribute, out string problem)
        {
            problem = null;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            // walked by hand: FlattenHierarchy leaves out a base type's private statics.
            for (var type = attribute.DeclaringType ?? member.DeclaringType; type != null; type = type.BaseType)
            {
                var candidates = type.GetMethods(flags).Where(m => m.Name == attribute.MethodName).ToArray();
                if (candidates.Length == 0)
                {
                    continue;
                }
                var method = candidates[0];
                if (candidates.Length > 1)
                {
                    problem = $"{Describe(member)}: {type.Name}.{method.Name} is overloaded, give it one signature.";
                }
                else if (method.ReturnType == typeof(void) || method.GetParameters().Length > 1 || method.ContainsGenericParameters)
                {
                    problem = $"{Describe(member)}: {type.Name}.{method.Name} must return the new value and take " +
                              "nothing, or only the field's owner.";
                }
                return problem == null ? method : null;
            }
            problem = $"{Describe(member)}: no static method {attribute.MethodName} on " +
                      $"{(attribute.DeclaringType ?? member.DeclaringType)?.Name} or its base types.";
            return null;
        }

        static bool Fits(Type returnType, Type slotType)
        {
            if (slotType == null)
            {
                return false;
            }
            returnType = Nullable.GetUnderlyingType(returnType) ?? returnType;
            slotType = Nullable.GetUnderlyingType(slotType) ?? slotType;
            return slotType.IsAssignableFrom(returnType);
        }

        /// A list's or array's element, a dictionary's value. Null for anything else.
        static Type ElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            return type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
                ? type.GetGenericArguments().Last()
                : null;
        }

        static string Describe(MemberInfo member) => $"[NeuroCreateWith] on {member.DeclaringType?.Name}.{member.Name}";
    }
}
