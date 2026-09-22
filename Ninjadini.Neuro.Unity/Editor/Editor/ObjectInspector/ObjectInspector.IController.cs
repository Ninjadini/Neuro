using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public partial class ObjectInspector
    {
        public interface IController
        {
            bool ShouldAddFoldOut(Data data, object value) => true;
            
            bool ShouldAutoExpandFoldout(Type type) => type.IsValueType;

            string GetDisplayName(Data data) => data.name;
            
            bool ShouldDrawField(FieldInfo fieldInfo, object holderObject) => true;
            bool ShouldDrawProperty(PropertyInfo propertyInfo, object holderObject) => false;
            
            bool CanEdit(Type type, object value) => true;
            bool CanSetToNull(Type type, object value) => true;
            bool CanCreateObject(Type type) => true;

            bool ShouldBeMultilineText(Data data)
            {
                var memberInfo = data.MemberInfo;
                if (memberInfo == null)
                {
                    return false;
                }
                return memberInfo.IsDefined(typeof(MultilineAttribute)) || memberInfo.IsDefined(typeof(TextAreaAttribute));
            }
            
            /// Custom header of class types
            VisualElement CreateCustomHeader(Data data, object value) => null;
            
            /// Custom header of given fields
            VisualElement CreateCustomFieldHeader(Data data) => null;
            
            VisualElement CreateCustomDrawer(Data data) => null;

            /// Whether this one field has anything of its own on a right click - a bulk edit across the table,
            /// say. Asked once per field as it is drawn: only a field that says yes gets a menu attached, so a
            /// field with nothing to offer keeps whatever menu it already had (a text box's cut / copy / paste)
            /// and does not sprout an empty one.
            bool HasFieldContextMenu(Data data) => false;

            /// Fills in that menu. The field's own items, where it has any, are already in it.
            void PopulateFieldContextMenu(Data data, ContextualMenuPopulateEvent evt) { }
            
            void ApplyStyle(Data data, VisualElement element) { }

            Type[] GetPossibleCreationTypesOf(Type type) => null;
            
            void CreateObject(Type type, VisualElement fromElement, Action<object> resultCallback)
            {
                resultCallback(null);
            }

            void SwitchObjectType(object originalObject, Type newType, ref object newObject){ }

            void OnValueChanged(object holderObject) {}

            NeuroEditorHistory History => null; // OK to be null
        }
        
        public class BasicController : IController
        {
            
        }
    }
}