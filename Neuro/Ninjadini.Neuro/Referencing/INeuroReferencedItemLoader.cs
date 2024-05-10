using Ninjadini.Neuro;

public interface INeuroReferencedItemLoader
{
    IReferencable Load(uint refId);
    string GetRefName(uint refId);
}