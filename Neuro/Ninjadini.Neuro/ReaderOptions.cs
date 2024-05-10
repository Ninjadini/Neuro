namespace Ninjadini.Neuro
{
    public struct ReaderOptions
    {
        public INeuroObjectPool ObjectPool;

        public ReaderOptions(INeuroObjectPool objectPool = null)
        {
            ObjectPool = objectPool;
        }
    }
}