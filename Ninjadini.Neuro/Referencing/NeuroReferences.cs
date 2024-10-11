using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    public class NeuroReferences : IReferenceResolver
    {
#if NEURO_DISABLE_STATIC_REFERENCES
        // Completely disable reference linking via the static instance. 
        // This disables being able to use myRef.GetValue()
        // It allows you to use multiple references roots without accidentally calling the wrong one. 
        public static NeuroReferences Default;
#elif NEURO_THREAD_STATIC_STATIC_REFERENCES
        // The default reference linking source can be set per thread
        // This allows you to run multiple references roots on different threads at the same time
        // e.g. if you are running a c# server / service that need to handle multiple versions of the config
        [ThreadStatic]
        static NeuroReferences _default;
        static NeuroReferences _globalDefault;

        public static NeuroReferences Default
        {
            get
            {
                _default ??= _globalDefault ??= new NeuroReferences();
                return _default;
            }
            set => _default = value;
        }
#else
        // Default unity friendly behaviour
        // Calling myRef.GetValue() is auto resolved to the default reference root shared across the untiy instance.
        public static NeuroReferences Default = new NeuroReferences();
#endif
        
        Dictionary<Type, INeuroReferenceTable> tables = new Dictionary<Type, INeuroReferenceTable>();

        public NeuroReferenceTable<T> GetTable<T>() where T: class,  IReferencable
        {
            return (NeuroReferenceTable<T>)GetTable(typeof(T));
        }

        public INeuroReferenceTable GetTable(Type type)
        {
            if(!tables.TryGetValue(type, out var dict))
            {
                var rootType = GetRootReferencable(type);
                if (rootType != type)
                {
                    dict = GetTable(rootType);
                }
                else
                {
                    var genericType = typeof(NeuroReferenceTable<>).MakeGenericType(type);
                    dict = (INeuroReferenceTable)Activator.CreateInstance(genericType, this);
                }
                tables[type] = dict;
            }
            return dict;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(IReferencable referencable)
        {
            GetTable(referencable.GetType()).Register(referencable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(uint refId) where T : class, IReferencable
        {
            return refId > 0 ? GetTable<T>().Get(refId) : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(string refName) where T : class, IReferencable
        {
            return string.IsNullOrEmpty(refName) ? default :  GetTable<T>().Get(refName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReferencable Get(Type type, uint refId)
        {
            return GetTable(type).Get(refId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>() where T : class, ISingletonReferencable
        {
            return GetTable<T>().SelectAll().FirstOrDefault();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> SelectAll<T>() where T : class, IReferencable
        {
            return GetTable<T>().SelectAll();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(Reference<T> reference) where T : class, IReferencable
        {
            return Get<T>(reference.RefId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetLinkedRef<T>(IReferencable referencable) where T : class, IReferencable
        {
            return referencable != null ? GetTable(typeof(T)).Get(referencable.RefId) as T : null;
        }

        public IEnumerable<Type> GetRegisteredBaseTypes()
        {
            return tables.Select(kv => kv.Key).Where(type => GetRootReferencable(type) == type);
        }

        public IEnumerable<INeuroReferenceTable> GetAllRegisteredTables()
        {
            return tables.Values;
        }

        public static IEnumerable<Type> GetAllPossibleBaseTypes()
        {
            return NeuroGlobalTypes.GetAllRootTypes().Where(t => typeof(IReferencable).IsAssignableFrom(t));
        }

        public static Type GetRootReferencable(Type type)
        {
            while(true)
            {
                var parent = type.BaseType;
                if (parent == null)
                {
                    break;
                }
                if (parent != typeof(Referencable) && typeof(IReferencable).IsAssignableFrom(parent))
                {
                    type = parent;
                }
                else
                {
                    break;
                }
            } 
            return type;
        }

        public void Clear()
        {
            foreach (var table in tables)
            {
                table.Value.Clear();
            }
        }
    }
}