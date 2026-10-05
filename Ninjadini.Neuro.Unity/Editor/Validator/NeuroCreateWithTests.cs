using NUnit.Framework;

namespace Ninjadini.Neuro.Editor
{
    /// Every [NeuroCreateWith] in the project names a method the editor can call. At creation time a broken one
    /// only logs and falls back to an empty object, which is easy to miss.
    public class NeuroCreateWithTests
    {
        [Test]
        public void AllCreateWithMethodsResolve()
        {
            var problems = NeuroCreateWith.FindProblems();
            if (problems.Count > 0)
            {
                Assert.Fail(string.Join("\n", problems));
            }
        }
    }
}
