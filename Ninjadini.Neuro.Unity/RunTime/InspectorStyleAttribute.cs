using System;

namespace Ninjadini.Neuro
{
    public class InspectorStyleAttribute : Attribute
    {
        public uint SpaceBefore;
        public uint SpaceAfter;
        public uint Horizontal;
        
        public InspectorStyleAttribute(uint spaceBefore = 0,uint spaceAfter = 0, uint horizontal = 0)
        {
            SpaceBefore = spaceBefore;
            SpaceAfter = spaceAfter;
            Horizontal = horizontal;
        }
    }
}