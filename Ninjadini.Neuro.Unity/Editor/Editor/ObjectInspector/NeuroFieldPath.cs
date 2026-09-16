using System;
using System.Collections.Generic;
using System.Text;

namespace Ninjadini.Neuro.Editor
{
    /// Where a drawn field sits in its root object, as the chain of Neuro field names it took to get there -
    /// `Attack > Damage`, or `Rounds > Rounds[2] > Count`. Unlike a getter/setter pair, which closes over the
    /// one object it was made for, a path can be followed on any other object of the same type, which is what
    /// lets the inspector offer edits across a whole table (see <see cref="NeuroBulkFieldEdit"/>).
    ///
    /// The steps are exactly what <see cref="NeuroEditVisitor"/> reports, so the two line up without
    /// translation: a list is one step under its own field name, and each of its elements is another step
    /// under that same name carrying an index. A path is null when it can not be followed - inside a
    /// dictionary, where the visitor can not tell a key from a value.
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public sealed class NeuroFieldPath
    {
        public readonly struct Step : IEquatable<Step>
        {
            public readonly string Name;
            /// -1 when this step is not a collection element.
            public readonly int Index;

            public Step(string name, int index = -1)
            {
                Name = name;
                Index = index;
            }

            public bool IsElement => Index >= 0;

            public bool Equals(Step other) => Index == other.Index && Name == other.Name;
        }

        readonly Step[] steps;

        NeuroFieldPath(Step[] steps)
        {
            this.steps = steps;
        }

        public static NeuroFieldPath Root => Empty;
        static readonly NeuroFieldPath Empty = new NeuroFieldPath(Array.Empty<Step>());

        public int Count => steps.Length;
        public Step this[int index] => steps[index];

        /// The path of a field of this object.
        public NeuroFieldPath Append(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return this;
            }
            var result = new Step[steps.Length + 1];
            Array.Copy(steps, result, steps.Length);
            result[steps.Length] = new Step(name);
            return new NeuroFieldPath(result);
        }

        /// The path of one element of this list. The visitor reports an element under the list's own name, so
        /// that is the name this carries too - only the index tells them apart.
        public NeuroFieldPath AppendElement(int index)
        {
            if (steps.Length == 0 || index < 0)
            {
                return null;
            }
            var result = new Step[steps.Length + 1];
            Array.Copy(steps, result, steps.Length);
            result[steps.Length] = new Step(steps[steps.Length - 1].Name, index);
            return new NeuroFieldPath(result);
        }

        /// Whether the place the visitor is standing in - `visited`, innermost step last - is this same field
        /// on some other object.
        ///
        /// An index is matched as a wildcard: the path of `Rounds[2] > Count` matches the Count of *every*
        /// round, not only the third. That is the reading a bulk edit wants ("every round's count"), and the
        /// alternative - only ever the third round, in items that may not even have three - is nearly useless.
        /// Callers say how many fields they are about to change so the choice is never silent.
        public bool Matches(IReadOnlyList<Step> visited)
        {
            // Leading unnamed steps are the walk's own root frames, not fields of anything.
            var start = 0;
            while (start < visited.Count && string.IsNullOrEmpty(visited[start].Name))
            {
                start++;
            }
            if (visited.Count - start != steps.Length)
            {
                return false;
            }
            for (var i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                var other = visited[start + i];
                if (step.Name != other.Name || step.IsElement != other.IsElement)
                {
                    return false;
                }
            }
            return true;
        }

        /// Reads as `Attack > Damage` or `Rounds[] > Count` - an element step prints as `[]` because that is
        /// what it means to anything following the path.
        public override string ToString()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < steps.Length; i++)
            {
                if (steps[i].IsElement)
                {
                    // the list step just before it already printed the name.
                    builder.Append("[]");
                    continue;
                }
                if (builder.Length > 0)
                {
                    builder.Append(" > ");
                }
                builder.Append(steps[i].Name);
            }
            return builder.ToString();
        }
    }
}
