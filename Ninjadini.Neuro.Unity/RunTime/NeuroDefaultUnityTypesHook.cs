using UnityEngine;

namespace Ninjadini.Neuro.Sync
{
    // This is auto picked up by code gen to be registered.
    struct NeuroDefaultUnityTypesHook : INeuroCustomTypesRegistryHook
    {
        static bool _registered;
        
        public void Register()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            
            AssetAddress.RegisterType();
            
            if(NeuroSyncTypes.IsEmpty<Color32>())
                NeuroSyncTypes.Register(FieldSizeType.VarInt, (INeuroSync neuro, ref Color32 value) => {
                    // RGBA
                    uint num = neuro.IsWriting ? value.r + (uint)(value.g << 8) + (uint)(value.b << 16) + (uint)(value.a << 24) : 0;
                    neuro.Sync(ref num);
                    if (neuro.IsReading)
                    {
                        value.r = (byte)num;
                        value.g = (byte)(num >> 8);
                        value.b = (byte)(num >> 16);
                        value.a = (byte)(num >> 24);
                    }
                });
            
            if(NeuroSyncTypes.IsEmpty<Color>())
                NeuroSyncTypes.Register(FieldSizeType.VarInt, (INeuroSync neuro, ref Color value) =>
                {
                    const int Bits = 12;
                    const int Bits2 = Bits * 2;
                    const int Bits3 = Bits * 3;
                    const float Base = 2L << Bits;
                    const long BaseL = 2L << Bits;
                    const long BaseL2 = 2L << Bits2;
                    const long BaseL3 = 2L << Bits3;
                    // RGBA
                    ulong num = neuro.IsWriting ? (ulong)(value.r * Base) + ((ulong)(value.g * Base)) * BaseL + ((ulong)(value.b * Base) * BaseL2) + ((ulong)(value.a * Base) * BaseL3) : 0;
                    neuro.Sync(ref num);
                    if (neuro.IsReading)
                    {
                        value.r = (num & BaseL) / Base;
                        value.g = ((num >> Bits) & BaseL) / Base;
                        value.b = ((num >> Bits2) & BaseL) / Base;
                        value.a = ((num >> Bits3) & BaseL) / Base;
                    }
                });
            
            if(NeuroSyncTypes.IsEmpty<Vector3>())
                NeuroSyncTypes.Register((INeuroSync neuro, ref Vector3 value) => {
                    neuro.Sync(1, nameof(value.x), ref value.x);
                    neuro.Sync(2, nameof(value.y), ref value.y);
                    neuro.Sync(3, nameof(value.z), ref value.z);
                });
            if(NeuroSyncTypes.IsEmpty<Vector2>())
                NeuroSyncTypes.Register((INeuroSync neuro, ref Vector2 value) => {
                    neuro.Sync(1, nameof(value.x), ref value.x);
                    neuro.Sync(2, nameof(value.y), ref value.y);
                });
            if(NeuroSyncTypes.IsEmpty<Vector2Int>())
                NeuroSyncTypes.Register((INeuroSync neuro, ref Vector2Int value) =>
                {
                    // they are properties so this is a long winded way :(
                    var x = value.x;
                    var y = value.y;
                    neuro.Sync(1, nameof(value.x), ref x);
                    neuro.Sync(2, nameof(value.y), ref y);
                    value.x = x;
                    value.y = y;
                });
            if(NeuroSyncTypes.IsEmpty<Vector3Int>())
                NeuroSyncTypes.Register((INeuroSync neuro, ref Vector3Int value) =>
                {
                    // they are properties so this is a long winded way :(
                    var x = value.x;
                    var y = value.y;
                    var z = value.z;
                    neuro.Sync(1, nameof(value.x), ref x);
                    neuro.Sync(2, nameof(value.y), ref y);
                    neuro.Sync(3, nameof(value.z), ref x);
                    value.x = x;
                    value.y = y;
                    value.z = z;
                });
            
            
            if(NeuroSyncTypes.IsEmpty<Rect>())
                NeuroSyncTypes.Register((INeuroSync neuro, ref Rect value) =>
                {
                    var pos = value.position;
                    var size = value.size;
                    neuro.Sync(1, "posX", ref pos.x);
                    neuro.Sync(2, "posY", ref pos.y);
                    neuro.Sync(3, "sizeX", ref size.x);
                    neuro.Sync(4, "sizeY", ref size.y);
                    value.position = pos;
                    value.size = size;
                });
            if(NeuroSyncTypes.IsEmpty<Bounds>())
                NeuroSyncTypes.Register((INeuroSync neuro, ref Bounds value) =>
                {
                    // they are properties so this is a long winded way :(
                    var pos = value.center;
                    var extents = value.extents;
                    neuro.Sync(1, "posX", ref pos.x);
                    neuro.Sync(2, "posY", ref pos.y);
                    neuro.Sync(3, "posZ", ref pos.z);
                    neuro.Sync(4, "extX", ref extents.x);
                    neuro.Sync(5, "extY", ref extents.y);
                    neuro.Sync(6, "extZ", ref extents.z);
                    value.center = pos;
                    value.extents = extents;
                });
        }
    }
}