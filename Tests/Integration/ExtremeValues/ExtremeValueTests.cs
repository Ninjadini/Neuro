using System;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    /// <summary>
    /// Round trips every Neuro supported type through both binary and json at the extreme ends of its range.
    /// Each value is its own test case so a failure points straight at the value that broke.
    /// Some of these are expected to fail - the point is to document exactly where the limits are.
    /// </summary>
    public partial class ExtremeValueTests
    {
        [OneTimeSetUp]
        public void RegisterTypes()
        {
            NeuroSyncTypes.TryRegisterAssemblyOf<BoolBox>();
        }

        const int MaxLogChars = 400;

        static void Log(string prefix, string content)
        {
            TestContext.WriteLine(content.Length > MaxLogChars
                ? prefix + content.Substring(0, MaxLogChars) + "... (" + content.Length + " chars)"
                : prefix + content);
        }

        static T Bin<T>(T src) where T : class, new()
        {
            var bytes = new NeuroBytesWriter().Write(src).ToArray();
            try
            {
                Log("binary: ", RawProtoReader.GetDebugString(bytes));
            }
            catch (Exception e)
            {
                TestContext.WriteLine("binary: <could not dump bytes> " + e.Message);
            }
            return new NeuroBytesReader().Read<T>(bytes, new ReaderOptions());
        }

        static T Jsn<T>(T src) where T : class, new()
        {
            var json = new NeuroJsonWriter().Write(src);
            Log("json: ", json);
            return new NeuroJsonReader().Read<T>(json, new ReaderOptions());
        }
    }
}
