using Ninjadini.Neuro;
using Ninjadini.Neuro.Utils;
using UnityEngine;

[Neuro(3)]
[Tooltip("Multiply the number of items produced from recipes")]
public class MultiplyCookStatusEffect : CookStatusEffect
{
    [Neuro(1)] public float Multiplier;
    [Neuro(2)] public int NextCount;

    class Validator : INeuroContentValidator<SpeedUpCookStatusEffect>
    {
        // This validator class will be auto picked up by neuro editor and run the validation in editor.
        
        public void Test(SpeedUpCookStatusEffect effect, NeuroContentValidatorContext context)
        {
            if (effect.SpeedMultiplier <= 0)
            {
                context.AddProblem("Speed effect's SpeedMultiplier must not be zero or less");
                // Alternative you can use Assert from Unity Test Framework it's not included in this project.
            }
            if (effect.NextCount <= 0)
            {
                context.AddProblem("Speed effect's NextCount must be 1 or more");
                // Alternative you can use Assert from Unity Test Framework it's not included in this project.
            }
        }
    }
}