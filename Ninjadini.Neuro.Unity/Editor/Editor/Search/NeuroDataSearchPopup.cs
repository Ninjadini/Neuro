using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    /// The "⌕ Search" popup of the Neuro Editor: type, and every field whose name or value contains the text
    /// lists underneath, one line per field - `Rounds[2].Count = 12` - prefixed by the item it is in unless
    /// the search is limited to the item being edited. Clicking a line goes to that item. The two scope
    /// toggles pick the code side (field names) or the data side (values) or both; the three settings are
    /// remembered per user.
    ///
    /// The whole database is searched on the main thread, on each keystroke after a short pause. The first
    /// search over everything reads every file that has not been opened yet, so it is the slow one; the
    /// walk itself is <see cref="NeuroDataSearch"/> and is quick. Results stop at <see cref="MaxResults"/>.
    public class NeuroDataSearchPopup : PopupWindowContent
    {
        public const int MaxResults = 500;

        const string PrefFieldNames = "Ninjadini.Neuro.Search.FieldNames";
        const string PrefValues = "Ninjadini.Neuro.Search.Values";
        const string PrefThisItemOnly = "Ninjadini.Neuro.Search.ThisItemOnly";
        const long DebounceMs = 120;

        readonly NeuroEditorDataProvider dataProvider;
        readonly Func<NeuroDataFile> currentItem;
        readonly Action<NeuroDataSearch.Match> picked;
        readonly int width;
        readonly NeuroDataSearch search;
        readonly List<NeuroDataSearch.Match> results = new List<NeuroDataSearch.Match>();

        ToolbarSearchField searchField;
        Toggle namesToggle;
        Toggle valuesToggle;
        Toggle thisItemToggle;
        ListView listView;
        Label footerLabel;
        IVisualElementScheduledItem pendingSearch;
        List<IReferencable> allItems;
        int itemsWithMatches;

        public NeuroDataSearchPopup(NeuroEditorDataProvider dataProvider, Func<NeuroDataFile> currentItem, Action<NeuroDataSearch.Match> picked, int width = 520)
        {
            this.dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            this.currentItem = currentItem;
            this.picked = picked;
            this.width = width;
            search = new NeuroDataSearch(dataProvider.References);
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
            searchField.style.marginLeft = 2;
            searchField.style.marginRight = 2;
            searchField.style.marginTop = 2;
            searchField.style.width = new StyleLength(StyleKeyword.Auto);
            searchField.RegisterValueChangedCallback(_ => ScheduleSearch());
            searchField.RegisterCallback<KeyDownEvent>(OnSearchFieldKeyDown, TrickleDown.TrickleDown);
            container.Add(searchField);
            searchField.schedule.Execute(searchField.Focus).ExecuteLater(50);

            var options = NeuroUiUtils.AddHorizontal(container);
            options.style.flexShrink = 0;
            options.style.paddingLeft = 4;
            options.style.paddingTop = 2;
            options.style.paddingBottom = 2;
            namesToggle = AddOption(options, "Field names", PrefFieldNames, true, "Match the field's name - the code side.");
            valuesToggle = AddOption(options, "Values", PrefValues, true, "Match what the field holds - the data side.");
            options.Add(new VisualElement { style = { flexGrow = 1 } });
            thisItemToggle = AddOption(options, "This item only", PrefThisItemOnly, false, "Search only the item being edited, instead of every file.");
            thisItemToggle.SetEnabled(currentItem?.Invoke() != null);

            listView = new ListView();
            listView.style.flexGrow = 1;
            listView.style.flexShrink = 1;
            listView.style.marginLeft = 2;
            listView.style.marginRight = 2;
            listView.reorderable = false;
            listView.selectionType = SelectionType.Single;
            listView.makeItem = MakeRow;
            listView.bindItem = BindRow;
            listView.itemsSource = results;
            listView.selectionChanged += _ => PickSelected();
            container.Add(listView);

            footerLabel = new Label();
            footerLabel.style.flexShrink = 0;
            footerLabel.style.paddingLeft = 5;
            footerLabel.style.paddingTop = 2;
            footerLabel.style.paddingBottom = 3;
            footerLabel.style.fontSize = 10;
            footerLabel.style.opacity = 0.7f;
            footerLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            footerLabel.style.borderTopWidth = 1;
            footerLabel.style.borderTopColor = new Color(0f, 0f, 0f, 0.3f);
            container.Add(footerLabel);

            RunSearch();
        }

        Toggle AddOption(VisualElement parent, string text, string prefKey, bool defaultValue, string tooltip)
        {
            var toggle = NeuroUiUtils.AddToggle(parent, text, EditorPrefs.GetBool(prefKey, defaultValue), evt =>
            {
                EditorPrefs.SetBool(prefKey, evt.newValue);
                RunSearch();
            });
            toggle.tooltip = tooltip;
            toggle.style.marginRight = 10;
            return toggle;
        }

        void OnSearchFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (results.Count > 0)
                {
                    Pick(results[0]);
                }
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.DownArrow && results.Count > 0)
            {
                listView.Focus();
                listView.SetSelectionWithoutNotify(new[] { 0 });
                listView.ScrollToItem(0);
                evt.StopPropagation();
            }
        }

        void ScheduleSearch()
        {
            pendingSearch?.Pause();
            pendingSearch = searchField.schedule.Execute(RunSearch).StartingIn(DebounceMs);
        }

        NeuroDataSearch.Scope CurrentScope()
        {
            var scope = (NeuroDataSearch.Scope)0;
            if (namesToggle.value) scope |= NeuroDataSearch.Scope.FieldNames;
            if (valuesToggle.value) scope |= NeuroDataSearch.Scope.Values;
            return scope;
        }

        bool ThisItemOnly => thisItemToggle.value && thisItemToggle.enabledSelf;

        void RunSearch()
        {
            pendingSearch?.Pause();
            pendingSearch = null;
            results.Clear();
            itemsWithMatches = 0;
            var term = searchField.value;
            var scope = CurrentScope();
            if (!string.IsNullOrEmpty(term) && scope != 0)
            {
                if (ThisItemOnly)
                {
                    var item = currentItem?.Invoke()?.Value;
                    if (item != null && search.Search(item, term, scope, results, MaxResults) > 0)
                    {
                        itemsWithMatches = 1;
                    }
                }
                else
                {
                    foreach (var item in AllItems())
                    {
                        if (results.Count >= MaxResults)
                        {
                            break;
                        }
                        if (search.Search(item, term, scope, results, MaxResults) > 0)
                        {
                            itemsWithMatches++;
                        }
                    }
                }
            }
            listView.SetSelectionWithoutNotify(Array.Empty<int>());
            listView.Rebuild();
            footerLabel.text = FooterText(term, scope);
        }

        string FooterText(string term, NeuroDataSearch.Scope scope)
        {
            if (scope == 0)
            {
                return "Tick Field names or Values to search something.";
            }
            if (string.IsNullOrEmpty(term))
            {
                var what = scope == NeuroDataSearch.Scope.Both ? "field names and values" : scope == NeuroDataSearch.Scope.FieldNames ? "field names" : "values";
                var where = ThisItemOnly ? "in this item" : "across every item";
                return $"Type to search {what} {where}.";
            }
            if (results.Count == 0)
            {
                return "No matches.";
            }
            var matches = results.Count == 1 ? "1 match" : $"{results.Count} matches";
            if (results.Count >= MaxResults)
            {
                return $"Showing the first {MaxResults} matches - type more to narrow it down.";
            }
            if (ThisItemOnly)
            {
                return matches;
            }
            return $"{matches} in {(itemsWithMatches == 1 ? "1 item" : $"{itemsWithMatches} items")}";
        }

        /// Every item in the database, loaded. Taken once per popup; a file that fails to read is skipped,
        /// the way the editor's own reload panel would report it anyway.
        List<IReferencable> AllItems()
        {
            if (allItems != null)
            {
                return allItems;
            }
            allItems = new List<IReferencable>();
            // ToArray because reading a file registers the item, which writes to the very list being walked.
            foreach (var dataFile in new List<NeuroDataFile>(dataProvider.DataFiles))
            {
                IReferencable value;
                try
                {
                    value = dataFile.Value;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Neuro ~ search is skipping {dataFile.FilePath}: {e.Message}");
                    continue;
                }
                if (value != null)
                {
                    allItems.Add(value);
                }
            }
            return allItems;
        }

        VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexGrow = 1;
            row.style.paddingLeft = 5;
            row.style.paddingRight = 5;

            var item = new Label();
            item.style.opacity = 0.6f;
            item.style.flexShrink = 0;
            item.style.marginRight = 8;
            item.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Add(item);

            var path = new Label();
            path.style.flexShrink = 0;
            path.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Add(path);

            var value = new Label();
            value.style.flexShrink = 1;
            value.style.flexGrow = 1;
            value.style.overflow = Overflow.Hidden;
            value.style.textOverflow = TextOverflow.Ellipsis;
            value.style.whiteSpace = WhiteSpace.NoWrap;
            value.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Add(value);
            return row;
        }

        void BindRow(VisualElement row, int index)
        {
            var match = results[index];
            var item = (Label)row[0];
            var path = (Label)row[1];
            var value = (Label)row[2];

            var showItem = !ThisItemOnly;
            item.style.display = showItem ? DisplayStyle.Flex : DisplayStyle.None;
            if (showItem)
            {
                item.text = NeuroEditorHistory.GetDropDownName(match.Item, dataProvider.References);
            }
            path.text = match.Path;
            path.style.unityFontStyleAndWeight = match.NameMatched ? FontStyle.Bold : FontStyle.Normal;
            value.text = " = " + OneLine(match.Value);
            value.style.unityFontStyleAndWeight = match.ValueMatched ? FontStyle.Bold : FontStyle.Normal;
            row.tooltip = $"{match.Path}\n{match.Value}";
        }

        static string OneLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "\"\"";
            }
            if (text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0)
            {
                text = text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
            }
            return text;
        }

        void PickSelected()
        {
            var index = listView.selectedIndex;
            if (index >= 0 && index < results.Count)
            {
                Pick(results[index]);
            }
        }

        void Pick(NeuroDataSearch.Match match)
        {
            var cb = picked;
            editorWindow.Close();
            cb?.Invoke(match);
        }
    }
}
