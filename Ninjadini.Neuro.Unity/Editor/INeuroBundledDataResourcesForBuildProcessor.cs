using Ninjadini.Neuro.Utils;
using UnityEditor.Build.Reporting;

namespace Ninjadini.Neuro
{
    /// Extend this interface to run custom data processing before build
    /// E.g you want to strip all developer comments.
    public interface INeuroBundledDataResourcesForBuildProcessor : IAssemblyTypeScannable
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        /// Pre processing before build
        /// You may modify the data as you see fit.
        void PrepBeforeBuildProcessing(NeuroReferences neuroReferences, BuildReport? buildReport);
        
        /// Return true to include the referencable in the build
        /// You may also modify the data as you see fit.
        bool ProcessForInclusion(IReferencable referencable);
        
#pragma warning restore CS8632

    }
}