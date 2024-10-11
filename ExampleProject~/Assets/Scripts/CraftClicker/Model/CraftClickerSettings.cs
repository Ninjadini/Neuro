using System.ComponentModel;
using Ninjadini.Neuro;

[NeuroGlobalType(14)]
[DisplayName("CraftClicker: Settings")]
public class CraftClickerSettings : ISingletonReferencable
{
    [Neuro(1)] public string SaveFileName = "save";
}