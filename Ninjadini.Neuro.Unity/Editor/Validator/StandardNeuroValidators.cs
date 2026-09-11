using System;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Utils;
using Ninjadini.Neuro.Editor;
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
            if(Resources.Load(address))
            {
                return;
            }
            if (address.Length > 32)
            {
                address = address.Substring(0, 32);
            }
            if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(address)))
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
                context.AddProblem($"Reference to {type.Name} with RefId #{NeuroEditorUtils.DisplayRefId(value.RefId)} does not exist");
            }
        }
    }

    /// Reports a reference whose value is rejected by a <see cref="NeuroReferenceFilterAttribute"/>, unless that
    /// attribute has Validate = false. Uses the same rule as the dropdown: the nearest member from the reference
    /// outwards that carries filter attributes decides, so an attribute on a list or struct field covers every
    /// reference nested beneath it.
    public class NeuroReferenceFilterValidator : INeuroContentValidator<INeuroReference>
    {
        public bool Enabled = true;

        bool INeuroContentValidator.ShouldTest(object valueToTest, Type type)
        {
            return Enabled && valueToTest is INeuroReference;
        }

        public void Test(INeuroReference value, NeuroContentValidatorContext context)
        {
            var refId = value.RefId;
            var type = value.RefType;
            if (refId == 0 || type == null)
            {
                return;
            }
            var filters = NeuroReferenceFilters.Applicable(NeuroReferenceFilters.FromStack(context.Stack), type);
            if (filters == null)
            {
                return;
            }
            IReferencable item = null;
            foreach (var attribute in filters)
            {
                if (!attribute.Validate)
                {
                    continue;
                }
                if (item == null)
                {
                    item = context.References.GetTable(type)?.Get(refId);
                    if (item == null)
                    {
                        return; // NeuroReferenceValidator reports the missing item
                    }
                }
                if (!attribute.Include(item, context.References))
                {
                    var attributeName = NeuroReferenceFilters.TrimAttributeSuffix(attribute.GetType().Name);
                    context.AddProblem($"{NeuroEditorUtils.DisplayIdAndName(item)} is not allowed here by [{attributeName}]");
                }
            }
        }
    }
}
