using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Ninjadini.Neuro
{
    public class RawProtoReader
    {
        byte[] bytes;
        int position;
        int end;

        public void Set(byte[] bytes, int position = 0, int length = -1)
        {
            this.bytes = bytes;
            this.position = position;
            end = position + (length >= 0 ? length : (bytes.Length - position));
        }

        public void Set(BytesChunk bytesChunk)
        {
            bytes = bytesChunk.Bytes;
            position = bytesChunk.Position;
            end = position + bytesChunk.Length;
        }

        public int Position => position;

        public int Available => end - position;

        public BytesChunk GetCurrentBytesChunk()
        {
            return new BytesChunk()
            {
                Bytes = bytes,
                Position = 0,
                Length = position,
            };
        }

        public bool ReadBool()
        {
            CheckAvailable(1);
            var b = bytes[position++];
            if (b == 0) return false;
            if (b == 1) return true;
            throw new EndOfStreamException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            CheckAvailable(1);
            return bytes[position++];
        }

        public byte[] ReadBytes()
        {
            var size = (int)ReadUint();
            CheckAvailable(size);
            var result = new byte[size];
            Array.Copy(bytes, position, result, 0, size);
            position += size;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt()
        {
            return Zag(ReadUint());
        }

        public uint ReadUint()
        {
            var value = 0u;
            int shift = 0;
            var available = Available;
            while (available > 0)
            {
                available--;
                uint chunk = bytes[position++];
                value |= (chunk & 0x7F) << shift;
                if ((chunk & 0x80) == 0)
                {
                    return value;
                }

                shift += 7;
            }

            throw new EndOfStreamException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadLong()
        {
            return Zag(ReadULong());
        }

        public ulong ReadULong()
        {
            var value = 0ul;
            int shift = 0;
            var available = Available;
            while (available > 0)
            {
                available--;
                ulong chunk = bytes[position++];
                value |= (chunk & 0x7F) << shift;
                if ((chunk & 0x80) == 0)
                {
                    return value;
                }

                shift += 7;
            }

            throw new EndOfStreamException();
        }

        public string ReadString()
        {
            var length = (int)ReadUint();
            if (length == 0)
            {
                return string.Empty;
            }

            CheckAvailable(length);
            var str = RawProtoWriter.UTF8Encoding.GetString(bytes, position, length);
            position += length;
            return str;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat()
        {
            return BitConverter.Int32BitsToSingle(ReadInt32());
            //var intBit = ReadInt32();
            //return *(float*)&intBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            return BitConverter.Int64BitsToDouble(ReadInt64());
            //var bits = ReadInt64();
            //return *(double*)&bits;
        }

        public int ReadInt32()
        {
            CheckAvailable(4);
            return bytes[position++] << 24 | bytes[position++] << 16 | bytes[position++] << 8 | bytes[position++];
        }
        
        public long ReadInt64()
        {
            CheckAvailable(8);
            return (long)bytes[position++] << 56 | (long)bytes[position++] << 48 | (long)bytes[position++] << 40 |
                   (long)bytes[position++] << 32
                   | (long)bytes[position++] << 24 | (long)bytes[position++] << 16 | (long)bytes[position++] << 8 |
                   bytes[position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int length)
        {
            CheckAvailable(length);
            position += length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckAvailable(int size)
        {
            if (end - position < size)
            {
                throw new EndOfStreamException();
            }
        }

        const int Int32Msb = 1 << 31;
        const long Int64Msb = 1L << 63;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Zag(uint ziggedValue)
        {
            var value = (int)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~Int32Msb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Zag(ulong ziggedValue)
        {
            var value = (long)ziggedValue;
            return (-(value & 0x01L)) ^ ((value >> 1) & ~Int64Msb);
        }

        public string GetDebugString()
        {
            return $"[{nameof(RawProtoReader)} position:{position} length:{end} bytes:{BitConverter.ToString(bytes, 0, end)}]";
        }
        
        public string GetDebugString(int index, int length)
        {
            return BitConverter.ToString(bytes, index, length);
        }

        public static string GetDebugString(ReadOnlySpan<byte> bytes)
        {
            return $"[{bytes.Length} bytes:{BitConverter.ToString(bytes.ToArray())}]";
        }
        
        public static byte[] Decompress(byte[] data)
        {
            using var compressedStream = new MemoryStream(data);
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }
    }
}