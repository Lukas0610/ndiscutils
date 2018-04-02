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

namespace nDiscUtils.IO
{

    public unsafe sealed class DynamicMemoryStream : Stream
    {

        private long mCapacity;
        private int mBlockSize;
        private long mBlockCount;
        private StreamMode mMode;
        private void*[] mMemory;

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
            mMemory = new void*[mBlockCount];
            for (int i = 0; i < mBlockCount; i++)
                mMemory[i] = Memory.NULL;
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
                    fixed (void* fixedBufferPtr = buffer)
                        bufferPtr = fixedBufferPtr;

                    if (memoryBlock == Memory.NULL)
                    {
                        Memory.Set(bufferPtr, blockCount, 0);
                    }
                    else
                    {
                        int blockPtrOffset = 0;

                        if ((memoryIndex * mBlockSize) < absoluteOffset)
                        {
                            var delta = (int)(absoluteOffset - (memoryIndex * mBlockSize));
                            blockPtrOffset = delta;
                            if (blockCount + delta > mBlockSize)
                                blockCount -= delta;
                        }

                        Memory.Copy(bufferPtr, offset + readCount, memoryBlock, blockPtrOffset, (uint)blockCount);
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
                    fixed (void* fixedBufferPtr = buffer)
                        bufferPtr = fixedBufferPtr;

                    if (memoryBlock == Memory.NULL)
                    {
                        memoryBlock = Allocate(memoryIndex);
                        Memory.Set(memoryBlock, mBlockSize, 0);
                    }

                    void* blockPtr = memoryBlock;
                    int blockPtrOffset = 0;

                    if ((memoryIndex * mBlockSize) < absoluteOffset)
                    {
                        var delta = (int)(absoluteOffset - (memoryIndex * mBlockSize));
                        blockPtrOffset = delta;
                        if (blockCount + delta > mBlockSize)
                            blockCount -= delta;
                    }

                    Memory.Copy(blockPtr, blockPtrOffset, bufferPtr, offset + writeCount, (uint)blockCount);
                }

                writeCount += blockCount;
                writableCount -= blockCount;
            }
        }

        public void* Allocate(long index)
        {
            var ptr = Memory.Allocate(mBlockSize);
            if (ptr == Memory.NULL)
                throw new IOException($"Failed to allocate {mBlockSize} bytes of memory");

            mMemory[index] = ptr;
            mLength += mBlockSize;

            return ptr;
        }

        public bool Free(long index)
        {
            var block = mMemory[index];
            if (block == Memory.NULL)
                return false;

            Memory.Free(block);

            mMemory[index] = Memory.NULL;
            mLength -= mBlockSize;

            return true;
        }

    }

}
