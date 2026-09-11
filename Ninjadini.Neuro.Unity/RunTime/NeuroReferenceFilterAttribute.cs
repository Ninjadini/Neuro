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
    /// <para>It also reaches down: put it on a list, struct or class field and every reference nested under
    /// that field picks it up - each element of a <c>List&lt;StatValue&gt;</c>, say - unless a member closer to
    /// the reference carries its own filter attribute, which then replaces it entirely (nearest wins, no
    /// merging). Override <see cref="AppliesTo"/> so a filter meant for one referencable type is ignored by
    /// references to other types that happen to share the container.</para>
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

        /// <summary>
        /// Whether this filter concerns references to <paramref name="referencableType"/> (the root type of the
        /// <c>Reference&lt;T&gt;</c>). Matters when the attribute sits on a container and is inherited by several
        /// kinds of reference beneath it; a filter that does not apply is simply skipped. Default: applies to all.
        /// </summary>
        public virtual bool AppliesTo(Type referencableType) => true;
    }
}
