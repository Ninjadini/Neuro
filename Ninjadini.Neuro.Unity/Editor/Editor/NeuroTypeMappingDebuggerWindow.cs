using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroTypeMappingDebuggerWindow : EditorWindow
    {
        [MenuItem("Tools/Neuro/Type Mapping Debugger", priority = 104)]
        public static void ShowWindow()
        {
            GetWindow<NeuroTypeMappingDebuggerWindow>("Neuro Types").Show();
        }

        ViewMode viewMode;
        ScrollView typesScrollView;
        ViewMode[] viewModes;
        ToolbarToggle[] viewModeBtns;

        enum ViewMode
        {
            Everything,
            PolymorphicTypes,
            GlobleTypes
        }

        public void CreateGUI()
        {
            CreateTypesView();
        }

        void SetViewMode(ViewMode newViewMode)
        {
            viewMode = newViewMode;
            var index = Array.IndexOf(viewModes, newViewMode);
            for (var i = 0; i < viewModes.Length; i++)
            {
                var btn = viewModeBtns[i];
                btn.SetValueWithoutNotify(i == index);
            }
            RefreshTypesView();
        }

        void CreateTypesView()
        {
            var toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);

            viewModes = (ViewMode[])Enum.GetValues(typeof(ViewMode));
            viewModeBtns = new ToolbarToggle[viewModes.Length];
            
            for (var i = 0; i < viewModes.Length; i++)
            {
                var mode = viewModes[i];
                var toggle = new ToolbarToggle();
                toggle.label = mode.ToString();
                toggle.style.flexShrink = 1f;
                toggle.style.flexGrow = 0f;
                toolbar.Add(toggle);
                toggle.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
                {
                    SetViewMode(mode);
                });
                viewModeBtns[i] = toggle;
            }
            typesScrollView = new ScrollView();
            typesScrollView.style.flexGrow = 1;
            rootVisualElement.Add(typesScrollView);
            SetViewMode(ViewMode.Everything);
        }

        void RefreshTypesView()
        {
            typesScrollView.Clear();
            var allTypes = NeuroEditorUtils.FindAllNeuroTypesCached();
            var dict = CollectTypesByRootType(allTypes);
            SortLists(dict);
            var dictKeys = dict.Keys.ToList();
            dictKeys.Sort(Sorter);
            foreach (var type in dictKeys)
            {
                var list = dict[type];
                if (list.Count > 0)
                {
                    var foldOut = new Foldout()
                    {
                        text = $"<b>{type.FullName}</b>",
                    };
                    foreach (var subType in list)
                    {
                        if (!subType.IsInterface)
                        {
                            AddType(subType, foldOut);
                        }
                    }
                    typesScrollView.Add(foldOut);
                }
                else
                {
                    AddType(type, typesScrollView);
                }
            }
        }

        Dictionary<Type, List<Type>> CollectTypesByRootType(Type[] allTypes)
        {
            var dict = new Dictionary<Type, List<Type>>();
            foreach (var type in allTypes)
            {
                if (viewMode == ViewMode.PolymorphicTypes)
                {
                    var baseNeuroType = NeuroSyncTypes.FindRegisteredRootTypeUsingReflection(type);
                    if (baseNeuroType != null)
                    {
                        if (!dict.TryGetValue(baseNeuroType, out var list))
                        {
                            list = new List<Type>();
                            dict.Add(baseNeuroType, list);
                        }
                        list.Add(type);
                    }
                }
                else if (viewMode == ViewMode.GlobleTypes)
                {
                    var globalTag = type.GetCustomAttribute<NeuroGlobalTypeAttribute>()?.Id ?? 0u;
                    if (globalTag > 0)
                    {
                        dict.Add(type, new List<Type>());
                    }
                }
                else
                {
                    dict.Add(type, new List<Type>());
                }
            }
            return dict;
        }

        void SortLists(Dictionary<Type, List<Type>> dict)
        {
            foreach (var list in dict.Values)
            {
                list.Sort(Sorter);
            }
        }

        int Sorter(Type a, Type b)
        {
            switch (viewMode)
            {
                case ViewMode.Everything:
                    return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                case ViewMode.PolymorphicTypes:
                    var aTag = a.GetCustomAttribute<NeuroAttribute>()?.Tag ?? 0;
                    var bTag = b.GetCustomAttribute<NeuroAttribute>()?.Tag ?? 0;
                    return aTag.CompareTo(bTag);
                case ViewMode.GlobleTypes:
                    var aGlobal = a.GetCustomAttribute<NeuroGlobalTypeAttribute>()?.Id ?? 0;
                    var bGlobal = b.GetCustomAttribute<NeuroGlobalTypeAttribute>()?.Id ?? 0;
                    return aGlobal.CompareTo(bGlobal);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void AddType(Type type, VisualElement visualElement)
        {
            var globalTag = type.GetCustomAttribute<NeuroGlobalTypeAttribute>()?.Id ?? 0u;
            var neuroTag = type.GetCustomAttribute<NeuroAttribute>()?.Tag ?? 0u;
            
            var button = new Button();
            var typeName = type.Name;
            var tagStr = "";
            var globalStr = "";
            
            if (viewMode == ViewMode.PolymorphicTypes || viewMode == ViewMode.Everything)
            {
                tagStr = neuroTag > 0 ? $"{{tag {neuroTag}}} " : "";
            }
            if (viewMode == ViewMode.GlobleTypes || viewMode == ViewMode.Everything)
            {
                globalStr = globalTag > 0 ? $"{{global {globalTag}}} " : "";
            }
            
            button.text = $"{typeName} {tagStr}{globalStr}";
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            visualElement.Add(button);
        }
    }
    
}