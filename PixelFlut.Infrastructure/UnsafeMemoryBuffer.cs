﻿using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace PixelFlut.Infrastructure
{
    internal unsafe class UnsafeMemoryBuffer : IDisposable
    {
        private readonly byte[] buffer;
        private readonly GCHandle pinnedArray;
        private readonly byte* bufferptr;

        private readonly int bufferSize;
        private int position;

        public int BufferSize => bufferSize;
        public int Position { get => position; set => position = value; }

        public UnsafeMemoryBuffer(int bufferSize)
        {
            this.buffer = new byte[bufferSize];
            this.pinnedArray = GCHandle.Alloc(this.buffer, GCHandleType.Pinned);
            this.bufferptr = (byte*)pinnedArray.AddrOfPinnedObject();

            this.bufferSize = bufferSize;
        }

        public void Write(byte[] source, int length)
        {
#if DEBUG
            Contract.Assert(length > 0);
            Contract.Assert(position + length < bufferSize, "Buffer too small!");
#endif

            var i = length;
            while (--i >= 0)
            {
                bufferptr[i + position] = source[i];
            }
            position += length;
        }

        public void Write(byte* source, int length)
        {
#if DEBUG
            Contract.Assert(length > 0);
            Contract.Assert(position + length < bufferSize, "Buffer too small!");
#endif
            Buffer.MemoryCopy(source, bufferptr + position, long.MaxValue, length);
            position += length;
        }

        public void WriteNullTerminated(byte* source)
        {
            var i = 0;
            byte chr = 0;
            while ((chr = source[i]) != 0)
            {
                bufferptr[position] = chr;
                i++;
                position++;
            }
        }

        public void WriteByte(byte b)
        {
#if DEBUG
            Contract.Assert(position + 1 < bufferSize, $"Buffer too small! {position} {bufferSize}");
#endif
            bufferptr[position++] = b;
        }

        public ArraySegment<byte> ToArraySegment()
        {
            return new ArraySegment<byte>(buffer, 0, position);
        }

        public void Dispose()
        {
            this.pinnedArray.Free();
        }
    }
}