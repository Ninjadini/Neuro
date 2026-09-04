using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    /// One time migration of data written before RefIds were base36, when they were spelled in plain decimal.
    ///
    /// The numbers do not change - only how they are spelled. `20-wood.json` holding RefId 20 becomes
    /// `k-wood.json` still holding RefId 20, so ids stored outside the json (save games, prefabs, hard coded
    /// numbers in your code) keep pointing at the same items.
    ///
    /// This can only run once. Both spellings can produce a name made only of digits - RefId 72 is `20` in
    /// base36 - so running it a second time would read those as decimal again and give them the wrong id.
    /// NeuroUnityEditorSettings.RefIdFormatVersion records that it has been done.
    public static class NeuroRefIdMigration
    {
        /// Bumped when the way RefIds are spelled on disk changes. 1 = base36.
        public const int CurrentFormatVersion = 1;

        public static bool IsMigrationNeeded()
        {
            return NeuroUnityEditorSettings.Get().RefIdFormatVersion < CurrentFormatVersion;
        }

        [MenuItem("Tools/Neuro/Migrate RefIds to base36...", priority = 210)]
        public static void MigrateMenuItem()
        {
            if (!IsMigrationNeeded())
            {
                EditorUtility.DisplayDialog("Nothing to migrate",
                    "This project's data is already written with base36 RefIds.\n\n" +
                    "It can not be run a second time - a name made only of digits is a valid base36 id, so re-reading it as decimal would change the ids.",
                    "OK");
                return;
            }
            var dataProvider = NeuroEditorDataProvider.Shared;
            var files = dataProvider.DataFiles.ToArray();
            if (files.Length == 0)
            {
                MarkAsMigrated();
                EditorUtility.DisplayDialog("Nothing to migrate",
                    "There are no data files yet, so there is nothing to convert.\n\nThis project is now marked as using base36 RefIds.",
                    "OK");
                return;
            }
            var plan = BuildPlan(files);
            var renames = plan.Count(p => p.OldPath != p.NewPath);
            var message = $"{plan.Count} data file(s) will be re-read as decimal RefIds and written back as base36.\n\n";
            message += $"{renames} file(s) will be renamed, and every Reference in them repointed at the same items.\n\n";
            message += "RefId numbers do not change, only how they are spelled, so ids stored outside the Neuro data keep working.\n\n";
            message += "This can only be done once. Make sure your data is committed to source control first.";
            if (!EditorUtility.DisplayDialog("Migrate RefIds to base36", message, "Migrate", "Cancel"))
            {
                return;
            }
            try
            {
                Migrate(dataProvider, plan);
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Migration failed",
                    $"{e.Message}\n\nThe data may be half converted - restore it from source control before trying again.",
                    "OK");
                Debug.LogException(e);
                return;
            }
            MarkAsMigrated();
            dataProvider.Reload();
            Debug.Log($"Neuro ~ migrated {plan.Count} data file(s) to base36 RefIds, {renames} renamed.");
            EditorUtility.DisplayDialog("Migrated",
                $"{plan.Count} data file(s) converted, {renames} renamed.\n\nHave a look at the diff before committing.",
                "OK");
        }

        internal static void MarkAsMigrated()
        {
            var settings = NeuroUnityEditorSettings.Get();
            settings.RefIdFormatVersion = CurrentFormatVersion;
            settings.Save();
        }

        static List<Item> BuildPlan(IReadOnlyList<NeuroDataFile> files)
        {
            var result = new List<Item>(files.Count);
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.FilePath);
                var splitIndex = fileName.IndexOf('-');
                var idPart = splitIndex > 0 ? fileName.Substring(0, splitIndex) : fileName;
                if (!NeuroRefId.TryParseLegacy(idPart, out var legacyRefId) || legacyRefId == 0)
                {
                    throw new Exception($"Can not work out the RefId of `{fileName}` @ {file.FilePath}");
                }
                var namePart = splitIndex > 0 ? fileName.Substring(splitIndex + 1) : "";
                var newName = NeuroRefId.ToString(legacyRefId) + (namePart.Length > 0 ? "-" + namePart : "");
                result.Add(new Item
                {
                    File = file,
                    RefId = legacyRefId,
                    OldPath = file.FilePath,
                    NewPath = Path.Combine(Path.GetDirectoryName(file.FilePath) ?? "", newName + ".json")
                });
            }
            var clashes = result.GroupBy(i => i.NewPath).Where(g => g.Count() > 1).ToArray();
            if (clashes.Length > 0)
            {
                throw new Exception($"`{clashes[0].Key}` would be written by {clashes[0].Count()} different data files.");
            }
            return result;
        }

        static void Migrate(NeuroEditorDataProvider dataProvider, List<Item> plan)
        {
            var jsonReader = dataProvider.JsonReader;
            var jsonWriter = new NeuroJsonWriter();
            var editVisitor = new NeuroEditVisitor();
            var fixer = new LegacyRefIdFixer();
            var written = new List<(string path, string json)>(plan.Count);
            try
            {
                for (var i = 0; i < plan.Count; i++)
                {
                    var item = plan[i];
                    EditorUtility.DisplayProgressBar("Migrating RefIds", item.OldPath, (float)i / plan.Count);

                    // Read with the normal reader, then undo the base36 reading of each id. That works because
                    // the two spellings only disagree on all digit text, and base36 text of the misread number
                    // is exactly the text that was in the file - so the original decimal id is recoverable.
                    var value = (IReferencable)jsonReader.ReadObject(File.ReadAllText(item.OldPath), item.File.RootType);
                    editVisitor.Visit(value, fixer);
                    value.RefId = item.RefId;
                    value.RefName = item.File.RefName;

                    written.Add((item.NewPath, jsonWriter.WriteObject(value, dataProvider.References,
                        NeuroJsonWriter.Options.ExcludeTopLevelGlobalType)));
                }
                // Nothing is touched on disk until every file has been read and converted, so a failure part way
                // through leaves the data as it was.
                foreach (var item in plan)
                {
                    if (item.OldPath != item.NewPath && File.Exists(item.OldPath))
                    {
                        File.Delete(item.OldPath);
                    }
                }
                foreach (var (path, json) in written)
                {
                    File.WriteAllText(path, json);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// Turns an id that was just read as base36 back into the number the old decimal spelling meant.
        class LegacyRefIdFixer : NeuroEditVisitor.IInterface
        {
            void NeuroEditVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex)
            {
            }

            void NeuroEditVisitor.IInterface.EndVisit()
            {
            }

            void NeuroEditVisitor.IInterface.VisitRef<T>(ref Reference<T> reference)
            {
                if (reference.RefId != 0 && NeuroRefId.TryParseLegacy(NeuroRefId.ToString(reference.RefId), out var legacy))
                {
                    reference.RefId = legacy;
                }
            }
        }

        struct Item
        {
            public NeuroDataFile File;
            public uint RefId;
            public string OldPath;
            public string NewPath;
        }
    }
}
