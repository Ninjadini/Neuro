using Ninjadini.Neuro;
using NUnit.Framework;

public class CraftingStationContentValidator : INeuroContentValidator<CraftingStation>
{
    public void Test(CraftingStation valueToTest, NeuroContentValidatorContext context)
    {
        Assert.IsNotEmpty(valueToTest.Name, "Name must not be empty");
        Assert.Greater(valueToTest.CraftItems.Count, 0, "Must have craft items");
    }
}