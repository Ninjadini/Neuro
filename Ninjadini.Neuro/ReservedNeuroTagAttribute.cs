using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Tombstones a tag that used to be in use, so it is never handed out again.
    /// </summary>
    /// <remarks>
    /// Old data still holds the tag, so reusing it would silently read the old value into the new field.
    /// Reserved tags count as taken everywhere Neuro reports tag usage - the tag map at the top of the
    /// generated NeuroTypesRegister file, and the "next free" number in tag conflict errors.
    /// <para>
    /// Use it on a class for a field tag, or on the base type for a deleted subclass' tag.
    /// Repeatable.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [ReservedNeuroTag(1)]  // was MyOldValue
    /// [ReservedNeuroTag(2)]  // was MyOtherOldValue
    /// public class MyObject
    /// {
    ///     [Neuro(3)] public int MyValue;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true)]
    public class ReservedNeuroTagAttribute : Attribute
    {
        /// <summary>The tag that must not be used again.</summary>
        public uint Tag;

        public ReservedNeuroTagAttribute(uint tag)
        {
            Tag = tag;
        }
    }
}
