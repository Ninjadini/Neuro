using System;
using System.IO;
using Ninjadini.Neuro.Sync;
using UnityEngine;

namespace Ninjadini.Neuro
{
    public class LocalNeuroContinuousSave<T> : IDisposable where T : class
    {
        readonly string _filePath;
        NeuroBytesWriter _bytesWriter;
        FileStream _fileStream;

        T _data;
        Func<T> _createDataFunc;
        
        public LocalNeuroContinuousSave(string filePath, Func<T> createDataFunc = null)
        {
            _filePath = filePath;
            _createDataFunc = createDataFunc;
        }

        public static LocalNeuroContinuousSave<T> CreateInPersistedData(string fileName)
        {
            return new LocalNeuroContinuousSave<T>(Application.persistentDataPath + "/" + fileName);
        }
        
        public T GetData()
        {
            if (_data == null)
            {
                try
                {
                    if (File.Exists(_filePath))
                    {
                        var bytes = File.ReadAllBytes(_filePath);
                        _data = new NeuroBytesReader().Read<T>(bytes);
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        var num = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                        var failPath = $"{_filePath}-failed{num}";
                        File.Copy(_filePath, failPath);
                        Debug.LogWarning($"Error loading from persisted data @ {_filePath}, filed backed up @ {failPath}. Error: {e}");
                    }
                    catch(Exception)
                    {
                        Debug.LogWarning($"Error loading from persisted data @ {_filePath}. Error: {e}");
                    }
                }
                _data ??= _createDataFunc?.Invoke() ?? Activator.CreateInstance<T>();
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