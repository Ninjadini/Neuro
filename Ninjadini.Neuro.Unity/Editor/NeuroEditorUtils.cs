using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Ninjadini.Neuro.Utils;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    public static class NeuroEditorUtils
    {
        static ReadOnlyCollection<Type> _allScannableTypes;

        public static IReadOnlyList<Type> GetAllScannableTypes()
        {
            if (_allScannableTypes == null)
            {
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
    }
}