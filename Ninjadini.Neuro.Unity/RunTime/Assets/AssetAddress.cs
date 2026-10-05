using System;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    [Serializable]
    public struct AssetAddress : IEquatable<AssetAddress>
    {
        public string Address;
        
        public bool Equals(AssetAddress other)
        {
            return Address == other.Address;
        }

        public override bool Equals(object obj)
        {
            return obj is AssetAddress other && Equals(other);
        }

        // Without this a dictionary keyed by an AssetAddress falls back to ValueType.GetHashCode,
        // which boxes the struct on every lookup.
        public override int GetHashCode()
        {
            return Address != null ? Address.GetHashCode() : 0;
        }

        public bool HasAddress() => !string.IsNullOrEmpty(Address);

        public bool IsEmpty() => string.IsNullOrEmpty(Address);

        
        internal static void RegisterType()
        {
            NeuroSyncTypes.Register(FieldSizeType.Length, delegate(INeuroSync neuro, ref AssetAddress value)
            {
                neuro.Sync(ref value.Address);
            });
        }
    }
}