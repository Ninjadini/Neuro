using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    public class NeuroPoolCollector : INeuroSync
    {
        [ThreadStatic] private static NeuroPoolCollector _shared;
        public static NeuroPoolCollector Shared => _shared ??= new NeuroPoolCollector();

        private INeuroObjectPool pool;
        
        public void ReturnAllToPool<T>(T obj, INeuroObjectPool objPool)
        {
            pool = objPool;
            try
            {
                SyncObj(ref obj);
            }
            finally
            {
                pool = null;
            }
        }
        
        bool INeuroSync.IsReading => true;
        bool INeuroSync.IsWriting => true;
        
        T INeuroSync.GetPooled<T>()
        {
            return null;
        }

        void INeuroSync.Sync(ref bool value)
        {
            
        }

        void INeuroSync.Sync(ref int value)
        {
            
        }

        void INeuroSync.Sync(ref uint value)
        {
        }

        void INeuroSync.Sync(ref long value)
        {
        }

        void INeuroSync.Sync(ref ulong value)
        {
        }

        void INeuroSync.Sync(ref float value)
        {
        }

        void INeuroSync.Sync(ref double value)
        {
        }

        void INeuroSync.Sync(ref string value)
        {
        }

        void INeuroSync.Sync<T>(ref Reference<T> value)
        {
        }

        void INeuroSync.SyncEnum<T>(ref int value)
        {
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value, T defaultValue)
        {
            SyncObj(ref value);
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T? value)
        {
            if (value != null)
            {
                // The struct itself is not poolable, but it can hold objects that are.
                var localValue = value.Value;
                SyncObj(ref localValue);
            }
        }

        void INeuroSync.SyncEnum<T>(uint key, string name, ref T value, int defaultValue)
        {
            
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value)
        {
            SyncObj(ref value);
        }

        void INeuroSync.SyncBaseClass<TRoot, TBase>(TBase value)
        {
            var baseValue = (TRoot)value;
            NeuroSyncSubTypes<TRoot>.GetOrThrow(typeof(TBase))(this, ref baseValue);
        }
        private void SyncObj<T>(ref T value)
        {
            if (value == null)
            {
                return;
            }
            var isGroup = IsGroupType<T>();
            if (isGroup && value.GetType() != typeof(T))
            {
                var subTag = NeuroSyncSubTypes<T>.GetTag(value.GetType());
                NeuroSyncSubTypes<T>.Sync(this, subTag, ref value);
            }
            else
            {
                NeuroSyncTypes<T>.GetOrThrow()(this, ref value);
            }
            if (value is INeuroPoolable)
            {
                pool.Return(value);
                value = default;
            }
        }

        /// Every item has to be walked, not just the ones that are poolable themselves - an item that is not
        /// poolable can still be holding poolable objects further down, and those would never come back.
        /// Primitives can not hold anything, so those collections are just cleared.
        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            if (values != null)
            {
                if (IsGroupType<T>())
                {
                    for (var index = 0; index < values.Count; index++)
                    {
                        var v = values[index];
                        SyncObj(ref v);
                    }
                }
                values.Clear();
            }
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            if (values != null)
            {
                // Keys are always single values, so only the values can be holding anything poolable.
                if (IsGroupType<TValue>())
                {
                    foreach (var kv in values)
                    {
                        var v = kv.Value;
                        SyncObj(ref v);
                    }
                }
                values.Clear();
            }
        }

        /// A group is a neuro class or struct - the only thing that can have fields, and so the only thing
        /// worth walking into. Asking for the delegate first because `SizeType` is only filled in once the
        /// type has been registered.
        static bool IsGroupType<T>()
        {
            NeuroSyncTypes<T>.GetOrThrow();
            return NeuroSyncTypes<T>.SizeType >= NeuroConstants.Child;
        }

        public class BasicPool : INeuroObjectPool
        {
            // TODO not real thing yet.
            
            public List<object> AllObjects = new List<object>();
            public T Borrow<T>() where T : class
            {
                // this is just a very inefficent pool
                var index = AllObjects.FindIndex(o => o.GetType() == typeof(T));
                if (index >= 0)
                {
                    var obj = AllObjects[index];
                    AllObjects.RemoveAt(index);
                    return (T)obj;
                }
                return null;
            }

            public void Return(object obj)
            {
                if (AllObjects.Contains(obj))
                {
                    throw new Exception("Object already in list " + obj);
                }
                AllObjects.Add(obj);
            }
        }
    }
}