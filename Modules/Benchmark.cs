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
using System.Runtime.InteropServices;

using CommandLine;

using nDiscUtils.IO;
using nDiscUtils.Options;

using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.NativeMethods;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class Benchmark
    {

        private static Random mRandom = new Random();

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            var path = $"{opts.Drive}:\\ndiscutils-benchmark.dat";

            if (File.Exists(path))
                File.Delete(path);

            Logger.Info("Attempting to open benchmark-file at \"{0}:\\\"", opts.Drive);
            var handle = CreateFile(
                path,
                FileAccess.ReadWrite,
                FileShare.None,
                IntPtr.Zero,
                FileMode.Create,
                FILE_FLAG_WRITE_THROUGH | FILE_FLAG_NO_BUFFERING | FILE_FLAG_SEQUENTIAL_SCAN,
                IntPtr.Zero);

            var error = Marshal.GetLastWin32Error();
            if (handle.IsInvalid || error != 0)
            {
                Logger.Error("Failed to open benchmark-file: Error {0}", error);
                return INVALID_ARGUMENT;
            }

            if ((opts.Size % opts.BufferSize) != 0)
            {
                Logger.Error("Requested size if not divisible by buffer size", error);
                return INVALID_ARGUMENT;
            }

            var buffer = new byte[opts.BufferSize];
            var iopsPos = new long[(opts.Size / opts.BufferSize) - 1];

            // generate random data
            Logger.Debug("Generating random data");
            for (int i = 0; i < Math.Sqrt(buffer.Length); i++)
                mRandom.NextBytes(buffer);

            // generate random IOPS positions
            Logger.Debug("Generating random IOPS read/write positions");
            for (long i = 0; i < iopsPos.LongLength; i++)
            {
                var pos = NextLongRandom(mRandom, 0, opts.Size - opts.BufferSize);
                if ((pos % opts.BufferSize) != 0)
                    pos -= (pos % opts.BufferSize);

                iopsPos[i] = pos;
            }

            using (var stream = new FileStream(handle, FileAccess.ReadWrite, (int)opts.BufferSize))
            {
                stream.SetLength(opts.Size);

                // check for correct alignment of buffer size and physical sector size
                try
                {
                    stream.Seek(opts.BufferSize, SeekOrigin.Begin);
                }
                catch (IOException)
                {
                    Logger.Error("Requested buffer size is not aligned to physical sector size");
                    return INVALID_ARGUMENT;
                }

                //
                // Data Write Process
                //
                {
                    stream.Position = 0;
                    Logger.Verbose("===== Starting Data Write Process");

                    var start = DateTime.Now;
                    Logger.Verbose("Data Write start: {0}", start);

                    while (stream.Position < stream.Length)
                    {
                        stream.Write(buffer, 0, buffer.Length);
                        stream.Flush();
                    }

                    var end = DateTime.Now;
                    Logger.Verbose("Data Write end:   {0}", end);

                    var duration = end.Subtract(start);
                    Logger.Info("Data Write duration: {0}", duration);
                    Logger.Info("Data Write speed:    {0}/s", FormatBytes(stream.Length / duration.TotalSeconds, 3));
                }

                //
                // Data Read Process
                //
                {
                    stream.Position = 0;
                    Logger.Verbose("===== Starting Data Read  Process");

                    var start = DateTime.Now;
                    Logger.Verbose("Data Read  start: {0}", start);

                    while (stream.Position < stream.Length)
                    {
                        stream.Read(buffer, 0, buffer.Length);
                    }

                    var end = DateTime.Now;
                    Logger.Verbose("Data Read  end:   {0}", end);

                    var duration = end.Subtract(start);
                    Logger.Info("Data Read  duration: {0}", duration);
                    Logger.Info("Data Read  speed:    {0}/s", FormatBytes(stream.Length / duration.TotalSeconds, 3));
                }

                //
                // IOPS Write Process
                //
                {
                    stream.Position = 0;
                    Logger.Verbose("===== Starting IOPS Write Process");

                    var start = DateTime.Now;
                    Logger.Verbose("IOPS Write start: {0}", start);

                    for (long i = 0; i < iopsPos.LongLength; i++)
                    {
                        stream.Seek(iopsPos[i], SeekOrigin.Begin);
                        stream.Write(buffer, 0, buffer.Length);
                        stream.Flush();
                    }

                    var end = DateTime.Now;
                    Logger.Verbose("IOPS Write end:   {0}", end);

                    var duration = end.Subtract(start);
                    Logger.Info("IOPS Write duration: {0}", duration);
                    Logger.Info("IOPS Write speed:    {0:N}/s",
                        (long)(iopsPos.LongLength / duration.TotalSeconds), 3);
                }

                //
                // IOPS Read Process
                //
                {
                    stream.Position = 0;
                    Logger.Verbose("===== Starting IOPS Read  Process");

                    var start = DateTime.Now;
                    Logger.Verbose("IOPS Read  start: {0}", start);

                    for (long i = 0; i < iopsPos.LongLength; i++)
                    {
                        stream.Position = iopsPos[i];
                        stream.Read(buffer, 0, buffer.Length);
                    }

                    var end = DateTime.Now;
                    Logger.Verbose("IOPS Read  end:   {0}", end);

                    var duration = end.Subtract(start);
                    Logger.Info("IOPS Read  duration: {0}", duration);
                    Logger.Info("IOPS Read  speed:    {0:N}/s", 
                        (long)(iopsPos.LongLength / duration.TotalSeconds), 3);
                }

                stream.Position = 0;
                Logger.Verbose("===== Starting IOPS Read  Process");
            }

            if (File.Exists(path))
                File.Delete(path);

            return SUCCESS;
        }
        
        [Verb("benchmark", HelpText = "Read and print S.M.A.R.T. values of hard drives")]
        public sealed class Options : BaseOptions
        {

            [Value(0, Default = null, HelpText = "Letter of the drive which should be benchmarked", Required = true)]
            public char Drive { get; set; }

            [Option('s', "size", Default = "1G", HelpText = "Size of the file which will be used to write and read the test-data", Required = false)]
            public string SizeString { get; set; }

            public long Size
            {
                get => ParseSizeString(SizeString);
            }

            [Option('b', "buffer-size", Default = "64K", HelpText = "Size of the I/O-buffer used to read/write the test-data", Required = false)]
            public string BufferSizeString { get; set; }

            public long BufferSize
            {
                get => ParseSizeString(BufferSizeString);
            }

        }

    }

}
