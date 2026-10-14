using System;
using System.Collections.Generic;

namespace Ninjadini.Neuro.Sync
{
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public static class NeuroSyncEnumTypes<T>
    {
        static Func<T, int> _getInt;
        static Func<int, T> _getEnum;
        static Dictionary<int, string> enumNames;

        public static bool IsEmpty()
        {
            return _getInt == null;
        }
        
        public static void Register(Func<T, int> getInt, Func<int, T> getEnum)
        {
            _getInt = getInt;
            _getEnum = getEnum;
        }

        public static int GetInt(T value)
        {
            if (_getInt == null)
            {
                EnsureRegistered();
            }
            return _getInt(value);
        }
        
        public static T GetEnum(int value)
        {
            if (_getEnum == null)
            {
                EnsureRegistered();
            }
            return _getEnum(value);
        }

        static void EnsureRegistered()
        {
            NeuroSyncTypes<T>.TryAutoRegisterTypeOrThrow();
            if (_getInt != null)
            {
                return;
            }
            // The assembly's generated register did not cover this enum (it only reaches ones the
            // generator saw in a field type - an older generator missed List<T> elements), or it had
            // already run before NeuroSyncTypes<T> installed its enum delegate, which makes
            // TryAutoRegisterTypeOrThrow return without registering anything. Fall back to a boxed
            // Convert / Enum.ToObject pair: slower than the generated casts, but correct for any
            // underlying type.
            var type = typeof(T);
            if (!type.IsEnum)
            {
                throw new InvalidOperationException($"{nameof(NeuroSyncEnumTypes<T>)} used with a non-enum type {type}");
            }
            Register(v => Convert.ToInt32(v), i => (T)Enum.ToObject(type, i));
        }
        
        public static string GetName(int value)
        {
            if (enumNames == null)
            {
                var valuesArray = Enum.GetValues(typeof(T));
                enumNames = new Dictionary<int, string>(valuesArray.Length);
                foreach (var enumValue in valuesArray)
                {
                    // Convert, not a cast: a boxed enum only unboxes as its own underlying type, so
                    // (int) threw for any enum declared on byte, sbyte, short and the like.
                    enumNames[Convert.ToInt32(enumValue)] = enumValue.ToString();
                }
            }
            return enumNames.GetValueOrDefault(value, "");
        }
    }
}