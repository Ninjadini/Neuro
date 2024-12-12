using System;
using Ninjadini.Neuro.Utils;

namespace Ninjadini.Neuro
{
    public interface INeuroContentValidator : IAssemblyTypeScannable
    {
        bool ShouldTest(object valueToTest, Type type);
        
        void Test(object valueToTest, NeuroContentValidatorContext context);
    }
    
    public interface INeuroContentValidator<in T> : INeuroContentValidator
    {
        bool INeuroContentValidator.ShouldTest(object valueToTest, Type type)
        {
            return valueToTest is T;
        }
        
        void INeuroContentValidator.Test(object valueToTest, NeuroContentValidatorContext context)
        {
            Test((T)valueToTest, context);
        }
        
        void Test(T valueToTest, NeuroContentValidatorContext context);
    }
}