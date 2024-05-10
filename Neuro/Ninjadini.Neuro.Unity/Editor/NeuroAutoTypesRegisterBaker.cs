using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    public static class NeuroAutoTypesRegisterBaker
    {
        public const string DefaultOutputFilePath = "Assets/NeuroBakedAutoTypesRegister.cs";

        [MenuItem("Tools/Neuro/Bake AutoTypesRegister Script")]
        public static void BakeTypesRegisterScript()
        {
            CreateTypesRegisterScript(DefaultOutputFilePath);
        }

        public static void CreateTypesRegisterScript(string outputPath)
        {
            var str = new StringBuilder();

            str.AppendLine(@"
[UnityEngine.Scripting.Preserve]
static class NeuroBakedAutoTypesRegister
{");

            str.AppendLine($"    // Generated script from {nameof(NeuroAutoTypesRegisterBaker)}");
            str.AppendLine($"    // This file can be deleted or ignored from source management.");
            str.AppendLine($"    // It will be regenerated at build time if ProjectSettings > Neuro > BakeAutoTypeRegistryForBuild is enabled.");
            
            str.Append(@"
    [UnityEngine.Scripting.Preserve]
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void OnAfterAssembliesLoaded()
    {");
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Select(a => a.name).ToArray();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyAttribute = assembly.GetCustomAttribute<NeuroAssemblyAttribute>();
                if (assemblyAttribute?.RegistryType != null && !string.IsNullOrEmpty(assemblyAttribute.RegistryMethodName) && playerAssemblies.Contains(assembly.GetName().Name))
                {
                    str.Append($"\n        {assemblyAttribute.RegistryType.FullName}.{assemblyAttribute.RegistryMethodName}();");
                }
            }
            str.AppendLine(@"
        if (!UnityEngine.Application.isEditor)
        {");
            str.Append($"            {typeof(NeuroSyncTypes).FullName}.{nameof(NeuroSyncTypes.DisableAssembliesScanning)}();");
            str.AppendLine(@"
        }
    }
}");
            File.WriteAllText(outputPath, str.ToString());
            Debug.Log($"{nameof(NeuroAutoTypesRegisterBaker)}: Baked script to: {outputPath}");
            AssetDatabase.Refresh();
        }
    }
}