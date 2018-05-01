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
using System.Linq;
using CommandLine;
using nDiscUtils.Core;
using nDiscUtils.Options;

using static nDiscUtils.Core.ModuleHelpers;
using static nDiscUtils.Core.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class PhysicalCheck
    {

        // DO NOT CHANGE THIS!!!
        private const int BLOCK_SIZE = 1024;

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            Logger.Info("Opening image \"{0}\"", opts.Path);
            Stream imageStream = null;
            
            if (opts.Write)
                imageStream = OpenPathUncached(opts.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            else
                imageStream = OpenPath(opts.Path, FileMode.Open, FileAccess.Read, FileShare.None);

            if (imageStream == null)
            {
                Logger.Error("Failed to open image!");
                WaitForUserExit();
                return INVALID_ARGUMENT;
            }

            var buffer = new byte[opts.BlockCount * BLOCK_SIZE];
            var lastPositionReport = DateTime.Now;

            Logger.Info("Starting physical disk check of {0} (0x{1:X})", FormatBytes(imageStream.Length, 3), imageStream.Length);
            while (imageStream.Position < imageStream.Length)
            {
                var initialPosition = imageStream.Position;

                try
                {
                    var read = imageStream.Read(buffer, 0, buffer.Length);

                    if (opts.Write)
                    {
                        var invertedBuffer = buffer
                            .Take(read)
                            .Select(v => (byte)~v)
                            .ToArray();

                        imageStream.Seek(initialPosition, SeekOrigin.Begin);
                        imageStream.Write(invertedBuffer, 0, read);

                        imageStream.Seek(initialPosition, SeekOrigin.Begin);
                        imageStream.Write(buffer, 0, read);
                    }
                }
                catch (IOException ex)
                {
                    Logger.Error("I/O-failure at offset 0x{0:X} ({1}): {2}", imageStream.Position, FormatBytes(imageStream.Position, 3), ex.Message);
                    imageStream.Seek(initialPosition + buffer.Length, SeekOrigin.Begin);
                }

                var now = DateTime.Now;
                if (now.Subtract(lastPositionReport).TotalSeconds >= opts.ReportInterval)
                {
                    Logger.Info("Currently checking at offset 0x{0:X} ({1}): {2}%", imageStream.Position, FormatBytes(imageStream.Position, 3), 
                        Math.Round(((double)imageStream.Position / imageStream.Length) * 100.0, 3));
                    lastPositionReport = now;
                }
            }

            Cleanup(imageStream);
            WaitForUserExit();
            return SUCCESS;
        }

        [Verb("physcheck", HelpText = "Performs a simple physical surface check by reading through the disk")]
        public sealed class Options : BaseOptions
        {

            [Value(0, Default = null, HelpText = "Path to the image or the disk which should be used", Required = true)]
            public string Path { get; set; }

            [Option('b', "block-count", Default = 64, HelpText = "Count of 1K-blocks to be read in one process", Required = false)]
            public int BlockCount { get; set; }

            [Option('r', "report-interval", Default = 10, HelpText = "Interval in seconds after which the task reports the current position", Required = false)]
            public int ReportInterval { get; set; }

            [Option('w', "write", Default = false, HelpText = "Checks the writing functionality of the targeted disk", Required = false)]
            public bool Write { get; set; }

        }

    }

}
