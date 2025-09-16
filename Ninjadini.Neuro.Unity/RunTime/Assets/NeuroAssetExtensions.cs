using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

namespace Ninjadini.Neuro
{
    public static class NeuroAssetExtensions
    {/*
        public static async Task<TObject> LoadAssetAsync<TObject>(this AssetAddress assetAddress) where TObject : Object
        {
            if (assetAddress.IsEmpty())
            {
                throw new Exception("Invalid asset address");
            }
            if (typeof(Component).IsAssignableFrom(typeof(TObject)))
            {
                throw new Exception($"Can't load component [{typeof(TObject)}] synchronously, TObject needs to be GameObject instead and get component manually after load.");
            }
            if (IsResourcePath(assetAddress.Address))
            {
                // TODO this might not be needed as addressable should auto detect resources path...
                var resReq = Resources.LoadAsync<TObject>(assetAddress.Address);
                await resReq;
                return resReq.asset as TObject;
            }
            var addressReq = Addressables.LoadAssetAsync<TObject>(assetAddress.Address);
            await addressReq.Task;
            return addressReq.Result;
        }*/
        
        public static TObject LoadFromResources<TObject>(this AssetAddress assetAddress) where TObject : Object
        {
            if (assetAddress.IsEmpty())
            {
                return null;
            }
            return Resources.Load<TObject>(assetAddress.Address);
        }
        
        public static void LoadAssetAsync<TObject>(this AssetAddress assetAddress, Action<TObject> callback) where TObject : Object
        {
            if (assetAddress.IsEmpty())
            {
                callback?.Invoke(default);
            }
            else if (IsResourcePath(assetAddress.Address)) // TODO this might not be needed as addressable should auto detect resources path...
            {
                var resReq = Resources.LoadAsync<TObject>(assetAddress.Address);
                resReq.completed += operation =>
                {
                    callback?.Invoke(resReq.asset as TObject);
                };
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

        static bool IsResourcePath(string path)
        {
            if (path.Length < 32) return true;
            if (path.Length > 32)
            {
                if (path[32] != '[') return true;
                if (path[^1] != ']') return true;
            }
            for (var i = 0; i < 32; i++)
            {
                var c = path[i];
                if (!((uint)(c - '0') <= 9 || (uint)(c - 'a') <= 5))
                {
                    return true;
                }
            }
            return false;
        }
        
        public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(this AssetAddress assetAddress)
        {
            return Addressables.LoadSceneAsync(assetAddress.Address);
        }
    }
}