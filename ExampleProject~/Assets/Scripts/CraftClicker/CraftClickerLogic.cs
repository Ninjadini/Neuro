using System;
using System.Collections.Generic;
using Ninjadini.Neuro;
using UnityEngine;

public class CraftClickerLogic : MonoBehaviour
{
    [SerializeField] string saveFileName = "save";
    
    LocalNeuroContinuousSave<CraftClickerSaveData> _gameSave;
    CraftClickerSaveData Data => _gameSave.GetData();

    void Start()
    {
        if (string.IsNullOrEmpty(saveFileName))
        {
            saveFileName = "save";
        }
        _gameSave = LocalNeuroContinuousSave<CraftClickerSaveData>.CreateInPersistedData(saveFileName);
    }

    public bool CanCraftItem(CraftItem item)
    {
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
                AddOwnedCount(requiredItem.Item, -requiredItem.Amount);
            }
        }
        AddOwnedCount(item, item.CraftOutputCount);
        
        Save(); 
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

    public int GetOwnedCount(Reference<CraftItem> item)
    {
        return GetOwned(item)?.Amount ?? 0;
    }

    public OwnedCraftItem GetOwned(Reference<CraftItem> item)
    {
        OwnedCraftItem result = null;
        Data.OwnedItems.TryGetValue(item, out result);
        return result;
    }

    void Save()
    {
        Data.SaveTime = DateTime.Now;
        _gameSave.Save();
        // ^ in the real world, you probably don't want to save on evey data change
        // maybe delay the save for 5 seconds so that if you did like 3 actions within 5 seconds it's all
        // rolled into just 1 save call.
    }

    void OnDestroy()
    {
        _gameSave?.Dispose();
    }
}