﻿/*
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
using CommandLine;
using DiscUtils;
using DiscUtils.Fat;
using DiscUtils.Ntfs;
using nDiscUtils.Options;
using static nDiscUtils.ModuleHelpers;

namespace nDiscUtils.Modules
{

    public static class Ramdisk
    {

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            if (opts.ReadOnly)
            {
                Logger.Error("Are you sure you want to mount a ramdisk as read-only?",
                    opts.FileSystem);
                Environment.Exit(1);
                return 1;
            }

            Logger.Info("Creating memory stream with size 0x{0:X}", opts.Size);
            var memoryStream = new MemoryStream(opts.Size);

            if (FormatStream(opts.FileSystem, memoryStream, opts.Size, "nDiscUtils Ramdisk") == null)
                return 1;

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
            return 0;
        }

        [Verb("ramdisk", HelpText = "Create a memory-located mount point")]
        public sealed class Options : BaseMountOptions
        {
            
            [Value(1, Default = "256MB", HelpText = "Size of the newly created ramdisk", Required = true)]
            public string SizeString { get; set; }

            public int Size
            {
                get => ParseSizeString(SizeString);
            }

            [Option('f', "fs", Default = "NTFS", HelpText = "Type of the filesystem the ramdisk should be formatted with")]
            public string FileSystem { get; set; }

        }

    }

}
