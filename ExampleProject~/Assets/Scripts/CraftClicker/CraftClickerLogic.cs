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
    }

    void OnDestroy()
    {
        _gameSave?.Dispose();
    }
}