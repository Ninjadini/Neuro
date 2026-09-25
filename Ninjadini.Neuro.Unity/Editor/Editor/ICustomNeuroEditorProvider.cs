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
        /// Drawn just above one field, under its [Header] if it has one - as well as the field, not instead of it.
        /// Match on data.MemberInfo; it is asked for every field of every object drawn, lists included.
        VisualElement CreateCustomFieldHeader(NeuroObjectInspector inspector, ObjectInspector.Data data) => null;
        VisualElement CreateCustomDrawer(NeuroObjectInspector inspector, ObjectInspector.Data data) => null;

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