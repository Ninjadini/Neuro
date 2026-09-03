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
        
        /// A per thread writer you can reuse instead of allocating one.
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
        
        /// Writes a neuro object to a JSON string. This is the one to use in almost all cases.
        /// `T` can be the base class or interface of `value` - the actual runtime type is written out as a "-subType" field and read back.
        /// Read it back with `NeuroJsonReader.Read&lt;T&gt;(json)`, where T can be any base type of the written object.
        /// Alternatives: `WriteObject(value)` when you only have a `System.Type` at read time,
        /// `WriteGlobalTyped(value)` when the reader has no idea what type to expect - see [NeuroGlobalType].
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
        
        /// Same as `Write&lt;T&gt;(value)` but appends into your own StringBuilder instead of allocating a string.
        public void WriteTo<T>(StringBuilder strBuilder, ref T value, NeuroReferences refs = null, Options options = 0)
        {
            if (strBuilder == null)
            {
                return;
            }
            if (value == null)
            {
                strBuilder.Append("null");
                return;
            }
            if (typeof(T) == typeof(object))
            {
                throw GetErrorAboutGlobalTypes(value.GetType());
            }
            NeuroSyncTypes<T>.TryAutoRegisterTypeOrThrow();
            var sizeType = NeuroJsonSyncTypes<T>.SizeType;
            if (sizeType != NeuroConstants.Child && sizeType != NeuroConstants.ChildWithType)
            {
                throw NeuroSyncErrors.NotAStandaloneType(typeof(T), "write");
            }
            opts = options & ~Options.ExcludeTopLevelGlobalType;
            references = refs ?? defaultReferences;
            stringBuilder = strBuilder;
            stringBuilder.Append("{\n");
            numIndents = 1;
            
            var type = value.GetType();
            var posAtStart = stringBuilder.Length;
            // The runtime type is what matters, `value` may well be a sub class of T.
            if (sizeType == NeuroConstants.ChildWithType || type != typeof(T))
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
        
        /// Same output as `Write&lt;T&gt;(value)` but for when the type is only known at runtime.
        /// Read it back with `NeuroJsonReader.ReadObject(json, type)`.
        /// This is a bit slower as it needs to use reflection once per type.
        public string WriteObject(object value, NeuroReferences refs = null, Options options = 0)
        {
            if (defaultStringBuilder == null)
            {
                defaultStringBuilder = new StringBuilder();
            }
            else
            {
                defaultStringBuilder.Length = 0;
            }
            WriteObjectTo(defaultStringBuilder, value, refs, options);
            var result = defaultStringBuilder.ToString();
            defaultStringBuilder.Clear();
            return result;
        }
        
        /// This is a bit slower as it needs to use reflection once.
        public void WriteObjectTo(StringBuilder strBuilder, object value, NeuroReferences refs = null, Options options = 0)
        {
            if (strBuilder == null)
            {
                return;
            }
            if (value == null)
            {
                strBuilder.Append("null");
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
            typeInfo.Sync(this, typeInfo.SubTypeTag, value);
            if (stringBuilder.Length > posAtStart)
            {
                stringBuilder.Length -= 2;
                stringBuilder.Append("\n");
            }
            stringBuilder.Append("}");
            references = null;
            stringBuilder = null;
        }

        /// Writes the object with a "-globalType" field in front, so the reader can work out the type from the json alone.
        /// Use this when the reading side doesn't know what to expect. The type must have a [NeuroGlobalType] attribute.
        /// Read it back with `NeuroJsonReader.ReadGlobalTyped(json)`.
        public string WriteGlobalTyped(object value, NeuroReferences refs = null, Options options = 0)
        {
            if (defaultStringBuilder == null)
            {
                defaultStringBuilder = new StringBuilder();
            }
            else
            {
                defaultStringBuilder.Length = 0;
            }
            WriteGlobalTypedTo(defaultStringBuilder, value, refs, options);
            var result = defaultStringBuilder.ToString();
            defaultStringBuilder.Clear();
            return result;
        }

        public void WriteGlobalTypedTo(StringBuilder strBuilder, object value, NeuroReferences refs = null, Options options = 0)
        {
            if (strBuilder == null)
            {
                return;
            }
            if (value == null)
            {
                strBuilder.Append("null");
                return;
            }
            opts = options & ~Options.ExcludeTopLevelGlobalType;
            references = refs ?? defaultReferences;
            stringBuilder = strBuilder;
            stringBuilder.Append("{\n");
            numIndents = 1;
            NeuroSyncTypes.TryRegisterAllAssemblies();
            
            var type = value.GetType();
            var globalId = NeuroGlobalTypes.GetTypeIdOrThrow(type, out var rootType);
            if ((options & Options.ExcludeTopLevelGlobalType) == 0)
            {
                AppendSubTagAndOrName(FieldName_GlobalType, globalId, rootType.Name);
            }
            var posAtStart = stringBuilder.Length;
            var subTag = NeuroGlobalTypes.GetSubTypeTagOrThrow(type);
            if (subTag > 0)
            {
                AppendSubTagAndOrName(FieldName_ClassTag, subTag, type.Name);
            }
            NeuroGlobalTypes.Sync(globalId, this, subTag, ref value);
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
                stringBuilder.Append('"');
                AppendEscaped(stringBuilder, value);
                stringBuilder.Append('"');
            }
            else
            {
                stringBuilder.Append("null");
            }
        }

        /// Writes the body of a json string. Escapes exactly what the spec requires - the quote, the backslash
        /// and every control character below U+0020 - using the short form where there is one and \u00xx otherwise.
        /// Everything else, unicode included, goes out as is.
        static void AppendEscaped(StringBuilder stringBuilder, string value)
        {
            var start = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c >= ' ' && c != '"' && c != '\\')
                {
                    continue;
                }
                if (i > start)
                {
                    stringBuilder.Append(value, start, i - start);
                }
                switch (c)
                {
                    case '"':
                        stringBuilder.Append("\\\"");
                        break;
                    case '\\':
                        stringBuilder.Append("\\\\");
                        break;
                    case '\n':
                        stringBuilder.Append("\\n");
                        break;
                    case '\r':
                        stringBuilder.Append("\\r");
                        break;
                    case '\t':
                        stringBuilder.Append("\\t");
                        break;
                    case '\b':
                        stringBuilder.Append("\\b");
                        break;
                    case '\f':
                        stringBuilder.Append("\\f");
                        break;
                    default:
                        stringBuilder.Append("\\u00");
                        AppendHexDigit(stringBuilder, (c >> 4) & 0xF);
                        AppendHexDigit(stringBuilder, c & 0xF);
                        break;
                }
                start = i + 1;
            }
            if (start < value.Length)
            {
                stringBuilder.Append(value, start, value.Length - start);
            }
        }

        static void AppendHexDigit(StringBuilder stringBuilder, int digit)
        {
            stringBuilder.Append((char)(digit < 10 ? '0' + digit : 'a' + digit - 10));
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
                    AppendEscaped(stringBuilder, refName);
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
            if (value != null && !NeuroSyncTypes.AreEqual(value, defaultValue))
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

        internal static Exception GetErrorAboutGlobalTypes(Type typeHint)
        {
            var str = $"Write<object>(...) call is ambiguous. Here are 3 alternative paths:";
            
            var typeName = typeHint != null && typeHint != typeof(object) ? typeHint.Name : "MyClassType";
            str += $"\n1. Try use the correct generic parameter for best efficiency, such as `Write<{typeName}>(value)`.";
            
            str += $"\n2. If passing generic parameter is not possible, use `WriteObject(value)` instead.";
            
            str += $"\n3. If you want to write global typed value, use `WriteGlobalTyped(value)`.";
            
            return new Exception(str);
        }
    }
}