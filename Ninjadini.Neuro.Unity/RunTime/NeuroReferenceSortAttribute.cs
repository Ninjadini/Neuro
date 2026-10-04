using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Adds a sort button to a reference dropdown, after the built-in "Id" and "Name". The choice is remembered
    /// per referencable type for the rest of the editor session, and also orders the Neuro Editor's ← / → buttons.
    /// </summary>
    /// <remarks>
    /// Placed and found the same way as <see cref="NeuroReferenceFilterButtonsAttribute"/>: on the referencable
    /// class it applies wherever that type is listed and <see cref="AppliesTo"/> is never called; on a field it
    /// adds a sort for references under that field only, after the class's own.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class SortByPriceAttribute : NeuroReferenceSortAttribute
    /// {
    ///     public override string Name => "Price";
    ///     public override int Compare(IReferencable a, IReferencable b, NeuroReferences references)
    ///         => ((Item)a).Price.CompareTo(((Item)b).Price);
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public abstract class NeuroReferenceSortAttribute : Attribute
    {
        /// <summary>The button label. Default: the attribute's class name without "Attribute".</summary>
        public virtual string Name
        {
            get
            {
                var name = GetType().Name;
                const string suffix = "Attribute";
                return name.EndsWith(suffix) && name.Length > suffix.Length ? name.Substring(0, name.Length - suffix.Length) : name;
            }
        }

        /// <summary>Orders two items; ties fall back to ref id.</summary>
        public abstract int Compare(IReferencable a, IReferencable b, NeuroReferences references);

        /// <summary>
        /// Only consulted on a field: whether this sort concerns references to <paramref name="referencableType"/>.
        /// Default: applies to all.
        /// </summary>
        public virtual bool AppliesTo(Type referencableType) => true;
    }
}
