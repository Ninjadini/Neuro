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

    public bool CraftItem(CraftItem item)
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
            foreach (var requiredItem in item.RequiredItems)
            {
                AddOwnedCount(requiredItem.Item, -requiredItem.Amount);
            }
        }
        AddOwnedCount(item, item.CraftOutputCount);
        Save();
        return true;
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
            Data.OwnedItems.Add(new OwnedCraftItem()
            {
                Item = item,
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
        foreach (var ownedItem in Data.OwnedItems)
        {
            if (ownedItem.Item == item)
            {
                return ownedItem;
            }
        }
        return null;
    }

    void Save()
    {
        Data.SaveTime = DateTime.Now;
        _gameSave.Save();
    }

    void OnDestroy()
    {
        _gameSave?.Dispose();
    }
}