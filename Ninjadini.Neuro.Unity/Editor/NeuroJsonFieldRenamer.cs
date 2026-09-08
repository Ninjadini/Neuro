using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ninjadini.Neuro.Sync;
using UnityEditor;

namespace Ninjadini.Neuro.Editor
{
    /// Moves a value from one json field name to another across the NeuroData files.
    ///
    /// JSON is keyed by field name and binary by tag, so renaming a `[Neuro]` field in code is free for
    /// binary data but silently drops the value out of every json file - the old key is just an unknown
    /// field to the reader. This renames the key in the files instead, so the data survives the rename.
    ///
    /// It is a key rename, not a re-serialise: the json is edited in place at the exact character range of
    /// the key, so formatting, field order and everything else in the file is left alone.
    ///
    /// The rename is scoped to one class. The files are walked with the C# types alongside them - each json
    /// object knows which type it belongs to, following `-subType` for polymorphic values - so a field of the
    /// same name on some other class is not touched. A key is only moved when the object does not already
    /// have the new name.
    public class NeuroJsonFieldRenamer
    {
        readonly NeuroEditorDataProvider dataProvider;
        readonly Dictionary<Type, FieldInfo[]> fieldsCache = new Dictionary<Type, FieldInfo[]>();
        readonly Dictionary<Type, Type[]> subTypesCache = new Dictionary<Type, Type[]>();

        public NeuroJsonFieldRenamer(NeuroEditorDataProvider provider = null)
        {
            dataProvider = provider ?? NeuroEditorDataProvider.Shared;
        }

        public struct Match
        {
            public string FilePath;
            /// Where in the file, e.g. `Attack.Projectiles[0]`. Empty for the root object.
            public string JsonPath;
            /// The type of the json object holding the field - `ownerType` or a subclass of it.
            public Type ObjectType;

            public override string ToString()
            {
                var where = string.IsNullOrEmpty(JsonPath) ? ObjectType.Name : $"{JsonPath} ({ObjectType.Name})";
                return $"{FilePath} > {where}";
            }
        }

        public struct Result
        {
            /// Fields that were (or with dryRun, would be) renamed.
            public List<Match> Renamed;
            /// Objects that had the old field but already had the new one too, so were left alone.
            public List<Match> Skipped;
            /// Files that could not be read, parsed, or would not read back after the rename.
            public List<string> Problems;
            public List<string> ChangedFiles;
        }

