using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    [Serializable]
    public struct Reference<T> : INeuroReference, IEquatable<Reference<T>>, IEquatable<T>
        where T : class, IReferencable
    {
        public uint RefId;

        public bool HasNoRefId => RefId == 0;
        public bool HasRefId => RefId != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue(NeuroReferences references)
        {
            return references?.Get<T>(RefId);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue(NeuroReferenceTable<T> table)
        {
            return table?.Get(RefId);
        }
        
#if !NEURO_DISABLE_STATIC_REFERENCES
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue()
        {
            return NeuroReferences.Default?.Get<T>(RefId);
        }
#endif
        
        public static implicit operator uint(Reference<T> obj) => obj.RefId;
        public static implicit operator Reference<T>(uint id) => new Reference<T>()
        {
            RefId = id
        };
        public static implicit operator Reference<T>(T obj)
        {
            if (obj != null)
            {
                var refId = obj.RefId;
                if(refId == 0)
                {
                    throw new InvalidOperationException($"{typeof(T)} has invalid RefId, '0'.");
                }
                return new Reference<T>()
                {
                    RefId = refId
                };
            }
            return new Reference<T>();
        }
        
        //public static implicit operator T(Reference<T> obj) => obj.Value;

        public static bool operator ==(Reference<T> a, Reference<T> b) => a.RefId == b.RefId;
        
        public static bool operator !=(Reference<T> a, Reference<T> b) => a.RefId != b.RefId;

        public static bool operator ==(Reference<T> a, T b) => a.RefId == (b?.RefId ?? 0);
        
        public static bool operator !=(Reference<T> a, T b) => a.RefId != (b?.RefId ?? 0);

        public static bool operator ==(T a, Reference<T> b) => (a?.RefId ?? 0) == b.RefId;
        
        public static bool operator !=(T a, Reference<T> b) => (a?.RefId ?? 0) != b.RefId;

        public bool Equals(Reference<T> other)
        {
            return RefId == other.RefId;
        }

        public bool Equals(T other)
        {
            return RefId == (other?.RefId ?? 0);
        }
        
        public override bool Equals(object obj)
        {
            return (obj is Reference<T> other && other.RefId == RefId)
                   || (obj is T otherT && otherT.RefId == RefId);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)RefId;
            }
        }

        public override string ToString()
        {
            return RefId == 0 ? $"[{typeof(T)} ref null]" : $"[{typeof(T)} ref #{RefId}]";
        }

        public static void Sync(INeuroSync neuro, ref Reference<T> value)
        {
            neuro.Sync(ref value);
        }

        Type INeuroReference.RefType => typeof(T);
        uint INeuroReference.RefId => RefId;
    }
}