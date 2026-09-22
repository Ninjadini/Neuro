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
        Type rootType;
        Label label;
        NeuroReferencablesDropdownField dropdown;
        Button createBtn;
        bool showCreateBtn = true;

        /// <summary>
        /// Whether the '+' button that creates, assigns and goes to a new item is offered while the field is null.
        /// Turn it off when the drawer wrapping this field has its own way of creating one.
        /// </summary>
        public bool ShowCreateBtn
        {
            get => showCreateBtn;
            set
            {
                showCreateBtn = value;
                UpdateCreateBtn();
            }
        }

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
            rootType = NeuroReferences.GetRootReferencable(elementType);
            dropdown.SetFilters(data.InheritedFilters ?? NeuroReferenceFilters.GetOwn(data.MemberInfo), rootType);
            dropdown.SetValue(rootType, GetRefId());
            dropdown.RegisterValueChangedCallback(OnDropDownChanged);

            Add(dropdown);
            
            AddCreateBtn();
            AddGoToReferenceBtn();
            schedule.Execute(OnUpdate).Every(ObjectInspectorFields.RefreshRate);
        }

        void AddCreateBtn()
        {
            if (typeof(ISingletonReferencable).IsAssignableFrom(elementType))
            {
                return;
            }
            createBtn = new Button(OnCreateBtnClicked)
            {
                text = "+",
                tooltip = $"Create a new {elementType.Name}, assign it here and go to it"
            };
            dropdown.Add(createBtn);
            UpdateCreateBtn();
        }

        void UpdateCreateBtn()
        {
            if (createBtn != null)
            {
                createBtn.style.display = showCreateBtn && GetRefId() == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void OnCreateBtnClicked()
        {
            var navElement = FindNavElement();
            navElement.CreateNewItem(elementType, createBtn, item =>
            {
                // assign before navigating - going to the new item redraws the editor this field lives in.
                data.SetValue(SetRefId(item.RefId));
                dropdown.SetValue(rootType, item.RefId, false);
                UpdateCreateBtn();
                navElement.SetSelectedItem(NeuroReferences.GetRootReferencable(item.Value.GetType()), item.RefId);
            });
        }

        void AddGoToReferenceBtn()
        {
            if (dropdown != null && !dropdown.HasGoToRefBtn())
            {
                dropdown.AddGoToReferenceBtn(delegate(Type type, uint u)
                {
                    FindNavElement().SetSelectedItem(type, u);
                });
            }
        }

        /// The Neuro editor this field is drawn in, or the Neuro editor window's when it is drawn elsewhere (a Unity inspector).
        NeuroEditorNavElement FindNavElement()
        {
            var p = parent;
            while (p != null)
            {
                if (p is NeuroEditorNavElement navElement)
                {
                    return navElement;
                }
                p = p.parent;
            }
            var window = EditorWindow.GetWindow<NeuroEditorWindow>();
            window.Show();
            return window.EditorElement;
        }

        void OnUpdate()
        {
            var newId = GetRefId();
            if (newId != dropdown.value && !NeuroUiUtils.IsFocused(dropdown))
            {
                dropdown.SetValueWithoutNotify(newId);
            }
            UpdateCreateBtn();
        }

        void OnDropDownChanged(ChangeEvent<uint> evt)
        {
            if (evt.currentTarget == evt.target)
            {
                var value = SetRefId(evt.newValue);
                data.SetValue(value);
                UpdateCreateBtn();
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