using System;
using System.Collections.Generic;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Adds filter buttons to the top of a reference dropdown, in the Neuro Editor's item picker and in every
    /// <see cref="Reference{T}"/> field. One button is active at a time, an "All" button comes first, and the
    /// choice is remembered per referencable type for the rest of the editor session.
    /// </summary>
    /// <remarks>
    /// Put it on the referencable class to offer the buttons wherever that type is listed - the editor finds it
    /// there by the type alone and never calls <see cref="AppliesTo"/>. Put it on a field, property, list or
    /// struct field to add buttons for references under that field only; they come after the class's own, and
    /// reach down the same way <see cref="NeuroReferenceFilterAttribute"/> does (nearest member wins), which is
    /// where <see cref="AppliesTo"/> is used to skip other kinds of reference sharing the container.
    /// <para>Buttons only narrow the list; they are never a content validation rule. They narrow within any
    /// <see cref="NeuroReferenceFilterAttribute"/>, and the current value stays listed whatever is picked.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ItemSlotButtonsAttribute : NeuroReferenceFilterButtonsAttribute
    /// {
    ///     public override IEnumerable&lt;NeuroReferenceFilterButton&gt; GetButtons(NeuroReferences references)
    ///     {
    ///         foreach (ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))
    ///             yield return new NeuroReferenceFilterButton(slot.ToString(), item => ((Item)item).Slot == slot);
    ///     }
    /// }
    ///
    /// [ItemSlotButtons]
    /// [NeuroGlobalType(5)] public class Item : Referencable { ... }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public abstract class NeuroReferenceFilterButtonsAttribute : Attribute
    {
        /// <summary>The buttons to show, in order. Called each time the dropdown opens.</summary>
        public abstract IEnumerable<NeuroReferenceFilterButton> GetButtons(NeuroReferences references);

        /// <summary>
        /// Only consulted on a field: whether these buttons concern references to <paramref name="referencableType"/>
        /// (the root type of the <c>Reference&lt;T&gt;</c>). Default: applies to all.
        /// </summary>
        public virtual bool AppliesTo(Type referencableType) => true;
    }

    /// <summary>One button of a <see cref="NeuroReferenceFilterButtonsAttribute"/>: its label and which items it shows.</summary>
    public readonly struct NeuroReferenceFilterButton
    {
        public readonly string Name;
        public readonly Func<IReferencable, bool> Include;

        public NeuroReferenceFilterButton(string name, Func<IReferencable, bool> include)
        {
            Name = name;
            Include = include;
        }
    }
}
