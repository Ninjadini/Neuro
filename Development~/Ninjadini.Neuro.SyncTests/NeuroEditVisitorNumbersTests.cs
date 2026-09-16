using System.Collections.Generic;
using System.Linq;
using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    /// NeuroEditVisitor's `visitPrimitiveValues` walk - the one the editor's bulk field edit is built on.
    /// It is what lets a tool say "every CreepTemplate's Health, times 1.2" without knowing anything about
    /// the shape of the data, so what matters here is that every number is reached, that it is reached under
    /// a path that tells it apart from the identically named number on a child object, and that writing to
    /// the `ref` lands in the real object rather than in a copy.
    public class NeuroEditVisitorNumbersTests
    {
        [SetUp]
        public void SetUp()
        {
            Register();
        }

        [Test]
        public void ReachesEveryNumberUnderItsOwnPath()
        {
            var obj = NewObj();

            var paths = Recorder.RecordPaths(obj);

            Assert.That(paths, Does.Contain("Health"), "plain int field");
            Assert.That(paths, Does.Contain("Speed"), "plain float field");
            Assert.That(paths, Does.Contain("Child>Health"), "number on a child object");
            Assert.That(paths, Does.Contain("Children>Children[0]>Health"), "number on a child in a list");
            Assert.That(paths, Does.Contain("Numbers>Numbers[1]"), "primitive in a list");
            Assert.That(paths, Does.Contain("Milli"), "custom number struct");
        }

        [Test]
        public void LeavesNumbersAloneWithoutTheFlag()
        {
            // The default walk skips primitives, which is why the RefId rewriter never sees them.
            var obj = NewObj();

            var paths = Recorder.RecordPaths(obj, visitPrimitives: false);

            Assert.That(paths, Does.Not.Contain("Health"));
            Assert.That(paths, Does.Contain("Milli"), "a registered struct is not a primitive, so it is still visited");
        }

        [Test]
        public void WritesLandInTheRealObject()
        {
            var obj = NewObj();

            Rewriter.Multiply<int>(obj, "Health", 2);

            Assert.That(obj.Health, Is.EqualTo(20), "plain field");
            Assert.That(obj.Child.Health, Is.EqualTo(5), "the child's same named field is a different path, so it is untouched");
        }

        [Test]
        public void WritesLandInsideChildrenAndLists()
        {
            var obj = NewObj();

            Rewriter.Multiply<int>(obj, "Child>Health", 3);
            Rewriter.Multiply<int>(obj, "Children>Children[0]>Health", 3);
            Rewriter.Multiply<int>(obj, "Numbers>Numbers[1]", 3);

            Assert.That(obj.Child.Health, Is.EqualTo(15), "field of a child object");
            Assert.That(obj.Children[0].Health, Is.EqualTo(21), "field of a child in a list");
            Assert.That(obj.Numbers[1], Is.EqualTo(6), "primitive in a list");
            Assert.That(obj.Numbers[0], Is.EqualTo(1), "the other element is untouched");
        }

        [Test]
        public void WritesLandInAStructHeldByAStruct()
        {
            // The nesting that plain reflection gets wrong: writing to a field of a boxed struct copy is
            // dropped unless every struct above it is written back too. The visitor's ref chain does that.
            var obj = NewObj();

            Rewriter.Multiply<float>(obj, "Nested>Inner>Speed", 2);

            Assert.That(obj.Nested.Inner.Speed, Is.EqualTo(5f));
        }

        [Test]
        public void WritesLandInANullable()
        {
            var obj = NewObj();

            Rewriter.Multiply<int>(obj, "MaybeHealth", 5);

            Assert.That(obj.MaybeHealth.Value, Is.EqualTo(35));
        }

        [Test]
        public void ACustomNumberStructIsWrittenAsItself()
        {
            // fp is registered this way: a struct whose sync delegate rewrites `value` on its way through.
            // A bulk edit has to survive that rewrite, and quantise the same way saving would.
            var obj = NewObj();

            Rewriter.Set(obj, "Milli", new Milli(2.5));

            Assert.That(obj.Milli.Value, Is.EqualTo(2.5).Within(0.0005));
        }

        [Test]
        public void ListElementsShareTheListsNameAndCarryAnIndex()
        {
            // The shape the editor's path matching relies on: the list itself is visited under its field name
            // with no index, then every element under that same name with its index.
            var obj = NewObj();

            var steps = Recorder.RecordSteps(obj).Where(s => s.Name == "Numbers").ToList();

            Assert.That(steps.Count, Is.EqualTo(1 + obj.Numbers.Count));
            Assert.That(steps[0].Index, Is.Null, "the list itself");
            Assert.That(steps.Skip(1).Select(s => s.Index), Is.EqualTo(new int?[] { 0, 1, 2 }));
        }

        static NumbersTestClass NewObj()
        {
            var obj = new NumbersTestClass();
            obj.Health = 10;
            obj.Speed = 1.5f;
            obj.MaybeHealth = 7;
            obj.Milli = new Milli(1.25);
            obj.Child = new NumbersChild() { Health = 5, Speed = 0.5f };
            obj.Children.Add(new NumbersChild() { Health = 7, Speed = 0.25f });
            obj.Numbers.AddRange(new[] { 1, 2, 3 });
            obj.Nested.Inner.Speed = 2.5f;
            return obj;
        }

        struct Step
        {
            public string Name;
            public int? Index;
        }

        /// Walks the object writing down the path of every value it is handed, in the same
        /// "skip the unnamed root frames" form the editor's bulk edit matches on.
        class Recorder : NeuroEditVisitor.IInterface
        {
            readonly List<Step> stack = new List<Step>();
            readonly List<Step> steps = new List<Step>();
            readonly List<string> paths = new List<string>();

            public static List<string> RecordPaths(NumbersTestClass obj, bool visitPrimitives = true)
            {
                var recorder = new Recorder();
                new NeuroEditVisitor().Visit(obj, recorder, visitPrimitives);
                return recorder.paths;
            }

            public static List<Step> RecordSteps(NumbersTestClass obj)
            {
                var recorder = new Recorder();
                new NeuroEditVisitor().Visit(obj, recorder, true);
                return recorder.steps;
            }

            void NeuroEditVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex)
            {
                var step = new Step() { Name = name, Index = listIndex };
                stack.Add(step);
                if (!string.IsNullOrEmpty(name))
                {
                    steps.Add(step);
                    paths.Add(PathOf(stack));
                }
            }

            void NeuroEditVisitor.IInterface.EndVisit()
            {
                stack.RemoveAt(stack.Count - 1);
            }

            void NeuroEditVisitor.IInterface.VisitRef<T>(ref Reference<T> reference)
            {
            }

            public static string PathOf(List<Step> stack)
            {
                return string.Join(">", stack
                    .SkipWhile(s => string.IsNullOrEmpty(s.Name))
                    .Select(s => s.Index.HasValue ? s.Name + "[" + s.Index.Value + "]" : s.Name));
            }
        }

        /// The bulk edit in miniature: find the one path, change the value that is there.
        class Rewriter : NeuroEditVisitor.IInterface
        {
            readonly List<Step> stack = new List<Step>();
            string targetPath;
            object newValue;
            double multiplier;
            public int Changes;

            public static void Multiply<T>(NumbersTestClass obj, string path, double by)
            {
                var rewriter = new Rewriter() { targetPath = path, multiplier = by };
                new NeuroEditVisitor().Visit(obj, rewriter, true);
                Assert.That(rewriter.Changes, Is.EqualTo(1), "expected to find exactly one " + path);
            }

            public static void Set<T>(NumbersTestClass obj, string path, T value)
            {
                var rewriter = new Rewriter() { targetPath = path, newValue = value };
                new NeuroEditVisitor().Visit(obj, rewriter, true);
                Assert.That(rewriter.Changes, Is.EqualTo(1), "expected to find exactly one " + path);
            }

            void NeuroEditVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex)
            {
                stack.Add(new Step() { Name = name, Index = listIndex });
                if (Recorder.PathOf(stack) != targetPath)
                {
                    return;
                }
                if (newValue is T typed)
                {
                    obj = typed;
                    Changes++;
                }
                else if (obj is int intValue)
                {
                    obj = (T)(object)(int)(intValue * multiplier);
                    Changes++;
                }
                else if (obj is float floatValue)
                {
                    obj = (T)(object)(float)(floatValue * multiplier);
                    Changes++;
                }
            }

            void NeuroEditVisitor.IInterface.EndVisit()
            {
                stack.RemoveAt(stack.Count - 1);
            }

            void NeuroEditVisitor.IInterface.VisitRef<T>(ref Reference<T> reference)
            {
            }
        }

        static bool _registered;

        static void Register()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            // Registered the way fp is: a varint sized struct whose delegate rewrites `value` on the way past,
            // quantising it to whole milli units.
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref Milli value)
            {
                var raw = value.Raw;
                neuro.Sync(ref raw);
                value = Milli.FromRaw(raw);
            });
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref NumbersChild value)
            {
                value = value ?? new NumbersChild();
                neuro.Sync(1, nameof(value.Health), ref value.Health, 0);
                neuro.Sync(2, nameof(value.Speed), ref value.Speed, 0f);
            });
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref NumbersInnerStruct value)
            {
                neuro.Sync(1, nameof(value.Speed), ref value.Speed, 0f);
            });
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref NumbersOuterStruct value)
            {
                neuro.Sync(1, nameof(value.Inner), ref value.Inner, default);
            });
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref NumbersTestClass value)
            {
                value = value ?? new NumbersTestClass();
                neuro.Sync(1, nameof(value.Health), ref value.Health, 0);
                neuro.Sync(2, nameof(value.Speed), ref value.Speed, 0f);
                neuro.Sync(3, nameof(value.MaybeHealth), ref value.MaybeHealth);
                neuro.Sync(4, nameof(value.Milli), ref value.Milli, default);
                neuro.Sync(5, nameof(value.Child), ref value.Child);
                neuro.Sync(6, nameof(value.Children), ref value.Children);
                neuro.Sync(7, nameof(value.Numbers), ref value.Numbers);
                neuro.Sync(8, nameof(value.Nested), ref value.Nested, default);
            });
        }

        class NumbersTestClass
        {
            public int Health;
            public float Speed;
            public int? MaybeHealth;
            public Milli Milli;
            public NumbersChild Child;
            public List<NumbersChild> Children = new List<NumbersChild>();
            public List<int> Numbers = new List<int>();
            public NumbersOuterStruct Nested;
        }

        class NumbersChild
        {
            public int Health;
            public float Speed;
        }

        struct NumbersOuterStruct : System.IEquatable<NumbersOuterStruct>
        {
            public NumbersInnerStruct Inner;

            public bool Equals(NumbersOuterStruct other) => Inner.Equals(other.Inner);
        }

        struct NumbersInnerStruct : System.IEquatable<NumbersInnerStruct>
        {
            public float Speed;

            public bool Equals(NumbersInnerStruct other) => Speed.Equals(other.Speed);
        }

        /// Stands in for fp: a number kept as a whole count of milli units.
        struct Milli : System.IEquatable<Milli>
        {
            public long Raw;

            public Milli(double value)
            {
                Raw = (long)System.Math.Round(value * 1000);
            }

            public static Milli FromRaw(long raw) => new Milli() { Raw = raw };

            public double Value => Raw / 1000.0;

            public bool Equals(Milli other) => Raw == other.Raw;
        }
    }
}
