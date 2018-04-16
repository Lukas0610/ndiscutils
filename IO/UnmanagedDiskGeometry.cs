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

using DiscUtils;

using Microsoft.Win32.SafeHandles;

using static nDiscUtils.Core.NativeMethods;

namespace nDiscUtils.IO
{

    public sealed class UnmanagedDiskGeometry
    {

        public static Geometry GetDiscUtilsGeometry(SafeFileHandle diskHandle)
        {
            var rawGeometry = new byte[24];
            var rawGeometryPtr = IntPtr.Zero;

            unsafe
            {
                fixed (byte* p = rawGeometry)
                    rawGeometryPtr = (IntPtr)p;
            }

            var ioctlFlag =
                DeviceIoControl(
                    diskHandle,
                    IOCTL_DISK_GET_DRIVE_GEOMETRY,
                    IntPtr.Zero,
                    0,
                    rawGeometryPtr,
                    (uint)rawGeometry.Length,
                    out var dummy1,
                    IntPtr.Zero
                );

            if (!ioctlFlag)
                return null;

            var cylinders = BitConverter.ToInt64(rawGeometry, 0);
            // 8+4 -> MediaType
            var tracksPerCylinder = BitConverter.ToInt32(rawGeometry, 12);
            var sectorsPerTrack = BitConverter.ToInt32(rawGeometry, 14);
            var bytesPerSector = BitConverter.ToInt32(rawGeometry, 14);

            var capacity = cylinders
                * tracksPerCylinder
                * sectorsPerTrack
                * bytesPerSector;

            return new Geometry(capacity, tracksPerCylinder, sectorsPerTrack, bytesPerSector);
        }

    }

}
