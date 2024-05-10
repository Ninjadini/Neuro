namespace Ninjadini.Neuro
{
    /**
     * Anything that implements this is auto picked up by Neuro code gen to be called at assembly's type registry time (NeuroTypesRegister).
     */
    public interface INeuroCustomTypesRegistryHook
    {
        void Register();
    }
}