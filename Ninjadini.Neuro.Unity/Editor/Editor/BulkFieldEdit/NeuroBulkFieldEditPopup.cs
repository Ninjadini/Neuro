using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    /// Asks for the number, says what it is about to touch, then does it.
    ///
    /// There is no second "are you sure" dialog on top of this: the count is in front of the user while they
    /// type, and the whole sweep is one undo entry, so the way back is Ctrl+Z rather than a modal.
    public class NeuroBulkFieldEditPopup : PopupWindowContent
    {
        const int Width = 340;

        readonly NeuroBulkFieldEditContext context;
        readonly ObjectInspector.Data data;
        readonly NeuroBulkFieldEdit.Operation operation;

        DoubleField operandField;
        Label previewLabel;
        Button applyButton;
        double operand;
        int itemCount;
        int fieldCount;

        public static void Show(NeuroBulkFieldEditContext context, ObjectInspector.Data data,
            NeuroBulkFieldEdit.Operation operation, VisualElement near)
        {
            var popup = new NeuroBulkFieldEditPopup(context, data, operation);
            var rect = near?.worldBound ?? new Rect(0, 0, Width, 0);
            UnityEditor.PopupWindow.Show(rect, popup);
        }

        NeuroBulkFieldEditPopup(NeuroBulkFieldEditContext context, ObjectInspector.Data data,
            NeuroBulkFieldEdit.Operation operation)
        {
            this.context = context;
            this.data = data;
            this.operation = operation;
            operand = DefaultOperand();
        }

        double DefaultOperand()
        {
            switch (operation)
            {
                case NeuroBulkFieldEdit.Operation.Multiply:
                    return 1;
                case NeuroBulkFieldEdit.Operation.Set:
                    // starts from what is in front of the user, so "set them all to this one" is one click.
                    return NeuroBulkFieldEdit.TryGetNumber(data.type, data.GetValue(), out var current) ? current : 0;
                default:
                    return 0;
            }
        }

        public override Vector2 GetWindowSize() => new Vector2(Width, 132);

        public override void OnOpen()
        {
            // Counted once, up front: how many fields the path finds does not depend on the number typed in.
            var dryRun = NeuroBulkFieldEdit.Apply(context.DataProvider, context.RootType, data.FieldPath,
                operation, operand, dryRun: true);
            itemCount = dryRun.ItemCount;
            fieldCount = dryRun.Fields;

            var root = editorWindow.rootVisualElement;
            root.style.paddingLeft = root.style.paddingRight = 8;
            root.style.paddingTop = root.style.paddingBottom = 6;

            var title = NeuroUiUtils.AddLabel(root, OperationName() + "  " + data.FieldPath);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4;

            operandField = new DoubleField(OperandLabel());
            operandField.value = operand;
            operandField.RegisterValueChangedCallback(evt =>
            {
                operand = evt.newValue;
                UpdatePreview();
            });
            root.Add(operandField);

            previewLabel = NeuroUiUtils.AddLabel(root, "");
            previewLabel.style.whiteSpace = WhiteSpace.Normal;
            previewLabel.style.marginTop = 4;
            previewLabel.style.marginBottom = 4;

            var buttons = NeuroUiUtils.AddHorizontal(root);
            buttons.style.justifyContent = Justify.FlexEnd;
            NeuroUiUtils.AddButton(buttons, "Cancel", () => editorWindow.Close());
            applyButton = NeuroUiUtils.AddButton(buttons, "Apply", OnApply);

            UpdatePreview();
            operandField.schedule.Execute(() => operandField.Focus());
        }

        string OperationName()
        {
            switch (operation)
            {
                case NeuroBulkFieldEdit.Operation.Multiply: return "Multiply all";
                case NeuroBulkFieldEdit.Operation.Add: return "Add to all";
                default: return "Set all";
            }
        }

        string OperandLabel()
        {
            switch (operation)
            {
                case NeuroBulkFieldEdit.Operation.Multiply: return "×";
                case NeuroBulkFieldEdit.Operation.Add: return "+";
                default: return "=";
            }
        }

        void UpdatePreview()
        {
            var typeName = context.RootType.Name;
            var text = fieldCount == 0
                ? $"Nothing to change - no {typeName} has this field."
                : $"{fieldCount} field(s) in {itemCount} of {typeName}.";
            if (fieldCount > itemCount)
            {
                // The path runs through a list, so it is every element of it, not one per item.
                text += $"\nEvery element of {data.FieldPath}.";
            }
            if (NeuroBulkFieldEdit.TryGetNumber(data.type, data.GetValue(), out var current))
            {
                var after = NeuroBulkFieldEdit.ToValue(data.type,
                    NeuroBulkFieldEdit.Calculate(current, operation, operand));
                NeuroBulkFieldEdit.TryGetNumber(data.type, after, out var afterNumber);
                text += $"\nThis one: {current:0.####} → {afterNumber:0.####}";
            }
            previewLabel.text = text;
            applyButton?.SetEnabled(fieldCount > 0);
        }

        void OnApply()
        {
            var window = context.GetWindow?.Invoke();
            NeuroBulkFieldEdit.Result result;
            try
            {
                result = NeuroBulkFieldEdit.Apply(context.DataProvider, context.RootType, data.FieldPath,
                    operation, operand, dryRun: false, window);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Could not apply", e.Message, "OK");
                return;
            }
            finally
            {
                editorWindow.Close();
            }
            Debug.Log($"Neuro ~ {data.FieldPath} {NeuroBulkFieldEdit.DescribeOperation(operation, operand)}" +
                      $" on {result.Fields} field(s) across {result.ItemCount} {context.RootType.Name} item(s).");
        }
    }
}
