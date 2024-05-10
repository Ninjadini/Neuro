using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    public class ScratchPad
    {
        static T WriteBinary_AndReadBack<T>(T obj)
        {
            var bytes = new NeuroBytesWriter().Write(obj).ToArray();

            try
            {
                Console.WriteLine("JSON:\n"+new NeuroJsonWriter().Write(obj));
            }
            catch (Exception e)
            {
                Console.WriteLine("JSON WRITE ERROR: " + e);
            }
            Console.WriteLine("Binary:" + new NeuroBytesDebugWalker().Walk(bytes));
            
            return new NeuroBytesReader().Read<T>(bytes, new ReaderOptions());
        }
        
        
        [Test]
        public void MyFirstNeuroObj()
        {
            var srcObj = new MyTestObj()
            {
                MyNum = 123,
            };

            var copiedObj = WriteBinary_AndReadBack(srcObj);
            
            Console.WriteLine("Num = "+copiedObj.MyNum);
            Assert.AreEqual(srcObj.MyNum, copiedObj.MyNum);
        }
    }

    public class MyTestObj
    {
        [Neuro(1)] public int MyNum;
    }
}