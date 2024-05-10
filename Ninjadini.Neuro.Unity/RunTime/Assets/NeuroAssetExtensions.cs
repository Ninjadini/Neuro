using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

namespace Ninjadini.Neuro
{
    public static class NeuroAssetExtensions
    {
        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(this AssetAddress assetAddress)
        {
            if (assetAddress.IsEmpty())
            {
                throw new Exception("Invalid asset address");
            }
            if (typeof(Component).IsAssignableFrom(typeof(TObject)))
            {
                throw new Exception($"Can't load component [{typeof(TObject)}] synchronously, TObject needs to be GameObject instead and get component manually after load.");
            }
            return Addressables.LoadAssetAsync<TObject>(assetAddress.Address);
        }
        
        public static TObject LoadFromResources<TObject>(this AssetAddress assetAddress) where TObject : Object
        {
            if (assetAddress.IsEmpty())
            {
                return null;
            }
            return Resources.Load<TObject>(assetAddress.Address);
        }
        
        public static void LoadAssetAsync<TObject>(this AssetAddress assetAddress, Action<TObject> callback)
        {
            if (assetAddress.IsEmpty())
            {
                callback?.Invoke(default);
            }
            else if (typeof(Component).IsAssignableFrom(typeof(TObject)))
            {
                Addressables.LoadAssetAsync<GameObject>(assetAddress.Address).Completed += delegate(AsyncOperationHandle<GameObject> handle)
                {
                    var component = handle.Result ? handle.Result.GetComponent<TObject>() : default;
                    if (component == null)
                    {
                        throw new Exception($"Can't find component [{typeof(TObject)}] on prefab (or prefab is null)");
                    }
                    callback?.Invoke(component);
                };
            }
            else
            {
                Addressables.LoadAssetAsync<TObject>(assetAddress.Address).Completed += delegate(AsyncOperationHandle<TObject> handle)
                {
                    callback?.Invoke(handle.Result);
                };
            }
        }
        
        public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(this AssetAddress assetAddress)
        {
            return Addressables.LoadSceneAsync(assetAddress.Address);
        }
    }
}