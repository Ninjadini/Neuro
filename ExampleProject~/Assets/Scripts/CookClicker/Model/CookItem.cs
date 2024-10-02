using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(10)]
[DisplayName("CookClicker: Items")]
[Tooltip("All possible types of cooking items, including just ingredients and meals. This is because some 'meals' can be ingredients to another meal.")]
public class CookItem : Referencable
{
    [Neuro(1)] public string Name;
    
    [AssetType(typeof(Sprite))]
    [Neuro(2)] public AssetAddress Icon; // < The icon sprite to show in UI
}