using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(11)]
[DisplayName("CookClicker: Recipes")]
public class CookRecipe : Referencable
{
    [Tooltip("The result from cooking this recipe")]
    [Neuro(1)] public CookItem ResultItem;
    
    [Tooltip("Required items for this recipe, the item and its amount")]
    [Neuro(2)] public List<CookRecipeRequiredItem> RequiredItems;
    
    [Tooltip("The time it takes to finish cooking this recipe")]
    [Neuro(3)] public TimeSpan CookDuration;
    
    [Neuro(4)] public List<CookStatusEffectApplication> PostCookStatusEffects;
    
    [Neuro(5)] public bool CanPickMultipleEffects;
}

public struct CookRecipeRequiredItem
{
    [Neuro(1)] public Reference<CookItem> Item;
    [Neuro(2)] public int Amount;
}

public struct CookStatusEffectApplication
{
    [Neuro(1)] public Reference<CookStatusEffect> Effect;
    [Neuro(2)] public float Chance;
}