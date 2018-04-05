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

#include "Memory.h"

using namespace System;
using namespace System::IO;

namespace nDiscUtils {
namespace IO {

    void* Memory::Allocate(size_t count)
    {
        return std::malloc(count);
    }

    void Memory::Free(void* ptr)
    {
        return free(ptr);
    }

    void Memory::Set(void* ptr, unsigned char data, size_t count)
    {
        Set(ptr, 0, data, count);
    }

    void Memory::Set(void* ptr, long long offset, unsigned char data, size_t count)
    {
        std::memset(((unsigned char*)ptr) + offset, data, count);
    }

    void Memory::Copy(void *src, const void *dst, size_t count)
    {
        Copy(src, (long long)0, dst, (long long)0, count);
    }

    void Memory::Copy(void *src, long long srcOffset, const void *dst, long long dstOffset, size_t count)
    {
        memcpy(
            ((unsigned char*)dst) + dstOffset,
            ((unsigned char*)src) + srcOffset,
            count);
    }

} // IO
} // nDiscUtils
