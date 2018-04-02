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

using nDiscUtils.Options;

using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class ListPartitions
    {

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            if (!File.Exists(opts.Path))
            {
                Logger.Info("Could not find image \"{0}\"", opts.Path);
                return INVALID_ARGUMENT;
            }

            Logger.Info("Opening image \"{0}\"", opts.Path);
            var imageStream = OpenPath(opts.Path, FileMode.Open, FileAccess.Read, FileShare.None);
            if (imageStream == null)
                return INVALID_ARGUMENT;

            var partitionTable = FindPartitionTable(imageStream);

            var seperator = string.Format("{0,-4}==={1,-64}==={2,-20}==={3,-20}",
                new string('=', 4), new string('=', 64), new string('=', 20), new string('=', 20));

            Logger.Info("Detected partitions:      {0}", partitionTable.Count);
            Logger.Info(seperator);
            Logger.Info("{0,-4} | {1,-64} | {2,-20} | {3,-20}",
                "ID", "Type", "Start", "End");
            Logger.Info(seperator);

            for (int i = 0; i < partitionTable.Count; i++)
            {
                var partition = partitionTable[i];
                var type = new StringBuilder();

                if (partition.GuidType == null || partition.GuidType == Guid.Empty)
                    type.AppendFormat("0x{0:X2}", partition.BiosType);
                else
                    type.AppendFormat("{0}", partition.GuidType);

                type.AppendFormat(" ({0})", partition.TypeAsString);

                Logger.Info("{0,-4} | {1,-64} | 0x{2,-18:X} | 0x{3,-18:X}",
                    i, type, partition.FirstSector, partition.LastSector);
            }

            Cleanup(imageStream);
            return SUCCESS;
        }

        [Verb("lspart", HelpText = "Lists the partitions located in an image or on a disk")]
        public sealed class Options : BaseOptions
        {

            [Value(1, Default = null, HelpText = "Path to the image or the disk which should be used", Required = true)]
            public string Path { get; set; }

        }

    }

}
