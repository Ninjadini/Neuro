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