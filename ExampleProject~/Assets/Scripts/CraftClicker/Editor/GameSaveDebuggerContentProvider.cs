using System;
using System.IO;
using Ninjadini.Neuro.Editor;
using UnityEngine;
using UnityEngine.UIElements;

public class GameSaveDebuggerContentProvider : NeuroDataDebugger.ContentProvider
{
    public override string DropDownName => "Game Save";
    
    public override NeuroDataDebugger.Format? GetAllowedFormat() => NeuroDataDebugger.Format.Binary;
            
    public override Type GetAllowedType() => typeof(CraftClickerSaveData);

    public override void CreateGUI(VisualElement container, NeuroDataDebugger window)
    {
        
    }

    public override byte[] Load()
    {
        var path = GetPath();
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    public override void Save(byte[] bytes)
    {
        var path = GetPath();
        File.WriteAllBytes(path, bytes);
    }

    public override void Delete()
    {
        var path = GetPath();
        File.Delete(path);
    }
    
    string GetPath() => Application.persistentDataPath + "/save";
}
