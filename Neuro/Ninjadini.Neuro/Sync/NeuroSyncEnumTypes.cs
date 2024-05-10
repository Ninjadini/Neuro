using System;
using System.Collections.Generic;

namespace Ninjadini.Neuro.Sync
{
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
                NeuroSyncTypes<T>.TryAutoRegisterTypeOrThrow();
            }
            return _getInt(value);
        }
        
        public static T GetEnum(int value)
        {
            if (_getEnum == null)
            {
                NeuroSyncTypes<T>.TryAutoRegisterTypeOrThrow();
            }
            return _getEnum(value);
        }
        
        public static string GetName(int value)
        {
            if (enumNames == null)
            {
                var valuesArray = Enum.GetValues(typeof(T));
                enumNames = new Dictionary<int, string>(valuesArray.Length);
                foreach (var enumValue in valuesArray)
                {
                    enumNames[(int)enumValue] = enumValue.ToString();
                }
            }
            return enumNames.GetValueOrDefault(value, "");
        }
    }
}