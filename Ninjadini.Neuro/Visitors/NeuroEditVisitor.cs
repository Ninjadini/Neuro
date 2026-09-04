using System.Collections.Generic;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    /// The editing counterpart to NeuroVisitor - the same walk over an object's Neuro fields, except that what
    /// the visitor is handed is written back into the object afterwards, so it can rewrite values as it goes.
    /// NeuroVisitor is a read only walk: it hands out the contents of lists, dictionaries and nullables as
    /// copies, so writing to those there would be silently dropped. Use this one when changing the data is the
    /// point, such as repointing every Reference<> from one RefId to another.
    public class NeuroEditVisitor : INeuroSync
    {
        public interface IInterface
        {
            void BeginVisit<T>(ref T obj, string name, int? listIndex);
            void EndVisit();

            /// Assign to `reference` to change it - the new value is kept, wherever the reference was held.
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
                // the nullable was visited as a copy, put back whatever the visitor made of it.
                value = v;
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

        void SyncObj<T>(ref T value, string name, int? listIndex)
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
                // the item was visited as a copy - without putting it back, a Reference<> held in a list would
                // silently keep its old value.
                values[index] = v;
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
            List<(TKey oldKey, TKey newKey, TValue value)> rewritten = null;
            foreach (var kv in values)
            {
                var k = kv.Key;
                SyncObj(ref k, name, index);
                var v = kv.Value;
                if (v != null)
                {
                    SyncObj(ref v, name, index);
                }
                // Same copy problem as the list, except a dictionary can not be written to while it is being
                // enumerated, so anything the visitor changed is collected here and applied below.
                if (!EqualityComparer<TKey>.Default.Equals(k, kv.Key)
                    || !EqualityComparer<TValue>.Default.Equals(v, kv.Value))
                {
                    rewritten ??= new List<(TKey, TKey, TValue)>();
                    rewritten.Add((kv.Key, k, v));
                }
                index++;
            }
            if (rewritten != null)
            {
                foreach (var (oldKey, newKey, value) in rewritten)
                {
                    if (!EqualityComparer<TKey>.Default.Equals(oldKey, newKey))
                    {
                        values.Remove(oldKey);
                    }
                    values[newKey] = value;
                }
            }
            visitor.EndVisit();
        }
    }
}
