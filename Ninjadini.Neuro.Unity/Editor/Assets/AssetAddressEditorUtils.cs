using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ninjadini.Neuro.Editor
{
    public static class AssetAddressEditorUtils
    {
        const string ResourceDirPart = "/Resources/";

        public static bool IsAddressablePath(string path)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return false;
            }
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            return entry != null;
        }
        
        public static bool IsInResourcePath(string path)
        {
            return path.Contains(ResourceDirPart);
        }
        
        public static string FullPathToResourcesPath(string path)
        {
            var index = path.IndexOf(ResourceDirPart, StringComparison.Ordinal);
            if (index < 0)
            {
                return null;
            }
            var subPath = path.Substring(index + ResourceDirPart.Length);
            var dir = Path.GetDirectoryName(subPath);
            if (string.IsNullOrEmpty(dir))
            {
                return Path.GetFileNameWithoutExtension(subPath);
            }
            return dir + "/" + Path.GetFileNameWithoutExtension(subPath);
        }

        public static string GetAddress(Object obj)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            var resPath = AssetAddressEditorUtils.FullPathToResourcesPath(path);
            string guidOrPath;
            if (!string.IsNullOrEmpty(resPath) && !AssetAddressEditorUtils.IsAddressablePath(path))
            {
                guidOrPath = resPath;
            }
            else
            {
                guidOrPath = AssetDatabase.AssetPathToGUID(path);
                if (AssetDatabase.IsSubAsset(obj))
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    if (assets.Length > 2)
                    {
                        return $"{guidOrPath}[{obj.name}]";
                    }
                }
            }
            return guidOrPath;
        }

        public static Object LoadObjectFromAddress(string address)
        {
            ExtractPathAndSubAsset(address, out var guidOrPath, out var subAsset);
            if (string.IsNullOrEmpty(guidOrPath))
            {
                return null;
            }
            Object obj = null;
            var path = AssetDatabase.GUIDToAssetPath(guidOrPath);
            if (!string.IsNullOrEmpty(path))
            {
                if(!string.IsNullOrEmpty(subAsset))
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in assets)
                    {
                        if (asset.name == subAsset)
                        {
                            obj = asset;
                            break;
                        }
                    }
                }
                if (!obj)
                {
                    obj = AssetDatabase.LoadMainAssetAtPath(path);
                }
            }
            if (!obj)
            {
                obj = Resources.Load(guidOrPath);
            }
            return obj;
        }

        public static void ExtractPathAndSubAsset(string address, out string pathOrGuid, out string subAsset)
        {
            if (string.IsNullOrEmpty(address))
            {
                pathOrGuid = subAsset = null;
            }
            else
            {
                var match = Regex.Match(address, @"[a-z0-9]{32}\[(.+?)\]$");
                if (match.Success)
                {
                    pathOrGuid = match.Groups[1].Value;
                    subAsset = match.Groups[2].Value;
                }
                else
                {
                    pathOrGuid = address;
                    subAsset = null;
                }
            }
        }

        public static Type TryGetAssetType(MemberInfo memberInfo)
        {
            if (memberInfo != null && memberInfo.IsDefined(typeof(AssetTypeAttribute), true))
            {
                var attribute = memberInfo.GetCustomAttribute<AssetTypeAttribute>();
                if (attribute != null)
                {
                    if (attribute.Type != null)
                    {
                        return attribute.Type;
                    }
                    else if (!string.IsNullOrEmpty(attribute.TypeString))
                    {
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            var t = assembly.GetType(attribute.TypeString);
                            if (t != null && t.FullName == attribute.TypeString)
                            {
                                attribute.Type = t;
                                return t;
                            }
                        }
                    }
                }
            }
            return null;
        }
        public static bool PrepObjectLinkable(Object obj)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (!IsInResourcePath(path) && !IsAddressablePath(path))
            {
                if (!EditorUtility.DisplayDialog("", "Object is not in Resources and not Addressable.\nMark it as Addressable now?", "Mark Addressable", "Cancel"))
                {
                    return false;
                }
                MakeAddressable(obj);
            }
            return true;
        }

        public static void MakeAddressable(Object obj)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("", "Addressables not set up. Go to 'Window → Asset Management → Addressables → Groups' first.", "OK");
                return;
            }
            if (path.ToLower().Contains("/resources/"))
            {
                EditorUtility.DisplayDialog("", "Object is already in resources folder", "OK");
                return;
            }
            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                return;
            }
            entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            if (entry != null)
            {
                entry.address = obj.name; // Use asset name as address
                Debug.Log($"✅ Made {obj.name} Addressable (address: {entry.address})");
            }
            AssetDatabase.SaveAssets();
        }
    }
}