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
            if (value == null)
            {
                return new Span<byte>();
            }
            if (typeof(T) == typeof(object))
            {
                throw NeuroJsonWriter.GetErrorAboutGlobalTypes(value.GetType());
            }
            proto.Position = 0;
            lastKey = 0;
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
            proto.Write(NeuroConstants.EndOfChild);
            return new ReadOnlySpan<byte>(proto.Buffer, 0, proto.Position);
        }

        /// This is a bit slower as it needs to use reflection once.
        public ReadOnlySpan<byte> WriteObject(object value)
        {
            if (value == null)
            {
                return new Span<byte>();
            }
            proto.Position = 0;
            lastKey = 0;
            var type = value.GetType();
            NeuroSyncTypes.TryRegisterAssembly(type.Assembly);
            var typeInfo = NeuroSyncTypes.GetTypeInfo(type);
            
            proto.Write(1 << NeuroConstants.HeaderShift | typeInfo.SizeType);
            if (typeInfo.SizeType == NeuroConstants.ChildWithType)
            {
                proto.Write(typeInfo.SubTypeTag);
            }
            typeInfo.Sync(this, typeInfo.SubTypeTag, value);
            proto.Write(NeuroConstants.EndOfChild);
            return new ReadOnlySpan<byte>(proto.Buffer, 0, proto.Position);
        }

        public ReadOnlySpan<byte> WriteGlobalTyped(object value)
        {
            proto.Position = 0;
            lastKey = 0;
            if (value == null)
            {
                return new ReadOnlySpan<byte>();
            }
            var type = value.GetType();
            var globalId = NeuroGlobalTypes.GetTypeIdOrThrow(type, out _);
            WriteHeader(lastKey + 1, NeuroConstants.VarInt);
            proto.Write(globalId);
            WriteHeader(lastKey + 1, NeuroConstants.ChildWithType);
            var key = lastKey;
            lastKey = 0;
            var subTag = NeuroGlobalTypes.GetSubTypeTagOrThrow(type);
            proto.Write(subTag);
            NeuroGlobalTypes.Sync(globalId, this, subTag, ref value);
            proto.Write(NeuroConstants.EndOfChild);
            lastKey = key;
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
                    proto.Write(NeuroConstants.EndOfChild);
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
                    proto.Write(NeuroConstants.EndOfChild);
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
                proto.Write(NeuroConstants.EndOfChild);
            }
        }

        void INeuroSync.SyncBaseClass<TRoot, TBase>(TBase value)
        {
            lastKey = 0;
            proto.Write(NeuroConstants.ChildWithType);
            var baseValue = (TRoot)value;
            NeuroSyncSubTypes<TRoot>.GetOrThrow(typeof(TBase))(this, ref baseValue);
            proto.Write(NeuroConstants.EndOfChild);
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
                WriteHeader(key, NeuroConstants.EndOfChild);
                return;
            }
            WriteList(key, name, ref values);
        }

        void WriteList<T>(uint key, string name, ref List<T> values)
        {
            WriteHeader(key, NeuroConstants.List);
            var count = values.Count;
            var sizeType = NeuroSyncTypes<T>.SizeType;
            if (sizeType >= NeuroConstants.Child && NeuroSyncSubTypes<T>.Exists())
            {
                sizeType = NeuroConstants.ChildWithType;
            }
            var containsNull = false;
            if (sizeType == NeuroConstants.Length || sizeType == NeuroConstants.Child)
            {
                for (var index = 0; index < count; index++)
                {
                    if (values[index] == null)
                    {
                        containsNull = true;
                    }
                }
            }
            WriteCollectionTypeAndSize(sizeType, count, containsNull);
            var del = NeuroSyncTypes<T>.GetOrThrow();
            if (sizeType < NeuroConstants.Length)
            {
                // Primitive types are simple
                for (var index = 0; index < count; index++)
                {
                    var value = values[index];
                    del(this, ref value);
                }
                return;
            }
            for (var index = 0; index < count; index++)
            {
                var value = values[index];
                if (value != null)
                {
                    lastKey = 0;
                    if (sizeType == NeuroConstants.ChildWithType)
                    {
                        var tag = NeuroSyncSubTypes<T>.GetTag(value.GetType());
                        proto.Write(tag + 1);
                        NeuroSyncSubTypes<T>.Sync(this, tag, ref value);
                    }
                    else
                    {
                        if (containsNull)
                        {
                            proto.Write(sizeType);
                        }
                        del(this, ref value);
                    }
                    if (sizeType >= NeuroConstants.Child)
                    {
                        proto.Write(NeuroConstants.EndOfChild);
                    }
                }
                else
                {
                    proto.Write(0u);
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
                WriteHeader(key, NeuroConstants.EndOfChild);
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
                var itemKey = kv.Key;
                kDel(this, ref itemKey);
                lastKey = 0;
                var itemValue = kv.Value;
                if (itemValue != null)
                {
                    lastKey = 0;
                    if (vSizeType == NeuroConstants.ChildWithType)
                    {
                        var tag = NeuroSyncSubTypes<TValue>.GetTag(itemValue.GetType());
                        proto.Write(tag + 1);
                        NeuroSyncSubTypes<TValue>.Sync(this, tag, ref itemValue);
                    }
                    else
                    {
                        proto.Write(1u);
                        vDel(this, ref itemValue);
                    }
                    if (vSizeType >= NeuroConstants.Child)
                    {
                        proto.Write(NeuroConstants.EndOfChild);
                    }
                }
                else
                {
                    proto.Write(0u);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void WriteCollectionTypeAndSize(uint type, int size, bool containsNulls)
        {
            proto.Write((uint)size << NeuroConstants.HeaderShift | type & NeuroConstants.CollectionTypeHeaderMask | (containsNulls ? NeuroConstants.CollectionHasNullMask : 0u));
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