        /// Renames `oldFieldName` to `newFieldName` on every `ownerType` object in the data files.
        /// `newFieldName` is expected to be a real `[Neuro]` field of `ownerType` - that is what makes the
        /// value readable again - but nothing here requires it.
        /// `dryRun` reports what it would do without writing anything.
        public Result Rename(Type ownerType, string oldFieldName, string newFieldName, bool dryRun)
        {
            if (ownerType == null)
            {
                throw new ArgumentNullException(nameof(ownerType));
            }
            ValidateFieldName(oldFieldName, nameof(oldFieldName));
            ValidateFieldName(newFieldName, nameof(newFieldName));
            if (oldFieldName == newFieldName)
            {
                throw new ArgumentException("The old and new field names are the same.");
            }
            var result = new Result
            {
                Renamed = new List<Match>(),
                Skipped = new List<Match>(),
                Problems = new List<string>(),
                ChangedFiles = new List<string>()
            };
            var files = dataProvider.DataFiles;
            try
            {
                for (var i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    EditorUtility.DisplayProgressBar(dryRun ? "Looking for the field" : "Renaming the field",
                        file.FilePath, (float)i / files.Count);
                    RenameInFile(file, ownerType, oldFieldName, newFieldName, dryRun, ref result);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (!dryRun && result.ChangedFiles.Count > 0)
            {
                dataProvider.Reload();
            }
            return result;
        }

        static void ValidateFieldName(string name, string argName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A field name is required.", argName);
            }
            if (name.StartsWith("-"))
            {
                throw new ArgumentException($"`{name}` starts with `-`, which is reserved for neuro's own json fields.", argName);
            }
        }

        void RenameInFile(NeuroDataFile file, Type ownerType, string oldFieldName, string newFieldName, bool dryRun, ref Result result)
        {
            if (file.RootType == null || string.IsNullOrEmpty(file.FilePath) || !File.Exists(file.FilePath))
            {
                return;
            }
            string json;
            try
            {
                json = File.ReadAllText(file.FilePath);
            }
            catch (Exception e)
            {
                result.Problems.Add($"Could not read `{file.FilePath}`: {e.Message}");
                return;
            }
            var walk = new Walk
            {
                Renamer = this,
                Json = json,
                FilePath = file.FilePath,
                OwnerType = ownerType,
                OldFieldName = oldFieldName,
                NewFieldName = newFieldName,
                Renamed = result.Renamed,
                Skipped = result.Skipped,
                Keys = new List<NeuroJsonTokenizer.StringRange>()
            };
            try
            {
                var nodes = new NeuroJsonTokenizer().Visit(json);
                walk.Nodes = nodes.Array;
                walk.WalkObject(0, nodes.Count, file.RootType, "");
            }
            catch (Exception e)
            {
                result.Problems.Add($"Could not parse `{file.FilePath}`: {e.Message}");
                return;
            }
            if (walk.Keys.Count == 0 || dryRun)
            {
                return;
            }
            // Last one first, so the earlier ranges are still where the tokenizer said they were.
            walk.Keys.Sort((a, b) => b.Start.CompareTo(a.Start));
            var edited = json;
            foreach (var key in walk.Keys)
            {
                edited = edited.Substring(0, key.Start) + newFieldName + edited.Substring(key.End);
            }
            try
            {
                // The point of the rename is that the data reads back - if it does not, this file is left as it was.
                new NeuroJsonReader().ReadObject(edited, file.RootType);
            }
            catch (Exception e)
            {
                result.Problems.Add($"`{file.FilePath}` would not read back after the rename, it is unchanged: {e.Message}");
                return;
            }
            try
            {
                File.WriteAllText(file.FilePath, edited);
            }
            catch (Exception e)
            {
                result.Problems.Add($"Could not write `{file.FilePath}`: {e.Message}");
                return;
            }
            result.ChangedFiles.Add(file.FilePath);
        }

        /// One file's walk - the json text, the tokenizer's nodes and the C# type each node belongs to.
        struct Walk
        {
            public NeuroJsonFieldRenamer Renamer;
            public string Json;
            public NeuroJsonTokenizer.VisitedNode[] Nodes;
            public string FilePath;
            public Type OwnerType;
            public string OldFieldName;
            public string NewFieldName;
            public List<Match> Renamed;
            public List<Match> Skipped;
            /// The key ranges to rewrite in this file.
            public List<NeuroJsonTokenizer.StringRange> Keys;

            /// `start`..`end` is the node range holding one json object's fields.
            public void WalkObject(int start, int end, Type declaredType, string path)
            {
                var type = declaredType;
                var oldNode = -1;
                var hasNew = false;
                for (var i = start; i < end; i = Nodes[i].NextNode)
                {
                    ref var node = ref Nodes[i];
                    if (KeyIs(node, NeuroJsonWriter.FieldName_ClassTag))
                    {
                        type = Renamer.ResolveSubType(declaredType, node.Value.AsSpan(Json));
                    }
                    else if (KeyIs(node, OldFieldName))
                    {
                        oldNode = i;
                    }
                    else if (KeyIs(node, NewFieldName))
                    {
                        hasNew = true;
                    }
                }
                if (oldNode >= 0 && type != null && OwnerType.IsAssignableFrom(type))
                {
                    var match = new Match { FilePath = FilePath, JsonPath = path, ObjectType = type };
                    if (hasNew)
                    {
                        Skipped.Add(match);
                    }
                    else
                    {
                        Renamed.Add(match);
                        Keys.Add(Nodes[oldNode].Key);
                    }
                }
                if (type == null)
                {
                    return;
                }
                for (var i = start; i < end; i = Nodes[i].NextNode)
                {
                    ref var node = ref Nodes[i];
                    if (node.Type != NeuroJsonTokenizer.NodeType.Group && node.Type != NeuroJsonTokenizer.NodeType.Array)
                    {
                        continue;
                    }
                    var key = node.Key.GetSubstring(Json);
                    if (key.Length == 0 || key[0] == '-')
                    {
                        continue;
                    }
                    var field = Renamer.FindField(type, key);
                    if (field != null)
                    {
                        WalkValue(i, field.FieldType, Append(path, key));
                    }
                }
            }

            /// One json value of a known C# type - an object to descend into, or a list/dictionary of them.
            void WalkValue(int nodeIndex, Type valueType, string path)
            {
                ref var node = ref Nodes[nodeIndex];
                var underlying = Nullable.GetUnderlyingType(valueType);
                if (underlying != null)
                {
                    valueType = underlying;
                }
                var start = nodeIndex + 1;
                var end = node.NextNode;
                if (node.Type == NeuroJsonTokenizer.NodeType.Array)
                {
                    var elementType = GetElementType(valueType);
                    if (elementType == null)
                    {
                        return;
                    }
                    var index = 0;
                    for (var i = start; i < end; i = Nodes[i].NextNode)
                    {
                        WalkValue(i, elementType, $"{path}[{index}]");
                        index++;
                    }
                }
                else if (node.Type == NeuroJsonTokenizer.NodeType.Group)
                {
                    // A dictionary is written as a json object too, so its keys are entry keys, not field names.
                    var dictValueType = GetDictionaryValueType(valueType);
                    if (dictValueType != null)
                    {
                        for (var i = start; i < end; i = Nodes[i].NextNode)
                        {
                            WalkValue(i, dictValueType, $"{path}[{Nodes[i].Key.GetSubstring(Json)}]");
                        }
                    }
                    else
                    {
                        WalkObject(start, end, valueType, path);
                    }
                }
            }

            static string Append(string path, string key) => path.Length == 0 ? key : path + "." + key;

            bool KeyIs(in NeuroJsonTokenizer.VisitedNode node, string name)
            {
                return NeuroJsonTokenizer.StringRange.Equals(node.Key, Json, name);
            }
        }

        static Type GetElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return type.GetGenericArguments()[0];
            }
            return null;
        }

