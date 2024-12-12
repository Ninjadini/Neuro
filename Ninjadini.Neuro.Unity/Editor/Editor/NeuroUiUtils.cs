using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public static class NeuroUiUtils
    {
        // Intentionally not using extension methods here to avoid conflicts with other libs
        
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

        public static void RevealFileOrDirInFinder(string file)
        {
            if (File.Exists(file))
            {
                EditorUtility.RevealInFinder(file);
            }
            else
            {
                var dir = file;
                var attempts = 0;
                while (!string.IsNullOrEmpty(dir) && attempts < 10)
                {
                    attempts++;
                    if (Directory.Exists(dir))
                    {
                        EditorUtility.OpenWithDefaultApp(dir);
                        return;
                    }
                    else
                    {
                        dir = Path.GetDirectoryName(dir);
                    }
                }
                EditorUtility.OpenWithDefaultApp("./");
            }
        }
        
        public static void SetPlaceholderText(TextField textField, string placeholder)
        {
            var lbl = new Label(placeholder);
            lbl.style.position = Position.Absolute;
            lbl.style.top = lbl.style.bottom = lbl.style.left = 2f;
            lbl.style.unityTextAlign = TextAnchor.MiddleLeft;
            lbl.style.color = new Color(0.35f, 0.35f, 0.35f);
            textField.hierarchy.Add(lbl);

            textField.RegisterCallback<FocusInEvent>(evt => OnFocusIn());
            textField.RegisterCallback<FocusOutEvent>(evt => OnFocusOut());
            textField.RegisterValueChangedCallback(OnValueChange);
            OnFocusOut();
            return;

            void OnValueChange(ChangeEvent<string> evt)
            {
                OnFocusOut();
            }

            void OnFocusIn()
            {
                lbl.style.display = DisplayStyle.None;
            }

            void OnFocusOut()
            {
                lbl.style.display = string.IsNullOrEmpty(textField.text) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        public static void UpdatePlaceholderTextVisibility(TextField textField)
        {
            var lbl = textField.hierarchy.Children().FirstOrDefault(c => c is Label);
            if (lbl != null)
            {
                lbl.style.display = string.IsNullOrEmpty(textField.text) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public static void SetBorder(VisualElement element, Color color, float width = 1f, float radius = 3f, bool addPadding = true)
        {
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            if (addPadding)
            {
                element.style.paddingTop = 2f;
                element.style.paddingBottom = 2f;
                element.style.marginBottom = 5f;
                element.style.marginLeft = 3f;
                element.style.marginRight = 3f;
                element.style.marginTop = 5f;
            }
        }
    }
}