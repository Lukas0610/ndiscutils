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
using System.IO;

using CommandLine;

using nDiscUtils.IO;
using nDiscUtils.Options;

using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class Ramdisk
    {

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            if (opts.ReadOnly)
            {
                Logger.Error("Are you sure you want to mount a ramdisk as read-only?");
                return INVALID_ARGUMENT;
            }

            if ((opts.Size % opts.BlockSize) != 0)
            {
                Logger.Error("Requested capacity is not aligned to block size ({0})", opts.BlockSize);
                return INVALID_ARGUMENT;
            }

            Logger.Info("Creating memory stream with size 0x{0:X}", opts.Size);
            Stream memoryStream = null;

            if (opts.MemoryFull)
                memoryStream = new StaticMemoryStream(opts.Size);
            else
                memoryStream = new DynamicMemoryStream(opts.Size, opts.BlockSize);

            if (FormatStream(opts.FileSystem, memoryStream, opts.Size, "nDiscUtils Ramdisk") == null)
                return INVALID_ARGUMENT;

            if (opts.FileSystem == "FAT")
            {
                Logger.Warn("*************************************************");
                Logger.Warn("**                                             **");
                Logger.Warn("**                W A R N I N G                **");
                Logger.Warn("**                                             **");
                Logger.Warn("**     FAT FILES CAN ONLY BE ACCESSED WITH     **");
                Logger.Warn("**                                             **");
                Logger.Warn("**               SHORT FILE NAME               **");
                Logger.Warn("**                                             **");
                Logger.Warn("*************************************************");
            }

            MountStream(memoryStream, opts);

            Cleanup(memoryStream);
            return SUCCESS;
        }

        [Verb("ramdisk", HelpText = "Create a memory-located mount point")]
        public sealed class Options : BaseMountOptions
        {

            [Value(1, Default = "256M", HelpText = "Size of the newly created ramdisk", Required = true)]
            public string SizeString { get; set; }

            public long Size
            {
                get => ParseSizeString(SizeString);
            }

            [Option('b', "block-size", Default = "64K", HelpText = "Size of each block in the internal memory allocation")]
            public string BlockSizeString { get; set; }

            public int BlockSize
            {
                get => (int)ParseSizeString(BlockSizeString);
            }

            [Option('f', "fs", Default = "NTFS", HelpText = "Type of the filesystem the ramdisk should be formatted with")]
            public string FileSystem { get; set; }

            [Option('m', "memory-full", Default = false, HelpText = "Allocate the full memory region at once")]
            public bool MemoryFull { get; set; }

        }

    }

}
