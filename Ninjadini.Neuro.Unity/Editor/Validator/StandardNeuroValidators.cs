using System;
using System.Reflection;
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

    /// Reports a reference whose value is rejected by a <see cref="NeuroReferenceFilterAttribute"/> on its
    /// field, unless that attribute has Validate = false. Only direct fields and properties are covered, the
    /// same scope the dropdown filter has: a reference inside a list element has no field of its own.
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
            var self = context.GetParentInStack(0);
            var parent = context.GetParentInStack(1);
            if (self == null || parent?.Object == null || self.Value.ListIndex.HasValue || string.IsNullOrEmpty(self.Value.Name))
            {
                return;
            }
            var member = FindMember(parent.Value.Object.GetType(), self.Value.Name);
            if (member == null)
            {
                return;
            }
            IReferencable item = null;
            var itemLoaded = false;
            foreach (var attribute in member.GetCustomAttributes<NeuroReferenceFilterAttribute>(true))
            {
                if (!attribute.Validate)
                {
                    continue;
                }
                if (!itemLoaded)
                {
                    itemLoaded = true;
                    item = context.References.GetTable(type)?.Get(refId);
                    if (item == null)
                    {
                        return; // NeuroReferenceValidator reports the missing item
                    }
                }
                if (!attribute.Include(item, context.References))
                {
                    var attributeName = attribute.GetType().Name;
                    if (attributeName.EndsWith("Attribute"))
                    {
                        attributeName = attributeName.Substring(0, attributeName.Length - "Attribute".Length);
                    }
                    context.AddProblem($"{NeuroEditorUtils.DisplayIdAndName(item)} is not allowed here by [{attributeName}]");
                }
            }
        }

        static MemberInfo FindMember(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
                var property = t.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null)
                {
                    return property;
                }
            }
            return null;
        }
    }
}
