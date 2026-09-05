using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests
{
    /// A field lookup only searches the group it is in. These cover the cases where searching too widely,
    /// or too narrowly, would pick up the wrong node - repeated names at different depths, siblings that
    /// come after a big subtree, and collections nested inside collections.
    public class JsonNestedLookupTests
    {
        public partial class Node
        {
            [Neuro(1)] public string Name;
            [Neuro(2)] public int Value;
            [Neuro(3)] public Node Child;
            [Neuro(4)] public List<Node> Children;
            [Neuro(5)] public Dictionary<string, Node> Map;
            [Neuro(6)] public string Trailing;
        }

        [Test]
        public void RepeatedFieldNamesAtEveryDepth_EachReadsItsOwn()
        {
            var json = @"{
                ""Name"": ""root"", ""Value"": 1,
                ""Child"": {
                    ""Name"": ""a"", ""Value"": 2,
                    ""Child"": { ""Name"": ""b"", ""Value"": 3, ""Trailing"": ""deep"" },
                    ""Trailing"": ""mid""
                },
                ""Trailing"": ""top""
            }";
            var result = NeuroJsonReader.Shared.Read<Node>(json);

            Assert.That(result.Name, Is.EqualTo("root"));
            Assert.That(result.Value, Is.EqualTo(1));
            Assert.That(result.Trailing, Is.EqualTo("top"));
            Assert.That(result.Child.Name, Is.EqualTo("a"));
            Assert.That(result.Child.Value, Is.EqualTo(2));
            Assert.That(result.Child.Trailing, Is.EqualTo("mid"));
            Assert.That(result.Child.Child.Name, Is.EqualTo("b"));
            Assert.That(result.Child.Child.Value, Is.EqualTo(3));
            Assert.That(result.Child.Child.Trailing, Is.EqualTo("deep"));
        }

        [Test]
        public void FieldAfterALargeSubtree_IsStillFound()
        {
            // "Trailing" sits after a subtree of its own siblings, so stepping over that subtree has to land
            // on it rather than past it.
            var source = new Node
            {
                Name = "root",
                Children = new List<Node>(),
                Trailing = "found me",
            };
            for (var i = 0; i < 50; i++)
            {
                source.Children.Add(new Node { Name = "c" + i, Value = i, Child = new Node { Name = "cc" + i, Value = i * 2 } });
            }
            var result = NeuroJsonReader.Shared.Read<Node>(NeuroJsonWriter.Shared.Write(source));

            Assert.That(result.Trailing, Is.EqualTo("found me"));
            Assert.That(result.Children.Count, Is.EqualTo(50));
            for (var i = 0; i < 50; i++)
            {
                Assert.That(result.Children[i].Name, Is.EqualTo("c" + i));
                Assert.That(result.Children[i].Value, Is.EqualTo(i));
                Assert.That(result.Children[i].Child.Name, Is.EqualTo("cc" + i));
                Assert.That(result.Children[i].Child.Value, Is.EqualTo(i * 2));
            }
        }

        [Test]
        public void ObjectsNestedInsideCollections_RoundTrip()
        {
            var source = new Node
            {
                Name = "root",
                Children = new List<Node>
                {
                    new Node { Name = "l0", Map = new Dictionary<string, Node> { { "x", new Node { Name = "l0x", Value = 10 } } } },
                    new Node { Name = "l1", Children = new List<Node> { new Node { Name = "l1a", Value = 11 } } },
                },
                Map = new Dictionary<string, Node>
                {
                    { "k0", new Node { Name = "m0", Children = new List<Node> { new Node { Name = "m0a", Value = 20 } } } },
                    { "k1", new Node { Name = "m1", Child = new Node { Name = "m1c", Value = 21 } } },
                },
                Trailing = "end",
            };
            var result = NeuroJsonReader.Shared.Read<Node>(NeuroJsonWriter.Shared.Write(source));

            Assert.That(result.Children[0].Map["x"].Value, Is.EqualTo(10));
            Assert.That(result.Children[1].Children[0].Value, Is.EqualTo(11));
            Assert.That(result.Map["k0"].Children[0].Value, Is.EqualTo(20));
            Assert.That(result.Map["k1"].Child.Value, Is.EqualTo(21));
            Assert.That(result.Trailing, Is.EqualTo("end"));
        }

        [Test]
        public void AFieldOnlyPresentDeeperDown_IsNotPickedUp()
        {
            // "Trailing" exists, but only inside Child - the root must not reach into it.
            var result = NeuroJsonReader.Shared.Read<Node>(@"{""Name"":""root"",""Child"":{""Trailing"":""inner""}}");
            Assert.That(result.Trailing, Is.Null);
            Assert.That(result.Child.Trailing, Is.EqualTo("inner"));
        }

        [Test]
        public void EmptyObjectsAndCollections_ReadAsNothing()
        {
            var result = NeuroJsonReader.Shared.Read<Node>(@"{""Child"":{},""Children"":[],""Map"":{},""Name"":""root""}");
            Assert.That(result.Name, Is.EqualTo("root"));
            Assert.That(result.Child, Is.Not.Null);
            Assert.That(result.Child.Name, Is.Null);
            Assert.That(result.Children, Is.Empty);
            Assert.That(result.Map, Is.Empty);
        }

        static Node BuildWideTree(int breadth, int depth)
        {
            var node = new Node { Name = "n" + depth, Value = depth, Trailing = "t" + depth };
            if (depth > 0)
            {
                node.Children = new List<Node>(breadth);
                for (var i = 0; i < breadth; i++)
                {
                    node.Children.Add(BuildWideTree(breadth, depth - 1));
                }
            }
            return node;
        }

        [TestCase(3, 3), TestCase(5, 4), TestCase(6, 6)]
        [Explicit("timing only")]
        public void ReadTiming(int breadth, int depth)
        {
            var json = NeuroJsonWriter.Shared.Write(BuildWideTree(breadth, depth));
            NeuroJsonReader.Shared.Read<Node>(json);
            var sw = Stopwatch.StartNew();
            const int reads = 20;
            for (var i = 0; i < reads; i++)
            {
                NeuroJsonReader.Shared.Read<Node>(json);
            }
            Console.WriteLine($"json length {json.Length:n0} -> {sw.Elapsed.TotalMilliseconds / reads:n2} ms per read");
        }
    }
}
