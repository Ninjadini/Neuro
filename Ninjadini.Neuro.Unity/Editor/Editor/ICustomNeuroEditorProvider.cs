using System;
using Ninjadini.Neuro.Utils;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public interface ICustomNeuroEditorProvider : IAssemblyTypeScannable
    {
        public int Priority => 0;
        
        /// Drawn at the top of an object's fields, above them all - as well as the fields, not instead of them.
        VisualElement CreateCustomHeader(NeuroObjectInspector inspector, ObjectInspector.Data data, object value) => null;
        VisualElement CreateCustomDrawer(NeuroObjectInspector inspector, ObjectInspector.Data data) => null;

        /// Called with every field once it is made - list elements and custom drawers included - to add to it or
        /// wrap it: return the element to place, which may be the one passed in. Every provider gets a turn, in
        /// Priority order, each handed what the one before returned. Match on data.MemberInfo / data.type and
        /// hand anything else straight back; data.Owner is the object holding the field.
        /// Not called for a cell of a row (data.Inline), see DecorateInlineField.
        VisualElement DecorateField(NeuroObjectInspector inspector, ObjectInspector.Data data, VisualElement element) => element;

        /// DecorateField for one cell of a row - an inline type's fields, or a custom drawer's that set
        /// data.Inline. What comes back sits in the row, so grow it sideways rather than stacking things on it.
        VisualElement DecorateInlineField(NeuroObjectInspector inspector, ObjectInspector.Data data, VisualElement element) => element;

        public delegate void BindRefItemDelegate(VisualElement element, uint id);
        public delegate VisualElement MakeRefItemDelegate();

        bool GetReferenceDropdownDecoratorsFor(Type type,
            ref MakeRefItemDelegate makeItem,
            ref BindRefItemDelegate bindItem,
            ref float itemHeight,
            NeuroReferences references) => false;
        
        public delegate string FormatValueDelegate(uint id);
        
        bool GetReferenceValueDecoratorsFor(Type type,
            ref FormatValueDelegate formatValue,
            ref VisualElement makeOverlayElement,
            ref BindRefItemDelegate bindOverlayElement,
            ref float itemHeight,
            NeuroReferences references) => false;
    }
}