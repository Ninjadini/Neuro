using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroReferencablesDropdownField : SearchablePopupField<uint>
    {
        const int DefaultHeight = 22;
        
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

        readonly NeuroReferences references;
        Type type;
        IReadOnlyDictionary<uint, IReferencable> dictionary;
        Action<Type, uint> gotoRefBtnCallback;
        VisualElement selectedItemCustomOverlay;
        ICustomNeuroEditorProvider.BindRefItemDelegate  selectedItemOverlayBind;
        
        public NeuroReferencablesDropdownField(NeuroReferences references) : base()
        {
            this.references = references;
            BeforePopupShown += RefreshChoices;
        }

        public bool HasGoToRefBtn() => gotoRefBtnCallback != null;

        /// <summary>
        /// Installs <see cref="Filter"/> from the <see cref="NeuroReferenceFilterAttribute"/>s on a field or
        /// property, if it has any. Returns true if one was found. Does not look at enclosing members; the
        /// Neuro editor resolves those itself and calls <see cref="SetFilters"/>.
        /// </summary>
        public bool SetFilterFrom(MemberInfo memberInfo, Type refType = null)
        {
            return SetFilters(NeuroReferenceFilters.GetOwn(memberInfo), refType);
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
            var total = references?.GetTable(type)?.GetIds().Count() ?? 0;
            var shown = choices.Count(id => id != 0);
            var by = string.IsNullOrEmpty(FilterName) ? "" : " by " + FilterName;
            return $"Showing {shown} of {total}, filtered{by} · {nameof(NeuroReferenceFilterAttribute)}";
        }

        bool PassesFilter(uint id)
        {
            if (Filter == null || id == 0 || type == null)
            {
                return true;
            }
            var item = references?.GetTable(type)?.Get(id);
            return item == null || Filter(item);
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
            return PassesFilter(id) ? result : result + " (filtered out)";
        }

        protected override void SetupWindow(SearchListPopupWindow window)
        {
            window.FooterText = BuildFooterText();
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
            var filter = Filter;
            if (filter == null)
            {
                list.AddRange(table.GetIds().OrderBy(x => x));
            }
            else
            {
                var current = value;
                // Snapshot first: Get() lazily loads items, which mutates the table while GetIds() enumerates it.
                var ids = table.GetIds().ToList();
                list.AddRange(ids
                    .Where(id => id == current || filter(table.Get(id)))
                    .OrderBy(x => x));
            }
            choices = list;
        }
    }
}