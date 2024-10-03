using System;
using System.Collections;
using System.Runtime.CompilerServices;
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

        ///  TODO, actually try it out
        public IEnumerator LoadFromResAsync()
        {
            _loadingAsync = true;
            var req = Resources.LoadAsync<TextAsset>("NeuroData");
            while (!req.isDone)
            {
                yield return null;
            }
            try
            {
                var textAsset = req.asset as TextAsset;
                var bytes = RawProtoReader.Decompress(textAsset.bytes);
                new NeuroBytesReader().ReadReferencesListInto(References, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read ({BinaryResourceName}) from Resources: " + e);
            }
            finally
            {
                _loadingAsync = false;
            }
        }

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