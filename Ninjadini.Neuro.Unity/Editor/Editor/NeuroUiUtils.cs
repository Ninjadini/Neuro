using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Ninjadini.Toolkit
{
    public static class NeuroUiUtils
    {
        public static void SetDisplay(VisualElement element, bool visible)
        {
            if (element != null)
            {
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        public static bool IsDisplayed(VisualElement element)
        {
            return element?.style.display.value == DisplayStyle.Flex;
        }
        
        public static bool IsFocused(VisualElement element)
        {
            return element != null && element.panel?.focusController?.focusedElement == element;
        }
        
        public static VisualElement AddHorizontal(VisualElement parent)
        {
            var horizontal = new VisualElement();
            horizontal.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            parent?.Add(horizontal);
            return horizontal;
        }
        
        public static Label AddLabel(VisualElement parent, string text)
        {
            var label = new Label();
            label.text = text;
            parent?.Add(label);
            return label;
        }
        
        public static Button AddButton(VisualElement parent, string name, Action callback)
        {
            var btn = new Button();
            btn.text = name;
            if (callback != null)
            {
                btn.clicked += callback;
            }
            parent?.Add(btn);
            return btn;
        }
        
        public static Toggle AddToggle(VisualElement parent, string name, bool value, EventCallback<ChangeEvent<bool>> callback = null)
        {
            var toggle = new Toggle();
            toggle.text = name;
            toggle.value = value;
            if (callback != null)
            {
                toggle.RegisterValueChangedCallback(callback);
            }
            parent?.Add(toggle);
            return toggle;
        }

        public static bool OpenScript(Type typeToSearch, bool showDialogOnFail = true)
        {
            var query = "t:Script " + typeToSearch.Name;
            var guids = AssetDatabase.FindAssets(query);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (scriptAsset.GetClass() == typeToSearch)
                {
                    AssetDatabase.OpenAsset(scriptAsset);
                    return true;
                }
            }
            EditorUtility.DisplayDialog("", "Couldn't find MonoScript file for " + typeToSearch.Name+ "\nThis can happen if the name of the file does not match the name or there are multiple classes in the cs file.", "OK");
            return false;
        }
    }
}