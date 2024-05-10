using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    public static class NeuroJsonSyncTypes
    {
        public static bool IsEmpty<T>()
        {
            return NeuroJsonSyncTypes<T>._Delegate == null;
        }
        
        public static void Register<T>(FieldSizeType sizeType, NeuroSyncDelegate<T> d)
        {
            NeuroJsonSyncTypes<T>._SizeType = (uint)sizeType;
            NeuroJsonSyncTypes<T>._Delegate = d;
        }
    }

    internal static class NeuroJsonSyncTypes<T>
    {
        internal static NeuroSyncDelegate<T> _Delegate;
        internal static uint? _SizeType;

        internal static uint SizeType => _SizeType ?? NeuroSyncTypes<T>.SizeType;
        
        internal static NeuroSyncDelegate<T> GetOrThrow()
        {
            if (_Delegate == null)
            {
                return NeuroSyncTypes<T>.GetOrThrow();
            }
            return _Delegate;
        }
    }
}