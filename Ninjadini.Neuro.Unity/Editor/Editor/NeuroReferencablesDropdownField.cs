using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroReferencablesDropdownField : SearchablePopupField<uint>
    {
        const int DefaultHeight = 22;
        const string FilterButtonPrefsPrefix = "NeuroRefDropdown.Filter.";
        const string SortPrefsPrefix = "NeuroRefDropdown.Sort.";
        const string SortById = "Id";
        const string SortByName = "Name";
        static readonly Color ActiveButtonColor = new Color(0.17f, 0.36f, 0.53f);
        
        public bool IncludeNullOption;

        /// <summary>
        /// Optional predicate over the candidate items. Only items it accepts are listed, plus the
        /// currently selected one, which is kept (and marked) so a stale value can still be seen and changed.
        /// </summary>
        public Func<IReferencable, bool> Filter;

        /// <summary>
        /// Short name of what installed <see cref="Filter"/>, shown in the dropdown's footer. Filled in by
        /// <see cref="SetFilterFrom"/> from the attribute names; set it yourself when assigning Filter directly.
        /// </summary>
        public string FilterName;

        /// Filter buttons and sorts from the field (and enclosing members); the referencable type's own are
        /// added in front of these whenever the dropdown lists that type. See <see cref="SetFilterButtonsAndSorts"/>.
        NeuroReferenceFilterButtonsAttribute[] fieldFilterButtons;
        NeuroReferenceSortAttribute[] fieldSorts;

        /// The filter button narrowing the list right now - null for All, which is also what a remembered
        /// button falls back to when it is not offered here or would leave nothing to pick.
        NeuroReferenceFilterButton? activeFilterButton;

        VisualElement popupHeader;
        VisualElement popupSortButtons;

        readonly NeuroReferences references;
        Type type;
        IReadOnlyDictionary<uint, IReferencable> dictionary;
        Action<Type, uint> gotoRefBtnCallback;
        VisualElement selectedItemCustomOverlay;
        ICustomNeuroEditorProvider.BindRefItemDelegate  selectedItemOverlayBind;
        
        /// Set while the popup's 'Show all' button has been pressed - lasts only as long as that popup is open,
        /// the filter is back on next time the dropdown is opened.
        bool showAllOverride;

        public NeuroReferencablesDropdownField(NeuroReferences references) : base()
        {
            this.references = references;
            BeforePopupShown += OnBeforePopupShown;
        }

        void OnBeforePopupShown()
        {
            showAllOverride = false;
            RefreshChoices();
        }

        public bool HasGoToRefBtn() => gotoRefBtnCallback != null;

        /// <summary>
        /// Installs <see cref="Filter"/> from the <see cref="NeuroReferenceFilterAttribute"/>s on a field or
        /// property, if it has any. Returns true if one was found. Does not look at enclosing members; the
        /// Neuro editor resolves those itself and calls <see cref="SetFilters"/>.
        /// </summary>
        public bool SetFilterFrom(MemberInfo memberInfo, Type refType = null)
        {
            SetFilterButtonsAndSorts(
                NeuroReferenceFilters.GetOwn<NeuroReferenceFilterButtonsAttribute>(memberInfo),
                NeuroReferenceFilters.GetOwn<NeuroReferenceSortAttribute>(memberInfo));
            return SetFilters(NeuroReferenceFilters.GetOwn(memberInfo), refType);
        }

        /// <summary>
        /// The field's own filter buttons and sorts, already resolved along enclosing members. Those whose
        /// AppliesTo rejects the listed type are skipped; the type's own class attributes are always added.
        /// </summary>
        public void SetFilterButtonsAndSorts(IEnumerable<NeuroReferenceFilterButtonsAttribute> filterButtons, IEnumerable<NeuroReferenceSortAttribute> sorts)
        {
            fieldFilterButtons = filterButtons?.ToArray();
            fieldSorts = sorts?.ToArray();
        }

        List<NeuroReferenceFilterButton> CollectFilterButtons()
        {
            var result = new List<NeuroReferenceFilterButton>();
            if (type == null)
            {
                return result;
            }
            var attributes = type.GetCustomAttributes<NeuroReferenceFilterButtonsAttribute>(true);
            if (fieldFilterButtons != null)
            {
                attributes = attributes.Concat(fieldFilterButtons.Where(b => b.AppliesTo(type)));
            }
            foreach (var attribute in attributes)
            {
                foreach (var button in attribute.GetButtons(references))
                {
                    if (!string.IsNullOrEmpty(button.Name) && button.Include != null && result.All(b => b.Name != button.Name))
                    {
                        result.Add(button);
                    }
                }
            }
            return result;
        }

        List<NeuroReferenceSortAttribute> CollectSorts()
        {
            var result = new List<NeuroReferenceSortAttribute>();
            if (type == null)
            {
                return result;
            }
            var attributes = type.GetCustomAttributes<NeuroReferenceSortAttribute>(true);
            if (fieldSorts != null)
            {
                attributes = attributes.Concat(fieldSorts.Where(s => s.AppliesTo(type)));
            }
            foreach (var attribute in attributes)
            {
                var name = attribute.Name;
                if (!string.IsNullOrEmpty(name) && name != SortById && name != SortByName && result.All(s => s.Name != name))
                {
                    result.Add(attribute);
                }
            }
            return result;
        }

        // Remembered per referencable type for the editor session, shared by every dropdown listing that type.
        string RememberedFilterButton
        {
            get => type == null ? "" : SessionState.GetString(FilterButtonPrefsPrefix + type.FullName, "");
            set => SessionState.SetString(FilterButtonPrefsPrefix + type.FullName, value ?? "");
        }

        string RememberedSort
        {
            get => type == null ? "" : SessionState.GetString(SortPrefsPrefix + type.FullName, "");
            set => SessionState.SetString(SortPrefsPrefix + type.FullName, value ?? "");
        }

        /// <summary>
        /// Installs <see cref="Filter"/> from already resolved attributes, keeping only those that apply to
        /// <paramref name="refType"/> (when given). Several must all accept an item. Returns true if any applied.
        /// </summary>
        public bool SetFilters(IEnumerable<NeuroReferenceFilterAttribute> filters, Type refType = null)
        {
            var applicable = NeuroReferenceFilters.Applicable(filters, refType);
            Filter = NeuroReferenceFilters.ToPredicate(applicable, references);
            FilterName = NeuroReferenceFilters.DisplayName(applicable);
            return Filter != null;
        }

        string BuildFooterText()
        {
            if (Filter == null || type == null)
            {
                return null;
            }
            var total = TotalCount();
            var by = string.IsNullOrEmpty(FilterName) ? "" : " by " + FilterName;
            if (showAllOverride)
            {
                return $"Ignoring{by} · {nameof(NeuroReferenceFilterAttribute)}";
            }
            var shown = choices.Count(id => id != 0);
            return $"Showing {shown} of {total}, filtered{by} · {nameof(NeuroReferenceFilterAttribute)}";
        }

        int TotalCount() => references?.GetTable(type)?.GetIds().Count() ?? 0;

        bool PassesFilter(uint id)
        {
            if (Filter == null || id == 0 || type == null)
            {
                return true;
            }
            var item = references?.GetTable(type)?.Get(id);
            return item == null || Filter(item);
        }

        bool PassesFilterButton(uint id)
        {
            if (activeFilterButton == null || id == 0 || type == null)
            {
                return true;
            }
            var item = references?.GetTable(type)?.Get(id);
            return item == null || activeFilterButton.Value.Include(item);
        }

        /// How many items the hard filter lets through, ignoring the current value it keeps regardless.
        int CountPassingFilter(Func<IReferencable, bool> extra = null)
        {
            var table = references?.GetTable(type);
            if (table == null)
            {
                return 0;
            }
            var filter = showAllOverride ? null : Filter;
            var count = 0;
            foreach (var id in table.GetIds().ToList())
            {
                var item = table.Get(id);
                if (item != null && (filter == null || filter(item)) && (extra == null || extra(item)))
                {
                    count++;
                }
            }
            return count;
        }

        public void AddGoToReferenceBtn(Action<Type, uint> callback)
        {
            if (callback == null)
            {
                return;
            }
            gotoRefBtnCallback = callback;
            var gotoRefBtn = new Button()
            {
                text = ">"
            };
            gotoRefBtn.tooltip = "Shift click to open in new window";
            gotoRefBtn.RegisterCallback<ClickEvent>(OnGotoBtnCallback);
            Add(gotoRefBtn);
        }

        void OnGotoBtnCallback(ClickEvent evt)
        {
            if (evt.modifiers == EventModifiers.Shift)
            {
                var window = NeuroEditorWindow.GetNewWindow();
                window.EditorElement.SetSelectedItem(type, value);
            }
            else
            {
                gotoRefBtnCallback(type, value);
            }
        }

        string FormatItemCallback(uint id)
        {
            if (id == 0)
            {
                if (IncludeNullOption)
                {
                    return "0 : null";
                }
                else
                {
                    return "";
                }
            }
            var idStr = NeuroEditorUtils.DisplayRefId(id);
            var refName = type != null ? references?.GetTable(type).GetRefName(id) : null;
            var result = string.IsNullOrEmpty(refName) ? idStr : idStr + " : " + refName;
            if (!PassesFilter(id))
            {
                return result + " (filtered out)";
            }
            return PassesFilterButton(id) ? result : result + $" (not in {activeFilterButton?.Name})";
        }

        protected override void SetupWindow(SearchListPopupWindow window)
        {
            window.FooterText = BuildFooterText();
            if (Filter != null && type != null && CountPassingFilter() < TotalCount())
            {
                window.FooterButtonText = "Ignore " + (string.IsNullOrEmpty(FilterName) ? "filter" : FilterName);
                window.FooterButtonClicked = ShowAllInPopup;
            }
            popupHeader = new VisualElement();
            window.HeaderElement = popupHeader;
            popupSortButtons = new VisualElement();
            window.SearchSideElement = popupSortButtons;
            PopulatePopupHeader(window);
            ICustomNeuroEditorProvider.MakeRefItemDelegate makeFunc = MakeItemOverride;
            ICustomNeuroEditorProvider.BindRefItemDelegate bindFunc = BindItemOverride;
            foreach (var customProvider in NeuroObjectInspector.CustomProviders)
            {
                var makeFuncCopy = makeFunc;
                var bindFuncCopy = bindFunc;
                var itemHeight = 0f;
                if(customProvider.GetReferenceDropdownDecoratorsFor(type, ref makeFuncCopy, ref bindFuncCopy, ref itemHeight, references))
                {
                    window.MakeItemOverride = makeFuncCopy.Invoke;
                    window.BindItemOverride = bindFuncCopy.Invoke;
                    if (itemHeight > 1f)
                    {
                        window.SetItemHeight(itemHeight);
                    }
                    break;
                }
            }
        }

        void ShowAllInPopup(SearchablePopupField<uint>.SearchListPopupWindow window)
        {
            showAllOverride = true;
            RefreshInPopup(window);
        }

        void RefreshInPopup(SearchListPopupWindow window)
        {
            RefreshChoices();
            window.SetFooterText(BuildFooterText());
            PopulatePopupHeader(window);
            window.RefreshList();
        }

        /// The sort toggles beside the search field and the filter button row under it (only when there are
        /// buttons), with the active one of each highlighted. Rebuilt after every click so the highlight and
        /// counts follow.
        void PopulatePopupHeader(SearchListPopupWindow window)
        {
            var header = popupHeader;
            if (header == null)
            {
                return;
            }
            header.Clear();
            header.style.paddingTop = 2;
            header.style.paddingBottom = 2;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0f, 0f, 0f, 0.3f);
            PopulateSortButtons(window);

            var activeName = activeFilterButton?.Name;
            var allCount = CountPassingFilter();
            var shownButtons = new List<(string name, int count)>();
            foreach (var button in CollectFilterButtons())
            {
                var count = CountPassingFilter(button.Include);
                // left out when it shows nothing, or the same as All, once this field's filter has had its say
                if (button.Name == activeName || (count > 0 && count < allCount))
                {
                    shownButtons.Add((button.Name, count));
                }
            }
            if (shownButtons.Count > 0)
            {
                var row = AddHeaderRow(header, "Filter");
                AddHeaderToggle(row, $"All ({allCount})", activeName == null, () =>
                {
                    RememberedFilterButton = "";
                    RefreshInPopup(window);
                });
                foreach (var (name, count) in shownButtons)
                {
                    AddHeaderToggle(row, $"{name} ({count})", activeName == name, () =>
                    {
                        RememberedFilterButton = name;
                        RefreshInPopup(window);
                    });
                }
            }

            header.style.display = header.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void PopulateSortButtons(SearchListPopupWindow window)
        {
            var sortRow = popupSortButtons;
            if (sortRow == null)
            {
                return;
            }
            sortRow.Clear();
            sortRow.style.flexDirection = FlexDirection.Row;
            sortRow.style.flexWrap = Wrap.Wrap;
            sortRow.style.alignItems = Align.Center;
            sortRow.style.paddingLeft = 4;
            sortRow.style.paddingRight = 2;
            var label = new Label("Sort");
            label.style.fontSize = 10;
            label.style.opacity = 0.7f;
            label.style.marginRight = 2;
            sortRow.Add(label);
            var sortName = ActiveSortName(CollectSorts());
            foreach (var name in new[] { SortById, SortByName }.Concat(CollectSorts().Select(s => s.Name)))
            {
                AddHeaderToggle(sortRow, name, sortName == name, () =>
                {
                    RememberedSort = name == SortById ? "" : name;
                    RefreshInPopup(window);
                });
            }
        }

        static VisualElement AddHeaderRow(VisualElement header, string title)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 4;
            row.style.paddingRight = 4;
            var label = new Label(title);
            label.style.width = 32;
            label.style.fontSize = 10;
            label.style.opacity = 0.7f;
            row.Add(label);
            header.Add(row);
            return row;
        }

        static Button AddHeaderToggle(VisualElement row, string text, bool active, Action clicked)
        {
            var button = new Button(clicked) { text = text };
            button.style.fontSize = 10;
            button.style.marginLeft = button.style.marginRight = 1;
            button.style.marginTop = button.style.marginBottom = 1;
            button.style.paddingLeft = button.style.paddingRight = 5;
            if (active)
            {
                button.style.backgroundColor = ActiveButtonColor;
                button.style.color = Color.white;
            }
            row.Add(button);
            return button;
        }

        string ActiveSortName(List<NeuroReferenceSortAttribute> sorts)
        {
            var remembered = RememberedSort;
            if (remembered == SortByName || sorts.Any(s => s.Name == remembered))
            {
                return remembered;
            }
            return SortById;
        }

        VisualElement MakeItemOverride()
        {
            var lbl = new Label();
            lbl.style.unityTextAlign = TextAnchor.MiddleLeft;
            lbl.style.paddingLeft = 5;
            return lbl;
        }

        void BindItemOverride(VisualElement element, uint id)
        {
            ((Label)element).text = FormatItemCallback(id);
        }

        public void SetValue(Type type_, IReferencable referencable, bool notifyChange = true)
        {
            SetValue(type_, referencable?.RefId ?? 0, notifyChange);
        }

        public void SetValue(Type type_, uint refIdValue, bool notifyChange = true)
        {
            if (type != type_)
            {
                type = type_;
                OnDrawingTypeChanged();
            }
            RefreshChoices();
            if (notifyChange)
            {
                SetValueWithoutNotify(0);
                value = refIdValue;
            }
            else
            {
                SetValueWithoutNotify(refIdValue);
            }
        }

        void OnDrawingTypeChanged()
        {
            if(selectedItemCustomOverlay != null)
            {
                selectedItemCustomOverlay.RemoveFromHierarchy();
            }
            selectedItemOverlayBind = null;
            
            var foundFormatFunc = false;
            ICustomNeuroEditorProvider.FormatValueDelegate formatFunc = FormatItemCallback;
            foreach (var customProvider in NeuroObjectInspector.CustomProviders)
            {
                var formatFuncCopy = formatFunc;
                VisualElement overlayElement = null;
                ICustomNeuroEditorProvider.BindRefItemDelegate  bindOverlayElement = null;
                var itemHeight = 0f;
                if(customProvider.GetReferenceValueDecoratorsFor(type, 
                       ref formatFuncCopy, 
                       ref overlayElement, 
                       ref bindOverlayElement,
                       ref itemHeight,
                       references))
                {
                    foundFormatFunc = true;
                    SetFormatFunc(formatFuncCopy.Invoke);
                    if (overlayElement != null)
                    {
                        selectedItemCustomOverlay = overlayElement;
                        selectedItemOverlayBind = bindOverlayElement;
                        textElement.parent.Add(overlayElement);
                        if(itemHeight > 5f)
                        {
                            style.height = itemHeight;
                        }
                        else
                        {
                            style.height = DefaultHeight;
                        }
                        formatSelectedValueCallback = (id) =>
                        {
                            if (selectedItemCustomOverlay != null)
                            {
                                selectedItemOverlayBind?.Invoke(selectedItemCustomOverlay, id);
                            }
                            return formatFuncCopy.Invoke(id);
                        };
                    }
                    break;
                }
                else
                {
                    style.height = DefaultHeight;
                }
            }
            if (!foundFormatFunc)
            {
                SetFormatFunc(FormatItemCallback);
            }
        }

        void RefreshChoices()
        {
            var list = choices;
            list.Clear();
            if (IncludeNullOption)
            {
                list.Add(0);
            }
            var table = references.GetTable(type);
            var filter = showAllOverride ? null : Filter;
            var current = value;
            // Snapshot first: Get() lazily loads items, which mutates the table while GetIds() enumerates it.
            IEnumerable<uint> ids = table.GetIds().ToList();
            if (filter != null)
            {
                ids = ids.Where(id => id == current || filter(table.Get(id))).ToList();
            }

            activeFilterButton = null;
            var rememberedButton = RememberedFilterButton;
            if (!string.IsNullOrEmpty(rememberedButton))
            {
                foreach (var button in CollectFilterButtons())
                {
                    if (button.Name != rememberedButton)
                    {
                        continue;
                    }
                    bool Includes(uint id)
                    {
                        var item = table.Get(id);
                        return item != null && button.Include(item);
                    }
                    // A button that would leave nothing to pick here falls back to All for this list only.
                    if (ids.Any(id => id != current && Includes(id)))
                    {
                        activeFilterButton = button;
                        ids = ids.Where(id => id == current || Includes(id)).ToList();
                    }
                    break;
                }
            }

            var sorts = CollectSorts();
            var sortName = ActiveSortName(sorts);
            if (sortName == SortByName)
            {
                ids = ids.OrderBy(id => table.GetRefName(id) ?? "", StringComparer.OrdinalIgnoreCase).ThenBy(id => id);
            }
            else if (sortName != SortById)
            {
                var sort = sorts.First(s => s.Name == sortName);
                var sorted = ids.Select(id => (id, item: table.Get(id))).ToList();
                sorted.Sort((a, b) =>
                {
                    if (a.item == null || b.item == null)
                    {
                        // anything that failed to load goes last
                        return a.item == b.item ? a.id.CompareTo(b.id) : (a.item == null ? 1 : -1);
                    }
                    var result = sort.Compare(a.item, b.item, references);
                    return result != 0 ? result : a.id.CompareTo(b.id);
                });
                ids = sorted.Select(pair => pair.id);
            }
            else
            {
                ids = ids.OrderBy(id => id);
            }
            list.AddRange(ids);
            choices = list;
        }
    }
}