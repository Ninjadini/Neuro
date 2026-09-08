using System;
using System.Runtime.CompilerServices;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    /// Undo/redo for the Neuro editor, on top of Unity's own undo stack - so Ctrl+Z / Ctrl+Y and the Edit menu
    /// work the same as they do for a scene or an inspector, and Neuro edits interleave correctly with other undos.
    ///
    /// How it works: a hidden ScriptableObject holds one serialized <see cref="State"/> - a json snapshot of a
    /// single item. To record a change, the state is set to how the item was, `Undo.RegisterCompleteObjectUndo`
    /// takes a copy of that, then the state is set to how the item is now. An undo puts the old state back into the
    /// object, a redo puts the new one back, and `Undo.undoRedoPerformed` is where whichever state landed gets
    /// written into the real data and saved to disk.
    ///
    /// The "before" side of an entry comes from the snapshot taken when the item was last drawn or last recorded
    /// (see <see cref="Snapshot"/>), so a change made to the data behind the editor's back - a script, a file
    /// reload - is not undoable; it just becomes the new "before" the next time the item is drawn.
    /// Only the one item that was edited is in an entry. A RefId change is the exception in spirit: undoing it
    /// calls `ChangeRefId` back, which also repoints the other items again.
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public class NeuroEditorUndoRedos : ScriptableObject
    {
        [Serializable]
        public struct State
        {
            /// Increments on every recorded change, so two consecutive states never serialize the same (Unity
            /// may drop an undo entry that changed nothing) and so the undo callback can tell whether the undo it
            /// is being told about touched this object at all.
            public int serial;
            public uint typeId;
            /// The item's id in this state.
            public uint refId;
            /// The id the same item has on the other side of the entry. Only differs from refId when the entry is a
            /// RefId change, and is how the item is found again when that change is undone or redone.
            public uint otherRefId;
            public string refName;
            public string filePath;
            /// null when the item does not exist in this state - before a Create, after a Delete. (Unity's
            /// serializer turns a null string into an empty one, so an empty one means the same thing.)
            public string json;
            /// The NeuroEditorWindow the change was made in, so undoing brings the item back into view there.
            public EditorWindow window;

            public bool Exists => !string.IsNullOrEmpty(json);
        }

        [SerializeField] State state;

        /// serial of the state the data currently reflects - set when a change is recorded or a state is applied.
        /// Not serialized: an undo must not roll it back, or it would stop matching what was actually applied.
        [NonSerialized] int appliedSerial;

        static NeuroEditorUndoRedos _instance;
        static int _nextSerial;

        /// The last known state of each item, keyed by the data file. Weak so a full Reload() does not leak the
        /// files it threw away.
        static readonly ConditionalWeakTable<NeuroDataFile, StrongBox<State>> Snapshots = new ConditionalWeakTable<NeuroDataFile, StrongBox<State>>();

        public static bool Enabled = true;

        [InitializeOnLoadMethod]
        static void Init()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            // Pick the survivor up now rather than on the first undo - finding it syncs appliedSerial to whatever
            // state it holds, which on the first undo would be the freshly restored one, swallowing that undo.
            _instance = FindExisting();
        }

        /// The one instance. It survives domain reloads (it is DontSave, and Unity keeps it alive because the undo
        /// stack refers to it) which is what lets an entry recorded before a script compile still undo afterwards.
        static NeuroEditorUndoRedos Instance
        {
            get
            {
                if (_instance)
                {
                    return _instance;
                }
                _instance = FindExisting();
                if (!_instance)
                {
                    _instance = CreateInstance<NeuroEditorUndoRedos>();
                    _instance.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                }
                return _instance;
            }
        }

        static NeuroEditorUndoRedos FindExisting()
        {
            var all = Resources.FindObjectsOfTypeAll<NeuroEditorUndoRedos>();
            if (all.Length == 0)
            {
                return null;
            }
            var found = all[0];
            // Coming back after a domain reload, what is in `state` is by definition what the data reflects.
            found.appliedSerial = found.state.serial;
            _nextSerial = Math.Max(_nextSerial, found.state.serial + 1);
            return found;
        }

        /// Remembers how the item is right now, to be the "before" of the next recorded change. Called whenever the
        /// editor draws an item; harmless to call more often than that.
        public static void Snapshot(NeuroDataFile dataFile)
        {
            if (!Enabled || dataFile?.Value == null)
            {
                return;
            }
            var box = Snapshots.GetOrCreateValue(dataFile);
            box.Value = Capture(dataFile);
        }

        /// Records that the item has just been changed - its fields, its RefName, or its RefId. The change itself
        /// has already happened; the "before" is the last <see cref="Snapshot"/> of the item.
        /// `action` is the verb in the Edit menu, e.g. "Edit", "Rename".
        public static void RecordChange(NeuroDataFile dataFile, string action, EditorWindow window = null)
        {
            if (!Enabled || dataFile?.Value == null)
            {
                return;
            }
            var after = Capture(dataFile);
            if (!Snapshots.TryGetValue(dataFile, out var box))
            {
                // never seen this item before, so there is no "before" to go back to. Start tracking from here.
                Snapshots.Add(dataFile, new StrongBox<State>(after));
                return;
            }
            var before = box.Value;
            if (before.json == after.json && (before.refName ?? "") == (after.refName ?? "") && before.refId == after.refId)
            {
                return;
            }
            before.otherRefId = after.refId;
            after.otherRefId = before.refId;
            Push(before, after, action, window);
            box.Value = after;
        }

        /// Records that the item was just added to the data provider.
        public static void RecordCreate(NeuroDataFile dataFile, EditorWindow window = null)
        {
            if (!Enabled || dataFile?.Value == null)
            {
                return;
            }
            var after = Capture(dataFile);
            var before = after;
            before.json = null;
            Push(before, after, "Create", window);
            Snapshots.GetOrCreateValue(dataFile).Value = after;
        }

        /// Records that the item is about to be deleted. Call this before `NeuroEditorDataProvider.Delete()`, while
        /// the item can still be read.
        public static void RecordDelete(NeuroDataFile dataFile, EditorWindow window = null)
        {
            if (!Enabled || dataFile?.Value == null)
            {
                return;
            }
            var before = Capture(dataFile);
            var after = before;
            after.json = null;
            Push(before, after, "Delete", window);
            Snapshots.Remove(dataFile);
        }

        static State Capture(NeuroDataFile dataFile)
        {
            var provider = NeuroEditorDataProvider.Shared;
            var value = dataFile.Value;
            return new State
            {
                typeId = NeuroGlobalTypes.GetTypeIdOrThrow(dataFile.RootType, out _),
                refId = dataFile.RefId,
                otherRefId = dataFile.RefId,
                refName = dataFile.RefName,
                filePath = dataFile.FilePath,
                json = provider.jsonWriter.WriteObject((object)value, refs: provider.References, options: NeuroJsonWriter.Options.ExcludeTopLevelGlobalType),
            };
        }

        static void Push(State before, State after, string action, EditorWindow window)
        {
            var instance = Instance;
            before.window = window;
            after.window = window;
            before.serial = _nextSerial++;
            after.serial = _nextSerial++;

            var type = NeuroGlobalTypes.FindTypeById(after.typeId);
            var itemName = string.IsNullOrEmpty(after.refName)
                ? NeuroEditorUtils.DisplayRefId(after.refId)
                : $"{NeuroEditorUtils.DisplayRefId(after.refId)}:{after.refName}";
            var undoName = $"Neuro {action} {type?.Name} {itemName}";

            instance.state = before;
            Undo.RegisterCompleteObjectUndo(instance, undoName);
            instance.state = after;
            instance.appliedSerial = after.serial;
        }

        static void OnUndoRedoPerformed()
        {
            // not via Instance - if nothing has been recorded there is nothing to do, and no reason to create one.
            var instance = _instance;
            if (!instance || instance.state.serial == instance.appliedSerial)
            {
                return;
            }
            instance.appliedSerial = instance.state.serial;
            try
            {
                Apply(instance.state);
            }
            catch (Exception e)
            {
                Debug.LogError($"Neuro ~ undo/redo could not be applied to the data: {e.Message}\n{e}");
            }
        }

        /// Makes the data match `state`, saving whatever that touches to disk.
        static void Apply(State state)
        {
            var type = NeuroGlobalTypes.FindTypeById(state.typeId);
            if (type == null)
            {
                Debug.LogWarning($"Neuro ~ can not undo/redo, type id {state.typeId} is no longer registered.");
                return;
            }
            var provider = NeuroEditorDataProvider.Shared;
            var dataFile = provider.Find(type, state.refId);
            if (dataFile == null && state.otherRefId != 0 && state.otherRefId != state.refId)
            {
                dataFile = provider.Find(type, state.otherRefId);
            }
            if (!state.Exists)
            {
                if (dataFile != null)
                {
                    Snapshots.Remove(dataFile);
                    provider.Delete(dataFile);
                }
            }
            else if (dataFile == null)
            {
                var value = provider.jsonReader.ReadObject(state.json, type) as IReferencable;
                if (value == null)
                {
                    Debug.LogWarning($"Neuro ~ can not undo/redo, the recorded json did not read back as a {type.Name}.");
                    return;
                }
                value.RefId = state.refId;
                value.RefName = state.refName;
                dataFile = provider.AddAtPath(value, state.filePath);
                Snapshots.GetOrCreateValue(dataFile).Value = state;
            }
            else
            {
                if (dataFile.RefId != state.refId)
                {
                    provider.ChangeRefId(dataFile, state.refId);
                }
                if ((dataFile.RefName ?? "") != (state.refName ?? ""))
                {
                    provider.SetRefName(dataFile, state.refName ?? "");
                }
                provider.ApplyJson(dataFile, state.json);
                Snapshots.GetOrCreateValue(dataFile).Value = state;
            }
            ShowInWindow(state, type);
        }

        static void ShowInWindow(State state, Type type)
        {
            var window = state.window as NeuroEditorWindow;
            if (!window || window.EditorElement == null)
            {
                return;
            }
            if (state.Exists)
            {
                window.EditorElement.SetSelectedItem(type, state.refId);
            }
            // when the item no longer exists, the nav element notices on its next update and clears the selection.
            window.Focus();
        }
    }
}
