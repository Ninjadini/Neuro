using System;

namespace Ninjadini.Neuro
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Assembly)]
    public class NeuroAttribute : Attribute
    {
        public uint Tag;

        public NeuroAttribute(uint tag, NeuroOptions options = 0)
        {
            Tag = tag;
        }
    }

    [Flags]
    public enum NeuroOptions : byte
    {
        // TODO / future ideas
        NoBackCompatibility = 1 << 0, // no need to write keys, it'll just be a sequence of values.
        MergeNullAndEmpty = 1 << 1, // treat null and empty strings/lists the same.
        FixedBit = 1 << 2, // use fixed 32bit or 64bit
    }
}