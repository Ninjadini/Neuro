using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    /// Walks every item in the neuro data and makes each asset an AssetAddress points at Addressable,
    /// unless it lives under a Resources folder. Catches addresses written without going through the
    /// inspector's picker (editor scripts, evals, hand-edited JSON), which would otherwise only fail to load in a build.
    public static class AssetAddressablesFixer
    {
        [MenuItem("Tools/Neuro/Make Asset Addresses Addressable", priority = 700)]
        public static void MenuItem()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("", "Addressables not set up. Go to 'Window → Asset Management → Addressables → Groups' first.", "OK");
                return;
            }
            var missing = FindNonAddressable(NeuroEditorDataProvider.SharedReferences, settings, out var unresolved);
            foreach (var address in unresolved)
            {
                Debug.LogWarning($"Neuro ~ AssetAddress `{address.Key}` does not resolve to an asset. Used by {address.Value}");
            }
            if (missing.Count == 0)
            {
                EditorUtility.DisplayDialog("", "Every asset referenced by an AssetAddress is already Addressable or in Resources.", "OK");
                return;
            }
            var message = new StringBuilder();
            message.AppendLine($"{missing.Count} asset(s) referenced by an AssetAddress are neither Addressable nor in Resources:\n");
            foreach (var path in missing.Keys.Take(15))
            {
                message.AppendLine(path);
            }
            if (missing.Count > 15)
            {
                message.AppendLine($"... and {missing.Count - 15} more");
            }
            message.Append($"\nAdd them to the '{settings.DefaultGroup.Name}' group?");
            if (!EditorUtility.DisplayDialog("Make Addressable", message.ToString(), "Make Addressable", "Cancel"))
            {
                return;
            }
            MakeAddressable(settings, missing);
        }

        /// Asset path → the first data item (and field path) found referencing it, for every asset that
        /// needs to be Addressable and isn't. `unresolved` gets addresses that match no asset at all.
        public static Dictionary<string, string> FindNonAddressable(NeuroReferences references, AddressableAssetSettings settings, out Dictionary<string, string> unresolved)
        {
            var visitor = new Visitor();
            var neuroVisitor = new NeuroVisitor();
            // ToArray: reading a table deserializes lazily loaded items, which writes to the tables being enumerated.
            foreach (var baseType in references.GetRegisteredBaseTypes().ToArray())
            {
                foreach (var referencable in references.GetTable(baseType).SelectAll().ToArray())
                {
                    visitor.Current = referencable;
                    neuroVisitor.Visit(referencable, visitor);
                }
            }

            var result = new Dictionary<string, string>();
            unresolved = new Dictionary<string, string>();
            foreach (var (address, usedBy) in visitor.Found)
            {
                AssetAddressEditorUtils.ExtractPathAndSubAsset(address, out var guidOrPath, out _);
                var path = AssetDatabase.GUIDToAssetPath(guidOrPath);
                if (string.IsNullOrEmpty(path))
                {
                    if (!Resources.Load(guidOrPath))
                    {
                        unresolved[address] = usedBy;
                    }
                    continue;
                }
                if (AssetAddressEditorUtils.IsInResourcePath(path) || settings.FindAssetEntry(guidOrPath) != null)
                {
                    continue;
                }
                result.TryAdd(path, usedBy);
            }
            return result;
        }

        static void MakeAddressable(AddressableAssetSettings settings, Dictionary<string, string> assets)
        {
            var group = settings.DefaultGroup;
            var entries = new List<AddressableAssetEntry>();
            foreach (var (path, usedBy) in assets)
            {
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group, postEvent: false);
                if (entry == null)
                {
                    Debug.LogWarning($"Neuro ~ Could not make {path} Addressable. Used by {usedBy}");
                    continue;
                }
                // Same key AssetAddressEditorUtils.MakeAddressable gives - only a label, loads go by GUID.
                entry.address = System.IO.Path.GetFileNameWithoutExtension(path);
                entries.Add(entry);
                Debug.Log($"Neuro ~ Made {path} Addressable. Used by {usedBy}", AssetDatabase.LoadMainAssetAtPath(path));
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"Neuro ~ Made {entries.Count} asset(s) Addressable in group '{group.Name}'");
        }

        class Visitor : NeuroVisitor.IInterface
        {
            public IReferencable Current;
            /// Address → first "#id:name (Type) > field.path" seen using it.
            public readonly Dictionary<string, string> Found = new();
            readonly List<NeuroVisitor.StackItem> stack = new();

            void NeuroVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex)
            {
                stack.Add(new NeuroVisitor.StackItem { Object = obj, Name = name, ListIndex = listIndex });
                if (obj is AssetAddress assetAddress && assetAddress.HasAddress() && !Found.ContainsKey(assetAddress.Address))
                {
                    Found[assetAddress.Address] = $"{NeuroEditorUtils.DisplayIdAndName(Current)} ({Current.GetType().Name}) > {NeuroVisitor.GeneratePathFromStack(stack)}";
                }
            }

            void NeuroVisitor.IInterface.EndVisit()
            {
                if (stack.Count > 0)
                {
                    stack.RemoveAt(stack.Count - 1);
                }
            }

            void NeuroVisitor.IInterface.VisitRef<T>(ref Reference<T> reference)
            {
            }
        }
    }
}
