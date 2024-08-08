using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using Ninjadini.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    internal class NeuroEditorHistory : VisualElement
    {
        NeuroEditorDataProvider dataProvider;
        Action<NeuroDataFile> onSelect;
        Func<NeuroDataFile> getSelected;
        Button backBtn;
        Button recentBtn;
        Button forwardBtn;

        HistoryData _historyData;

        public NeuroEditorHistory(NeuroEditorDataProvider dataProvider, Action<NeuroDataFile> onSelect,
            Func<NeuroDataFile> getSelected)
        {
            this.dataProvider = dataProvider;
            this.onSelect = onSelect;
            this.getSelected = getSelected;
            
            backBtn = NeuroUiUtils.AddButton(this, "↰ Back", BackBtnClicked);
            backBtn.style.width = 65;
            backBtn.style.flexShrink = 1f;
            
            forwardBtn = NeuroUiUtils.AddButton(this, "↳ Forward", ForwardBtnClicked);
            forwardBtn.style.width = 72;
            forwardBtn.style.flexShrink = 1f;
            
            recentBtn = NeuroUiUtils.AddButton(this, "⋮ Recent", HistoryBtnClicked);
            recentBtn.style.width = 65;
            recentBtn.style.flexShrink = 1f;
        }

        public void SetHistoryData(HistoryData historyData_)
        {
            _historyData = historyData_;
        }

        public void AddCurrentToHistory(bool keepForwards = false)
        {
            var selectedItem = getSelected();
            if (selectedItem?.Value == null)
            {
                return;
            }
            var item = AsRecentItem(selectedItem);
            AddToSharedRecentHistory(selectedItem);
            if (backs.Count == 0 || !backs[^1].Equals(item))
            {
                backs.Add(item);
            }
            if (!keepForwards)
            {
                forwards.Clear();
            }
        }

        public void UpdateBackForwardBtns()
        {
            forwardBtn.SetEnabled(forwards.Count > 0);
            backBtn.SetEnabled(backs.Count > 0);
        }
        
        void BackBtnClicked()
        {
            while (backs.Count > 0)
            {
                var index = backs.Count - 1;
                var item = FindItem(backs[index]);
                backs.RemoveAt(index);
                if (item?.Value == null)
                {
                    continue;
                }
                var selectedItem = getSelected();
                if (selectedItem != null && selectedItem != item)
                { 
                    forwards.Add(AsRecentItem(selectedItem));
                }
                onSelect(item);
                break;
            }
        }

        void ForwardBtnClicked()
        {
            while (true)
            {
                var index = forwards.Count - 1;
                if (index >= 0)
                {
                    var item = FindItem(forwards[index]);
                    forwards.RemoveAt(index);
                    if (item?.Value == null)
                    {
                        continue;
                    }
                    AddCurrentToHistory(true);
                    onSelect(item);
                }
                break;
            }
        }

        void HistoryBtnClicked()
        {
            var menu = new GenericMenu();
            var history = SharedHistory;

            var current = getSelected();
            
            for (var i = history.Count - 1; i >= 0; i--)
            {
                var item = FindItem(history[i]);
                if (item == current)
                {
                    continue;
                }
                var value = item?.Value;
                if(value == null)
                {
                    continue;
                }
                menu.AddItem(new GUIContent(GetDropDownName(value)), false, () =>
                {
                    onSelect(item);
                });
            }
            if (history.Count == 0)
            {
                menu.AddSeparator("No recent items");
            }
            menu.ShowAsContext();
        }

        static RecentItem AsRecentItem(NeuroDataFile item)
        {
            var typeId = NeuroGlobalTypes.GetTypeIdOrThrow(item.Value.GetType(), out _);
            var refId = item.RefId;
            return new RecentItem {typeId = typeId, refId = refId};
        }

        NeuroDataFile FindItem(RecentItem recentItem)
        {
            var type = NeuroGlobalTypes.FindTypeById(recentItem.typeId);
            if (type == null)
            {
                return null;
            }
            return dataProvider.Find(type, recentItem.refId);
        }
        
        List<RecentItem> backs
        {
            get
            {
                _historyData ??= new HistoryData();
                return _historyData.BackItems;
            }
        }

        List<RecentItem> forwards
        {
            get
            {
                _historyData ??= new HistoryData();
                return _historyData.ForwardItems;
            }
        }

        [Serializable]
        internal class HistoryData
        {
            public List<RecentItem> BackItems = new List<RecentItem>();
            public List<RecentItem> ForwardItems = new List<RecentItem>();
        }
        
        [Serializable]
        internal struct RecentItem
        {
            public uint typeId;
            public uint refId;

            public bool Equals(RecentItem other)
            {
                return typeId == other.typeId && refId == other.refId;
            }
        }

        const string PrefHistoryKey = "Neuro.Editor.History";
        const int MaxHistoryItems = 15;
        
        static List<RecentItem> _sharedHistory;
        static List<RecentItem> SharedHistory
        {
            get
            {
                if (_sharedHistory != null)
                {
                    return _sharedHistory;
                }
                _sharedHistory = new List<RecentItem>();
                var str = EditorPrefs.GetString(PrefHistoryKey, "");
                if (!string.IsNullOrEmpty(str))
                {
                    var items = str.Split(',');
                    for(var i = 0; i < items.Length; i+=2)
                    {
                        if (uint.TryParse(items[i], out var typeId) && uint.TryParse(items[i + 1], out var refId))
                        {
                            _sharedHistory.Add(new RecentItem {typeId = typeId, refId = refId});
                        }
                    }
                }
                return _sharedHistory;
            }
        }
        public static void AddToSharedRecentHistory(NeuroDataFile itemFile)
        {
            var item = AsRecentItem(itemFile);
            if (SharedHistory.Exists(other => other.Equals(item)))
            {
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