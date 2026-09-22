using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ninjadini.Neuro.Editor
{
    /// Finds where a piece of text turns up in the data: in a field's name (the code side) or in what the
    /// field holds (the data side). Behind the "⌕ Search" button in the Neuro Editor, see
    /// <see cref="NeuroDataSearchPopup"/>; usable from a script as
    /// `new NeuroDataSearch(references).Search(item, "damage", NeuroDataSearch.Scope.Both, results)`.
    ///
    /// The walk is <see cref="NeuroVisitor"/> with primitives on, so every field is seen at the place Neuro
    /// serializes it - inside structs, list elements, dictionary entries, subtypes. Matching is a plain
    /// case-insensitive substring test on strings: a number matches what its `ToString` prints, an enum its
    /// name, a `Reference<>` its target's RefId (as displayed, so base36) and RefName. The item's own RefName
    /// is offered under the name `RefName` even though it is not a `[Neuro]` field, because it is the first
    /// thing anyone searches for.
    ///
    /// A field's name is only tested once, on the field itself - never again on each element of a list it
    /// holds, otherwise a list of fifty would answer a name search fifty times over. A value is only tested on
    /// a leaf: something with no Neuro fields of its own. A struct or list whose name matched shows what it
    /// holds as `{TypeName}` / `{n items}` so the row still reads as a line of the file.
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public class NeuroDataSearch
    {
        [Flags]
        public enum Scope
        {
            FieldNames = 1 << 0,
            Values = 1 << 1,
            Both = FieldNames | Values,
        }

        public readonly struct Match
        {
            /// The item the field belongs to.
            public readonly IReferencable Item;
            /// The table the item is in - what the editor's type dropdown selects.
            public readonly Type RootType;
            /// The field's place in the item, `Rounds[2].Count` - the same form the bulk edit reads.
            public readonly string Path;
            /// The field's own name, the last step of <see cref="Path"/>.
            public readonly string Name;
            /// What the field holds, as text. A container shows as `{n items}` or `{TypeName}`.
            public readonly string Value;
            public readonly bool NameMatched;
            public readonly bool ValueMatched;

            public Match(IReferencable item, Type rootType, string path, string name, string value, bool nameMatched, bool valueMatched)
            {
                Item = item;
                RootType = rootType;
                Path = path;
                Name = name;
                Value = value;
                NameMatched = nameMatched;
                ValueMatched = valueMatched;
            }
        }

        readonly NeuroVisitor walk = new NeuroVisitor();
        readonly Visitor visitor;

        /// <paramref name="references"/> is what a `Reference<>` field's RefName is looked up in; null shows
        /// the RefId alone.
        public NeuroDataSearch(NeuroReferences references)
        {
            visitor = new Visitor(references);
        }

        /// Appends every field of <paramref name="item"/> that answers <paramref name="term"/> to
        /// <paramref name="results"/>, in the order the fields are serialized. Returns how many were added.
        /// An empty term matches nothing. Stops adding once <paramref name="results"/> holds
        /// <paramref name="maxResults"/> entries.
        public int Search(IReferencable item, string term, Scope scope, List<Match> results, int maxResults = int.MaxValue)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (string.IsNullOrEmpty(term) || scope == 0 || results.Count >= maxResults)
            {
                return 0;
            }
            var before = results.Count;
            visitor.Begin(item, term, scope, results, maxResults);
            try
            {
                walk.Visit(item, visitor, true);
            }
            finally
            {
                visitor.End();
            }
            return results.Count - before;
        }

        /// <see cref="Search(IReferencable,string,Scope,List{Match},int)"/> over several items.
        public int Search(IEnumerable<IReferencable> items, string term, Scope scope, List<Match> results, int maxResults = int.MaxValue)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var count = 0;
            foreach (var item in items)
            {
                if (results.Count >= maxResults)
                {
                    break;
                }
                count += Search(item, term, scope, results, maxResults);
            }
            return count;
        }

        public static bool Contains(string text, string term)
        {
            return !string.IsNullOrEmpty(text) && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// The one-line text for a leaf value: a string as is, a number as it prints, a bool as json spells it,
        /// an enum by name. Anything else with its own `ToString` uses that (Unity's Vector3, Toolkit's fp);
        /// what is left is written as json, which is what the file holds anyway.
        public static string DescribeValue(object value, NeuroReferences references)
        {
            switch (value)
            {
                case null:
                    return "null";
                case string s:
                    return s;
                case bool b:
                    return b ? "true" : "false";
                case IFormattable formattable when value.GetType().IsPrimitive:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                case Enum e:
                    return e.ToString();
            }
            var type = value.GetType();
            if (OverridesToString(type))
            {
                return value.ToString();
            }
            try
            {
                return new NeuroJsonWriter(references).WriteObject(value, references, NeuroJsonWriter.Options.TagValuesOnly);
            }
            catch (Exception)
            {
                return "{" + type.Name + "}";
            }
        }

        static readonly Dictionary<Type, bool> overridesToString = new Dictionary<Type, bool>();

        static bool OverridesToString(Type type)
        {
            if (!overridesToString.TryGetValue(type, out var result))
            {
                var declaringType = type.GetMethod("ToString", Type.EmptyTypes)?.DeclaringType;
                result = declaringType != null && declaringType != typeof(object) && declaringType != typeof(ValueType);
                overridesToString[type] = result;
            }
            return result;
        }

        class Visitor : NeuroVisitor.IInterface
        {
            struct Frame
            {
                public string Name;
                public int? ListIndex;
                public object Value;
                /// A `Reference<>` fills this in from VisitRef; everything else is described from Value when
                /// the frame turns out to be a leaf.
                public string Text;
                public bool HasChildren;
            }

            readonly NeuroReferences references;
            readonly List<Frame> stack = new List<Frame>();
            readonly StringBuilder pathBuilder = new StringBuilder();

            IReferencable item;
            Type rootType;
            string term;
            Scope scope;
            List<Match> results;
            int maxResults;

            public Visitor(NeuroReferences references)
            {
                this.references = references;
            }

            public void Begin(IReferencable item, string term, Scope scope, List<Match> results, int maxResults)
            {
                this.item = item;
                this.term = term;
                this.scope = scope;
                this.results = results;
                this.maxResults = maxResults;
                rootType = NeuroReferences.GetRootReferencable(item.GetType());
                stack.Clear();
                if ((scope & Scope.Values) != 0 && !(item is ISingletonReferencable) && Contains(item.RefName, term))
                {
                    Add("RefName", "RefName", item.RefName, false, true);
                }
            }

            public void End()
            {
                item = null;
                results = null;
                stack.Clear();
            }

            void NeuroVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex)
            {
                if (stack.Count > 0)
                {
                    var parent = stack[stack.Count - 1];
                    parent.HasChildren = true;
                    stack[stack.Count - 1] = parent;
                }
                stack.Add(new Frame { Name = name, ListIndex = listIndex, Value = obj });
            }

            void NeuroVisitor.IInterface.VisitRef<T>(ref Reference<T> reference)
            {
                if (stack.Count == 0)
                {
                    return;
                }
                var frame = stack[stack.Count - 1];
                var id = NeuroEditorUtils.DisplayRefId(reference.RefId);
                var refName = reference.RefId != 0 ? references?.GetTable(typeof(T))?.GetRefName(reference.RefId) : null;
                frame.Text = string.IsNullOrEmpty(refName) ? id : $"{id} : {refName}";
                stack[stack.Count - 1] = frame;
            }

            void NeuroVisitor.IInterface.EndVisit()
            {
                var frame = stack[stack.Count - 1];
                if (string.IsNullOrEmpty(frame.Name) || results.Count >= maxResults)
                {
                    // The unnamed frames are the root object itself; the name-less walk around a global type
                    // gives it two.
                    stack.RemoveAt(stack.Count - 1);
                    return;
                }
                var nameMatched = (scope & Scope.FieldNames) != 0 && frame.ListIndex == null && Contains(frame.Name, term);
                var isLeaf = !frame.HasChildren && frame.Text == null && !(frame.Value is ICollection);
                string text = null;
                var valueMatched = false;
                if (frame.Text != null || isLeaf)
                {
                    text = frame.Text ?? DescribeValue(frame.Value, references);
                    valueMatched = (scope & Scope.Values) != 0 && Contains(text, term);
                }
                if (nameMatched || valueMatched)
                {
                    text ??= DescribeContainer(frame.Value);
                    Add(BuildPath(), frame.Name, text, nameMatched, valueMatched);
                }
                stack.RemoveAt(stack.Count - 1);
            }

            static string DescribeContainer(object value)
            {
                if (value is ICollection collection)
                {
                    return "{" + collection.Count + (collection.Count == 1 ? " item}" : " items}");
                }
                return "{" + (value?.GetType().Name ?? "null") + "}";
            }

            /// `Rounds[2].Count`: a list element is `[i]` on the list's own name, the same form as
            /// <see cref="NeuroVisitor.GeneratePathFromStack"/> and the bulk edit's NeuroFieldPath.
            string BuildPath()
            {
                pathBuilder.Clear();
                foreach (var frame in stack)
                {
                    if (string.IsNullOrEmpty(frame.Name))
                    {
                        continue;
                    }
                    if (frame.ListIndex.HasValue)
                    {
                        pathBuilder.Append('[').Append(frame.ListIndex.Value).Append(']');
                    }
                    else
                    {
                        if (pathBuilder.Length > 0)
                        {
                            pathBuilder.Append('.');
                        }
                        pathBuilder.Append(frame.Name);
                    }
                }
                return pathBuilder.ToString();
            }

            void Add(string path, string name, string value, bool nameMatched, bool valueMatched)
            {
                if (results.Count < maxResults)
                {
                    results.Add(new Match(item, rootType, path, name, value, nameMatched, valueMatched));
                }
            }
        }
    }
}
