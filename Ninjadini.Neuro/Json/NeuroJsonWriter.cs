using System;
using System.Collections.Generic;
using System.Text;
using Ninjadini.Neuro.Utils;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    public class NeuroJsonWriter : INeuroSync
    {
        public const string FieldName_GlobalType = "-globalType";
        public const string FieldName_ClassTag = "-subType";
        
        [ThreadStatic] private static NeuroJsonWriter _shared;
        public static NeuroJsonWriter Shared => _shared ??= new NeuroJsonWriter();
        
        private StringBuilder defaultStringBuilder;
        private NeuroReferences defaultReferences;
        private NeuroReferences references;
        private StringBuilder stringBuilder;
        private Options opts;

        public const string SingleIndent = "    ";

        private int numIndents;
        
        [Flags]
        public enum Options
        {
            TagValuesOnly = 1 << 0, // don't write the ref id name or type name;
            ExcludeTopLevelGlobalType = 1 << 2,
        }

        public NeuroJsonWriter(NeuroReferences refs = null)
        {
            defaultReferences = refs;
        }
        
        static NeuroJsonWriter()
        {
            NeuroDefaultJsonSyncTypes.Register();
        }
        
        public string Write<T>(T value, NeuroReferences refs = null, Options options = 0)
        {
            if (defaultStringBuilder == null)
            {
                defaultStringBuilder = new StringBuilder();
            }
            else
            {
                defaultStringBuilder.Length = 0;
            }
            WriteTo(defaultStringBuilder, ref value, refs, options);
            var result = defaultStringBuilder.ToString();
            defaultStringBuilder.Clear();
            return result;
        }

        public string WriteGlobalTyped(object value, NeuroReferences refs = null, Options options = 0)
        {
            return Write<object>(value, refs, options);
        }
        
        public void WriteTo<T>(StringBuilder strBuilder, ref T value, NeuroReferences refs = null, Options options = 0)
        {
            if (strBuilder == null)
            {
                return;
            }
            if (value == null)
            {
                stringBuilder.Append("null");
                return;
            }
            var excludeTopLevelGlobalType = (options & Options.ExcludeTopLevelGlobalType) != 0;
            opts = options & ~Options.ExcludeTopLevelGlobalType;
            references = refs ?? defaultReferences;
            stringBuilder = strBuilder;
            stringBuilder.Append("{\n");
            numIndents = 1;
            NeuroSyncTypes<T>.TryAutoRegisterTypeOrThrow();
            
            var type = value.GetType();
            var isGlobalType = typeof(T) == typeof(object);
            uint globalId = 0;
            if (!excludeTopLevelGlobalType && isGlobalType)
            {
                globalId = NeuroGlobalTypes.GetTypeIdOrThrow(type, out var rootType);
                AppendSubTagAndOrName(FieldName_GlobalType, globalId, rootType.Name);
            }
            var posAtStart = stringBuilder.Length;
            if (isGlobalType)
            {
                if (globalId == 0)
                {
                    globalId = NeuroGlobalTypes.GetTypeIdOrThrow(type, out _);
                }
                var typedValue = (object)value;
                var subTag = NeuroGlobalTypes.GetSubTypeTagOrThrow(type);
                if (subTag > 0)
                {
                    AppendSubTagAndOrName(FieldName_ClassTag, subTag, type.Name);
                }
                NeuroGlobalTypes.Sync(globalId, this, subTag, ref typedValue);
            }
            else if (NeuroJsonSyncTypes<T>.SizeType == NeuroConstants.ChildWithType)
            {
                var subTag = NeuroSyncSubTypes<T>.GetTag(type);
                AppendSubTagAndOrName(FieldName_ClassTag, subTag, type.Name);
                NeuroSyncSubTypes<T>.Sync(this, subTag, ref value);
            }
            else
            {
                NeuroJsonSyncTypes<T>.GetOrThrow()(this, ref value);
            }
            if (stringBuilder.Length > posAtStart)
            {
                stringBuilder.Length -= 2;
                stringBuilder.Append("\n");
            }
            stringBuilder.Append("}");
            references = null;
            stringBuilder = null;
        }
        
        /// This is a bit slower as it needs to use reflection once.
        public string Write(object value, NeuroReferences refs = null, Options options = 0)
        {
            if (defaultStringBuilder == null)
            {
                defaultStringBuilder = new StringBuilder();
            }
            else
            {
                defaultStringBuilder.Length = 0;
            }
            WriteTo(defaultStringBuilder, value, refs, options);
            var result = defaultStringBuilder.ToString();
            defaultStringBuilder.Clear();
            return result;
        }
        
        /// This is a bit slower as it needs to use reflection once.
        public void WriteTo(StringBuilder strBuilder, object value, NeuroReferences refs = null, Options options = 0)
        {
            if (strBuilder == null)
            {
                return;
            }
            if (value == null)
            {
                stringBuilder.Append("null");
                return;
            }
            opts = options & ~Options.ExcludeTopLevelGlobalType;
            references = refs ?? defaultReferences;
            stringBuilder = strBuilder;
            stringBuilder.Append("{\n");
            numIndents = 1;
            var type = value.GetType();
            NeuroSyncTypes.TryRegisterAssembly(type.Assembly);
            
            var posAtStart = stringBuilder.Length;
            var typeInfo = NeuroSyncTypes.GetTypeInfo(type);
            if (typeInfo.SizeType == NeuroConstants.ChildWithType && typeInfo.SubTypeTag != 0)
            {
                AppendSubTagAndOrName(FieldName_ClassTag, typeInfo.SubTypeTag, type.Name);
            }
            typeInfo.Sync(this, value);
            if (stringBuilder.Length > posAtStart)
            {
                stringBuilder.Length -= 2;
                stringBuilder.Append("\n");
            }
            stringBuilder.Append("}");
            references = null;
            stringBuilder = null;
        }

        void AppendSubTagAndOrName(string fieldName, uint subTag, string name)
        {
            AppendIndents().Append("\"").Append(fieldName);
            if ((opts & Options.TagValuesOnly) == 0)
            {
                stringBuilder.Append("\": \"").AppendNum(subTag).Append(":").Append(name).Append("\",\n");
            }
            else
            {
                stringBuilder.Append("\": ").AppendNum(subTag).Append(",\n");
            }
        }
        
        public StringBuilder CurrentStringBuilder => stringBuilder;

        bool INeuroSync.IsWriting => true;

        void INeuroSync.Sync(ref bool value)
        {
            stringBuilder.Append(value ? "true" : "false");
        }
        
        void INeuroSync.Sync(ref int value)
        {
            stringBuilder.AppendNum(value, false);
        }
        
        void INeuroSync.Sync(ref uint value)
        {
            stringBuilder.AppendNum(value, false);
        }

        void INeuroSync.Sync(ref long value)
        {
            stringBuilder.AppendNum(value, false);
        }

        void INeuroSync.Sync(ref ulong value)
        {
            stringBuilder.AppendNum(value, false);
        }

        void INeuroSync.Sync(ref float value)
        {
            stringBuilder.AppendNum(value);
        }

        void INeuroSync.Sync(ref double value)
        {
            stringBuilder.AppendNum(value);
        }

        T INeuroSync.GetPooled<T>()
        {
            return null;
        }

        void INeuroSync.Sync(ref string value)
        {
            if (value != null)
            {
                stringBuilder.Append("\"");
                var strSpan = value.AsSpan();
                var lineBreakIndex = strSpan.IndexOf("\n", StringComparison.Ordinal);
                var quoteIndex = strSpan.IndexOf("\"", StringComparison.Ordinal);
                var slashIndex = strSpan.IndexOf("\\", StringComparison.Ordinal);
                // TODO, this is just way too complicated for what it is
                while (quoteIndex >= 0 || slashIndex >= 0 || lineBreakIndex >= 0)
                {
                    if (lineBreakIndex >= 0 
                        && (quoteIndex == -1 || lineBreakIndex < quoteIndex)
                        && (slashIndex == -1 || lineBreakIndex < slashIndex))
                    {
                        stringBuilder.Append(strSpan.Slice(0, lineBreakIndex));
                        strSpan = strSpan.Slice(lineBreakIndex + 1);
                        stringBuilder.Append("\\n");
                    }
                    else if ((quoteIndex < slashIndex && quoteIndex != -1) || slashIndex == -1)
                    {
                        stringBuilder.Append(strSpan.Slice(0, quoteIndex));
                        strSpan = strSpan.Slice(quoteIndex + 1);
                        stringBuilder.Append("\\\"");
                    }
                    else
                    {
                        stringBuilder.Append(strSpan.Slice(0, slashIndex));
                        strSpan = strSpan.Slice(slashIndex + 1);
                        stringBuilder.Append(@"\\");
                    }
                    lineBreakIndex = strSpan.IndexOf("\n", StringComparison.Ordinal);
                    slashIndex = strSpan.IndexOf("\\", StringComparison.Ordinal);
                    quoteIndex = strSpan.IndexOf("\"", StringComparison.Ordinal);
                }
                stringBuilder.Append(strSpan);
                stringBuilder.Append("\"");
            }
            else
            {
                stringBuilder.Append("null");
            }
        }

        void INeuroSync.Sync<T>(ref Reference<T> value)
        {
            if ((opts & Options.TagValuesOnly) == 0)
            {
                var refName = value.GetValue(references)?.RefName;
                if (!string.IsNullOrEmpty(refName))
                {
                    stringBuilder.Append("\"");
                    stringBuilder.Append(value.RefId);
                    stringBuilder.Append(":");
                    stringBuilder.Append(refName);
                    stringBuilder.Append("\"");
                    return;
                }
            }
            stringBuilder.AppendNum(value.RefId);
        }

        void INeuroSync.SyncEnum<T>(ref int value)
        {
            if ((opts & Options.TagValuesOnly) == 0)
            {
                var name = NeuroSyncEnumTypes<T>.GetName(value);
                if (string.IsNullOrEmpty(name))
                {
                    stringBuilder.AppendNum(value);
                }
                else
                {
                    stringBuilder.Append("\"");
                    stringBuilder.AppendNum(value);
                    stringBuilder.Append(":");
                    stringBuilder.Append(name);
                    stringBuilder.Append("\"");
                }
            }
            else
            {
                stringBuilder.Append(value);
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T value, T defaultValue)
        {
            if (value != null && !value.Equals(defaultValue))
            {
                SyncObj(name, ref value);
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref T? value)
        {
            if (value.HasValue)
            {
                var v = value.Value;
                SyncObj(name, ref v);
            }
        }

        StringBuilder AppendIndents()
        {
            for (var i = 0; i < numIndents; i++)
            {
                stringBuilder.Append(SingleIndent);
            }
            return stringBuilder;
        }

        void INeuroSync.SyncEnum<T>(uint key, string name, ref T value, int defaultValue)
        {
            var intValue = NeuroSyncEnumTypes<T>.GetInt(value);
            if (intValue != defaultValue)
            {
                SyncObj(name, ref value);
            }
        }


        void INeuroSync.Sync<T>(uint key, string name, ref T value)
        {
            SyncObj(name, ref value);
        }

        private void SyncObj<T>(string name, ref T value)
        {
            if (value == null)
            {
                return;
            }
            var sizeType = NeuroJsonSyncTypes<T>.SizeType;

            if (!string.IsNullOrEmpty(name))
            {
                AppendIndents().Append("\"").Append(name).Append("\": ");
            }
            var isGroup = sizeType >= NeuroConstants.Child;
            if (isGroup)
            {
                numIndents++;
                stringBuilder.Append("{\n");
            }
            var posAtStart = stringBuilder.Length;
            if (isGroup && value.GetType() != typeof(T))
            {
                var subTag = NeuroSyncSubTypes<T>.GetTag(value.GetType());
                
                AppendIndents().Append("\"").Append(FieldName_ClassTag);
                if ((opts & Options.TagValuesOnly) == 0)
                {
                    stringBuilder.Append("\": \"").AppendNum(subTag).Append(":").Append(value.GetType().Name).Append("\",\n");
                }
                else
                {
                    stringBuilder.Append("\": ").AppendNum(subTag).Append(",\n");
                }
                
                NeuroSyncSubTypes<T>.Sync(this, subTag, ref value);
            }
            else
            {
                NeuroJsonSyncTypes<T>.GetOrThrow()(this, ref value);
            }
            if (isGroup)
            {
                if (stringBuilder.Length > posAtStart)
                {
                    stringBuilder.Length -= 2;
                    stringBuilder.Append("\n");
                }
                numIndents--;
                AppendIndents().Append("}");
            }
            stringBuilder.Append(",\n");
        }

        void INeuroSync.SyncBaseClass<TRoot, TBase>(TBase value)
        {
            var baseValue = (TRoot)value;
            NeuroSyncSubTypes<TRoot>.GetOrThrow(typeof(TBase))(this, ref baseValue);
        }

        void INeuroSync.Sync<T>(uint key, string name, List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                WriteList(key, name, ref values);
            }
        }

        void INeuroSync.Sync<T>(uint key, string name, ref List<T> values)
        {
            if (values != null)
            {
                WriteList(key, name, ref values);
            }
        }

        void WriteList<T>(uint key, string name, ref List<T> values)
        {
            AppendIndents().Append("\"").Append(name).Append("\": [\n");
            numIndents++;
            foreach (var value in values)
            {
                AppendIndents();
                if (value != null)
                {
                    var v = value;
                    SyncObj(null, ref v);
                }
                else
                {
                    stringBuilder.Append("null,\n");
                }
            }
            numIndents--;
            if (values.Count == 0)
            {
                stringBuilder.Length -= 1;
                stringBuilder.Append("],\n");
            }
            else
            {
                stringBuilder.Length -= 2;
                stringBuilder.Append("\n");
                AppendIndents().Append("],\n");
            }
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, Dictionary<TKey, TValue> values)
        {
            if (values != null && values.Count > 0)
            {
                WriteDictionary(key, name, ref values);
            }
        }

        void INeuroSync.Sync<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            if (values != null)
            {
                WriteDictionary(key, name, ref values);
            }
        }

        void WriteDictionary<TKey, TValue>(uint key, string name, ref Dictionary<TKey, TValue> values)
        {
            if (values == null)
            {
                return;
            }
            AppendIndents().Append("\"").Append(name).Append("\": {\n");
            numIndents++;
            var keySizeType = NeuroSyncTypes<TKey>.SizeType;
            var kDel = NeuroJsonSyncTypes<TKey>.GetOrThrow();
            foreach (var value in values)
            {
                AppendIndents();
                var startInd = -1;
                if (keySizeType != NeuroConstants.Length)
                {
                    startInd = stringBuilder.Length;
                    stringBuilder.Append("\"");
                }
                var k = value.Key;
                kDel(this, ref k);
                if (stringBuilder[^1] != '\"')
                {
                    stringBuilder.Append("\"");
                }
                else if (startInd >= 0)
                {
                    // oops, the content already contains ", 
                    stringBuilder.Remove(startInd, 1);
                }
                stringBuilder.Append(": ");
                var v = value.Value;
                if (v != null)
                {
                    SyncObj(null, ref v);
                    stringBuilder.Length -= 2;
                }
                else
                {
                    stringBuilder.Append("null");
                }
                stringBuilder.AppendLine(",");
            }
            numIndents--;
            if (values.Count > 0)
            {
                stringBuilder.Remove(stringBuilder.Length -2, 1);
                AppendIndents().AppendLine("},");
            }
            else
            {
                stringBuilder.Length--;
                stringBuilder.AppendLine("},");
            }
        }
    }
}