using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(13)]
[DisplayName("CraftClicker: Stations []")]
public class CraftingStation : Referencable, INeuroRefDropDownIconCustomizable
{
    [Neuro(1)]
    public string Name;
    
    [Neuro(3)] [AssetType(typeof(Texture2D))]
    public AssetAddress Icon;
    
    [Neuro(2)] 
    public readonly List<Reference<CraftItem>> CraftItems = new ();
    
    class Validator : INeuroContentValidator<CraftingStation>
    {
        // This validator class will be auto picked up by neuro editor and run the validation in editor.
        public void Test(CraftingStation station, NeuroContentValidatorContext context)
        {
            if (station.CraftItems == null || station.CraftItems.Count == 0)
            {
                context.AddProblem("At least one craft item is required");
                // Alternatively, you can use Assert from Unity Test Framework it's not included in this project.
            }
        }
    }

    string INeuroRefDropDownCustomizable.GetRefDropdownText(NeuroReferences references)
    {
        // this is so that we don't need to assign the RefName in editor and it'll still show the name.
        return string.IsNullOrEmpty(RefName) ? $"{RefId} : {Name}" : null;
    }
    AssetAddress INeuroRefDropDownIconCustomizable.RefDropdownIcon => Icon;
}