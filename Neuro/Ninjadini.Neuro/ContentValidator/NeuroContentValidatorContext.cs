using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninjadini.Neuro.Utils
{
    public class NeuroContentValidatorContext
    {
        public readonly NeuroReferences References;
        readonly Action<string> ProblemCallback;

        /// This is set to true if its ran from Neuro Editor - where it checks for tests on every data change.
        /// Meaning we want to only do light tests for the UI to be responsive.
        public bool SkipHeavyTests;
        
        public string TesterName;
        public object TesterSource;
        public object UserData;

        public IReadOnlyList<NeuroVisitor.StackItem> Stack { get; private set; }
        
        public NeuroContentValidatorContext(NeuroReferences references, Action<string> addProblemCallback)
        {
            References = references ?? throw new ArgumentNullException(nameof(references));
            ProblemCallback = addProblemCallback ?? throw new ArgumentNullException(nameof(addProblemCallback));
        }
        
        public virtual void AddProblem(string message)
        {
            if (Stack.Count > 1)
            {
                var path = NeuroVisitor.GeneratePathFromStack(Stack.Skip(1));
                message = $"{path}: {message}";
            }
            ProblemCallback(message);
        }
        
        public virtual void AddProblemWithoutPath(string message)
        {
            ProblemCallback(message);
        }
        
        public NeuroVisitor.StackItem? GetParentInStack(int depth)
        {
            if (depth < Stack.Count)
            {
                return Stack[Stack.Count - 1 - depth];
            }
            return null;
        }

        public void _SetStack(IReadOnlyList<NeuroVisitor.StackItem> stack)
        {
            Stack = stack;
        }
    }
}