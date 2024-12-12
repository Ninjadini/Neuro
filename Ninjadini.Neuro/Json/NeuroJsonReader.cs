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
        
        public const string NoGlobalTypeIdFoundErrMsg = "No global type id found in json.";
        
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
                throw GetErrorAboutGlobalTypes("json");
            }
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
        
        /// This is a bit slower as it needs to use reflection once.
        public object ReadObject(string json, Type type, ReaderOptions opts = default)
        {
            object result = null;
            ReadObject(json, type, ref result, opts);
            return result;
        }
        
        /// This is a bit slower as it needs to use reflection once.
        public void ReadObject(string json, Type type, ref object resultTarget, ReaderOptions opts = default)
        {
            options = opts;
            jsonStr = json;
            nodes = _jsonVisitor.Visit(json);
            currentParent = nodes.Array[0].Parent;
            NeuroSyncTypes.TryRegisterAssembly(type.Assembly);
            var subTypeNode = FindNode(NeuroJsonWriter.FieldName_ClassTag);
            var tag = GetFirstUintPart(subTypeNode.Value);
            var typeInfo = NeuroSyncTypes.GetTypeInfo(type);
            resultTarget = typeInfo.Sync(this, tag, resultTarget);
        }
        
        public object ReadGlobalTyped(string json, ReaderOptions opts = default)
        {
            object result = null;
            options = opts;
            jsonStr = json;
            nodes = _jsonVisitor.Visit(json);
            currentParent = nodes.Array[0].Parent;
            NeuroSyncTypes.TryRegisterAllAssemblies();
            var subTypeNode = FindNode(NeuroJsonWriter.FieldName_ClassTag);
            var globalTypeNode = FindNode(NeuroJsonWriter.FieldName_GlobalType);
            if (globalTypeNode.Type != NeuroJsonTokenizer.NodeType.Unknown)
            {
                var typeId = GetFirstUintPart(globalTypeNode.Value);
                var tag = GetFirstUintPart(subTypeNode.Value);
                NeuroGlobalTypes.Sync(typeId, this, tag, ref result);
            }
            else
            {
                throw new Exception(NoGlobalTypeIdFoundErrMsg);
            }
            return result;
        }

        T INeuroSync.GetPooled<T>()
        {
            return options.ObjectPool?.Borrow<T>();
        }

        bool INeuroSync.IsReading => true;

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
            else if(IsCurrentValueNull())
            {
                value = null;
            }
            else
            {
                value = jsonStr.Substring(currentValue.Start, currentValue.Length);
            }
        }

        bool IsCurrentValueNull()
        {
            return currentValue.Length == 4 && jsonStr[currentValue.Start] == 'n' &&
                   jsonStr[currentValue.Start + 1] == 'u' && jsonStr[currentValue.Start + 2] == 'l' &&
                   jsonStr[currentValue.Start + 2] == 'l';
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

        void INeuroSync.Sync<T>(uint key, string name, List<T> values)
        {
            var node = FindNode(name);
            if (node.Type == NeuroJsonTokenizer.NodeType.Array)
            {
                ReadList(node, ref values);
            }
            else
            {
                values?.Clear();
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            var node = FindNode(name);
            if (node.Type == NeuroJsonTokenizer.NodeType.Array)
            {
                ReadList(node, ref values);
            }
            else
            {
                values = default;
            }
        }

        void ReadList<T>(NeuroJsonTokenizer.VisitedNode node, ref List<T> values)
        {
            var parentBefore = currentParent;
            var nodeId = node.Value.Start;
            var count = node.Value.End;
            if (values == null)
            {
                values = new List<T>(count);
            }
            else if (values.Count > count)
            {
                values.RemoveRange(count, values.Count - count);
            }
            else if (values.Capacity < count)
            {
                values.Capacity = count;
            }

            var del = NeuroJsonSyncTypes<T>.GetOrThrow();

            var arr = nodes.Array;
            var targetIndex = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                // TODO this can be optimised via skipping some nodes + nextNode
                ref var childNode = ref arr[i];
                if (childNode.Parent == nodeId)
                {
                    currentParent = childNode.Value.Start;
                    currentValue = childNode.Value;
                    T value = i < values.Count ? values[i] : default;
                    if (IsCurrentValueNull())
                    {
                        value = default;
                    }
                    else if (NeuroSyncSubTypes<T>.Exists())
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

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, Dictionary<TKey, TValue> values)
        {
            var node = FindNode(name);
            if (node.Type == NeuroJsonTokenizer.NodeType.Group)
            {
                ReadDictionary(node, ref values);
            }
            else
            {
                values?.Clear();
            }
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            var node = FindNode(name);
            if (node.Type == NeuroJsonTokenizer.NodeType.Group)
            {
                ReadDictionary(node, ref values);
            }
            else
            {
                values = default;
            }
        }

        void ReadDictionary<TKey, TValue>(NeuroJsonTokenizer.VisitedNode node, ref Dictionary<TKey, TValue> values)
        {
            var count = node.Value.End;
            values ??= new Dictionary<TKey, TValue>(count);
            values.Clear();
            if (count == 0)
            {
                return;
            }
            var parentBefore = currentParent;
            var nodeId = node.Value.Start;
            
            var kDel = NeuroJsonSyncTypes<TKey>.GetOrThrow();
            var vDel = NeuroJsonSyncTypes<TValue>.GetOrThrow();
            var isPloyValues = NeuroSyncSubTypes<TValue>.Exists();
            
            var arr = nodes.Array;
            for (var i = 0; i < nodes.Count; i++)
            {
                // TODO this can be optimised via skipping some nodes + nextNode
                ref var childNode = ref arr[i];
                if (childNode.Parent == nodeId)
                {
                    currentParent = childNode.Value.Start;
                    currentValue = childNode.Key;
                    TKey itemKey = default;
                    kDel(this, ref itemKey);
                    
                    currentValue = childNode.Value;
                    TValue itemValue = default;
                    if (IsCurrentValueNull())
                    {
                        // NA
                    }
                    else if (isPloyValues)
                    {
                        var subTypeNode = FindNode(NeuroJsonWriter.FieldName_ClassTag);
                        if (subTypeNode.Type != NeuroJsonTokenizer.NodeType.Unknown)
                        {
                            var tag = GetFirstUintPart(subTypeNode.Value);
                            NeuroSyncSubTypes<TValue>.Sync(this, tag, ref itemValue);
                        }
                        else
                        {
                            vDel(this, ref itemValue);
                        }
                    }
                    else
                    {
                        vDel(this, ref itemValue);
                    }
                    values[itemKey] = itemValue;
                }
            }
            currentParent = parentBefore;
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


        internal static Exception GetErrorAboutGlobalTypes(string inputName)
        {
            var str = $"Read<object>({inputName}) call is ambiguous. Here are 3 alternative paths:";
            
            str += $"\n1. Try use the correct generic parameter for best efficiency, such as `Read<MyClassType>({inputName})`";
            
            str += $"\n2. If passing generic parameter is not possible, use `ReadObject({inputName}, <type>)` instead.";
            
            str += $"\n3. If you want to read global typed value, use `ReadGlobalTyped({inputName})`.";
            
            return new Exception(str);
        }
    }
}