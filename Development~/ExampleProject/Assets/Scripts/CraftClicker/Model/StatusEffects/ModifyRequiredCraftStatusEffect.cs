using System.Collections.Generic;
using Ninjadini.Neuro;
using UnityEngine;

[Neuro(2)]
//[Tooltip("Modifies the required items to craft a specific item. for example a chance to start producing things for cheaper...")]
[Tooltip("WARNING: These are not hooked up to be functional yet")]
public class ModifyRequiredCraftStatusEffect : PostCraftStatusEffect
{
    [Neuro(1)] 
    public Reference<CraftItem> ForItem;
    
    [Neuro(2)] [Tooltip("New required items to produce this item")]
    public List<CraftRecipeRequiredItem> RequiredItems;
}