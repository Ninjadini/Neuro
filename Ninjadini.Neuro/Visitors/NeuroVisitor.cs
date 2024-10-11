using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    public class NeuroVisitor : INeuroSync
    {
        public interface IInterface
        {
            void BeginVisit<T>(ref T obj, string name, int? listIndex);
            void EndVisit();

            void VisitRef<T>(ref Reference<T> reference) where T : class, IReferencable;
        }

        IInterface visitor;
        bool includePrimitiveValues;
        
        public void Visit<T>(T obj, IInterface iInterface, bool visitPrimitiveValues = false)
        {
            if (iInterface == null)
            {
                return;
            }
            visitor = iInterface;
            includePrimitiveValues = visitPrimitiveValues;
            try
            {
                visitor.BeginVisit(ref obj, "", null);
                if (NeuroGlobalTypes.IsPossiblyGlobalType<T>())
                {
                    var looseObj = (object)obj;
                    NeuroGlobalTypes.Sync(this, ref looseObj);
                }
                else
                {
                    SyncObj(ref obj, "", null);
                }
                visitor.EndVisit();
            }
            finally
            {
                visitor = null;
            }
        }
        
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
            visitor.VisitRef(ref value);
        }

        void INeuroSync.SyncEnum<T>(ref int value)
        {
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value, T defaultValue)
        {
            SyncObj(ref value, name, null);
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T? value)
        {
            if (value != null)
            {
                var v = value.Value;
                SyncObj(ref v, name, null);
            }
        }

        void INeuroSync.SyncEnum<T>(uint key, string name, ref T value, int defaultValue)
        {
            
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value)
        {
            SyncObj(ref value, name, null);
        }

        void INeuroSync.SyncBaseClass<TRoot, TBase>(TBase value)
        {
            var baseValue = (TRoot)value;
            NeuroSyncSubTypes<TRoot>.GetOrThrow(typeof(TBase))(this, ref baseValue);
        }
        
        private void SyncObj<T>(ref T value, string name, int? listIndex)
        {
            if (value == null)
            {
                return;
            }
            if (!includePrimitiveValues && (typeof(T).IsPrimitive || typeof(T) == typeof(string)))
            {
                return;
            }
            visitor.BeginVisit(ref value, name, listIndex);
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
            visitor.EndVisit();
        }

        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            if (values == null)
            {
                return;
            }
            visitor.BeginVisit(ref values, name, null);
            for (var index = 0; index < values.Count; index++)
            {
                var v = values[index];
                SyncObj(ref v, name, index);
            }
            visitor.EndVisit();
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            if (values == null)
            {
                return;
            }
            visitor.BeginVisit(ref values, name, null);
            var index = 0;
            foreach (var kv in values)
            {
                var k = kv.Key;
                SyncObj(ref k, name, index);
                var v = kv.Value;
                if (v != null)
                {
                    SyncObj(ref v, name, index);
                }
                index++;
            }
            visitor.EndVisit();
        }

        public static string GeneratePathFromStack(IEnumerable<StackItem> stack)
        {
            var str = "";
            foreach (var v in stack)
            {
                if(v.ListIndex.HasValue)
                {
                    str += $"[{v.ListIndex}]";
                }
                else if(str.Length > 0)
                {
                    str += "."+v.Name;
                }
                else
                {
                    str += v.Name;
                }
            }
            return str;
        }
        
        public struct StackItem
        {
            public object Object;
            public string Name;
            public int? ListIndex;
        }
    }
}