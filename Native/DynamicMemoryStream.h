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

#include "StreamMode.h"

using namespace System;
using namespace System::IO;

namespace nDiscUtils {
namespace IO {

    public ref class DynamicMemoryStream : Stream
    {

    public:
        DynamicMemoryStream(long long capacity);
        DynamicMemoryStream(long long capacity, int blockSize);
        DynamicMemoryStream(long long capacity, StreamMode streamMode);
        DynamicMemoryStream(long long capacity, int blockSize, StreamMode streamMode);

        property bool CanRead {
            bool get() override {
                return ((mMode & StreamMode::Read) == StreamMode::Read);
            }
        }

        property bool CanWrite {
            bool get() override {
                return ((mMode & StreamMode::Write) == StreamMode::Write);
            }
        }

        property bool CanSeek {
            bool get() override {
                return true;
            }
        }

        property bool CanTimeout {
            bool get() override {
                return false;
            }
        }

        property long long Length
        {
            long long get() override {
                return mCapacity;
            }
        }

        property long long Size
        {
            long long get() {
                return mLength;
            }
        }

        property long long Position
        {
            long long get() override {
                return mPosition;
            }
            void set(long long value) override {
                Seek(value, SeekOrigin::Begin);
            }
        }

        void Flush() override { }

        void SetLength(long long value) override { }

        long long Seek(long long offset, SeekOrigin origin) override;

        int Read(array<unsigned char> ^buffer, int offset, int count) override;

        void Write(array<unsigned char> ^buffer, int offset, int count) override;

    private:
        long long mCapacity;
        int mBlockSize;
        long long mBlockCount;
        StreamMode mMode;
        void** mMemory;
        long long mMemorySize;

        long long mLength;
        long long mPosition;

        void AssertBufferParameters(array<unsigned char> ^buffer, int offset, int count);

    };

} // IO
} // nDiscUtils
