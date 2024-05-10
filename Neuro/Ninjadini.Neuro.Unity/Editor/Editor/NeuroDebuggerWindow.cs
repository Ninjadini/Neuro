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
    public class NeuroDebuggerWindow : EditorWindow
    {
        [MenuItem("Tools/Neuro/Debugger", priority = 100)]
        public static void ShowWindow()
        {
            GetWindow<NeuroDebuggerWindow>("Neuro Debugger").Show();
        }

        bool fullName;
        bool groupSubTypes = true;
        SortType sortType;
        ScrollView typesScrollView;

        enum SortType
        {
            Name,
            Tag,
            GlobalId
        }

        public void CreateGUI()
        {
            CreateTypesView();
        }

        void CreateTypesView()
        {
            var toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);

            var toggle = new ToolbarToggle();
            toggle.label = "Full Name";
            toggle.style.width = 100;
            toggle.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                fullName = evt.newValue;
                RefreshTypesView();
            });
            toolbar.Add(toggle);
            toggle = new ToolbarToggle();
            toggle.label = "Group Subs";
            toggle.style.width = 100;
            toggle.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                groupSubTypes = evt.newValue;
                RefreshTypesView();
            });
            toggle.SetValueWithoutNotify(groupSubTypes);
            toolbar.Add(toggle);

            var spacer = new ToolbarSpacer();
            spacer.style.width = 20;
            toolbar.Add(spacer);
            
            var byName = new ToolbarToggle();
            byName.label = "By Name";
            byName.style.width = 80;
            toolbar.Add(byName);
            
            var byTag = new ToolbarToggle();
            byTag.label = "By Tag";
            byTag.style.width = 80;
            toolbar.Add(byTag);
            
            var byGlobalId = new ToolbarToggle();
            byGlobalId.label = "By Global Id";
            byGlobalId.style.width = 80;
            toolbar.Add(byGlobalId);
            
            Action<SortType> setSortType = delegate(SortType type)
            {
                sortType = type;
                byName.SetValueWithoutNotify(sortType == SortType.Name);
                byTag.SetValueWithoutNotify(sortType == SortType.Tag);
                byGlobalId.SetValueWithoutNotify(sortType == SortType.GlobalId);
                RefreshTypesView();
            };

            byName.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                setSortType(SortType.Name);
            });
            
            byTag.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                setSortType(SortType.Tag);
            });
            
            byGlobalId.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                setSortType(SortType.GlobalId);
            });
            typesScrollView = new ScrollView();
            typesScrollView.style.flexGrow = 1;
            rootVisualElement.Add(typesScrollView);
            setSortType(SortType.Name);
        }

        void RefreshTypesView()
        {
            typesScrollView.Clear();
            var allTypes = FindAllTypes();
            var dict = CollectTypesByRootType(allTypes);
            SortLists(dict);
            var dictKeys = dict.Keys.ToList();
            dictKeys.Sort(Sorter);
            foreach (var type in dictKeys)
            {
                var list = dict[type];
                if (list.Count > 0)
                {
                    var globalTag = type.GetCustomAttribute<NeuroGlobalTypeAttribute>()?.Id ?? 0u;
                    var foldOut = new Foldout()
                    {
                        text = $"<b>{type.FullName}</b> {(globalTag > 0 ? $"[global({globalTag})]" : "")}",
                    };
                    if (!type.IsInterface)
                    {
                        AddType(type, foldOut);
                    }
                    foreach (var subType in list)
                    {
                        AddType(subType, foldOut);
                    }
                    typesScrollView.Add(foldOut);
                }
                else
                {
                    AddType(type, typesScrollView);
                }
            }
        }

        Type[] FindAllTypes()
        {
            return (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                    where !domainAssembly.IsDynamic && domainAssembly.IsDefined(typeof(NeuroAssemblyAttribute))
                    where NeuroSyncTypes.TryRegisterAssembly(domainAssembly)
                    from type in domainAssembly.GetExportedTypes()
                    where type.IsClass && !type.IsGenericType
                                       && NeuroSyncTypes.CheckIfTypeRegisteredUsingReflection(type)
                    select type)
                .ToArray();
        }

        Dictionary<Type, List<Type>> CollectTypesByRootType(Type[] allTypes)
        {
            var dict = new Dictionary<Type, List<Type>>();
            foreach (var type in allTypes)
            {
                if (!groupSubTypes)
                {
                    dict.Add(type, new List<Type>());
                    continue;
                }
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
            switch (sortType)
            {
                case SortType.Name:
                    if (fullName)
                    {
                        return string.Compare(a.FullName, b.FullName, StringComparison.Ordinal);
                    }
                    return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                case SortType.Tag:
                    var aTag = a.GetCustomAttribute<NeuroAttribute>()?.Tag ?? uint.MaxValue;
                    var bTag = b.GetCustomAttribute<NeuroAttribute>()?.Tag ?? uint.MaxValue;
                    return aTag.CompareTo(bTag);
                case SortType.GlobalId:
                    var aGlobal = a.GetCustomAttribute<NeuroGlobalTypeAttribute>()?.Id ?? uint.MaxValue;
                    var bGlobal = b.GetCustomAttribute<NeuroGlobalTypeAttribute>()?.Id ?? uint.MaxValue;
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
            var typeName = fullName ? type.FullName : type.Name;
            var tagStr = neuroTag > 0 ? $"[tag {neuroTag}] " : "";
            var globalStr = globalTag > 0 ? $"{{global {globalTag}}}" : "";
            button.text = $"{typeName} \t{tagStr}{globalStr}";
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            visualElement.Add(button);
        }
    }
    
}