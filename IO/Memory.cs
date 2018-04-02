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
using System.Reflection.Emit;

namespace nDiscUtils.IO
{

    public static unsafe class Memory
    {

        private static ILMethod kMemorySet;
        private static ILMethod kMemoryCopy;

        static Memory()
        {
            kMemorySet = new ILMethod(typeof(void));
            kMemorySet.Write(
                OpCodes.Ldarg_0,
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Initblk,
                OpCodes.Ret);
            kMemorySet.Generate<IntPtr, byte, int>();

            kMemoryCopy = new ILMethod(typeof(void));
            kMemoryCopy.Write(
                OpCodes.Ldarg_0,
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Cpblk,
                OpCodes.Ret);
            kMemoryCopy.Generate<IntPtr, IntPtr, uint>();
        }

        public static void Set(void* buffer, int count, byte b)
        {
            kMemorySet.Run(
                new IntPtr(buffer),
                b,
                count);
        }

        public static void Copy(void* sourceBuffer, long sourceIndex, void* destinationBuffer, long destinationIndex, uint count)
        {
            kMemoryCopy.Run(
                new IntPtr((byte*)sourceBuffer + sourceIndex),
                new IntPtr((byte*)destinationBuffer + destinationIndex),
                count);
        }

    }

}
