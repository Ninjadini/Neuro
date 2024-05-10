namespace Ninjadini.Neuro
{
    public interface INeuroPoolable
    {

    }
    public interface INeuroObjectPool
    {
        T Borrow<T>() where T : class;
        void Return(object obj);
    }
}