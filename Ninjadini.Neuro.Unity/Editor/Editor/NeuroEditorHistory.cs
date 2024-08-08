using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using Ninjadini.Toolkit;
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
            backBtn.style.width = 80;
            
            forwardBtn = NeuroUiUtils.AddButton(this, "↳ Forward", ForwardBtnClicked);
            forwardBtn.style.width = 75;
            
            recentBtn = NeuroUiUtils.AddButton(this, "⋮ Recent", ForwardBtnClicked);
            recentBtn.style.width = 70;
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

        RecentItem AsRecentItem(NeuroDataFile item)
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
    }
}