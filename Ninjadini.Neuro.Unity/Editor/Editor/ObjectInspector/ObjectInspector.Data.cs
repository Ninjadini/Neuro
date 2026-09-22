using System;
using System.Reflection;

namespace Ninjadini.Neuro.Editor
{
    public partial class ObjectInspector
    {
        public struct Data
        {
            public string name;
            public Type type;
            public Func<object> getter;
            public Action<object> setter;
            public IController Controller;
            public MemberInfo MemberInfo;
            public string path;
            /// Where this field sits in the root object, as a chain that can be followed on any other object of
            /// the same type - which is what lets a field be edited across a whole table. Null where it can not
            /// be followed (inside a dictionary), and null when the root was drawn without one.
            /// Not the same as <see cref="path"/>, which is a display key for remembering open foldouts.
            public NeuroFieldPath FieldPath;
            /// The nearest enclosing member's NeuroReferenceFilterAttributes, carried down to nested
            /// references (list elements, struct fields). Null when nothing above declared one.
            public NeuroReferenceFilterAttribute[] InheritedFilters;

            public object GetValue()
            {
                return getter?.Invoke();
            }

            public void SetValue(object value)
            {
                setter?.Invoke(value);
                Controller?.OnValueChanged(value);
            }

            public string GetDisplayName() => Controller?.GetDisplayName(this) ?? name;
        }
    }
}