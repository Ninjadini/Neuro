using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Gives a type a project wide id. Required on every root <see cref="IReferencable"/> type, and on
    /// anything you read or write with WriteGlobalTyped / ReadGlobalTyped.
    /// </summary>
    /// <remarks>
    /// The id is unique across the whole project, not per assembly, and it goes into the data - so treat it
    /// like a <see cref="NeuroAttribute"/> tag and never reuse the id of a deleted type.
    /// Put it on the root of a hierarchy only; subclasses are identified by their own <c>[Neuro(#)]</c>.
    /// <para>
    /// To find a free id, write 0 and read the compile error, or check the tag map at the top of the
    /// generated NeuroTypesRegister file. In Unity, Tools > Neuro > Type Mapping Debugger lists every
    /// assembly at once.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [NeuroGlobalType(1)]
    /// public class Troop : Referencable
    /// {
    ///     [Neuro(1)] public string DisplayName;
    /// }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface | AttributeTargets.Assembly)]
    public class NeuroGlobalTypeAttribute : Attribute
    {
        /// <summary>Project wide unique id. Written into the data, so it can not change or be reused.</summary>
        public uint Id;

        public NeuroGlobalTypeAttribute(uint id)
        {
            Id = id;
        }
    }
}
