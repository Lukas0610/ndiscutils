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
using System.Threading.Tasks;
using CommandLine;
using DiscUtils;
using DiscUtils.Ext;
using DiscUtils.Fat;
using DiscUtils.Ntfs;
using nDiscUtils.Core;
using nDiscUtils.IO;
using nDiscUtils.Options;

using static nDiscUtils.Core.ModuleHelpers;
using static nDiscUtils.Core.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class DeepScan
    {

        // DO NOT CHANGE THIS!!!
        private const int BLOCK_SIZE = 1024;

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            Logger.Info("Opening image \"{0}\"", opts.Path);
            var imageStream = OpenPath(opts.Path, FileMode.Open, FileAccess.Read, FileShare.None);
            if (imageStream == null)
            {
                Logger.Error("Failed to open image!");
                WaitForUserExit();
                return INVALID_ARGUMENT;
            }

            var bufferSize = opts.BlockCount * BLOCK_SIZE;
            var buffer = new byte[bufferSize];
            var writeLock = new object();
            var lastPositionReportLock = new object();
            var lastPositionReport = DateTime.Now;

            Logger.Info("Starting deep scan of {0} (0x{1:X})", FormatBytes(imageStream.Length, 3), imageStream.Length);
            while (imageStream.Position < imageStream.Length)
            {
                var read = imageStream.Read(buffer, 0, buffer.Length);
                if (read != buffer.Length)
                {
                    Logger.Error("Failed to read {0} of data, got {1} at position 0x{2:X}", 
                        FormatBytes(buffer.Length, 3), FormatBytes(read, 3), imageStream.Position);
                    break;
                }

                if (buffer[3] == 0x4E)
                {

                }

                Parallel.For(0, bufferSize - BLOCK_SIZE + 1, new ParallelOptions() { MaxDegreeOfParallelism = opts.Threads },
                    (i, l) =>
                    {
                        var actualPosition = imageStream.Position - buffer.Length + i;

                        if (i % opts.ScanGranularity != 0)
                            return;

                        Stream stream = new MemoryStream(BLOCK_SIZE);
                        stream.Write(buffer, i, (stream as MemoryStream).Capacity);

                        // assume file system is on the rest of the stream to fix file table position validating
                        stream = new FixedLengthStream(stream, imageStream.Length - imageStream.Position, false);

                        if (opts.ScanForNtfs && NtfsFileSystem.Detect(stream))
                        {
                            lock (writeLock)
                                Logger.Info("Found NTFS file system at offset 0x{0:X} ({1})", actualPosition, FormatBytes(actualPosition, 3));
                        }
                        else if (opts.ScanForFat && FatFileSystem.Detect(stream))
                        {
                            lock (writeLock)
                                Logger.Info("Found FAT file system at offset 0x{0:X} ({1})", actualPosition, FormatBytes(actualPosition, 3));
                        }
                        else if (opts.ScanForExt && ExtFileSystem.Detect(stream))
                        {
                            lock (writeLock)
                                Logger.Info("Found EXT file system at offset 0x{0:X} ({1})", actualPosition, FormatBytes(actualPosition, 3));
                        }

                        lock(lastPositionReportLock)
                        {
                            var now = DateTime.Now;
                            if (now.Subtract(lastPositionReport).TotalSeconds >= 10.0)
                            {
                                lock (writeLock)
                                    Logger.Info("Currently scanning at offset 0x{0:X} ({1})", actualPosition, FormatBytes(actualPosition, 3));

                                lastPositionReport = now;
                            }
                        }
                    });
            }

            Cleanup(imageStream);
            WaitForUserExit();
            return SUCCESS;
        }

        [Verb("deepscan", HelpText = "Perform a full scan on images to find lost partitions and files")]
        public sealed class Options : BaseOptions
        {

            [Value(0, Default = null, HelpText = "Path to the image or the disk which should be used", Required = true)]
            public string Path { get; set; }

            [Option('b', "block-count", Default = 64, HelpText = "Count of 1K-blocks buffered in memory", Required = false)]
            public int BlockCount { get; set; }

            [Option('s', "scan-granularity", Default = 1, HelpText = "Granularity of the scan process/Count of bytes to skip when scanning", Required = false)]
            public int ScanGranularity { get; set; }

            [Option('t', "threads", Default = 2, HelpText = "Count of different threads deep scan operations run in", Required = false)]
            public int Threads { get; set; }

            [Option("ntfs", Default = false, HelpText = "Scan for NTFS file systems", Required = false)]
            public bool ScanForNtfs { get; set; }

            [Option("fat", Default = false, HelpText = "Scan for FAT file systems", Required = false)]
            public bool ScanForFat { get; set; }

            [Option("ext", Default = false, HelpText = "Scan for EXT file systems", Required = false)]
            public bool ScanForExt { get; set; }

        }

    }

}
