using System;

namespace Ninjadini.Neuro
{
    [System.AttributeUsage(AttributeTargets.Assembly)]
    public class NeuroAssemblyAttribute : Attribute
    {
        public readonly Type RegistryType;
        public readonly string RegistryMethodName;

        public NeuroAssemblyAttribute(Type registryType, string registryMethodName)
        {
            RegistryType = registryType;
            RegistryMethodName = registryMethodName;
        }
    }
}