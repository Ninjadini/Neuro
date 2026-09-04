using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Points at the generated registration method for an assembly, so Neuro can find and call it without
    /// scanning every type. Written by the source generator - you should never need to add it yourself.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Assembly)]
    public class NeuroAssemblyAttribute : Attribute
    {
        /// <summary>The generated NeuroTypesRegister type.</summary>
        public readonly Type RegistryType;

        /// <summary>Name of the static method on <see cref="RegistryType"/> that registers the types.</summary>
        public readonly string RegistryMethodName;

        public NeuroAssemblyAttribute(Type registryType, string registryMethodName)
        {
            RegistryType = registryType;
            RegistryMethodName = registryMethodName;
        }
    }
}
