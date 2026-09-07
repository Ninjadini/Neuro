using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    [Neuro(1)]
    public partial interface IShapeRoot
    {
    }

    [Neuro(2)]
    public partial class ShapeCircle : IShapeRoot
    {
        [Neuro(1)] public int Radius;
    }

    /// Codegen can not put a [NeuroGlobalType] on an interface (Neuro314 says so), but a registry hook can
    /// register the id by hand. The type lookup has to find it - walking base classes alone never reaches
    /// an interface, so the concrete type would look unregistered.
    public class GlobalTypeInterfaceRootTests
    {
        const uint ShapeGlobalId = 9910;

        [SetUp]
        public void SetUp()
        {
            NeuroSyncTypes.TryRegisterAllAssemblies();
            NeuroGlobalTypes.Register<IShapeRoot>(ShapeGlobalId);
        }

        [Test]
        public void GetTypeId_FindsTheIdOnTheInterface()
        {
            Assert.That(NeuroGlobalTypes.GetIdByType(typeof(ShapeCircle)), Is.EqualTo(ShapeGlobalId));
            Assert.That(NeuroGlobalTypes.GetTypeIdOrThrow(typeof(ShapeCircle), out var rootType), Is.EqualTo(ShapeGlobalId));
            Assert.That(rootType, Is.EqualTo(typeof(IShapeRoot)));
        }

        [Test]
        public void GlobalTyped_Binary_RoundTrips()
        {
            var bytes = NeuroBytesWriter.Shared.WriteGlobalTyped(new ShapeCircle { Radius = 5 }).ToArray();
            var back = NeuroBytesReader.Shared.ReadGlobalTyped(bytes);
            Assert.That(back, Is.TypeOf<ShapeCircle>());
            Assert.That(((ShapeCircle)back).Radius, Is.EqualTo(5));
        }

        [Test]
        public void GlobalTyped_Json_RoundTrips()
        {
            var json = NeuroJsonWriter.Shared.WriteGlobalTyped(new ShapeCircle { Radius = 7 });
            var back = NeuroJsonReader.Shared.ReadGlobalTyped(json);
            Assert.That(back, Is.TypeOf<ShapeCircle>());
            Assert.That(((ShapeCircle)back).Radius, Is.EqualTo(7));
        }
    }
}
