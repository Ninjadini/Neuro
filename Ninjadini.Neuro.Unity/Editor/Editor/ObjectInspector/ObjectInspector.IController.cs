using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace Ninjadini.Toolkit
{
    public partial class ObjectInspector
    {
        public interface IController
        {
            bool ShouldAutoExpand(Type type) => type.IsValueType;
            
            bool ShouldDrawField(FieldInfo fieldInfo, object holderObject) => true;
            bool ShouldDrawProperty(PropertyInfo propertyInfo, object holderObject) => false;
            
            bool CanEdit(Type type, object value) => true;
            bool CanSetToNull(Type type, object value) => true;
            bool CanCreateObject(Type type) => true;
            
            VisualElement Root => null;
            VisualElement CreateCustomHeader(Data data, object value) => null;
            VisualElement CreateCustomFieldHeader(Data data) => null;
            VisualElement CreateCustomDrawer(Data data) => null;

            Type[] GetPossibleCreationTypesOf(Type type) => null;
            void CreateObject(Type type, VisualElement fromElement, Action<object> resultCallback)
            {
                resultCallback(null);
            }

            object SwitchObjectType(object originalObject, Type newType) => null;

            void OnValueChanged(object holderObject) {}
        }
        
        public class BasicController : IController
        {
            
        }
    }
}