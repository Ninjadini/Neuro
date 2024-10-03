using Ninjadini.Neuro;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CraftClickerUI : MonoBehaviour
{
    [SerializeField] CraftClickerLogic logic;
    [SerializeField] VisualTreeAsset stationItemTree;
    [SerializeField] VisualTreeAsset craftItemTree;

    VisualElement _root;
    
    void Start()
    {
        var doc = GetComponent<UIDocument>();

        _root = doc.rootVisualElement;
        PopulateStationsList();
    }

    void PopulateStationsList()
    {
        var stationsHolder = Get<VisualElement>(_root, "stationsHolder");

        var stationsTable = NeuroDataProvider.GetSharedTable<CraftingStation>();

        foreach (var stationConfig in stationsTable.SelectAll())
        {
            var stationItemElement = stationItemTree.CloneTree();

            var btn = Get<Button>(stationItemElement);
            btn.text = stationConfig.Name;
            var localStationConfig = stationConfig; 
            // ^ if we don't do this, it'll always be the last item when you click any.
            btn.clicked += () =>
            {
                PopulateStation(localStationConfig);
            };
            stationsHolder.Add(stationItemElement);
        }
    }

    void PopulateStation(CraftingStation station)
    {
        var stationNameLbl = Get<Label>(_root, "stationName");
        stationNameLbl.text = station.Name;
        
        var itemsHolder = Get<VisualElement>(_root, "itemsHolder");
        itemsHolder.Clear();
        foreach (var craftItemRef in station.CraftItems)
        {
            var craftItem = craftItemRef.GetValue();
            
            var craftItemElement = craftItemTree.CloneTree();

            var btn = Get<Button>(craftItemElement);
            btn.text = craftItem.Name;
            // ^ if we don't do this, it'll always be the last item when you click any.
            btn.clicked += () =>
            {
                OnCraftItemClicked(craftItem);
            };
            itemsHolder.Add(craftItemElement);
        }
    }

    void OnCraftItemClicked(CraftItem craftItem)
    {
        
    }

    static T Get<T>(VisualElement parent, string name = null) where T : VisualElement
    {
        var first = parent.Query<T>(name).First();
        return first;
    }
}
