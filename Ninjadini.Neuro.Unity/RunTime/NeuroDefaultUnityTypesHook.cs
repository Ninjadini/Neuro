using Ninjadini.Neuro;
using UnityEngine;

[assembly:Neuro(0)]

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
                // not necessary here because we use built-in Color32 field editor
                // NeuroSyncEditorFields.AddProperty(typeof(Color32), nameof(Color32.r));
                // ...
            
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
            // not necessary here because we use built-in Color field editor
            // NeuroSyncEditorFields.AddProperty(typeof(Color), nameof(Color.r));
            // ...
            
            if(NeuroSyncTypes.IsEmpty<Vector3>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref Vector3 value) => {
                    neuro.Sync(1, nameof(value.x), ref value.x, 0f);
                    neuro.Sync(2, nameof(value.y), ref value.y, 0f);
                    neuro.Sync(3, nameof(value.z), ref value.z, 0f);
                });
                // not necessary here because we use built-in Vector3 field editor
                // NeuroSyncEditorFields.AddField(typeof(Vector3), nameof(Vector3.x));
                // NeuroSyncEditorFields.AddField(typeof(Vector3), nameof(Vector3.y));
                // NeuroSyncEditorFields.AddField(typeof(Vector3), nameof(Vector3.z));
            }
            if(NeuroSyncTypes.IsEmpty<Vector2>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref Vector2 value) => {
                    neuro.Sync(1, nameof(value.x), ref value.x, 0f);
                    neuro.Sync(2, nameof(value.y), ref value.y, 0f);
                });
                // not necessary here because we use built-in Vector2 field editor
                // NeuroSyncEditorFields.AddField(typeof(Vector2), nameof(Vector2.x));
                // NeuroSyncEditorFields.AddField(typeof(Vector2), nameof(Vector2.y));
            }
            if(NeuroSyncTypes.IsEmpty<Vector2Int>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref Vector2Int value) =>
                {
                    // they are properties so this is a long winded way :(
                    var x = value.x;
                    var y = value.y;
                    neuro.Sync(1, nameof(value.x), ref x, 0);
                    neuro.Sync(2, nameof(value.y), ref y, 0);
                    value.x = x;
                    value.y = y;
                });
                // not necessary here because we use built-in Vector2Int field editor
                // NeuroSyncEditorFields.AddProperty(typeof(Vector2Int), nameof(Vector2Int.x));
                // NeuroSyncEditorFields.AddProperty(typeof(Vector2Int), nameof(Vector2Int.y));
            }
            if(NeuroSyncTypes.IsEmpty<Vector3Int>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref Vector3Int value) =>
                {
                    // they are properties so this is a long winded way :(
                    var x = value.x;
                    var y = value.y;
                    var z = value.z;
                    neuro.Sync(1, nameof(value.x), ref x, 0);
                    neuro.Sync(2, nameof(value.y), ref y, 0);
                    neuro.Sync(3, nameof(value.z), ref z);
                    value.x = x;
                    value.y = y;
                    value.z = z;
                });
                // not necessary here because we use built-in Vector3Int field editor
                // NeuroSyncEditorFields.AddProperty(typeof(Vector3Int), nameof(Vector3Int.x));
                // NeuroSyncEditorFields.AddProperty(typeof(Vector3Int), nameof(Vector3Int.y));
                // NeuroSyncEditorFields.AddProperty(typeof(Vector3Int), nameof(Vector3Int.z));
            }


            if(NeuroSyncTypes.IsEmpty<Rect>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref Rect value) =>
                {
                    var pos = value.position;
                    var size = value.size;
                    neuro.Sync(1, "posX", ref pos.x, 0f);
                    neuro.Sync(2, "posY", ref pos.y, 0f);
                    neuro.Sync(3, "sizeX", ref size.x, 0f);
                    neuro.Sync(4, "sizeY", ref size.y, 0f);
                    value.position = pos;
                    value.size = size;
                });
                // not necessary here because we use built-in Rect field editor
                // NeuroSyncEditorFields.AddProperty(typeof(Rect), nameof(Rect.x));
                // NeuroSyncEditorFields.AddProperty(typeof(Rect), nameof(Rect.y));
                // NeuroSyncEditorFields.AddProperty(typeof(Rect), nameof(Rect.width));
                // NeuroSyncEditorFields.AddProperty(typeof(Rect), nameof(Rect.height));
            }
            if(NeuroSyncTypes.IsEmpty<RectInt>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref RectInt value) =>
                {
                    // they are properties so this is a long winded way :(
                    var x = value.x;
                    var y = value.y;
                    var width = value.width;
                    var height = value.height;
                    neuro.Sync(1, nameof(value.x), ref x, 0);
                    neuro.Sync(2, nameof(value.y), ref y, 0);
                    neuro.Sync(3, nameof(value.width), ref width, 0);
                    neuro.Sync(4, nameof(value.height), ref height, 0);
                    value.x = x;
                    value.y = y;
                    value.width = width;
                    value.height = height;
                });
                // not necessary here because we use built-in RectInt field editor
                // NeuroSyncEditorFields.AddProperty(typeof(RectInt), nameof(RectInt.x));
                // NeuroSyncEditorFields.AddProperty(typeof(RectInt), nameof(RectInt.y));
                // NeuroSyncEditorFields.AddProperty(typeof(RectInt), nameof(RectInt.width));
                // NeuroSyncEditorFields.AddProperty(typeof(RectInt), nameof(RectInt.height));
            }
            if(NeuroSyncTypes.IsEmpty<Bounds>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref Bounds value) =>
                {
                    // they are properties so this is a long winded way :(
                    var pos = value.center;
                    var extents = value.extents;
                    neuro.Sync(1, "posX", ref pos.x, 0f);
                    neuro.Sync(2, "posY", ref pos.y, 0f);
                    neuro.Sync(3, "posZ", ref pos.z, 0f);
                    neuro.Sync(4, "extX", ref extents.x, 0f);
                    neuro.Sync(5, "extY", ref extents.y, 0f);
                    neuro.Sync(6, "extZ", ref extents.z, 0f);
                    value.center = pos;
                    value.extents = extents;
                });
                // not necessary here because we use built-in Bounds field editor
                // NeuroSyncEditorFields.AddProperty(typeof(Bounds), nameof(Bounds.center));
                // NeuroSyncEditorFields.AddProperty(typeof(Bounds), nameof(Bounds.extents));
            }
            if(NeuroSyncTypes.IsEmpty<BoundsInt>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref BoundsInt value) =>
                {
                    // they are properties so this is a long winded way :(
                    var pos = value.position;
                    var size = value.size;
                    neuro.Sync(1, "pos", ref pos);
                    neuro.Sync(2, "size", ref size);
                    value.position = pos;
                    value.size = size;
                });
                // not necessary here because we use built-in BoundsInt field editor
                // NeuroSyncEditorFields.AddProperty(typeof(BoundsInt), nameof(BoundsInt.position));
                // NeuroSyncEditorFields.AddProperty(typeof(BoundsInt), nameof(BoundsInt.size));
            }
        }
    }
}