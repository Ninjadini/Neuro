using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    /// What the table a field belongs to is, for the right click menu on that field. The editor window's item
    /// element sets one on its inspector; anything else drawing an ObjectInspector leaves it null and gets no
    /// bulk edit, which is what a read only view such as the content debugger wants.
    public class NeuroBulkFieldEditContext
    {
        public NeuroEditorDataProvider DataProvider;
        /// The referencable type whose table the edit sweeps - what the type dropdown is showing.
        public Type RootType;
        /// Where the undo entry belongs, so undoing brings the item back into view there. May be null.
        public Func<EditorWindow> GetWindow;
    }

    /// The `Multiply all…` / `Add all…` / `Set all…` items on a number field's right click menu.
    public static class NeuroBulkFieldEditMenu
    {
        /// A field only gets these when it is a number that can be found again on the other items: the root has
        /// to be a table this context knows, the field has to be writable, and the path has to be followable
        /// (nothing inside a dictionary is).
        public static bool CanOffer(NeuroBulkFieldEditContext context, ObjectInspector.Data data)
        {
            return context?.DataProvider != null
                   && context.RootType != null
                   && data.FieldPath != null
                   && data.FieldPath.Count > 0
                   && data.setter != null
                   && NeuroBulkFieldEdit.IsNumber(data.type)
                   && (data.Controller?.CanEdit(data.type, data.GetValue()) ?? true);
        }

        public static void Populate(NeuroBulkFieldEditContext context, ObjectInspector.Data data,
            ContextualMenuPopulateEvent evt)
        {
            if (!CanOffer(context, data))
            {
                return;
            }
            if (evt.menu.MenuItems().Count > 0)
            {
                evt.menu.AppendSeparator();
            }
            // currentTarget, not target: the field element the menu was hung on outlives the click, whereas
            // the target may be a text input inside it that a redraw throws away.
            var target = evt.currentTarget as VisualElement;
            AppendOperation(context, data, evt, target, NeuroBulkFieldEdit.Operation.Multiply, "Multiply all…");
            AppendOperation(context, data, evt, target, NeuroBulkFieldEdit.Operation.Add, "Add to all…");
            AppendOperation(context, data, evt, target, NeuroBulkFieldEdit.Operation.Set, "Set all…");
        }

        static void AppendOperation(NeuroBulkFieldEditContext context, ObjectInspector.Data data,
            ContextualMenuPopulateEvent evt, VisualElement target,
            NeuroBulkFieldEdit.Operation operation, string label)
        {
            evt.menu.AppendAction(label, _ => NeuroBulkFieldEditPopup.Show(context, data, operation, target));
        }
    }
}
