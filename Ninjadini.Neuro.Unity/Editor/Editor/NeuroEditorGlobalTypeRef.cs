using System;
using System.Linq;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public struct NeuroEditorGlobalTypeRef
{
    [Neuro(1)] public uint TypeId;
    
    public Type GetNeuroType()
    {
        return NeuroGlobalTypes.FindTypeById(TypeId);
    }
    
    [CustomPropertyDrawer(typeof(NeuroEditorGlobalTypeRef), true)]
    class Editor : PropertyDrawer 
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var typeIdProp = property.FindPropertyRelative(nameof(TypeId));

            var value = typeIdProp.uintValue;

            var types = NeuroGlobalTypes.GetAllRootTypes().Where(t => typeof(IReferencable).IsAssignableFrom(t)).ToList();
            types.Insert(0, null);
            var options = types.Select(t => t?.Name ?? "-");
            var typeIds = types.Select(t => t != null ? (int)NeuroGlobalTypes.GetTypeIdOrThrow(t, out _) : 0).ToArray();

            var index = Array.IndexOf(typeIds, (int)value);
            index = EditorGUI.Popup(position, index, options.ToArray());

            if (index >= 0)
            {
                value = (uint)typeIds[index];
            }
            else
            {
                value = 0;
            }
            typeIdProp.uintValue = value;
        }
    }
}