using Ninjadini.Neuro;
using UnityEngine;


[Neuro(3)]
//[Tooltip("Multiply the number of items produced")]
[Tooltip("WARNING: These are not hooked up to be functional yet")]
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