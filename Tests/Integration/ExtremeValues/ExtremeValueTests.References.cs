using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
// ---------------------------------------------------------------------------------------------------- Reference<>

        [NeuroGlobalType(901)]
        public partial class ExtremeReferencable : Referencable
        {
            [Neuro(1)] public string Name;
        }

        public partial class ReferenceBox
        {
            [Neuro(1)] public Reference<ExtremeReferencable> Value;
        }

        static readonly uint[] ReferenceIds = { 0u, 1u, 127u, 128u, int.MaxValue, 2147483648u, uint.MaxValue };

        [TestCaseSource(nameof(ReferenceIds))]
        public void Reference_Binary(uint id) => Assert.That(Bin(new ReferenceBox { Value = id }).Value.RefId, Is.EqualTo(id));

        [TestCaseSource(nameof(ReferenceIds))]
        public void Reference_Json(uint id) => Assert.That(Jsn(new ReferenceBox { Value = id }).Value.RefId, Is.EqualTo(id));


// ---------------------------------------------------------------------------------------------------- references in collections

        public partial class RefCollectionBox
        {
            [Neuro(1)] public List<Reference<ExtremeReferencable>> List;
            [Neuro(2)] public Dictionary<Reference<ExtremeReferencable>, string> Keys;
        }

        static RefCollectionBox RefCollection() => new RefCollectionBox
        {
            List = new List<Reference<ExtremeReferencable>> { 0u, 1u, uint.MaxValue },
            Keys = new Dictionary<Reference<ExtremeReferencable>, string>
            {
                { (Reference<ExtremeReferencable>)0u, "zero" },
                { (Reference<ExtremeReferencable>)1u, "one" },
                { (Reference<ExtremeReferencable>)uint.MaxValue, "max" }
            }
        };

        static void AssertRefCollection(RefCollectionBox a, RefCollectionBox b)
        {
            Assert.That(b.List, Is.EqualTo(a.List).AsCollection);
            Assert.That(b.Keys, Is.EquivalentTo(a.Keys));
        }

        [Test]
        public void RefCollection_Binary() => AssertRefCollection(RefCollection(), Bin(RefCollection()));

        [Test]
        public void RefCollection_Json() => AssertRefCollection(RefCollection(), Jsn(RefCollection()));
    }
}
