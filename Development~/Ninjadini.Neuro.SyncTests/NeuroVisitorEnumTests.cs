using System.Collections.Generic;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    /// An enum is a value the way a number is, so a walk that asked for primitives is handed it too - the
    /// editor's data search relies on that to find an enum by its name. A walk that did not ask sees no
    /// enum, the same as it sees no int.
    public class NeuroVisitorEnumTests
    {
        [SetUp]
        public void SetUp()
        {
            UberTestClass.RegisterAll();
        }

        [Test]
        public void NeuroVisitorHandsOverEnumsWithPrimitives()
        {
            var obj = new UberTestClass { Enum = TestEnum1.B };
            var recorder = new Recorder();

            new NeuroVisitor().Visit(obj, recorder, true);

            Assert.That(recorder.Visited, Does.Contain(("Enum", (object)TestEnum1.B)));
        }

        [Test]
        public void NeuroEditVisitorHandsOverEnumsWithPrimitives()
        {
            var obj = new UberTestClass { Enum = TestEnum1.C };
            var recorder = new Recorder();

            new NeuroEditVisitor().Visit(obj, recorder, true);

            Assert.That(recorder.Visited, Does.Contain(("Enum", (object)TestEnum1.C)));
        }

        [Test]
        public void NoEnumsWithoutPrimitives()
        {
            var obj = new UberTestClass { Enum = TestEnum1.B };
            var recorder = new Recorder();
            var editRecorder = new Recorder();

            new NeuroVisitor().Visit(obj, recorder);
            new NeuroEditVisitor().Visit(obj, editRecorder);

            Assert.That(recorder.Names, Does.Not.Contain("Enum"));
            Assert.That(editRecorder.Names, Does.Not.Contain("Enum"));
        }

        class Recorder : NeuroVisitor.IInterface, NeuroEditVisitor.IInterface
        {
            public readonly List<(string name, object value)> Visited = new List<(string, object)>();
            public readonly List<string> Names = new List<string>();

            void Record<T>(T obj, string name)
            {
                Visited.Add((name, obj));
                Names.Add(name);
            }

            void NeuroVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex) => Record(obj, name);
            void NeuroVisitor.IInterface.EndVisit() { }
            void NeuroVisitor.IInterface.VisitRef<T>(ref Reference<T> reference) { }

            void NeuroEditVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex) => Record(obj, name);
            void NeuroEditVisitor.IInterface.EndVisit() { }
            void NeuroEditVisitor.IInterface.VisitRef<T>(ref Reference<T> reference) { }
        }
    }
}
