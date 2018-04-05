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
#pragma once

#include "stdafx.h"

using namespace System;
using namespace System::IO;

namespace nDiscUtils {
    namespace IO {

        public ref class StaticMemoryStream : Stream, IDisposable
        {

        public:
            StaticMemoryStream(long long capacity);

            ~StaticMemoryStream();

            property bool CanRead
            {
                bool get() override
                {
                    return true;
                }
            }

            property bool CanWrite
            {
                bool get() override
                {
                    return true;
                }
            }

            property bool CanSeek
            {
                bool get() override
                {
                    return true;
                }
            }

            property bool CanTimeout
            {
                bool get() override
                {
                    return false;
                }
            }

            property long long Length
            {
                long long get() override
                {
                    return mCapacity;
                }
            }

            property long long Position
            {
                long long get() override
                {
                    return mPosition;
                }
                void set(long long value) override
                {
                    Seek(value, SeekOrigin::Begin);
                }
            }

            void Flush() override { }

            void SetLength(long long value) override { }

            long long Seek(long long offset, SeekOrigin origin) override;

            int Read(array<unsigned char> ^buffer, int offset, int count) override;

            void Write(array<unsigned char> ^buffer, int offset, int count) override;

        private:
            size_t mCapacity;
            HGLOBAL mMemory;

            size_t mPosition;

        };

    } // IO
} // nDiscUtils
