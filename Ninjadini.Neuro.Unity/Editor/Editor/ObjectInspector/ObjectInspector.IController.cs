using System;
using System.Reflection;
using UnityEditor;
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
            
            /// Custom header of class types
            VisualElement CreateCustomHeader(Data data, object value) => null;
            
            /// Custom header of given fields
            VisualElement CreateCustomFieldHeader(Data data) => null;
            
            VisualElement CreateCustomDrawer(Data data) => null;

            Type[] GetPossibleCreationTypesOf(Type type) => null;
            
            void CreateObject(Type type, VisualElement fromElement, Action<object> resultCallback)
            {
                resultCallback(null);
            }

            void SwitchObjectType(object originalObject, Type newType, ref object newObject){ }

            void OnValueChanged(object holderObject) {}
        }
        
        public class BasicController : IController
        {
            
        }
    }
}