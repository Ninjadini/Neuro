using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Marks a field to be serialised, or a class/interface/struct to take part in Neuro.
    /// </summary>
    /// <remarks>
    /// The tag - not the field name - is what gets written to binary, so it is the wire format:
    /// <list type="bullet">
    /// <item><description>Unique within the class. Subclasses have their own numbering.</description></item>
    /// <item><description>Never reuse the tag of a removed field - mark it with
    /// <see cref="ReservedNeuroTagAttribute"/> instead.</description></item>
    /// <item><description>Changing a field's type means changing its tag too, or old data reads back wrong.</description></item>
    /// </list>
    /// On a class or interface the tag identifies it among the subtypes of its base - unique across all
    /// subclasses of that base, not just the direct siblings. It is also how you opt a type in when it has
    /// no neuro fields of its own (0 is allowed for interfaces, classes need a non-zero number).
    /// <para>
    /// To find a free tag, write 0 and read the compile error - it lists the used tags and the next free one.
    /// </para>
    /// <para>
    /// On an assembly (<c>[assembly: Neuro]</c>) it declares that the assembly defines neuro types, which is
    /// required under the NEURO_FAST_CODEGEN define.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [NeuroGlobalType(1)]
    /// public class Troop : Referencable
    /// {
    ///     [Neuro(1)] public string DisplayName;
    ///     [Neuro(2)] public int Health;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Assembly)]
    public class NeuroAttribute : Attribute
    {
        /// <summary>The field tag, or the subtype id when used on a class or interface.</summary>
        public uint Tag;

        /// <param name="tag">Unique per class for fields, unique across the type hierarchy for subtypes.</param>
        /// <param name="options">Not implemented yet, see <see cref="NeuroOptions"/>.</param>
        public NeuroAttribute(uint tag = 0, NeuroOptions options = 0)
        {
            Tag = tag;
        }
    }

    /// <summary>Future ideas, none of these do anything yet.</summary>
    [Flags]
    public enum NeuroOptions : byte
    {
        // TODO / future ideas
        NoBackCompatibility = 1 << 0, // no need to write keys, it'll just be a sequence of values.
        MergeNullAndEmpty = 1 << 1, // treat null and empty strings/lists the same.
        FixedBit = 1 << 2, // use fixed 32bit or 64bit
    }
}
