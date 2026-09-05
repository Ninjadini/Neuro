using System;
using System.IO;
using UnityEngine;

namespace Ninjadini.Neuro
{
    /// This MonoBehaviour provides an easy and efficient way to save data continuously to disk.
    /// For example, you want to save player progress.
    /// Saves alternate between two files so that the one being written is never the one holding the last
    /// good save - whatever a crash or the OS killing the app interrupts, a whole save is still on disk.
    /// Both files are held open, so a save allocates nothing at all no matter how big the data is.
    /// This only work for writing to a single file at a time, if you want to save to different files, use LocalNeuroStorage - but not as efficient
    /// If you don't want to use MonoBehaviour, use <c>LocalNeuroContinuousSave&lt;T&gt;</c> directly.
    public class LocalNeuroContinuousSave : MonoBehaviour
    {
        [Tooltip("Warning: The value you set here may be overridden at runtime by call to `SetSaveFileName()`.")]
        [SerializeField] string saveFileName = "save";
        
        INeuroSavable _gameSave;
        Delegate _createDataFunc;
        float _targetSaveTime;

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
            var path = GetSavePath(saveFileName);
            return File.Exists(path + Slot0Extension) || File.Exists(path + Slot1Extension);
        }

        public void Save()
        {
            _gameSave?.Save();
            _targetSaveTime = 0f;
        }

        /// Instead of saving all the time on every user interaction, lets only save at max every 2 seconds
        public void DelayedSave(float seconds)
        {
            if (_targetSaveTime <= 0f)
            {
                _targetSaveTime = Time.unscaledTime + Math.Max(0f, seconds);
            }
        }

        void Update()
        {
            if (_targetSaveTime > 0f && Time.unscaledTime >= _targetSaveTime)
            {
                Save();
            }
        }

