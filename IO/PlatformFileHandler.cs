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
using System.Management;
using System.Runtime.InteropServices;
using static nDiscUtils.NativeMethods;

namespace nDiscUtils.IO
{

    /// <summary>
    /// Manages all I/O-operations which require specific handling on different platforms
    /// </summary>
    public static class PlatformFileHandler
    {
        
        public static Stream OpenDisk(string diskPath,
            FileAccess access, FileShare share)
        {
            uint diskAttributes = 0;

            diskAttributes |= FILE_FLAG_NO_BUFFERING;

            // Open disk
            var handle = CreateFile(diskPath, access, share, IntPtr.Zero,
                    FileMode.Open, diskAttributes, IntPtr.Zero);

            var error = Marshal.GetLastWin32Error();
            Logger.Info("CreateFile({0}) returned with {1} (hwnd 0x{2:X})",
                diskPath, error, handle.DangerousGetHandle());

            if (error != 0)
                throw new IOException("Failed to call CreateFile() on " + diskPath + ": " + 
                    "Error " + error);
            
            // Lock disk
            if (!DeviceIoControl(handle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0,
                out var dummy, IntPtr.Zero))
            {
                throw new IOException("Failed to call DeviceIoControl(FSCTL_LOCK_VOLUME) on "
                    + diskPath + ": Error " + error);
            }

            error = Marshal.GetLastWin32Error();
            Logger.Info("DeviceIoControl(0x{0:X}, FSCTL_LOCK_VOLUME) returned with {1}",
                handle.DangerousGetHandle(), error);

            if (error != 0)
                throw new IOException("Failed to call DeviceIoControl(FSCTL_LOCK_VOLUME) on "
                    + diskPath + ": Error " + error);

            // fetch size Windows presents
            var managementObjectSearcher = new ManagementObjectSearcher("SELECT DeviceID, Size FROM Win32_DiskDrive");
            var managementObjectCollection = managementObjectSearcher.Get();
            ManagementBaseObject wmiDiskInfo = null;

            foreach (var managementObject in managementObjectCollection)
            {
                if ((managementObject["DeviceID"] as string) == diskPath)
                {
                    wmiDiskInfo = managementObject;
                    break;
                }
            }

            if (wmiDiskInfo == null)
                throw new InvalidDataException("Failed to find any disk with the ID \"" + diskPath + "\"");

            var size = wmiDiskInfo["Size"] as ulong?;
            if (!size.HasValue)
                throw new InvalidDataException("Failed to fetch size for " + diskPath);

            Logger.Info("Fetched size for {0}: {1} Bytes",
                diskPath, size);

            return new FixedLengthStream(handle, access, (long)size.Value);
        }

    }
    
}
