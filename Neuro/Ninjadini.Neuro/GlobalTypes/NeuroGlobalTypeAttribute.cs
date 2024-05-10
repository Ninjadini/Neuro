using System;

namespace Ninjadini.Neuro
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface | AttributeTargets.Assembly)]
    public class NeuroGlobalTypeAttribute : Attribute
    {
        public uint Id;

        public NeuroGlobalTypeAttribute(uint id)
        {
            Id = id;
        }
    }
}