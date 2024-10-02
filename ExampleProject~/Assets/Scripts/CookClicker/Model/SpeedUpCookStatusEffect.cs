using Ninjadini.Neuro;
using Ninjadini.Neuro.Utils;
using UnityEngine;

[Neuro(2)]
[Tooltip("Shorten the time required to cook the next X items")]
public class SpeedUpCookStatusEffect : CookStatusEffect
{
    [Range(0.1f, 10f)]
    [Neuro(1)] public float SpeedMultiplier;
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