using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    [CustomPropertyDrawer(typeof(Reference<>))]
    public class NeuroReferencePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = fieldInfo;
            var type = FindRefType();
            var refIdProp = property.FindPropertyRelative(NeuroConstants.Reference_RefId_FieldName);
            var refId = refIdProp.uintValue;
            var references = NeuroEditorDataProvider.Shared.References;
            var dropdown = new NeuroReferencablesDropdownField(references);
            dropdown.label = string.IsNullOrEmpty(preferredLabel) ? field.Name : preferredLabel;
            dropdown.IncludeNullOption = true;
            dropdown.SetFilters(NeuroReferenceFilters.FromSerializedProperty(property), type);
            dropdown.RegisterValueChangedCallback(delegate(ChangeEvent<uint> evt)
            {
                refIdProp.uintValue = evt.newValue;
                refIdProp.serializedObject.ApplyModifiedProperties();
            });
            dropdown.SetValue(type, refId, false);
            dropdown.AddGoToReferenceBtn(delegate(Type type, uint u)
            {
                var window = EditorWindow.GetWindow<NeuroEditorWindow>();
                window.Show();
                window.EditorElement.SetSelectedItem(type, u);
            });
            return dropdown;
        }

        Type FindRefType()
        {
            var field = fieldInfo;
            Type type = null;
            if (field != null)
            {
                if(field.FieldType.IsArray && field.FieldType.GetElementType().IsGenericType)
                {
                    type = field.FieldType.GetElementType().GetGenericArguments()[0];
                }
                else if(field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = field.FieldType.GetGenericArguments()[0].GetGenericArguments()[0];
                }
                else
                {
                    type = field.FieldType.GetGenericArguments()[0];
                }
            }
            return type;
        }

        List<string> guiNames = new List<string>();
        List<IReferencable> guiItems = new List<IReferencable>();

        static Func<IReferencable, bool> FilterFor(SerializedProperty property, Type refType, NeuroReferences references)
        {
            var filters = NeuroReferenceFilters.Applicable(NeuroReferenceFilters.FromSerializedProperty(property), refType);
            return NeuroReferenceFilters.ToPredicate(filters, references);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var type = FindRefType();
            if (type == null)
            {
                return;
            }
            var references = NeuroEditorDataProvider.Shared.References;
            var table = references?.GetTable(type);
            if (table == null)
            {
                return;
            }
            guiNames.Clear();
            guiItems.Clear();
            var refIdProp = property.FindPropertyRelative(NeuroConstants.Reference_RefId_FieldName);
            var refId = refIdProp.uintValue;
            
            guiNames.Add("0 : null");
            var prevIndex = 0;
            var filter = FilterFor(property, type, references);
            foreach (var referencable in table.SelectAll())
            {
                var isCurrent = refId == referencable.RefId;
                var passes = filter == null || filter(referencable);
                if (!passes && !isCurrent)
                {
                    continue;
                }
                guiItems.Add(referencable);
                var name = NeuroEditorUtils.DisplayRefId(referencable.RefId) + " : " + referencable.RefName;
                guiNames.Add(passes ? name : name + " (filtered out)");
                if (isCurrent)
                {
                    prevIndex = guiItems.Count;
                }
            }
            position.width -= 24;
            var newIndex = EditorGUI.Popup(position, label.text, prevIndex, guiNames.ToArray());
            if (newIndex != prevIndex)
            {
                var newId = 0u;
                if (newIndex > 0)
                {
                    newId = guiItems[newIndex - 1].RefId;
                }
                refIdProp.uintValue = newId;
            }

            position.x += position.width;
            position.width = 24;
            if (GUI.Button(position, ">"))
            {
                var window = EditorWindow.GetWindow<NeuroEditorWindow>();
                window.Show();
                var id = newIndex > 0 ? guiItems[newIndex - 1].RefId : 0u;
                window.EditorElement.SetSelectedItem(type, id);
            }
            guiNames.Clear();
            guiItems.Clear();
        }
    }
}