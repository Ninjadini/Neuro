using System;
using Ninjadini.Neuro.Utils;
using Ninjadini.Toolkit;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public interface ICustomNeuroEditorProvider : IAssemblyTypeScannable
    {
        public int Priority => 0;
        
        VisualElement CreateCustomHeader(NeuroObjectInspector inspector, ObjectInspector.Data data, object value) => null;
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