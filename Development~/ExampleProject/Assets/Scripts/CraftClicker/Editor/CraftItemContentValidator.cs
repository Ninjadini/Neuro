using Ninjadini.Neuro;
using NUnit.Framework;

public class CraftItemContentValidator : INeuroContentValidator<CraftItem>
{
    public void Test(CraftItem valueToTest, NeuroContentValidatorContext context)
    {
        Assert.IsNotEmpty(valueToTest.Name, "Name must not be empty");
        Assert.Greater(valueToTest.CraftOutputCount, 0, "Must have at least 1 output count");
    }
}