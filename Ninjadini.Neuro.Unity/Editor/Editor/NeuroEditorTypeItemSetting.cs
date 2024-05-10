using System;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro.Editor
{
    [Serializable]
    public class NeuroEditorTypeItemSetting
    {
        public NeuroEditorGlobalTypeRef Type;
        public string DropDownName;
        public bool BakeToResources;
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