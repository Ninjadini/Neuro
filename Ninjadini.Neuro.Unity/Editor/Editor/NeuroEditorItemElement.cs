using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroEditorItemElement : VisualElement
    {
        NeuroEditorHistory _history;
        UnsignedIntegerField refIdTxt;
        TextField refNameTxt;
        NeuroObjectInspector objectInspector;
        NeuroDataFile dataFile;
        NeuroEditorDataProvider _dataProvider;
        NeuroEditorRefLinkItemsElement refLinksElement;

        uint drawnRefId;
        string drawnRefName;
        
        public Action AnyValueChanged;

        public NeuroEditorItemElement(NeuroEditorHistory history)
        {
            _history = history;
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
            NeuroUiUtils.SetPlaceholderText(refNameTxt, "RefName");
            refNameTxt.selectAllOnFocus = false;
            refNameTxt.selectAllOnMouseUp = false;
            refNameTxt.RegisterCallback<KeyDownEvent>((evt) =>
            {
                if (!char.IsControl(evt.character) &&
                    Regex.IsMatch(evt.character.ToString(), NeuroDataFile.InvalidFileNameRegExp))
                {
                    evt.StopPropagation();
#if UNITY_2023_2_OR_NEWER
                    focusController?.IgnoreEvent(evt);
#else
                    evt.PreventDefault();
#endif
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
                objectInspector = new NeuroObjectInspector(dataProvider.References, _history);
                Add(objectInspector);
                objectInspector.AnyValueChanged = OnAnyValueChanged;
            }

            refLinksElement.Draw(dataProvider, type, value);
            objectInspector.Draw(type, value, OnValueSet);
            UpdateFilePath();
            RecordUndo(NeuroEditorUndoRedos.UndoType.View);
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
            RecordUndo(NeuroEditorUndoRedos.UndoType.Update);
            UpdateFilePath();
            AnyValueChanged?.Invoke();
            _dataProvider.SaveData(dataFile);
        }

        void RecordUndo(NeuroEditorUndoRedos.UndoType undoType)
        {
            EditorWindow window = null;
            var p = parent;
            while (p != null)
            {
                if (p is NeuroEditorNavElement w)
                {
                    window = w.EditorWindow;
                    break;
                }
                p = p.parent;
            }
            NeuroEditorUndoRedos.Record(dataFile, undoType, window);
        }

        void UpdateFilePath()
        {
            refIdTxt.SetValueWithoutNotify(dataFile.RefId);
            refNameTxt.SetValueWithoutNotify(dataFile.RefName);
            NeuroUiUtils.UpdatePlaceholderTextVisibility(refNameTxt);
            var enable = !typeof(ISingletonReferencable).IsAssignableFrom(dataFile.RootType);
            refIdTxt.SetEnabled(enable);
            refNameTxt.SetEnabled(enable);
        }

        void OnRefNameChanged(ChangeEvent<string> evt)
        {
            _dataProvider.SetRefName(dataFile, evt.newValue);
            UpdateFilePath();
            AnyValueChanged?.Invoke();
        }
    }
}