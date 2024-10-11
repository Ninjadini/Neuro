using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(301)]
[LinkedReference(typeof(MyTestRef))]
public class MyLinkedRef : Referencable
{
    [Neuro(1)] public uint Uint;
    [Neuro(2)] public string Str;
    [AssetType(typeof(Sprite))]
    [Neuro(3)] public AssetAddress Asset;
}