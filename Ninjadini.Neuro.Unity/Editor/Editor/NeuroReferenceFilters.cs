using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninjadini.Neuro.Sync;
using UnityEditor;

namespace Ninjadini.Neuro.Editor
{
    /// <summary>
    /// Finds the <see cref="NeuroReferenceFilterAttribute"/>s that govern a reference, for the Neuro editor,
    /// the Unity property drawer and the content validator alike. Rule: the nearest member on the way from
    /// the reference up to the root that carries any filter attribute wins outright; list and dictionary
    /// elements have no member of their own and fall through to the collection's field.
    /// </summary>
    public static class NeuroReferenceFilters
    {
        const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>The filter attributes declared on this member, or null if it has none.</summary>
        public static NeuroReferenceFilterAttribute[] GetOwn(MemberInfo member)
        {
            if (member == null)
            {
                return null;
            }
            var result = member.GetCustomAttributes<NeuroReferenceFilterAttribute>(true).ToArray();
            return result.Length > 0 ? result : null;
        }

        /// <summary>Those of <paramref name="filters"/> that apply to references of <paramref name="refType"/>.</summary>
        public static NeuroReferenceFilterAttribute[] Applicable(IEnumerable<NeuroReferenceFilterAttribute> filters, Type refType)
        {
            if (filters == null)
            {
                return null;
            }
            var result = filters.Where(f => refType == null || f.AppliesTo(refType)).ToArray();
            return result.Length > 0 ? result : null;
        }

        /// <summary>A predicate over items that every filter in <paramref name="filters"/> accepts, or null.</summary>
        public static Func<IReferencable, bool> ToPredicate(NeuroReferenceFilterAttribute[] filters, NeuroReferences references)
        {
            if (filters == null || filters.Length == 0)
            {
                return null;
            }
            if (filters.Length == 1)
            {
                var single = filters[0];
                return item => single.Include(item, references);
            }
            return item => filters.All(f => f.Include(item, references));
        }

        /// <summary>"[StatTypeTag] [Other]" for the footer.</summary>
        public static string DisplayName(IEnumerable<NeuroReferenceFilterAttribute> filters)
        {
            return filters == null ? null : string.Join(" ", filters.Select(f => "[" + TrimAttributeSuffix(f.GetType().Name) + "]"));
        }

        public static string TrimAttributeSuffix(string name)
        {
            const string suffix = "Attribute";
            return name.EndsWith(suffix) && name.Length > suffix.Length ? name.Substring(0, name.Length - suffix.Length) : name;
        }

        public static MemberInfo FindMember(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name, MemberFlags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
                var property = t.GetProperty(name, MemberFlags | BindingFlags.DeclaredOnly);
                if (property != null)
                {
                    return property;
                }
            }
            return null;
        }

        /// <summary>
        /// Filters governing the reference at the top of a content validator stack: walks from the reference's
        /// own field outwards until a member with filter attributes is found.
        /// </summary>
        public static NeuroReferenceFilterAttribute[] FromStack(IReadOnlyList<NeuroVisitor.StackItem> stack)
        {
            if (stack == null)
            {
                return null;
            }
            for (var i = stack.Count - 1; i >= 1; i--)
            {
                var item = stack[i];
                if (item.ListIndex.HasValue || string.IsNullOrEmpty(item.Name))
                {
                    continue; // a collection element: its field is the collection's, one level up
                }
                var parentObj = stack[i - 1].Object;
                if (parentObj == null)
                {
                    continue;
                }
                var own = GetOwn(FindMember(parentObj.GetType(), item.Name));
                if (own != null)
                {
                    return own;
                }
            }
            return null;
        }

        /// <summary>
        /// Filters governing a serialized property: walks its propertyPath from the reference back to the
        /// target object, so an attribute on an enclosing list or struct field is honoured.
        /// </summary>
        public static NeuroReferenceFilterAttribute[] FromSerializedProperty(SerializedProperty property)
        {
            var target = property?.serializedObject?.targetObject;
            if (target == null)
            {
                return null;
            }
            var path = property.propertyPath;
            // the Reference<T> struct's own RefId child may be what we were handed
            var refIdSuffix = "." + NeuroConstants.Reference_RefId_FieldName;
            if (path.EndsWith(refIdSuffix))
            {
                path = path.Substring(0, path.Length - refIdSuffix.Length);
            }
            var segments = path.Split('.');
            var members = new List<MemberInfo>();
            var type = target.GetType();
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (segment == "Array" && i + 1 < segments.Length && segments[i + 1].StartsWith("data["))
                {
                    i++;
                    type = type.IsArray ? type.GetElementType() : type.GetGenericArguments().FirstOrDefault();
                    continue;
                }
                var member = FindMember(type, segment);
                if (member == null)
                {
                    break;
                }
                members.Add(member);
                type = member is FieldInfo f ? f.FieldType : ((PropertyInfo)member).PropertyType;
            }
            for (var i = members.Count - 1; i >= 0; i--)
            {
                var own = GetOwn(members[i]);
                if (own != null)
                {
                    return own;
                }
            }
            return null;
        }
    }
}
