namespace Ninjadini.Neuro
{
    public interface IReferencable
    {
        uint RefId { get; set; }
        string RefName { get; set; }
    }
    
    public abstract class Referencable : IReferencable
    {
        public uint RefId { get; set; }
        public string RefName { get; set; }
    }
    
    /// Expects only 1 item in references list
    public interface ISingletonReferencable : IReferencable
    {
        uint IReferencable.RefId { get => 1; set { } }
        string IReferencable.RefName
        {
            get => "";
            set { }
        }
    }
}