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
using System.Threading;

using CommandLine;

using nDiscUtils.Core;
using nDiscUtils.Options;

using static nDiscUtils.Core.ModuleHelpers;
using static nDiscUtils.Core.NativeMethods;
using static nDiscUtils.Core.ReturnCodes;

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

            var attributes = 0U;

            if ((!opts.NoWriteThrough || opts.Unbuffered) && !opts.Pratical)
                attributes |= FILE_FLAG_WRITE_THROUGH;
            else
                Logger.Warn("Disabling WRITE_THROUGH flag");

            if ((!opts.Buffering || opts.Unbuffered) && !opts.Pratical)
                attributes |= FILE_FLAG_NO_BUFFERING;
            else
                Logger.Warn("Disabling NO_BUFFERING flag");

            if ((!opts.NoSequentialScan || opts.Unbuffered) && !opts.Pratical)
                attributes |= FILE_FLAG_SEQUENTIAL_SCAN;
            else
                Logger.Warn("Disabling SEQUENTIAL_SCAN flag");

            Logger.Info("Attempting to open benchmark-file at \"{0}:\\\"", opts.Drive);
            var handle = CreateFile(
                path,
                FileAccess.ReadWrite,
                FileShare.None,
                IntPtr.Zero,
                FileMode.Create,
                attributes,
                IntPtr.Zero);

            var error = Marshal.GetLastWin32Error();
            if (handle.IsInvalid || error != 0)
            {
                Logger.Error("Failed to open benchmark-file: Error {0}", error);
                WaitForUserExit();
                return INVALID_ARGUMENT;
            }

            if ((opts.Size % opts.BufferSize) != 0)
            {
                Logger.Error("Requested size if not divisible by buffer size", error);
                WaitForUserExit();
                return INVALID_ARGUMENT;
            }

            var buffer = new byte[opts.BufferSize];
            var secondaryBuffer = new byte[0];
            var randomPos = new long[(opts.Size / opts.BufferSize) - 1];

            // generate random data
            Logger.Debug("Generating random data");
            for (int i = 0; i < 4; i++)
            {
                mRandom.NextBytes(buffer);
                Thread.Sleep(mRandom.Next(1, 25));
            }

            if (opts.Pratical)
            {
                Logger.Debug("Generating secondary random data");
                secondaryBuffer = new byte[opts.BufferSize];
                for (int i = 0; i < 4; i++)
                {
                    mRandom.NextBytes(secondaryBuffer);
                    Thread.Sleep(mRandom.Next(1, 25));
                }
            }

            // generate random positions
            Logger.Debug("Generating random read/write positions");
            for (long i = 0; i < randomPos.LongLength; i++)
            {
                var pos = NextLongRandom(mRandom, 0, opts.Size - opts.BufferSize);
                if ((pos % opts.BufferSize) != 0)
                    pos -= (pos % opts.BufferSize);

                randomPos[i] = pos;
            }

            var usePrimaryBuffer = true;
            var getBuffer = new Func<byte[]>(() =>
            {
                if (opts.Pratical)
                {
                    usePrimaryBuffer = !usePrimaryBuffer;
                    return (usePrimaryBuffer ? buffer : secondaryBuffer);
                }
                else
                {
                    return buffer;
                }
            });

            using (var stream = new FileStream(handle, FileAccess.ReadWrite, (int)opts.InternalBufferSize))
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
                    WaitForUserExit();
                    return INVALID_ARGUMENT;
                }

                var sequentialAction = new Func<string, Action, TimeSpan>((name, action) =>
                {
                    stream.Position = 0;
                    Logger.Info("===== Starting Sequential {0} Process", name);

                    var start = DateTime.Now;
                    Logger.Verbose("Sequential {0} start: {1}", name, start);

                    while (stream.Position < stream.Length)
                        action();

                    var end = DateTime.Now;
                    Logger.Verbose("Sequential {0} end:   {1}", name, end);

                    var duration = end.Subtract(start);
                    Logger.Info("Sequential {0} duration: {1}", name, duration);
                    Logger.Info("Sequential {0} speed:    {1}/s", name,
                        FormatBytes(stream.Length / duration.TotalSeconds, 3));
                    return duration;
                });

                var randomAction = new Func<string, Action, TimeSpan>((name, action) =>
                {
                    stream.Position = 0;
                    Logger.Info("===== Starting Random {0} Process", name);

                    var start = DateTime.Now;
                    Logger.Verbose("Random {0} start: {1}", name, start);

                    for (long i = 0; i < randomPos.Length; i++)
                    {
                        stream.Seek(randomPos[i], SeekOrigin.Begin);
                        action();
                    }

                    var end = DateTime.Now;
                    Logger.Verbose("Random {0} end:   {1}", name, end);

                    var duration = end.Subtract(start);
                    Logger.Info("Random {0} duration: {1}", name, duration);
                    Logger.Info("Random {0} speed:    {1}/s", name,
                        FormatBytes(stream.Length / duration.TotalSeconds, 3));
                    return duration;
                });

                var write = new Action(() =>
                {
                    var useBuffer = getBuffer();
                    stream.Write(useBuffer, 0, useBuffer.Length);
                    stream.Flush();
                });

                var read = new Action(() =>
                {
                    stream.Read(buffer, 0, buffer.Length);
                });

                if (!opts.SkipSequential)
                {
                    var sequentialWriteDuration = sequentialAction("write", write);
                    var sequentialReadDuration = sequentialAction("read", read);
                }

                if (!opts.SkipRandom)
                {
                    var randomWriteDuration = randomAction("write", write);
                    var randomReadDuration = randomAction("read", read);
                }
            }

            if (File.Exists(path))
                File.Delete(path);

            WaitForUserExit();
            return SUCCESS;
        }
        
        [Verb("benchmark", HelpText = "Run various kind of benchmarks on several kind of disks")]
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

            [Option('b', "buffer-size", Default = "16M", HelpText = "Size of the I/O-buffer used to read/write the test-data", Required = false)]
            public string BufferSizeString { get; set; }

            public long BufferSize
            {
                get => ParseSizeString(BufferSizeString);
            }

            [Option('i', "internal-buffer-size", Default = "512", HelpText = "Size of the internal I/O-buffer used to read/write the test-data", Required = false)]
            public string InternalBufferSizeString { get; set; }

            public long InternalBufferSize
            {
                get => ParseSizeString(InternalBufferSizeString);
            }

            [Option('u', "unbuffered", Default = false, HelpText = "Disables all buffers to get the most realistic physical I/O speed", Required = false)]
            public bool Unbuffered { get; set; }

            [Option('p', "pratical", Default = false, HelpText = "Tries to simulate a default userspace I/O-operation as good as possible", Required = false)]
            public bool Pratical { get; set; }

            [Option("skip-sequential", Default = false, HelpText = "Skips sequential read/write tests", Required = false)]
            public bool SkipSequential { get; set; }

            [Option("skip-random", Default = false, HelpText = "Skips random-positioned read/write tests", Required = false)]
            public bool SkipRandom { get; set; }

            [Option("no-write-through", Default = false, HelpText = "Disable WRITE_THROUGH flag when creating the benchmarking-file", Required = false)]
            public bool NoWriteThrough { get; set; }

            [Option("buffering", Default = false, HelpText = "Disable NO_BUFFERING flag when creating the benchmarking-file", Required = false)]
            public bool Buffering { get; set; }

            [Option("no-sequential-scan", Default = false, HelpText = "Disable sequential scan flag when creating the benchmarking-file", Required = false)]
            public bool NoSequentialScan { get; set; }

        }

    }

}
