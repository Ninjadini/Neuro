using System;
using Ninjadini.Neuro.Sync;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    [Serializable]
    public class NeuroEditorTypeItemSetting
    {
        public NeuroEditorGlobalTypeRef Type;
        
        [Tooltip("Customise the name used in type dropdown.\nIn case you want to categorize things like Economy > **, Combat > **, LiveEvents > **\nNOTE: For now, you need to click on the dropdown to see the change")]
        public string DropDownName;
        
        [Tooltip("You can choose to not bake this particular type for builds - perhaps maybe because it is for debug / editor use only.\nNOTE: If you have turned off `BakeDataResourcesForBuild` in Neuro editor settings, it will not be baked even if you have it ticked here.")]
        public bool BakeToResources;
        
        [Tooltip("You can specifically store the JSON data files for this type in a different place from the path set in `PrimaryDataPath` in Neuro editor settings.\n\nThis could be useful for something like localization where you point to specific language version of texts OR maybe prototyping where you point to specific balancing set")]
        public string DataPath;

        public void SetDefaults(Type type)
        {
            Type = new NeuroEditorGlobalTypeRef(){ TypeId = NeuroGlobalTypes.GetTypeIdOrThrow(type, out _) };
            BakeToResources = true;
        }

        public bool IsDefaultValues()
        {
            return string.IsNullOrEmpty(DropDownName) && string.IsNullOrEmpty(DataPath) && BakeToResources;
        }
        
        public void CopyFrom(NeuroEditorTypeItemSetting other)
        {
            DropDownName = other.DropDownName;
            BakeToResources = other.BakeToResources;
            DataPath = other.DataPath;
        }
    }
}