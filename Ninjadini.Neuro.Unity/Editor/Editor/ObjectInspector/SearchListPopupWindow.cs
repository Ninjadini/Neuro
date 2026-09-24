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

        /// When set, a choice's text is split on it into a path: "Core / Audio / Events []" lists as
        /// "Events []" under an "Audio" header nested under "Core". A group's own items come before its
        /// subgroups; items keep the order given, groups are sorted by name.
        public string GroupSeparator;

        /// Names this field for the popup's 'Remember' tick, e.g. "NeuroEditorNav.Type". The tick and the
        /// search it keeps are per key and last the editor session. Leave null for no tick.
        public string RememberSearchKey;

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
            return getStringFunc != null ? getStringFunc(choice) : choice.ToString();
        }

        bool IsCurrent(TValueChoice choice)
        {
            return EqualityComparer<TValueChoice>.Default.Equals(choice, value);
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
                var window = new SearchListPopupWindow(Math.Max(320, (int)rect.width), choices, StrFunc, OnChoiceSelected, null, IsCurrent);
                window.GroupSeparator = GroupSeparator;
                window.RememberSearchKey = RememberSearchKey;
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
            List<Row> rows = new List<Row>();
            Func<TValueChoice, string> getStringFunc;
            Func<TValueChoice, bool> isCurrentFunc;
            Action<TValueChoice> selectedAct;
            Action cancelledAct;
            ToolbarSearchField searchField;
            ListView listView;
            Label footerLabel;
            Button footerButton;
            string searchTerm;

            public Func<VisualElement> MakeItemOverride;
            public Action<VisualElement, TValueChoice> BindItemOverride;

            /// See SearchablePopupField.GroupSeparator. Set before the window is shown.
            public string GroupSeparator;

            /// Optional one-line note under the list, e.g. "Showing 32 of 50". Set before the window is shown.
            public string FooterText;

            /// Optional button at the right of the footer, e.g. "Show all". Only shown when <see cref="FooterText"/>
            /// is set too. Set before the window is shown.
            public string FooterButtonText;

            /// Called when the footer button is clicked, with the window itself so the handler can change what the
            /// choices list holds and then call <see cref="SetFooterText"/> / <see cref="RefreshList"/> to show it.
            public Action<SearchListPopupWindow> FooterButtonClicked;

            /// When set, a 'Remember' tick sits beside the search field. Ticked, the search is kept under this
            /// key (in SessionState) and filled back in the next time a window with the same key opens; clearing
            /// the search forgets it, unticking forgets both. Set before the window is shown.
            public string RememberSearchKey;

            const string RememberStatePrefix = "Neuro.SearchPopup.Remember.";
            const string SearchStatePrefix = "Neuro.SearchPopup.Search.";

            const float GroupIndent = 14;
            const string CurrentMarker = "✔ ";
            const string NotCurrentMarker = "    ";

            /// One line of the list: either a group header or a selectable choice. Depth is the indent level -
            /// a top level header is 0, its items and subgroup headers are 1, and so on.
            public readonly struct Row
            {
                public readonly bool IsHeader;
                public readonly string Text;
                public readonly TValueChoice Choice;
                public readonly int Depth;

                public Row(bool isHeader, string text, TValueChoice choice, int depth)
                {
                    IsHeader = isHeader;
                    Text = text;
                    Choice = choice;
                    Depth = depth;
                }
            }

            class GroupNode
            {
                public readonly List<Row> Items = new List<Row>();
                public readonly SortedDictionary<string, GroupNode> Groups = new SortedDictionary<string, GroupNode>(StringComparer.OrdinalIgnoreCase);
            }

            public SearchListPopupWindow(int width_, 
                List<TValueChoice> choices, 
                Func<TValueChoice, string> getStringFunc_, 
                Action<TValueChoice> selectedAct_,
                Action cancelledAct_ = null,
                Func<TValueChoice, bool> isCurrentFunc_ = null)
            {
                width = width_;
                fullChoices = choices;
                getStringFunc = getStringFunc_;
                isCurrentFunc = isCurrentFunc_;
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
                string initialSearch = null;
                if (string.IsNullOrEmpty(RememberSearchKey))
                {
                    container.Add(searchField);
                }
                else
                {
                    var searchRow = new VisualElement();
                    searchRow.style.flexDirection = FlexDirection.Row;
                    searchRow.style.alignItems = Align.Center;
                    searchRow.style.flexShrink = 0;
                    searchField.style.flexGrow = 1;
                    searchField.style.flexShrink = 1;
                    searchField.style.width = StyleKeyword.Auto;
                    searchRow.Add(searchField);

                    var remember = SessionState.GetBool(RememberStatePrefix + RememberSearchKey, false);
                    var rememberToggle = new Toggle("Remember") { value = remember };
                    rememberToggle.tooltip = "Reopen this dropdown with the search last typed into it";
                    rememberToggle.style.flexShrink = 0;
                    rememberToggle.style.marginRight = 6;
                    rememberToggle.labelElement.style.minWidth = StyleKeyword.Auto;
                    rememberToggle.labelElement.style.paddingRight = 4;
                    rememberToggle.RegisterValueChangedCallback(OnRememberToggled);
                    searchRow.Add(rememberToggle);
                    container.Add(searchRow);

                    if (remember)
                    {
                        initialSearch = SessionState.GetString(SearchStatePrefix + RememberSearchKey, "");
                        searchField.SetValueWithoutNotify(initialSearch);
                    }
                }
                searchField.schedule.Execute(FocusSearchField).ExecuteLater(50);
                
                listView.makeItem = MakeItem;
                listView.bindItem = BindItem;
                listView.style.flexGrow = 1;
                listView.style.flexShrink = 1;
                container.Add(listView);
                if (!string.IsNullOrEmpty(FooterText))
                {
                    var footer = new VisualElement();
                    footer.style.flexDirection = FlexDirection.Row;
                    footer.style.alignItems = Align.Center;
                    footer.style.flexShrink = 0;
                    footer.style.paddingTop = 2;
                    footer.style.paddingBottom = 3;
                    footer.style.borderTopWidth = 1;
                    footer.style.borderTopColor = new Color(0f, 0f, 0f, 0.3f);

                    footerLabel = new Label(FooterText);
                    footerLabel.style.flexGrow = 1;
                    footerLabel.style.flexShrink = 1;
                    footerLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                    footerLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                    footerLabel.style.fontSize = 10;
                    footerLabel.style.paddingLeft = 5;
                    footerLabel.style.opacity = 0.7f;
                    footerLabel.style.whiteSpace = WhiteSpace.Normal;
                    footer.Add(footerLabel);

                    if (!string.IsNullOrEmpty(FooterButtonText) && FooterButtonClicked != null)
                    {
                        footerButton = new Button(OnFooterButtonClicked)
                        {
                            text = FooterButtonText
                        };
                        footerButton.style.flexShrink = 0;
                        footerButton.style.fontSize = 10;
                        footerButton.style.marginRight = 4;
                        footer.Add(footerButton);
                    }
                    container.Add(footer);
                }
                RefreshChoices(initialSearch);
            }

            void FocusSearchField()
            {
                searchField.Focus();
                // A remembered search comes back selected, so typing replaces it rather than appending.
                var textInput = searchField.Q<TextField>();
                if (textInput != null && !string.IsNullOrEmpty(textInput.value))
                {
                    textInput.SelectAll();
                }
            }

            void OnRememberToggled(ChangeEvent<bool> evt)
            {
                if (evt.newValue)
                {
                    SessionState.SetBool(RememberStatePrefix + RememberSearchKey, true);
                    SaveRememberedSearch(searchTerm);
                }
                else
                {
                    SessionState.EraseBool(RememberStatePrefix + RememberSearchKey);
                    SessionState.EraseString(SearchStatePrefix + RememberSearchKey);
                }
            }

            void SaveRememberedSearch(string term)
            {
                if (string.IsNullOrEmpty(RememberSearchKey) || !SessionState.GetBool(RememberStatePrefix + RememberSearchKey, false))
                {
                    return;
                }
                if (string.IsNullOrEmpty(term))
                {
                    SessionState.EraseString(SearchStatePrefix + RememberSearchKey);
                }
                else
                {
                    SessionState.SetString(SearchStatePrefix + RememberSearchKey, term);
                }
            }
            
            public void SetItemHeight(float height)
            {
                listView.fixedItemHeight = height;
            }

            /// Each list element carries both a header label and a choice element; binding shows one of them.
            VisualElement MakeItem()
            {
                var container = new VisualElement();
                container.style.flexGrow = 1;
                var header = new Label();
                header.style.unityTextAlign = TextAnchor.MiddleLeft;
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.style.paddingLeft = 5;
                header.style.flexGrow = 1;
                header.style.backgroundColor = new Color(0f, 0f, 0f, 0.2f);
                container.Add(header);
                VisualElement item;
                if (MakeItemOverride != null)
                {
                    item = MakeItemOverride();
                }
                else
                {
                    var label = new Label();
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    label.style.paddingLeft = 5;
                    item = label;
                }
                item.style.flexGrow = 1;
                container.Add(item);
                return container;
            }

            void BindItem(VisualElement element, int index)
            {
                var row = rows[index];
                var header = element[0];
                var item = element[1];
                header.style.display = row.IsHeader ? DisplayStyle.Flex : DisplayStyle.None;
                item.style.display = row.IsHeader ? DisplayStyle.None : DisplayStyle.Flex;
                if (row.IsHeader)
                {
                    ((Label)header).text = row.Text;
                    header.style.marginLeft = row.Depth * GroupIndent;
                    return;
                }
                item.style.marginLeft = row.Depth * GroupIndent;
                if (BindItemOverride != null)
                {
                    BindItemOverride(item, row.Choice);
                }
                else
                {
                    var marker = isCurrentFunc == null ? "" : (isCurrentFunc(row.Choice) ? CurrentMarker : NotCurrentMarker);
                    ((Label)item).text = marker + row.Text;
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

            void OnFooterButtonClicked()
            {
                var cb = FooterButtonClicked;
                if (cb == null)
                {
                    return;
                }
                // The handler is expected to change the choices list and then refresh what the footer says;
                // hide the button first so a one-shot action such as 'show all' can't be run twice.
                if (footerButton != null)
                {
                    footerButton.style.display = DisplayStyle.None;
                }
                cb(this);
            }

            /// Replaces the footer note, e.g. after the footer button changed what the list shows.
            public void SetFooterText(string text)
            {
                FooterText = text;
                if (footerLabel != null)
                {
                    footerLabel.text = text;
                }
            }

            /// Rebuilds the rows from the choices list, keeping the current search term.
            public void RefreshList()
            {
                RefreshChoices(searchTerm);
            }

            void OnSearchFieldChanged(ChangeEvent<string> evt)
            {
                RefreshChoices(evt.newValue);
                SaveRememberedSearch(evt.newValue);
            }
            
            void RefreshChoices(string searchTerm = null)
            {
                this.searchTerm = searchTerm;
                BuildRows(rows, fullChoices, getStringFunc, GroupSeparator, searchTerm);
                listView.itemsSource = rows;
                listView.Rebuild();
            }

            /// Filters the choices by the search term (matched against the whole text, path included) and,
            /// with a separator, arranges them as a tree of headers: "A > B > Item" goes under header B nested
            /// under header A. Empty groups never appear, so a search only shows the headers it needs.
            public static void BuildRows(List<Row> result, List<TValueChoice> choices, Func<TValueChoice, string> getString, string separator, string searchTerm)
            {
                result.Clear();
                var root = new GroupNode();
                var hasSeparator = !string.IsNullOrEmpty(separator);
                foreach (var choice in choices)
                {
                    var text = getString(choice);
                    if (!string.IsNullOrEmpty(searchTerm) && text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    var node = root;
                    var label = text;
                    if (hasSeparator)
                    {
                        var parts = text.Split(new[] { separator }, StringSplitOptions.None);
                        for (var i = 0; i < parts.Length - 1; i++)
                        {
                            var name = parts[i].Trim();
                            if (name.Length == 0)
                            {
                                continue;
                            }
                            if (!node.Groups.TryGetValue(name, out var child))
                            {
                                child = new GroupNode();
                                node.Groups.Add(name, child);
                            }
                            node = child;
                        }
                        label = parts[parts.Length - 1].Trim();
                    }
                    node.Items.Add(new Row(false, label, choice, 0));
                }
                AppendRows(result, root, 0);
            }

            static void AppendRows(List<Row> result, GroupNode node, int depth)
            {
                foreach (var item in node.Items)
                {
                    result.Add(new Row(false, item.Text, item.Choice, depth));
                }
                foreach (var kv in node.Groups)
                {
                    result.Add(new Row(true, kv.Key, default, depth));
                    AppendRows(result, kv.Value, depth + 1);
                }
            }

            void ListViewOnSelectionChanged(IEnumerable<object> obj)
            {
                var index = listView.selectedIndex;
                if (index >= 0 && rows[index].IsHeader)
                {
                    // Headers only label the rows under them.
                    listView.SetSelectionWithoutNotify(new int[0]);
                    return;
                }
                if (index >= 0)
                {
                    var cb = selectedAct;
                    selectedAct = null;
                    cb?.Invoke(rows[index].Choice);
                }
                editorWindow.Close();
            }
        }
    }
}