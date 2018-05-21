using System;
using System.Diagnostics.Contracts;

namespace PixelFlut.Infrastructure
{
    internal class FastUnsafeMemoryStream
    {
        private byte[] buffer;
        private int position;

        public FastUnsafeMemoryStream(int bufferSize)
        {
            this.buffer = new byte[bufferSize];
        }

        public void Write(byte[] source, int sourceIndex, int length)
        {
#if DEBUG
            Contract.Assert(length > 0);
            Contract.Assert(position + length < buffer.Length, "Buffer too small!");
#endif

            var i = length;
            while (--i >= 0)
            {
                buffer[i + position] = source[i];
            }
            position += length;
        }

        public void WriteByte(byte b)
        {
#if DEBUG
            Contract.Assert(position + 1 < buffer.Length, "Buffer too small!");
#endif
            buffer[position++] = b;
        }

        public byte[] ToArray()
        {
            var toReturn = new byte[position];
            Buffer.BlockCopy(buffer, 0, toReturn, 0, position);
            return toReturn;
        }
    }
}