using System;
using Ninjadini.Neuro;
using UnityEngine;

public class CraftClickerSaveManager : MonoBehaviour
{
    LocalNeuroContinuousSave<CraftClickerSaveData> _gameSave;
    
    void Start()
    {
        var settings = NeuroDataProvider.GetSharedSingleton<CraftClickerSettings>();
        var saveFileName = string.IsNullOrEmpty(settings?.SaveFileName) ? "save" : settings.SaveFileName;
        _gameSave = LocalNeuroContinuousSave<CraftClickerSaveData>.CreateInPersistedData(saveFileName);
    }

    public CraftClickerSaveData GetData()
    {
        return _gameSave.GetData();
    }

    public void Save()
    {
        GetData().SaveTime = DateTime.Now;
        _gameSave.Save();
        // ^ in the real world, you probably don't want to save on evey data change
        // maybe delay the save for 5 seconds so that if you did like 3 actions within 5 seconds it's all
        // rolled into just 1 save call.
    }

    void OnDestroy()
    {
        _gameSave?.Dispose();
        //^ Because we keep the file open for writing very fast, we need to close it when you stop playing.
    }
}