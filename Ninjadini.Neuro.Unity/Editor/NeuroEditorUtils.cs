using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Ninjadini.Neuro.Sync;
using Ninjadini.Neuro.Utils;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    public static class NeuroEditorUtils
    {
        static ReadOnlyCollection<Type> _allScannableTypes;

        public static IReadOnlyList<Type> GetAllScannableTypes()
        {
            if (_allScannableTypes != null)
            {
                return _allScannableTypes;
            }
            
            var startTime = DateTime.Now;
            var result = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (Exception)
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t =>
                {
                    if (t.IsClass
                        && !t.IsAbstract
                        && typeof(IAssemblyTypeScannable).IsAssignableFrom(t))
                    {
                        if (t.GetConstructor(Type.EmptyTypes) != null)
                        {
                            return true;
                        }
                        Debug.LogError($"{t} is a {nameof(IAssemblyTypeScannable)} type but does not have a parameterless constructor.");
                    }
                    return false;
                })
                .ToArray();
            _allScannableTypes = new ReadOnlyCollection<Type>(result);

            if (NeuroUnityEditorSettings.Get().LogTimings)
            {
                var timeTaken = DateTime.Now - startTime;
                Debug.Log($"All scannable types found in {timeTaken.TotalMilliseconds}ms");
            }

            return _allScannableTypes;
        }

        public static IEnumerable<Type> SelectScannableTypes<T>() where T : IAssemblyTypeScannable
        {
            return GetAllScannableTypes().Where(t => typeof(T).IsAssignableFrom(t));
        }

        public static T[] CreateFromScannableTypes<T>() where T : IAssemblyTypeScannable
        {
            return SelectScannableTypes<T>()
                .Select(t => (T)Activator.CreateInstance(t))
                .ToArray();
        }

        public static void ClearScannableTypesCache()
        {
            _allScannableTypes = null;
        }
        
        static string _uniqueProjectPathHash;
        public static string UniqueProjectPathHash
        {
            get
            {
                return _uniqueProjectPathHash ??= string.Join("", Encoding.UTF8.GetBytes(Application.dataPath).Select(b => b.ToString("x2")));
            }
        }

        public static Type[] FindAllNeuroTypes()
        {
            return (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                    where !domainAssembly.IsDynamic && domainAssembly.IsDefined(typeof(NeuroAssemblyAttribute))
                    where NeuroSyncTypes.TryRegisterAssembly(domainAssembly)
                    from type in domainAssembly.GetExportedTypes()
                    where type.IsClass && !type.IsGenericType
                                       && NeuroSyncTypes.CheckIfTypeRegisteredUsingReflection(type)
                    select type)
                .ToArray();
        }

        static Type[] _allNeuroTypes;
        public static Type[] FindAllNeuroTypesCached()
        {
            _allNeuroTypes ??= FindAllNeuroTypes();
            return _allNeuroTypes;
        }

        public static string GetTypeName(Type type)
        {
            var result = type.Name;
            type = type.DeclaringType;
            while (type != null)
            {
                result = type.Name + "." + result;
                type = type.DeclaringType;
            }
            return result;
        }
    }
}