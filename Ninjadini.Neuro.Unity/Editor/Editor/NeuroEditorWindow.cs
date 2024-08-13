using System.Collections.Generic;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroEditorWindow : EditorWindow, IHasCustomMenu
    {
        [SerializeField] uint typeId;
        [SerializeField] uint refId;
        [SerializeField] NeuroEditorHistory historyData;

        NeuroEditorNavElement editorElement;
        
        public NeuroEditorNavElement EditorElement => editorElement;
        
        [MenuItem("Tools/Neuro/❖ Editor", priority = 100)]
        [MenuItem("Window/❖ Neuro Editor")]
        static void NewWindow()
        {
            GetNewWindow();
        }
        
        public static NeuroEditorWindow GetNewWindow()
        {
            var window = CreateWindow<NeuroEditorWindow>("❖ NeuroEditor");
            window.Show();
            return window;
        }
        
        public void CreateGUI()
        {
            historyData ??= new NeuroEditorHistory();
            editorElement = new NeuroEditorNavElement(NeuroEditorDataProvider.Shared, historyData);
            editorElement.style.flexGrow = 1;
            rootVisualElement.Add(editorElement);
            
            var type = NeuroGlobalTypes.FindTypeById(typeId);
            if (type != null)
            {
                editorElement.SetSelectedItem(type, refId);
            }
        }

        void OnDisable()
        {
            if (editorElement != null)
            {
                var type = editorElement.SelectedType;
                typeId = type != null ? NeuroGlobalTypes.GetTypeIdOrThrow(type, out _) : 0;
                refId = editorElement.SelectedItemId;
            }
        }

        
        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Show Debugger Window"), false, NeuroDebuggerWindow.ShowWindow);
            menu.AddItem(new GUIContent("Recompile scripts"), false, CompilationPipeline.RequestScriptCompilation);
        }
    }
}