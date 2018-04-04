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
#include <iostream>
#include <cstring>

#include "StreamMode.h"

using namespace System;
using namespace System::IO;

namespace nDiscUtils {
namespace IO {

    public ref class Memory
    {

    public:

        static void* Allocate(size_t count);

        static void Free(void* ptr);

        static void Set(void* ptr, unsigned char data, size_t count);

        static void Set(void* ptr, long long offset, unsigned char data, size_t count);

        static void Copy(void *src, const void *dst, size_t count);

        static void Copy(void *src, long long srcOffset, const void *dst, long long dstOffset, size_t count);

    };

} // IO
} // nDiscUtils
