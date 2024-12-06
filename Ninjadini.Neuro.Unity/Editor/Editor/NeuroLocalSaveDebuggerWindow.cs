using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public partial class NeuroLocalSaveDebuggerWindow : EditorWindow
    {
        [MenuItem("Tools/Neuro/File Content Debugger", priority = 106)]
        public static void ShowWindow()
        {
            GetWindow<NeuroLocalSaveDebuggerWindow>("Neuro File").Show();
        }
        
        public enum Format
        {
            JSON,
            Binary,
            //BinaryAs0XString
        }

        [SerializeField] string srcType;
        [SerializeField] string srcFilePath;
        [SerializeField] string persistentDataName;
        [SerializeField] string srcTxt;
        [SerializeField] string typeName;
        [SerializeField] Format srcFormat;

        const string GlobalTypeDropDownName = "object with <u>-globalType</u>";
        static IContentSourceProvider[] _sourceProviders;

        VisualElement _srcProviderContent;
        IContentSourceProvider _srcProvider;
        NeuroObjectInspector _objectInspector;
        object _drawnObj;
        Button _newBtn;

        public void CreateGUI()
        {
            _sourceProviders ??= NeuroEditorUtils.CreateFromScannableTypes<IContentSourceProvider>().ToArray();
            
            var choices = _sourceProviders.Select(p => p.DropDownName).ToList();
            if (!choices.Contains(srcType)) srcType = choices.First();
            var sourceDropDown = new DropdownField(choices, srcType);
            sourceDropDown.label = "Source";
            sourceDropDown.RegisterValueChangedCallback(OnSourceDropDownChanged);
            rootVisualElement.Add(sourceDropDown);

            _srcProviderContent = new VisualElement();
            _srcProviderContent.style.marginLeft = 10;
            rootVisualElement.Add(_srcProviderContent);
            OnSourceDropDownChanged(ChangeEvent<string>.GetPooled("", srcType));

            if (string.IsNullOrEmpty(typeName)) typeName = GlobalTypeDropDownName;
            var typeDropdown = new SearchablePopupField<string>();
            typeDropdown.label = "Type";
            typeDropdown.BeforePopupShown = () => OnBeforeTypesPopupShown(typeDropdown);
            typeDropdown.value = typeName;
            typeDropdown.RegisterValueChangedCallback(OnTypeDropDownChanged);
            rootVisualElement.Add(typeDropdown);

            var formatDropDown = new EnumField("Format", srcFormat);
            formatDropDown.RegisterValueChangedCallback(OnFormatDropDownChanged);
            rootVisualElement.Add(formatDropDown);

            var horizontal = NeuroUiUtils.AddHorizontal(rootVisualElement);
            
            var horizontalLeft = NeuroUiUtils.AddHorizontal(horizontal);
            horizontalLeft.style.backgroundColor = new Color(0f, 0.2f, 0.1f);
            horizontalLeft.style.flexGrow = 1f;
            NeuroUiUtils.AddButton(horizontalLeft, "Load v", OnLoadClicked);
            _newBtn = NeuroUiUtils.AddButton(horizontalLeft, "New v", OnNewClicked);
            
            var horizontalRight = NeuroUiUtils.AddHorizontal(horizontal);
            horizontalRight.style.backgroundColor = new Color(0.2f, 0.0f, 0.1f);
            horizontalRight.style.flexGrow = 1f;
            horizontalRight.style.flexDirection = FlexDirection.RowReverse;
            NeuroUiUtils.AddButton(horizontalRight, "Save ^", OnSaveClicked);
            NeuroUiUtils.AddButton(horizontalRight, "Delete X", OnDeleteClicked);
            rootVisualElement.Add(new VisualElement()
            {
                style =
                {
                    height = 2f,
                    marginTop = 5f,
                    marginBottom = 5f,
                    backgroundColor = Color.gray
                }
            });
        }

        void OnBeforeTypesPopupShown(SearchablePopupField<string> typeDropdown)
        {
            if (typeDropdown.choices == null || typeDropdown.choices.Count == 0)
            {
                var choices = new List<string>();
                choices.Add(GlobalTypeDropDownName);
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
        }

        void OnFormatDropDownChanged(ChangeEvent<Enum> evt)
        {
            srcFormat = (Format)evt.newValue;
        }

        void OnSourceDropDownChanged(ChangeEvent<string> evt)
        {
            srcType = evt.newValue;
            var srcProvider = _sourceProviders.FirstOrDefault(t => t.DropDownName == srcType);
            if (srcProvider != null)
            {
                _srcProvider = srcProvider;
                _srcProviderContent.Clear();
                srcProvider.CreateGUI(_srcProviderContent, this);
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
                if (bytes != null)
                {
                    Show(bytes);
                }
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
                    Show(NeuroEditorDataProvider.Shared.jsonReader.Read(json, type));
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
                    Show(NeuroBytesReader.Shared.Read(bytes, type));
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
                return;
            }
            if (_objectInspector == null)
            {
                _objectInspector = new NeuroObjectInspector(NeuroEditorDataProvider.SharedReferences);
                rootVisualElement.Add(_objectInspector);
            }
            _drawnObj = obj;
            _objectInspector.Draw(new ObjectInspector.Data()
            {
                getter = ()=>_drawnObj
            });
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
                        json = NeuroEditorDataProvider.Shared.jsonWriter.Write(_drawnObj);
                    }
                    bytes = Encoding.UTF8.GetBytes(json);
                }
                else if (srcFormat == Format.Binary)
                {
                    if (type == typeof(object))
                    {
                        bytes = NeuroBytesWriter.Shared.WriteGlobalType(_drawnObj).ToArray();
                    }
                    else
                    {
                        bytes = NeuroBytesWriter.Shared.Write(_drawnObj).ToArray();
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