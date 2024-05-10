using System;

namespace Ninjadini.Neuro
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AssetTypeAttribute : Attribute
    {
        public Type Type;
        public string TypeString;
        
        public AssetTypeAttribute(Type type)
        {
            Type = type;
        }
        
        public AssetTypeAttribute(string typeStr)
        {
            TypeString = typeStr;
        }
    }
}