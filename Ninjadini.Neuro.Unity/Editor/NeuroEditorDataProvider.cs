using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    [InitializeOnLoad]
    public class NeuroEditorDataProvider : IReferencesProvider
    {
        static NeuroEditorDataProvider()
        {
            if (NeuroReferences.Default == null)
            {
                NeuroReferences.Default = new NeuroReferences();
            }
            NeuroDataProvider.Shared.SetReferenceProvider(new NeuroEditorDataProviderHook());
        }

        static NeuroEditorDataProvider _shared;
        public static NeuroEditorDataProvider Shared
        {
            get
            {
                if (_shared == null)
                {
                    _shared = new NeuroEditorDataProvider(NeuroReferences.Default);
                    _shared.LoadFromProject();
                }
                return _shared;
            }
        }
        
        public static NeuroReferences SharedReferences => Shared.References;

        public bool HadProblemsLoading;
        List<NeuroDataFile> dataFiles;
        NeuroJsonReader jsonReader;
        NeuroJsonWriter jsonWriter;
        readonly List<FileSystemWatcher> fileSystemWatchers = new ();
        Dictionary<string, DateTime> ignoreFileChangesExpiry = new Dictionary<string, DateTime>();

        bool loadedFromProject;
        public readonly NeuroReferences References;
        public IReadOnlyList<NeuroDataFile> DataFiles => dataFiles;
        public NeuroJsonReader JsonReader => jsonReader;

        public NeuroEditorDataProvider(NeuroReferences references)
        {
            References = references ?? throw new ArgumentNullException(nameof(references));
            jsonReader = new NeuroJsonReader();
            jsonWriter = new NeuroJsonWriter();
        }

        NeuroReferences IReferencesProvider.References => References;

        public virtual void Reload()
        {
            if (loadedFromProject)
            {
                fileChangesCount = 0;
                References.Clear();
                LoadFromProject();
            }
            else
            {
                throw new Exception($"This {GetType().Name} was not loaded from project files, therefore there is nothing to reload.");
            }
        }

        void LoadFromProject()
        {
            HadProblemsLoading = false;
            loadedFromProject = true;
            NeuroSyncTypes.TryRegisterAllAssemblies();
            dataFiles = new List<NeuroDataFile>();
            var settings = NeuroUnityEditorSettings.Get();
            ClearAllFileWatchers();
            var dataPaths = new List<string>() { settings.PrimaryDataPath };
            foreach (var classSetting in settings.ClassSettings)
            {
                if (!string.IsNullOrEmpty(classSetting.DataPath) && !dataPaths.Contains(classSetting.DataPath))
                {
                    dataPaths.Add(classSetting.DataPath);
                }
            }
            LoadDirectories(dataPaths);
        }

        void LoadDirectories(List<string> dataPaths)
        {
            var count = 0;
            var startTime = DateTime.UtcNow;
            foreach (var dirPath in dataPaths)
            {
                if (!Directory.Exists(dirPath))
                {
                    if (dirPath == NeuroUnityEditorSettings.DEFAULT_DATA_PATH)
                    {
                        Directory.CreateDirectory(NeuroUnityEditorSettings.DEFAULT_DATA_PATH);
                    }
                    else
                    {
                        Debug.LogError("Neuro data path does not exist: " + Path.GetFullPath(dirPath));
                        continue;
                    }
                }
                AddFileWatchers(dirPath);
                foreach (var subDir in Directory.GetDirectories(dirPath))
                {
                    var typeId = NeuroDataFile.ReadIdFromFileName(subDir);
                    if (typeId > 0)
                    {
                        var globalType = NeuroGlobalTypes.FindTypeById(typeId);
                        if (globalType != null)
                        {
                            foreach (var filePath in Directory.GetFiles(subDir, "*.json", SearchOption.TopDirectoryOnly))
                            {
                                count++;
                                LoadFile(globalType, filePath);
                            }
                        }
                    }
                }
            }
            Debug.Log($"Neuro ~ Found {count:N0} json files in {(DateTime.UtcNow - startTime).TotalMilliseconds:N0} ms");
        }

        void LoadFile(Type globalType, string filePath)
        {
            try
            {
                var fileData = new NeuroDataFile(globalType, filePath, this);
                var refId = fileData.RefId;
                if (refId == 0)
                {
                    Debug.LogError("Neuro data file has RefId 0 @ " + filePath);
                    return;
                }
                var type = fileData.RootType;
                if (References.Get(type, refId) != null)
                {
                    Debug.LogError($"Neuro data file with duplicate RefId `{refId}` found @ {filePath}");
                    return;
                }
                References.GetTable(type).Register(refId, fileData);
                dataFiles.Add(fileData);
            }
            catch (Exception)
            {
                HadProblemsLoading = true;
                throw;
            }
        }

        void ClearAllFileWatchers()
        {
            foreach (var fileSystemWatcher in fileSystemWatchers)
            {
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Dispose();
            }
            fileSystemWatchers.Clear();
        }

        void AddFileWatchers(string dirPath)
        {
            var watcher = new FileSystemWatcher(dirPath);
            watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileChanged;
            watcher.Filter = "*.json";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            fileSystemWatchers.Add(watcher);
        }
        
        void OnFileChanged(object sender, FileSystemEventArgs fileArgs)
        {
            var fullPath = fileArgs.FullPath;
            if(ignoreFileChangesExpiry.TryGetValue(fullPath, out var ignoreUntil) && ignoreUntil > DateTime.UtcNow)
            {
                return;
            }
            fileChangesCount++;
            if (updatesCountSinceFilesChanged < 0 && NeuroUnityEditorSettings.Get().ShowDialogOnDataFileChange)
            {
                updatesCountSinceFilesChanged = 0;
                EditorApplication.update += OnEditorUpdateForFileChanges;
            }
        }

        public bool HasPendingFileChanges => fileChangesCount > 0;

        int fileChangesCount;
        int updatesCountSinceFilesChanged = -1;

        void OnEditorUpdateForFileChanges()
        {
            updatesCountSinceFilesChanged++;
            if (updatesCountSinceFilesChanged <= 5)
            {
                return;
            }
            EditorApplication.update -= OnEditorUpdateForFileChanges;
            updatesCountSinceFilesChanged = -1;
            if (fileChangesCount <= 0)
            {
                return;
            }
            if (EditorUtility.DisplayDialog(
                    "",
                    $"~{fileChangesCount} data files may have changed. \nWould you like to reload Neuro data?",
                    "YES", 
                    "Later"))
            {
                Reload();
            }
        }

        public NeuroDataFile Find(Type type, uint id)
        {
            type = NeuroReferences.GetRootReferencable(type);
            var typeIsClass = type.IsClass;
            return DataFiles.FirstOrDefault(f =>
            {
                var itemType = f.RootType;
                if (itemType == null)
                {
                    return false;
                }
                return f.RefId == id && (itemType == type || (typeIsClass
                        ? itemType.IsSubclassOf(type)
                        : type.IsAssignableFrom(itemType))
                        );
            });
        }

        public uint FindNextId(Type type)
        {
            var nextId = 1u;
            var keys = References.GetTable(type).GetIds();
            foreach (var key in keys)
            {
                nextId = Math.Max(nextId, key + 1);
            }
            return nextId;
        }

        public NeuroDataFile Add(IReferencable newObj, uint customRefId = 0)
        {
            var type = NeuroReferences.GetRootReferencable(newObj.GetType());
            uint nextId;
            if (customRefId > 0)
            {
                nextId = customRefId;
                if (References.Get(newObj.GetType(), customRefId) != null)
                {
                    throw new Exception($"Custom RefId `{customRefId}` is already in use for type `{newObj.GetType()}`");
                }
            }
            else
            {
                nextId = FindNextId(type);
            }
            newObj.RefId = nextId;
            var resultId = newObj.RefId;
            if (resultId != nextId)
            {
                Debug.LogError($"Tried to assign {newObj.GetType().Name}'s RefId to `{nextId}` but it is still `{resultId}`");
                return null;
            }
            var fileName = GetFileName(newObj)+".json";
            var dir = GetDirForType(type);
            var result = new NeuroDataFile(type, Path.Combine(dir, fileName), this)
            {
                Value = newObj
            };
            dataFiles.Add(result);
            References.Register(newObj);
            SaveData(result);
            return result;
        }

        string GetDirForType(Type type)
        {
            var settings = NeuroUnityEditorSettings.Get();
            var dir = settings.PrimaryDataPath;
            var typeSetting = settings.FindTypeSetting(type);
            if(typeSetting != null && !string.IsNullOrEmpty(typeSetting.DataPath))
            {
                dir = typeSetting.DataPath;
            }
            var typeId = NeuroGlobalTypes.GetIdByType(type);
            return Path.Combine(dir, typeId +"-"+type.Name);
        }

        public void SaveData(IReferencable data)
        {
            if (data == null)
            {
                return;
            }
            var type = NeuroReferences.GetRootReferencable(data.GetType());
            var refId = data.RefId;
            
            var existingObj = References.Get(type, refId);
            if(existingObj != data)
            {
                var table = References.GetTable(type);
                table.Unregister(refId);
                table.Register(data);
            }
            var dataFile = Find(type, refId);
            if (dataFile != null)
            {
                SaveData(dataFile);
            }
            else
            {
                Debug.LogWarning("Data file not found for " + type +" with id " + data.RefId);
            }
        }

        public void SaveData(NeuroDataFile dataFile)
        {
            if (string.IsNullOrEmpty(dataFile.FilePath))
            {
                return;
            }
            var value = dataFile.Value as object;
            if (value == null)
            {
                throw new Exception($"Null data file value for {dataFile.FilePath}. Please use Delete() instead");
            }
            else
            {
                var dir = Path.GetDirectoryName(dataFile.FilePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var json = jsonWriter.Write(value, refs:References, options:NeuroJsonWriter.Options.ExcludeTopLevelGlobalType);
                AddTempIgnoreFile(dataFile.FilePath);
                File.WriteAllText(dataFile.FilePath, json);
            }
        }

        public void Delete(NeuroDataFile dataFile)
        {
            var value = dataFile.Value;
            if (value != null)
            {
                References.GetTable(value.GetType()).Unregister(value.RefId);
            }
            //dataFile.Value = null;
            if (!string.IsNullOrEmpty(dataFile.FilePath) && File.Exists(dataFile.FilePath))
            {
                AddTempIgnoreFile(dataFile.FilePath);
                File.Delete(dataFile.FilePath);
            }
            dataFiles.Remove(dataFile);
        }

        public void SetRefName(NeuroDataFile dataFile, string newName)
        {
            var obj = dataFile.Value;
            obj.RefName = newName ?? "";
            var fileName = GetFileName(obj)+".json";
            var dir = Path.GetDirectoryName(dataFile.FilePath);
            var newPath = Path.Combine(dir, fileName);
            if (!string.IsNullOrEmpty(dataFile.FilePath) && File.Exists(dataFile.FilePath))
            {
                AddTempIgnoreFile(dataFile.FilePath);
                File.Delete(dataFile.FilePath);
            }
            AddTempIgnoreFile(newPath);
            dataFile.SetFilePath(newPath);
            SaveData(dataFile);
        }

        void AddTempIgnoreFile(string filePath)
        {
            var timeNow = DateTime.UtcNow;
            var fullPath = Path.GetFullPath(filePath);
            ignoreFileChangesExpiry[fullPath] = timeNow.AddMilliseconds(1000);

            if (ignoreFileChangesExpiry.Count > 100)
            {
                ignoreFileChangesExpiry = ignoreFileChangesExpiry
                    .Where(kv => kv.Value > timeNow)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }
        }
        
        public string GetFileName(IReferencable referencable)
        {
            if (referencable is ISingletonReferencable)
            {
                return "1-"+referencable.GetType().Name;
            }
            var name = Regex.Replace(referencable.RefName ?? "", NeuroDataFile.InvalidFileNameRegExp, "");
            if (string.IsNullOrEmpty(name))
            {
                return referencable.RefId.ToString();
            }
            return referencable.RefId + "-" + (name.Length > 64 ? name.Substring(0, 64) : name);
        }

        public void SaveBundledBinaryToResources(BuildReport report)
        {
            var settings = NeuroUnityEditorSettings.Get();
            var resDir = settings.ResourcesDir;
            if (string.IsNullOrEmpty(resDir))
            {
                Debug.LogError("Resources folder not defined in Neuro Settings.");
                return;
            }
            var allData = CollectAllReferencesForBaking(report);
            
            var bytes = new NeuroBytesWriter().WriteReferencesList(allData.AsSpan()).ToArray();
            bytes = RawProtoWriter.Compress(bytes);
            var path = Path.Combine(resDir, NeuroDataProvider.BinaryResourceName + "." + NeuroDataProvider.BinaryResourceExtension);
            Debug.Log($"Neuro: SaveBinaryToResources @ {path}. bytes: {bytes.Length:N0}");
            if (!Directory.Exists(resDir))
            {
                Directory.CreateDirectory(resDir);
            }
            File.WriteAllBytes(path, bytes);
            Reload();
            AssetDatabase.Refresh();
        }
        
        IReferencable[] CollectAllReferencesForBaking(BuildReport report = null)
        {
            Reload();
            var settings = NeuroUnityEditorSettings.Get();
            var allProcessors = NeuroEditorUtils.CreateFromScannableTypes<INeuroBundledDataResourcesForBuildProcessor>();
            foreach (var processor in allProcessors)
            {
                processor.PrepBeforeBuildProcessing(References, report);
            }
            return DataFiles
                .Select(d => d.Value)
                .Where(d => d != null && d.RefId > 0)
                .Where(d => settings.FindTypeSetting(d.GetType())?.BakeToResources ?? true )
                .Where(d => allProcessors.All(processor => processor.ProcessForInclusion(d)))
                .ToArray();
        }
        

        public void SaveBakedDataAsJson(string savePath = null)
        {
            var stringBuilder = new System.Text.StringBuilder();
            stringBuilder.Append("[\n");
            var allData = CollectAllReferencesForBaking();

            var jsonWriter = new NeuroJsonWriter();
            foreach (var referencable in allData)
            {
                if (stringBuilder.Length > 2)
                {
                    stringBuilder.Append(",\n");
                }
                var localRef = (object)referencable;
                jsonWriter.WriteTo(stringBuilder, ref localRef, References);
            }
            stringBuilder.Append("\n]");

            if (string.IsNullOrEmpty(savePath))
            {
                savePath = EditorUtility.SaveFilePanel("Save JSON", "", "neuro_data.json", "json");
            }
            if (!string.IsNullOrEmpty(savePath))
            {
                File.WriteAllText(savePath, stringBuilder.ToString());
                Debug.Log("Saved Neuro JSON data to " + savePath);
            }
            Reload();
        }

        class NeuroEditorDataProviderHook : IReferencesProvider
        {
            public NeuroReferences References => Shared.References;
        }
    }
}