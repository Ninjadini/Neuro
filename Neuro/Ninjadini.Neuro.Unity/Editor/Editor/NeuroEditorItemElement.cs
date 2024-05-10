using System;
using System.IO;
using System.Text.RegularExpressions;
using Ninjadini.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroEditorItemElement : VisualElement, ObjectInspector.IController
    {
        UnsignedIntegerField refIdTxt;
        TextField refNameTxt;
        NeuroObjectInspector objectInspector;
        NeuroDataFile dataFile;
        NeuroEditorDataProvider _dataProvider;
        NeuroEditorRefLinkItemsElement refLinksElement;

        uint drawnRefId;
        string drawnRefName;
        
        public Action AnyValueChanged;

        public NeuroEditorItemElement()
        {
            var horizontal = NeuroUiUtils.AddHorizontal(this);
            refIdTxt = new UnsignedIntegerField();
            refIdTxt.style.minWidth = 30;
            refIdTxt.isReadOnly = true;
            refIdTxt.tooltip = "RefId";
            horizontal.Add(refIdTxt);
            
            refNameTxt = new TextField();
            refNameTxt.style.flexGrow = 1f;
            refNameTxt.isDelayed = true;
            refNameTxt.tooltip = "RefName";
            refNameTxt.selectAllOnFocus = false;
            refNameTxt.selectAllOnMouseUp = false;
            refNameTxt.RegisterCallback<KeyDownEvent>((evt) =>
            {
                if (!char.IsControl(evt.character) &&
                    Regex.IsMatch(evt.character.ToString(), NeuroDataFile.InvalidFileNameRegExp))
                {
                    evt.StopPropagation();
                    //evt.PreventDefault();
                    focusController?.IgnoreEvent(evt);
                }
            });
            refNameTxt.RegisterValueChangedCallback(OnRefNameChanged);
            horizontal.Add(refNameTxt);
            
            NeuroUiUtils.AddButton(horizontal, "⊙ File", OnLocateFileClicked);
            NeuroUiUtils.AddButton(horizontal, "⌨ Code", GoToScriptBtnClicked);
        }

        void GoToScriptBtnClicked()
        {
            NeuroUiUtils.OpenScript(dataFile.Value.GetType());
        }

        void OnLocateFileClicked()
        {
            EditorUtility.RevealInFinder(dataFile.FilePath);
        }

        public void Draw(NeuroEditorDataProvider dataProvider, Type type, NeuroDataFile dataFile_)
        {
            _dataProvider = dataProvider;
            dataFile = dataFile_;
            var value = dataFile.Value;

            if (refLinksElement == null)
            {
                refLinksElement = new NeuroEditorRefLinkItemsElement();
                Add(refLinksElement);
            }
            if (objectInspector == null)
            {
                objectInspector = new NeuroObjectInspector(dataProvider.References);
                Add(objectInspector);
                objectInspector.AnyValueChanged = OnAnyValueChanged;
            }

            refLinksElement.Draw(dataProvider, type, value);
            objectInspector.Draw(type, value, OnValueSet, this);
            UpdateFilePath();
        }

        bool ObjectInspector.IController.CanSetToNull(Type type, object value)
        {
            return value != dataFile.Value;
        }

        void OnValueSet(object newValue)
        {
            if (newValue is IReferencable referencable)
            {
                dataFile.Value = referencable;
                OnAnyValueChanged();
            }
        }

        void OnAnyValueChanged()
        {
            UpdateFilePath();
            AnyValueChanged?.Invoke();
            _dataProvider.SaveData(dataFile);
        }

        void UpdateFilePath()
        {
            refIdTxt.value = dataFile.RefId;
            refNameTxt.value = dataFile.RefName;
            var enable = !typeof(ISingletonReferencable).IsAssignableFrom(dataFile.RootType);
            refIdTxt.SetEnabled(enable);
            refNameTxt.SetEnabled(enable);
        }

        void OnRefNameChanged(ChangeEvent<string> evt)
        {
            _dataProvider.SetRefName(dataFile, evt.newValue);
            UpdateFilePath();
        }
    }
}