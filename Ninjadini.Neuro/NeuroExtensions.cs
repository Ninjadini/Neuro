using System;
using System.Runtime.CompilerServices;

namespace Ninjadini.Neuro
{
    public static class NeuroExtensions
    {
        public static DateTime StripMicroSeconds(this DateTime dateTime)
        {
            return new DateTime((dateTime.Ticks / 10000L) * 10000L, dateTime.Kind);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetLinkedRef<T>(this NeuroReferences refs, IReferencable referencable) where T : class, IReferencable
        {
            return referencable != null ? refs.Get(typeof(T), referencable.RefId) as T : null;
        }
        
        public static string TryGetIdAndName(this IReferencable referencable)
        {
            if (referencable == null)
            {
                return "null";
            }
            var name = referencable.RefName;
            return string.IsNullOrEmpty(name) ? $"#{referencable.RefId.ToString()}" : $"#{referencable.RefId}:{name}";
        }
        
        public static string TryGetIdAndName<T>(this Reference<T> reference, NeuroReferences refs) where T : class, IReferencable
        {
            var value = reference.GetValue(refs);
            return value != null ? TryGetIdAndName(value) : $"#{reference.RefId.ToString()}";
        }
        
        public static string TryGetIdAndName<T>(this Reference<T> reference, NeuroReferenceTable<T> table) where T : class, IReferencable
        {
            var value = reference.GetValue(table);
            return value != null ? TryGetIdAndName(value) : $"#{reference.RefId.ToString()}";
        }
        
        
#if !NEURO_DISABLE_STATIC_REFERENCES
        public static string TryGetIdAndName<T>(this Reference<T> reference) where T : class, IReferencable
        {
            var value = reference.GetValue();
            return value != null ? TryGetIdAndName(value) : $"#{reference.RefId.ToString()}";
        }
#endif
        
        
    }
}