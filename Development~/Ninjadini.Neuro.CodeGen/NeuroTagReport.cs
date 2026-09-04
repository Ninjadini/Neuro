using System.Collections.Generic;
using System.Text;

namespace Ninjadini.Neuro.CodeGen
{
    /// Formats the "which tag numbers are already spent?" half of a tag diagnostic.
    /// Every caller is on a path that is already reporting an error, so sorting and allocating here
    /// costs nothing on a clean compile.
    /// An entry with no Name is a [ReservedNeuroTag] tombstone - still taken, just not by a member.
    public static class NeuroTagReport
    {
        public struct Entry
        {
            public uint Tag;
            public string Name;

            public Entry(uint tag, string name)
            {
                Tag = tag;
                Name = name;
            }

            public bool IsReserved
            {
                get { return string.IsNullOrEmpty(Name); }
            }
        }

        /// The tail appended to a tag diagnostic:
        /// `Used tags: 1-7, 10, 12(reserved). Next free: 8. Full list: 1=Foo; 2=Bar; 3=[reserved]`
        /// Deliberately single line - unity's console does not render multiline messages.
        public static string Describe(List<Entry> entries, string noun = "tags")
        {
            var sorted = Sorted(entries);
            var builder = new StringBuilder();
            builder.Append("Used ").Append(noun).Append(": ").Append(UsedRanges(sorted));
            builder.Append(". Next free: ").Append(NextFree(sorted));
            builder.Append(". Full list: ").Append(FullList(sorted));
            return builder.ToString();
        }

        /// `1-7, 10, 12(reserved)`. Runs only merge across entries of the same kind so a reserved
        /// block stays visible as one.
        public static string UsedRanges(List<Entry> entries)
        {
            var sorted = Sorted(entries);
            var builder = new StringBuilder();
            var index = 0;
            var count = sorted.Count;
            while (index < count)
            {
                var start = sorted[index];
                var end = start;
                var next = index + 1;
                while (next < count
                       && sorted[next].IsReserved == start.IsReserved
                       && (sorted[next].Tag == end.Tag || sorted[next].Tag == end.Tag + 1))
                {
                    end = sorted[next];
                    next++;
                }
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(start.Tag);
                if (end.Tag != start.Tag)
                {
                    builder.Append("-").Append(end.Tag);
                }
                if (start.IsReserved)
                {
                    builder.Append("(reserved)");
                }
                index = next;
            }
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        /// Lowest tag >= 1 that nothing - member or reserved tombstone - has taken.
        public static uint NextFree(List<Entry> entries)
        {
            var sorted = Sorted(entries);
            var free = 1u;
            foreach (var entry in sorted)
            {
                if (entry.Tag > free)
                {
                    break;
                }
                if (entry.Tag == free)
                {
                    free++;
                }
            }
            return free;
        }

        /// `1=Foo; 2=Bar; 3=[reserved]`, sorted by tag so the gaps are readable.
        public static string FullList(List<Entry> entries)
        {
            var sorted = Sorted(entries);
            var builder = new StringBuilder();
            foreach (var entry in sorted)
            {
                if (builder.Length > 0)
                {
                    builder.Append("; ");
                }
                builder.Append(entry.Tag).Append("=").Append(entry.IsReserved ? "[reserved]" : entry.Name);
            }
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        /// Sorted copy, with tag 0 dropped - 0 is never a valid tag, it just means 'not set yet'.
        /// The caller's list is left alone; it is often the live list a walker is still using.
        public static List<Entry> Sorted(List<Entry> entries)
        {
            var result = new List<Entry>(entries.Count);
            foreach (var entry in entries)
            {
                if (entry.Tag > 0)
                {
                    result.Add(entry);
                }
            }
            result.Sort(delegate(Entry a, Entry b)
            {
                var byTag = a.Tag.CompareTo(b.Tag);
                // reserved after the member that clashes with it, so `3=Foo; 3=[reserved]` reads in the
                // order the conflict message describes it.
                return byTag != 0 ? byTag : a.IsReserved.CompareTo(b.IsReserved);
            });
            return result;
        }
    }
}
