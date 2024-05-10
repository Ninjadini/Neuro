using System;
using System.Collections.Generic;
using System.Linq;
using Ninjadini.Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroReferencablesDropdownField : SearchablePopupField<uint>
    {
        const int DefaultHeight = 22;
        
        public bool IncludeNullOption;

        readonly NeuroReferences references;
        Type type;
        IReadOnlyDictionary<uint, IReferencable> dictionary;
        Button gotoRefBtn;
        VisualElement selectedItemCustomOverlay;
        ICustomNeuroEditorProvider.BindRefItemDelegate  selectedItemOverlayBind;
        
        public NeuroReferencablesDropdownField(NeuroReferences references) : base()
        {
            this.references = references;
            BeforePopupShown += RefreshChoices;
        }

        public bool HasGoToRefBtn() => gotoRefBtn != null;

        public void AddGoToReferenceBtn(Action<Type, uint> callback)
        {
            if (gotoRefBtn != null)
            {
                gotoRefBtn.clickable = new Clickable(() => callback(type, value));
            }
            else
            {
                gotoRefBtn = new Button(() => callback(type, value))
                {
                    text = ">"
                };
                Add(gotoRefBtn);
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
            var refName = type != null ? references?.GetTable(type).GetRefName(id) : null;
            if (string.IsNullOrEmpty(refName))
            {
                return id.ToString();
            }
            return id + " : " + refName;
        }

        protected override void SetupWindow(SearchListPopupWindow window)
        {
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
            list.AddRange(references.GetTable(type).GetIds().OrderBy(x => x));
            choices = list;
        }
    }
}