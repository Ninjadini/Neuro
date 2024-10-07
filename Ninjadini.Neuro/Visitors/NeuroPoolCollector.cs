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
            SyncObj(ref obj);
            pool = null;
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
            var isGroup = NeuroSyncTypes<T>.SizeType >= NeuroConstants.Child;
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

        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (value is INeuroPoolable)
                    {
                        var v = value;
                        SyncObj<T>(ref v);
                    }
                }
                values.Clear();
            }
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            if (values != null)
            {
                foreach (var kv in values)
                {
                    if (kv.Value is INeuroPoolable)
                    {
                        var v = kv.Value;
                        SyncObj<TValue>(ref v);
                    }
                }
                values.Clear();
            }
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