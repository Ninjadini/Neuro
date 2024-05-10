using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ninjadini.Neuro;

public interface INeuroReferenceTable
{
    Type Type { get; }
    void Register(IReferencable referencable);
    void Register(uint refId, INeuroReferencedItemLoader loader);
    void Unregister(uint refId);
    IReferencable Get(uint refId);
    IReferencable Get(string refName);
    string GetRefName(uint refId);
    IEnumerable<IReferencable> SelectAll();
    IEnumerable<uint> GetIds();
    int Count { get; }
    void Clear();
}

public class NeuroReferenceTable<T> : INeuroReferenceTable where T: class,  IReferencable
{
    private NeuroReferences _refs;
    private Dictionary<uint, T> _byId = new Dictionary<uint, T>();
    private Dictionary<string, uint> _nameToId;
    private Dictionary<uint, INeuroReferencedItemLoader> _loaders = new Dictionary<uint, INeuroReferencedItemLoader>();

    public NeuroReferences Refs => _refs;

    public int Count => _byId.Count + _loaders.Count;
    
    public NeuroReferenceTable(NeuroReferences refs)
    {
        _refs = refs;
    }

    public void Register(IReferencable referencable)
    {
        var t = (T)referencable;
        _byId.Add(referencable.RefId, t);
        if (_nameToId != null)
        {
            _nameToId[referencable.RefName] = referencable.RefId;
        }
    }

    public void Register(uint refId, INeuroReferencedItemLoader loader)
    {
        _byId.Remove(refId);
        _loaders[refId] = loader;
    }
        
    public void Unregister(uint refId)
    {
        if(_byId.ContainsKey(refId))
        {
            _byId.Remove(refId);
        }
        else if(_loaders.ContainsKey(refId))
        {
            _loaders.Remove(refId);
        }
        _nameToId = null;
    }

    public IEnumerable<uint> GetIds()
    {
        foreach (var key in _byId.Keys)
        {
            yield return key;
        }
        foreach (var key in _loaders.Keys)
        {
            yield return key;
        }
    }

    public string GetRefName(uint refId)
    {
        if (_byId.TryGetValue(refId, out var result))
        {
            return result.RefName;
        }
        if(_loaders.TryGetValue(refId, out var loader))
        {
            return loader.GetRefName(refId);
        }
        return null;
    }

    Type INeuroReferenceTable.Type => typeof(T);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IReferencable INeuroReferenceTable.Get(uint refId) => Get(refId);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IReferencable INeuroReferenceTable.Get(string refId) => Get(refId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerable<IReferencable> INeuroReferenceTable.SelectAll() => SelectAll();

    public T Get(uint refId)
    {
        if (_byId.TryGetValue(refId, out var result))
        {
            return result;
        }
        return TryLoad(refId);
    }
    
    public T Get(string refName)
    {
        if (GetNameToIdMap().TryGetValue(refName, out var refId))
        {
            return Get(refId);
        }
        return default;
    }

    T TryLoad(uint refId)
    {
        if(_loaders.TryGetValue(refId, out var loader))
        {
            _loaders.Remove(refId);
            return Load(refId, loader);
        }
        return default;
    }

    void DeserializeAll()
    {
        foreach (var kv in _loaders)
        {
            Load(kv.Key, kv.Value);
        }
        _loaders.Clear();
    }
    
    T Load(uint refId, INeuroReferencedItemLoader loader)
    {
        var result = (T)loader.Load(refId);
        if (result.RefId == 0)
        {
            result.RefId = refId;
        }
        else if (result.RefId != refId)
        {
            throw new Exception($"Ref id does not match, expecting {refId}, got {result.RefId}");
        }
        var refName = loader.GetRefName(refId);
        result.RefName = refName;
        if (_nameToId != null)
        {
            _nameToId[refName] = refId;
        }
        Register(result);
        return result;
    }

    public bool IsAllLoaded() => _loaders.Count == 0;

    public bool IsLoaded(uint refId) => _byId.ContainsKey(refId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> SelectAll()
    {
        return GetDictionary().Select(kv => kv.Value);
    }
    
    public IReadOnlyDictionary<uint, T> GetDictionary()
    {
        if (_loaders.Count > 0)
        {
            DeserializeAll();
        }
        return _byId;
    }
    
    public IReadOnlyDictionary<string, uint> GetNameToIdMap()
    {
        if (_nameToId == null)
        {
            _nameToId = new Dictionary<string, uint>(_byId.Count);
            foreach (var kv in _byId)
            {
                var name = kv.Value.RefName;
                if (!string.IsNullOrEmpty(name))
                {
#if DEBUG
                    _nameToId.Add(name, kv.Key);
#else
                    _nameToId[name] = kv.Key;
#endif
                }
            }
            foreach (var kv in _loaders)
            {
                var name = kv.Value.GetRefName(kv.Key);
                if (!string.IsNullOrEmpty(name))
                {
#if DEBUG
                    _nameToId.Add(name, kv.Key);
#else
                    _nameToId[name] = kv.Key;
#endif
                }
            }
        }
        return _nameToId;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NeuroReferenceTable<T>(NeuroReferences refs) => refs.GetTable<T>();

    public void Clear()
    {
        _byId.Clear();
        _loaders.Clear();
        _nameToId = null;
    }
}