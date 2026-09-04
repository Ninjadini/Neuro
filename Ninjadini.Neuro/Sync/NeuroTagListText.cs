using System;
using System.Collections.Generic;
using System.Text;

namespace Ninjadini.Neuro.Sync
{
    /// Formats "which numbers are already taken" for a registration conflict, matching the wording the
    /// code generator uses for the same problem at compile time.
    /// Only ever reached on a throw path, so it is free to sort and allocate.
    internal static class NeuroTagListText
    {
        /// `Used tags: 1-7, 10. Next free: 8. Full list: 1=Foo; 2=Bar`
        /// Tag 0 is skipped - it is never a real tag, it just marks the root of a hierarchy.
        internal static string Describe(Dictionary<Type, uint> tagByType, string noun)
        {
            var sorted = new List<KeyValuePair<Type, uint>>(tagByType.Count);
            foreach (var kv in tagByType)
            {
                if (kv.Value > 0)
                {
                    sorted.Add(kv);
                }
            }
            sorted.Sort((a, b) => a.Value.CompareTo(b.Value));

            var ranges = new StringBuilder();
            var list = new StringBuilder();
            var nextFree = 1u;
            var index = 0;
            while (index < sorted.Count)
            {
                var start = sorted[index].Value;
                var end = start;
                var next = index + 1;
                while (next < sorted.Count && (sorted[next].Value == end || sorted[next].Value == end + 1))
                {
                    end = sorted[next].Value;
                    next++;
                }
                if (ranges.Length > 0)
                {
                    ranges.Append(", ");
                }
                ranges.Append(start);
                if (end != start)
                {
                    ranges.Append("-").Append(end);
                }
                if (nextFree >= start && nextFree <= end)
                {
                    nextFree = end + 1;
                }
                index = next;
            }
            foreach (var kv in sorted)
            {
                if (list.Length > 0)
                {
                    list.Append("; ");
                }
                list.Append(kv.Value).Append("=").Append(kv.Key.Name);
            }
            var builder = new StringBuilder();
            builder.Append("Used ").Append(noun).Append(": ").Append(ranges.Length > 0 ? ranges.ToString() : "none");
            builder.Append(". Next free: ").Append(nextFree);
            builder.Append(". Full list: ").Append(list.Length > 0 ? list.ToString() : "none");
            return builder.ToString();
        }
    }
}
