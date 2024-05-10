using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    internal class NeuroBuildProcessor : IPreprocessBuildWithReport
    {
        public const int PreprocessBuildCallbackOrder = -100;
        
        public int callbackOrder => PreprocessBuildCallbackOrder;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = NeuroUnityEditorSettings.Get();
            if (settings && settings.BakeAutoTypeRegistryForBuild)
            {
                Debug.Log($"Generating baked Neuro types register file @ {NeuroAutoTypesRegisterBaker.DefaultOutputFilePath}");
                NeuroAutoTypesRegisterBaker.BakeTypesRegisterScript();
            }
            if (settings && settings.BakeDataResourcesForBuild)
            {
                NeuroEditorDataProvider.Shared.SaveBundledBinaryToResources(report);
            }
        }
    }
}