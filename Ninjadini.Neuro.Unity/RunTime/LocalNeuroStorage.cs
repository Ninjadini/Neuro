using System.IO;
using UnityEngine;

namespace Ninjadini.Neuro
{
    public class LocalNeuroStorage
    {
        NeuroBytesWriter _bytesWriter;
        string _saveDirectory;

        public LocalNeuroStorage(string saveDirectory = null)
        {
            _bytesWriter = new NeuroBytesWriter();
            if (string.IsNullOrEmpty(saveDirectory))
            {
                saveDirectory = Application.persistentDataPath + "/";
            }
            else if (!saveDirectory.EndsWith("/"))
            {
                saveDirectory += "/";
            }
            _saveDirectory = saveDirectory;
        }
        
        public string SaveDirectory => _saveDirectory;
        
        public void Save<T>(T obj, string name)
        {
            if (obj != null)
            {
                var path = GetPath(name);
                var bytesSpan = _bytesWriter.Write(obj);
                //var bytes = bytesSpan.ToArray();
                //File.WriteAllBytes(path, bytes);
                using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
                fileStream.Write(bytesSpan);
            }
        }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public T? TryLoad<T>(string name) where T : class
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            var path = GetPath(name);
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                return new NeuroBytesReader().Read<T>(bytes);
            }
            return null;
        }

        public void Delete(string name)
        {
            var path = GetPath(name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public string GetPath(string name)
        {
            if(string.IsNullOrEmpty(name))
            {
                throw new System.ArgumentNullException();
            }
            return _saveDirectory + name;
        }
    }
}