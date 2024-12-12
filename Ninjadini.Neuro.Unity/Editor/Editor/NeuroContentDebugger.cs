using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public partial class NeuroContentDebugger : EditorWindow
    {
        [MenuItem("Tools/Neuro/Content Debugger", priority = 102)]
        public static void ShowWindow()
        {
            //GetWindow<NeuroDataDebugger>("NeuroContentDebugger").Show();
            CreateWindow<NeuroContentDebugger>("NeuroContentDebugger").Show();
        }
        
        public enum Format
        {
            JSON,
            Binary
        }

        [SerializeField] string srcType;
        [SerializeField] string srcFilePath;
        [SerializeField] string persistentDataName = "save";
        [SerializeField] string srcTxt;
        [SerializeField] string typeName;
        [SerializeField] Format srcFormat;

        const string GlobalTypeDropDownName = "object with <u>-globalType</u>";
        static ContentProvider[] _contentProviders;

        VisualElement _srcProviderContent;
        ContentProvider _srcProvider;
        NeuroObjectInspector _objectInspector;
        object _drawnObj;
        EnumField _formatField;
        SearchablePopupField<string> _typeField;
        Button _newBtn;
        Button _saveBtn;
        Button _delBtn;
        NeuroItemDebugDisplay _debugDisplay;

        public void CreateGUI()
        {
            _contentProviders ??= GetContentProviders();

            var initialProvider = _contentProviders.FirstOrDefault();
            if (_contentProviders.Length > 1)
            {
                var line = NeuroUiUtils.AddHorizontal(rootVisualElement);
                
                var choices = _contentProviders.ToList();
                if (!string.IsNullOrEmpty(srcType))
                {
                    var t = choices.Find(t => t.GetType().FullName == srcType);
                    if (t != null)
                    {
                        initialProvider = t;
                    }
                }
                var providerDropDown = new PopupField<ContentProvider>(choices, initialProvider, CreateFriendlyName, CreateFriendlyName)
                {
                    label = "Content Provider"
                };
                providerDropDown.style.flexGrow = 1f;
                providerDropDown.RegisterValueChangedCallback(OnProviderDropDownChanged);
                line.Add(providerDropDown);
                NeuroUiUtils.AddButton(line, "!", () =>
                {
                    EditorUtility.DisplayDialog(
                        "ContentProvider", 
                        "You can create your own content provider by extending `NeuroContentDebugger.ContentProvider`.\n\nYou may want to look at `NeuroContentDebugger.PersistentDataContentProvider` or `NeuroContentDebugger.FileContentProvider` as base class examples too.", 
                        "OK");
                });
                rootVisualElement.Add(line);
            }

            _srcProviderContent = new VisualElement();
            NeuroUiUtils.SetBorder(_srcProviderContent, new Color(0.3f, 0.3f, 0.2f));
            rootVisualElement.Add(_srcProviderContent);

            if (string.IsNullOrEmpty(typeName)) typeName = GlobalTypeDropDownName;
            _typeField = new SearchablePopupField<string>
            {
                label = "Type"
            };
            _typeField.BeforePopupShown = () => OnBeforeTypesPopupShown(_typeField);
            _typeField.value = typeName;
            _typeField.RegisterValueChangedCallback(OnTypeDropDownChanged);
            rootVisualElement.Add(_typeField);

            _formatField = new EnumField("Format", srcFormat);
            _formatField.RegisterValueChangedCallback(OnFormatDropDownChanged);
            rootVisualElement.Add(_formatField);

            var horizontal = NeuroUiUtils.AddHorizontal(rootVisualElement);
            horizontal.style.flexShrink = 0f;
            
            var horizontalLeft = NeuroUiUtils.AddHorizontal(horizontal);
            horizontalLeft.style.backgroundColor = new Color(0f, 0.2f, 0.1f);
            horizontalLeft.style.width = Length.Percent(50f);
            NeuroUiUtils.AddButton(horizontalLeft, "Load ↴", OnLoadClicked);
            _newBtn = NeuroUiUtils.AddButton(horizontalLeft, "New ↴", OnNewClicked);
            
            var horizontalRight = NeuroUiUtils.AddHorizontal(horizontal);
            horizontalRight.style.backgroundColor = new Color(0.2f, 0.0f, 0.1f);
            horizontalRight.style.width = Length.Percent(50f);
            horizontalRight.style.flexDirection = FlexDirection.RowReverse;
            _saveBtn = NeuroUiUtils.AddButton(horizontalRight, "Save ⤴", OnSaveClicked);
            _delBtn = NeuroUiUtils.AddButton(horizontalRight, "Delete ✕", OnDeleteClicked);
            
            OnProviderDropDownChanged(ChangeEvent<ContentProvider>.GetPooled(null, initialProvider));
            Show((object)null);
        }

        string CreateFriendlyName(ContentProvider provider)
        {
            var result = provider.GetType().Name;
            var baseName = nameof(ContentProvider);
            if (result.EndsWith(baseName) && result.Length > baseName.Length)
            {
                result = result.Substring(0, result.Length - baseName.Length);
            }
            return ObjectNames.NicifyVariableName(result);
        }

        protected virtual ContentProvider[] GetContentProviders()
        {
            return NeuroEditorUtils.CreateFromScannableTypes<ContentProvider>().ToArray();
        }

        void OnBeforeTypesPopupShown(SearchablePopupField<string> typeDropdown)
        {
            if (typeDropdown.choices == null || typeDropdown.choices.Count == 0)
            {
                var choices = new List<string> { GlobalTypeDropDownName };
                choices.AddRange(NeuroEditorUtils.FindAllNeuroTypesCached().Select(GetTypeName));
                typeDropdown.choices = choices;
            }
        }

        string GetTypeName(Type type)
        {
            var ns = type.Namespace;
            return string.IsNullOrEmpty(ns) ? NeuroEditorUtils.GetTypeName(type) : ns +"."+NeuroEditorUtils.GetTypeName(type);
        }

        void OnTypeDropDownChanged(ChangeEvent<string> evt)
        {
            typeName = evt.newValue;
            Show((object)null);
        }

        void OnFormatDropDownChanged(ChangeEvent<Enum> evt)
        {
            srcFormat = (Format)evt.newValue;
        }

        void OnProviderDropDownChanged(ChangeEvent<ContentProvider> evt)
        {
            var srcProvider = evt.newValue;
            if (srcProvider != null)
            {
                srcType = srcProvider.GetType().FullName;
                _srcProvider = srcProvider;
                _srcProviderContent.Clear();

                var allowedFormat = srcProvider.GetAllowedFormat();
                if (allowedFormat != null)
                {
                    _formatField.value = allowedFormat.Value;
                }
                _formatField.style.display = allowedFormat != null ? DisplayStyle.None : DisplayStyle.Flex;

                var allowedType = srcProvider.GetAllowedType();
                if (allowedType != null)
                {
                    _typeField.value = GetTypeName(allowedType);
                }
                _typeField.style.display = allowedType != null ? DisplayStyle.None : DisplayStyle.Flex;
                srcProvider.CreateGUI(_srcProviderContent, this);
                _srcProviderContent.style.display = _srcProviderContent.childCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        void OnNewClicked()
        {
            var type = GetSelectedType() ?? typeof(object);
            if (type == typeof(object))
            {
                ObjectInspector.ShowCreateInstanceWindow(NeuroGlobalTypes.GetAllRootTypes().ToArray(), _newBtn, Show);
            }
            else
            {
                ObjectInspector.ShowCreateInstanceWindow(type, _newBtn, Show);
            }
        }

        void OnLoadClicked()
        {
            try
            {
                var bytes = _srcProvider?.Load();
                Show(bytes);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                var msg =
                    $"There was an error loading the content.\nRead console log for more details.\n\"{e.InnerException?.Message ?? e.Message}\"";
                
                if (e.Message.Contains(NeuroJsonReader.NoGlobalTypeIdFoundErrMsg))
                {
                    msg = "The JSON text does not have the global type info (\"-globalType\").";
                }
                EditorUtility.DisplayDialog("Error", msg, "OK");
            }
        }

        void Show(byte[] bytes)
        {
            if (bytes == null)
            {
                Show((object)null);
                return;
            }
            var type = GetSelectedType();
            if (type == null)
            {
                EditorUtility.DisplayDialog("Error", $"No type was found with name {typeName}.\nWas it renamed since last time?", "OK");
                return;
            }
            
            if (srcFormat == Format.JSON)
            {
                var json = Encoding.UTF8.GetString(bytes);
                if (type == typeof(object))
                {
                    Show(NeuroEditorDataProvider.Shared.jsonReader.ReadGlobalTyped(json));
                }
                else
                {
                    Show(NeuroEditorDataProvider.Shared.jsonReader.ReadObject(json, type));
                }
            }
            else if (srcFormat == Format.Binary)
            {
                if (type == typeof(object))
                {
                    Show(NeuroBytesReader.Shared.ReadGlobalTyped(bytes));
                }
                else
                {
                    Show(NeuroBytesReader.Shared.ReadObject(bytes, type));
                }
            }
        }

        public Type GetSelectedType()
        {
            if (typeName == GlobalTypeDropDownName)
            {
                return typeof(object);
            }
            return NeuroEditorUtils.FindAllNeuroTypesCached().FirstOrDefault(t => GetTypeName(t) == typeName);
        }
        
        public Format GetSelectedFormat() => srcFormat;

        public void Show(object obj)
        {
            if (obj == null)
            {
                if (_objectInspector != null)
                {
                    _objectInspector.style.display = DisplayStyle.None;
                    _debugDisplay.style.display = DisplayStyle.None;
                }
                _saveBtn.style.display = DisplayStyle.None;
                return;
            }
            if (_objectInspector == null)
            {
                var scrollView = new ScrollView();
                scrollView.style.flexGrow = 1f;
                rootVisualElement.Add(scrollView);
                _objectInspector = new NeuroObjectInspector(NeuroEditorDataProvider.SharedReferences);
                scrollView.Add(_objectInspector);
                var border = new Color(0.3f, 0.35f, 0.3f);
                NeuroUiUtils.SetBorder(_objectInspector, border, 2f, 5f);
                _objectInspector.AnyValueChanged += OnAnyValueChanged;
                
                _debugDisplay = new NeuroItemDebugDisplay(NeuroEditorDataProvider.SharedReferences, () => _drawnObj, null);
                _debugDisplay.style.bottom = 0;
                _debugDisplay.style.flexShrink = 0.01f;
                _debugDisplay.SetReferencesTabEnabled(false);
                _debugDisplay.SetTestsTabEnabled(false);
                rootVisualElement.Add(_debugDisplay);
            }
            _drawnObj = obj;
            _objectInspector.style.display = DisplayStyle.Flex;
            _debugDisplay.style.display = DisplayStyle.Flex;
            _saveBtn.style.display = DisplayStyle.Flex;
            _objectInspector.Draw(new ObjectInspector.Data()
            {
                getter = ()=>_drawnObj
            });
            _debugDisplay.PrintAsGlobalType = typeName == GlobalTypeDropDownName;
            _debugDisplay.Refresh();
        }

        void OnAnyValueChanged()
        {
            _debugDisplay?.Refresh();
        }

        void OnDeleteClicked()
        {
            try
            {
                _srcProvider?.Delete();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                string msg = "There was an error deleting the content.\nRead console log for more details.";
                EditorUtility.DisplayDialog("Error", msg, "OK");
            }
        }

        void OnSaveClicked()
        {
            try
            {
                var type = GetSelectedType();
                if (type == null)
                {
                    EditorUtility.DisplayDialog("Error", $"No type was found with name {typeName}.\nWas it renamed since last time?", "OK");
                    return;
                }
                byte[] bytes = null;
                if (srcFormat == Format.JSON)
                {
                    string json;
                    if (type == typeof(object))
                    {
                        json = NeuroEditorDataProvider.Shared.jsonWriter.WriteGlobalTyped(_drawnObj);
                    }
                    else
                    {
                        json = NeuroEditorDataProvider.Shared.jsonWriter.WriteObject(_drawnObj);
                    }
                    bytes = Encoding.UTF8.GetBytes(json);
                }
                else if (srcFormat == Format.Binary)
                {
                    if (type == typeof(object))
                    {
                        bytes = NeuroBytesWriter.Shared.WriteGlobalTyped(_drawnObj).ToArray();
                    }
                    else
                    {
                        bytes = NeuroBytesWriter.Shared.WriteObject(_drawnObj).ToArray();
                    }
                }
                if (bytes != null)
                {
                    _srcProvider.Save(bytes);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                var msg =
                    $"There was an error saving the content.\nRead console log for more details.\n\"{e.InnerException?.Message ?? e.Message}\"";
                EditorUtility.DisplayDialog("Error", msg, "OK");
            }
        }
    }
}