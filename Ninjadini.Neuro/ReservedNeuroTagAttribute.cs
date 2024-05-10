using System;

namespace Ninjadini.Neuro
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true)]
    public class ReservedNeuroTagAttribute : Attribute
    {
        public uint Tag;

        public ReservedNeuroTagAttribute(uint tag)
        {
            Tag = tag;
        }
    }
}