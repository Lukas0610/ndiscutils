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
using System.Runtime.InteropServices;

using static nDiscUtils.Runtime.Emit.ILMethods;

namespace nDiscUtils.IO
{

    public static unsafe class Memory
    {

        public static readonly void* NULL = (void*)0;

        public static void* Allocate(int count)
        {
            return Marshal.AllocHGlobal(count).ToPointer();
        }

        public static void Free(void* buffer)
        {
            Marshal.FreeHGlobal(new IntPtr(buffer));
        }

        public static void Set(void* buffer, uint count, byte b)
        {
            initblk.Run(
                new IntPtr(buffer),
                b,
                count);
        }

        public static void Copy(void* sourceBuffer, long sourceIndex, void* destinationBuffer, long destinationIndex, uint count)
        {
            cpblk.Run(
                new IntPtr((byte*)sourceBuffer + sourceIndex),
                new IntPtr((byte*)destinationBuffer + destinationIndex),
                count);
        }

    }

}
