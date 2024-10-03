using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(10)]
[DisplayName("CraftClicker: Items")]
public class CraftItem : Referencable
{
    [Neuro(1)] public string Name;
    
    [AssetType(typeof(Sprite))]
    [Neuro(2)] public AssetAddress Icon; // < The icon sprite to show in UI
    
    [Header("Crafting the item")]
    
    [Tooltip("When this item is crafted, how many items should it produce")] 
    [Neuro(3)] public int CraftOutputCount = 1;
    [Tooltip("Required items to produce this item")]
    [Neuro(4)] public List<CraftRecipeRequiredItem> RequiredItems;

    [InspectorStyle(spaceBefore:10)]
    [Tooltip("After crafting this item, any post craft status effects?")]
    [Neuro(5)] public PostCraftStatusEffectApplication PostCraftEffect;
}

public struct CraftRecipeRequiredItem
{
    [Neuro(1)] public Reference<CraftItem> Item;
    [Neuro(2)] public int Amount;
}

public class PostCraftStatusEffectApplication
{
    [Neuro(1)] public Reference<PostCraftStatusEffect> Effect;
    [Neuro(2)] public float Chance;
}
