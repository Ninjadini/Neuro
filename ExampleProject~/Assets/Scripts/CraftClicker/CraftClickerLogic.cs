using System;
using System.Collections.Generic;
using Ninjadini.Neuro;
using UnityEngine;

public class CraftClickerLogic : MonoBehaviour
{
    [SerializeField] CraftClickerSaveManager saveManager;
    
    CraftClickerSaveData Data => saveManager.GetData();
    
    public bool CanCraftItem(CraftItem item)
    {
        if (Data.ItemCraftEndTimes.ContainsKey(item))
        {
            return false;
        }
        if (item.RequiredItems != null)
        {
            foreach (var requiredItem in item.RequiredItems)
            {
                if (GetOwnedCount(requiredItem.Item) < requiredItem.Amount)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void CraftItem(CraftItem item)
    {
        if (item.RequiredItems != null)
        {
            foreach (var requiredItem in item.RequiredItems)
            {
                // deduct the required items
                AddOwnedCount(requiredItem.Item, - requiredItem.Amount);
            }
        }
        // set the time it'll finish crafting
        Data.ItemCraftEndTimes[item] = GetTime() + item.CraftDuration;
        
        saveManager.Save(); 
    }

    void AddOwnedCount(Reference<CraftItem> item, int amount)
    {
        var owned = GetOwned(item);
        if (owned != null)
        {
            owned.Amount += amount;
            if (owned.Amount < 0)
            {
                throw new Exception($"{item.TryGetIdAndName()}'s owned amount has become negative");
            }
        }
        else
        {
            if (amount < 0)
            {
                throw new Exception($"{item.TryGetIdAndName()}'s owned amount can not be negative");
            }
            Data.OwnedItems.Add(item, new OwnedCraftItem()
            {
                Amount = amount
            });
        }
    }

    void Update()
    {
        UpdateCraftingItems();
    }

    readonly List<Reference<CraftItem>> _itemsReadyToFinish = new List<Reference<CraftItem>>();
    void UpdateCraftingItems()
    {
        var timeNow = GetTime();
        _itemsReadyToFinish.Clear();
        foreach (var (item, craftFinishTime) in Data.ItemCraftEndTimes)
        {
            if (craftFinishTime <= timeNow)
            {
                // this item is ready to finish crafting...
                _itemsReadyToFinish.Add(item);
            }
        }
        foreach (var itemRef in _itemsReadyToFinish)
        {
            Data.ItemCraftEndTimes.Remove(itemRef);
            var craftOutputCount = itemRef.GetValue().CraftOutputCount;
            AddOwnedCount(itemRef, craftOutputCount);
        }
    }
    
    DateTime GetTime()
    {
        return DateTime.UtcNow;
    }

    public int GetOwnedCount(Reference<CraftItem> item)
    {
        return GetOwned(item)?.Amount ?? 0;
    }

    public OwnedCraftItem GetOwned(Reference<CraftItem> item)
    {
        Data.OwnedItems.TryGetValue(item, out var result);
        return result;
    }

    public float? GetCraftingProgress(Reference<CraftItem> itemRef)
    {
        if (Data.ItemCraftEndTimes.TryGetValue(itemRef, out var endTime))
        {
            var item = itemRef.GetValue();
            var currentTime = GetTime();
            return 1f - Mathf.Clamp01((float)((endTime - currentTime).TotalSeconds / item.CraftDuration.TotalSeconds));
        }
        return null;
    }
}