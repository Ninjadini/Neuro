using System;
using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Ninjadini.Neuro.Editor
{
    /// Repoints `Reference&lt;&gt;` fields that live outside the Neuro data - in prefabs, ScriptableObjects and
    /// scenes - after an item's RefId has been changed.
    ///
    /// NeuroEditorDataProvider.ChangeRefId() only rewrites the json data; anything a Unity asset stored is
    /// still pointing at the old id. This walks every asset's SerializedObject and moves those over too.
    ///
    /// Unlike the data side this is not undoable, so the caller is expected to ask first.
    public static class NeuroAssetRefIdRewriter
    {
        public struct Match
        {
            /// Path of the asset or scene the reference was found in.
            public string AssetPath;
            /// SerializedProperty path of the `Reference&lt;&gt;` within the object.
            public string PropertyPath;
            /// The object holding it - a Component, ScriptableObject, etc. Null once a scene has been closed again.
            public Object Obj;
            /// Where in the scene/prefab hierarchy, when the object is a Component.
            public string ObjectPath;

            public override string ToString()
            {
                return string.IsNullOrEmpty(ObjectPath)
                    ? $"{AssetPath} > {PropertyPath}"
                    : $"{AssetPath} > {ObjectPath} > {PropertyPath}";
            }
        }

        public struct Result
        {
            public List<Match> Matches;
            /// Asset and scene paths that were written to.
            public List<string> ChangedFiles;
            /// Why some of the sweep did not run - scenes skipped, an asset that would not load, etc.
            public List<string> Problems;
        }

        /// Points every `Reference&lt;rootType&gt;` in the project's assets that held `oldRefId` at `newRefId`.
        /// `rootType` is the root referencable type - use NeuroReferences.GetRootReferencable() if you have a subtype.
        /// Set `dryRun` to only report what it would touch.
        public static Result Rewrite(Type rootType, uint oldRefId, uint newRefId, bool includeScenes, bool dryRun = false)
        {
            if (rootType == null)
            {
                throw new ArgumentNullException(nameof(rootType));
            }
            if (oldRefId == 0)
            {
                throw new ArgumentException("RefId `0` means 'no reference', it can not be repointed.", nameof(oldRefId));
            }
            var result = new Result
            {
                Matches = new List<Match>(),
                ChangedFiles = new List<string>(),
                Problems = new List<string>()
            };
            if (oldRefId == newRefId)
            {
                return result;
            }
            try
            {
                RewriteInAssets(rootType, oldRefId, newRefId, dryRun, ref result);
                if (includeScenes)
                {
                    RewriteInScenes(rootType, oldRefId, newRefId, dryRun, ref result);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (!dryRun && result.ChangedFiles.Count > 0)
            {
                AssetDatabase.SaveAssets();
            }
            return result;
        }

        static void RewriteInAssets(Type rootType, uint oldRefId, uint newRefId, bool dryRun, ref Result result)
        {
            // Only under Assets - package contents are not ours to rewrite.
            var searchFolders = new[] { "Assets" };
            var guids = new List<string>(AssetDatabase.FindAssets("t:Prefab", searchFolders));
            guids.AddRange(AssetDatabase.FindAssets("t:ScriptableObject", searchFolders));
            for (var i = 0; i < guids.Count; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }
                EditorUtility.DisplayProgressBar("Repointing references", path, (float)i / guids.Count * 0.5f);
                var changed = false;
                try
                {
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (asset is GameObject gameObject)
                        {
                            foreach (var component in gameObject.GetComponents<Component>())
                            {
                                changed |= RewriteInObject(component, rootType, oldRefId, newRefId, path, dryRun, ref result);
                            }
                        }
                        else if (asset is ScriptableObject scriptableObject)
                        {
                            changed |= RewriteInObject(scriptableObject, rootType, oldRefId, newRefId, path, dryRun, ref result);
                        }
                    }
                }
                catch (Exception e)
                {
                    result.Problems.Add($"Could not scan `{path}`: {e.Message}");
                    Debug.LogException(e);
                    continue;
                }
                if (changed)
                {
                    result.ChangedFiles.Add(path);
                }
            }
        }

        static void RewriteInScenes(Type rootType, uint oldRefId, uint newRefId, bool dryRun, ref Result result)
        {
            if (Application.isPlaying)
            {
                result.Problems.Add("Scenes were not scanned - the editor is in play mode.");
                return;
            }
            if (!dryRun && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                result.Problems.Add("Scenes were not scanned - the currently open scenes have unsaved changes.");
                return;
            }
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }
                EditorUtility.DisplayProgressBar("Repointing references", path, 0.5f + (float)i / guids.Length * 0.5f);
                var scene = SceneManager.GetSceneByPath(path);
                var wasOpen = scene.IsValid() && scene.isLoaded;
                try
                {
                    if (!wasOpen)
                    {
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    }
                }
                catch (Exception e)
                {
                    result.Problems.Add($"Could not open `{path}`: {e.Message}");
                    Debug.LogException(e);
                    continue;
                }
                var changed = false;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var component in root.GetComponentsInChildren<Component>(true))
                    {
                        changed |= RewriteInObject(component, rootType, oldRefId, newRefId, path, dryRun, ref result);
                    }
                }
                if (changed)
                {
                    result.ChangedFiles.Add(path);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                if (!wasOpen)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static bool RewriteInObject(Object obj, Type rootType, uint oldRefId, uint newRefId, string assetPath, bool dryRun, ref Result result)
        {
            if (!obj)
            {
                // A missing script - there is nothing readable to rewrite.
                return false;
            }
            using var serializedObject = new SerializedObject(obj);
            var property = serializedObject.GetIterator();
            var changed = false;
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.Generic)
                {
                    continue;
                }
                var refIdProperty = property.FindPropertyRelative(NeuroConstants.Reference_RefId_FieldName);
                if (refIdProperty == null
                    || refIdProperty.propertyType != SerializedPropertyType.Integer
                    || refIdProperty.uintValue != oldRefId)
                {
                    continue;
                }
                if (!IsReferenceOf(property, rootType))
                {
                    continue;
                }
                result.Matches.Add(new Match
                {
                    AssetPath = assetPath,
                    PropertyPath = property.propertyPath,
                    Obj = obj,
                    ObjectPath = GetObjectPath(obj)
                });
                if (!dryRun)
                {
                    refIdProperty.uintValue = newRefId;
                    changed = true;
                }
            }
            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(obj);
            }
            return changed;
        }

        /// Whether the property really is a `Reference&lt;&gt;` pointing at `rootType` - a field named `RefId` on
        /// some other struct would otherwise be mistaken for one.
        static bool IsReferenceOf(SerializedProperty property, Type rootType)
        {
            object boxed;
            try
            {
                boxed = property.boxedValue;
            }
            catch (Exception)
            {
                // Some generic properties have no resolvable managed type (missing script, unsupported generic).
                return false;
            }
            return boxed is INeuroReference reference
                   && reference.RefType != null
                   && NeuroReferences.GetRootReferencable(reference.RefType) == rootType;
        }

        static string GetObjectPath(Object obj)
        {
            if (obj is not Component component)
            {
                return "";
            }
            var gameObject = component.gameObject;
            var path = $"{gameObject.name} > [{component.GetType().Name}]";
            var parent = gameObject.transform.parent;
            while (parent != null)
            {
                path = parent.name + " > " + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
