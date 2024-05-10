using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ninjadini.Neuro.Utils;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroContentTester
    {
        public IReadOnlyList<Type> ValidatorsVisited => visitor.ValidatorsVisited;
        public TimeSpan TimeTaken { get; private set; }
        
        readonly Visitor visitor;
        
        static IReadOnlyList<INeuroContentValidator> allValidators;
        
        public NeuroContentTester(NeuroContentValidatorContext context)
        {
            visitor = new Visitor(context);
        }

        public void Test(object obj)
        {
            var startTime = DateTime.Now;
            visitor.Visit(obj);
            TimeTaken = DateTime.Now - startTime;
        }
        
        public static IEnumerable<INeuroContentValidator> GetAllValidatorsFor(object obj)
        {
            var type = obj.GetType();
            foreach (var validator in GetAllValidators())
            {
                if(validator.ShouldTest(obj, type))
                {
                    yield return validator;
                }
            }
        }

        public static IEnumerable<INeuroContentValidator> GetAllValidators()
        {
            if (allValidators == null)
            {
                var result = NeuroEditorUtils.CreateFromScannableTypes<INeuroContentValidator>();
                allValidators = new ReadOnlyCollection<INeuroContentValidator>(result);
            }
            return allValidators;
        }

        class Visitor : NeuroVisitor, NeuroVisitor.IInterface
        {
            public readonly List<Type> ValidatorsVisited = new List<Type>();
            
            readonly NeuroContentValidatorContext context;
            readonly List<NeuroVisitor.StackItem> stack = new ();

            public Visitor(NeuroContentValidatorContext context)
            {
                this.context = context;
            }

            public void Visit(object obj)
            {
                ValidatorsVisited.Clear();
                stack.Clear();
                context._SetStack(stack);
                Visit(obj, this);
            }
            
            void IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex)
            {
                stack.Add(new StackItem
                    {
                        Object = obj,
                        Name = name,
                        ListIndex = listIndex
                    });
                var validators = GetAllValidatorsFor(obj);
                foreach (var validator in validators)
                {
                    if (!ValidatorsVisited.Contains(validator.GetType()))
                    {
                        ValidatorsVisited.Add(validator.GetType());
                    }
                    
                    try
                    {
                        validator.Test(obj, context);
                    }
                    /*
                    catch (AssertionException e)
                    {
                        context.AddProblem(e.ToString());
                    }
                    catch (UnityEngine.Assertions.AssertionException e)
                    {
                        context.AddProblem(e.ToString());
                    }*/
                    catch (Exception e)
                    {
                        context.AddProblem(e.ToString());
                    }
                }
            }

            void IInterface.EndVisit()
            {
                if (stack.Count > 0)
                {
                    stack.RemoveAt(context.Stack.Count - 1);
                }
            }

            void IInterface.VisitRef<T>(ref Reference<T> reference)
            {
            
            }
        }
    }
}