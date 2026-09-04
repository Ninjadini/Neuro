using System.Collections.Generic;
using System.Linq;
using Ninjadini.Neuro;
using UnityEngine;

public class CraftClickerUI : MonoBehaviour
{
    [SerializeField] CraftClickerLogic logic;
    [SerializeField] CraftingStationUI stationPrefab;
    [SerializeField] CraftItemUI itemPrefab;

    CraftingStationUI _selectedStation;
    readonly List<CraftItemUI> _drawnItems = new List<CraftItemUI>();
    
    void Start()
    {
        PopulateStations();
        itemPrefab.gameObject.SetActive(false);
    }

    void PopulateStations()
    {
        var stationsTable = NeuroDataProvider.GetSharedTable<CraftingStation>();

        foreach (var station in stationsTable.SelectAll().OrderBy(s => s.RefId))
        {
            var stationUI = Instantiate(stationPrefab, stationPrefab.transform.parent);
            stationUI.Populate(station);
            stationUI.Button.onClick.AddListener(() =>
            {
                OnStationClicked(stationUI);
            });
            stationUI.gameObject.SetActive(true);
        }
        stationPrefab.gameObject.SetActive(false);
    }

    void OnStationClicked(CraftingStationUI stationUI)
    {
        if (_selectedStation)
        {
            _selectedStation.Button.interactable = true;
        }
        _selectedStation = stationUI;
        stationUI.Button.interactable = false;
        DrawStationItems(stationUI.Station);
    }

    void DrawStationItems(CraftingStation station)
    {
        foreach (var drawnItem in _drawnItems)
        {
            Destroy(drawnItem.gameObject);
        }
        _drawnItems.Clear();
        foreach (var item in station.CraftItems)
        {
            var itemUI = Instantiate(itemPrefab, itemPrefab.transform.parent);
            itemUI.Populate(item.GetValue(), logic);
            itemUI.Button.onClick.AddListener(() =>
            {
                OnItemClicked(itemUI);
            });
            itemUI.gameObject.SetActive(true);
            _drawnItems.Add(itemUI);
        }
    }

    void OnItemClicked(CraftItemUI itemUI)
    {
        if (logic.CanCraftItem(itemUI.Item))
        {
            logic.CraftItem(itemUI.Item);
        }
    }
}
