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
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace nDiscUtils
{

    public static class NativeMethods
    {

        public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
        public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;

        public const uint FSCTL_ALLOW_EXTENDED_DASD_IO = 0x00090083;
        public const uint FSCTL_LOCK_VOLUME = 0x00090018;
        public const uint FSCTL_UNLOCK_VOLUME = 0x0009001C;

        public const uint IOCTL_STORAGE_PREDICT_FAILURE = 0x002D1100;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
            [MarshalAs(UnmanagedType.LPTStr)]
            string filename,

            [MarshalAs(UnmanagedType.U4)]
            FileAccess access,

            [MarshalAs(UnmanagedType.U4)]
            FileShare share,

            IntPtr securityAttributes,

            [MarshalAs(UnmanagedType.U4)]
            FileMode creationDisposition,

            [MarshalAs(UnmanagedType.U4)]
            uint flagsAndAttributes,

            IntPtr templateFile
        );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,

            uint dwIoControlCode,

            IntPtr lpInBuffer,

            uint nInBufferSize,

            IntPtr lpOutBuffer,

            uint nOutBufferSize,

            out uint lpBytesReturned,

            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory")]
        public unsafe static extern bool ZeroMemory(void* destination, int length);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "CopyMemory")]
        public unsafe static extern void CopyMemory(void* destination, void* source, uint length);

    }

}
