using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    /// A referencable read by `ReadReferencesListInto` is not deserialized until something asks for it, and
    /// that can happen part way through another read on the same reader. The nested read has to leave the
    /// outer one exactly where it found it.
    public class LazyRefLoadReentrancyTests
    {
        [NeuroGlobalType(9930)]
        public partial class LazyRefItem : IReferencable
        {
            [Neuro(1)] public string Value;
            public uint RefId { get; set; }
            public string RefName { get; set; }
        }

        /// Registered by hand so that reading one can run arbitrary code - standing in for anything that
        /// reaches for a reference while a read is still in flight.
        public struct ReadHook
        {
            [ThreadStatic] internal static Action OnRead;
        }

        public struct ReadHookRegistry : INeuroCustomTypesRegistryHook
        {
            public void Register()
            {
                if (NeuroSyncTypes.IsEmpty<ReadHook>())
                {
                    NeuroSyncTypes.Register(FieldSizeType.VarInt, (INeuroSync neuro, ref ReadHook value) =>
                    {
                        var num = 0u;
                        neuro.Sync(ref num);
                        if (neuro.IsReading)
                        {
                            ReadHook.OnRead?.Invoke();
                        }
                    });
                }
            }
        }

        public partial class Holder
        {
            [Neuro(1)] public string Before;
            [Neuro(2)] public ReadHook Hook;
            [Neuro(3)] public string After;
            [Neuro(4)] public List<string> Tail;
        }

        [TearDown]
        public void TearDown() => ReadHook.OnRead = null;

        [Test]
        public void LoadingAReferenceMidReadDoesNotDisturbTheOuterRead()
        {
            NeuroSyncTypes.TryRegisterAssemblyOf<LazyRefItem>();
            var refs = new NeuroReferences();
            var refBytes = new NeuroBytesWriter().WriteReferencesList(new IReferencable[]
            {
                new LazyRefItem { RefId = 1, RefName = "one", Value = "first" },
                new LazyRefItem { RefId = 2, RefName = "two", Value = "second" },
            }.AsSpan()).ToArray();

            // The same reader indexes the reference list and then reads the holder, so the lazy load below
            // runs on a reader that is already part way through the holder.
            var reader = new NeuroBytesReader();
            reader.ReadReferencesListInto(refs, refBytes);

            var source = new Holder { Before = "before", After = "after", Tail = new List<string> { "a", "b" } };
            var holderBytes = new NeuroBytesWriter().Write(source).ToArray();

            LazyRefItem loaded = null;
            ReadHook.OnRead = () => loaded = refs.Get<LazyRefItem>(2u);

            var result = reader.Read<Holder>(holderBytes);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Value, Is.EqualTo("second"));
            Assert.That(loaded.RefName, Is.EqualTo("two"));
            // Everything after the nested load has to still read correctly.
            Assert.That(result.Before, Is.EqualTo("before"));
            Assert.That(result.After, Is.EqualTo("after"));
            Assert.That(result.Tail, Is.EqualTo(new List<string> { "a", "b" }));
        }

        [Test]
        public void ReadingARefNameMidReadDoesNotDisturbTheOuterRead()
        {
            NeuroSyncTypes.TryRegisterAssemblyOf<LazyRefItem>();
            var refs = new NeuroReferences();
            var refBytes = new NeuroBytesWriter().WriteReferencesList(new IReferencable[]
            {
                new LazyRefItem { RefId = 1, RefName = "one", Value = "first" },
            }.AsSpan()).ToArray();

            var reader = new NeuroBytesReader();
            reader.ReadReferencesListInto(refs, refBytes);

            var source = new Holder { Before = "before", After = "after" };
            var holderBytes = new NeuroBytesWriter().Write(source).ToArray();

            string name = null;
            ReadHook.OnRead = () => name = refs.GetTable<LazyRefItem>().GetRefName(1u);

            var result = reader.Read<Holder>(holderBytes);

            Assert.That(name, Is.EqualTo("one"));
            Assert.That(result.Before, Is.EqualTo("before"));
            Assert.That(result.After, Is.EqualTo("after"));
        }
    }
}
