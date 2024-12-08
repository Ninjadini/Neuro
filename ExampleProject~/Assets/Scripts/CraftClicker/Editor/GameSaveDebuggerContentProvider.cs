using Ninjadini.Neuro;
using Ninjadini.Neuro.Editor;

public class GameSaveDebuggerContentProvider : NeuroContentDebugger.PersistentDataContentProvider
{
    protected override string GetFixedFileName()
    {
        var saveName = NeuroDataProvider.GetSharedSingleton<CraftClickerSettings>()?.SaveFileName ?? "save";
        return saveName;
    }
}
