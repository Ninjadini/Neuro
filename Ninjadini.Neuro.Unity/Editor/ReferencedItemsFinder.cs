using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Ninjadini.Neuro.Editor
{
    public class ReferencedItemsFinder
    {
        public class SearchReferencesTask
        {
            IReferencable _searchingObj;
            List<(IReferencable referencable, string path)> _result;
            CancellationTokenSource _cancellationTokenSource;
            
            public bool Busy => _searchingObj != null && _result == null;
            public IReadOnlyList<(IReferencable referencable, string path)> Result => _result;

            public static SearchReferencesTask Start(IReferencable obj, NeuroReferences references)
            {
                var task = new SearchReferencesTask();
                task._searchingObj = obj;
                task._cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = task._cancellationTokenSource.Token;
                // The snapshot is taken here, on the calling (main) thread. Reading a table deserializes the
                // items it was still holding as lazy loaders, which reads files and writes back into the table -
                // none of which may happen on a background thread while the editor is using the same tables.
                var allItems = CollectAllReferencables(references);
                ThreadPool.QueueUserWorkItem((state) =>
                {
                    task._result = SearchInReferences(obj, allItems, cancellationToken);
                }, cancellationToken);
                return task;
            }

            public void Cancel()
            {
                _cancellationTokenSource?.Cancel();
            }

            public void DrawResultToUI(VisualElement visualElement, Action<IReferencable> callback)
            {
                if (Busy || _result == null)
                {
                    return;
                }
                foreach (var refObj in _result)
                {
                    var btn = new Button();
                    if (refObj.referencable is ISingletonReferencable)
                    {
                        btn.text = $"[{refObj.referencable.GetType().Name}] > {refObj.path}";
                    }
                    else
                    {
                        btn.text = $"[{refObj.referencable.GetType().Name}] {NeuroEditorUtils.DisplayIdAndName(refObj.referencable)} > {refObj.path}";
                    }
                    btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                    var localRefObj = refObj.referencable;
                    btn.clicked += delegate
                    {
                        callback(localRefObj);
                    };
                    visualElement.Add(btn);
                }
                if (_result.Count == 0)
                {
                    visualElement.Add(new Label("No references found"));
                }
                
                var searchInPrefabsResult = new VisualElement();
                var searchResultLbl = new Label();
                searchResultLbl.style.paddingTop = 5;
                searchResultLbl.style.paddingLeft = 5;

                var searchInPrefabsBtn = new Button();
                searchInPrefabsBtn.text = "Search in Unity objects (not scenes)";
                searchInPrefabsBtn.clicked += delegate
                {
                    var prefabPaths = SearchInPrefabs(_searchingObj);
                    searchInPrefabsResult.Clear();
                    searchInPrefabsResult.Add(searchResultLbl);
                    foreach (var match in prefabPaths)
                    {
                        var btn = new Button();
                        var obj = match.obj;
                        btn.text = GetPath(obj) + " > " + match.Item2;
                        btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                        if (obj)
                        {
                            btn.clicked += () =>
                            {
                                EditorUtility.FocusProjectWindow();
                                EditorGUIUtility.PingObject(obj);
                            };
                        }
                        else
                        {
                            btn.SetEnabled(false);
                        }
                        searchInPrefabsResult.Add(btn);
                    }

                    if (prefabPaths.Count > 0)
                    {
                        searchResultLbl.text = prefabPaths.Count + " references found in Unity objects...";
                    }
                    else
                    {
                        var searchingType = NeuroReferences.GetRootReferencable(_searchingObj.GetType());
                        searchResultLbl.text = $"No references found in Unity objects.\nWe looked for serialized fields with type:\n<b>Reference<{searchingType}></b>\n- in Prefabs and ScriptableObjects";
                    }
                    if (searchInPrefabsResult.parent == null)
                    {
                        visualElement.Add(searchInPrefabsResult);
                        searchInPrefabsResult.PlaceBehind(searchInPrefabsBtn);
                    }
                };
                visualElement.Add(searchInPrefabsBtn);
            }
        }

        public static List<(Object obj, string path)> SearchInPrefabs(IReferencable referencable)
        {
            const string progressTitle = "Searching";
            try
            {
                EditorUtility.DisplayProgressBar(progressTitle, "Loading prefabs...", 0f);
                var allObjects = new List<Object>();
                var allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
                foreach (var prefabGuid in allPrefabGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!prefab)
                    {
                        continue;
                    }
                    allObjects.AddRange(prefab.GetComponentsInChildren<Component>(true));
                }
                EditorUtility.DisplayProgressBar(progressTitle, "Loading ScriptableObject...", 0.6f);
                var allSOGuids = AssetDatabase.FindAssets("t:ScriptableObject");
                foreach (var soGuid in allSOGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(soGuid);
                    try
                    {
                        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                        {
                            if (obj is ScriptableObject scriptableObject)
                            {
                                allObjects.Add(scriptableObject);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                       Debug.LogException(e);
                    }
                }
                EditorUtility.DisplayProgressBar(progressTitle, "Scanning inside objects...", 0.8f);
                return SearchInObjects(referencable, allObjects.Distinct());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        
        public static List<(Object obj, string path)> SearchInObjects(IReferencable referencable, IEnumerable<Object> objects)
        {
            var searchingType = NeuroReferences.GetRootReferencable(referencable.GetType());
            var searchingRefId = referencable.RefId;
            var result = new List<(Object, string)>();
            
            foreach (var obj in objects)
            {
                if (!obj)
                {
                    continue;
                }
                var serializedObject = new SerializedObject(obj);
                var property = serializedObject.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.Generic)
                    {
                        var field = obj.GetType().GetField(property.propertyPath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field == null)
                        {
                            continue;
                        }
                        var value = field.GetValue(obj);
                        if(value is INeuroReference reference)
                        {
                            if (reference.RefType == searchingType && reference.RefId == searchingRefId)
                            {
                                result.Add((obj, property.propertyPath));
                            }
                        }
                        else if(value is IList list)
                        {
                            for (var index = 0; index < list.Count; index++)
                            {
                                var childObj = list[index];
                                if (childObj is INeuroReference childReference && childReference.RefType == searchingType && childReference.RefId == searchingRefId)
                                {
                                    result.Add((obj, $"{property.propertyPath}[{index}]"));
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
        
        static string GetPath(Object obj)
        {
            if (obj is not Component component)
            {
                return obj.name;
            }
            var gameObject = component.gameObject;
            var path = $"{gameObject.name} > [{component.GetType().Name}]";
            var parent = gameObject.transform.parent;
            while (parent != null)
            {
                path = parent.name + " > " + path;
                parent = parent.parent;
            }
            if(gameObject.scene.name != null)
            {
                path = gameObject.scene.name + " (scene) > " + path;
            }
            return path;
        }   
        
        public static List<(IReferencable referencable, string path)> SearchInReferences(IReferencable obj, NeuroReferences references, CancellationToken? cancellationToken = null)
        {
            return SearchInReferences(obj, CollectAllReferencables(references), cancellationToken);
        }

        /// Every item in the database, fully loaded. Must be called from the main thread - see SearchReferencesTask.
        static List<IReferencable> CollectAllReferencables(NeuroReferences references)
        {
            var result = new List<IReferencable>();
            // ToArray twice because reading a table deserializes the lazily loaded items, which writes to the
            // very tables (and table list) that would otherwise still be being enumerated.
            foreach (var baseType in references.GetRegisteredBaseTypes().ToArray())
            {
                result.AddRange(references.GetTable(baseType).SelectAll().ToArray());
            }
            return result;
        }

        static List<(IReferencable referencable, string path)> SearchInReferences(IReferencable obj, List<IReferencable> allReferencables, CancellationToken? cancellationToken)
        {
            var neuroVisitor = new NeuroVisitor();
            var visitor = new Visitor(obj);
            var result = new List<(IReferencable referencable, string path)>();
            foreach (var referencable in allReferencables)
            {
                if (cancellationToken is { IsCancellationRequested: true })
                {
                    return result;
                }
                if (referencable == obj)
                {
                    continue;
                }
                neuroVisitor.Visit(referencable, visitor);
                foreach (var path in visitor.Paths)
                {
                    result.Add((referencable, path));
                }
                visitor.Reset();
            }
            return result;
        }

        class Visitor : NeuroVisitor.IInterface
        {
            IReferencable referencable;
            Type rootType;

            public readonly List<string> Paths = new List<string>();
            public readonly List<NeuroVisitor.StackItem> Stack = new ();

            public Visitor(IReferencable reference)
            {
                referencable = reference;
                rootType = NeuroReferences.GetRootReferencable(reference.GetType());
            }

            public void Reset()
            {
                Paths.Clear();
                Stack.Clear();
            }
            
            void NeuroVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex)
            {
                if (obj != null)
                {
                    Stack.Add(new NeuroVisitor.StackItem
                    {
                        Object = obj,
                        Name = name,
                        ListIndex = listIndex
                    });
                }
            }

            void NeuroVisitor.IInterface.EndVisit()
            {
                if (Stack.Count > 0)
                {
                    Stack.RemoveAt(Stack.Count - 1);
                }
            }

            void NeuroVisitor.IInterface.VisitRef<T>(ref Reference<T> reference)
            {
                if (reference.RefId == referencable.RefId && typeof(T) == rootType)
                {
                    Paths.Add(NeuroVisitor.GeneratePathFromStack(Stack));
                }
            }
        }
    }
}