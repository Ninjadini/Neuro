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
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public static class NeuroEditorUtils
    {
        static ReadOnlyCollection<Type> _allScannableTypes;

        /// How a RefId is shown in the editor UI - base36, plus the plain number after it when
        /// `Show Raw Ref Id Numbers` is turned on in the Neuro settings.
        /// Display only. Never use it where the text is read back, such as file names or the RefId field.
        public static string DisplayRefId(uint refId)
        {
            var text = NeuroRefId.ToString(refId);
            return NeuroUnityUserSettings.Get().ShowRawRefIdNumbers ? text + " (" + refId.ToString() + ")" : text;
        }

        /// The editor's version of IReferencable.TryGetIdAndName(), honouring `Show Raw Ref Id Numbers`.
        public static string DisplayIdAndName(IReferencable referencable)
        {
            if (referencable == null)
            {
                return "null";
            }
            var id = DisplayRefId(referencable.RefId);
            var name = referencable.RefName;
            return string.IsNullOrEmpty(name) ? $"#{id}" : $"#{id}:{name}";
        }

        public static IReadOnlyList<Type> GetAllScannableTypes()
        {
            if (_allScannableTypes != null)
            {
                return _allScannableTypes;
            }
            
            var startTime = DateTime.Now;
            
#if UNITY_6000_5_OR_NEWER
            var assemblies = UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies();
#else
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
#endif 
            var result = assemblies
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

            if (NeuroUnityUserSettings.Get().LogTimings)
            {
                var timeTaken = DateTime.Now - startTime;
                Debug.Log($"All scannable types found in {timeTaken.TotalMilliseconds}ms");
            }

            return _allScannableTypes;
        }

        public static IEnumerable<Type> SafeGetExportedTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetExportedTypes();
            }
            catch (Exception)
            {
                return Array.Empty<Type>();
            }
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
#if UNITY_6000_5_OR_NEWER
            var assemblies = UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies();
#else
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
#endif
            return (from domainAssembly in assemblies
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