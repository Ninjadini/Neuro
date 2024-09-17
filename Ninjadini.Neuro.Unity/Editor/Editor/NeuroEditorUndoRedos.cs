using System;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    [Serializable]
    public class NeuroEditorUndoRedos : ScriptableObject
    {
        public UndoData CurrentUndoData;
        
        static NeuroEditorUndoRedos _instance;
        public static NeuroEditorUndoRedos Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = CreateInstance<NeuroEditorUndoRedos>();
                _instance.hideFlags = HideFlags.HideAndDontSave;
                return _instance;
            }
        }
        
        public enum UndoType
        {
            NA,
            View,
            Create,
            Update,
            Delete
        }
        
        [Serializable]
        public struct UndoData
        {
            public UndoType type;
            public uint typeId;
            public uint refId;
            public string json;
            public EditorWindow window;
        }
        
        public static void Record(NeuroDataFile dataFile, UndoType undoType, EditorWindow changedWindow)
        {
            var type = dataFile.Value.GetType();
            var json = NeuroEditorDataProvider.Shared.jsonWriter.Write((object)dataFile.Value, refs:NeuroEditorDataProvider.SharedReferences, options:NeuroJsonWriter.Options.ExcludeTopLevelGlobalType);

            var instance = Instance;
            
            var undoName = $"{undoType} Neuro Data {dataFile.Value.TryGetIdAndName()}";
            instance.CurrentUndoData = new UndoData()
            {
                type = undoType,
                typeId = NeuroGlobalTypes.GetTypeIdOrThrow(type, out _),
                refId = dataFile.RefId,
                json = json,
                window = changedWindow
            };
            Undo.RegisterCompleteObjectUndo(instance, undoName);
        }
            
        void OnValidate()
        {
            var undoData = CurrentUndoData;
            if(undoData.typeId != 0 && undoData.refId != 0)
            {
                var type = NeuroGlobalTypes.FindTypeById(undoData.typeId);
                if (type != null)
                {
                    var item = NeuroEditorDataProvider.Shared.Find(type, undoData.refId);
                    if (item != null)
                    {
                        var value = (object)item.Value;
                        NeuroEditorDataProvider.Shared.jsonReader.Read(undoData.json, type, ref value);
                        SelectWindowAfterUndo(type);

                    }
                    else if (undoData.type == UndoType.Delete)
                    {
                        if (NeuroEditorDataProvider.Shared.jsonReader.Read(undoData.json, type) is IReferencable value)
                        {
                            NeuroEditorDataProvider.Shared.Add(value);
                            SelectWindowAfterUndo(type);
                        }
                    }
                }
            }
        }

        void SelectWindowAfterUndo(Type type)
        {
            if(CurrentUndoData.window is NeuroEditorWindow editorWindow)
            {
                editorWindow.EditorElement.SetSelectedItem(type, CurrentUndoData.refId);
                editorWindow.Focus();
            }
        }
    }
}