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

#include "DynamicMemoryStream.h"
#include "Memory.h"

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;

using namespace nDiscUtils::IO;

namespace nDiscUtils {
namespace IO {

    DynamicMemoryStream::DynamicMemoryStream(long long capacity) :
        DynamicMemoryStream(capacity, 4096) { }

    DynamicMemoryStream::DynamicMemoryStream(long long capacity, int blockSize) :

#pragma warning(push)
#pragma warning(disable: 4244) // possible loss of data
        mCapacity(capacity),
#pragma warning(pop)

        mBlockSize(blockSize)
    {
        if (capacity != (long long)mCapacity) {
            throw gcnew OverflowException("Detected numeric overflow in capacity");
        }

        AssertRequestedBlockSize();

        mBlockCount = (long)Math::Ceiling((double)mCapacity / mBlockSize);
        mMemorySize = mBlockCount * sizeof(void*);
        mMemory = (void**)Memory::Allocate(mMemorySize);
        Memory::Set(mMemory, 0, mMemorySize);

        mLength = __mem_cast(0);
        mPosition = __mem_cast(0);
    }

    long long DynamicMemoryStream::Seek(long long offset, SeekOrigin origin)
    {
        auto soffs = (size_t)offset;

        switch (origin)
        {
            case SeekOrigin::Begin: mPosition = soffs; break;
            case SeekOrigin::Current: mPosition += soffs; break;
            case SeekOrigin::End: mPosition = mCapacity - soffs; break;
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

    DynamicMemoryStream::~DynamicMemoryStream()
    {
        for (size_t i = 0; i < mBlockCount; i++)
        {
            if (mMemory[i] != nullptr)
            {
                VirtualFree(mMemory[i], 0, MEM_RELEASE);
                mMemory[i] = nullptr;

                mLength -= mBlockSize;
            }
        }
    }

    int DynamicMemoryStream::Read(array<unsigned char> ^buffer, int offset, int count)
    {
        auto readCount = 0;
        
        AssertBufferParameters(buffer, offset, count);

        auto bufferHandle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
        auto bufferPointer = (void*)bufferHandle.AddrOfPinnedObject();

        while (readCount < count)
        {
            auto bufferOffset = offset + readCount;

            auto readBlockSize = (size_t)Math::Min(__mem_cast(count - readCount), __mem_cast(mBlockSize));
            auto blockIndex = (size_t)Math::Floor((double)mPosition / mBlockSize);
            auto blockBegin = (size_t)(blockIndex * mBlockSize);
            auto nextBlockBegin = (size_t)(blockBegin + mBlockSize);
            auto innerBlockOffset = (size_t)0;

            if (mPosition > blockBegin)
                innerBlockOffset = mPosition - blockBegin;

            if (innerBlockOffset + readBlockSize > mBlockSize)
                readBlockSize = mBlockSize - innerBlockOffset;

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

        AssertBufferParameters(buffer, offset, count);

        auto bufferHandle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
        auto bufferPointer = (void*)bufferHandle.AddrOfPinnedObject();

        while (writeCount < count)
        {
            auto bufferOffset = offset + writeCount;

            auto writeBlockSize = (size_t)Math::Min(__mem_cast(count - writeCount), __mem_cast(mBlockSize));
            auto blockIndex = (size_t)Math::Floor((double)mPosition / mBlockSize);
            auto blockBegin = (size_t)(blockIndex * mBlockSize);
            auto nextBlockBegin = (size_t)(blockBegin + mBlockSize);
            auto innerBlockOffset = (size_t)0;

            if (mPosition > blockBegin)
                innerBlockOffset = mPosition - blockBegin;

            if (innerBlockOffset + writeBlockSize > mBlockSize)
                writeBlockSize = mBlockSize - innerBlockOffset;

            auto blockMemory = mMemory[blockIndex];
            if (blockMemory == nullptr)
            {
                blockMemory = VirtualAlloc(nullptr, mBlockSize, MEM_COMMIT, PAGE_READWRITE);
                if (blockMemory == nullptr)
                    throw gcnew IOException("Failed to allocate " + mBlockSize + " bytes of memory");

                mMemory[blockIndex] = blockMemory;
                mLength += mBlockSize;
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

    void DynamicMemoryStream::AssertRequestedBlockSize()
    {
        SYSTEM_INFO info;
        GetSystemInfo(&info);

        if ((mBlockSize % info.dwAllocationGranularity) != 0)
            throw gcnew ArgumentException(String::Format("Requested block-size is not aligned to allocation granularity ({0})",
                info.dwAllocationGranularity));
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

        if (mPosition + count > mCapacity)
            throw gcnew IOException("Operation would exceed memory limits");
    }

} // IO
} // nDiscUtils
