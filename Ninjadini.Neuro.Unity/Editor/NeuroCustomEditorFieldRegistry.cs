using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ninjadini.Neuro.Editor
{
    public static class NeuroCustomEditorFieldRegistry
    {
        static Dictionary<Type, List<string>> fields = new Dictionary<Type, List<string>>();

        public static void RegisterFieldOf<T>(string fieldName)
        {
            var type = typeof(T);
            MemberInfo member = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (member == null)
            {
                member = type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            if (member == null)
            {
                throw new Exception($"Member with name {fieldName} not found in {type.FullName}");
            }
            if (!fields.TryGetValue(type, out var list))
            {
                list = new List<string>();
                fields.Add(type, list);
            }
            list.Add(member.Name);
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