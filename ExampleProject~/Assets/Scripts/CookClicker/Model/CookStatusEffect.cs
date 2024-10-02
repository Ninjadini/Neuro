using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(12)]
[DisplayName("CookClicker: Status Effects")]
public abstract class CookStatusEffect : Referencable
{
    [Neuro(1)] public string Name;
    
    [AssetType(typeof(Sprite))]
    [Neuro(2)] public AssetAddress Icon; // < The icon sprite to show in UI
}