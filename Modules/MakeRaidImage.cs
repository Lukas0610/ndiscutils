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
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CommandLine;

using nDiscUtils.Core;
using nDiscUtils.IO;
using nDiscUtils.IO.SoftRaid;
using nDiscUtils.Options;

using static nDiscUtils.Core.ModuleHelpers;
using static nDiscUtils.Core.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class MakeRaidImage
    {

        public static readonly Dictionary<string, string> kRaidTypes = new Dictionary<string, string>
        {
            { "0", "0" },
            { "striping", "0" },
            { "1", "1" },
            { "mirroring", "1" },
        };

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            if (opts.Paths.Count() < 2)
            {
                Logger.Error("A RAID with only one disk sounds a bit useless, doesn't it?");
                return INVALID_ARGUMENT;
            }

            if (!kRaidTypes.Keys.Any(t => t.Equals(opts.RaidType, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Error("Could not determined RAID type of \"{0}\"", opts.RaidType);
                Logger.Error("Supported RAID types are: {0}", string.Join(", ", kRaidTypes.Keys));
                return INVALID_ARGUMENT;
            }

            var raidType = kRaidTypes[opts.RaidType.ToLower()];
            AbstractSoftRaidStream raidStream = null;

            var rawOffsets = opts.Offsets;
            var offsets = new List<long>();
            foreach (var rawOffset in opts.Offsets)
            {
                offsets.Add(ParseSizeString(rawOffset));
            }

            if (raidType == "0")
            {
                raidStream = new SoftRaid0Stream();
            }
            else if (raidType == "1")
            {
                raidStream = new SoftRaid1Stream();
            }
            else
            {
                Logger.Error("RAID-type {0} is not implemented yet", raidType );
                return NOT_IMPLEMENTED;
            }

            raidStream.StripeSize = opts.StripeSize;

            var paths = new List<string>(opts.Paths);

            for (int i = 0; i < paths.Count; i++)
            {
                var pathStream = OpenPathUncached(paths[i], FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                var offset = 0L;

                if (offsets.Count > 0)
                {
                    if (i < offsets.Count)
                        offset = offsets[i];
                    else
                        offset = offsets[offsets.Count - 1];
                }

                if (offset > 0)
                    pathStream = new OffsetableStream(pathStream, offset);

                raidStream.AddStream(pathStream);
            }

            raidStream.Open();
            raidStream.SetLength(opts.Size);

            if (FormatStream(opts.FileSystem, raidStream, opts.Size, "nDiscUtils Image") == null)
                return INVALID_ARGUMENT;

            Cleanup(raidStream);
            WaitForUserExit();
            return SUCCESS;
        }

        [Verb("mkraidimg", HelpText = "Create a file system image")]
        public sealed class Options : BaseOptions
        {

            [Value(0, Default = "256M", HelpText = "(Maximal) size of the newly created image", Required = true)]
            public string SizeString { get; set; }

            public long Size
            {
                get => ParseSizeString(SizeString);
            }

            [Value(1, Default = "0", HelpText = "Type of the software-RAID to create.", Required = true)]
            public string RaidType { get; set; }

            [Value(2, Default = null, HelpText = "Paths of the new software-RAID images, volumes or disks", Required = true, Min = 1)]
            public IEnumerable<string> Paths { get; set; }

            [Option('f', "fs", Default = "NTFS", HelpText = "Type of the filesystem the new image should be formatted with", Required = false)]
            public string FileSystem { get; set; }

            [Option('s', "stripe-size", Default = "64K", HelpText = "Size of the single data stripes. Only used on certain variants.", Required = false)]
            public string StripeSizeString { get; set; }

            public long StripeSize
            {
                get => ParseSizeString(StripeSizeString);
            }

            [Option('o', "offset", Default = new string[] { }, 
                HelpText = "Offset in bytes at which the image will be written to the target. Comma-separated for multiple values.", Separator = ',')]
            public IEnumerable<string> Offsets { get; set; }

        }

    }

}
