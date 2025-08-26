using System;

namespace Ninjadini.Neuro
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class FieldOffsetToNeuro : Attribute
    {
        public Type Type;
        public FieldOffsetToNeuro(Type type)
        {
            Type = type;
        }
    }
}