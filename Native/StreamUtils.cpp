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

#include "StreamUtils.h"

using namespace System;
using namespace System::IO;

namespace nDiscUtils {
namespace IO {

    void StreamUtils::AssertBufferParameters(size_t capacity, size_t position, array<unsigned char> ^buffer, int offset, int count)
    {
        if (buffer->Length <= 0)
            throw gcnew IOException("<buffer.Length> was expected to be greater than zero");

        if (offset < 0)
            throw gcnew IOException("<offset> was expected to be greater than or equal to zero");

        if (count <= 0)
            throw gcnew IOException("<count> was expected to be greater than zero");

        if (buffer->Length - (offset + count) < 0)
            throw gcnew IOException("Buffer does not contain enough data");

        if (position + count > capacity)
            throw gcnew IOException("Operation would exceed memory limits");
    }

    bool StreamUtils::IsAllocationAligned(size_t value)
    {
        SYSTEM_INFO info;
        GetSystemInfo(&info);
        return ((value % info.dwAllocationGranularity) == 0);
    }

    void StreamUtils::IsAllocationAlignedStrict(size_t value, const char *description)
    {
        SYSTEM_INFO info;
        GetSystemInfo(&info);

        if ((value % info.dwAllocationGranularity) != 0)
            throw gcnew ArgumentException(String::Format("{0} is not aligned to allocation granularity ({1})",
                gcnew String(description), info.dwAllocationGranularity));
    }

} // IO
} // nDiscUtils
