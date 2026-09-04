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
        internal NeuroJsonReader jsonReader;
        internal NeuroJsonWriter jsonWriter;
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

        public void FullScriptReload()
        {
            EditorUtility.RequestScriptReload();
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

        static uint ReadTypeIdFromDirName(string dirPath)
        {
            var dirName = Path.GetFileName(dirPath.AsSpan());
            var splitIndex = dirName.IndexOf("-");
            if (splitIndex > 0)
            {
                dirName = dirName.Slice(0, splitIndex);
            }
            return uint.TryParse(dirName, out var id) ? id : 0;
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
                    // the type directory is `<globalTypeId>-<TypeName>`, and a global type id is a plain decimal
                    // number - not a RefId, so it must not go through the base36 reading.
                    var typeId = ReadTypeIdFromDirName(subDir);
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
            if (NeuroUnityUserSettings.Get().LogTimings)
            {
                Debug.Log($"Neuro ~ Found {count:N0} json files in {(DateTime.UtcNow - startTime).TotalMilliseconds:N0} ms");
            }
            WarnIfRefIdsNeedMigrating(count);
        }

        /// RefIds used to be spelled in decimal and are now base36, and the two disagree on any name made only of
        /// digits - `20-item.json` used to be RefId 20 and now reads as 72. That is silent, so it gets said out
        /// loud once per load until the data is converted.
        static void WarnIfRefIdsNeedMigrating(int fileCount)
        {
            if (!NeuroRefIdMigration.IsMigrationNeeded())
            {
                return;
            }
            if (fileCount == 0)
            {
                // nothing on disk to be misread, so a new project is simply already in the current format.
                NeuroRefIdMigration.MarkAsMigrated();
                return;
            }
            Debug.LogWarning("Neuro ~ this project's data files were written when RefIds were spelled in decimal," +
                             " and RefIds are now base36. Ids that are all digits are being read as the wrong number." +
                             "\nRun `Tools > Neuro > Migrate RefIds to base36...` to convert the data. It keeps the id numbers as they are.");
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
                    Debug.LogError($"Neuro data file with duplicate RefId `{NeuroEditorUtils.DisplayRefId(refId)}` found @ {filePath}");
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
            if (updatesCountSinceFilesChanged < 0 && NeuroUnityUserSettings.Get().ShowDialogOnDataFileChange)
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

        public NeuroDataFile Find(IReferencable referencable)
        {
            return referencable == null ? null : Find(referencable.GetType(), referencable.RefId);
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

        static readonly System.Random RefIdRandom = new System.Random();
        
        public uint FindNextId(Type type)
        {
            var used = new HashSet<uint>(References.GetTable(type).GetIds());
            var rootType = NeuroReferences.GetRootReferencable(type);
            if (dataFiles != null)
            {
                foreach (var dataFile in dataFiles)
                {
                    if (dataFile.RootType == rootType)
                    {
                        used.Add(dataFile.RefId);
                    }
                }
            }
            const int maxAttempts = 1000;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var id = (uint)RefIdRandom.Next((int)NeuroRefId.GeneratedMinValue, (int)NeuroRefId.GeneratedMaxValue + 1);
                if (!used.Contains(id))
                {
                    return id;
                }
            }
            throw new Exception($"Could not find a free RefId for `{type}` after {maxAttempts} attempts, there are {used.Count} ids in use.");
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
                    throw new Exception($"Custom RefId `{NeuroEditorUtils.DisplayRefId(customRefId)}` is already in use for type `{newObj.GetType()}`");
                }
            }
            else if(newObj.RefId > 0)
            {
                nextId = newObj.RefId;
                var other = Find(type, nextId);
                if(other != null)
                {
                    if(other.Value == newObj)
                    {
                        return other;
                    }
                    throw new Exception($"Object with RefId `{NeuroEditorUtils.DisplayRefId(nextId)}` already exists, set the ref of the new object to 0 to generate a new next number");
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
                Debug.LogError($"Tried to assign {newObj.GetType().Name}'s RefId to `{NeuroEditorUtils.DisplayRefId(nextId)}` but it is still `{NeuroEditorUtils.DisplayRefId(resultId)}`");
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
                Debug.LogWarning("Data file not found for " + type +" with id " + NeuroEditorUtils.DisplayRefId(data.RefId));
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
                var json = jsonWriter.WriteObject(value, refs:References, options:NeuroJsonWriter.Options.ExcludeTopLevelGlobalType);
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

        /// Moves an existing item to a different RefId and rewrites every Reference<> in the database that
        /// pointed at the old one, so nothing is left dangling. Returns the other items that had to be updated.
        /// Throws if the new id is not free - check with GetRefIdChangeProblem() first if you want to ask first.
        public IReadOnlyList<IReferencable> ChangeRefId(NeuroDataFile dataFile, uint newRefId)
        {
            if (dataFile == null)
            {
                throw new ArgumentNullException(nameof(dataFile));
            }
            var oldRefId = dataFile.RefId;
            var rootType = dataFile.RootType;
            var problem = GetRefIdChangeProblem(dataFile, newRefId);
            if (problem != null)
            {
                throw new Exception(problem);
            }
            if (newRefId == oldRefId)
            {
                return Array.Empty<IReferencable>();
            }
            // Get() rather than dataFile.Value so that the item is moved out of the table's lazy loaders and into
            // its loaded items. Otherwise the full scan below would load it under the old id afterwards, and the
            // table checks the loaded object's RefId against the id it asked for.
            var value = References.Get(rootType, oldRefId) ?? dataFile.Value;

            // Check the id can actually be assigned before anything is modified - an IReferencable is free to
            // implement RefId however it likes, and failing half way would leave the loaded data pointing at an
            // id that no item has.
            value.RefId = newRefId;
            var assignable = value.RefId == newRefId;
            value.RefId = oldRefId;
            if (!assignable)
            {
                throw new Exception($"{value.GetType().Name}'s RefId can not be assigned - tried to set it to `{NeuroEditorUtils.DisplayRefId(newRefId)}` and it stayed `{NeuroEditorUtils.DisplayRefId(value.RefId)}`.");
            }

            var updated = RewriteReferencesTo(rootType, oldRefId, newRefId);

            var table = References.GetTable(rootType);
            table.Unregister(oldRefId);
            value.RefId = newRefId;
            table.Register(value);

            // the id is part of the file name, so moving the id renames the file.
            var newPath = Path.Combine(Path.GetDirectoryName(dataFile.FilePath), GetFileName(value) + ".json");
            if (!string.IsNullOrEmpty(dataFile.FilePath) && File.Exists(dataFile.FilePath))
            {
                AddTempIgnoreFile(dataFile.FilePath);
                File.Delete(dataFile.FilePath);
            }
            AddTempIgnoreFile(newPath);
            dataFile.SetFilePath(newPath);
            dataFile.Value = value;
            SaveData(dataFile);

            foreach (var referencable in updated)
            {
                var otherFile = Find(referencable.GetType(), referencable.RefId);
                if (otherFile != null)
                {
                    SaveData(otherFile);
                }
            }
            return updated;
        }

        /// Why `newRefId` can not be given to this item, or null when it can.
        public string GetRefIdChangeProblem(NeuroDataFile dataFile, uint newRefId)
        {
            var rootType = dataFile.RootType;
            if (rootType == null)
            {
                return "Can not determine the type of this item.";
            }
            if (typeof(ISingletonReferencable).IsAssignableFrom(rootType))
            {
                return $"`{rootType.Name}` is a singleton, its RefId is always 1.";
            }
            if (newRefId == dataFile.RefId)
            {
                return null;
            }
            if (newRefId == 0)
            {
                return "RefId `0` is reserved for 'no reference', it can not be used for an item.";
            }
            var existing = Find(rootType, newRefId);
            if (existing != null)
            {
                return $"RefId `{NeuroEditorUtils.DisplayRefId(newRefId)}` is already used by `{existing.RefName}`\n@ {existing.FilePath}";
            }
            if (References.Get(rootType, newRefId) != null)
            {
                return $"RefId `{NeuroEditorUtils.DisplayRefId(newRefId)}` is already in use for type `{rootType.Name}`.";
            }
            return null;
        }

        /// Points every Reference<rootType> that held `oldRefId` at `newRefId`, across every item in the
        /// database. Returns the items that changed - they still need saving.
        List<IReferencable> RewriteReferencesTo(Type rootType, uint oldRefId, uint newRefId)
        {
            var neuroVisitor = new NeuroEditVisitor();
            var rewriter = new RefIdRewriteVisitor(rootType, oldRefId, newRefId);
            var updated = new List<IReferencable>();
            foreach (var baseType in References.GetRegisteredBaseTypes().ToArray())
            {
                // ToArray because visiting deserializes the lazily loaded items, which writes to the very table
                // we would otherwise still be enumerating.
                foreach (var referencable in References.GetTable(baseType).SelectAll().ToArray())
                {
                    rewriter.Changes = 0;
                    neuroVisitor.Visit(referencable, rewriter);
                    if (rewriter.Changes > 0)
                    {
                        updated.Add(referencable);
                    }
                }
            }
            return updated;
        }

        class RefIdRewriteVisitor : NeuroEditVisitor.IInterface
        {
            readonly Type rootType;
            readonly uint oldRefId;
            readonly uint newRefId;

            public int Changes;

            public RefIdRewriteVisitor(Type rootType, uint oldRefId, uint newRefId)
            {
                this.rootType = rootType;
                this.oldRefId = oldRefId;
                this.newRefId = newRefId;
            }

            void NeuroEditVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex)
            {
            }

            void NeuroEditVisitor.IInterface.EndVisit()
            {
            }

            void NeuroEditVisitor.IInterface.VisitRef<T>(ref Reference<T> reference)
            {
                if (reference.RefId == oldRefId && typeof(T) == rootType)
                {
                    reference.RefId = newRefId;
                    Changes++;
                }
            }
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
            var id = NeuroRefId.ToString(referencable.RefId);
            var name = Regex.Replace(referencable.RefName ?? "", NeuroDataFile.InvalidFileNameRegExp, "");
            if (string.IsNullOrEmpty(name))
            {
                return id;
            }
            return id + "-" + (name.Length > 64 ? name.Substring(0, 64) : name);
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
                jsonWriter.WriteGlobalTypedTo(stringBuilder, referencable, References);
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