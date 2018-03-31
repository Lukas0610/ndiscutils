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
using nDiscUtils.Options;
using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class MakeImage
    {

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            Logger.Info("Opening image \"{0}\"", opts.Path);
            var imageStream = OpenPath(opts.Path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            if (FormatStream(opts.FileSystem, imageStream, opts.Size, "nDiscUtils Image") == null)
                return INVALID_ARGUMENT;

            Cleanup(imageStream);
            return SUCCESS;
        }

        [Verb("mkimg", HelpText = "Create a file system image")]
        public sealed class Options : BaseOptions
        {

            [Value(0, Default = null, HelpText = "Path to the image or the disk to which the image will be written to", Required = true)]
            public string Path { get; set; }

            [Value(1, Default = "256MB", HelpText = "(Maximal) size of the newly created image", Required = true)]
            public string SizeString { get; set; }

            public int Size
            {
                get => ParseSizeString(SizeString);
            }

            [Option('f', "fs", Default = "NTFS", HelpText = "Type of the filesystem the new image should be formatted with")]
            public string FileSystem { get; set; }

        }

    }

}
