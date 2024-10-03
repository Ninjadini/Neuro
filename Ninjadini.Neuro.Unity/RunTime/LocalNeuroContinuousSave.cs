using System;
using System.IO;
using UnityEngine;

namespace Ninjadini.Neuro
{
    public class LocalNeuroContinuousSave<T> : IDisposable where T : new()
    {
        readonly string _filePath;
        NeuroBytesWriter _bytesWriter;
        FileStream _fileStream;

        T _data;
        
        public LocalNeuroContinuousSave(string filePath)
        {
            _filePath = filePath;
        }

        public static LocalNeuroContinuousSave<T> CreateInPersistedData(string fileName)
        {
            return new LocalNeuroContinuousSave<T>(Application.persistentDataPath + "/" + fileName);
        }
        
        public T GetData()
        {
            if (_data == null)
            {
                if (File.Exists(_filePath))
                {
                    var bytes = File.ReadAllBytes(_filePath);
                    _data = new NeuroBytesReader().Read<T>(bytes);
                }
                _data ??= new T();
            }
            return _data;
        }

        public void SetData(T value)
        {
            _data = value;
        }

        public void Save()
        {
            if (_data != null)
            {
                _bytesWriter ??= new NeuroBytesWriter();
                var bytesSpan = _bytesWriter.Write(_data);
                
                _fileStream ??= new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                _fileStream.Position = 0;
                _fileStream.Write(bytesSpan);
                _fileStream.SetLength(bytesSpan.Length);
                _fileStream.Flush(true);
            }
        }

        public void DeleteAndDispose()
        {
            Dispose();
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
            _fileStream = null;
        }
    }
}