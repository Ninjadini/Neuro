using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    public class NeuroJsonReader : INeuroSync
    {
        [ThreadStatic] private static NeuroJsonReader _shared;
        public static NeuroJsonReader Shared => _shared ??= new NeuroJsonReader();
        
        private NeuroJsonTokenizer _jsonVisitor = new NeuroJsonTokenizer();

        private ReaderOptions options;
        private string jsonStr;
        NeuroJsonTokenizer.VisitedNodes nodes;
        private int currentParent;
        private NeuroJsonTokenizer.StringRange currentValue;
        private StringBuilder stringBuilder;
        
        static NeuroJsonReader()
        {
            NeuroDefaultJsonSyncTypes.Register();
        }
        
        public T Read<T>(string json, ReaderOptions opts = default)
        {
            T value = default;
            Read(json, ref value, opts);
            return value;
        }
        
        public object Read(string json, Type type, ReaderOptions opts = default)
        {
            options = opts;
            jsonStr = json;
            nodes = _jsonVisitor.Visit(json);
            currentParent = nodes.Array[0].Parent;
            NeuroSyncTypes.TryRegisterAssembly(type.Assembly);
            var typeId = NeuroGlobalTypes.GetIdByType(type);
            object globalResult = null;
            var subTypeNode = FindNode(NeuroJsonWriter.FieldName_ClassTag);
            var tag = GetFirstUintPart(subTypeNode.Value);
            NeuroGlobalTypes.Sync(typeId, this, tag, ref globalResult);
            return globalResult;
        }

        public void Read<T>(string json, ref T result, ReaderOptions opts = default)
        {
            options = opts;
            jsonStr = json;
            nodes = _jsonVisitor.Visit(json);
            currentParent = nodes.Array[0].Parent;
            NeuroSyncTypes<T>.TryAutoRegisterTypeOrThrow();
            var subTypeNode = FindNode(NeuroJsonWriter.FieldName_ClassTag);
            if (typeof(T) == typeof(object))
            {
                var globalTypeNode = FindNode(NeuroJsonWriter.FieldName_GlobalType);
                if (globalTypeNode.Type != NeuroJsonTokenizer.NodeType.Unknown)
                {
                    var typeId = GetFirstUintPart(globalTypeNode.Value);
                    object globalResult = result;
                    var tag = GetFirstUintPart(subTypeNode.Value);
                    NeuroGlobalTypes.Sync(typeId, this, tag, ref globalResult);
                    result = (T)globalResult;
                }
                else
                {
                    throw new System.Exception("No type id found in json.");
                }
            }
            else
            {
                if (subTypeNode.Type != NeuroJsonTokenizer.NodeType.Unknown)
                {
                    var tag = GetFirstUintPart(subTypeNode.Value);
                    NeuroSyncSubTypes<T>.Sync(this, tag, ref result);
                }
                else
                {
                    NeuroJsonSyncTypes<T>.GetOrThrow()(this, ref result);
                }
            }
        }

        T INeuroSync.GetPooled<T>()
        {
            return options.ObjectPool?.Borrow<T>();
        }

        bool INeuroSync.IsReading => true;
        bool INeuroSync.IsWriting => false;

        void INeuroSync.Sync(ref bool value)
        {
            value = bool.Parse(currentValue.AsSpan(jsonStr));
        }

        void INeuroSync.Sync(ref int value)
        {
            value = int.Parse(currentValue.AsSpan(jsonStr));
        }

        void INeuroSync.Sync(ref uint value)
        {
            value = uint.Parse(currentValue.AsSpan(jsonStr));
        }

        void INeuroSync.Sync(ref long value)
        {
            value = long.Parse(currentValue.AsSpan(jsonStr));
        }

        void INeuroSync.Sync(ref ulong value)
        {
            value = ulong.Parse(currentValue.AsSpan(jsonStr));
        }

        void INeuroSync.Sync(ref float value)
        {
            value = float.Parse( currentValue.AsSpan(jsonStr));
        }

        void INeuroSync.Sync(ref double value)
        {
            value = double.Parse( currentValue.AsSpan(jsonStr));
        }

        void INeuroSync.Sync(ref string value)
        {
            var strSpan = jsonStr.AsSpan(currentValue.Start, currentValue.Length);
            var slashIndex = strSpan.IndexOf("\\", StringComparison.Ordinal);
            if (slashIndex >= 0)
            {
                if (stringBuilder == null)
                {
                    stringBuilder = new StringBuilder();
                }
                else
                {
                    stringBuilder.Length = 0;
                }
                while (slashIndex >= 0)
                {
                    stringBuilder.Append(strSpan.Slice(0, slashIndex));
                    if (strSpan.Length > slashIndex + 1 && strSpan[slashIndex + 1] == 'n')
                    {
                        stringBuilder.Append("\n");
                    }
                    else
                    {
                        stringBuilder.Append(strSpan[slashIndex + 1]);
                    }
                    strSpan = strSpan.Slice(slashIndex + 2);
                    slashIndex = strSpan.IndexOf("\\", StringComparison.Ordinal);
                }
                stringBuilder.Append(strSpan);
                value = stringBuilder.ToString();
                stringBuilder.Length = 0;
            }
            else
            {
                value = jsonStr.Substring(currentValue.Start, currentValue.Length);
            }
        }
        
        public ReadOnlySpan<char> CurrentValue => jsonStr != null ? currentValue.AsSpan(jsonStr) : default;

        void INeuroSync.Sync<T>(ref Reference<T> value)
        {
            value.RefId = GetFirstUintPart(currentValue);
        }

        void INeuroSync.SyncEnum<T>(ref int value)
        {
            var endIndex = jsonStr.IndexOf(':', currentValue.Start, currentValue.Length);
            value = int.Parse(jsonStr.AsSpan(currentValue.Start, (endIndex > 0 ? endIndex : currentValue.End) - currentValue.Start));
        }

        NeuroJsonTokenizer.VisitedNode FindNode(string key)
        {
            var arr = nodes.Array;
            for(var i = 0; i < nodes.Count; i++)
            {
                // TODO this can be optimised via skipping some nodes + nextNode
                ref var node = ref arr[i];
                if (node.Parent == currentParent && NeuroJsonTokenizer.StringRange.Equals(node.Key, jsonStr, key))
                {
                    return node;
                }
            }
            return new NeuroJsonTokenizer.VisitedNode();
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value, T defaultValue)
        {
            var node = FindNode(name);
            if (node.Type != NeuroJsonTokenizer.NodeType.Unknown)
            {
                var parentBefore = currentParent;
                if (node.Type == NeuroJsonTokenizer.NodeType.Group)
                {
                    currentParent = node.Value.Start;
                }
                else
                {
                    currentValue = node.Value;
                }
                NeuroJsonSyncTypes<T>.GetOrThrow()(this, ref value);
                currentParent = parentBefore;
            }
            else
            {
                value = defaultValue;
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value)
        {
            var node = FindNode(name);
            if (node.Type != NeuroJsonTokenizer.NodeType.Unknown)
            {
                var parentBefore = currentParent;
                if (node.Type == NeuroJsonTokenizer.NodeType.Group)
                {
                    currentParent = node.Value.Start;
                }
                else
                {
                    currentValue = node.Value;
                }
                if (NeuroSyncSubTypes<T>.Exists())
                {
                    var subTypeNode = FindNode(NeuroJsonWriter.FieldName_ClassTag);
                    if (subTypeNode.Type != NeuroJsonTokenizer.NodeType.Unknown)
                    {
                        var tag = GetFirstUintPart(subTypeNode.Value);
                        NeuroSyncSubTypes<T>.Sync(this, tag, ref value);
                        currentParent = parentBefore;
                        return;
                    }
                }
                
                NeuroJsonSyncTypes<T>.GetOrThrow()(this, ref value);
                currentParent = parentBefore;
            }
            else
            {
                value = default;
            }
        }

        void INeuroSync.SyncBaseClass<TRoot, TBase>(TBase value)
        {
            var baseValue = (TRoot)value;
            NeuroSyncSubTypes<TRoot>.GetOrThrow(typeof(TBase))(this, ref baseValue);
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T? value)
        {
            var node = FindNode(name);
            if (node.Type != NeuroJsonTokenizer.NodeType.Unknown)
            {
                var parentBefore = currentParent;
                if (node.Type == NeuroJsonTokenizer.NodeType.Group)
                {
                    currentParent = node.Value.Start;
                }
                else
                {
                    currentValue = node.Value;
                }
                T localValue = default;
                NeuroJsonSyncTypes<T>.GetOrThrow()(this, ref localValue);
                value = localValue;
                currentParent = parentBefore;
            }
            else
            {
                value = null;
            }
        }

        void INeuroSync.SyncEnum<T>(uint key, string name, ref T value, int defaultValue)
        {
            var node = FindNode(name);
            if (node.Type != NeuroJsonTokenizer.NodeType.Unknown)
            {
                var parentBefore = currentParent;
                currentValue = node.Value;
                NeuroJsonSyncTypes<T>.GetOrThrow()(this, ref value);
                currentParent = parentBefore;
            }
            else
            {
                value = NeuroSyncEnumTypes<T>.GetEnum(defaultValue);
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            var node = FindNode(name);
            if (node.Type == NeuroJsonTokenizer.NodeType.Array)
            {
                var parentBefore = currentParent;
                var nodeId = node.Value.Start;
                var count = node.Value.End;
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
                var del = NeuroJsonSyncTypes<T>.GetOrThrow();

                var arr = nodes.Array;
                var targetIndex = 0;
                for(var i = 0; i < nodes.Count; i++)
                {
                    // TODO this can be optimised via skipping some nodes + nextNode
                    ref var childNode = ref arr[i];
                    if (childNode.Parent == nodeId)
                    {
                        currentParent = childNode.Value.Start;
                        currentValue = childNode.Value;
                        T value = i < values.Count ? values[i] : default;
                        
                        if (NeuroSyncSubTypes<T>.Exists())
                        {
                            var subTypeNode = FindNode(NeuroJsonWriter.FieldName_ClassTag);
                            if (subTypeNode.Type != NeuroJsonTokenizer.NodeType.Unknown)
                            {
                                var tag = GetFirstUintPart(subTypeNode.Value);
                                NeuroSyncSubTypes<T>.Sync(this, tag, ref value);
                            }
                            else
                            {
                                del(this, ref value);
                            }
                        }
                        else
                        {
                            del(this, ref value);
                        }
                        if (targetIndex < values.Count)
                        {
                            values[targetIndex] = value;
                        }
                        else
                        {
                            values.Add(value);
                        }
                        targetIndex++;
                    }
                }
                currentParent = parentBefore;
            }
            else
            {
                values = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint GetFirstUintPart(in NeuroJsonTokenizer.StringRange stringRange)
        {
            var len = stringRange.Length;
            if (len == 0)
            {
                return 0;
            }
            var endIndex = jsonStr.IndexOf(':', stringRange.Start, len);
            return uint.Parse(jsonStr.AsSpan(stringRange.Start, (endIndex > 0 ? endIndex : stringRange.End) - stringRange.Start));
        }
    }
}