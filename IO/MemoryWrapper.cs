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

namespace nDiscUtils.IO
{

    public static unsafe class MemoryWrapper
    {

        public static void* Allocate(long count)
        {
#if __x64__
            return Memory.Allocate((ulong)count);
#elif __x86__
            return Memory.Allocate((uint)count);
#endif
        }

        public static void Copy(byte[] src, byte[] dst, long count)
        {
            fixed (void* srcptr = src)
            fixed (void* dstptr = dst)
#if __x64__
                Memory.Copy(srcptr, dstptr, (ulong)count);
#elif __x86__
                Memory.Copy(srcptr, dstptr, (uint)count);
#endif
        }

        public static void Copy(void* src, void* dst, long count)
        {
#if __x64__
            Memory.Copy(src, dst, (ulong)count);
#elif __x86__
            Memory.Copy(src, dst, (uint)count);
#endif
        }

        public static void Copy(byte[] src, long srcOffset, byte[] dst, long dstOffset, long count)
        {
            fixed (void* srcptr = src)
            fixed (void* dstptr = dst)
#if __x64__
                Memory.Copy(srcptr, srcOffset, dstptr, dstOffset, (ulong)count);
#elif __x86__
                Memory.Copy(srcptr, srcOffset, dstptr, dstOffset, (uint)count);
#endif
        }

        public static void Copy(void* src, long srcOffset, void* dst, long dstOffset, long count)
        {
#if __x64__
            Memory.Copy(src, srcOffset, dst, dstOffset, (ulong)count);
#elif __x86__
            Memory.Copy(src, srcOffset, dst, dstOffset, (uint)count);
#endif
        }

        public static void Free(byte[] buffer)
        {
            fixed (void* bufferptr = buffer)
                Memory.Free(bufferptr);
        }

        public static void Free(void* ptr)
        {
            Memory.Free(ptr);
        }

        public static void Set(byte[] buffer, byte data, long count)
        {
            fixed (void* bufferptr = buffer)
#if __x64__
                Memory.Set(bufferptr, data, (ulong)count);
#elif __x86__
                Memory.Set(bufferptr, data, (uint)count);
#endif
        }

        public static void Set(void* ptr, byte data, long count)
        {
#if __x64__
            Memory.Set(ptr, data, (ulong)count);
#elif __x86__
            Memory.Set(ptr, data, (uint)count);
#endif
        }

    }

}
