namespace Ninjadini.Neuro
{
    public interface IReferenceResolver
    {
        T Get<T>(uint refId) where T : class, IReferencable;
    }
    
    public interface IReferenceResolver<T> where T : class, IReferencable
    {
        T Get(uint refId);
    }
}