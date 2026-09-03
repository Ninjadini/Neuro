namespace Ninjadini.Neuro.CodeGen
{
    /// <summary>
    /// How much of an assembly the Neuro code gen is allowed to look at.
    /// </summary>
    public enum NeuroScanMode
    {
        /// Scan everything. A type takes part in Neuro if it, or any of its fields, is attributed.
        Full,
        
        /// NEURO_FAST_CODEGEN is on and this assembly opted in with [assembly:Neuro(0)].
        /// Only types carrying a class level [Neuro(#)] / [NeuroGlobalType(#)] take part.
        Fast,
        
        /// NEURO_FAST_CODEGEN is on and this assembly did not opt in, so there is nothing to do here.
        Skip
    }
}
