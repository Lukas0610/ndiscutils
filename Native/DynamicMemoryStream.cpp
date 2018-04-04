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
#include "stdafx.h"
#include "stdio.h"
#include "stdlib.h"
#include "string.h"

#include "DynamicMemoryStream.h"
#include "Memory.h"
#include "StreamMode.h"

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;

using namespace nDiscUtils::IO;

namespace nDiscUtils {
namespace IO {

    DynamicMemoryStream::DynamicMemoryStream(long long capacity) :
        DynamicMemoryStream(capacity, 4096, StreamMode::ReadWrite) { }

    DynamicMemoryStream::DynamicMemoryStream(long long capacity, int blockSize) :
        DynamicMemoryStream(capacity, blockSize, StreamMode::ReadWrite) { }

    DynamicMemoryStream::DynamicMemoryStream(long long capacity, StreamMode streamMode) :
        DynamicMemoryStream(capacity, 4096, streamMode) { }

    DynamicMemoryStream::DynamicMemoryStream(long long capacity, int blockSize, StreamMode streamMode) :
        mCapacity(capacity),
        mBlockSize(blockSize),
        mMode(streamMode)
    {
        mBlockCount = (long)Math::Ceiling((double)mCapacity / mBlockSize);
        mMemorySize = mBlockCount * sizeof(void*);
        mMemory = (void**)Memory::Allocate(mMemorySize);
        Memory::Set(mMemory, 0, mMemorySize);
    }

    long long DynamicMemoryStream::Seek(long long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin::Begin: mPosition = offset; break;
            case SeekOrigin::Current: mPosition += offset; break;
            case SeekOrigin::End: mPosition = mCapacity - offset; break;
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

    int DynamicMemoryStream::Read(array<unsigned char> ^buffer, int offset, int count)
    {
        auto readCount = 0;
        auto readableCount = Math::Min(buffer->Length - offset, count);

        if (!CanRead)
            throw gcnew ArgumentException("Stream is not opened for read-access");

        if (readableCount <= 0)
            throw gcnew ArgumentException("Final buffer position would not meet requested write count");

        auto bufferHandle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
        auto bufferPointer = (void*)bufferHandle.AddrOfPinnedObject();

        while (readableCount > 0)
        {
            auto absoluteOffset = (mPosition + readCount);
            if (absoluteOffset > mCapacity)
                throw gcnew ArgumentException("Offset is out of stream boundaries");

            auto memoryIndex = (long)Math::Floor((double)(mPosition + readCount) / mBlockSize);
            auto memoryBlock = mMemory[memoryIndex];
            auto blockCount = Math::Min(mBlockSize, count - readCount);

            if (memoryBlock == nullptr)
            {
                Memory::Set(bufferPointer, offset + readCount, 0, mBlockSize);
            }
            else
            {
                auto blockPtrOffset = 0;

                if ((memoryIndex * mBlockSize) < absoluteOffset)
                {
                    auto delta = (int)(absoluteOffset - (memoryIndex * mBlockSize));
                    blockPtrOffset = delta;
                    if (blockCount + delta > mBlockSize)
                        blockCount -= delta;
                }

                Memory::Copy(
                    memoryBlock, blockPtrOffset,
                    bufferPointer, offset + readCount,
                    blockCount);
            }

            readCount += blockCount;
            readableCount -= blockCount;
        }

        bufferHandle.Free();
        return readCount;
    }

    void DynamicMemoryStream::Write(array<unsigned char> ^buffer, int offset, int count)
    {
        auto writeCount = 0;
        auto writableCount = Math::Min(buffer->Length - offset, count);

        if (!CanWrite)
            throw gcnew ArgumentException("Stream is not opened for write-access");

        if (writableCount != count)
            throw gcnew ArgumentException("Final buffer position would not meet requested write count");

        auto bufferHandle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
        auto bufferPointer = (void*)bufferHandle.AddrOfPinnedObject();

        while (writableCount > 0)
        {
            auto absoluteOffset = (mPosition + writeCount);
            if (absoluteOffset > mCapacity)
                throw gcnew ArgumentException("Offset is out of stream boundaries");

            auto memoryIndex = (long)Math::Floor((double)(mPosition + writeCount) / mBlockSize);
            auto memoryBlock = mMemory[memoryIndex];
            auto blockCount = Math::Min(mBlockSize, count - writeCount);            

            if (memoryBlock == nullptr)
            {
                mMemory[memoryIndex]
                    = memoryBlock
                    = Memory::Allocate(mBlockSize);
            }

            auto blockPtr = memoryBlock;
            auto blockPtrOffset = 0;

            if ((memoryIndex * mBlockSize) < absoluteOffset)
            {
                auto delta = (int)(absoluteOffset - (memoryIndex * mBlockSize));
                blockPtrOffset = delta;
                if (blockCount + delta > mBlockSize)
                    blockCount -= delta;
            }

            Memory::Copy(
                bufferPointer, offset + writeCount,
                memoryBlock, blockPtrOffset,
                blockCount);

            writeCount += blockCount;
            writableCount -= blockCount;
        }

        bufferHandle.Free();
    }

} // IO
} // nDiscUtils
