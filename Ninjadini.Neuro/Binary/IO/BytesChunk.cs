using System;

namespace Ninjadini.Neuro
{
    public struct BytesChunk
    {
        public byte[] Bytes;
        public int Position;
        public int Length;

        public readonly Span<byte> GetSpan()
        {
            return new Span<byte>(Bytes, Position, Length);
        }

        public static implicit operator BytesChunk(byte[] bytes)
        {
            return new BytesChunk()
            {
                Bytes = bytes,
                Position = 0,
                Length = bytes.Length
            };
        }
    }
}