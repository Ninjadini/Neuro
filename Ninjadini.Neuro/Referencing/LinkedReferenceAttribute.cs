using System;

namespace Ninjadini.Neuro
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LinkedReferenceAttribute : Attribute
    {
        public readonly Type To;
        public readonly string FromName;
        public readonly string ToName;
        public readonly bool Optional;

        public LinkedReferenceAttribute(Type to, string toName = null, string fromName = null, bool optional = false)
        {
            To = to;
            ToName = toName;
            FromName = fromName;
            Optional = optional;
        }
    }
}