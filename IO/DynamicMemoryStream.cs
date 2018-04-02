/*
 * nDiscUtils - Advanced utilities for disc management
 * Copyright (C) 2018  Lukas Berger
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;

using static nDiscUtils.NativeMethods;

namespace nDiscUtils.IO
{

    public sealed class DynamicMemoryStream : Stream
    {

        private long mCapacity;
        private int mBlockSize;
        private long mBlockCount;
        private StreamMode mMode;
        private IntPtr[] mMemory;

        private long mLength;
        private long mPosition;

        public DynamicMemoryStream(long capacity)
            : this(capacity, 4096, StreamMode.ReadWrite)
        { }

        public DynamicMemoryStream(long capacity, int blockSize)
            : this(capacity, blockSize, StreamMode.ReadWrite)
        { }

        public DynamicMemoryStream(long capacity, StreamMode mode)
            : this(capacity, 4096, mode)
        { }

        public DynamicMemoryStream(long capacity, int blockSize, StreamMode mode)
        {
            mCapacity = capacity;
            mBlockSize = blockSize;
            mBlockCount = (long)Math.Ceiling((double)mCapacity / mBlockSize);
            mMode = mode;
            mMemory = new IntPtr[mBlockCount];
            for (int i = 0; i < mBlockCount; i++)
                mMemory[i] = IntPtr.Zero;
        }

        public override bool CanRead
        {
            get => (mMode & StreamMode.Read) == StreamMode.Read;
        }

        public override bool CanSeek
        {
            get => true;
        }

        public override bool CanTimeout
        {
            get => false;
        }

        public override bool CanWrite
        {
            get => (mMode & StreamMode.Write) == StreamMode.Write;
        }

        public override long Length
        {
            get => mCapacity;
        }

        public long Size
        {
            get => mLength;
        }

        public override long Position
        {
            get => mPosition;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readCount = 0;
            var readableCount = count - offset;

            if (readableCount <= 0)
                throw new ArgumentException("Not enough buffer capacity available to read");

            while (readableCount > 0)
            {
                var absoluteOffset = (mPosition + readCount);
                if (absoluteOffset > mCapacity)
                    throw new ArgumentException("Offset is out of stream boundaries");

                var memoryIndex = (long)Math.Floor((double)(mPosition + readCount) / mBlockSize);
                var memoryBlock = mMemory[memoryIndex];
                var blockCount = Math.Min(mBlockSize, count - readCount);

                unsafe
                {
                    void* bufferPtr;

                    fixed (void* fixedBufferPtr = &buffer[offset + readCount])
                        bufferPtr = fixedBufferPtr;

                    if (memoryBlock == IntPtr.Zero)
                    {
                        ZeroMemory(bufferPtr, blockCount);
                    }
                    else
                    {
                        void* blockPtr;

                        if ((memoryIndex * mBlockSize) < absoluteOffset)
                        {
                            var delta = (int)(absoluteOffset - (memoryIndex * mBlockSize));
                            blockPtr = (void*)((byte*)memoryBlock.ToPointer() + delta);

                            if (blockCount + delta > mBlockSize)
                                blockCount -= delta;
                        }
                        else
                        {
                            blockPtr = memoryBlock.ToPointer();
                        }

                        CopyMemory(bufferPtr, blockPtr, (uint)blockCount);
                    }
                }

                readCount += blockCount;
                readableCount -= blockCount;
            }

            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: mPosition = offset; break;
                case SeekOrigin.Current: mPosition += offset; break;
                case SeekOrigin.End: mPosition = mCapacity - offset; break;
            }

            if (mPosition < 0)
            {
                while (mPosition < 0)
                    mPosition += mCapacity;
            }
            else if (mPosition >= mCapacity)
            {
                while (mPosition >= mCapacity)
                    mPosition -= mCapacity;
            }

            return mPosition;
        }

        public override void SetLength(long value) { }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var writeCount = 0;
            var writableCount = count - offset;

            if (writableCount <= 0)
                throw new ArgumentException("Not enough buffer capacity available to write");

            while (writableCount > 0)
            {
                var absoluteOffset = (mPosition + writeCount);
                if (absoluteOffset > mCapacity)
                    throw new ArgumentException("Offset is out of stream boundaries");

                var memoryIndex = (long)Math.Floor((double)(mPosition + writeCount) / mBlockSize);
                var memoryBlock = mMemory[memoryIndex];
                var blockCount = Math.Min(mBlockSize, count - writeCount);

                unsafe
                {
                    void* bufferPtr;

                    fixed (void* fixedBufferPtr = &buffer[offset + writeCount])
                        bufferPtr = fixedBufferPtr;

                    if (memoryBlock == IntPtr.Zero)
                    {
                        memoryBlock = Allocate(memoryIndex);
                    }

                    void* blockPtr;

                    if ((memoryIndex * mBlockSize) < absoluteOffset)
                    {
                        var delta = (int)(absoluteOffset - (memoryIndex * mBlockSize));
                        blockPtr = (void*)((byte*)memoryBlock.ToPointer() + delta);

                        if (blockCount + delta > mBlockSize)
                            blockCount -= delta;
                    }
                    else
                    {
                        blockPtr = memoryBlock.ToPointer();
                    }

                    CopyMemory(blockPtr, bufferPtr, (uint)blockCount);
                }

                writeCount += blockCount;
                writableCount -= blockCount;
            }
        }

        public IntPtr Allocate(long index)
        {
            var ptr = Marshal.AllocHGlobal(mBlockSize);
            if (ptr == IntPtr.Zero)
                throw new IOException($"Failed to allocate {mBlockSize} bytes of memory");

            mMemory[index] = ptr;
            mLength += mBlockSize;

            return ptr;
        }

        public bool Free(long index)
        {
            var block = mMemory[index];
            if (block == IntPtr.Zero)
                return false;

            Marshal.FreeHGlobal(block);

            mMemory[index] = IntPtr.Zero;
            mLength -= mBlockSize;

            return true;
        }

    }

}
