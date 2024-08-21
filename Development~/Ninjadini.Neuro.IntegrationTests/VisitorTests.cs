
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests;

public partial class VisitorTests
{
    [Test]
    public void TestCloning()
    {
        var uberObj = new UberTestObject();
        uberObj.PopulateValues();

        Assert.IsFalse(string.IsNullOrEmpty(uberObj.Name));
        
        for(var i = 0; i < 10; i++)
        {
            var copy = NeuroBytesWriter.Clone(uberObj);
            Assert.AreEqual(uberObj.Id, copy.Id);
            Assert.AreEqual(uberObj.Name, copy.Name);
            Assert.AreEqual(uberObj.Date, copy.Date);
        }
    }
}