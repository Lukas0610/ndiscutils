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
using nDiscUtils.IO;
using nDiscUtils.Options;
using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.nConsole;

namespace nDiscUtils.Modules
{

    public static class Sync
    {

        private static string[] kSupportedComparators =
        {
            "length",
            "writetime",
            "creationtime",
            "content",
        };

        public static int Run(Options opts)
        {
            RunHelpers(opts);
            OpenNewConsoleBuffer();
            InitializeConsole();
            ResetColor();

            // print source/destination
            WriteFormat(ContentLeft, ContentTop,     "Source:      {0}", opts.Source);
            WriteFormat(ContentLeft, ContentTop + 1, "Destination: {0}", opts.Target);
            
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop,     "Files:       {0:8} / {1:8}", 0, 0);
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 1, "Directories: {0:8} / {1:8}", 0, 0);

            // print progress placeholder
            Write(ContentLeft, ContentTop + 3, "0 / 0");
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 3, "0 Bytes / 0 Bytes");
            Write(ContentLeft, ContentTop + 4, '[');
            Write(ContentLeft + ContentWidth, ContentTop + 4, ']');

            WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 7, "0 Bytes / 0 Bytes");
            Write(ContentLeft, ContentTop + 8, '[');
            Write(ContentLeft + ContentWidth, ContentTop + 8, ']');
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 9, "ETA: 00:00:00  @  0 Bytes/s");

            // update advanced logging
            Logger.SetAdvancedLoggingOffset(11);

            if (!Directory.Exists(opts.Source))
            {
                Logger.Error("Could not source directory \"{0}\"", opts.Source);
                goto exit;
            }

            if (!Directory.Exists(opts.Target))
            {
                Logger.Error("Could not target directory \"{0}\"", opts.Target);
                goto exit;
            }

            string[] comparators = null;

            if (opts.Comparators != null)
                comparators = opts.Comparators.Split(',');

            if (comparators != null &&
                comparators.Any(c => !kSupportedComparators.Contains(c)))
            {
                var unsupportedComparator = comparators
                    .Where(c => !kSupportedComparators.Contains(c))
                    .First();

                Logger.Error("Found unsupported comparator: \"{0}\"", unsupportedComparator);
                Logger.Error("Supported comparators: [ {0} ]", string.Join(", ", kSupportedComparators));
                goto exit;
            }

            Logger.Info("Indexing files and directories in \"{0}\"", opts.Source);
            var fileList = new LinkedList<FileInfo>();
            var directoryList = new LinkedList<DirectoryInfo>();
            long fileSize = 0, fileCount = 0, directoryCount = 0;

            Action<DirectoryInfo> recursiveFileIndexer = null;
            recursiveFileIndexer = new Action<DirectoryInfo>((parentDir) =>
            {
                try
                {
                    foreach (var file in parentDir.GetFiles())
                    {
                        fileList.AddLast(file);

                        fileCount++;
                        fileSize += file.Length;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                try
                {
                    foreach (var subDir in parentDir.GetDirectories())
                    {
                        directoryList.AddLast(subDir);
                        directoryCount++;

                        Logger.Verbose("Advancing recursion into \"{0}\"", subDir.FullName);
                        recursiveFileIndexer(subDir);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            });

            recursiveFileIndexer(new DirectoryInfo(opts.Source));

            Logger.Info("Found {0} director{1} and {2} file{3} with a size of {4}", 
                directoryCount, (directoryCount == 1 ? "y" : "ies"),
                fileCount, (fileCount == 1 ? "" : "s"),
                FormatBytes(fileSize, 3));

            WriteFormatRight(ContentLeft + ContentWidth, ContentTop,     "Files:       {0,8} / {1,8}", 0, fileCount);
            WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 1, "Directories: {0,8} / {1,8}", 0, directoryCount);

            var absoluteSource = Path.GetFullPath(opts.Source).TrimEnd('\\');
            var absoluteTarget = Path.GetFullPath(opts.Target).TrimEnd('\\');

            // progress stuff
            var currentFile = 0L;
            var currentDirectory = 0L;
            var currentFileBytes = 0L;
            var lastFilesProgressString = "";
            var lastProgressString = "";
            var lastSpeedString = "";

            foreach (var sourceFile in fileList)
            {
                var relativeProgressWidth = ContentWidth - 1;
                currentFile++;

                ResetColor();

                WriteFormat(ContentLeft, ContentTop + 3, "File {0} / {1}", currentFile, fileCount);
                WriteFormatRight(ContentLeft + ContentWidth, ContentTop,     "Files:       {0,8} / {1,8}", currentFile, fileCount);
                WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 1, "Directories: {0,8} / {1,8}", currentDirectory, directoryCount);

                var fileProgress = ((double)currentFile / fileCount) * 100;
                Write(ContentLeft + 1, ContentTop + 4, '|', (int)((fileProgress / 100) * relativeProgressWidth));
                WriteFormat(ContentLeft, ContentTop + 5, "{0:0.00} %  ", fileProgress);

                var needsUpdate = false;
                var relativePath = sourceFile.FullName.Substring(absoluteSource.Length).Trim('\\');
                var targetFile = new FileInfo(Path.Combine(absoluteTarget, relativePath));

                Logger.Info("Processing file \"{0}\"...", relativePath);

                if (!targetFile.Exists || opts.Comparators == null)
                    needsUpdate = true;

                if (comparators != null)
                {
                    foreach (var comparator in comparators)
                    {
                        if (needsUpdate)
                            break;

                        switch (comparator)
                        {
                            case "length":
                                needsUpdate = (targetFile.Length != sourceFile.Length);
                                break;

                            case "writetime":
                                needsUpdate = (targetFile.LastWriteTime < sourceFile.LastWriteTime);
                                break;

                            case "creationtime":
                                needsUpdate = (targetFile.CreationTime < sourceFile.CreationTime);
                                break;

                            case "content":
                                needsUpdate = (targetFile.Length != sourceFile.Length);
                                if (needsUpdate)
                                    break;

                                using (var sourceStream = sourceFile.OpenRead())
                                using (var targetStream = targetFile.OpenRead())
                                {
                                    var sourceBuffer = new byte[64 * 1024];
                                    var targetBuffer = new byte[64 * 1024];

                                    while (sourceStream.Position < sourceStream.Length)
                                    {
                                        var sourceRead = sourceStream.Read(sourceBuffer, 0, sourceBuffer.Length);
                                        var targetRead = targetStream.Read(targetBuffer, 0, targetBuffer.Length);

                                        needsUpdate = (sourceRead != targetRead);
                                        if (needsUpdate)
                                            break;

                                        needsUpdate = EqualBytesLongUnrolled(sourceBuffer, targetBuffer);
                                        if (needsUpdate)
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                }

                if (needsUpdate)
                {
                    Logger.Verbose("Updating file \"{0}\"...", relativePath);

                    // progress stuff
                    var lastSpeedMeasure = DateTime.Now;
                    var lastSpeedCurrent = 0L;

                    // check target directory structure
                    if (!targetFile.Directory.Exists)
                        targetFile.Directory.CreateRecursive();

                    Write(ContentLeft + 1, ContentTop + 8, ' ', relativeProgressWidth);

                    using (var sourceStream = sourceFile.OpenRead())
                    using (var targetStream = new FileStream(targetFile.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                    {
                        var buffer = new byte[64 * 1024];

                        targetStream.SetLength(sourceStream.Length);

                        while (sourceStream.Position < sourceStream.Length)
                        {
                            var read = sourceStream.Read(buffer, 0, buffer.Length);
                            targetStream.Write(buffer, 0, read);

                            currentFileBytes += read;

                            var speedMeasureNow = DateTime.Now;
                            var speedMeasureDiff = DateTime.Now.Subtract(lastSpeedMeasure);
                            if (speedMeasureDiff.TotalSeconds >= 1.0 || sourceStream.Position == sourceStream.Length)
                            {
                                var current = sourceStream.Position;
                                var total = sourceStream.Length;

                                var progress = ((double)current / total) * 100;
                                var widthProgress = (int)Math.Min((progress / 100) * relativeProgressWidth, relativeProgressWidth);

                                var currentDelta = current - lastSpeedCurrent;
                                var averageSpeed = (currentDelta <= 0 ? 0.0 :
                                    currentDelta / speedMeasureDiff.TotalSeconds);

                                var estimatedEnd = (averageSpeed == 0 ? TimeSpan.MaxValue :
                                TimeSpan.FromSeconds((total - current) / averageSpeed));

                                ResetColor();

                                Write(ContentLeft + 1, ContentTop + 8, '|', widthProgress);
                                WriteFormat(ContentLeft, ContentTop + 9, "{0:0.00} %  ", progress);

                                var filesProgressString = string.Format(
                                    "{0} / {1}",
                                    FormatBytes(currentFileBytes, 3), FormatBytes(fileSize, 3));

                                var filesProgressPadding = "";
                                if (filesProgressString.Length < lastFilesProgressString.Length)
                                    filesProgressPadding = new string(' ', lastFilesProgressString.Length - filesProgressString.Length);
                                lastFilesProgressString = filesProgressString;

                                WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 3,
                                    "{0}{1}", filesProgressPadding, filesProgressString);

                                var progressString = string.Format(
                                    "{0} / {1}",
                                    FormatBytes(current, 3), FormatBytes(total, 3));

                                var progressPadding = "";
                                if (progressString.Length < lastProgressString.Length)
                                    progressPadding = new string(' ', lastProgressString.Length - progressString.Length);
                                lastProgressString = progressString;

                                WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 7,
                                    "{0}{1}", progressPadding, progressString);

                                var speedString = string.Format(
                                    "ETA: {0:hh\\:mm\\:ss}  @  {1}/s",
                                    estimatedEnd, FormatBytes(averageSpeed, 3));

                                var speedPadding = "";
                                if (speedString.Length < lastSpeedString.Length)
                                    speedPadding = new string(' ', lastSpeedString.Length - speedString.Length);
                                lastSpeedString = speedString;

                                WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 9,
                                    "{0}{1}", speedPadding, speedString);

                                lastSpeedCurrent = current;
                                lastSpeedMeasure = speedMeasureNow;
                            }
                        }
                    }

                    // transfering attributes
                    if (!opts.SkipAttributes)
                    {
                        targetFile.Attributes = sourceFile.Attributes;
                        targetFile.IsReadOnly = sourceFile.IsReadOnly;
                    }

                    // transfering dates
                    if (!opts.SkipDates)
                    {
                        targetFile.CreationTime = sourceFile.CreationTime;
                        targetFile.LastAccessTime = sourceFile.LastAccessTime;
                        targetFile.LastWriteTime = sourceFile.LastWriteTime;
                    }

                    // transfering security descriptors
                    if (!opts.SkipSecurity)
                    {
                        targetFile.SetAccessControl(sourceFile.GetAccessControl());
                    }
                }
                else
                {
                    Logger.Verbose("Skipping files \"{0}\"!", relativePath);
                }
            }

            foreach (var sourceDirectory in directoryList)
            {
                currentDirectory++;

                var relativePath = sourceDirectory.FullName.Substring(absoluteSource.Length).Trim('\\');
                var targetDirectory = new DirectoryInfo(Path.Combine(absoluteTarget, relativePath));
                
                Logger.Info("Processing directory \"{0}\"...", sourceDirectory.FullName);

                ResetColor();
                WriteFormatRight(ContentLeft + ContentWidth, ContentTop,     "Files:       {0,8} / {1,8}", currentFile, fileCount);
                WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 1, "Directories: {0,8} / {1,8}", currentDirectory, directoryCount);

                // transfering attributes
                if (!opts.SkipAttributes)
                {
                    targetDirectory.Attributes = sourceDirectory.Attributes;
                }

                // transfering dates
                if (!opts.SkipDates)
                {
                    targetDirectory.CreationTime = sourceDirectory.CreationTime;
                    targetDirectory.LastAccessTime = sourceDirectory.LastAccessTime;
                    targetDirectory.LastWriteTime = sourceDirectory.LastWriteTime;
                }

                // transfering security descriptors
                if (!opts.SkipSecurity)
                {
                    targetDirectory.SetAccessControl(sourceDirectory.GetAccessControl());
                }
            }

            Logger.Fine("Finished!");

exit:
            WaitForUserExit();
            RestoreOldConsoleBuffer();
            return 0;
        }

        private static bool HasComparator(string[] comparators, string comp)
        {
            return comparators != null && comparators.Any(c => (comp == c));
        }

        [Verb("sync", HelpText = "Synchronises files between two directories")]
        public sealed class Options : BaseOptions
        {

            [Value(1, Default = null, HelpText = "Source of the the sync-process", Required = true)]
            public string Source { get; set; }

            [Value(2, Default = null, HelpText = "Target of the sync-process", Required = true)]
            public string Target { get; set; }

            [Option('c', "comparators", HelpText = "File state comparators and their order of execution", Default = null, Required = false)]
            public string Comparators { get; set; }

            [Option("no-attributes", HelpText = "Skip synchronizing file attributes", Default = false, Required = false)]
            public bool SkipAttributes { get; set; }

            [Option("no-dates", HelpText = "Skip synchronizing file change/access/write dates", Default = false, Required = false)]
            public bool SkipDates { get; set; }

            [Option("no-security", HelpText = "Skip synchronizing file security data", Default = false, Required = false)]
            public bool SkipSecurity { get; set; }

        }

    }

}
