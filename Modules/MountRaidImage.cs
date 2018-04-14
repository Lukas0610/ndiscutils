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

    public static class MountRaidImage
    {

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            if (opts.Paths.Count() < 2)
            {
                Logger.Error("A RAID with only one disk sounds a bit useless, doesn't it?");
                return INVALID_ARGUMENT;
            }

            if (!MakeRaidImage.kRaidTypes.Keys.Any(t => t.Equals(opts.RaidType, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Error("Could not determined RAID type of \"{0}\"", opts.RaidType);
                Logger.Error("Supported RAID types are: {0}", string.Join(", ", MakeRaidImage.kRaidTypes.Keys));
                return INVALID_ARGUMENT;
            }

            var raidType = MakeRaidImage.kRaidTypes[opts.RaidType.ToLower()];
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
                Logger.Error("RAID-type {0} is not implemented yet", raidType);
                return NOT_IMPLEMENTED;
            }

            raidStream.StripeSize = opts.StripeSize;

            var paths = new List<string>(opts.Paths);

            for (int i = 0; i < paths.Count; i++)
            {
                var pathStream = OpenPathUncached(paths[i], FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
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

                if (pathStream is FixedLengthStream fixedStream)
                {
                    var geometry = FindGeometry(fixedStream);
                    fixedStream.SetLength(geometry.TotalSectorsLong * geometry.BytesPerSector);
                }

                raidStream.AddStream(pathStream);
            }

            raidStream.Open();
            raidStream.InvalidateLength();

            MountStream(raidStream, opts);

            Cleanup(raidStream);
            WaitForUserExit();
            return SUCCESS;
        }

        [Verb("mntraidimg", HelpText = "Mounts the file system located in an image")]
        public sealed class Options : BaseMountOptions
        {

            [Value(1, Default = "0", HelpText = "Type of the software-RAID to create.", Required = true)]
            public string RaidType { get; set; }

            [Value(2, Default = null, HelpText = "Paths of the new software-RAID images, volumes or disks", Required = true, Min = 1)]
            public IEnumerable<string> Paths { get; set; }

            [Option('s', "stripe-size", Default = "64K", HelpText = "Size of the single data stripes. Only used on certain variants.", Required = false)]
            public string StripeSizeString { get; set; }

            public int StripeSize
            {
                get => (int)ParseSizeString(StripeSizeString);
            }

            [Option('o', "offset", Default = new string[] { },
                HelpText = "Offset in bytes at which the image will be written to the target. Comma-separated for multiple values.", Separator = ',')]
            public IEnumerable<string> Offsets { get; set; }

        }

    }

}
