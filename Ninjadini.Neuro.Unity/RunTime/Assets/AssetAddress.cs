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