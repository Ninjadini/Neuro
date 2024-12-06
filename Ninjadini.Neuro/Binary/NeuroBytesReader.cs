using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    public class NeuroBytesReader : INeuroSync
    {
        [ThreadStatic] private static NeuroBytesReader _shared;
        public static NeuroBytesReader Shared => _shared ??= new NeuroBytesReader();

        RawProtoReader proto = new RawProtoReader();

        private ReaderOptions options;
        private uint nextKey;
        private uint nextHeader;

        public ReaderOptions Options => options;
        
        const uint BaseClassKey = uint.MaxValue - 1; 

        public T Read<T>(in BytesChunk bytesChunk, in ReaderOptions opts = default)
        {
            var result = default(T);
            Read(bytesChunk, ref result, opts);
            return result;
        }

        public void Read<T>(in BytesChunk bytesChunk, ref T result,  in ReaderOptions opts = default)
        {
            if (bytesChunk.Length == 0)
            {
                result = default;
                return;
            }
            if (typeof(T) == typeof(object))
            {
                object obj = result;
                ReadGlobalTyped(bytesChunk, ref obj, opts);
                if (obj != null)
                {
                    result = (T)obj;
                }
                else
                {
                    result = default;
                }
                return;
            }
            proto.Set(bytesChunk);
            options = opts;
            nextKey = 0;
            var sizeType = proto.ReadUint() & NeuroConstants.HeaderMask;
            if (sizeType == NeuroConstants.ChildWithType)
            {
                var tag = proto.ReadUint();
                NeuroSyncTypes.TryRegisterAssemblyOf<T>();
                NeuroSyncSubTypes<T>.Sync(this, tag, ref result);
            }
            else if (sizeType == NeuroConstants.Child)
            {
                NeuroSyncTypes<T>.GetOrThrow()(this, ref result);
            }
            else
            {
                throw new Exception("Invalid type " + sizeType);
            }
            if (proto.Available > 1) // 1 byte left is fine, thats the group end tag.
            {
                throw new System.Exception($"Did not reach end of stream, {proto.Available} bytes left.");
            }
            proto.Set(Array.Empty<byte>());
        }

        /// WARNING: THIS IS SLOW due to needing reflection and allocation
        public object Read(in BytesChunk bytesChunk, Type type, in ReaderOptions opts = default)
        {
            proto.Set(bytesChunk);
            options = opts;
#pragma warning disable CS0162 // Unreachable code detected
            if (false)
            {
                _ReadByReflection<object>();
                // ^ This is just a cheap way to try preserve the method in Unity.
            }
#pragma warning restore CS0162 // Unreachable code detected
            var method = GetType().GetMethod("_ReadByReflection",  BindingFlags.Instance | BindingFlags.NonPublic);
            var genericMethod = method.MakeGenericMethod(type);
            return genericMethod.Invoke(this, Array.Empty<object>());
        }

        object _ReadByReflection<T>()
        {
            var result = default(T);
            var chunk = proto.GetCurrentBytesChunk();
            chunk.Length = proto.Available;
            Read(chunk, ref result, options);
            return result;
        }

        public object ReadGlobalType(Type type, in BytesChunk bytesChunk, in ReaderOptions opts)
        {
            var typeId = NeuroGlobalTypes.GetIdByType(type);
            if (typeId == 0)
            {
                if (type.IsDefined(typeof(NeuroGlobalTypeAttribute), true))
                {
                    throw new Exception($"Type {type} is not registered yet.");
                }
                else
                {
                    throw new Exception($"Type {type.FullName} does not have NeuroGlobalType attribute. it can not be read via non generic Read method. Use Read<T>() instead.");
                }
            }
            
            object result = null;
            proto.Set(bytesChunk);
            options = opts;
            nextKey = 0;
            
            var sizeType = proto.ReadUint() & NeuroConstants.HeaderMask;
            if (sizeType == NeuroConstants.ChildWithType)
            {
                var tag = proto.ReadUint();
                NeuroGlobalTypes.Sync(typeId, this, tag, ref result);
            }
            else if (sizeType == NeuroConstants.Child)
            {
                NeuroGlobalTypes.Sync(typeId, this, 0, ref result);
            }
            if (proto.Available > 1) // 1 byte left is fine, thats the group end tag.
            {
                throw new System.Exception($"Did not reach end of stream, {proto.Available} bytes left.");
            }
            proto.Set(Array.Empty<byte>());
            return result;
        }
        
        public object ReadGlobalTyped(in BytesChunk bytesChunk, in ReaderOptions opts = default)
        {
            object result = null;
            ReadGlobalTyped(bytesChunk, ref result, opts);
            return result;
        }

        public void ReadGlobalTyped(in BytesChunk bytesChunk, ref object result, in ReaderOptions opts = default)
        {
            proto.Set(bytesChunk);
            options = opts;
            nextKey = 0;
            
            var sizeType = proto.ReadUint() & NeuroConstants.HeaderMask;
            if ((sizeType & NeuroConstants.HeaderMask) != NeuroConstants.VarInt)
            {
                throw new Exception("Invalid header " + sizeType);
            }
            var typeId = proto.ReadUint();
            var tag = 0u;
            sizeType = proto.ReadUint() & NeuroConstants.HeaderMask;
            if (sizeType == NeuroConstants.ChildWithType)
            {
                tag = proto.ReadUint();
            }
            NeuroGlobalTypes.Sync(typeId, this, tag, ref result);
            
            if (proto.Available > 1) // 1 byte left is fine, thats the group end tag.
            {
                throw new System.Exception($"Did not reach end of stream, {proto.Available} bytes left.");
            }
            proto.Set(Array.Empty<byte>());
        }

        public List<object> ReadGlobalTypedList(BytesChunk bytesChunk, ReaderOptions opts)
        {
            proto.Set(bytesChunk);
            options = opts;
            nextKey = 0;
            var result = new List<object>();

            while (proto.Available > 1)
            {
                nextHeader = proto.ReadUint();
                if ((nextHeader & NeuroConstants.HeaderMask) != NeuroConstants.VarInt)
                {
                    throw new Exception("Invalid header " + nextHeader);
                }
                var globalId = proto.ReadUint();
                if (!NeuroGlobalTypes.TypeIdExists(globalId))
                {
                    throw new NotImplementedException( $"Global id {globalId} isn't registered (this could be due to code stripping). Need ability to skip + report problems");
                    //continue;
                }
                nextHeader = proto.ReadUint();
                if ((nextHeader & NeuroConstants.HeaderMask) != NeuroConstants.ChildWithType)
                {
                    throw new Exception("Invalid header " + nextHeader);
                }
                var countLeft = 1u;
                if ((nextHeader & NeuroConstants.HeaderMask) != 0)
                {
                    countLeft = proto.ReadUint();
                }
                while (countLeft > 0)
                {
                    countLeft--;
                    nextKey = 0;
                    var tag = proto.ReadUint();
                    object item = null;
                    NeuroGlobalTypes.Sync(globalId, this, tag, ref item);
                    SeekKey(uint.MaxValue);
                    result.Add(item);
                }
            }
            proto.Set(Array.Empty<byte>());
            return result;
        }

        public void ReadReferencesListInto(NeuroReferences neuroReferences, BytesChunk bytesChunk)
        {
            proto.Set(bytesChunk);
            nextKey = 0;
            while (proto.Available > 1)
            {
                var globalId = proto.ReadUint();
                var type = NeuroGlobalTypes.FindTypeById(globalId);
                var countLeft = proto.ReadUint();
                while (countLeft > 0)
                {
                    countLeft--;
                    var namePos = proto.Position;
                    var nameSize = (int)proto.ReadUint();
                    proto.Skip(nameSize);
                    var size = (int)proto.ReadUint();
                    var start = proto.Position;
                    var contentByteChunk = new BytesChunk()
                    {
                        Bytes = bytesChunk.Bytes,
                        Position = start,
                        Length = size
                    };
                    var refId = proto.ReadUint() >> NeuroConstants.HeaderShift;
                    proto.Skip(size - (proto.Position - start));
                    if (type != null)
                    {
                        neuroReferences.GetTable(type).Register(refId, new LocalRefProvider()
                        {
                            type = type,
                            reader = this,
                            namePos = namePos,
                            contentByteChunk = contentByteChunk
                        });
                    }
                    nextKey = 0;
                }
            }
            proto.Set(Array.Empty<byte>());
        }

        class LocalRefProvider : INeuroReferencedItemLoader
        {
            internal Type type;
            internal NeuroBytesReader reader;
            internal int namePos;
            internal BytesChunk contentByteChunk;
            private IReferencable data;
            
            public IReferencable Load(uint refId)
            {
                if (data != null)
                {
                    return data;
                }
                var obj = (IReferencable)reader.ReadGlobalType(type, contentByteChunk, reader.Options);
                obj.RefId = refId;
                obj.RefName = GetRefName(refId);
                contentByteChunk.Bytes = null;
                return obj;
            }

            private string _refName;
            public string GetRefName(uint refId)
            {
                if (data != null)
                {
                    return data.RefName;
                }
                if (_refName == null)
                {
                    reader.proto.Set(contentByteChunk.Bytes, namePos);
                    _refName = reader.proto.ReadString();
                    reader.proto.Set(Array.Empty<byte>());
                }
                return _refName;
            }
        }
        

        T INeuroSync.GetPooled<T>()
        {
            return options.ObjectPool?.Borrow<T>();
        }

        bool INeuroSync.IsReading => true;
        
        void INeuroSync.Sync(ref bool value)
        {
            value = proto.ReadBool();
        }

        void INeuroSync.Sync(ref int value)
        {
            value = proto.ReadInt();
        }
        void INeuroSync.Sync(ref uint value)
        {
            value = proto.ReadUint();
        }

        void INeuroSync.Sync(ref long value)
        {
            value = proto.ReadLong();
        }

        void INeuroSync.Sync(ref ulong value)
        {
            value = proto.ReadULong();
        }

        void INeuroSync.Sync(ref float value)
        {
            value = proto.ReadFloat();
        }

        void INeuroSync.Sync(ref double value)
        {
            value = proto.ReadDouble();
        }

        void INeuroSync.Sync(ref string value)
        {
            value = proto.ReadString();
        }

        void INeuroSync.Sync<T>(ref Reference<T> value)
        {
            value.RefId = proto.ReadUint();
        }

        void INeuroSync.SyncEnum<T>(ref int value)
        {
            value = proto.ReadInt();
        }
        
        void INeuroSync.Sync<T>(uint key, string name, ref T value, T defaultValue)
        {
            if(key == 0 || SeekKey(key))
            {
                var sizeType = nextHeader & NeuroConstants.HeaderMask;
                if (sizeType >= NeuroConstants.Child)
                {
                    nextKey = 0;
                    NeuroSyncTypes<T>.GetOrThrow()(this, ref value);
                    if (nextKey > 0)
                    {
                        SeekKey(uint.MaxValue);
                    }
                    nextKey = key;
                }
                else
                {
                    NeuroSyncTypes<T>.GetOrThrow()(this, ref value);
                }
            }
            else
            {
                value = defaultValue;
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T? value) where T : struct
        {
            if(key == 0 || SeekKey(key))
            {
                var sizeType = nextHeader & NeuroConstants.HeaderMask;
                T localValue = default;
                if (sizeType >= NeuroConstants.Child)
                {
                    nextKey = 0;
                    NeuroSyncTypes<T>.GetOrThrow()(this, ref localValue);
                    if (nextKey > 0)
                    {
                        SeekKey(uint.MaxValue);
                    }
                    nextKey = key;
                }
                else
                {
                    NeuroSyncTypes<T>.GetOrThrow()(this, ref localValue);
                }
                value = localValue;
            }
            else
            {
                value = null;
            }
        }

        void INeuroSync.SyncEnum<T>(uint key, string name, ref T value, int defaultValue)
        {
            if(key == 0 || SeekKey(key))
            {
                NeuroSyncTypes<T>.GetOrThrow()(this, ref value);
            }
            else
            {
                value = NeuroSyncEnumTypes<T>.GetEnum(defaultValue);
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value)
        {
            if(key == 0 || SeekKey(key))
            {
                var sizeType = nextHeader & NeuroConstants.HeaderMask;
                if (sizeType >= NeuroConstants.Child)
                {
                    nextKey = 0;
                    if (sizeType == NeuroConstants.ChildWithType)
                    {
                        var tag = proto.ReadUint();
                        NeuroSyncSubTypes<T>.Sync(this, tag, ref value);
                    }
                    else
                    {
                        NeuroSyncTypes<T>.GetOrThrow()(this, ref value);
                    }
                    SeekKey(uint.MaxValue);
                    nextKey = key;
                }
                else
                {
                    NeuroSyncTypes<T>.GetOrThrow()(this, ref value);
                }
            }
            else
            {
                value = default;
            }
        }

        void INeuroSync.SyncBaseClass<TRoot, TBase>(TBase value)
        {
            if (nextHeader == NeuroConstants.EndOfChild)
            {
                return;
            }
            if (!SeekKey(BaseClassKey))
            {
                return;
            }
            nextKey = 0;
            var baseValue = (TRoot)value;
            NeuroSyncSubTypes<TRoot>.GetOrThrow(typeof(TBase))(this, ref baseValue);
            SeekKey(uint.MaxValue);
            if (nextHeader == NeuroConstants.EndOfChild)
            {
                nextKey = 0;
                SeekKey(uint.MaxValue);
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            if(key == 0 || SeekKey(key))
            {
                if ((nextHeader & NeuroConstants.HeaderMask) == NeuroConstants.EndOfChild)
                {
                    values ??= new List<T>();
                    values.Clear();
                    return;
                }
                var header = proto.ReadUint();
                ReadCollectionTypeAndSize(header, out var sizeType, out var count, out var containsNulls);
                if (values == null)
                {
                    values = new List<T>(count);
                }
                else if(values.Count > count)
                {
                    values.RemoveRange(count, values.Count - count);
                }
                else if(values.Capacity < count)
                {
                    values.Capacity = count;
                }
                var del = NeuroSyncTypes<T>.GetOrThrow();
                
                for(var i = 0; i < count; i++)
                {
                    nextKey = 0;
                    T value = i < values.Count ? values[i] : default;
                    if (sizeType == NeuroConstants.ChildWithType)
                    {
                        var itemTypeTagOrNull = proto.ReadUint();
                        if (itemTypeTagOrNull != 0)
                        {
                            NeuroSyncSubTypes<T>.Sync(this, itemTypeTagOrNull - 1, ref value);
                            SeekKey(uint.MaxValue);
                        }
                    }
                    else if (containsNulls && proto.ReadUint() == 0)
                    {
                        // null
                    }
                    else
                    {
                        del(this, ref value);
                        if (sizeType >= NeuroConstants.Child)
                        {
                            SeekKey(uint.MaxValue);
                        }
                    }
                    if (i < values.Count)
                    {
                        values[i] = value;
                    }
                    else
                    {
                        values.Add(value);
                    }
                }
                nextKey = key;
            }
            else if(values != null)
            {
                values.Clear();
                values = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReadCollectionTypeAndSize(uint value, out uint type, out int size, out bool containsNulls)
        {
            type = value & NeuroConstants.CollectionTypeHeaderMask;
            containsNulls = (value & NeuroConstants.CollectionHasNullMask) != 0;
            size = (int)(value >> NeuroConstants.HeaderShift);
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            if(key == 0 || SeekKey(key))
            {
                if ((nextHeader & NeuroConstants.HeaderMask) == NeuroConstants.EndOfChild)
                {
                    values ??= new Dictionary<TKey, TValue>();
                    values.Clear();
                    return;
                }
                var vSizeType = proto.ReadUint() >> NeuroConstants.HeaderShift;
                
                var count = (int)proto.ReadUint();
                values ??= new Dictionary<TKey, TValue>(count);
                values.Clear();
                
                var kDel = NeuroSyncTypes<TKey>.GetOrThrow();
                var vDel = NeuroSyncTypes<TValue>.GetOrThrow();
                for (var i = 0; i < count; i++)
                {
                    TKey itemKey = default;
                    kDel(this, ref itemKey);
                    
                    TValue itemValue = default;
                    nextKey = 0;
                    var itemHeader = proto.ReadUint();
                    if (itemHeader == 0)
                    {
                        // null
                    }
                    else if (vSizeType == NeuroConstants.ChildWithType)
                    {
                        NeuroSyncSubTypes<TValue>.Sync(this, itemHeader - 1, ref itemValue);
                        SeekKey(uint.MaxValue);
                    }
                    else
                    {
                        vDel(this, ref itemValue);
                        if (vSizeType >= NeuroConstants.Child)
                        {
                            SeekKey(uint.MaxValue);
                        }
                    }
                    values[itemKey] = itemValue;
                }
                nextKey = key;
            }
            else if(values != null)
            {
                values.Clear();
                values = null;
            }
        }

        bool SeekKey(uint key)
        {
            if (nextKey == key)
            {
                return true;
            }
            if (nextKey > key)
            {
                return false;
            }
            while(proto.Available > 0)
            {
                nextHeader = proto.ReadUint();
                if (nextHeader == NeuroConstants.EndOfChild)
                {
                    nextKey = uint.MaxValue;
                    return false; // reached end of group or just before start of base class
                }
                if (nextHeader == NeuroConstants.ChildWithType)
                {
                    nextKey = BaseClassKey;
                }
                else
                {
                    nextKey += nextHeader >> NeuroConstants.HeaderShift;
                }
                if (nextKey == key)
                {
                    return true;
                }
                if (nextKey > key)
                {
                    return false;
                }
                Skip(nextHeader);
            }
            return false;
        }

        void Skip(uint header, uint? subClassTag = null)
        {
            var sizeType = header & NeuroConstants.HeaderMask;
            switch (sizeType)
            {
                case NeuroConstants.VarInt:
                    proto.ReadUint();
                    break;
                case NeuroConstants.Fixed32:
                    proto.Skip(4);
                    break;
                case NeuroConstants.Fixed64:
                    proto.Skip(8);
                    break;
                case NeuroConstants.Length:
                    proto.Skip((int)proto.ReadUint());
                    break;
                case NeuroConstants.List:
                    SkipList();
                    break;
                case NeuroConstants.Dictionary:
                    SkipDictionary();
                    break;
                case NeuroConstants.Child:
                {
                    var prevKey = nextKey;
                    nextKey = 0;
                    SeekKey(uint.MaxValue);
                    nextKey = prevKey;
                    break;
                }
                case NeuroConstants.ChildWithType:
                {
                    var prevKey = nextKey;
                    nextKey = 0;
                    if (header == NeuroConstants.ChildWithType && !subClassTag.HasValue)
                    {
                        SeekKey(uint.MaxValue);
                    }
                    else
                    {
                        if (subClassTag == null)
                        {
                            proto.ReadUint();
                        }
                        SeekKey(uint.MaxValue);
                    }
                    nextKey = prevKey;
                    break;
                }
                case NeuroConstants.EndOfChild:
                    // empty list / dictionary
                    break;
                default:
                    throw new Exception($"Unexpected sizeType: {sizeType}");
            }
        }

        void SkipList()
        {
            var header = proto.ReadUint();
            ReadCollectionTypeAndSize(header, out var sizeType, out var count, out var containsNulls);
            header = (header & ~NeuroConstants.HeaderMask) | sizeType;
            var countLeft = count;
            while(countLeft > 0)
            {
                countLeft--;
                if (sizeType == NeuroConstants.ChildWithType)
                {
                    var itemTypeTagOrNull = proto.ReadUint();
                    if (itemTypeTagOrNull != 0)
                    {
                        Skip(header, itemTypeTagOrNull - 1);
                    }
                }
                else if (containsNulls)
                {
                    var itemHeader = proto.ReadUint();
                    if (itemHeader != 0)
                    {
                        Skip(itemHeader);
                    }
                }
                else
                {
                    Skip(sizeType);
                }
            }
        }

        void SkipDictionary()
        {
            var types = proto.ReadUint();
            var keyType = types & NeuroConstants.HeaderMask;
            var valueType = types >> NeuroConstants.HeaderShift;
            var count = proto.ReadUint();
            for (var i = 0; i < count; i++)
            {
                Skip(keyType);
                var header = proto.ReadUint();
                if (header == 0u)
                {
                    // null
                }
                else if (valueType == NeuroConstants.ChildWithType)
                {
                    Skip(valueType, header - 1);
                }
                else
                {
                    Skip(valueType);
                }
            }
        }
    }
}