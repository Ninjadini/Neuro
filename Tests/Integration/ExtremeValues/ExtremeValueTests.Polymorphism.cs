using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public partial class ExtremeValueTests
    {
// ---------------------------------------------------------------------------------------------------- polymorphism

        [Neuro(1), NeuroGlobalType(902)]
        public partial class PolyBase
        {
            [Neuro(1)] public int Id;
        }

        [Neuro(2)]
        public partial class PolySmallTag : PolyBase
        {
            [Neuro(1)] public string Value;
        }

        [Neuro(127)]
        public partial class PolyTag127 : PolyBase
        {
            [Neuro(1)] public string Value;
        }

        [Neuro(128)]
        public partial class PolyTag128 : PolyBase
        {
            [Neuro(1)] public string Value;
        }

        [Neuro(16384)]
        public partial class PolyBigTag : PolyBase
        {
            [Neuro(1)] public string Value;
        }

        [Neuro(200)]
        public partial class PolyNoFields : PolyBase
        {
        }

        public partial class PolyBox
        {
            [Neuro(1)] public PolyBase Value;
            [Neuro(2)] public List<PolyBase> List;
        }

        static IEnumerable<TestCaseData> PolyValues()
        {
            yield return new TestCaseData(new PolyBase { Id = int.MaxValue }).SetName("{m}_base");
            yield return new TestCaseData(new PolySmallTag { Id = 1, Value = "small" }).SetName("{m}_tag2");
            yield return new TestCaseData(new PolyTag127 { Id = 2, Value = "127" }).SetName("{m}_tag127");
            yield return new TestCaseData(new PolyTag128 { Id = 3, Value = "128" }).SetName("{m}_tag128");
            yield return new TestCaseData(new PolyBigTag { Id = 4, Value = "16384" }).SetName("{m}_tag16384");
            yield return new TestCaseData(new PolySmallTag { Id = 0, Value = null }).SetName("{m}_allDefaults");
        }

        static void AssertPoly(PolyBase expected, PolyBase actual)
        {
            Assert.IsNotNull(actual);
            Assert.That(actual.GetType(), Is.EqualTo(expected.GetType()), "sub type was not preserved");
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            if (expected is PolySmallTag a) Assert.That(((PolySmallTag)actual).Value, Is.EqualTo(a.Value));
            if (expected is PolyTag127 b) Assert.That(((PolyTag127)actual).Value, Is.EqualTo(b.Value));
            if (expected is PolyTag128 c) Assert.That(((PolyTag128)actual).Value, Is.EqualTo(c.Value));
            if (expected is PolyBigTag d) Assert.That(((PolyBigTag)actual).Value, Is.EqualTo(d.Value));
        }

        [TestCaseSource(nameof(PolyValues))]
        public void Poly_Binary(PolyBase v) => AssertPoly(v, Bin(new PolyBox { Value = v }).Value);

        [TestCaseSource(nameof(PolyValues))]
        public void Poly_Json(PolyBase v) => AssertPoly(v, Jsn(new PolyBox { Value = v }).Value);

        [Test]
        public void Poly_MixedListWithNulls_Binary()
        {
            var src = new PolyBox { List = new List<PolyBase> { null, new PolyBase { Id = 1 }, new PolyBigTag { Id = 2, Value = "x" }, null } };
            var copy = Bin(src);
            Assert.That(copy.List.Count, Is.EqualTo(4));
            Assert.IsNull(copy.List[0]);
            AssertPoly(src.List[1], copy.List[1]);
            AssertPoly(src.List[2], copy.List[2]);
            Assert.IsNull(copy.List[3]);
        }

        [Test]
        public void Poly_MixedListWithNulls_Json()
        {
            var src = new PolyBox { List = new List<PolyBase> { null, new PolyBase { Id = 1 }, new PolyBigTag { Id = 2, Value = "x" }, null } };
            var copy = Jsn(src);
            Assert.That(copy.List.Count, Is.EqualTo(4));
            Assert.IsNull(copy.List[0]);
            AssertPoly(src.List[1], copy.List[1]);
            AssertPoly(src.List[2], copy.List[2]);
            Assert.IsNull(copy.List[3]);
        }
    }
}
