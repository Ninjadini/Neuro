using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ninjadini.Neuro.Sync;
using UnityEngine;

namespace Ninjadini.Neuro
{
    public class NeuroDataProvider : IReferencesProvider
    {
        public const string BinaryResourceName = "NeuroData";
        public const string BinaryResourceExtension = "bytes";

        private IReferencesProvider _referencesProvider;
        readonly bool _loadFromResFile;
        bool _loadingAsync;

        public NeuroDataProvider(bool loadFromResFile = false)
        {
            _loadFromResFile = loadFromResFile;
        }

        public async Task LoadFromResAsync(bool forceLoadFromResInEditor = false)
        {
            const string alreadyLoadedStr = "LoadFromResAsync did nothing because it appears to be already loaded, possibly from a different call.";
            
            if (__references != null)
            {
                Debug.LogWarning(alreadyLoadedStr);
                return;
            }
            if (_loadingAsync)
            {
                while (_loadingAsync)
                {
                    Thread.Sleep(20);
                }
                return;
            }

            _loadingAsync = true;
            
            if (_referencesProvider != null)
            {
                // doesn't really change much for editor path where we load from neuro editor / json files.
                __references = _referencesProvider.References;
                if (__references == null)
                {
                    throw new Exception($"{nameof(IReferencesProvider)} returned null reference");
                }
            }
            else
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                var req = Resources.LoadAsync<TextAsset>("NeuroData");
                req.completed += operation =>
                {
                    taskCompletionSource.SetResult(true);
                };
                await taskCompletionSource.Task;
                var bytes = req.asset is TextAsset textAsset ? textAsset.bytes : null;
                if (bytes == null)
                {
                    Debug.LogError("Neuro data resource file not found");
                }
                await Task.Run(() =>
                {
                    try
                    {
                        if (__references == null)
                        {
                            __references = NeuroReferences.Default ??= new NeuroReferences();
                            NeuroSyncTypes.TryRegisterAllAssemblies();
                            bytes = RawProtoReader.Decompress(bytes);
                            new NeuroBytesReader().ReadReferencesListInto(__references, bytes);
                        }
                        else
                        {
                            Debug.LogWarning(alreadyLoadedStr);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to read ({BinaryResourceName}) from resources. Error: " + e);
                    }
                });
            }
            _loadingAsync = false;
        }

        public bool LoadingAsync => _loadingAsync;

        private NeuroReferences __references;
        public NeuroReferences References
        {
            get
            {
                if(__references == null)
                {
                    if (_loadingAsync)
                    {
                        throw new Exception("Cannot access NeuroReferences while LoadFromResAsync() is not finished, consider just not calling it and let neuro use async loading.");
                    }
                    if (_referencesProvider != null)
                    {
                        __references = _referencesProvider.References;
                        if (__references == null)
                        {
                            throw new Exception($"{nameof(IReferencesProvider)} returned null reference");
                        }
                    }
                    else
                    {
                        __references = NeuroReferences.Default ??= new NeuroReferences();
                        if (_loadFromResFile)
                        {
                            var startTime = DateTime.UtcNow;
                            NeuroSyncTypes.TryRegisterAllAssemblies();
                            var bytes = LoadFromBinary();
                            Debug.Log($"Neuro ~ Loaded from {bytes:N0}bytes binary in {(DateTime.UtcNow - startTime).TotalMilliseconds:N0}ms");
                        }
                    }
                }
                return __references;
            }
        }

        public T Get<T>(Reference<T> reference) where T : class, IReferencable
        {
            return reference.GetValue(References);
        }
        
        public void SetReferenceProvider(IReferencesProvider referencesProvider)
        {
            if (__references != null)
            {
                throw new Exception($"{nameof(NeuroReferences)} already set, this is called too late");
            }
            _referencesProvider = referencesProvider;
        }

        int LoadFromBinary()
        {
            var textAsset = Resources.Load<TextAsset>(BinaryResourceName);
            if (textAsset == null)
            {
                Debug.LogError($"Data resource file ({BinaryResourceName}) not found in Resources.");
            }
            else
            {
                try
                {
                    var bytes =  RawProtoReader.Decompress(textAsset.bytes);
                    new NeuroBytesReader().ReadReferencesListInto(References, bytes);
                    return bytes.Length;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to read ({BinaryResourceName}) from Resources: " + e);
                }
            }
            return 0;
        }
        
        private static NeuroDataProvider __shared;
        public static NeuroDataProvider Shared
        {
            get
            {
                if (__shared == null)
                {
                    __shared = new NeuroDataProvider(true);
                }
                return __shared;
            }
        }
        public static NeuroReferences SharedReferences => Shared.References;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NeuroReferenceTable<T> GetSharedTable<T>() where T: class,  IReferencable
        {
            return Shared.References.GetTable<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static INeuroReferenceTable GetSharedTable(Type type)
        {
            return Shared.References.GetTable(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSharedSingleton<T>() where T : class, ISingletonReferencable
        {
            return Shared.References.Get<T>();
        }
    }
}