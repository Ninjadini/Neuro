using System.Collections.Generic;
using Ninjadini.Neuro;
using UnityEngine;

[Neuro(2)]
[Tooltip("Modifies the required items to craft a specific item. for example a chance to start producing things for cheaper...")]
public class ModifyRequiredCraftStatusEffect : PostCraftStatusEffect
{
    [Neuro(1)] public Reference<CraftItem> ForItem;
    [Tooltip("New required items to produce this item")]
    [Neuro(2)] public List<CraftRecipeRequiredItem> RequiredItems;
}