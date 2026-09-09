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
    ///
    /// [InspectorStyle(Inline = true)]
    /// public struct StatValue
    /// {
    ///     [Neuro(1)] public Reference&lt;Stat&gt; Stat;
    ///     [InspectorStyle(horizontal: 90)] [Neuro(2)] public long Value;
    /// }
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

        /// <summary>
        /// On a struct or class: draw its fields on one row in place of the usual foldout, so it reads as a
        /// single line - in a list, as a plain field, or as one of several inline fields in an object. The
        /// first field takes the row's name (the list index, or the field name); the rest have no label.
        /// A field with <see cref="Horizontal"/> gets that width, any other shares what is left.
        /// [Header] attributes inside an inline type are ignored. A null class value falls back to the
        /// foldout so it can still be created.
        /// </summary>
        public bool Inline;

        public InspectorStyleAttribute(uint spaceBefore = 0,uint spaceAfter = 0, uint horizontal = 0)
        {
            SpaceBefore = spaceBefore;
            SpaceAfter = spaceAfter;
            Horizontal = horizontal;
        }
    }
}
