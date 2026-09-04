using System;
using UnityEditor;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    /// Per-user settings - things that only affect what the person at this machine sees.
    /// Nothing in here may change baked output, files on disk or anything else the team shares,
    /// otherwise it belongs in NeuroUnityEditorSettings instead.
    /// UserSettings/ is not meant to be checked into version control.
    /// Shown inside the same settings page as NeuroUnityEditorSettings, see its SettingsUIProvider.
    [FilePath("UserSettings/NeuroUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class NeuroUnityUserSettings : ScriptableSingleton<NeuroUnityUserSettings>
    {
        [Tooltip("Debug.Log() neuro loading timings in case you need to know how long things are taking.")]
        public bool LogTimings;
        
        [Tooltip("Show dialog if json data file changes are detected")]
        public bool ShowDialogOnDataFileChange;
        
        [Tooltip("Show the plain number next to RefIds in the editor UI, e.g. `1v83 (87123)`.\n" +
                 "Display only - it does not change file names, json, or what you type into the RefId field.\n" +
                 "Already open windows pick it up when you next select an item.\n" +
                 "Default value: false")]
        public bool ShowRawRefIdNumbers;

        /// The day the user asked to stop being told that data files were reloaded on entering play mode,
        /// as days since epoch. Stored as a day rather than a bool so it comes back the next day
        /// instead of being muted forever.
        [HideInInspector] public int PlayModeReloadDialogMutedDay;

        /// These used to live in NeuroUnityEditorSettings, this carries the old values over once.
        [HideInInspector] public bool MigratedFromProjectSettings;

        public static NeuroUnityUserSettings Get()
        {
            var result = instance;
            if (!result.MigratedFromProjectSettings)
            {
                result.MigrateFromProjectSettings();
            }
            return result;
        }

        void MigrateFromProjectSettings()
        {
            MigratedFromProjectSettings = true;
            var projectSettings = NeuroUnityEditorSettings.Get();
            LogTimings = projectSettings.MigratedLogTimings;
            ShowDialogOnDataFileChange = projectSettings.MigratedShowDialogOnDataFileChange;
            ShowRawRefIdNumbers = projectSettings.MigratedShowRawRefIdNumbers;
            Save();
        }

        public bool IsPlayModeReloadDialogMutedToday()
        {
            return PlayModeReloadDialogMutedDay == Today();
        }

        public void MutePlayModeReloadDialogForToday()
        {
            PlayModeReloadDialogMutedDay = Today();
            Save();
        }

        /// Local days since epoch - the mute is about the person's day, not UTC's.
        static int Today() => (int)(DateTime.Now - new DateTime(1970, 1, 1)).TotalDays;

        public void Save()
        {
            Save(true);
        }
    }
}
