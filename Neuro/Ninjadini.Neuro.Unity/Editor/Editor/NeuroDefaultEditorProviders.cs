using Ninjadini.Toolkit;
using UnityEditor;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    internal class NeuroDefaultEditorProviders : ICustomNeuroEditorProvider
    {
        public VisualElement CreateCustomDrawer(NeuroObjectInspector inspector, ObjectInspector.Data data)
        {
            if (data.type.IsGenericType && data.type.GetGenericTypeDefinition() == typeof(Reference<>))
            {
                return new NeuroReferenceFieldElement(data, inspector.References);
            }
            if (data.type == typeof(AssetAddress))
            {
                return new AssetAddressFieldElement(data);
            }
            return null;
        }
    }
}