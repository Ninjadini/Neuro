using System;
using Ninjadini.Neuro;
using UnityEngine;

public class CraftClickerLogic : MonoBehaviour
{
    LocalNeuroContinuousSave<CraftClickerSaveData> _gameSave;
    CraftClickerSaveData _data;

    void Start()
    {
        _gameSave = LocalNeuroContinuousSave<CraftClickerSaveData>.CreateInPersistedData("save");

        _data = _gameSave.GetData();
        
        Debug.Log(_data);
        Save();
    }

    void Save()
    {
        _data.SaveTime = DateTime.Now;
        _gameSave.Save();
    }

    void OnDestroy()
    {
        _gameSave?.Dispose();
    }
}