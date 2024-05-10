using Ninjadini.Neuro.Utils;
using UnityEditor.Build.Reporting;

namespace Ninjadini.Neuro
{
    /// Extend this interface to run custom data processing before build
    /// E.g you want to strip all developer comments.
    public interface INeuroBundledDataResourcesForBuildProcessor : IAssemblyTypeScannable
    {
        /// Pre processing before build
        /// You may modify the data as you see fit.
        void PrepBeforeBuildProcessing(NeuroReferences neuroReferences, BuildReport? buildReport);
        
        /// Return true to include the referencable in the build
        /// You may also modify the data as you see fit.
        bool ProcessForInclusion(IReferencable referencable);
    }
}