using System;
using System.IO;
using UnityEngine;

namespace Ninjadini.Neuro
{
    /// This MonoBehaviour provides an easy and efficient way to save data continuously to disk.
    /// For example, you want to save player progress.
    /// This feature is written in such a way that it will not allocate memory to write into disk.
    /// This only work for writing to a single file at a time, if you want to save to different files, use LocalNeuroStorage - but not as efficient
    /// If you don't want to use MonoBehaviour, use LocalNeuroContinuousSave<T> directly.
    public class LocalNeuroContinuousSave : MonoBehaviour
    {
        [Tooltip("Warning: The value you set here may be overridden at runtime by call to `SetSaveFileName()`.")]
        [SerializeField] string saveFileName = "save";
        
        INeuroSavable _gameSave;
        Delegate _createDataFunc;

        public T GetData<T>() where T : class
        {
            EnsureGameSave<T>();
            return (T)_gameSave.GetData();
        }

        public void SetData<T>(T value) where T : class
        {
            EnsureGameSave<T>();
            if (_gameSave.DataType != typeof(T))
            {
                throw new Exception($"Save data type mismatch. Was {_gameSave.DataType} but trying to set {typeof(T)}");
            }
            _gameSave.SetData(value);
        }

        void EnsureGameSave<T>() where T : class
        {
            if (_gameSave != null)
            {
                return;
            }
            if (string.IsNullOrEmpty(saveFileName))
            {
                throw new Exception($"{nameof(saveFileName)} can not be empty.");
            }
            Func<T> createDataFunc = _createDataFunc != null ? () => ((Func<T>)_createDataFunc)() : null;
            _gameSave = LocalNeuroContinuousSave<T>.CreateInPersistedData(saveFileName, createDataFunc);
        }

        public void SetSaveFileName(string saveName)
        {
            if (_gameSave != null)
            {
                throw new Exception($"Game data is already loaded, it is too late to call {nameof(SetSaveFileName)}");
            }
            if (string.IsNullOrEmpty(saveName))
            {
                throw new ArgumentNullException(nameof(saveName));
            }
            saveFileName = saveName;
        }

        public void SetCustomCreationFunction<T>(Func<T> createDataFunc)
        {
            if (_gameSave != null)
            {
                throw new Exception($"Game data is already loaded, it is too late to call {nameof(SetCustomCreationFunction)}");
            }
            _createDataFunc = createDataFunc;
        }

        public bool FileExists()
        {
            return File.Exists(GetSavePath(saveFileName));
        }

        public void Save()
        {
            _gameSave?.Save();
        }

        public void DeleteAndDispose()
        {
            if (_gameSave != null)
            {
                _gameSave.DeleteAndDispose();
            }
            else
            {
                var path = Application.persistentDataPath + "/" + saveFileName;
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception err)
                {
                    Debug.LogWarning($"Failed to delete path {path}: {err}");
                    throw;
                }
            }
        }

        void OnDestroy()
        {
            _gameSave?.Dispose();
            _gameSave = null;
        }

        public static string GetSavePath(string saveName)
        {
            return Application.persistentDataPath + "/" + saveName;
        }
    }
        
    public interface INeuroSavable : IDisposable
    {
        Type DataType { get; }
        object GetData();
        void SetData(object value);
        void Save();
        void DeleteAndDispose();
    }

    /// This provides an easy and efficient way to save data continuously to disk.
    /// For example, you want to save player progress.
    /// This feature is written in such a way that it will not allocate memory to write into disk.
    /// This only work for writing to a single file at a time, if you want to save to different files, use LocalNeuroStorage - but not as efficient
    public class LocalNeuroContinuousSave<T> : INeuroSavable where T : class
    {
        readonly string _filePath;
        NeuroBytesWriter _bytesWriter;
        FileStream _fileStream;

        T _data;
        Func<T> _createDataFunc;

        public Type DataType => typeof(T);
        
        public LocalNeuroContinuousSave(string filePath, Func<T> createDataFunc = null)
        {
            _filePath = filePath;
            _createDataFunc = createDataFunc;
        }

        public static LocalNeuroContinuousSave<T> CreateInPersistedData(string fileName, Func<T> createDataFunc = null)
        {
            var path = LocalNeuroContinuousSave.GetSavePath(fileName);
            return new LocalNeuroContinuousSave<T>(path, createDataFunc);
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

        void INeuroSavable.SetData(object value)
        {
            SetData((T)value);
        }

        object INeuroSavable.GetData()
        {
            return GetData();
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