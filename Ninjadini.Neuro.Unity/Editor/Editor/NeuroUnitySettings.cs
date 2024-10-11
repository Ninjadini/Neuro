using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Ninjadini.Toolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    [FilePath("ProjectSettings/NeuroSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class NeuroUnityEditorSettings : ScriptableSingleton<NeuroUnityEditorSettings>
    {
        public const string SETTINGS_MENU_PATH = "Project/Ninjadini: ❖ Neuro";
        public const string DEFAULT_DATA_PATH = "NeuroData";

        public bool ShowDialogOnDataFileChange;
        public bool BakeAutoTypeRegistryForBuild = true;
        public bool BakeDataResourcesForBuild = true;
        public string ResourcesDir = "Assets/Resources/";
        public string PrimaryDataPath;
        
        [Tooltip("Debug.Log() neuro loading timings in case you need to know how long things are taking.")]
        public bool LogTimings;
        
        [Header("Experimental")]
        public bool UndoRedosEnabled;

        public List<NeuroEditorTypeItemSetting> ClassSettings = new List<NeuroEditorTypeItemSetting>();
        
        public NeuroUnityEditorSettings() : base()
        {
            PrimaryDataPath = DEFAULT_DATA_PATH;
        }

        public static NeuroUnityEditorSettings Get()
        {
            return instance;
        }

        public NeuroEditorTypeItemSetting FindTypeSetting(Type type)
        {
            var rootType = NeuroReferences.GetRootReferencable(type);
            return ClassSettings?.FirstOrDefault(s => s.Type.GetNeuroType() == rootType);
        }
        
        public static string GetTypeDropDownName(Type type)
        {
            var typeSetting = Get().FindTypeSetting(type);
            if (!string.IsNullOrEmpty(typeSetting?.DropDownName))
            {
                return typeSetting.DropDownName;
            }
            var displayNameAttribute = type.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
            if(!string.IsNullOrEmpty(displayNameAttribute?.DisplayName))
            {
                return displayNameAttribute.DisplayName;
            }
            if (typeof(ISingletonReferencable).IsAssignableFrom(type))
            {
                return type.Name;
            }
            return type.Name +" []";
        }

        public void Save()
        {
            for (var index = ClassSettings.Count - 1; index >= 0; index--)
            {
                if (ClassSettings[index]?.IsDefaultValues() ?? true)
                {
                    ClassSettings.RemoveAt(index);
                }
            }
            Save(true);
        }

/*
        public static bool LiveContentValidationTestsEnabled
        {
            get => EditorPrefs.GetBool("neuro_LiveContentValidationTestsEnabled", true);
            set => EditorPrefs.SetBool("neuro_LiveContentValidationTestsEnabled", value);
        }
        */
        
        class SettingsUIProvider : SettingsProvider
        {
            SettingsUIProvider() : base(NeuroUnityEditorSettings.SETTINGS_MENU_PATH, SettingsScope.Project) { }
                       
            public override void OnActivate(string searchContext, VisualElement rootElement)
            {
                var settings = NeuroUnityEditorSettings.Get();
                settings.hideFlags = HideFlags.None;
                
                var title = NeuroUiUtils.AddLabel(rootElement, "❖ Neuro");
                title.style.fontSize = 19;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.paddingLeft = 10;
                title.style.paddingTop = 1;
                title.style.paddingBottom = 5;
                
                var serializedObject = new SerializedObject(settings);
                //rootElement.Add(new InspectorElement(serializedObject));
                            
                var serializedProperty = serializedObject.GetIterator();
                if (serializedProperty.NextVisible(true))
                {
                    while (serializedProperty.NextVisible(false))
                    {
                        var field = new PropertyField();
                        field.style.paddingLeft = 8;
                        field.BindProperty(serializedProperty);
                        rootElement.Add(field);
                    }
                }
            }

            public override void OnInspectorUpdate()
            {
                OnSaveClicked();
            }

            void OnSaveClicked()
            {
                Get().Save();
            }
                            
            [SettingsProvider]
            public static SettingsProvider CreateSettingsProvider()
            {
                return new SettingsUIProvider();
            }
        }
    }
}