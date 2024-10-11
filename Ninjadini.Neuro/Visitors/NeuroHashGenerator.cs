using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    public class NeuroHashGenerator : INeuroSync
    {
        [ThreadStatic] private static NeuroHashGenerator _shared;
        public static NeuroHashGenerator Shared => _shared ??= new NeuroHashGenerator();
        
        private int hash;
        
        public int Generate<T>(T obj)
        {
            hash = 0;
            SyncObj(ref obj);
            return hash;
        }

        T INeuroSync.GetPooled<T>()
        {
            return null;
        }

        void INeuroSync.Sync(ref bool value)
        {
            hash += value ? 123 : 456;
        }

        void INeuroSync.Sync(ref int value)
        {
            hash += value * 33;
        }

        void INeuroSync.Sync(ref uint value)
        {
            hash += value.GetHashCode();
        }

        void INeuroSync.Sync(ref long value)
        {
            hash += value.GetHashCode();
        }

        void INeuroSync.Sync(ref ulong value)
        {
            hash += value.GetHashCode();
        }

        void INeuroSync.Sync(ref float value)
        {
            hash += value.GetHashCode();
        }

        void INeuroSync.Sync(ref double value)
        {
            hash += value.GetHashCode();
        }

        void INeuroSync.Sync(ref string value)
        {
            hash += value.GetHashCode();
        }

        void INeuroSync.Sync<T>(ref Reference<T> value)
        {
            hash += value.RefId.GetHashCode();
        }

        void INeuroSync.SyncEnum<T>(ref int value)
        {
            hash += value.GetHashCode();
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value, T defaultValue)
        {
            if (value != null && !value.Equals(defaultValue))
            {
                hash += key.GetHashCode();
                SyncObj(ref value);
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T? value)
        {
            if (value != null)
            {
                hash += key.GetHashCode();
                var v = value.Value;
                SyncObj(ref v);
            }
        }

        void INeuroSync.SyncEnum<T>(uint key, string name, ref T value, int defaultValue)
        {
            var intValue = NeuroSyncEnumTypes<T>.GetInt(value);
            if (intValue != defaultValue)
            {
                hash += key.GetHashCode();
                hash += intValue;
            }
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
        }

        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            if (values != null)
            {
                hash += key.GetHashCode();
                hash += values.Count;
                foreach (var value in values)
                {
                    var v = value;
                    SyncObj<T>(ref v);
                }
            }
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            if (values != null)
            {
                hash += key.GetHashCode();
                hash += values.Count;
                foreach (var kv in values)
                {
                    var k = kv.Key;
                    var v = kv.Value;
                    SyncObj(ref k);
                    SyncObj(ref v);
                }
            }
        }
    }
}