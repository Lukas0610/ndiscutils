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

using CommandLine;

using nDiscUtils.IO;
using nDiscUtils.Options;

using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.nConsole;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class Erase
    {

        private static Random mRandom = new Random();

        private static long mTotalPosition;
        private static long mTotalLength;
        private static DateTime mTotalStartTime;

        private static string mLastTotalProgressString = "";
        private static string mLastTotalSpeedString = "";

        private static string mLastProgressString = "";
        private static string mLastSpeedString = "";

        private static Func<string, Stream> mOpenAction = null;

        public static int Run(Options opts)
        {
            RunHelpers(opts);
            OpenNewConsoleBuffer();
            InitializeConsole();
            ResetColor();

            // print source/destination
            WriteFormat(ContentLeft, ContentTop, "Target: {0}", opts.Target);

            Write(ContentLeft, ContentTop + 2, "0 / 0");
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 2, "0 Bytes / 0 Bytes");
            Write(ContentLeft, ContentTop + 3, '[');
            Write(ContentLeft + ContentWidth, ContentTop + 3, ']');
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 4, "ETA: 00:00:00  @  0 Bytes/s");

            Write(ContentLeft, ContentTop + 6, "0 / 0");
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 6, "0 Bytes / 0 Bytes");
            Write(ContentLeft, ContentTop + 7, '[');
            Write(ContentLeft + ContentWidth, ContentTop + 7, ']');
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 8, "ETA: 00:00:00  @  0 Bytes/s");

            // update advanced logging
            Logger.SetAdvancedLoggingOffset(10);

            var returnCode = StartInternal(opts);

            if (returnCode == SUCCESS)
                Logger.Fine("Finished!");
            else
                Logger.Error("One or more errors occurred...");

            UpdateBackgroundIfRequired(returnCode);
            WaitForUserExit();
            RestoreOldConsoleBuffer();
            return SUCCESS;
        }

        private static int StartInternal(Options opts)
        {
            if (opts.Cached)
            {
                mOpenAction = new Func<string, Stream>((path)
                    => OpenPath(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read));
            }
            else
            {
                mOpenAction = new Func<string, Stream>((path)
                    => OpenPathUncached(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read));
            }

            if (Directory.Exists(opts.Target))
                return StartInternalDirectory(opts);
            else
                return StartInternalFile(opts);
        }

        private static int StartInternalFile(Options opts)
        {
            var handle = mOpenAction(opts.Target);
            if (handle == null || !handle.CanWrite)
            {
                Logger.Error("Failed to open target \"{0}\" for writing", opts.Target);
                return INVALID_ARGUMENT;
            }

            mTotalPosition = 0;
            mTotalLength = handle.Length * opts.EraseCount;
            mTotalStartTime = DateTime.Now;

            EraseStream(1, opts.Target, handle, opts);
            return SUCCESS;
        }

        private static int StartInternalDirectory(Options opts)
        {
            long fileSize = 0, fileCount = 0, directoryCount = 0;
            List<FileInfo> fileList = null;
            List<DirectoryInfo> directoryList = null;
            
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop, "Files: {0,10} / {1,10}", 0, 0);

            var baseDirectory = new DirectoryInfo(opts.Target);
            IndexDirectory(baseDirectory, opts.Threads, ref fileList, ref directoryList, ref fileSize, ref fileCount, ref directoryCount,
                (iFileCount, iDirectoryCount) =>
                {
                    WriteFormatRight(ContentLeft + ContentWidth, ContentTop, "Files: {0,10} / {1,10}", 0, iFileCount);
                });

            mTotalPosition = 0;
            mTotalLength = fileSize * opts.EraseCount;
            mTotalStartTime = DateTime.Now;

            for (int i = 0; i < fileCount; i++)
            {
                var file = fileList[i];

                using (var stream = mOpenAction(file.FullName))
                    EraseStream(i + 1, file.FullName, stream, opts);

                WriteFormatRight(ContentLeft + ContentWidth, ContentTop, "Files: {0,10} / {1,10}", i + 1, fileCount);
            }

            return SUCCESS;
        }

        private static void EraseStream(long task, string path, Stream destination, Options opts)
        {
            Logger.Info("[{0}] Starting to delete target \"{1}\"", task, opts.Target);

            for (int i = 0; i < opts.EraseCount; i++)
            {
                destination.Position = 0;
                EraseStreamImpl(task, i, destination, opts);
            }
        }

        private static void EraseStreamImpl(long task, int loop, Stream destination, Options opts)
        {
            var buffer = new byte[opts.BufferSize];

            if (opts.Randomize)
            {
                mRandom.NextBytes(buffer);
            }
            else
            {
                MemoryWrapper.Set(buffer, 0, buffer.Length);
            }

            var relativeProgressWidth = ContentWidth - 1;
            var lastPosition = 0L;
            var lastTime = DateTime.Now;

            Logger.Verbose("[{0}] Starting loop #{1}", task, loop);

            Write(ContentLeft + 1, ContentTop + 7, ' ', relativeProgressWidth);

            while (destination.Position < destination.Length)
            {
                if (opts.Randomize && !opts.RandomizeOnce)
                    mRandom.NextBytes(buffer);

                var writeCount = (int)Math.Min(
                    buffer.Length, 
                    destination.Length - destination.Position);

                destination.Write(buffer, 0, writeCount);
                mTotalPosition += writeCount;


                var now = DateTime.Now;
                var timeDelta = now.Subtract(lastTime);
                if (timeDelta.TotalSeconds >= 1.0 || destination.Position == destination.Length || opts.FastRefresh)
                {

                    ResetColor();

                    //
                    // Total progress
                    //
                    {
                        var totalTimeDelta = now.Subtract(mTotalStartTime);
                        var progress = ((double)mTotalPosition / mTotalLength);
                        var averageSpeed = (mTotalPosition == 0 ? 0.0 : mTotalPosition / totalTimeDelta.TotalSeconds);

                        var estimatedEnd = (averageSpeed == 0 ? TimeSpan.MaxValue :
                            TimeSpan.FromSeconds((mTotalLength - mTotalPosition) / averageSpeed));

                        var widthProgress = (int)Math.Min(progress * relativeProgressWidth, relativeProgressWidth);

                        Write(ContentLeft + 1, ContentTop + 3, '|', widthProgress);
                        WriteFormat(ContentLeft, ContentTop + 4, "{0:0.00} %  ", progress * 100);

                        var progressString = string.Format(
                            "{0} / {1}",
                            FormatBytes(mTotalPosition, 3), FormatBytes(mTotalLength, 3));

                        var progressPadding = "";
                        if (progressString.Length < mLastTotalProgressString.Length)
                            progressPadding = new string(' ', mLastTotalProgressString.Length - progressString.Length);
                        mLastTotalProgressString = progressString;

                        WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 2,
                            "{0}{1}", progressPadding, progressString);

                        var speedString = string.Format(
                            "ETA: {0:hh\\:mm\\:ss}  @  {1}/s",
                            estimatedEnd, FormatBytes(averageSpeed, 3));

                        var speedPadding = "";
                        if (speedString.Length < mLastTotalSpeedString.Length)
                            speedPadding = new string(' ', mLastTotalSpeedString.Length - speedString.Length);
                        mLastTotalSpeedString = speedString;

                        WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 4,
                            "{0}{1}", speedPadding, speedString);
                    }

                    //
                    // Current progress
                    //
                    {
                        var posDelta = destination.Position - lastPosition;
                        var progress = ((double)destination.Position / destination.Length);
                        var averageSpeed = (posDelta == 0 ? 0.0 : posDelta / timeDelta.TotalSeconds);

                        var estimatedEnd = (averageSpeed == 0 ? TimeSpan.MaxValue :
                            TimeSpan.FromSeconds((destination.Length - destination.Position) / averageSpeed));

                        var widthProgress = (int)Math.Min(progress * relativeProgressWidth, relativeProgressWidth);

                        Write(ContentLeft + 1, ContentTop + 7, '|', widthProgress);
                        WriteFormat(ContentLeft, ContentTop + 8, "{0:0.00} %  ", progress * 100);

                        var progressString = string.Format(
                            "{0} / {1}",
                            FormatBytes(destination.Position, 3), FormatBytes(destination.Length, 3));

                        var progressPadding = "";
                        if (progressString.Length < mLastProgressString.Length)
                            progressPadding = new string(' ', mLastProgressString.Length - progressString.Length);
                        mLastProgressString = progressString;

                        WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 6,
                            "{0}{1}", progressPadding, progressString);

                        var speedString = string.Format(
                            "ETA: {0:hh\\:mm\\:ss}  @  {1}/s",
                            estimatedEnd, FormatBytes(averageSpeed, 3));

                        var speedPadding = "";
                        if (speedString.Length < mLastSpeedString.Length)
                            speedPadding = new string(' ', mLastSpeedString.Length - speedString.Length);
                        mLastSpeedString = speedString;

                        WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 8,
                            "{0}{1}", speedPadding, speedString);
                    }

                    lastPosition = destination.Position;
                    lastTime = now;
                }
            }

            destination.Flush();
        }

        [Verb("erase", HelpText = "Securely erase disks, volumes, single files or whole directories")]
        public sealed class Options : BaseOptions
        {

            [Value(0, Default = null, HelpText = "Target to erase. May be disk, volume, file or directory", Required = true)]
            public string Target { get; set; }

            [Option('b', "buffer-size", Default = "512K", HelpText = "Size of the I/O-buffer used to write data", Required = false)]
            public string BufferSizeString { get; set; }

            public long BufferSize
            {
                get => ParseSizeString(BufferSizeString);
            }

            [Option('c', "count", Default = 1, HelpText = "Count of erase loops run on each disk/volume/file", Required = false)]
            public int EraseCount { get; set; }

            [Option('r', "randomize", Default = false, HelpText = "Always write randomized data. Regenerate random data after each write command.", Required = false)]
            public bool Randomize { get; set; }

            [Option('s', "randomize-once", Default = false, HelpText = "Just generate random data once when starting erase process", Required = false)]
            public bool RandomizeOnce { get; set; }

            [Option('t', "threads", Default = 2, HelpText = "If target is directory: Count of threads used to index files", Required = false)]
            public int Threads { get; set; }

            [Option("cached", Default = false, HelpText = "Opens files with basic disk caches. May not properly delete files on phyiscal layer", Required = false)]
            public bool Cached { get; set; }

            [Option('g', "fast-refresh", Default = false, HelpText = "Fast-refresh the outputted progress. May slow down the erase process.", Required = false)]
            public bool FastRefresh { get; set; }

        }

    }

}
