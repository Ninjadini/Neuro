using System.Collections.Generic;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    public class ReferenceTests
    {
        private NeuroReferences references;
        private ReferencableClass ref1;
        private ReferencableClass ref2;
        private ReferencableClass ref3;
        
        [SetUp]
        public void set()
        {
            UberTestClass.RegisterAll();
            RegisterAll();
            references = new NeuroReferences();

            ref1 = new ReferencableClass()
            {
                RefId = 1,
                Name = "Ref1"
            };
            references.Register(ref1);
            ref2 = new ReferencableClass()
            {
                RefId = 2,
                Name = "Ref2"
            };
            references.Register(ref2);
            ref3 = new ReferencableClass()
            {
                RefId = 3,
                Name = "Ref3"
            };
            references.Register(ref3);
        }

        [Test]
        public void Test()
        {
            var testObj = new RefTestClass();
            testObj.Ref1 = references.Get<ReferencableClass>(1u);
            testObj.Ref2 = references.Get<ReferencableClass>(5u);

            testObj.Refs1.Add(references.Get<ReferencableClass>(3u));
            testObj.Refs1.Add(references.Get<ReferencableClass>(2u));
            testObj.Refs1.Add(references.Get<ReferencableClass>(1u));
            testObj.Refs2.Add(references.Get<ReferencableClass>(2u));
            testObj.Refs2.Add(references.Get<ReferencableClass>(1u));
            
            Assert.AreEqual(1, testObj.Ref1.RefId);
            Assert.AreEqual(ref1, testObj.Ref1.GetValue(references));
            Assert.AreEqual(0, testObj.Ref2.RefId);
            Assert.AreEqual(null, testObj.Ref2.GetValue(references));
            
            Assert.AreEqual(3, testObj.Refs1.Count);
            Assert.AreEqual(3, testObj.Refs1[0].RefId);
            Assert.AreEqual(2, testObj.Refs1[1].RefId);
            Assert.AreEqual(1, testObj.Refs1[2].RefId);
            Assert.AreEqual(ref3, testObj.Refs1[0].GetValue(references));
            Assert.AreEqual(ref2, testObj.Refs1[1].GetValue(references));
            Assert.AreEqual(ref1,testObj.Refs1[2].GetValue(references));
            
            
            Assert.AreEqual(2, testObj.Refs2.Count);
            Assert.AreEqual(2, testObj.Refs2[0].RefId);
            Assert.AreEqual(1, testObj.Refs2[1].RefId);
            Assert.AreEqual(ref2, testObj.Refs2[0].GetValue(references));
            Assert.AreEqual(ref1, testObj.Refs2[1].GetValue(references));

            testObj.Refs1.Remove(references.Get<ReferencableClass>(2u));
            testObj.Refs1.Remove(ref1);
            Assert.AreEqual(1, testObj.Refs1.Count);
            Assert.AreEqual(3, testObj.Refs1[0].RefId);
            Assert.AreEqual(ref3, testObj.Refs1[0].GetValue(references));
        }

        private static bool _registered;
        public static void RegisterAll()
        {
            if (_registered)
            {
                return;
            }
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref RefTestClass value)
            {
                value ??= new RefTestClass();
                neuro.Sync(1, nameof(value.Ref1), ref value.Ref1);
                neuro.Sync(2, nameof(value.Ref2), ref value.Ref2);
                neuro.Sync(10, nameof(value.Refs1), ref value.Refs1);
                neuro.Sync(11, nameof(value.Refs2), ref value.Refs2);
            });
        }

        class RefTestClass
        {
            public Reference<ReferencableClass> Ref1;
            public Reference<ReferencableClass> Ref2;
            
            public List<Reference<ReferencableClass>> Refs1 = new List<Reference<ReferencableClass>>();
            public List<Reference<ReferencableClass>> Refs2 = new List<Reference<ReferencableClass>>();
        }
    }
}