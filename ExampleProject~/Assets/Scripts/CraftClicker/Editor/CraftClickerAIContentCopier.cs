using System;
using System.Collections.Generic;
using System.Linq;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CraftClickerAIContentCopier : EditorWindow
{
    [MenuItem("Tools/CraftClicker/AI content copier")]
    public static void ShowWindow()
    {
        CreateWindow<CraftClickerAIContentCopier>("AI content copier").Show();
    }

    // ok I might have taken less time to just do the content myself
    // but I wanted to give some examples on how to modify the data in editor as well.
    
    void CreateGUI()
    {
        rootVisualElement.Add(AddAiPromptItems());
        rootVisualElement.Add(AddInputItems());
    }

    VisualElement AddAiPromptItems()
    {
        var foldout = new Foldout();
        foldout.text = "1. AI text prompt";
        foldout.Add(new Label("Ask AI such as ChatGPT the question below"));

        var scrollView = new ScrollView();
        foldout.Add(scrollView);

        var textField = new TextField()
        {
            value = @"I am making a crafting simulation game where there are 5 stations, each station must contain craftable items.
Each craftable item may require 0-3 other items to be produced, the other required item must exist in the full craftable list. For each type of required item, it maybe vary between 1 to 3 count.
Craftable item has varying number of output count which starts high for lower tier items such as 3 and reduce to 1 on later tech items. There is also duration it takes to produce, it start from 1 seconds and increase exponentially up to 1 hour as the complexity increases.  
The first initial station starts with basic craftable items such as wood, stone, etc. These first items from station does not need any other items to produce. 
The last item of the last station ends up crafting a spaceship. All other items must be part of the dependencies of the spaceship so that it is required to be crafted at some point.
I want you to create this crafting tech tree where you start off from basic wood and stone items to all leading to finally building a spaceship. 
Reminder that there should be 5 stations in total and each station produce 5 items.

The format you should create is as follows:
{
""CraftableItems"":[
{
""RefId"": <unique number id of item>,
""Name"": ""<name of item>"",
""CraftOutputCount"": <number of item output count>,
""CraftDuration"": <duration to produce in milliseconds>
""RequiredItems"": [{ Item: <unique number id of other item>, Amount: <amount required for other item } ]
}
],
""CraftingStations"": [
{
""Name"": ""<name of station>"",
""CraftItems"": [ <unique number id of craftable item> ]
}]
}"
        };
        textField.isReadOnly = true;
        scrollView.Add(textField);
        foldout.Add(new VisualElement()
        {
            style =
            {
                height = 20
            }
        });
        return foldout;
    }

    [SerializeField] string jsonText;

    VisualElement AddInputItems()
    {
        var container = new VisualElement();
        var foldout = new Foldout();
        foldout.text = "2. Paste the JSON result";
        container.Add(foldout);
        var scrollView = new ScrollView();
        foldout.Add(scrollView);
        var itemsTf = new TextField()
        {
            value = jsonText,
            multiline = true
        };
        itemsTf.RegisterValueChangedCallback((evt) =>
        {
            jsonText = evt.newValue;
        });
        scrollView.Add(itemsTf);
        
        NeuroUiUtils.AddButton(container, "Apply", () =>
        {
            jsonText = itemsTf.value;
            ApplyTexts();
        });
        return container;
    }

    void ApplyTexts()
    {
        try
        {
            var jsonObj = NeuroJsonReader.Shared.Read<JsonResultWrapper>(jsonText);

            if (!EditorUtility.DisplayDialog("Apply", $"Apply {jsonObj.CraftableItems.Count} items and {jsonObj.CraftingStations.Count} stations?\nThis will delete all existing craft items and stations.\nYou will have to reassign the icons.", "OK", "Cancel"))
            {
                return;
            }
            DeleteExistingItems();
            var editorData = NeuroEditorDataProvider.Shared;
            
            var itemId = 0u;
            foreach (var item in jsonObj.CraftableItems)
            {
                item.RefId = ++itemId;
                item.RefName = item.Name.ToLower().Replace(" ", "_");
                editorData.Add(item);
            }
            foreach (var item in jsonObj.CraftingStations)
            {
                item.RefName = item.Name.ToLower().Replace(" ", "_");
                editorData.Add(item);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Error", "There was an error, look at the log pls", "OK");
        }
    }

    void DeleteExistingItems()
    {
        var editorData = NeuroEditorDataProvider.Shared;
        var craftItemsTable = editorData.References.GetTable<CraftItem>();
        foreach (var craftItem in craftItemsTable.SelectAll().ToArray())
        {
            var dataFile = editorData.Find(craftItem);
            if (dataFile != null)
            {
                editorData.Delete(dataFile);
            }
        }
        var stationsTable = editorData.References.GetTable<CraftingStation>();
        foreach (var craftingStation in stationsTable.SelectAll().ToArray())
        {
            var dataFile = editorData.Find(craftingStation);
            if (dataFile != null)
            {
                editorData.Delete(dataFile);
            }
        }
    }

    public class JsonResultWrapper
    {
        [Neuro(1)] public List<CraftItem> CraftableItems;
        [Neuro(2)] public List<CraftingStation> CraftingStations;
    }

}