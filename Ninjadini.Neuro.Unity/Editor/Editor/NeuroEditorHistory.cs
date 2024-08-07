using System;
using System.Collections.Generic;
using Ninjadini.Toolkit;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    internal class NeuroEditorHistory : VisualElement
    {
        Action<NeuroDataFile> onSelect;
        Func<NeuroDataFile> getSelected;
        Button backBtn;
        Button forwardBtn;
        
        List<NeuroDataFile> history = new List<NeuroDataFile>();
        List<NeuroDataFile> forwards = new List<NeuroDataFile>();

        public NeuroEditorHistory(Action<NeuroDataFile> onSelect, Func<NeuroDataFile> getSelected)
        {
            this.onSelect = onSelect;
            this.getSelected = getSelected;
            backBtn = NeuroUiUtils.AddButton(this, "↰ Back", BackBtnClicked);
            //backBtn.style.minWidth = 100;
            backBtn.style.flexGrow = 1;
            forwardBtn = NeuroUiUtils.AddButton(this, "↳ Forward", ForwardBtnClicked);
            //forwardBtn.style.minWidth = 100;
            forwardBtn.style.flexGrow = 1;
        }
        
        public void AddCurrentToHistory(bool keepForwards = false)
        {
            var selectedItem = getSelected();
            if (selectedItem != null && (history.Count == 0 || history[^1] != selectedItem))
            {
                history.Add(selectedItem);
            }
            if (!keepForwards)
            {
                forwards.Clear();
            }
        }

        public void UpdateBackForwardBtns()
        {
            forwardBtn.SetEnabled(forwards.Count > 0);
            backBtn.SetEnabled(history.Count > 0);
        }
        
        void BackBtnClicked()
        {
            while (history.Count > 0)
            {
                var index = history.Count - 1;
                var item = history[index];
                history.RemoveAt(index);
                if (item.Value == null)
                {
                    continue;
                }
                var selectedItem = getSelected();
                if (selectedItem != null && selectedItem != item)
                { 
                    forwards.Add(selectedItem);
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
                    var item = forwards[index];
                    forwards.RemoveAt(index);
                    if (item.Value == null)
                    {
                        continue;
                    }
                    AddCurrentToHistory(true);
                    onSelect(item);
                }
                break;
            }
        }
    }
}