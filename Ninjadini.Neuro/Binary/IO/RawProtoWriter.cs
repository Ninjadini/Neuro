using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ninjadini.Neuro
{
    public class RawProtoWriter
    {
        internal static readonly UTF8Encoding UTF8Encoding = new UTF8Encoding();
        
        byte[] buffer = Array.Empty<byte>();
        protected int position;

        public void Set(byte[] bytes, int index = 0)
        {
            buffer = bytes;
            position = index;
        }

        public byte[] Buffer => buffer;

        public int Position
        {
            get => position;
            set
            {
                if (buffer == null || value < 0 || value > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                position = value;
            }
        }

        public BytesChunk GetCurrentBytesChunk()
        {
            return new BytesChunk()
            {
                Bytes = buffer,
                Position = 0,
                Length = position,
            };
        }

        public void Write(bool value)
        {
            const byte BYTE_0 = 0;
            const byte BYTE_1 = 1;
            EnsureSize(1);
            buffer[position++] = value ? BYTE_1 : BYTE_0;
        }

        public void Write(byte value)
        {
            EnsureSize(1);
            buffer[position++] = value;
        }

        public void Write(byte[] value, int index = 0, int length = -1)
        {
            if (length < 0)
            {
                length = value.Length - index;
            }
            Write((uint)length);
            EnsureSize(length);
            Array.Copy(value, index, buffer, position, length);
            position += length;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            Write(Zig(value));
        }
        
        public void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Write((uint)0);
            }
            else
            {
                var bytesSize = UTF8Encoding.GetByteCount(value);
                Write((uint)bytesSize);
                EnsureSize(bytesSize);
                UTF8Encoding.GetBytes(value, 0, value.Length, buffer, position);
                position += bytesSize;
            }
        }

        public void Write(uint value)
        {
            // https://github.com/protobuf-net/protobuf-net/blob/4c43137a7653c7dc6f6e28b1455d6f7ac943a9c1/src/protobuf-net.Core/ProtoWriter.Stream.cs#L272
            EnsureSize(10); // just preempt, wont ever really need 10.
            var b = (byte)((value & 0x7F) | 0x80);
            while ((value >>= 7) != 0)
            {
                buffer[position++] = b;
                b = (byte)((value & 0x7F) | 0x80);
            }
            buffer[position++] = (byte)(b & 0x7F);
        }

        public void InsertUint(uint value, int index)
        {
            var uintBytes = 1;
            var tempValue = value;
            while ((tempValue >>= 7) != 0)
            {
                uintBytes++;
            }
            var existingL = position - index;
            Array.Copy(buffer, index, buffer, index + uintBytes, existingL);
            position = index;
            Write(value);
            position += existingL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value)
        {
            Write(Zig(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value)
        {
            EnsureSize(20); // just preempt, wont ever really need 20.
            var b = (byte)((value & 0x7F) | 0x80);
            while ((value >>= 7) != 0)
            {
                buffer[position++] = b;
                b = (byte)((value & 0x7F) | 0x80);
            }
            buffer[position++] = (byte)(b & 0x7F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(float value)
        {
            WriteInt32(BitConverter.SingleToInt32Bits(value));
            //WriteInt32(*(int*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(double value)
        {
            WriteInt64(BitConverter.DoubleToInt64Bits(value));
            //WriteInt64(*(long*)&value);
        }

        void WriteInt32(int value)
        {
            EnsureSize(4);
            buffer[position++] = (byte)(value >> 24);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)value;
        }

        void WriteInt64(long value)
        {
            EnsureSize(8);
            buffer[position++] = (byte)(value >> 56);
            buffer[position++] = (byte)(value >> 48);
            buffer[position++] = (byte)(value >> 40);
            buffer[position++] = (byte)(value >> 32);
            buffer[position++] = (byte)(value >> 24);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)value;
        }

        void EnsureSize(int size)
        {
            if (position + size > buffer.Length)
            {
                var newLength = position + size;
                newLength = Math.Max(newLength * 2, newLength + 128);
                var newBytes = new byte[newLength];
                Array.Copy(buffer, newBytes, buffer.Length);
                buffer = newBytes;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Zig(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong Zig(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        public string GetDebugString()
        {
            return $"[{nameof(RawProtoWriter)} length:{position} free:{buffer.Length - position} bytes:{BitConverter.ToString(buffer, 0, position)}]";
        }
        
        public static byte[] Compress(ReadOnlySpan<byte> data)
        {
            using var compressedStream = new MemoryStream();
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Compress);
            zipStream.Write(data);
            zipStream.Close();
            return compressedStream.ToArray();
        }
    }
}