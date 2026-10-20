using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    [Neuro(1)]
    public partial interface ISubFieldRoot
    {
    }

    [Neuro(1)]
    public partial class SubFieldLeaf : ISubFieldRoot
    {
        [Neuro(1)] public SubFieldInner Inner;
        [Neuro(2)] public int Number;
    }

    public partial class SubFieldInner
    {
        [Neuro(1)] public int Value;
    }

    public partial class SubFieldBaseClass
    {
        [Neuro(1)] public int BaseValue;
    }

    [Neuro(1)]
    public partial class SubFieldSubClass : SubFieldBaseClass
    {
        [Neuro(1)] public int SubValue;
    }

    [Neuro(2)]
    public partial class SubFieldSubSubClass : SubFieldSubClass
    {
        [Neuro(1)] public int SubSubValue;
    }

    public partial class SubFieldHolder
    {
        [Neuro(1)] public SubFieldLeaf Leaf;
        [Neuro(2)] public SubFieldSubClass Sub;
        [Neuro(3)] public SubFieldSubClass Deeper;
        [Neuro(4)] public int After;
    }

    /// A field declared as a subclass rather than its root (`SubFieldLeaf Leaf`, not `ISubFieldRoot`).
    /// Holding exactly the declared type it goes untagged, a deeper subclass (`Deeper`) tagged, and either
    /// way the base classes' fields come along. The binary writer used to put a ChildWithTag header with no
    /// tag on the untagged case, so the reader took the first field's header as the tag ("Sub type tag `22`
    /// ... is not registered"), and both formats dropped the base class's fields.
    public class SubClassTypedFieldTests
    {
        [SetUp]
        public void SetUp()
        {
            NeuroSyncTypes.TryRegisterAllAssemblies();
        }

        static SubFieldHolder Make()
        {
            return new SubFieldHolder
            {
                Leaf = new SubFieldLeaf { Inner = new SubFieldInner { Value = 3 }, Number = 4 },
                Sub = new SubFieldSubClass { BaseValue = 5, SubValue = 6 },
                Deeper = new SubFieldSubSubClass { BaseValue = 8, SubValue = 9, SubSubValue = 10 },
                After = 7
            };
        }

        static void AssertSame(SubFieldHolder back)
        {
            Assert.That(back.Leaf.Inner.Value, Is.EqualTo(3));
            Assert.That(back.Leaf.Number, Is.EqualTo(4));
            Assert.That(back.Sub.BaseValue, Is.EqualTo(5));
            Assert.That(back.Sub.SubValue, Is.EqualTo(6));
            Assert.That(back.Sub, Is.TypeOf<SubFieldSubClass>());
            var deeper = back.Deeper as SubFieldSubSubClass;
            Assert.That(deeper, Is.Not.Null);
            Assert.That(deeper.BaseValue, Is.EqualTo(8));
            Assert.That(deeper.SubValue, Is.EqualTo(9));
            Assert.That(deeper.SubSubValue, Is.EqualTo(10));
            Assert.That(back.After, Is.EqualTo(7));
        }

        [Test]
        public void Binary_RoundTrips()
        {
            var bytes = NeuroBytesWriter.Shared.Write(Make()).ToArray();
            AssertSame(NeuroBytesReader.Shared.Read<SubFieldHolder>(bytes));
        }

        [Test]
        public void Json_RoundTrips()
        {
            var json = NeuroJsonWriter.Shared.Write(Make());
            AssertSame(NeuroJsonReader.Shared.Read<SubFieldHolder>(json));
        }
    }
}
