using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    [Serializable]
    public class NeuroEditorHistory
    {
        const int MaxHistoryItems = 15;
        
        [SerializeField] List<Item> BackItems = new List<Item>();
        [SerializeField] List<Item> ForwardItems = new List<Item>();
        
        [Serializable]
        public struct Item
        {
            public uint typeId;
            public uint refId;

            public bool Equals(Item other)
            {
                return typeId == other.typeId && refId == other.refId;
            }
            
            public NeuroDataFile FindDataFile(NeuroEditorDataProvider dataProvider)
            {
                var type = NeuroGlobalTypes.FindTypeById(typeId);
                if (type == null)
                {
                    return null;
                }
                return dataProvider.Find(type, refId);
            }
        }
        
        public bool HasBackItems => BackItems.Count > 0;
        public bool HasForwardItems => ForwardItems.Count > 0;

        public NeuroDataFile PopFromBackItems(NeuroEditorDataProvider dataProvider, NeuroDataFile currentItem = null)
        {
            while (BackItems.Count > 0)
            {
                var index = BackItems.Count - 1;
                var item = BackItems[index].FindDataFile(dataProvider);
                BackItems.RemoveAt(index);
                if (item?.Value == null)
                {
                    continue;
                }
                if (currentItem != null && currentItem != item)
                { 
                    ForwardItems.Add(AsRecentItem(currentItem));
                    if (ForwardItems.Count > MaxHistoryItems)
                    {
                        ForwardItems.RemoveAt(0);
                    }
                }
                return item;
            }
            return null;
        }

        public NeuroDataFile PopFromForwardItems(NeuroEditorDataProvider dataProvider, NeuroDataFile currentItem = null)
        {
            while (true)
            {
                var index = ForwardItems.Count - 1;
                if (index >= 0)
                {
                    var item = ForwardItems[index].FindDataFile(dataProvider);
                    ForwardItems.RemoveAt(index);
                    if (item?.Value == null)
                    {
                        continue;
                    }
                    AddToHistory(currentItem, true);
                    return item;
                }
                break;
            }
            return null;
        }

        public void AddToHistory(NeuroDataFile selectedItem, bool keepForwards = false)
        {
            if (selectedItem?.Value == null)
            {
                return;
            }
            var item = AsRecentItem(selectedItem);
            AddToSharedRecentHistory(selectedItem);
            if (BackItems.Count == 0 || !BackItems[^1].Equals(item))
            {
                BackItems.Add(item);
                if (BackItems.Count > NeuroEditorHistory.MaxHistoryItems)
                {
                    BackItems.RemoveAt(0);
                }
            }
            if (!keepForwards)
            {
                ForwardItems.Clear();
            }
        }

        public static Item AsRecentItem(NeuroDataFile item)
        {
            var typeId = NeuroGlobalTypes.GetTypeIdOrThrow(item.Value.GetType(), out _);
            var refId = item.RefId;
            return new Item {typeId = typeId, refId = refId};
        }

        public static NeuroDataFile FindItem(Item recentItem, NeuroEditorDataProvider dataProvider)
        {
            var type = NeuroGlobalTypes.FindTypeById(recentItem.typeId);
            if (type == null)
            {
                return null;
            }
            return dataProvider.Find(type, recentItem.refId);
        }
        
        static readonly string PrefHistoryKey = "NeuroEditorHistory" + NeuroEditorUtils.UniqueProjectPathHash;
        
        static List<Item> _sharedHistory;
        public static List<Item> SharedHistory
        {
            get
            {
                if (_sharedHistory != null)
                {
                    return _sharedHistory;
                }
                _sharedHistory = new List<Item>();
                var str = EditorPrefs.GetString(PrefHistoryKey, "");
                if (!string.IsNullOrEmpty(str))
                {
                    var items = str.Split(',');
                    for(var i = 0; i < items.Length; i += 2)
                    {
                        if (uint.TryParse(items[i], out var typeId) && uint.TryParse(items[i + 1], out var refId))
                        {
                            _sharedHistory.Add(new Item {typeId = typeId, refId = refId});
                        }
                    }
                }
                return _sharedHistory;
            }
        }
        
        public static void AddToSharedRecentHistory(NeuroDataFile itemFile)
        {
            var item = AsRecentItem(itemFile);
            var index = SharedHistory.FindIndex(other => other.Equals(item));
            if (index >= 0)
            {
                SharedHistory.RemoveAt(index);
                SharedHistory.Add(item);
                // we won't bother writing to prefs for this one.
                return;
            }
            SharedHistory.Add(item);
            if (SharedHistory.Count > MaxHistoryItems)
            {
                SharedHistory.RemoveRange(0, SharedHistory.Count - MaxHistoryItems);
            }
            var str = string.Join(",", SharedHistory.ConvertAll(i => $"{i.typeId},{i.refId}").ToArray());
            EditorPrefs.SetString(PrefHistoryKey, str);
        }

        public static string GetDropDownName(IReferencable value)
        {
            if (value is ISingletonReferencable)
            {
                return value.GetType().Name;
            }
            var str = value.RefName;
            str = string.IsNullOrEmpty(str) ? value.RefId.ToString() : $"{value.RefId} : {str}";
            return $"{value.GetType().Name} > {str}";
        }

    }
}