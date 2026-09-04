using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Declares that this referencable type is a companion table of another one - the two share the same
    /// RefId, so one logical item is split across two tables.
    /// </summary>
    /// <remarks>
    /// Useful when part of an item belongs somewhere else: different file, different owner, or data you want
    /// to strip from builds.
    /// <para>
    /// In the Neuro Editor each item gets a link bar to jump to its counterpart, creating it on the other
    /// side if it doesn't exist yet. Unless <see cref="Optional"/> is set, the type carrying this attribute
    /// can not be added directly - you create the item on the <see cref="To"/> side, which is what mints
    /// the RefId.
    /// </para>
    /// <para>
    /// Repeat the attribute for more than one link. Both types must be root referencables, not singletons.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [NeuroGlobalType(2)]
    /// [LinkedReference(typeof(Troop))]  // TroopArt items are added from Troop, sharing its RefId
    /// public class TroopArt : Referencable
    /// {
    ///     [Neuro(1)] public AssetAddress Icon;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public class LinkedReferenceAttribute : Attribute
    {
        /// <summary>The type this one is linked to, and the side new items are created from.</summary>
        public readonly Type To;

        /// <summary>
        /// Label shown for this type in the editor's link bar, when viewing a <see cref="To"/> item.
        /// Defaults to this type's name.
        /// </summary>
        public readonly string FromName;

        /// <summary>
        /// Label shown for the <see cref="To"/> type in the editor's link bar, when viewing an item of this
        /// type. Defaults to the <see cref="To"/> type's name.
        /// </summary>
        public readonly string ToName;

        /// <summary>
        /// When true, items of this type can also be added on their own.
        /// When false (default), they can only be created from the <see cref="To"/> side.
        /// </summary>
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
