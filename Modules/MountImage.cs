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
using System.IO;

using CommandLine;

using nDiscUtils.Core;
using nDiscUtils.IO;
using nDiscUtils.Options;

using static nDiscUtils.Core.ModuleHelpers;
using static nDiscUtils.Core.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class MountImage
    {

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            Logger.Info("Opening image \"{0}\"", opts.Path);
            var imageStream = OpenPath(opts.Path,
                FileMode.Open,
                FileAccess.Read | (opts.ReadOnly ? 0 : FileAccess.Write),
                FileShare.None);

            if (imageStream == null)
            {
                Logger.Error("Failed to open image/access disk!");
                WaitForUserExit();
                return INVALID_ARGUMENT;
            }

            if (opts.Offset > 0)
                imageStream = new OffsetableStream(imageStream, opts.Offset);

            if (imageStream is FixedLengthStream fixedStream)
            {
                var geometry = FindGeometry(fixedStream);
                fixedStream.SetLength(geometry.TotalSectorsLong * geometry.BytesPerSector);
            }

            MountStream(imageStream, opts);

            Cleanup(imageStream);
            WaitForUserExit();
            return SUCCESS;
        }

        [Verb("mntimg", HelpText = "Mounts the file system located in an image")]
        public sealed class Options : BaseMountOptions
        {

            [Value(1, Default = null, HelpText = "Path to the image or the disk which should be mounted", Required = true)]
            public string Path { get; set; }

            [Option('o', "offset", Default = "0", HelpText = "Offset in bytes at which the image is expected to start")]
            public string OffsetString { get; set; }

            public long Offset
            {
                get => ParseSizeString(OffsetString);
            }

        }

    }

}
