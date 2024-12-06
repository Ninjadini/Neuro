using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    class NeuroRefDropDownCustomizableEditorDrawer : ICustomNeuroEditorProvider
    {
        bool ICustomNeuroEditorProvider.GetReferenceDropdownDecoratorsFor(Type type,
            ref ICustomNeuroEditorProvider.MakeRefItemDelegate makeItem,
            ref ICustomNeuroEditorProvider.BindRefItemDelegate bindItem, 
            ref float itemHeight,
            NeuroReferences references)
        {
            if (!typeof(INeuroRefDropDownCustomizable).IsAssignableFrom(type))
            {
                return false;
            }
            
            var defaultMake = makeItem;
            var defaultBind = bindItem;
            var hasIcon = typeof(INeuroRefDropDownIconCustomizable).IsAssignableFrom(type);
            if (hasIcon)
            {
                itemHeight = 24;
            }
            makeItem = () =>
            {
                var item = defaultMake();
                if (hasIcon)
                {
                    var img = new VisualElement
                    {
                        name = "image",
                        style =
                        {
                            position = Position.Absolute,
                            right = 16,
                            width = 24,
                            height = 24,
                            display = DisplayStyle.None
                        }
                    };
                    item.Add(img);
                }
                return item;
            };
            bindItem = (element, id) =>
            {
                var item = references.GetTable(type).Get(id) as INeuroRefDropDownCustomizable;
                if (item == null)
                {
                    defaultBind(element, id);
                    return;
                }
                var txt = item.GetRefDropdownText(references);
                if (txt == null)
                {
                    defaultBind(element, id);
                }
                else
                {
                    ((Label)element).text = txt;
                }
                var img = element.Q<VisualElement>("image");
                if (img != null)
                {
                    UpdateImage(img, item);
                }
            };
            return true;
        }

        bool ICustomNeuroEditorProvider.GetReferenceValueDecoratorsFor(Type type,
            ref ICustomNeuroEditorProvider.FormatValueDelegate formatValue,
            ref VisualElement overlayElement,
            ref ICustomNeuroEditorProvider.BindRefItemDelegate bindOverlayElement,
            ref float itemHeight,
            NeuroReferences references)
        {
            if (!typeof(INeuroRefDropDownCustomizable).IsAssignableFrom(type))
            {
                return false;
            }
            var defaultFormatter = formatValue;
            formatValue = (id) =>
            {
                if (references.GetTable(type).Get(id) is INeuroRefDropDownCustomizable item)
                {
                    var txt = item.GetRefDropdownText(references);
                    if(txt != null)
                    {
                        return txt;
                    }
                }
                return defaultFormatter(id);
            };
            
            if (typeof(INeuroRefDropDownIconCustomizable).IsAssignableFrom(type))
            {
                itemHeight = 26;
            }
            var img = new VisualElement();
            img.style.position = Position.Absolute;
            img.style.right = 16;
            img.style.width = img.style.height = 24;
            overlayElement = img;

            bindOverlayElement = (element, id) =>
            {
                var item = references.GetTable(type).Get(id) as INeuroRefDropDownCustomizable;
                UpdateImage(element, item);
            };
            return true;
        }

        static void UpdateImage(VisualElement element, INeuroRefDropDownCustomizable item)
        {
            Texture2D icon = null;
            if (item is INeuroRefDropDownIconCustomizable iconCustomizable)
            {
                var iconAddress = iconCustomizable.RefDropdownIcon;
                if (iconAddress.HasAddress())
                {
                    var task = iconAddress.LoadAssetAsync<Texture2D>();
                    task.WaitForCompletion();
                    icon = task.Result;
                }
            }
            element.style.display = icon ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.backgroundImage = icon ? new StyleBackground(icon) : new StyleBackground();
        }
    }
}