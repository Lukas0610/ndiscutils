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
using System.Text;

using CommandLine;

using nDiscUtils.IO;
using nDiscUtils.Options;

using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class MountPartition
    {

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            if (opts.Partition < 0)
            {
                Logger.Error("Invalid partition index (Expected number greater than 0)");
                WaitForUserExit();
                return INVALID_ARGUMENT;
            }

            Logger.Info("Opening image \"{0}\"", opts.Path);
            var imageStream = OpenPath(opts.Path,
                FileMode.Open,
                FileAccess.Read | (opts.ReadOnly ? 0 : FileAccess.Write),
                FileShare.None);

            if (imageStream == null)
            {
                Logger.Error("Failed to open image!");
                WaitForUserExit();
                return INVALID_ARGUMENT;
            }

            var partitionTable = FindPartitionTable(imageStream);

            if (opts.Partition >= partitionTable.Count)
            {
                Logger.Error("Invalid partition index (Expected number lower than than {0})", partitionTable.Count);
                WaitForUserExit();
                return INVALID_ARGUMENT;
            }

            var partition = partitionTable[opts.Partition];
            {
                var type = new StringBuilder();

                if (partition.GuidType == null || partition.GuidType == Guid.Empty)
                    type.AppendFormat("0x{0:X2}", partition.BiosType);
                else
                    type.AppendFormat("{0}", partition.GuidType);

                type.AppendFormat(" ({0})", partition.TypeAsString);

                Logger.Info("Partition: {0}; 0x{1:X}-0x{2:X}", type, partition.FirstSector, partition.LastSector);
            }

            var geometry = FindGeometry(imageStream);
            var size = partition.SectorCount * geometry.BytesPerSector;

            if (imageStream is FixedLengthStream fixedStream)
                fixedStream.SetLength(geometry.TotalSectorsLong * geometry.BytesPerSector);

            using (var partitionStream = new FixedLengthStream(partition.Open(), size))
                MountStream(partitionStream, opts);

            Cleanup(imageStream);
            WaitForUserExit();
            return SUCCESS;
        }

        [Verb("mntpart", HelpText = "Mounts a partition located in an image or on a disk")]
        public sealed class Options : BaseMountOptions
        {

            [Value(1, Default = null, HelpText = "Path to the image or the disk which contains the partition to be mounted", Required = true)]
            public string Path { get; set; }

            [Value(2, HelpText = "Index of the partition which should be mounted", Required = true)]
            public int Partition { get; set; }

        }

    }

}
