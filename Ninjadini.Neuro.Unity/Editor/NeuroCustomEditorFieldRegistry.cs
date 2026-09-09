using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Ninjadini.Neuro.Editor
{
    [InitializeOnLoad]
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public static class NeuroCustomEditorFieldRegistry
    {
        static Dictionary<Type, List<string>> fields = new Dictionary<Type, List<string>>();
        /// value = show field names, see InspectorStyleAttribute.InlineFieldNames
        static Dictionary<Type, bool> inlineTypes = new Dictionary<Type, bool>();

        static NeuroCustomEditorFieldRegistry()
        {
            NeuroSyncEditorFields.SetEditorHook((type, name, isProperty) =>
            {
                RegisterMember(type, name, isProperty);
            });
            NeuroSyncEditorFields.SetInlineEditorHook(RegisterInline);
        }

        /// Editor-side equivalent of <see cref="NeuroSyncEditorFields.SetInline"/>.
        public static void RegisterInline(Type type, bool showFieldNames = false)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            inlineTypes[type] = showFieldNames;
        }

        /// True if the type was registered via <see cref="NeuroSyncEditorFields.SetInline"/> or
        /// <see cref="RegisterInline"/>. The [InspectorStyle(Inline = true)] attribute is checked separately
        /// by ObjectInspector.IsInlineType, which is what drawing code should call.
        public static bool IsRegisteredInline(Type type, out bool showFieldNames) => inlineTypes.TryGetValue(type, out showFieldNames);

        public static void RegisterFieldOf<T>(string fieldName)
        {
            RegisterMember(typeof(T), fieldName, null);
        }

        static void RegisterMember(Type type, string memberName, bool? isProperty)
        {
            MemberInfo member = null;
            if (isProperty != true)
            {
                member = type.GetField(memberName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            if (member == null && isProperty != false)
            {
                member = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            if (member == null)
            {
                throw new Exception($"Member with name {memberName} not found in {type.FullName}");
            }
            if (!fields.TryGetValue(type, out var list))
            {
                list = new List<string>();
                fields.Add(type, list);
            }
            if (!list.Contains(member.Name))
            {
                list.Add(member.Name);
            }
        }

        public static bool IsNameCustomField(Type type, string name)
        {
            while (type != null)
            {
                if (fields.TryGetValue(type, out var list))
                {
                    if (list.Contains(name))
                    {
                        return true;
                    }
                }
                type = type.BaseType;
            }
            return false;
        }
    }
}