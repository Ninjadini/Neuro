using System;
using System.Collections.Generic;
using Ninjadini.Neuro;

public class CookSaveData
{
    [Neuro(1)] public List<OwnedCookItem> OwnedItems;
    [Neuro(2)] public List<CookRecipeInProgress> RecipesInProgress;
}

public class OwnedCookItem
{
    [Neuro(1)] public Reference<CookItem> Item;
    [Neuro(2)] public int Amount;
}

public struct CookRecipeInProgress
{
    [Neuro(1)] public Reference<CookRecipe> Recipe;
    
    [Neuro(2)] public DateTime TimeStarted; // User's device time when the recipe was started cooking.
}