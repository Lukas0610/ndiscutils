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

#include "StaticMemoryStream.h"
#include "Memory.h"
#include "StreamMode.h"

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;

using namespace nDiscUtils::IO;

namespace nDiscUtils {
namespace IO {

    StaticMemoryStream::StaticMemoryStream(long long capacity) :

#pragma warning(push)
#pragma warning(disable: 4244) // possible loss of data
        mCapacity(capacity)
#pragma warning(pop)

    {
        if (capacity != (long long)mCapacity) {
            throw gcnew OverflowException("Detected numeric overflow in capacity");
        }

        AssertRequestedCapacity();

        mMemory = nullptr;
        mPosition = 0;

        mMemory = GlobalAlloc(GHND, mCapacity);
        if (mMemory == nullptr || GlobalSize(mMemory) != mCapacity)
            throw gcnew IOException(String::Format("Failed to allocate {0} bytes of memory", mCapacity), GetLastError());
    }

    StaticMemoryStream::~StaticMemoryStream()
    {
        GlobalFree(mMemory);
    }

    long long StaticMemoryStream::Seek(long long offset, SeekOrigin origin)
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

    int StaticMemoryStream::Read(array<unsigned char> ^buffer, int offset, int count)
    {
        auto readCount = 0;
        
        AssertBufferParameters(buffer, offset, count);

        auto bufferHandle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
        auto bufferPointer = (void*)bufferHandle.AddrOfPinnedObject();

        auto memoryPointer = GlobalLock(mMemory);

        auto actualCount = (int)Math::Min((int)(mCapacity - (mPosition + count)), count);

        Memory::Copy(memoryPointer, mPosition, bufferPointer, offset, actualCount);

        mPosition += actualCount;
        readCount = actualCount;

        GlobalUnlock(mMemory);
        auto error = GetLastError();
        if (error != 0)
            throw gcnew IOException("Failed to unlock memory object", error);

        bufferHandle.Free();
        return readCount;
    }

    void StaticMemoryStream::Write(array<unsigned char> ^buffer, int offset, int count)
    {
        AssertBufferParameters(buffer, offset, count);

        auto bufferHandle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
        auto bufferPointer = (void*)bufferHandle.AddrOfPinnedObject();

        auto memoryPointer = GlobalLock(mMemory);

        auto actualCount = (int)Math::Min((int)(mCapacity - (mPosition + count)), count);

        Memory::Copy(bufferPointer, offset, memoryPointer, mPosition, actualCount);

        mPosition += actualCount;

        GlobalUnlock(mMemory);
        auto error = GetLastError();
        if (error != 0)
            throw gcnew IOException("Failed to unlock memory object", error);

        bufferHandle.Free();
    }

    void StaticMemoryStream::AssertRequestedCapacity()
    {
        SYSTEM_INFO info;
        GetSystemInfo(&info);

        if ((mCapacity & info.dwAllocationGranularity) != 0)
            throw gcnew ArgumentException(String::Format("Requested capacity is not aligned to allocation granularity ({0})",
                info.dwAllocationGranularity));
    }

    void StaticMemoryStream::AssertBufferParameters(array<unsigned char> ^buffer, int offset, int count)
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
