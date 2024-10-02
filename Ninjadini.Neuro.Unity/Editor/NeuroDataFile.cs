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
                        _value = (IReferencable)dataProvider.JsonReader.Read(json, RootType);
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

        public static uint ReadIdFromFileName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath.AsSpan());
            var splitIndex = fileName.IndexOf("-");
            if (splitIndex > 0)
            {
                fileName = fileName.Slice(0, splitIndex);
            }
            return uint.TryParse(fileName, out var id) ? id : (uint)0;
        }

        IReferencable INeuroReferencedItemLoader.Load(uint refId)
        {
            if (refId != RefId)
            {
                throw new InvalidOperationException($"Wrong ref id requested expecting {RefId} but {refId}");
            }
            return Value;
        }

        string INeuroReferencedItemLoader.GetRefName(uint refId)
        {
            return RefName;
        }
    }
}