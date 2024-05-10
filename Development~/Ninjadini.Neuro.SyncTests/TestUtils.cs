using System;
using Ninjadini.Neuro;

namespace Ninjadini.Neuro.SyncTests
{
    public static class TestUtils
    {
        public static T CloneViaBinary<T>(T obj, bool printProtoBreakdown = false) where T : new()
        {
            var writer = NeuroBytesWriter.Shared;
            writer.Write(obj);
            if (printProtoBreakdown)
            {
                Console.WriteLine(new NeuroBytesDebugWalker().Walk(writer.GetCurrentBytesChunk()));
            }
            return NeuroBytesReader.Shared.Read<T>(writer.GetCurrentBytesChunk(), new ReaderOptions());
        }
        
        public static T CloneViaBinary<T>(T obj, out BytesChunk bytes, bool printProtoBreakdown = false) where T : new()
        {
            var writer = NeuroBytesWriter.Shared;
            writer.Write(obj);
            bytes = writer.GetCurrentBytesChunk();
            if (printProtoBreakdown)
            {
                Console.WriteLine(new NeuroBytesDebugWalker().Walk(bytes));
            }
            return NeuroBytesReader.Shared.Read<T>(bytes, new ReaderOptions());
        }
    }
}