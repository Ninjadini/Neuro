using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    /// Moves a value from one json field name to another after a `[Neuro]` field has been renamed in code.
    /// See NeuroJsonFieldRenamer for what actually happens to the files.
    public class NeuroFieldRenameWindow : EditorWindow
    {
        [MenuItem("Tools/Neuro/Migrate Renamed Field...", priority = 202)]
        public static void ShowWindow()
        {
            GetWindow<NeuroFieldRenameWindow>("Neuro Field").Show();
        }

        readonly List<FieldInfo> fields = new List<FieldInfo>();
        Dictionary<string, Type> typesByName;
        Type selectedType;

        SearchablePopupField<string> classDropdown;
        ListView fieldsList;
        TextField oldNameTxt;
        Label warningLbl;
        Button migrateBtn;
        Label resultLbl;

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = root.style.paddingRight = root.style.paddingTop = 5;

            AddWrappedLabel(root,
                "JSON data is keyed by field name, binary by tag. So renaming a <b>[Neuro]</b> field in code costs nothing in binary, " +
                "but every NeuroData json still holds the old key and the value is dropped on the next read.\n" +
                "Pick the class and the field's new name, type what it used to be called, and the key is renamed in the data files. " +
                "Only json objects of that class are touched, and only where the new name is not there already.");

            classDropdown = new SearchablePopupField<string>();
            classDropdown.label = "Class";
            // Up front rather than in BeforePopupShown - a PopupField only draws a value that is in its choices.
            classDropdown.choices = GetTypesByName().Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            classDropdown.RegisterValueChangedCallback(_ => OnClassChanged());
            root.Add(classDropdown);

            var fieldsLbl = AddWrappedLabel(root, "");
            fieldsLbl.text = "Neuro fields - pick the one the value should end up in:";
            fieldsLbl.style.marginTop = 6;

            fieldsList = new ListView
            {
                itemsSource = fields,
                fixedItemHeight = 18,
                selectionType = SelectionType.Single,
                showBorder = true,
                style = { height = 180, flexShrink = 0 }
            };
            fieldsList.makeItem = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4 } };
            fieldsList.bindItem = (element, i) => ((Label)element).text = DescribeField(fields[i]);
            fieldsList.selectionChanged += _ => UpdateState();
            root.Add(fieldsList);

            oldNameTxt = new TextField("Old field name");
            oldNameTxt.tooltip = "The name the field had in the json, before it was renamed in code.";
            oldNameTxt.style.marginTop = 6;
            oldNameTxt.RegisterValueChangedCallback(_ => UpdateState());
            root.Add(oldNameTxt);

            warningLbl = AddWrappedLabel(root, "");
            warningLbl.style.color = new Color(1f, 0.75f, 0.3f);

            var buttons = NeuroUiUtils.AddHorizontal(root);
            buttons.style.marginTop = 4;
            NeuroUiUtils.AddButton(buttons, "Preview", () => Run(true));
            migrateBtn = NeuroUiUtils.AddButton(buttons, "Migrate", () => Run(false));

            var scroll = new ScrollView { style = { flexGrow = 1, marginTop = 6 } };
            resultLbl = AddWrappedLabel(scroll, "");
            resultLbl.selection.isSelectable = true;
            root.Add(scroll);

            UpdateState();
        }

        static Label AddWrappedLabel(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.enableRichText = true;
            parent.Add(label);
            return label;
        }

        Dictionary<string, Type> GetTypesByName()
        {
            if (typesByName == null)
            {
                typesByName = new Dictionary<string, Type>();
                foreach (var type in NeuroEditorUtils.FindAllNeuroTypesCached())
                {
                    // FullName spells nested types with a `+`, which nobody writes in C#.
                    typesByName[type.FullName?.Replace('+', '.') ?? type.Name] = type;
                }
            }
            return typesByName;
        }

        static string DescribeField(FieldInfo field)
        {
            var tag = field.GetCustomAttribute<NeuroAttribute>()?.Tag ?? 0;
            return $"<b>{field.Name}</b>   <color=#808080>{{tag {tag}}}  {NeuroEditorUtils.GetTypeName(field.FieldType)}  ({NeuroEditorUtils.GetTypeName(field.DeclaringType)})</color>";
        }

        void OnClassChanged()
        {
            selectedType = classDropdown.value != null && GetTypesByName().TryGetValue(classDropdown.value, out var type) ? type : null;
            fields.Clear();
            if (selectedType != null)
            {
                fields.AddRange(NeuroJsonFieldRenamer.GetNeuroFields(selectedType));
            }
            fieldsList.ClearSelection();
            fieldsList.Rebuild();
            UpdateState();
        }

        FieldInfo SelectedField => fieldsList.selectedIndex >= 0 && fieldsList.selectedIndex < fields.Count
            ? fields[fieldsList.selectedIndex]
            : null;

        void UpdateState()
        {
            var newName = SelectedField?.Name;
            var oldName = oldNameTxt.value?.Trim();
            var warning = "";
            if (selectedType != null && !string.IsNullOrEmpty(oldName))
            {
                if (oldName == newName)
                {
                    warning = "The old and new names are the same.";
                }
                else if (fields.Any(f => f.Name == oldName))
                {
                    warning = $"`{oldName}` is still a [Neuro] field on this class - if the rename has not happened in code yet, " +
                              "migrating now would move live data onto the other field.";
                }
            }
            warningLbl.text = warning;
            NeuroUiUtils.SetDisplay(warningLbl, warning.Length > 0);
            migrateBtn.SetEnabled(selectedType != null && !string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(oldName) && oldName != newName);
        }

        void Run(bool dryRun)
        {
            var newField = SelectedField;
            var oldName = oldNameTxt.value?.Trim();
            if (selectedType == null || newField == null || string.IsNullOrEmpty(oldName) || oldName == newField.Name)
            {
                resultLbl.text = "<color=#e08080>Pick a class, a field, and type the old field name first.</color>";
                return;
            }
            NeuroJsonFieldRenamer.Result result;
            var renamer = new NeuroJsonFieldRenamer();
            try
            {
                result = renamer.Rename(selectedType, oldName, newField.Name, true);
            }
            catch (Exception e)
            {
                resultLbl.text = "<color=#e08080>" + e.Message + "</color>";
                Debug.LogException(e);
                return;
            }
            if (!dryRun)
            {
                if (result.Renamed.Count == 0)
                {
                    EditorUtility.DisplayDialog("Nothing to migrate",
                        DescribeResult(result, true), "OK");
                    resultLbl.text = DescribeResult(result, true);
                    return;
                }
                var message = $"Rename `{oldName}` to `{newField.Name}` on {result.Renamed.Count} {NeuroEditorUtils.GetTypeName(selectedType)} object(s), " +
                              $"across {result.Renamed.Select(m => m.FilePath).Distinct().Count()} data file(s)?\n\n" +
                              "Only the json key is rewritten - the rest of each file is left as it is. Make sure your data is committed to source control first.";
                if (!EditorUtility.DisplayDialog("Migrate field name", message, "Migrate", "Cancel"))
                {
                    return;
                }
                try
                {
                    result = renamer.Rename(selectedType, oldName, newField.Name, false);
                }
                catch (Exception e)
                {
                    resultLbl.text = "<color=#e08080>" + e.Message + "</color>";
                    Debug.LogException(e);
                    return;
                }
                Debug.Log($"Neuro ~ renamed `{oldName}` to `{newField.Name}` on {NeuroEditorUtils.GetTypeName(selectedType)} in {result.ChangedFiles.Count} data file(s).");
            }
            resultLbl.text = DescribeResult(result, dryRun);
        }

        static string DescribeResult(NeuroJsonFieldRenamer.Result result, bool dryRun)
        {
            var text = result.Renamed.Count == 0
                ? result.Skipped.Count > 0
                    ? "<b>Nothing to rename.</b>"
                    : "<b>No object had the old field name.</b>"
                : dryRun
                    ? $"<b>{result.Renamed.Count} field(s) would be renamed:</b>"
                    : $"<b>{result.Renamed.Count} field(s) renamed in {result.ChangedFiles.Count} file(s):</b>";
            foreach (var match in result.Renamed)
            {
                text += "\n   " + match;
            }
            if (result.Skipped.Count > 0)
            {
                text += $"\n\n<b>{result.Skipped.Count} left alone - they already have the new field:</b>";
                foreach (var match in result.Skipped)
                {
                    text += "\n   " + match;
                }
            }
            if (result.Problems.Count > 0)
            {
                text += "\n\n<color=#e08080><b>Problems:</b>";
                foreach (var problem in result.Problems)
                {
                    text += "\n   " + problem;
                }
                text += "</color>";
            }
            return text;
        }
    }
}
