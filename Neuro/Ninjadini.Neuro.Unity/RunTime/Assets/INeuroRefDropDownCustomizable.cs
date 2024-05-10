namespace Ninjadini.Neuro
{
    public interface INeuroRefDropDownCustomizable
    {
        string GetRefDropdownText(NeuroReferences references);
    }
    
    public interface INeuroRefDropDownIconCustomizable : INeuroRefDropDownCustomizable
    {
        AssetAddress RefDropdownIcon { get; }
    }
}