        /// Going to the background is the last moment a mobile app is guaranteed to get - the OS can kill it
        /// after this without OnDestroy ever running, so a save that is only pending has to happen now.
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _targetSaveTime > 0f)
            {
                Save();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _targetSaveTime > 0f)
            {
                Save();
            }
        }

        public void DeleteAndDispose()
        {
            _targetSaveTime = 0f;
            if (_gameSave != null)
            {
                _gameSave.DeleteAndDispose();
                _gameSave = null;
            }
            else
            {
                var path = GetSavePath(saveFileName);
                try
                {
                    DeleteIfExists(path + Slot0Extension);
                    DeleteIfExists(path + Slot1Extension);
                    // Anything left over from before saves were split into two slots.
                    DeleteIfExists(path);
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
            if (_targetSaveTime > 0f)
            {
                Save();
            }
            _gameSave?.Dispose();
            _gameSave = null;
        }

        static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static string GetSavePath(string saveName)
        {
            return Application.persistentDataPath + "/" + saveName;
        }

        /// Saves alternate between these two files, so the one being written is never the one holding the
        /// last good save.
        public const string Slot0Extension = ".0";
        public const string Slot1Extension = ".1";
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
    /// Saves alternate between two files so that the one being written is never the one holding the last
    /// good save - whatever a crash or the OS killing the app interrupts, a whole save is still on disk.
    /// Both files are held open, so a save allocates nothing at all no matter how big the data is.
    /// This only work for writing to a single file at a time, if you want to save to different files, use LocalNeuroStorage - but not as efficient
    public class LocalNeuroContinuousSave<T> : INeuroSavable where T : class
    {
        /// magic(4) + sequence(8) + payload length(4) + payload checksum(4).
        /// The length and the checksum are what make a half written slot recognisable - without them there
        /// is no telling a truncated save from a whole one, and a truncated neuro payload can still read
        /// back as a plausible looking object.
        public const int HeaderSize = 20;
        const byte FormatVersion = 1;

        readonly string _filePath;
        // Built once - saves run over and over and must not allocate a string every time.
        readonly string _slot0Path;
        readonly string _slot1Path;
        readonly FileStream[] _slotStreams = new FileStream[2];
        readonly byte[] _headerBuffer = new byte[HeaderSize];

        NeuroBytesWriter _bytesWriter;
        T _data;
        Func<T> _createDataFunc;

        bool _slotsScanned;
        ulong _sequence;
        int _newestSlot = -1;
        int _nextSlot;

        public Type DataType => typeof(T);

        public LocalNeuroContinuousSave(string filePath, Func<T> createDataFunc = null)
        {
            _filePath = filePath;
            _slot0Path = filePath + LocalNeuroContinuousSave.Slot0Extension;
            _slot1Path = filePath + LocalNeuroContinuousSave.Slot1Extension;
            _createDataFunc = createDataFunc;
        }

        public static LocalNeuroContinuousSave<T> CreateInPersistedData(string fileName, Func<T> createDataFunc = null)
        {
            var path = LocalNeuroContinuousSave.GetSavePath(fileName);
            return new LocalNeuroContinuousSave<T>(path, createDataFunc);
        }

        string SlotPath(int slot) => slot == 0 ? _slot0Path : _slot1Path;

        public T GetData()
        {
            if (_data == null)
            {
                _data = LoadFromSlots();
                _data ??= _createDataFunc?.Invoke() ?? Activator.CreateInstance<T>();
            }
            return _data;
        }

        T LoadFromSlots()
        {
            ScanSlots();
            if (_newestSlot < 0)
            {
                return null;
            }
            var result = TryDeserializeSlot(_newestSlot);
            if (result != null)
            {
                return result;
            }
            // The newest slot is whole but will not read back - a changed data type, most likely. The other
            // slot is the next best thing, and the next save should land on the bad one rather than on it.
            // The sequence is already the highest of the two, so that save still supersedes both.
            var older = 1 - _newestSlot;
            result = TryDeserializeSlot(older);
            if (result != null)
            {
                _nextSlot = _newestSlot;
            }
            return result;
        }

        T TryDeserializeSlot(int slot)
        {
            if (!TryReadSlot(slot, out _, out var bytes, out var length))
            {
                return null;
            }
            try
            {
                return new NeuroBytesReader().Read<T>(new BytesChunk()
                {
                    Bytes = bytes,
                    Position = HeaderSize,
                    Length = length
                });
            }
            catch (Exception e)
            {
                var path = SlotPath(slot);
                try
                {
                    var num = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    var failPath = $"{path}-failed{num}";
                    File.Copy(path, failPath);
                    Debug.LogWarning($"Error loading from persisted data @ {path}, filed backed up @ {failPath}. Error: {e}");
                }
                catch (Exception)
                {
                    Debug.LogWarning($"Error loading from persisted data @ {path}. Error: {e}");
                }
                return null;
            }
        }

        /// Works out which slot holds the newest whole save. Has to happen before the first save even when
        /// nothing was loaded, otherwise a save could land on top of the only good copy.
        void ScanSlots()
        {
            if (_slotsScanned)
            {
                return;
            }
            _slotsScanned = true;
            for (var slot = 0; slot < 2; slot++)
            {
                if (TryReadSlot(slot, out var sequence, out _, out _)
                    && (_newestSlot < 0 || sequence > _sequence))
                {
                    _newestSlot = slot;
                    _sequence = sequence;
                }
            }
            // Save into whichever slot is not holding the newest save.
            _nextSlot = _newestSlot < 0 ? 0 : 1 - _newestSlot;
        }

        bool TryReadSlot(int slot, out ulong sequence, out byte[] bytes, out int length)
        {
            sequence = 0;
            bytes = null;
            length = 0;
            var path = SlotPath(slot);
            if (!File.Exists(path))
            {
                return false;
            }
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error reading persisted data @ {path}. Error: {e}");
                bytes = null;
                return false;
            }
            if (bytes.Length < HeaderSize
                || bytes[0] != (byte)'N' || bytes[1] != (byte)'S' || bytes[2] != (byte)'V'
                || bytes[3] != FormatVersion)
            {
                bytes = null;
                return false;
            }
            sequence = ReadULong(bytes, 4);
            length = ReadInt(bytes, 12);
            var checksum = ReadUInt(bytes, 16);
            if (length < 0 || HeaderSize + (long)length > bytes.Length || Checksum(bytes, HeaderSize, length) != checksum)
            {
                // A save that was interrupted part way through writing this slot.
                sequence = 0;
                bytes = null;
                length = 0;
                return false;
            }
            return true;
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

        /// Writing over the live save file means a crash, a kill from the OS, or a battery dying part way
        /// through leaves a half written file and no way back. So each save goes to whichever of the two
        /// slots is not holding the last good one, and only becomes the one that gets loaded once it is
        /// whole - which its length and checksum are what prove.
        public void Save()
        {
            if (_data == null)
            {
                return;
            }
            ScanSlots();
            _bytesWriter ??= new NeuroBytesWriter();
            _bytesWriter.Write(_data);
            // The byte[] and count, rather than the span, because Stream's span overload is only guaranteed
            // not to allocate where FileStream overrides it - this one never does, on any runtime.
            var payload = _bytesWriter.GetCurrentBytesChunk();

            var slot = _nextSlot;
            var stream = _slotStreams[slot] ??= new FileStream(SlotPath(slot), FileMode.OpenOrCreate,
                // bufferSize 1 turns the stream's own buffering off - the save goes out in two writes as it
                // is, so a buffer would only be 4KB of garbage per stream.
                FileAccess.Write, FileShare.Read, 1);

            var sequence = _sequence + 1;
            WriteHeader(sequence, payload);
            stream.Position = 0;
            stream.Write(_headerBuffer, 0, HeaderSize);
            stream.Write(payload.Bytes, payload.Position, payload.Length);
            stream.SetLength(HeaderSize + payload.Length);
            stream.Flush(true);

            _sequence = sequence;
            _newestSlot = slot;
            // The next save goes to the other one, so this - now the newest whole save - is left alone.
            _nextSlot = 1 - slot;
        }

        void WriteHeader(ulong sequence, in BytesChunk payload)
        {
            var buffer = _headerBuffer;
            buffer[0] = (byte)'N';
            buffer[1] = (byte)'S';
            buffer[2] = (byte)'V';
            buffer[3] = FormatVersion;
            WriteULong(buffer, 4, sequence);
            WriteInt(buffer, 12, payload.Length);
            WriteUInt(buffer, 16, Checksum(payload.Bytes, payload.Position, payload.Length));
        }

        static void WriteULong(byte[] buffer, int index, ulong value)
        {
            for (var i = 0; i < 8; i++)
            {
                buffer[index + i] = (byte)(value >> (8 * i));
            }
        }

        static void WriteInt(byte[] buffer, int index, int value) => WriteUInt(buffer, index, (uint)value);

        static void WriteUInt(byte[] buffer, int index, uint value)
        {
            for (var i = 0; i < 4; i++)
            {
                buffer[index + i] = (byte)(value >> (8 * i));
            }
        }

        static ulong ReadULong(byte[] buffer, int index)
        {
            var value = 0ul;
            for (var i = 0; i < 8; i++)
            {
                value |= (ulong)buffer[index + i] << (8 * i);
            }
            return value;
        }

        static int ReadInt(byte[] buffer, int index) => (int)ReadUInt(buffer, index);

        static uint ReadUInt(byte[] buffer, int index)
        {
            var value = 0u;
            for (var i = 0; i < 4; i++)
            {
                value |= (uint)buffer[index + i] << (8 * i);
            }
            return value;
        }

        /// FNV-1a. Only has to catch a slot that was not written all the way through, and it must not
        /// allocate - this runs on every save.
        static uint Checksum(byte[] bytes, int index, int length)
        {
            var hash = 2166136261u;
            var end = index + length;
            for (var i = index; i < end; i++)
            {
                hash = (hash ^ bytes[i]) * 16777619u;
            }
            return hash;
        }

        public void DeleteAndDispose()
        {
            Dispose();
            DeleteIfExists(_slot0Path);
            DeleteIfExists(_slot1Path);
            // Anything left over from before saves were split into two slots.
            DeleteIfExists(_filePath);
            _slotsScanned = true;
            _sequence = 0;
            _newestSlot = -1;
            _nextSlot = 0;
        }

        static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void Dispose()
        {
            for (var i = 0; i < _slotStreams.Length; i++)
            {
                _slotStreams[i]?.Dispose();
                _slotStreams[i] = null;
            }
        }
    }
}
