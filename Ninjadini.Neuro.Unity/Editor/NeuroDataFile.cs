using System;
using System.IO;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroDataFile : INeuroReferencedItemLoader
    {
        public const string InvalidFileNameRegExp = @"[^\w\d._,+\-()_ ]";
        
        public readonly Type RootType;
        public uint RefId { get; private set; }
        public string FilePath { get; private set; }
        IReferencable _value;
        NeuroEditorDataProvider dataProvider;
        
        public NeuroDataFile(Type rootType, string filePath, NeuroEditorDataProvider dataProvider)
        {
            RootType = rootType;
            this.dataProvider = dataProvider;
            SetFilePath(filePath);
        }

        string _refName;
        public string RefName
        {
            get
            {
                if (_refName == null)
                {
                    var nameSpan = Path.GetFileNameWithoutExtension(FilePath.AsSpan());
                    var splitIndex = nameSpan.IndexOf("-");
                    _refName = splitIndex > 0 ? nameSpan.Slice(splitIndex + 1).ToString() : "";
                }
                return _refName;
            }
        }

        public IReferencable Value
        {
            get
            {
                if (_value == null)
                {
                    try
                    {
                        var json = File.ReadAllText(FilePath);
                        SetLastKnownContent(json);
                        _value = (IReferencable)dataProvider.JsonReader.ReadObject(json, RootType);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to read neuro data file {FilePath}", e);
                    }
                    _value.RefId = RefId;
                    _value.RefName = RefName;
                }
                return _value;
            }
            set
            {
                if (value != null)
                {
                    value.RefId = RefId;
                    value.RefName = RefName;
                }
                _value = value;
            }
        }
        
        public bool IsLoaded => _value != null;

        int lastKnownContentHash;
        int lastKnownContentLength = -1;

        /// Remembers the json this file holds as far as we are concerned, so a file change event can be told apart
        /// from the write that caused it. Comparing content rather than timing the write means a change event that
        /// arrives late - the editor was in the background, or the disk was busy - is still recognised as our own.
        internal void SetLastKnownContent(string json)
        {
            lastKnownContentLength = json?.Length ?? -1;
            lastKnownContentHash = json?.GetHashCode() ?? 0;
        }

        /// True if `json` is what this file already held, so there is nothing to reload.
        internal bool IsLastKnownContent(string json)
        {
            return json != null
                   && lastKnownContentLength == json.Length
                   && lastKnownContentHash == json.GetHashCode();
        }

        /// Re-reads json from disk into the object that is already loaded, so open editors and anything else
        /// holding this item see the new values instead of a stale copy.
        /// Returns the resulting value, which is a new object in the one case the old one can not be reused:
        /// the json turned out to be a different subtype. The caller has to repoint the reference table then.
        /// Throws if nothing was loaded to begin with - check IsLoaded first.
        internal IReferencable ReloadValueFromDisk(string json)
        {
            if (_value == null)
            {
                throw new InvalidOperationException($"Nothing loaded to reload @ {FilePath}");
            }
            var target = (object)_value;
            dataProvider.JsonReader.ReadObject(json, RootType, ref target);
            if (target is not IReferencable result)
            {
                throw new Exception($"Neuro data file read back as null or a non referencable @ {FilePath}");
            }
            SetLastKnownContent(json);
            result.RefId = RefId;
            result.RefName = RefName;
            _value = result;
            return result;
        }
        
        internal void SetFilePath(string filePath)
        {
            var refId = ReadIdFromFileName(filePath);
            if (refId > 0)
            {
                FilePath = filePath;
                RefId = refId;
                _refName = null;
            }
            else
            {
                throw new Exception($"Invalid file name {Path.GetFileNameWithoutExtension(filePath)}, can not determine refId @ {filePath}");
            }
        }

        /// The RefId a data file's name starts with. Data file names spell the RefId in base36 - this is not
        /// for the <c>&lt;typeId&gt;-&lt;TypeName&gt;</c> directory names, whose numbers are plain decimal global type ids.
        public static uint ReadIdFromFileName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath.AsSpan());
            var splitIndex = fileName.IndexOf("-");
            if (splitIndex > 0)
            {
                fileName = fileName.Slice(0, splitIndex);
            }
            return NeuroRefId.TryParse(fileName, out var id) ? id : (uint)0;
        }

        IReferencable INeuroReferencedItemLoader.Load(uint refId)
        {
            if (refId != RefId)
            {
                throw new InvalidOperationException($"Wrong ref id requested expecting {NeuroRefId.ToString(RefId)} but {NeuroRefId.ToString(refId)}");
            }
            return Value;
        }

        string INeuroReferencedItemLoader.GetRefName(uint refId)
        {
            return RefName;
        }
    }
}