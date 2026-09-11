using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Narrows which items the Neuro Editor and the Unity inspector offer in a <see cref="Reference{T}"/>
    /// field's dropdown. Subclass it and decide per item in <see cref="Include"/>; the editor reads the
    /// attribute off the field and calls it for every candidate whenever the dropdown opens.
    /// </summary>
    /// <remarks>
    /// The dropdown never restricts what the data can hold, so a value that fails the filter (hand edited
    /// JSON, or an item whose category changed later) still shows as the current selection, marked with
    /// "(filtered out)", and can still be reselected or cleared. Such a value is reported as a content
    /// validation problem instead, in the editor's Tests section and by <c>NeuroContentTestsRunner</c>;
    /// set <see cref="Validate"/> to false for a filter that is only a convenience.
    /// <para>The attribute applies to the field it sits on. It is not inherited by references nested in a
    /// struct or list element under that field.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ArmourOnlyAttribute : NeuroReferenceFilterAttribute
    /// {
    ///     public override bool Include(IReferencable item, NeuroReferences references)
    ///         => item is Item i &amp;&amp; i.Slot == ItemSlot.Armour;
    /// }
    ///
    /// [ArmourOnly]
    /// [Neuro(1)] public Reference&lt;Item&gt; Chest;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class NeuroReferenceFilterAttribute : Attribute
    {
        /// <summary>
        /// When true (the default) a stored value that <see cref="Include"/> rejects is a content validation
        /// problem. Set false to only narrow the dropdown and accept any stored value.
        /// </summary>
        public bool Validate { get; set; } = true;

        /// <summary>Return true to list <paramref name="item"/> in the dropdown.</summary>
        public abstract bool Include(IReferencable item, NeuroReferences references);
    }
}
