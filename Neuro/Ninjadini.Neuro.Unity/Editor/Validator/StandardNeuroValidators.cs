using System;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Utils;
using UnityEditor;
using UnityEngine;

public class StandardNeuroValidators
{
    public class NeuroAssetAddressValidator : INeuroContentValidator<AssetAddress>
    {
        public bool Enabled = true;
        
        bool INeuroContentValidator.ShouldTest(object valueToTest, Type type)
        {
            return Enabled && type == typeof(AssetAddress);
        }
        
        public void Test(AssetAddress value, NeuroContentValidatorContext context)
        {
            if (!Enabled)
            {
                return;
            }
            var address = value.Address;
            if (string.IsNullOrEmpty(address))
            {
                return;
            }
            if (address.Length > 32)
            {
                address = address.Substring(0, 32);
            }
            if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(address)) && !Resources.Load(address))
            {
                context.AddProblem($"Asset with GUID {value.Address} does not exist");
            }
        }
    }
    
    public class NeuroReferenceValidator : INeuroContentValidator<INeuroReference>
    {
        public bool Enabled = true;

        bool INeuroContentValidator.ShouldTest(object valueToTest, Type type)
        {
            return Enabled && valueToTest is INeuroReference;
        }
        
        public void Test(INeuroReference value, NeuroContentValidatorContext context)
        {
            var refId = value.RefId;
            if (refId == 0)
            {
                return;
            }
            var type = value.RefType;
            if(type != null && context.References.GetTable(type).Get(refId) == null)
            {
                context.AddProblem($"Reference to {type.Name} with RefId #{value.RefId} does not exist");
            }
        }
    }
}