using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(10)]
[DisplayName("CraftClicker: Items []")]
public class CraftItem : Referencable, INeuroRefDropDownCustomizable
{
    [Neuro(1)] public string Name;
    
    [Neuro(2)] [AssetType(typeof(Sprite))] 
    public AssetAddress Icon; // < The icon sprite to show in UI
    
    [Header(">Crafting the item")] // starting with '>' makes it a fold out
    
    [Neuro(3)] [Tooltip("When this item is crafted, how many items should it produce")] 
    public int CraftOutputCount = 1;

    [Neuro(6)] [Tooltip("How long does it take to craft")]
    public TimeSpan CraftDuration;
    
    [Neuro(4)] [Tooltip("Required items to produce this item")]
    public readonly List<CraftRecipeRequiredItem> RequiredItems = new List<CraftRecipeRequiredItem>();
    
    [Neuro(5)] [Tooltip("After crafting this item, any post craft status effects?")] [InspectorStyle(spaceBefore:10)]
    public PostCraftStatusEffectApplication PostCraftEffect;

    
    string INeuroRefDropDownCustomizable.GetRefDropdownText(NeuroReferences references)
    {
        // this is so that we don't need to assign the RefName in editor and it'll still show the name.
        return string.IsNullOrEmpty(RefName) ? $"{RefId} : {Name}" : null;
    }
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
