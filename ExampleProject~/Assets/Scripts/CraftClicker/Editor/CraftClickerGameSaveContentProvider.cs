using System;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Editor;

public class CraftClickerGameSaveContentProvider : NeuroContentDebugger.PersistentDataContentProvider
{
    protected override string GetFixedFileName()
    {
        var saveName = NeuroDataProvider.GetSharedSingleton<CraftClickerSettings>()?.SaveFileName;
        return string.IsNullOrEmpty(saveName) ? "save" : saveName;
    }
    public override Type GetAllowedType() => typeof(CraftClickerSaveData);

    public override NeuroContentDebugger.Format? GetAllowedFormat() => NeuroContentDebugger.Format.Binary;
}
