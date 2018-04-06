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

using DiscUtils;
using DiscUtils.Ntfs;

using nDiscUtils.Core;

namespace nDiscUtils.IO
{

    public static class DiscHandler
    {
        
        public static bool IsStreamSupportedFileSystem(Stream stream)
        {
            if (NtfsFileSystem.Detect(stream))
            {
                Logger.Verbose("Detected stream as <NTFS> file system");
                return true;
            }

            /* if (FatFileSystem.Detect(stream))
            {
                Logger.Verbose("Detected stream as <FAT> file-system");
                return true;
            } */

            // only NTFS can be detected and formatted right now
            return false;
        }

        public static DiscFileSystem GetFileSystem(Stream stream)
        {
            if (NtfsFileSystem.Detect(stream))
            {
                Logger.Verbose("Loading stream as <NTFS> file system");
                return new NtfsFileSystem(stream);
            }

            /* if (FatFileSystem.Detect(stream))
            {
                Logger.Verbose("Loading stream as <FAT> file system");
                return new FatFileSystem(stream);
            } */

            return null;
        }

        public static DiscFileSystem FormatFileSystemWithTemplate(Stream template, Stream output,
            long firstSector, long sectorCount, long availableSpace)
        {
            if (NtfsFileSystem.Detect(template))
            {
                var templateFS = new NtfsFileSystem(template);
                var newSize = Math.Min(templateFS.Size, availableSpace);

                Logger.Verbose("Formatting stream as <NTFS> file system ({0}B, 0x{1}-0x{2})",
                    newSize, firstSector.ToString("X2"), sectorCount.ToString("X2"));

                return NtfsFileSystem.Format(output, templateFS.VolumeLabel,
                    Geometry.FromCapacity(newSize), firstSector, sectorCount,
                    templateFS.ReadBootCode());
            }
            /* else if (FatFileSystem.Detect(template))
            {
                var templateFS = new FatFileSystem(template);
                var newSize = Math.Min(templateFS.Size, availableSpace);

                Logger.Verbose("Formatting streams as <FAT> ({0}B, 0x{1}-0x{2})",
                    newSize, firstSector.ToString("X2"), sectorCount.ToString("X2"));

                return FatFileSystem.FormatPartition(output, templateFS.VolumeLabel,
                    Geometry.FromCapacity(newSize), (int)firstSector, (int)sectorCount, 13);
            } */

            return null;
        }

    }

}
