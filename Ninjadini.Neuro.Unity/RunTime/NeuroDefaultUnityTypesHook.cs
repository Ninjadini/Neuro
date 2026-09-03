using System;
using System.Collections.Generic;
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


            if(NeuroSyncTypes.IsEmpty<Vector4>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref Vector4 value) => {
                    neuro.Sync(1, nameof(value.x), ref value.x, 0f);
                    neuro.Sync(2, nameof(value.y), ref value.y, 0f);
                    neuro.Sync(3, nameof(value.z), ref value.z, 0f);
                    neuro.Sync(4, nameof(value.w), ref value.w, 0f);
                });
                // not necessary here because we use built-in Vector4 field editor
            }
            if(NeuroSyncTypes.IsEmpty<Quaternion>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref Quaternion value) => {
                    neuro.Sync(1, nameof(value.x), ref value.x, 0f);
                    neuro.Sync(2, nameof(value.y), ref value.y, 0f);
                    neuro.Sync(3, nameof(value.z), ref value.z, 0f);
                    neuro.Sync(4, nameof(value.w), ref value.w, 0f);
                });
                // drawn with a Vector4Field, same as Unity's own inspector shows a Quaternion field.
            }
            if(NeuroSyncTypes.IsEmpty<Matrix4x4>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref Matrix4x4 value) => {
                    neuro.Sync(1, nameof(value.m00), ref value.m00, 0f);
                    neuro.Sync(2, nameof(value.m10), ref value.m10, 0f);
                    neuro.Sync(3, nameof(value.m20), ref value.m20, 0f);
                    neuro.Sync(4, nameof(value.m30), ref value.m30, 0f);
                    neuro.Sync(5, nameof(value.m01), ref value.m01, 0f);
                    neuro.Sync(6, nameof(value.m11), ref value.m11, 0f);
                    neuro.Sync(7, nameof(value.m21), ref value.m21, 0f);
                    neuro.Sync(8, nameof(value.m31), ref value.m31, 0f);
                    neuro.Sync(9, nameof(value.m02), ref value.m02, 0f);
                    neuro.Sync(10, nameof(value.m12), ref value.m12, 0f);
                    neuro.Sync(11, nameof(value.m22), ref value.m22, 0f);
                    neuro.Sync(12, nameof(value.m32), ref value.m32, 0f);
                    neuro.Sync(13, nameof(value.m03), ref value.m03, 0f);
                    neuro.Sync(14, nameof(value.m13), ref value.m13, 0f);
                    neuro.Sync(15, nameof(value.m23), ref value.m23, 0f);
                    neuro.Sync(16, nameof(value.m33), ref value.m33, 0f);
                });
                // no built-in Matrix4x4 field, so let the object inspector draw the 16 components.
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m00));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m10));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m20));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m30));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m01));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m11));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m21));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m31));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m02));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m12));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m22));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m32));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m03));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m13));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m23));
                NeuroSyncEditorFields.AddField(typeof(Matrix4x4), nameof(Matrix4x4.m33));
            }

            if(NeuroSyncTypes.IsEmpty<Gradient>())
            {
                NeuroSyncEnumTypes<GradientMode>.Register(e => (int)e, i => (GradientMode)i);
                NeuroSyncTypes.Register((INeuroSync neuro, ref GradientColorKey value) =>
                {
                    neuro.Sync(1, nameof(value.color), ref value.color, default);
                    neuro.Sync(2, nameof(value.time), ref value.time, 0f);
                });
                NeuroSyncTypes.Register((INeuroSync neuro, ref GradientAlphaKey value) =>
                {
                    neuro.Sync(1, nameof(value.alpha), ref value.alpha, 0f);
                    neuro.Sync(2, nameof(value.time), ref value.time, 0f);
                });
                NeuroSyncTypes.Register((INeuroSync neuro, ref Gradient value) =>
                {
                    value ??= new Gradient();
                    // the keys are arrays on Unity's side, which Neuro does not do, so they go via lists.
                    var colorKeys = neuro.IsReading ? null : new List<GradientColorKey>(value.colorKeys);
                    var alphaKeys = neuro.IsReading ? null : new List<GradientAlphaKey>(value.alphaKeys);
                    var mode = value.mode;
                    neuro.Sync(1, nameof(value.colorKeys), ref colorKeys);
                    neuro.Sync(2, nameof(value.alphaKeys), ref alphaKeys);
                    neuro.SyncEnum(3, nameof(value.mode), ref mode, 0);
                    if (neuro.IsReading)
                    {
                        value.SetKeys(colorKeys?.ToArray() ?? Array.Empty<GradientColorKey>(),
                            alphaKeys?.ToArray() ?? Array.Empty<GradientAlphaKey>());
                        value.mode = mode;
                    }
                });
                // drawn with Unity's own GradientField.
            }
            if(NeuroSyncTypes.IsEmpty<AnimationCurve>())
            {
                NeuroSyncEnumTypes<WeightedMode>.Register(e => (int)e, i => (WeightedMode)i);
                NeuroSyncEnumTypes<WrapMode>.Register(e => (int)e, i => (WrapMode)i);
                NeuroSyncTypes.Register((INeuroSync neuro, ref Keyframe value) =>
                {
                    // they are properties so this is a long winded way :(
                    var time = value.time;
                    var val = value.value;
                    var inTangent = value.inTangent;
                    var outTangent = value.outTangent;
                    var inWeight = value.inWeight;
                    var outWeight = value.outWeight;
                    var weightedMode = value.weightedMode;
                    neuro.Sync(1, nameof(value.time), ref time, 0f);
                    neuro.Sync(2, nameof(value.value), ref val, 0f);
                    neuro.Sync(3, nameof(value.inTangent), ref inTangent, 0f);
                    neuro.Sync(4, nameof(value.outTangent), ref outTangent, 0f);
                    neuro.Sync(5, nameof(value.inWeight), ref inWeight, 0f);
                    neuro.Sync(6, nameof(value.outWeight), ref outWeight, 0f);
                    neuro.SyncEnum(7, nameof(value.weightedMode), ref weightedMode, 0);
                    if (neuro.IsReading)
                    {
                        value.time = time;
                        value.value = val;
                        value.inTangent = inTangent;
                        value.outTangent = outTangent;
                        value.inWeight = inWeight;
                        value.outWeight = outWeight;
                        value.weightedMode = weightedMode;
                    }
                });
                NeuroSyncTypes.Register((INeuroSync neuro, ref AnimationCurve value) =>
                {
                    value ??= new AnimationCurve();
                    var keys = neuro.IsReading ? null : new List<Keyframe>(value.keys);
                    var preWrapMode = value.preWrapMode;
                    var postWrapMode = value.postWrapMode;
                    neuro.Sync(1, nameof(value.keys), ref keys);
                    neuro.SyncEnum(2, nameof(value.preWrapMode), ref preWrapMode, 0);
                    neuro.SyncEnum(3, nameof(value.postWrapMode), ref postWrapMode, 0);
                    if (neuro.IsReading)
                    {
                        value.keys = keys?.ToArray() ?? Array.Empty<Keyframe>();
                        value.preWrapMode = preWrapMode;
                        value.postWrapMode = postWrapMode;
                    }
                });
                // drawn with Unity's own CurveField.
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