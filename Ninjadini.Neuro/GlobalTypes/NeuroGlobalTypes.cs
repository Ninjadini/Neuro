using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninjadini.Neuro.Sync
{
    public static class NeuroGlobalTypes
    {
        private static Dictionary<Type, uint> typeIds = new Dictionary<Type, uint>();
        private static Dictionary<uint, NeuroSyncSubDelegate<object>> syncsById = new Dictionary<uint, NeuroSyncSubDelegate<object>>();
        private static Dictionary<uint, Func<Type, uint>> tags = new Dictionary<uint, Func<Type, uint>>();

        public static void Register<T>(uint typeId)
        {
            if (syncsById.ContainsKey(typeId))
            {
                Type otherType = null;
                foreach (var kv in typeIds)
                {
                    if (kv.Value == typeId)
                    {
                        otherType = kv.Key;
                        break;
                    }
                }
                if (otherType == typeof(T))
                {
                    return;
                }
                throw new Exception($"Global type id {typeId} conflict found between {typeof(T).FullName} and {otherType?.FullName}");
            }
            typeIds[typeof(T)] = typeId;
            syncsById[typeId] = (INeuroSync neuro, uint tag, ref object value) =>
            {
                T typedValue = default;
                if (value != null)
                {
                    typedValue = (T)value;
                }
                NeuroSyncSubTypes<T>.Sync(neuro, tag, ref typedValue);
                value = typedValue;
            };
            tags[typeId] = (t) => NeuroSyncSubTypes<T>.Exists() ? NeuroSyncSubTypes<T>.GetTag(t) : 0u;
        }

        public static IReadOnlyList<Type> GetAllRootTypes()
        {
            var result = typeIds.Keys.ToArray();
            if (result.Length == 0)
            {
                NeuroSyncTypes.TryRegisterAllAssemblies();
                result = typeIds.Keys.ToArray();
            }
            return result;
        }

        public static bool TypeIdExists(uint typeId)
        {
            return syncsById.ContainsKey(typeId);
        }

        public static Type FindTypeById(uint typeId)
        {
            if (typeId == 0)
            {
                return null;
            }
            foreach (var kv in typeIds)
            {
                if (kv.Value == typeId)
                {
                    return kv.Key;
                }
            }
            return null;
        }

        public static uint GetIdByType(Type type)
        {
            var baseType = type;
            while (baseType != null)
            {
                if (typeIds.TryGetValue(baseType, out var result))
                {
                    return result;
                }
                baseType = baseType.BaseType;
            }
            return 0;
        }

        public static uint GetSubTypeTag(Type type)
        {
            var typeId = GetTypeIdOrThrow(type, out _);
            if(tags.TryGetValue(typeId, out var func))
            {
                return func(type);
            }
            return 0;
        }

        public static uint GetSubTypeTagOrThrow(Type type)
        {
            var typeId = GetTypeIdOrThrow(type, out var rootType);
            if(tags.TryGetValue(typeId, out var func))
            {
                var result = func(type);
                if (result == 0 && type != rootType)
                {
                    throw new Exception($"{type.FullName} is not registered as a subtype of {rootType}, is it missing the [Neuro()] class tag?");
                }
                return result;
            }
            return 0;
        }

        public static bool HasSubTypeTags(Type type)
        {
            var typeId = GetTypeIdOrThrow(type, out _);
            return tags.TryGetValue(typeId, out var func);
        }

        public static uint GetTypeIdOrThrow(Type type, out Type rootType)
        {
            var baseType = type;
            while (baseType != null)
            {
                if (typeIds.TryGetValue(baseType, out var result))
                {
                    rootType = baseType;
                    return result;
                }
                baseType = baseType.BaseType;
            }
            throw new Exception($"{type.FullName} is not registered to {nameof(NeuroGlobalTypes)}");
        }

        public static bool IsPossiblyGlobalType<T>()
        {
            return typeof(T) == typeof(object) || NeuroSyncTypes.IsEmpty<T>();
        }

        public static void Sync(INeuroSync neuro, ref object obj)
        {
            var type = obj.GetType();
            var globalId = GetTypeIdOrThrow(type, out _);
            var subTag = GetSubTypeTag(type);
            Sync(globalId, neuro, subTag, ref obj);
        }

        public static void Sync(uint typeID, INeuroSync neuro, uint tag, ref object value)
        {
            if (syncsById.TryGetValue(typeID, out var del))
            {
                del(neuro, tag, ref value);
            }
            else
            {
                throw new Exception($"Global type id {typeID} is not registered to {nameof(NeuroGlobalTypes)}");
            }
        }
    }
}