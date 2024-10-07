using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    // TODO list with base classes
    // TODO list with null items.
    
    public class NeuroBytesWriter : INeuroSync
    {
        [ThreadStatic] private static NeuroBytesWriter _shared;
        public static NeuroBytesWriter Shared => _shared ??= new NeuroBytesWriter(5120);
        
        readonly RawProtoWriter proto;
        uint lastKey;

        public NeuroBytesWriter()
        {
            proto = new RawProtoWriter();
        }

        public NeuroBytesWriter(int initialCapacity)
        {
            proto = new RawProtoWriter();
            proto.Set(new byte[initialCapacity]);
        }
        
        public ReadOnlySpan<byte> Write<T>(T value)
        {
            if (typeof(T) == typeof(object))
            {
                return WriteGlobalType(value);
            }
            proto.Position = 0;
            lastKey = 0;
            if (value == null)
            {
                return new Span<byte>();
            }
            NeuroSyncTypes<T>.TryAutoRegisterTypeOrThrow();
            if (NeuroSyncTypes<T>.SizeType == NeuroConstants.ChildWithType)
            {
                proto.Write(1 << NeuroConstants.HeaderShift | NeuroConstants.ChildWithType);
                var tag = NeuroSyncSubTypes<T>.GetTag(value.GetType());
                proto.Write(tag);
                NeuroSyncSubTypes<T>.Sync(this, tag, ref value);
            }
            else
            {
                proto.Write(1 << NeuroConstants.HeaderShift | NeuroSyncTypes<T>.SizeType);
                NeuroSyncTypes<T>.GetOrThrow()(this, ref value);
            }
            proto.Write(NeuroConstants.Child);
            return new ReadOnlySpan<byte>(proto.Buffer, 0, proto.Position);
        }

        public ReadOnlySpan<byte> WriteGlobalType(object value)
        {
            proto.Position = 0;
            lastKey = 0;
            if (value == null)
            {
                return new ReadOnlySpan<byte>();
            }
            return WriteGlobalType(MemoryMarshal.CreateSpan(ref value, 1));
        }

        public ReadOnlySpan<byte> WriteGlobalType(Span<object> values)
        {
            proto.Position = 0;
            lastKey = 0;
            var length = values != null ? values.Length : 0;
            if (length == 0)
            {
                return new ReadOnlySpan<byte>();
            }
            for (var index = 0; index < length; index++)
            {
                var type = values[index].GetType();
                var count = 1;
                var globalId = NeuroGlobalTypes.GetTypeIdOrThrow(type, out _);
                for (var i = index + 1; i < length; i++)
                {
                    if (NeuroGlobalTypes.GetTypeIdOrThrow(values[i].GetType(), out _) != globalId)
                    {
                        break;
                    }
                    count++;
                }
                WriteHeader(lastKey + 1, NeuroConstants.VarInt);
                proto.Write(globalId);
                var repeatedMask = count > 1 ? NeuroConstants.RepeatedMask : 0u;
                WriteHeader(lastKey + 1, NeuroConstants.ChildWithType | repeatedMask);
                if (repeatedMask != 0)
                {
                    proto.Write((uint)count);
                }
                var key = lastKey;
                var endIndex = index + count;
                while(index < endIndex)
                {
                    lastKey = 0;
                    var subValue = values[index];
                    var subTag = NeuroGlobalTypes.GetSubTypeTagOrThrow(subValue.GetType());
                    proto.Write(subTag);
                    NeuroGlobalTypes.Sync(globalId, this, subTag, ref subValue);
                    proto.Write(NeuroConstants.Child);
                    index++;
                }
                lastKey = key;
            }

            return new ReadOnlySpan<byte>(proto.Buffer, 0, proto.Position);
        }

        public ReadOnlySpan<byte> WriteReferencesList(Span<IReferencable> values)
        {
            proto.Position = 0;
            lastKey = 0;
            var length = values != null ? values.Length : 0;
            if (length == 0)
            {
                return new ReadOnlySpan<byte>();
            }
            for (var index = 0; index < length;)
            {
                var type = values[index].GetType();
                var count = 1;
                var globalId = NeuroGlobalTypes.GetTypeIdOrThrow(type, out _);
                for (var i = index + 1; i < length; i++)
                {
                    if (NeuroGlobalTypes.GetTypeIdOrThrow(values[i].GetType(), out _) != globalId)
                    {
                        break;
                    }
                    count++;
                }
                proto.Write(globalId);
                proto.Write((uint)count);
                var endIndex = index + count;
                while(index < endIndex)
                {
                    lastKey = 0;
                    var subValue = values[index];
                    proto.Write(subValue.RefName);
                    var start = proto.Position;
                    var subTag = NeuroGlobalTypes.GetSubTypeTagOrThrow(subValue.GetType());
                    if (subTag == 0)
                    {
                        proto.Write(subValue.RefId << NeuroConstants.HeaderShift | NeuroConstants.Child);
                    }
                    else
                    {
                        proto.Write(subValue.RefId << NeuroConstants.HeaderShift | NeuroConstants.ChildWithType);
                        proto.Write(subTag);
                    }
                    var asObject = (object)subValue;
                    NeuroGlobalTypes.Sync(globalId, this, subTag, ref asObject);
                    proto.Write(NeuroConstants.Child);
                    
                    var size = proto.Position - start;
                    proto.InsertUint((uint)size, start);
                    index++;
                }
            }
            return new ReadOnlySpan<byte>(proto.Buffer, 0, proto.Position);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BytesChunk GetCurrentBytesChunk()
        {
            return proto.GetCurrentBytesChunk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetDebugString()
        {
            return proto.GetDebugString();
        }
        
        T INeuroSync.GetPooled<T>()
        {
            return null;
        }
        
        bool INeuroSync.IsWriting => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INeuroSync.Sync(ref bool value)
        {
            proto.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INeuroSync.Sync(ref int value)
        {
            proto.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INeuroSync.Sync(ref uint value)
        {
            proto.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INeuroSync.Sync(ref long value)
        {
            proto.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INeuroSync.Sync(ref ulong value)
        {
            proto.Write(value);
        }

        void INeuroSync.Sync(ref float value)
        {
            proto.Write(value);
        }

        void INeuroSync.Sync(ref double value)
        {
            proto.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INeuroSync.Sync(ref string value)
        {
            proto.Write(value);
        }

        void INeuroSync.Sync<T>(ref Reference<T> value)
        {
            proto.Write(value.RefId);
        }

        void INeuroSync.SyncEnum<T>(ref int value)
        {
            proto.Write(value);
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value, T defaultValue)
        {
            if (value != null && !value.Equals(defaultValue))
            {
                var sizeType = NeuroSyncTypes<T>.SizeType;
                WriteHeader(key, sizeType);
                lastKey = 0;
                NeuroSyncTypes<T>.GetOrThrow()(this, ref value);
                lastKey = key;
                if (sizeType >= NeuroConstants.Child)
                {
                    proto.Write(NeuroConstants.Child);
                }
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T? value)
        {
            if (value != null)
            {
                var sizeType = NeuroSyncTypes<T>.SizeType;
                WriteHeader(key, sizeType);
                lastKey = 0;
                var localValue = value.Value;
                NeuroSyncTypes<T>.GetOrThrow()(this, ref localValue);
                lastKey = key;
                if (sizeType >= NeuroConstants.Child)
                {
                    proto.Write(NeuroConstants.Child);
                }
            }
        }

        void INeuroSync.SyncEnum<T>(uint key, string name, ref T value, int defaultValue)
        {
            var intValue = NeuroSyncEnumTypes<T>.GetInt(value);
            if (intValue != defaultValue)
            {
                WriteHeader(key, 0);
                proto.Write(intValue);
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value)
        {
            if (value == null)
            {
                return;
            }
            var sizeType = NeuroSyncTypes<T>.SizeType;
            if (sizeType == NeuroConstants.Child && value.GetType() != typeof(T))
            {
                WriteHeader(key, NeuroConstants.ChildWithType);
                lastKey = 0;
                var subTag = NeuroSyncSubTypes<T>.GetTag(value.GetType());
                proto.Write(subTag);
                NeuroSyncSubTypes<T>.Sync(this, subTag, ref value);
            }
            else
            {
                WriteHeader(key, sizeType);
                lastKey = 0;
                NeuroSyncTypes<T>.GetOrThrow()(this, ref value);
            }
            lastKey = key;
            if (sizeType >= NeuroConstants.Child)
            {
                proto.Write(NeuroConstants.Child);
            }
        }

        void INeuroSync.SyncBaseClass<TRoot, TBase>(TBase value)
        {
            lastKey = 0;
            proto.Write(NeuroConstants.ChildWithType);
            var baseValue = (TRoot)value;
            NeuroSyncSubTypes<TRoot>.GetOrThrow(typeof(TBase))(this, ref baseValue);
            proto.Write(NeuroConstants.Child);
        }

        void INeuroSync.Sync<T>(uint key, string name, List<T> values)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }
            WriteList(key, name, ref values);
        }

        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            if (values == null)
            {
                return;
            }
            if (values.Count == 0)
            {
                WriteHeader(key, NeuroConstants.RepeatedMask);
                proto.Write(0u);
                return;
            }
            WriteList(key, name, ref values);
        }

        void WriteList<T>(uint key, string name, ref List<T> values)
        {
            var sizeType = NeuroSyncTypes<T>.SizeType;
            if (sizeType >= NeuroConstants.Child && NeuroSyncSubTypes<T>.Exists())
            {
                sizeType = NeuroConstants.ChildWithType;
            }
            WriteHeader(key, sizeType | NeuroConstants.RepeatedMask);
            var count = values.Count;
            proto.Write((uint)count);
            var del = NeuroSyncTypes<T>.GetOrThrow();
            for (var index = 0; index < count; index++)
            {
                lastKey = 0;
                var value = values[index];
                if (sizeType == NeuroConstants.ChildWithType)
                {
                    if (value != null)
                    {
                        var tag = NeuroSyncSubTypes<T>.GetTag(value.GetType());
                        proto.Write(tag);
                        NeuroSyncSubTypes<T>.Sync(this, tag, ref value);
                    }
                    else
                    {
                        proto.Write(0);
                        proto.Write(0);
                    }
                }
                else
                {
                    del(this, ref value);
                }
                if (sizeType >= NeuroConstants.Child)
                {
                    proto.Write(NeuroConstants.Child);
                }
            }
            lastKey = key;
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, Dictionary<TKey, TValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }
            WriteDictionary(key, name, ref values);
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            if (values == null)
            {
                return;
            }
            if (values.Count == 0)
            {
                WriteHeader(key, NeuroConstants.RepeatedMask);
                proto.Write(0u);
                return;
            }
            WriteDictionary(key, name, ref values);
        }

        void WriteDictionary<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            WriteHeader(key, NeuroConstants.Dictionary);
            var kSizeType = NeuroSyncTypes<TKey>.SizeType;
            var vSizeType = NeuroSyncTypes<TValue>.SizeType;
            if (vSizeType >= NeuroConstants.Child && NeuroSyncSubTypes<TValue>.Exists())
            {
                vSizeType = NeuroConstants.ChildWithType;
            }
            proto.Write(kSizeType | vSizeType << NeuroConstants.HeaderShift);
            proto.Write((uint)values.Count);
            var kDel = NeuroSyncTypes<TKey>.GetOrThrow();
            var vDel = NeuroSyncTypes<TValue>.GetOrThrow();
            foreach (var kv in values)
            {
                var k = kv.Key;
                kDel(this, ref k);
                //if (kSizeType >= NeuroConstants.Child)
                //{
                //    proto.Write(NeuroConstants.Child);
                //}
                lastKey = 0;
                var v = kv.Value;
                if (vSizeType == NeuroConstants.ChildWithType)
                {
                    if (v != null)
                    {
                        var tag = NeuroSyncSubTypes<TValue>.GetTag(v.GetType());
                        proto.Write(tag);
                        NeuroSyncSubTypes<TValue>.Sync(this, tag, ref v);
                    }
                    else
                    {
                        proto.Write(0);
                        proto.Write(0);
                    }
                }
                else
                {
                    vDel(this, ref v);
                }
                if (vSizeType >= NeuroConstants.Child)
                {
                    proto.Write(NeuroConstants.Child);
                }
            }
            lastKey = key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void WriteHeader(uint key, uint type)
        {
#if DEBUG
            if (key <= lastKey)
                throw new System.Exception($"Key index out of sync, expecting above {lastKey} but was {key}");
#endif
            var newKey = key - lastKey;
            lastKey = key;
            proto.Write(newKey << NeuroConstants.HeaderShift | type);
        }

        /// Clone the object by serializing to bytes and back out to a new object.
        /// If writer and read params are null, the static shared instances are used (thread static).
        public static T Clone<T>(T value, NeuroBytesWriter writer = null, NeuroBytesReader reader = null)
        {
            writer ??= Shared;
            reader ??= NeuroBytesReader.Shared;
            writer.Write(value);
            return reader.Read<T>(writer.GetCurrentBytesChunk(), reader.Options);
        }
    }
}