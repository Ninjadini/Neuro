using System;
using System.Collections.Generic;
using Ninjadini.Neuro;

/// Player save data that gets saved on device
public class CraftClickerSaveData
{
    [Neuro(1)] public DateTime CreationTime;
    [Neuro(2)] public DateTime LastSaveTime;
    [Neuro(3)] public readonly Dictionary<Reference<CraftItem>, DateTime> ItemCraftEndTimes = new ();
    [Neuro(5)] public readonly Dictionary<Reference<CraftItem>, OwnedCraftItem> OwnedItems = new ();
    
    //[Neuro(6)] public List<ActiveCraftStatusEffect> StatusEffects = new List<ActiveCraftStatusEffect>();
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