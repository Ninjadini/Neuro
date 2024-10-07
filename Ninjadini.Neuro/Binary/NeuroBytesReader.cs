using System;
using System.Collections.Generic;
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
            var sizeType = proto.ReadUint() & NeuroConstants.SizeTypeMask;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ReadGlobalType<T>(in BytesChunk bytesChunk, in ReaderOptions opts = default) where T : IReferencable
        {
            return (T)ReadGlobalType(typeof(T), bytesChunk, opts);
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
            
            var sizeType = proto.ReadUint() & NeuroConstants.SizeTypeMask;
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
        
        public object ReadGlobalTyped(in BytesChunk bytesChunk,  in ReaderOptions opts = default)
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
            
            var sizeType = proto.ReadUint() & NeuroConstants.SizeTypeMask;
            if ((sizeType & NeuroConstants.SizeTypeMask) != NeuroConstants.VarInt)
            {
                throw new Exception("Invalid header " + sizeType);
            }
            var typeId = proto.ReadUint();
            var tag = 0u;
            sizeType = proto.ReadUint() & NeuroConstants.SizeTypeMask;
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
                if ((nextHeader & NeuroConstants.SizeTypeMask) != NeuroConstants.VarInt)
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
                if ((nextHeader & NeuroConstants.SizeTypeMask) != NeuroConstants.ChildWithType)
                {
                    throw new Exception("Invalid header " + nextHeader);
                }
                var countLeft = 1u;
                if ((nextHeader & NeuroConstants.RepeatedMask) != 0)
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
                var sizeType = nextHeader & NeuroConstants.SizeTypeMask;
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
            SeekKey(uint.MaxValue); // skip to the end of the group (but before base classes start);
            nextKey = 0;
            var baseValue = (TRoot)value;
            NeuroSyncSubTypes<TRoot>.GetOrThrow(typeof(TBase))(this, ref baseValue);
            SeekKey(uint.MaxValue);// read to end of base class
            nextKey = 0;
        }

        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            if(key == 0 || SeekKey(key))
            {
                var sizeType = nextHeader & NeuroConstants.SizeTypeMask;
                var count = (int)proto.ReadUint();
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
                    T value = i < values.Count ? values[i] : default;
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
                            del(this, ref value);
                        }
                        SeekKey(uint.MaxValue);
                        nextKey = key;
                    }
                    else
                    {
                        del(this, ref value);
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
            }
            else if(values != null)
            {
                values.Clear();
                values = null;
            }
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            if(key == 0 || SeekKey(key))
            {
                var sizeType = nextHeader & NeuroConstants.SizeTypeMask;
                if (sizeType != NeuroConstants.Dictionary)
                {
                    if (proto.ReadUint() == 0)
                    {
                        values ??= new Dictionary<TKey, TValue>();
                        values.Clear();
                        return;
                    }
                    throw new Exception("Invalid dictionary content.");
                }
                var vSizeType = proto.ReadUint() >> NeuroConstants.HeaderShift;
                
                var count = (int)proto.ReadUint();
                values ??= new Dictionary<TKey, TValue>(count);
                values.Clear();
                
                var kDel = NeuroSyncTypes<TKey>.GetOrThrow();
                var vDel = NeuroSyncTypes<TValue>.GetOrThrow();
                //var kSizeType = NeuroSyncTypes<TKey>.SizeType;
                for (var i = 0; i < count; i++)
                {
                    TKey itemKey = default;
                    /*if (kSizeType >= NeuroConstants.Child)
                    {
                        nextKey = 0;
                        kDel(this, ref itemKey);
                        SeekKey(uint.MaxValue);
                        nextKey = key;
                    }
                    else
                    {*/
                        kDel(this, ref itemKey);
                    //}
                    TValue itemValue = default;
                    if (vSizeType >= NeuroConstants.Child)
                    {
                        nextKey = 0;
                        if (vSizeType == NeuroConstants.ChildWithType)
                        {
                            var tag = proto.ReadUint();
                            NeuroSyncSubTypes<TValue>.Sync(this, tag, ref itemValue);
                        }
                        else
                        {
                            vDel(this, ref itemValue);
                        }
                        SeekKey(uint.MaxValue);
                        nextKey = key;
                    }
                    else
                    {
                        vDel(this, ref itemValue);
                    }
                    values[itemKey] = itemValue;
                }
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
                var keyIncrement = (nextHeader >> NeuroConstants.HeaderShift);
                if(keyIncrement == 0 && (nextHeader & NeuroConstants.RepeatedMask) == 0)
                {
                    nextKey = uint.MaxValue;
                    return false; // reached end of group
                }
                nextKey += keyIncrement;
                if (nextKey == key)
                {
                    return true;
                }
                if (nextKey > key)
                {
                    return false;
                }

                var count = 1u;
                if ((nextHeader & NeuroConstants.RepeatedMask) != 0)
                {
                    count = proto.ReadUint();
                }
                var sizeType = nextHeader & NeuroConstants.SizeTypeMask;
                while (count > 0)
                {
                    count--;
                    Skip(sizeType, keyIncrement > 0);
                }
            }
            return false;
        }

        void Skip(uint sizeType, bool isFirstPoly = true)
        {
            if(sizeType == NeuroConstants.VarInt)
            {
                proto.ReadUint();
            }
            else if(sizeType == NeuroConstants.Fixed32)
            {
                proto.Skip(4);
            }
            else if(sizeType == NeuroConstants.Fixed64)
            {
                proto.Skip(8);
            }
            else if(sizeType == NeuroConstants.Length)
            {
                proto.Skip((int)proto.ReadUint());
            }
            else if(sizeType == NeuroConstants.Dictionary)
            {
                var types = proto.ReadUint();
                var keyType = types & NeuroConstants.HeaderMask;
                var valueType = types >> NeuroConstants.HeaderShift;
                var dictCount = proto.ReadUint();
                for (var i = 0; i < dictCount; i++)
                {
                    Skip(keyType);
                    Skip(valueType);
                }
            }
            else
            {
                var prevKey = nextKey;
                if (sizeType == NeuroConstants.ChildWithType)
                {
                    if (isFirstPoly)
                    {
                        proto.ReadUint(); // subtype's tag.
                    }
                    SkipGroup();
                }
                else
                {
                    nextKey = 0;
                    SeekKey(uint.MaxValue);
                }
                nextKey = prevKey;
            }
        }

        void SkipGroup()
        { 
            while(proto.Available > 0)
            {
                var header = proto.ReadUint();
                var keyIncrement = (header >> NeuroConstants.HeaderShift);
                if(header == NeuroConstants.Child)
                {
                    return; // reached end of group
                }
                var count = 1u;
                var isList = (header & NeuroConstants.RepeatedMask) != 0;
                if (isList)
                {
                    count = proto.ReadUint();
                }
                var sizeType = header & NeuroConstants.SizeTypeMask;
                var countLeft = count;
                while (countLeft > 0)
                {
                    countLeft--;
                    if(sizeType == NeuroConstants.VarInt)
                    {
                        proto.ReadUint();
                    }
                    else if(sizeType == NeuroConstants.Length)
                    {
                        proto.Skip((int)proto.ReadUint());
                    }
                    else
                    {
                        if (sizeType == NeuroConstants.ChildWithType && keyIncrement > 0)
                        {
                            proto.ReadUint();
                        }
                        SkipGroup();
                    }
                }
            }
        }
    }
}