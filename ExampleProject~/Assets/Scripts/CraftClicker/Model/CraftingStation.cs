using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(13)]
[DisplayName("CraftClicker: Stations []")]
public class CraftingStation : Referencable
{
    [Neuro(1)] public string Name;
    
    [AssetType(typeof(Texture2D))]
    [Neuro(3)] public AssetAddress Icon;
    
    [Neuro(2)] public List<Reference<CraftItem>> CraftItems = new List<Reference<CraftItem>>();
    
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
}