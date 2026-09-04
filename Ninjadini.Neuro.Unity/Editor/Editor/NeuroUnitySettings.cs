using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    [FilePath("ProjectSettings/NeuroSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class NeuroUnityEditorSettings : ScriptableSingleton<NeuroUnityEditorSettings>
    {
        public const string SETTINGS_MENU_PATH = "Project/Ninjadini ❖ Neuro";
        public const string DEFAULT_DATA_PATH = "NeuroData";
        /// Ends with `~` so unity's asset importer ignores the folder, the json data can then live inside Assets/
        /// without being imported as assets.
        public const string DEFAULT_EXTRA_DATA_PATH = "Assets/NeuroData/";
        const string PrimaryDataPathTooltip = "Location of JSON data files.\nDefault value: " + DEFAULT_DATA_PATH;
        const string ExtraDataPathsTooltip = "Extra locations of JSON data files, they are loaded in addition to PrimaryDataPath.\n" +
                                             "A directory that does not exist is simply skipped, so you can leave paths in here for optional data sets.\n" +
                                             "New objects are still created in PrimaryDataPath (or the type's own DataPath).\n" +
                                             "Default value: " + DEFAULT_EXTRA_DATA_PATH;

        [Tooltip(PrimaryDataPathTooltip)]
        public string PrimaryDataPath;

        [Tooltip(ExtraDataPathsTooltip)]
        public List<string> ExtraDataPaths = new List<string>() { DEFAULT_EXTRA_DATA_PATH };
        
        [Tooltip("This is required so you can access your references data in build. But you can turn it off if you want to manage the loading manually.\nDefault value: true")]
        public bool BakeDataResourcesForBuild = true;
        
        [Tooltip("This is where Neuro will bake the data for builds as "+ NeuroDataProvider.BinaryResourceName + "."+NeuroDataProvider.BinaryResourceExtension+" file.\nDefault value: Assets/Resources/")]
        public string ResourcesDir = "Assets/Resources/";
        
        [Header("Experimental")]
        public bool UndoRedosEnabled;

        [Header("Advanced")]
        [Tooltip("This is required for Neuro to function properly but you can disable it if you know what you are doing.\nDefault value: true")]
        public bool BakeAutoTypeRegistryForBuild = true;
        
        public List<NeuroEditorTypeItemSetting> ClassSettings = new List<NeuroEditorTypeItemSetting>();

        /// The RefId text format the data files on disk are written in. See NeuroRefIdMigration.
        /// 0 is the original decimal spelling, and is what a project that predates base36 RefIds deserializes as.
        [HideInInspector] public int RefIdFormatVersion;

        /// These moved to NeuroUnityUserSettings, they only exist so old ProjectSettings/NeuroSettings.asset
        /// values can be carried over once. Safe to delete once everyone has opened the project on this version.
        [SerializeField, HideInInspector, FormerlySerializedAs("LogTimings")] internal bool MigratedLogTimings;
        [SerializeField, HideInInspector, FormerlySerializedAs("ShowDialogOnDataFileChange")] internal bool MigratedShowDialogOnDataFileChange;
        [SerializeField, HideInInspector, FormerlySerializedAs("ShowRawRefIdNumbers")] internal bool MigratedShowRawRefIdNumbers;
        
        public NeuroUnityEditorSettings() : base()
        {
            PrimaryDataPath = DEFAULT_DATA_PATH;
        }

        public static NeuroUnityEditorSettings Get()
        {
            return instance;
        }

        /// True if the path is one of the optional extra data paths, those are allowed to not exist on disk.
        public bool IsExtraDataPath(string path)
        {
            return ExtraDataPaths != null && ExtraDataPaths.Contains(path);
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
                var settings = Get();
                settings.hideFlags = HideFlags.None;
                
                var title = NeuroUiUtils.AddLabel(rootElement, "❖ Neuro");
                title.style.fontSize = 19;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.paddingLeft = 10;
                title.style.paddingTop = 1;
                title.style.paddingBottom = 5;

                AddUserSettingsUI(rootElement);

                var serializedObject = new SerializedObject(settings);
                
                var dataPathsBox = new VisualElement();
                NeuroUiUtils.SetBorder(dataPathsBox, new Color(0.3f, 0.3f, 0.2f));
                dataPathsBox.style.marginLeft = 8;
                rootElement.Add(dataPathsBox);
                SetUpPrimaryDataPathUI(dataPathsBox, settings);
                SetUpExtraDataPathsUI(dataPathsBox, serializedObject);

                var serializedProperty = serializedObject.GetIterator();
                if (serializedProperty.NextVisible(true))
                {
                    while (serializedProperty.NextVisible(false))
                    {
                        if (serializedProperty.name == nameof(PrimaryDataPath)
                            || serializedProperty.name == nameof(ExtraDataPaths))
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

            void AddUserSettingsUI(VisualElement rootElement)
            {
                var userSettings = NeuroUnityUserSettings.Get();
                userSettings.hideFlags = HideFlags.None;
                
                var box = new VisualElement();
                NeuroUiUtils.SetBorder(box, new Color(0.2f, 0.3f, 0.22f));
                box.style.marginLeft = 8;
                box.style.marginTop = 10;
                box.style.marginRight = 8;
                rootElement.Add(box);
                
                var header = NeuroUiUtils.AddLabel(box, "User settings");
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                
                var note = NeuroUiUtils.AddLabel(box, "These apply to you only, they are stored in UserSettings/ instead of ProjectSettings/ so they are not shared with the rest of the team.");
                note.style.opacity = 0.7f;
                note.style.whiteSpace = WhiteSpace.Normal;
                note.style.paddingBottom = 5;
                
                var serializedObject = new SerializedObject(userSettings);
                var serializedProperty = serializedObject.GetIterator();
                if (serializedProperty.NextVisible(true))
                {
                    while (serializedProperty.NextVisible(false))
                    {
                        var field = new PropertyField();
                        field.BindProperty(serializedProperty);
                        box.Add(field);
                    }
                }
            }

            void SetUpPrimaryDataPathUI(VisualElement parent, NeuroUnityEditorSettings settings)
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
                
                var horizontal = NeuroUiUtils.AddHorizontal(parent);
                
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

            void SetUpExtraDataPathsUI(VisualElement box, SerializedObject serializedObject)
            {
                var field = new PropertyField();
                field.BindProperty(serializedObject.FindProperty(nameof(ExtraDataPaths)));
                field.tooltip = ExtraDataPathsTooltip;
                box.Add(field);
                
                // the paths are only read at load time, so the list needs an explicit apply like PrimaryDataPath does.
                NeuroUiUtils.AddButton(box, "Apply extra data paths", () =>
                {
                    OnSaveClicked();
                    NeuroEditorDataProvider.Shared.FullScriptReload();
                });
            }

            public override void OnInspectorUpdate()
            {
                OnSaveClicked();
            }

            void OnSaveClicked()
            {
                Get().Save();
                NeuroUnityUserSettings.Get().Save();
            }
                            
            [SettingsProvider]
            public static SettingsProvider CreateSettingsProvider()
            {
                return new SettingsUIProvider();
            }
        }
    }
}