using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroEditorItemElement : VisualElement
    {
        NeuroEditorHistory _history;
        TextField refIdTxt;
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
            // A text field rather than an UnsignedIntegerField because RefIds are shown in their base36 form
            // here, the same form that is in the file name.
            refIdTxt = new TextField();
            refIdTxt.style.minWidth = 40;
            refIdTxt.isDelayed = true;
            refIdTxt.tooltip = RefIdTooltip;
            refIdTxt.selectAllOnFocus = false;
            refIdTxt.selectAllOnMouseUp = false;
            refIdTxt.RegisterCallback<KeyDownEvent>((evt) =>
            {
                // RefIds are base36, so anything that is not a letter or a digit can not be part of one.
                if (!char.IsControl(evt.character) && !char.IsLetterOrDigit(evt.character))
                {
                    evt.StopPropagation();
#if UNITY_2023_2_OR_NEWER
                    focusController?.IgnoreEvent(evt);
#else
                    evt.PreventDefault();
#endif
                }
            });
            refIdTxt.RegisterValueChangedCallback(OnRefIdChanged);
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

        const string RefIdTooltip = "RefId - editing this moves the item to a new id and repoints everything that referenced it";

        void UpdateFilePath()
        {
            refIdTxt.SetValueWithoutNotify(NeuroRefId.ToString(dataFile.RefId));
            // the field shows base36, but the id is a uint everywhere else - in code, in save games, in a binary
            // dump - so the plain number is worth being able to see without doing the maths.
            refIdTxt.tooltip = $"RefId {NeuroRefId.ToString(dataFile.RefId)} = {dataFile.RefId.ToString()} raw number\n\n{RefIdTooltip}";
            refNameTxt.SetValueWithoutNotify(dataFile.RefName);
            NeuroUiUtils.UpdatePlaceholderTextVisibility(refNameTxt);
            var enable = !typeof(ISingletonReferencable).IsAssignableFrom(dataFile.RootType);
            refIdTxt.SetEnabled(enable);
            refIdTxt.isReadOnly = !enable;
            refNameTxt.SetEnabled(enable);
        }

        void OnRefNameChanged(ChangeEvent<string> evt)
        {
            _dataProvider.SetRefName(dataFile, evt.newValue);
            UpdateFilePath();
            AnyValueChanged?.Invoke();
        }

        void OnRefIdChanged(ChangeEvent<string> evt)
        {
            if (dataFile == null || evt.newValue == NeuroRefId.ToString(dataFile.RefId))
            {
                return;
            }
            if (!NeuroRefId.TryParse(evt.newValue, out var newRefId))
            {
                EditorUtility.DisplayDialog("Invalid RefId",
                    $"`{evt.newValue}` is not a valid RefId.\n\nRefIds are base36 - digits and letters only, such as `4zbc`.",
                    "OK");
                UpdateFilePath();
                return;
            }
            var problem = _dataProvider.GetRefIdChangeProblem(dataFile, newRefId);
            if (problem != null)
            {
                EditorUtility.DisplayDialog("Can not use that RefId", problem, "OK");
                UpdateFilePath();
                return;
            }
            var oldRefId = dataFile.RefId;
            // count first so that the confirmation can say how much of the database this is about to touch.
            var referencingItems = ReferencedItemsFinder.SearchInReferences(dataFile.Value, _dataProvider.References);
            var referencingCount = referencingItems.Select(r => r.referencable).Distinct().Count();
            // The raw numbers are spelled out whatever the `Show Raw Ref Id Numbers` setting says - this is the
            // moment someone needs them, to go and fix up ids held outside the data. DisplayRefId is not used
            // here because it would print the number twice when that setting is on.
            var message = $"Change RefId from `{NeuroRefId.ToString(oldRefId)}` ({oldRefId.ToString()})";
            message += $" to `{NeuroRefId.ToString(newRefId)}` ({newRefId.ToString()})?\n\n";
            message += referencingCount == 0
                ? "Nothing else in the data references this item."
                : $"{referencingCount} other item(s) reference this one and will be repointed at the new id and saved.";
            message += "\n\nThe data file will be renamed. Undo only covers this item, not the others that get repointed.";
            message += "\nAnything outside the Neuro data that stored the old id (scenes, prefabs, save games, hard coded ids) will not be updated.";
            if (!EditorUtility.DisplayDialog("Change RefId", message, "Change", "Cancel"))
            {
                UpdateFilePath();
                return;
            }
            try
            {
                var updated = _dataProvider.ChangeRefId(dataFile, newRefId);
                Debug.Log($"RefId changed from `{NeuroEditorUtils.DisplayRefId(oldRefId)}` to `{NeuroEditorUtils.DisplayRefId(newRefId)}`, {updated.Count} referencing item(s) updated.");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Could not change RefId", e.Message, "OK");
                Debug.LogException(e);
                UpdateFilePath();
                return;
            }
            RecordUndo(NeuroEditorUndoRedos.UndoType.Update);
            UpdateFilePath();
            AnyValueChanged?.Invoke();
        }
    }
}