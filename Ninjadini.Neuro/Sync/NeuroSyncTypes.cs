using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ninjadini.Neuro.Sync
{
    public delegate void NeuroSyncDelegate<T>(INeuroSync neuro, ref T value);
    public delegate void NeuroSyncSubDelegate<T>(INeuroSync neuro, uint tag, ref T value);

    public static class NeuroSyncTypes
    {
        static NeuroSyncTypes()
        {
            NeuroDefaultSyncTypes.Register();
        }

        public static bool IsEmpty<T>()
        {
            return NeuroSyncTypes<T>.Delegate == null;
        }

        public static bool Exists<T>()
        {
            return NeuroSyncTypes<T>.Delegate != null;
        }

        public static void Register<T>(NeuroSyncDelegate<T> d, uint globalTypeId = 0)
        {
            NeuroSyncTypes<T>.SizeType = (uint)FieldSizeType.Child;
            NeuroSyncTypes<T>.Delegate = d;
            if (globalTypeId > 0)
            {
                NeuroGlobalTypes.Register<T>(globalTypeId);
            }
        }

        public static void RegisterSubClass<TBaseType, TSubType>(uint typeTag, NeuroSyncDelegate<TSubType> d) where TSubType : class, TBaseType
        {
            NeuroSyncTypes<TSubType>.SizeType = (uint)FieldSizeType.ChildWithTag;
            NeuroSyncTypes<TSubType>.Delegate = d;
            NeuroSyncSubTypes<TBaseType>.RegisterSubClass<TSubType>(typeTag);
        }
        
        public static void RegisterSubClass<TRootType, TBaseType, TSubType>(uint typeTag, NeuroSyncDelegate<TSubType> d) where TSubType : class, TBaseType where TBaseType : class, TRootType
        {
            NeuroSyncTypes<TSubType>.SizeType = (uint)FieldSizeType.ChildWithTag;
            NeuroSyncTypes<TSubType>.Delegate = d;
            NeuroSyncSubTypes<TRootType>.RegisterSubClass<TBaseType, TSubType>(typeTag);
        }
        
        public static void Register<T>(FieldSizeType sizeType, NeuroSyncDelegate<T> d)
        {
            NeuroSyncTypes<T>.SizeType = (uint)sizeType;
            NeuroSyncTypes<T>.Delegate = d;
        }
        
        static bool _scannedAssemblies;
        public static void TryRegisterAllAssemblies(bool forceRescan = false)
        {
            if (_scannedAssemblies && !forceRescan)
            {
                return;
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    TryRegisterAssembly(assembly);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            _scannedAssemblies = true;
        }

        public static void DisableAssembliesScanning()
        {
            _scannedAssemblies = true;
        }

        public static void TryRegisterAssemblyOf<T>()
        {
            if (NeuroSyncTypes<T>.Delegate != null)
            {
                return;
            }
            var assembly = typeof(T).Assembly;
            if (TryRegisterAssembly(assembly) && NeuroSyncTypes<T>.Delegate != null)
            {
                return;
            }
            throw new Exception($"{nameof(NeuroSyncTypes)} of type: {typeof(T)} is not registered. You might just need to call NeuroTypesRegister.Register()");
        }

        public static bool TryRegisterAssembly(Assembly assembly)
        {
            var assemblyAttribute = assembly.GetCustomAttribute<NeuroAssemblyAttribute>();
            if (assemblyAttribute != null)
            {
                return TryRegisterAssembly(assemblyAttribute);
            }
            return false;
        }
        
        static bool TryRegisterAssembly(NeuroAssemblyAttribute assemblyAttribute)
        {
            var registryType = assemblyAttribute.RegistryType;
            if (registryType != null && !string.IsNullOrEmpty(assemblyAttribute.RegistryMethodName))
            {
                var method = registryType.GetMethod(assemblyAttribute.RegistryMethodName, BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, null);
                    return true;
                }
            }
            return false;
        }
        
        public static bool CheckIfTypeRegisteredUsingReflection(Type type)
        {
            var method = typeof(NeuroSyncTypes).GetMethod("Exists", BindingFlags.Public | BindingFlags.Static);
            return !type.ContainsGenericParameters && (bool)method.MakeGenericMethod(type).Invoke(null, null);
        }
        
        public static Type[] FindRegisteredSubTypesUsingReflection(Type type)
        {
            var subTypesType = typeof(NeuroSyncSubTypes<>).MakeGenericType(type);
            var method = subTypesType.GetMethod("GetAllSubTypes", BindingFlags.NonPublic | BindingFlags.Static);
            return method.Invoke(null, null) as Type[];
        }
        
        public static Type FindRegisteredRootTypeUsingReflection(Type type)
        {
            var subTypesType = typeof(NeuroSyncSubTypes<>).MakeGenericType(type);
            var method = subTypesType.GetMethod("GetRootType", BindingFlags.NonPublic | BindingFlags.Static);
            return method.Invoke(null, null) as Type;
        }

        static readonly Dictionary<Type, TypeInfo> _typeInfos = new Dictionary<Type, TypeInfo>();

        /// This is a bit slow because we need to do reflection to get the generic type.
        public static TypeInfo GetTypeInfo(Type type)
        {
            if (_typeInfos.TryGetValue(type, out var result))
            {
                return result;
            }
            var subTypesType = typeof(NeuroSyncTypes<>).MakeGenericType(type);
            var method = subTypesType.GetMethod("GetTypeInfo", BindingFlags.NonPublic | BindingFlags.Static);
            result = (TypeInfo) method.Invoke(null, null);
            _typeInfos[type] = result;
            return result;
        }
        
        public struct TypeInfo
        {
            public uint SizeType;
            public uint SubTypeTag;
            public Func<INeuroSync, uint, object, object> Sync;
        }
    }

    internal static class NeuroSyncTypes<T>
    {
        internal static uint SizeType;
        internal static NeuroSyncDelegate<T> Delegate;
        
        internal static NeuroSyncDelegate<T> GetOrThrow()
        {
            if (Delegate == null)
            {
                if (typeof(T).IsEnum)
                {
                    SizeType = NeuroConstants.VarInt;
                    Delegate = delegate(INeuroSync neuro, ref T value)
                    {
                        var intValue = NeuroSyncEnumTypes<T>.GetInt(value);
                        neuro.SyncEnum<T>(ref intValue);
                        value = NeuroSyncEnumTypes<T>.GetEnum(intValue);
                    };
                    return Delegate;
                }
                TryAutoRegisterTypeOrThrow();
            }
            return Delegate;
        }

        internal static void TryAutoRegisterTypeOrThrow()
        {
            NeuroSyncTypes.TryRegisterAssemblyOf<T>();
        }

        // this is called via reflection
        internal static NeuroSyncTypes.TypeInfo GetTypeInfo()
        {
            if (!NeuroSyncTypes.Exists<T>())
            {
                NeuroSyncTypes.TryRegisterAssemblyOf<T>();
            }
            if (SizeType == NeuroConstants.ChildWithType || NeuroSyncSubTypes<T>.Exists())
            {
                var thisTag = NeuroSyncSubTypes<T>.GetTag(typeof(T));
                return new NeuroSyncTypes.TypeInfo()
                {
                    SizeType = SizeType,
                    SubTypeTag = thisTag,
                    Sync = (neuro, tag, input) =>
                    {
                        var output = (T)input;
                        NeuroSyncSubTypes<T>.Sync(neuro, tag, ref output);
                        return output;
                    }
                };
            }
            if(SizeType == NeuroConstants.Child)
            {
                return new NeuroSyncTypes.TypeInfo()
                {
                    SizeType = SizeType,
                    Sync = (neuro, tag, input) =>
                    {
                        var output = (T)input;
                        Delegate(neuro, ref output);
                        return output;
                    }
                };
            }
            throw new Exception($"{typeof(T)} is not registered. You might just need to call NeuroTypesRegister.Register()");
        }
    }
}