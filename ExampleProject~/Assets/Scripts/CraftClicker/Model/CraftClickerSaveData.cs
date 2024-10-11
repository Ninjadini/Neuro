using System;
using System.Collections.Generic;
using Ninjadini.Neuro;

public class CraftClickerSaveData
{
    [Neuro(1)] public DateTime SaveTime;
    [Neuro(3)] public readonly List<CraftingItemInProgress> ItemsInProgress = new List<CraftingItemInProgress>();
    [Neuro(5)] public readonly Dictionary<Reference<CraftItem>, OwnedCraftItem> OwnedItems = new Dictionary<Reference<CraftItem>, OwnedCraftItem>();
    
    //[Neuro(6)] public List<ActiveCraftStatusEffect> StatusEffects = new List<ActiveCraftStatusEffect>();
}

public class CraftingItemInProgress
{
    [Neuro(1)] public Reference<CraftItem> Item;
    [Neuro(2)] public DateTime StartedTime;
}


public class OwnedCraftItem
{
    [Neuro(1)] public int Amount;
    // Right now we only have 1 field, but we keep it as a class so that we may have more later.
}


/*
public class ActiveCraftStatusEffect
{
    public Reference<PostCraftStatusEffect> Effect;
    public Reference<CraftItem> FromItem;
    public int AppliedCount;
    public DateTime StartedTime;
}*/