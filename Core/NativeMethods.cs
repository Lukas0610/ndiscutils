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

namespace nDiscUtils.Core
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
        public const uint IOCTL_DISK_GET_DRIVE_GEOMETRY = 0x00070000;

        public const int CTRL_C_EVENT = 0;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true,
            BestFitMapping = true, ThrowOnUnmappableChar = true)]
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

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool FreeConsole();

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("kernel32.dll")]
        public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern int ResumeThread(IntPtr hThread);

        // https://stackoverflow.com/a/71457
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

    }

}
