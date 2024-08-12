using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    public static class NeuroUnityEditorMenus
    {
        [MenuItem("Tools/Neuro/ReadMe @ github url", priority = 102)]
        public static void ReadMe()
        {
            Application.OpenURL("https://github.com/Ninjadini/Neuro/");
        }
        
        
        [MenuItem("Tools/Neuro/Save To Resources")]
        public static void SaveToResources()
        {
            NeuroEditorDataProvider.Shared.SaveBundledBinaryToResources(null);
        }
        
        
        [MenuItem("Tools/Neuro/Save Resources data as JSON")]
        public static void SaveResourceDataAsJson()
        {
            NeuroEditorDataProvider.Shared.SaveBakedDataAsJson();
        }
        
        [MenuItem("Tools/Neuro/Reload")]
        public static void Reload()
        {
            NeuroEditorDataProvider.Shared.Reload();
        }
        
        [MenuItem("Tools/Neuro/Reload + Read all data")]
        public static void ReloadAndReadAll()
        {
            var dataProvider = NeuroEditorDataProvider.Shared;
            dataProvider.Reload();
            var startTime = DateTime.UtcNow;
            var count = 0;
            foreach (var type in dataProvider.References.GetRegisteredBaseTypes())
            {
                count += dataProvider.References.GetTable(type).SelectAll().Count();
            }
            Debug.Log($"Neuro ~ Read all {count:N0} data files in {(DateTime.UtcNow - startTime).TotalMilliseconds:N0} ms");
        }
    }
}