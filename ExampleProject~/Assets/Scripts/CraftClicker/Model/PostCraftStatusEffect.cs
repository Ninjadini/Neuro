using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

// TODO 

[NeuroGlobalType(12)]
[DisplayName("CraftClicker: Status Effects")]
public abstract class PostCraftStatusEffect : Referencable
{
    [Header("Lifespan of this status effect")]
    
    [Tooltip("Optional: its active for the next N items crafted, then its removed")]
    [Neuro(1)] public int ForNextCount;
    [Tooltip("Optional: its active for the duration, then its removed")]
    [Neuro(2)] public TimeSpan ForDuration;
}

[Neuro(2)]
[Tooltip("Modifies the required items to craft a specific item. for example a chance to start producing things for cheaper...")]
public class ModifyRequiredCraftStatusEffect : PostCraftStatusEffect
{
    [Neuro(1)] public Reference<CraftItem> ForItem;
    [Tooltip("New required items to produce this item")]
    [Neuro(2)] public List<CraftRecipeRequiredItem> RequiredItems;
}


[Neuro(3)]
[Tooltip("Multiply the number of items produced")]
public class MultiplyCraftOutputStatusEffect : PostCraftStatusEffect
{
    [Neuro(1)] public float Multiplier;

    class Validator : INeuroContentValidator<MultiplyCraftOutputStatusEffect>
    {
        // This validator class will be auto picked up by neuro editor and run the validation in editor.
        
        public void Test(MultiplyCraftOutputStatusEffect value, NeuroContentValidatorContext context)
        {
            if (value.Multiplier <= 0)
            {
                context.AddProblem("Multiplier must not be zero or less");
                // Alternatively you can use Assert from Unity Test Framework it's not included in this project.
            }
        }
    }
}