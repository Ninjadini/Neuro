using System;
using System.Reflection;
using Ninjadini.Neuro.Utils;

namespace Ninjadini.Neuro.Editor
{
    /// This interface lets you control the general behaviour of the editor.
    /// For example, you could have custom attributes to further modify the behaviour of the editor.
    /// There should really be only one of implemented type per project.
    public interface ICustomNeuroObjectInspectorController : ObjectInspector.IController, IAssemblyTypeScannable
    {
        /// Only the class with the highest number will be used in Neuro editor.
        /// If you somehow have many things implementing this due to libs etc, use a higher number than everything else to override
        public int Priority => 0;
            
        bool ShouldDrawNonNeuroField(FieldInfo fieldInfo, object holderObject) => false;
        bool ShouldDrawNonNeuroProperty(PropertyInfo propertyInfo, object holderObject) => false;
    }
}