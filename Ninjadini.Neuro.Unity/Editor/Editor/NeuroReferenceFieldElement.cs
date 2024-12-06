using System;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroReferenceFieldElement : VisualElement
    {
        ObjectInspector.Data data;
        NeuroReferences references;
        Type elementType;
        Label label;
        NeuroReferencablesDropdownField dropdown;

        public static NeuroReferenceFieldElement CreateFromSerialisedProperty<T>(SerializedProperty property) where T : class, IReferencable
        {
            var refIdProp = property.FindPropertyRelative(NeuroConstants.Reference_RefId_FieldName);
            return new NeuroReferenceFieldElement(new ObjectInspector.Data()
            {
                name = property.name,
                type = typeof(Reference<T>),
                getter = () => new Reference<T>()
                {
                    RefId = refIdProp.uintValue
                },
                setter = (v) =>
                {
                    refIdProp.uintValue = ((Reference<T>)v).RefId;
                    property.serializedObject.ApplyModifiedProperties();
                },
            }, NeuroEditorDataProvider.Shared.References);
        }
        
        public NeuroReferenceFieldElement(ObjectInspector.Data data_, NeuroReferences references_)
        { 
            data = data_;
            references = references_;
            elementType = data.type.GenericTypeArguments[0];
            dropdown = new NeuroReferencablesDropdownField(references);
            dropdown.IncludeNullOption = true;
            dropdown.label = data.name;
            dropdown.SetValue(NeuroReferences.GetRootReferencable(elementType), GetRefId());
            dropdown.RegisterValueChangedCallback(OnDropDownChanged);

            Add(dropdown);
            
            AddGoToReferenceBtn();
            schedule.Execute(OnUpdate).Every(ObjectInspectorFields.RefreshRate);
        }

        void AddGoToReferenceBtn()
        {
            if (dropdown != null && !dropdown.HasGoToRefBtn())
            {
                dropdown.AddGoToReferenceBtn(delegate(Type type, uint u)
                {
                    var p = parent;
                    while (p != null)
                    {
                        if (p is NeuroEditorNavElement navElement)
                        { 
                            navElement.SetSelectedItem(type, u);
                            return;
                        }
                        p = p.parent;
                    }
                    var window = EditorWindow.GetWindow<NeuroEditorWindow>();
                    window.Show();
                    window.EditorElement.SetSelectedItem(type, u);
                });
            }
        }

        void OnUpdate()
        {
            var newId = GetRefId();
            if (newId != dropdown.value && !NeuroUiUtils.IsFocused(dropdown))
            {
                dropdown.SetValueWithoutNotify(newId);
            }
        }

        void OnDropDownChanged(ChangeEvent<uint> evt)
        {
            if (evt.currentTarget == evt.target)
            {
                var value = SetRefId(evt.newValue);
                data.SetValue(value);
            }
        }

        public uint GetRefId()
        {
            // see Reference<T>
            var refStruct = data.getter();
            return (uint)data.type.GetField(NeuroConstants.Reference_RefId_FieldName).GetValue(refStruct);
        }

        public T GetRef<T>() where T : class, IReferencable
        {
            return references.Get<T>(GetRefId());
        }

        object SetRefId(uint id)
        {
            // see Reference<T>
            var refStruct = data.getter();
            data.type.GetField(NeuroConstants.Reference_RefId_FieldName).SetValue(refStruct, id);
            return refStruct;
        }
    }
}