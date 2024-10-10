using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninjadini.Neuro.Sync
{
    internal static class NeuroSyncSubTypes<TRootType>
    {
        static Dictionary<uint, NeuroSyncDelegate<TRootType>> _subClassesByTag;
        internal static Dictionary<Type, uint> _tagBySubClass;
        static NeuroSyncSubDelegate<TRootType> Delegate;
        
        internal static void RegisterSubClass<TSubClass>(uint tag) where TSubClass : class, TRootType
        {
            RegisterSubClass<TRootType, TSubClass>(tag);
        }

        internal static void RegisterSubClass<TBaseClass, TSubClass>(uint tag) where TSubClass : class, TBaseClass where TBaseClass : TRootType
        {
            RegisterClass<TBaseClass, TSubClass>(tag);
            NeuroSyncSubTypes<TSubClass>.Delegate = (INeuroSync neuro, uint tag, ref TSubClass value) =>
            {
                var baseValue = value != null ? (TRootType)value : default;
                _subClassesByTag[tag](neuro, ref baseValue);
                value = baseValue as TSubClass;
            };
            if (NeuroSyncTypes<TRootType>.Delegate == null && typeof(TRootType).IsInterface)
            {
                NeuroSyncTypes<TRootType>.SizeType = (uint)FieldSizeType.Child;
                NeuroSyncTypes<TRootType>.Delegate = delegate(INeuroSync neuro, ref TRootType value)
                {
                };
            }
            if (typeof(TBaseClass).IsInterface)
            {
                _subClassesByTag[tag] = delegate(INeuroSync neuro, ref TRootType baseValue)
                {
                    var typedObj = baseValue as TSubClass;
                    NeuroSyncTypes<TSubClass>.GetOrThrow()(neuro, ref typedObj);
                    baseValue = typedObj;
                };
            }
            else
            {
                _subClassesByTag[tag] = delegate(INeuroSync neuro, ref TRootType baseValue)
                {
                    var typedObj = baseValue as TSubClass;
                    NeuroSyncTypes<TSubClass>.GetOrThrow()(neuro, ref typedObj);
                    baseValue = typedObj;
                    neuro.SyncBaseClass<TRootType, TBaseClass>(typedObj);
                };
            }
        }

        static void RegisterClass<TBaseClass, TSubClass>(uint tag) where TSubClass : TBaseClass where TBaseClass : TRootType
        {
            if (_subClassesByTag == null)
            {
                _subClassesByTag = new Dictionary<uint, NeuroSyncDelegate<TRootType>>();
                _tagBySubClass = new Dictionary<Type, uint>();
                _tagBySubClass[typeof(TRootType)] = 0;
            }
            if (tag == 0)
            {
                throw new Exception($"Tried to register sub class [{typeof(TSubClass)}] with tag 0");
            }
            if (_subClassesByTag.ContainsKey(tag))
            {
                foreach (var kv in _tagBySubClass)
                {
                    if (kv.Value == tag && kv.Key != typeof(TSubClass))
                    {
                        throw new System.Exception($"{typeof(TRootType)}'s subClass tag is already registered for {kv.Key} but we are trying to register again for {typeof(TSubClass)}");
                    }
                }
            }
            _tagBySubClass[typeof(TSubClass)] = tag;
            NeuroSyncSubTypes<TSubClass>._tagBySubClass = _tagBySubClass;
        }

        internal static bool Exists()
        {
            return _subClassesByTag != null;
        }
        
        internal static Type[] GetAllSubTypes() // used by NeuroSyncTypes's reflection
        {
            return _tagBySubClass?.Keys.ToArray();
        }
        
        internal static Type GetRootType() // used by NeuroSyncTypes's reflection
        {
            if (_tagBySubClass != null)
            {
                foreach (var kv in _tagBySubClass)
                {
                    if (kv.Value == 0)
                    {
                        return kv.Key;
                    }
                }
            }
            return null;
        }
        
        internal static void Sync(INeuroSync neuro, uint tag, ref TRootType baseValue)
        {
            if (tag == 0)
            {
                NeuroSyncTypes<TRootType>.Delegate(neuro, ref baseValue);
            }
            else if(Delegate != null)
            {
                Delegate(neuro, tag, ref baseValue);
            }
            else
            {
                _subClassesByTag[tag](neuro, ref baseValue);
            }
        }
        
        internal static NeuroSyncDelegate<TRootType> GetOrThrow(Type type)
        {
            var tag = GetTag(type);
            if (tag == 0)
            {
                return NeuroSyncTypes<TRootType>.Delegate;
            }
            if (_subClassesByTag == null)
            {
                throw new SystemException($"`{type}` is not registered as a subtype of {typeof(TRootType).Name}.");
            }
            return _subClassesByTag[tag];
        }
        
        internal static uint GetTag(Type type)
        {
            if (_tagBySubClass == null)
            {
                throw new Exception($"Base type `{typeof(TRootType)}`, requested by `{type}` is not registered.");
            }
            if (!_tagBySubClass.TryGetValue(type, out var result))
            {
                throw new Exception($"Sub type `{type}` of base type `{typeof(TRootType)}` is not registered.");
            }
            return result;
        }
    }
}