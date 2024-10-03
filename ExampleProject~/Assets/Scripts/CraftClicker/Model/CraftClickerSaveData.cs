using System;
using System.Collections.Generic;
using Ninjadini.Neuro;

public class CraftClickerSaveData
{
    [Neuro(1)] public DateTime SaveTime;
    [Neuro(4)] public List<OwnedCraftItem> OwnedItems;
    [Neuro(6)] public List<ActiveCraftStatusEffect> StatusEffects;
}

public class OwnedCraftItem
{
    [Neuro(1)] public Reference<CraftItem> Item;
    [Neuro(2)] public int Amount;
}

public class ActiveCraftStatusEffect
{
    public Reference<PostCraftStatusEffect> Effect;
    public Reference<CraftItem> FromItem;
    public int AppliedCount;
    public DateTime StartedTime;
}