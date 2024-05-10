using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using Ninjadini.Neuro.Utils;
using NUnit.Framework;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroContentTestsRunner
    {
        [Test]
        [TestCaseSource(nameof(GetAllRefTableTestCases))]
        public void TestRefTables(Type refTableType)
        {
            var problems = new List<string>();

            var references = NeuroEditorDataProvider.Shared.References;

            string itemName = null;

            var context = new NeuroContentValidatorContext(references, (message) =>
            {
                problems.Add($"[{itemName}] {message}");
            })
            {
                SkipHeavyTests = false,
                TesterName = nameof(NeuroContentTestsRunner),
                TesterSource = this,
            };
            var table = references.GetTable(refTableType);
            if (table == null)
            {
                Assert.Fail("Failed to get table for " + refTableType.Name);
                return;
            }

            var tester = new NeuroContentTester(context);
            foreach (var referencable in table.SelectAll())
            {
                itemName = referencable.TryGetIdAndName();
                tester.Test(referencable);
            }
            if (problems.Count > 0)
            {
                Assert.Fail($"Problems found for {refTableType}:\n{string.Join("\n", problems)}");
            }
        }
        
        public static IEnumerable<TestCaseData> GetAllRefTableTestCases()
        {
            NeuroSyncTypes.TryRegisterAllAssemblies();
            foreach (var type in NeuroReferences.GetAllPossibleBaseTypes())
            {
                //if (NeuroContentTester.HasAnyValidatorsFor(type))
                {
                    yield return new TestCaseData(type).SetName(type.Name);
                }
            }
        }
    }
}