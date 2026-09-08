using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    [InitializeOnLoad]
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public class NeuroEditorDataProvider : IReferencesProvider
    {
        static NeuroEditorDataProvider()
        {
            if (NeuroReferences.Default == null)
            {
                NeuroReferences.Default = new NeuroReferences();
            }
            NeuroDataProvider.Shared.SetReferenceProvider(new NeuroEditorDataProviderHook());
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            // the watchers hold OS handles and a background thread, they don't survive a domain reload usefully.
            AssemblyReloadEvents.beforeAssemblyReload += () => _shared?.ClearAllFileWatchers();
        }

        /// Anything that could not be picked up as it happened - a file was added or removed, or auto reload is
        /// off - is still pending here, and entering play mode with stale data would silently run the wrong
        /// content, so it is reloaded at that point.
        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // _shared is deliberately not touched via Shared here, there is nothing to reload if nothing loaded it.
            if (state != PlayModeStateChange.ExitingEditMode || _shared == null)
            {
                return;
            }
            _shared.ProcessPendingFileChangesNow();
            if (!_shared.HasPendingFileChanges)
            {
                return;
            }
            var changesCount = _shared.PendingFileChangesCount;
            _shared.Reload();
            Debug.Log(changesCount > 0
                ? $"Neuro ~ {changesCount:N0} data file change(s) on disk could not be applied one by one, reloaded all the data before entering play mode."
                : "Neuro ~ data file changes may have been missed, reloaded all the data before entering play mode.");
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
            foreach (var extraPath in settings.ExtraDataPaths ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrEmpty(extraPath) && !dataPaths.Contains(extraPath))
                {
                    dataPaths.Add(extraPath);
                }
            }
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
                    else if (NeuroUnityEditorSettings.Get().IsExtraDataPath(dirPath))
                    {
                        // extra data paths are optional, a project simply may not have that data set.
                        continue;
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
            watchedDirs.Clear();
            if (watchingEditorUpdate)
            {
                watchingEditorUpdate = false;
                EditorApplication.update -= OnEditorUpdateForFileChanges;
            }
            while (pendingFileChanges.TryDequeue(out _))
            {
            }
            pendingChangedFiles.Clear();
            watcherLostEvents = 0;
            needsFullReload = false;
            timeOfLastFileChange = -1d;
        }

        void AddFileWatchers(string dirPath)
        {
            var fullDirPath = WithTrailingSeparator(Path.GetFullPath(dirPath));
            // the watchers include subdirectories, so a data path nested inside another one is already covered
            // and a second watcher on it would only report everything twice.
            if (watchedDirs.Any(d => fullDirPath.StartsWith(d, StringComparison.Ordinal)))
            {
                return;
            }
            var watcher = new FileSystemWatcher(dirPath);
            watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            watcher.Error += OnFileWatcherError;
            watcher.Filter = "*.json";
            watcher.IncludeSubdirectories = true;
            // the default 8kb overflows on a big burst - a branch switch or a bulk edit - and the events that
            // did not fit are simply lost. It is not free (unpaged memory) but it is far cheaper than missing data.
            watcher.InternalBufferSize = 64 * 1024;
            watcher.EnableRaisingEvents = true;
            fileSystemWatchers.Add(watcher);
            watchedDirs.Add(fullDirPath);
            if (!watchingEditorUpdate)
            {
                watchingEditorUpdate = true;
                EditorApplication.update += OnEditorUpdateForFileChanges;
            }
        }

        static string WithTrailingSeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
        }

        /// Raised on the FileSystemWatcher's own thread, where Unity's APIs may not be touched. Everything the
        /// event means is worked out on the editor update tick instead, so all this does is hand the path over.
        void OnFileChanged(object sender, FileSystemEventArgs fileArgs)
        {
            pendingFileChanges.Enqueue(fileArgs.FullPath);
        }

        /// A rename is two paths worth of news - the file that is no longer there and the one that now is.
        void OnFileRenamed(object sender, RenamedEventArgs fileArgs)
        {
            pendingFileChanges.Enqueue(fileArgs.OldFullPath);
            pendingFileChanges.Enqueue(fileArgs.FullPath);
        }

        /// The watcher gave up on us, most likely its buffer overflowed. Whatever it dropped is unknowable, so
        /// the only honest answer is that the whole data set may be stale.
        void OnFileWatcherError(object sender, ErrorEventArgs errorArgs)
        {
            Interlocked.Increment(ref watcherLostEvents);
        }

        /// True while there are data file changes on disk that could not be applied to what is already loaded,
        /// so only a full Reload() will pick them up.
        public bool HasPendingFileChanges => needsFullReload || pendingChangedFiles.Count > 0 || watcherLostEvents > 0;

        /// How many files HasPendingFileChanges is about.
        public int PendingFileChangesCount => pendingChangedFiles.Count;

        /// Raised for each data file that was re-read in place after it changed on disk, right after the new
        /// values have landed in the item. Editor UI showing that item wants to redraw itself.
        public event Action<NeuroDataFile> DataFileReloaded;

        readonly ConcurrentQueue<string> pendingFileChanges = new ConcurrentQueue<string>();
        /// Full paths that changed and could not be dealt with on the spot. A set, so a file saved ten times
        /// counts once - the old counter counted events and could only ever say "~n".
        readonly HashSet<string> pendingChangedFiles = new HashSet<string>();
        readonly List<string> watchedDirs = new List<string>();
        bool watchingEditorUpdate;
        int watcherLostEvents;
        bool needsFullReload;
        double timeOfLastFileChange = -1d;

        /// A burst of changes - a git pull, a save-all in another editor - arrives as a stream of events, this is
        /// how long the stream has to be quiet before we act on it.
        const double FileChangeSettleSeconds = 0.25d;

        void OnEditorUpdateForFileChanges()
        {
            if (DequeueFileChanges())
            {
                // a burst arrives as a stream of events, and a file that is still being written reads back as
                // broken json - let it settle before acting on it.
                timeOfLastFileChange = EditorApplication.timeSinceStartup;
            }
            else if (timeOfLastFileChange >= 0d
                     && EditorApplication.timeSinceStartup - timeOfLastFileChange >= FileChangeSettleSeconds)
            {
                timeOfLastFileChange = -1d;
                ApplyFileChanges();
            }
        }

        /// Deals with everything the watchers have queued up right now, without waiting for the burst to settle.
        /// Whatever is left in pendingChangedFiles afterwards needs a full Reload().
        public void ProcessPendingFileChangesNow()
        {
            DequeueFileChanges();
            timeOfLastFileChange = -1d;
            ApplyFileChanges();
        }

        /// Moves what the watcher thread queued into the pending set. Returns whether anything new turned up.
        bool DequeueFileChanges()
        {
            var anyNew = false;
            while (pendingFileChanges.TryDequeue(out var fullPath))
            {
                pendingChangedFiles.Add(fullPath);
                anyNew = true;
            }
            return anyNew;
        }

        /// Works out what each changed path actually means and applies the ones that can be applied, leaving
        /// only the changes that need a full Reload() in pendingChangedFiles.
        void ApplyFileChanges()
        {
            if (Interlocked.Exchange(ref watcherLostEvents, 0) > 0)
            {
                // We can not know which files the watcher dropped, so nothing loaded can be trusted to be current
                // and there is no file by file fix for it - only a full reload.
                needsFullReload = true;
                Debug.LogWarning("Neuro ~ the data file watcher dropped events, so file changes may have been missed." +
                                 " Reload the neuro data (Tools > Neuro > Reload) to be sure of what is loaded.");
            }
            if (pendingChangedFiles.Count == 0)
            {
                return;
            }
            var autoReload = NeuroUnityEditorSettings.Get().AutoReloadChangedDataFiles;
            var byFullPath = BuildDataFilesByFullPath();
            var reloadedFiles = new List<NeuroDataFile>();
            var startTime = DateTime.UtcNow;
            foreach (var fullPath in pendingChangedFiles.ToArray())
            {
                if (TryApplyFileChange(fullPath, byFullPath, autoReload, reloadedFiles))
                {
                    pendingChangedFiles.Remove(fullPath);
                }
            }
            if (reloadedFiles.Count == 0)
            {
                return;
            }
            LogReloadedFiles(reloadedFiles, startTime);
            // told after the fact rather than one by one, so a listener that reloads or rebuilds can't disturb
            // the pass that is still running.
            foreach (var dataFile in reloadedFiles)
            {
                DataFileReloaded?.Invoke(dataFile);
            }
        }

        void LogReloadedFiles(List<NeuroDataFile> reloadedFiles, DateTime startTime)
        {
            const int maxNamed = 5;
            var names = string.Join("\n", reloadedFiles.Take(maxNamed)
                .Select(f => $"  {NeuroEditorUtils.DisplayRefId(f.RefId)}-{f.RefName} @ {f.FilePath}"));
            if (reloadedFiles.Count > maxNamed)
            {
                names += $"\n  ...and {reloadedFiles.Count - maxNamed:N0} more";
            }
            var timing = NeuroUnityUserSettings.Get().LogTimings
                ? $" in {(DateTime.UtcNow - startTime).TotalMilliseconds:N0} ms"
                : "";
            Debug.Log($"Neuro ~ reloaded {reloadedFiles.Count:N0} data file(s) changed on disk{timing}:\n{names}");
        }

        Dictionary<string, NeuroDataFile> BuildDataFilesByFullPath()
        {
            var result = new Dictionary<string, NeuroDataFile>(dataFiles.Count);
            foreach (var dataFile in dataFiles)
            {
                if (!string.IsNullOrEmpty(dataFile.FilePath))
                {
                    result[Path.GetFullPath(dataFile.FilePath)] = dataFile;
                }
            }
            return result;
        }

        /// Returns true when this path needs nothing further - either it turned out to be no change at all, or
        /// the file was re-read in place. False leaves it pending for a full Reload().
        bool TryApplyFileChange(string fullPath, Dictionary<string, NeuroDataFile> byFullPath, bool autoReload, List<NeuroDataFile> reloadedFiles)
        {
            if (!byFullPath.TryGetValue(fullPath, out var dataFile))
            {
                // A path we don't have an item for. If it is there, it is a new file and only a full reload can
                // add it; if it is not, it is a file we deleted ourselves (or one that was never ours anyway).
                return !File.Exists(fullPath);
            }
            if (!File.Exists(fullPath))
            {
                // the item is still registered but its file is gone - deleted or renamed away outside the editor.
                return false;
            }
            string json;
            try
            {
                json = File.ReadAllText(fullPath);
            }
            catch (Exception e)
            {
                // most likely still being written, the writer's own completion will raise another event.
                Debug.LogWarning($"Neuro ~ could not read changed data file, leaving it for a full reload @ {fullPath}\n{e.Message}");
                return false;
            }
            if (dataFile.IsLastKnownContent(json))
            {
                // this is the write we made ourselves, or a touch that did not change anything.
                return true;
            }
            if (!dataFile.IsLoaded)
            {
                // nothing is holding stale values, it will be read off disk whenever something asks for it.
                dataFile.SetLastKnownContent(json);
                return true;
            }
            if (!autoReload)
            {
                return false;
            }
            try
            {
                ReloadDataFileInPlace(dataFile, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Neuro ~ failed to reload changed data file @ {fullPath}\n{e}");
                return false;
            }
            reloadedFiles.Add(dataFile);
            return true;
        }

        /// Reads the json into the object that is already loaded so that everything holding on to the item sees
        /// the new values. The reference table only needs repointing in the one case the object can not be
        /// reused - the json turned out to be a different subtype and the reader had to build a new one.
        void ReloadDataFileInPlace(NeuroDataFile dataFile, string json)
        {
            var previousValue = dataFile.Value;
            var newValue = dataFile.ReloadValueFromDisk(json);
            if (!ReferenceEquals(previousValue, newValue))
            {
                var table = References.GetTable(dataFile.RootType);
                table.Unregister(dataFile.RefId);
                table.Register(newValue);
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
            return AddAtPath(newObj, Path.Combine(GetDirForType(type), GetFileName(newObj) + ".json"));
        }

        /// Adds an item whose RefId is already decided, as the file at `filePath` - a null or empty path means the
        /// usual place for its type. This is how undo puts a deleted item back where it was, which may not be the
        /// primary data path. Throws if the id is taken.
        internal NeuroDataFile AddAtPath(IReferencable newObj, string filePath)
        {
            var type = NeuroReferences.GetRootReferencable(newObj.GetType());
            if (newObj.RefId == 0)
            {
                throw new ArgumentException("The object needs a RefId, use Add() to have one generated.", nameof(newObj));
            }
            if (Find(type, newObj.RefId) != null || References.Get(type, newObj.RefId) != null)
            {
                throw new Exception($"Object with RefId `{NeuroEditorUtils.DisplayRefId(newObj.RefId)}` already exists for type `{type.Name}`");
            }
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(GetDirForType(type), GetFileName(newObj) + ".json");
            }
            var result = new NeuroDataFile(type, filePath, this)
            {
                Value = newObj
            };
            dataFiles.Add(result);
            References.Register(newObj);
            SaveData(result);
            return result;
        }

        /// Replaces the loaded item's content with `json` - into the same object where possible, as a file changed
        /// on disk would be reloaded - then saves it and raises <see cref="DataFileReloaded"/> so open editors
        /// redraw. This is how undo/redo lands a recorded state.
        internal void ApplyJson(NeuroDataFile dataFile, string json)
        {
            ReloadDataFileInPlace(dataFile, json);
            SaveData(dataFile);
            DataFileReloaded?.Invoke(dataFile);
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
                File.WriteAllText(dataFile.FilePath, json);
                // so the watcher event this write is about to raise is recognised as our own rather than a change.
                dataFile.SetLastKnownContent(json);
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
                File.Delete(dataFile.FilePath);
            }
            dataFiles.Remove(dataFile);
        }

        /// Moves an existing item to a different RefId and rewrites every <c>Reference&lt;&gt;</c> in the database that
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
                File.Delete(dataFile.FilePath);
            }
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

        /// Points every <c>Reference&lt;rootType&gt;</c> that held `oldRefId` at `newRefId`, across every item in the
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
                File.Delete(dataFile.FilePath);
            }
            dataFile.SetFilePath(newPath);
            SaveData(dataFile);
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