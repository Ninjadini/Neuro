using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    [FilePath("ProjectSettings/NeuroSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class NeuroUnityEditorSettings : ScriptableSingleton<NeuroUnityEditorSettings>
    {
        public const string SETTINGS_MENU_PATH = "Project/Ninjadini ❖ Neuro";
        public const string DEFAULT_DATA_PATH = "NeuroData";
        const string PrimaryDataPathTooltip = "Location of JSON data files.\nDefault value: " + DEFAULT_DATA_PATH;

        [Tooltip(PrimaryDataPathTooltip)]
        public string PrimaryDataPath;
        
        [Tooltip("This is required so you can access your references data in build. But you can turn it off if you want to manage the loading manually.\nDefault value: true")]
        public bool BakeDataResourcesForBuild = true;
        
        [Tooltip("This is where Neuro will bake the data for builds as "+ NeuroDataProvider.BinaryResourceName + "."+NeuroDataProvider.BinaryResourceExtension+" file.\nDefault value: Assets/Resources/")]
        public string ResourcesDir = "Assets/Resources/";
        
        [Header("Debug")]
        [Tooltip("Debug.Log() neuro loading timings in case you need to know how long things are taking.")]
        public bool LogTimings;
        
        [Tooltip("Show dialog if json data file changes are detected")]
        public bool ShowDialogOnDataFileChange;
        
        [Header("Experimental")]
        public bool UndoRedosEnabled;

        [Header("Advanced")]
        [Tooltip("This is required for Neuro to function properly but you can disable it if you know what you are doing.\nDefault value: true")]
        public bool BakeAutoTypeRegistryForBuild = true;
        
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

                SetUpPrimaryDataPathUI(rootElement, settings);
                
                var serializedObject = new SerializedObject(settings);

                var serializedProperty = serializedObject.GetIterator();
                if (serializedProperty.NextVisible(true))
                {
                    while (serializedProperty.NextVisible(false))
                    {
                        if (serializedProperty.name == nameof(PrimaryDataPath))
                        {
                            continue;
                        }
                        
                        var field = new PropertyField();
                        field.style.paddingLeft = 8;
                        field.BindProperty(serializedProperty);
                        rootElement.Add(field);
                    }
                }
            }

            void SetUpPrimaryDataPathUI(VisualElement rootElement, NeuroUnityEditorSettings settings)
            {
                var dataPathField = new TextField(nameof(PrimaryDataPath));
                
                Action applyAct = () =>
                {
                    if (dataPathField.value == settings.PrimaryDataPath)
                    {
                        // NA
                    }
                    else if (string.IsNullOrEmpty(dataPathField.value) || dataPathField.value == "./" || dataPathField.value == ".")
                    {
                        dataPathField.value = settings.PrimaryDataPath;
                    }
                    else if (Directory.Exists(dataPathField.value))
                    {
                        settings.PrimaryDataPath = dataPathField.value;
                        OnSaveClicked();
                        NeuroEditorDataProvider.Shared.FullScriptReload();
                    }
                    else
                    {
                        if (EditorUtility.DisplayDialog("PrimaryDataPath",
                                $"Directory {dataPathField.value} does not exist", "Show closest directory", "Cancel"))
                        {
                            NeuroUiUtils.RevealFileOrDirInFinder(dataPathField.value);
                        }

                        dataPathField.value = settings.PrimaryDataPath;
                    }
                };
                
                var horizontal = NeuroUiUtils.AddHorizontal(rootElement);
                NeuroUiUtils.SetBorder(horizontal, new Color(0.3f, 0.3f, 0.2f));
                horizontal.style.marginLeft = 8;
                
                dataPathField.value = settings.PrimaryDataPath;
                dataPathField.style.flexGrow = 1f;
                dataPathField.tooltip = PrimaryDataPathTooltip;
                horizontal.Add(dataPathField);
                
                NeuroUiUtils.AddButton(horizontal, "⊙", () =>
                {
                    var newDir = EditorUtility.OpenFolderPanel("", "./", DEFAULT_DATA_PATH);
                    if (!string.IsNullOrEmpty(newDir))
                    {
                        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), newDir);
                        if (!string.IsNullOrEmpty(relativePath))
                        {
                            dataPathField.value = relativePath;
                            applyAct();
                        }
                    }
                });
                NeuroUiUtils.AddButton(horizontal, "Apply", applyAct);
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