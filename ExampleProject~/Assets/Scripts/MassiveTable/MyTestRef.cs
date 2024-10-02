using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(302)]
[DisplayName("MyTestRef (custom name via attribute)")]
public class MyTestRef : Referencable
{
    [Neuro(6)] public Vector3 UnityVector3;
    [Tooltip("Rect tooltip here")]
    [Neuro(7)] public Rect UnityRect;
    [Header(">String header")]
    [Neuro(2)] public string Str;
    
    [Header(">Objects")]
    [Tooltip("Asset tooltip here")]
    [AssetType(typeof(Sprite))]
    [Neuro(3)] public AssetAddress Asset;
    [AssetType(typeof(Canvas))]
    [Neuro(4)] public AssetAddress Component;
}