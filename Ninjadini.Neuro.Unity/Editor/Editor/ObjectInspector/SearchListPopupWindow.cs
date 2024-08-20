using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class SearchablePopupField<TValueChoice> : PopupField<TValueChoice>
    {
        Func<TValueChoice, string> getStringFunc;
        
        public Action BeforePopupShown;
        public Action<TValueChoice> ValueChanged;
        
        public SearchablePopupField()
        {
            RegisterCallback<PointerDownEvent>(OnDropDownBtnDown, TrickleDown.TrickleDown);
        }

        public void SetFormatFunc(Func<TValueChoice, string> getStringFunc)
        {
            this.getStringFunc = getStringFunc;
            formatListItemCallback = getStringFunc;
            formatSelectedValueCallback = getStringFunc;
        }
        
        string StrFunc(TValueChoice choice)
        {
            var str = getStringFunc != null ? getStringFunc(choice) : choice.ToString();
            if (EqualityComparer<TValueChoice>.Default.Equals(choice, value))
            {
                return "✔ " + str;
            }
            return "    " + str;
        }

        void OnDropDownBtnDown(PointerDownEvent evt)
        {
            if (evt.currentTarget == evt.target)
            {
                BeforePopupShown?.Invoke();
                var rect = worldBound;
                var lbl = labelElement;
                if (lbl != null && lbl.visible)
                {
                    var w = lbl.worldBound.width;
                    if (w > 0f)
                    {
                        rect.xMin += w;
                    }
                }
                var window = new SearchListPopupWindow(Math.Max(320, (int)rect.width), choices, StrFunc, OnChoiceSelected);
                SetupWindow(window);
                UnityEditor.PopupWindow.Show(rect, window);
                evt.StopPropagation();
            }
        }

        protected virtual void SetupWindow(SearchListPopupWindow window)
        {
            
        }

        void OnChoiceSelected(TValueChoice obj)
        {
            value = obj;
        }

        public class SearchListPopupWindow : PopupWindowContent
        {
            int width;
            List<TValueChoice> fullChoices;
            List<TValueChoice> filteredChoices;
            Func<TValueChoice, string> getStringFunc;
            Action<TValueChoice> selectedAct;
            Action cancelledAct;
            ToolbarSearchField searchField;
            ListView listView;

            public Func<VisualElement> MakeItemOverride;
            public Action<VisualElement, TValueChoice> BindItemOverride;

            public SearchListPopupWindow(int width_, 
                List<TValueChoice> choices, 
                Func<TValueChoice, string> getStringFunc_, 
                Action<TValueChoice> selectedAct_,
                Action cancelledAct_ = null)
            {
                width = width_;
                fullChoices = choices;
                getStringFunc = getStringFunc_;
                selectedAct = selectedAct_;
                cancelledAct = cancelledAct_;
                
                listView = new ListView();
                listView.style.top = listView.style.left = listView.style.right = 2;
                listView.reorderable = false;
                listView.selectionType = SelectionType.Single;
                listView.selectionChanged += ListViewOnSelectionChanged;
                listView.selectedIndex = -1;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(width, 500);
            }

            public override void OnGUI(Rect rect)
            {
            }

            public override void OnOpen()
            {
                var container = editorWindow.rootVisualElement;

                searchField = new ToolbarSearchField();
                searchField.style.right = 2;
                searchField.RegisterValueChangedCallback(OnSearchFieldChanged);
                container.Add(searchField);
                searchField.schedule.Execute(searchField.Focus).ExecuteLater(50);
                
                listView.makeItem = MakeItem;
                listView.bindItem = BindItem;
                container.Add(listView);
                RefreshChoices();
            }
            
            public void SetItemHeight(float height)
            {
                listView.fixedItemHeight = height;
            }

            VisualElement MakeItem()
            {
                if (MakeItemOverride != null)
                {
                    return MakeItemOverride();
                }
                var label = new Label();
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.paddingLeft = 5;
                return label;
            }

            void BindItem(VisualElement element, int index)
            {
                var value = (TValueChoice)listView.itemsSource[index];
                if (BindItemOverride != null)
                {
                    BindItemOverride(element, value);
                }
                else
                {
                    ((Label)element).text = getStringFunc(value);
                }
            }

            public override void OnClose()
            {
                if (selectedAct != null)
                {
                    cancelledAct?.Invoke();
                    selectedAct = null;
                }
                base.OnClose();
            }

            void OnSearchFieldChanged(ChangeEvent<string> evt)
            {
                RefreshChoices(evt.newValue);
            }
            
            void RefreshChoices(string searchTerm = null)
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    listView.itemsSource = fullChoices;
                }
                else
                {
                    if (filteredChoices == null)
                    {
                        filteredChoices = new List<TValueChoice>();
                    }
                    else
                    {
                        filteredChoices.Clear();   
                    }
                    filteredChoices.AddRange(fullChoices
                        .Where(choice => getStringFunc(choice).IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0));

                    listView.itemsSource = filteredChoices;
                }
                listView.Rebuild();
            }

            void ListViewOnSelectionChanged(IEnumerable<object> obj)
            {
                if (listView.selectedIndex >= 0)
                {
                    var cb = selectedAct;
                    selectedAct = null;
                    cb?.Invoke((TValueChoice)listView.itemsSource[listView.selectedIndex]);
                }
                editorWindow.Close();
            }
        }
    }
}