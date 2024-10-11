using System.Collections.Generic;
using System.Text;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Utils;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CraftClickerUI : MonoBehaviour
{
    [SerializeField] Reference<CraftingStation> autoSelectStation;
    
    [SerializeField] CraftClickerLogic logic;
    [SerializeField] VisualTreeAsset stationItemTree;
    [SerializeField] VisualTreeAsset craftItemTree;

    VisualElement _root;
    List<VisualElement> _craftItems = new List<VisualElement>();
    StringBuilder _stringBuilder = new StringBuilder();
    
    async void Start()
    {
        await NeuroDataProvider.Shared.LoadFromResAsync();
        // ^ this is not required. Just showcasing that you can async load neuro config data.
        // Limitation is that you can't grab any data while its loading.
        // This is useful if you want to start loading things while your splash screen shows or intro animation is playing.
        
        var doc = GetComponent<UIDocument>();

        _root = doc.rootVisualElement;
        PopulateStationsList();

        var autoStation = autoSelectStation.GetValue();
        if (autoStation != null)
        {
            PopulateStation(autoStation);
        }
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
        _craftItems.Clear();
        foreach (var craftItemRef in station.CraftItems)
        {
            var craftItem = craftItemRef.GetValue();
            
            var craftItemElement = craftItemTree.CloneTree();

            var btn = Get<Button>(craftItemElement);
            // ^ if we don't do this, it'll always be the last item when you click any.
            btn.clicked += () =>
            {
                OnCraftItemClicked(craftItem);
            };
            craftItemElement.userData = craftItem;
            _craftItems.Add(craftItemElement);
            itemsHolder.Add(craftItemElement);
        }
        RefreshItems();
    }

    void RefreshItems()
    {
        foreach (var craftItemElement in _craftItems)
        {
            var craftItem = craftItemElement.userData as CraftItem;
            if (craftItem == null)
            {
                continue;
            }
            _stringBuilder.Clear();
            _stringBuilder.Append("<b>").AppendNum(craftItem.CraftOutputCount).AppendLine("x ").Append(craftItem.Name).AppendLine("</b>");

            if (craftItem.RequiredItems is { Count: > 0 })
            {
                _stringBuilder.AppendLine("\nRequires");
                foreach (var requiredItem in craftItem.RequiredItems)
                {
                    var ownedReqCount = logic.GetOwnedCount(requiredItem.Item);
                    _stringBuilder
                        .Append(requiredItem.Item.GetValue().Name)
                        .Append(" (");
                    if (ownedReqCount < requiredItem.Amount)
                    {
                        _stringBuilder.Append("<color=red>");
                    }
                    _stringBuilder
                        .AppendNum(logic.GetOwnedCount(requiredItem.Item))
                        .Append("/")
                        .AppendNum(requiredItem.Amount);
                    if (ownedReqCount < requiredItem.Amount)
                    {
                        _stringBuilder.Append("</color>");
                    }
                    _stringBuilder.AppendLine(")");
                }
            }
            
            _stringBuilder.Append("\nOwned: ").Append(logic.GetOwnedCount(craftItem));
            
            var btn = Get<Button>(craftItemElement);
            btn.text = _stringBuilder.ToString();
        }
    }

    void OnCraftItemClicked(CraftItem craftItem)
    {
        if (logic.CanCraftItem(craftItem))
        {
            logic.CraftItem(craftItem);
            RefreshItems();
        }
    }

    static T Get<T>(VisualElement parent, string name = null) where T : VisualElement
    {
        var first = parent.Query<T>(name).First();
        return first;
    }
}
