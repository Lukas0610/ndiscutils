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

        if (!CanRead)
            throw gcnew ArgumentException("Stream is not opened for read-access");

        AssertBufferParameters(buffer, offset, count);

        auto bufferHandle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
        auto bufferPointer = (void*)bufferHandle.AddrOfPinnedObject();

        while (readCount < count)
        {
            auto bufferOffset = offset + readCount;

            auto readBlockSize = (long long)Math::Min(count - readCount, mBlockSize);
            auto blockIndex = (long long)Math::Floor((double)mPosition / mBlockSize);
            auto blockBegin = (blockIndex * mBlockSize);
            auto nextBlockBegin = blockBegin + mBlockSize;
            auto innerBlockOffset = 0LL;

            if (mPosition + readBlockSize > nextBlockBegin)
                readBlockSize = (mPosition + readBlockSize) - nextBlockBegin;

            if (mPosition > blockBegin)
                innerBlockOffset = mPosition - blockBegin;

            auto blockMemory = mMemory[blockIndex];
            if (blockMemory == nullptr)
            {
                Memory::Set(bufferPointer, bufferOffset, 0, readBlockSize);
            }
            else
            {
                Memory::Copy(
                    blockMemory, innerBlockOffset,
                    bufferPointer, bufferOffset,
                    readBlockSize);
            }

            mPosition += readBlockSize;
            readCount += (int)readBlockSize;
        }

        bufferHandle.Free();
        return readCount;
    }

    void DynamicMemoryStream::Write(array<unsigned char> ^buffer, int offset, int count)
    {
        auto writeCount = 0;

        if (!CanWrite)
            throw gcnew ArgumentException("Stream is not opened for write-access");

        AssertBufferParameters(buffer, offset, count);

        auto bufferHandle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
        auto bufferPointer = (void*)bufferHandle.AddrOfPinnedObject();

        while (writeCount < count)
        {
            auto bufferOffset = offset + writeCount;

            auto writeBlockSize = (long long)Math::Min(count - writeCount, mBlockSize);
            auto blockIndex = (long long)Math::Floor((double)mPosition / mBlockSize);
            auto blockBegin = (blockIndex * mBlockSize);
            auto nextBlockBegin = blockBegin + mBlockSize;
            auto innerBlockOffset = 0LL;

            if (mPosition + writeBlockSize > nextBlockBegin)
                writeBlockSize = (mPosition + writeBlockSize) - nextBlockBegin;

            if (mPosition > blockBegin)
                innerBlockOffset = mPosition - blockBegin;

            auto blockMemory = mMemory[blockIndex];
            if (blockMemory == nullptr)
            {
                blockMemory = Memory::Allocate(mBlockSize);
                if (blockMemory == nullptr)
                    throw gcnew IOException("Failed to allocate " + mBlockSize + " bytes of memory");

                mMemory[blockIndex] = blockMemory;
            }

            Memory::Copy(
                bufferPointer, bufferOffset,
                blockMemory, innerBlockOffset,
                writeBlockSize);

            mPosition += writeBlockSize;
            writeCount += (int)writeBlockSize;
        }

        bufferHandle.Free();
    }

    void DynamicMemoryStream::AssertBufferParameters(array<unsigned char> ^buffer, int offset, int count)
    {
        if (buffer->Length <= 0)
            throw gcnew IOException("<buffer.Length> was expected to be greater than zero");

        if (offset < 0)
            throw gcnew IOException("<offset> was expected to be greater than or equal to zero");

        if (count <= 0)
            throw gcnew IOException("<count> was expected to be greater than zero");

        if (buffer->Length - (offset + count) < 0)
            throw gcnew IOException("Buffer does not contain enough data");
    }

} // IO
} // nDiscUtils
