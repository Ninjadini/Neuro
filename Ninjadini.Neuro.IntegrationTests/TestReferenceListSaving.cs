using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests;

public partial class TestReferenceListSaving
{
    [Test]
    public void TestListRefStoring()
    {
        var list = new List<IReferencable>()
        {
            new MyRefObject1()
            {
                RefId = 1,
                Name = "1"
            },
            new MyRefObject1()
            {
                RefId = 2,
                Name = "2"
            },
            new MyRefObject2()
            {
                RefId = 1,
                Name = "3"
            },
            new MyRefObject2()
            {
                RefId = 2,
                Name = "4"
            }
        };
        
        
        NeuroSyncTypes.TryRegisterAssemblyOf<MyRefObject2>();
        var refs = new NeuroReferences();

        
        var bytes = new NeuroBytesWriter().WriteReferencesList(list.ToArray().AsSpan()).ToArray();
        
        Console.WriteLine("Binary:" + RawProtoReader.GetDebugString(bytes));
        
        new NeuroBytesReader().ReadReferencesListInto(refs, bytes);
        
        Assert.AreEqual("1", refs.Get<MyRefObject1>(1u).Name);
        Assert.AreEqual("2", refs.Get<MyRefObject1>(2u).Name);
        Assert.AreEqual("4", refs.Get<MyRefObject2>(2u).Name);
    }

    [NeuroGlobalType(111)]
    public partial class MyRefObject1 : IReferencable
    {
        [Neuro(2)] public string Name;
        
        [Neuro(3)] string MyValues;

        public uint RefId { get; set; }
        public string RefName { get; set; }
    }
    
    [NeuroGlobalType(222)]
    public partial class MyRefObject2 : IReferencable
    {
        [Neuro(2)] public string Name;
        
        [Neuro(3)] string MyValues;

        public uint RefId { get; set; }
        public string RefName { get; set; }
    }
}