        static Type GetDictionaryValueType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                ? type.GetGenericArguments()[1]
                : null;
        }

        /// The type a `-subType` value names, or null when it can not be worked out - which stops the walk
        /// there rather than guessing at the wrong class.
        Type ResolveSubType(Type declaredType, ReadOnlySpan<char> value)
        {
            // Written as `3:MyClass`, or just `3` under Options.TagValuesOnly.
            var separator = value.IndexOf(':');
            var tagSpan = separator >= 0 ? value.Slice(0, separator) : value;
            var name = separator >= 0 ? value.Slice(separator + 1).ToString() : null;
            uint.TryParse(tagSpan, out var tag);
            Type byName = null;
            foreach (var type in GetSubTypes(declaredType))
            {
                if (tag != 0 && (type.GetCustomAttribute<NeuroAttribute>()?.Tag ?? 0) == tag)
                {
                    return type;
                }
                if (name != null && type.Name == name)
                {
                    byName = type;
                }
            }
            return byName;
        }

        Type[] GetSubTypes(Type declaredType)
        {
            if (subTypesCache.TryGetValue(declaredType, out var result))
            {
                return result;
            }
            var list = new List<Type>();
            foreach (var type in NeuroEditorUtils.FindAllNeuroTypesCached())
            {
                if (type != declaredType && declaredType.IsAssignableFrom(type))
                {
                    list.Add(type);
                }
            }
            result = list.ToArray();
            subTypesCache.Add(declaredType, result);
            return result;
        }

        FieldInfo FindField(Type type, string name)
        {
            foreach (var field in GetAllFields(type))
            {
                if (field.Name == name)
                {
                    return field;
                }
            }
            return null;
        }

        FieldInfo[] GetAllFields(Type type)
        {
            if (fieldsCache.TryGetValue(type, out var result))
            {
                return result;
            }
            var list = new List<FieldInfo>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                list.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            }
            result = list.ToArray();
            fieldsCache.Add(type, result);
            return result;
        }

        /// The `[Neuro]` fields of a type, its base classes' included, in declaration order from the base up.
        public static List<FieldInfo> GetNeuroFields(Type type)
        {
            var result = new List<FieldInfo>();
            var types = new List<Type>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                types.Insert(0, t);
            }
            foreach (var t in types)
            {
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsDefined(typeof(NeuroAttribute)))
                    {
                        result.Add(field);
                    }
                }
            }
            return result;
        }
    }
}
