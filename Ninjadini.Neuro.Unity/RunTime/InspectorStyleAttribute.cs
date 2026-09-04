using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Adjusts how a field is laid out in the Neuro Editor. Editor only, no effect on serialisation.
    /// </summary>
    /// <example>
    /// <code>
    /// [InspectorStyle(spaceBefore: 10, spaceAfter: 4)]
    /// [Neuro(1)] public string Name;
    ///
    /// [InspectorStyle(horizontal: 100)] [Neuro(2)] public int Min;
    /// [InspectorStyle(horizontal: 100)] [Neuro(3)] public int Max;
    /// </code>
    /// </example>
    public class InspectorStyleAttribute : Attribute
    {
        /// <summary>Extra pixels above the field.</summary>
        public uint SpaceBefore;

        /// <summary>Extra pixels below the field.</summary>
        public uint SpaceAfter;

        /// <summary>
        /// The field's width in pixels. Neighbouring fields that also set this share one row - the row ends
        /// at the first field without it, or at the next [Header]. 0 means normal full width layout.
        /// </summary>
        public uint Horizontal;

        public InspectorStyleAttribute(uint spaceBefore = 0,uint spaceAfter = 0, uint horizontal = 0)
        {
            SpaceBefore = spaceBefore;
            SpaceAfter = spaceAfter;
            Horizontal = horizontal;
        }
    }
}
