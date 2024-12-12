using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(14)]
[DisplayName("CraftClicker: Settings")]
public class CraftClickerSettings : ISingletonReferencable
{
    [Neuro(1)] 
    public string SaveFileName = "save";
    
    [Neuro(3)] 
    [AssetType(typeof(Sprite))]
    public AssetAddress DefaultItemIcon;

    [Neuro(5)] 
    public bool EffectsFeatureEnabled;
}