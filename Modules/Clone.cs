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

using DiscUtils;
using DiscUtils.Fat;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;

using nDiscUtils.IO;
using nDiscUtils.Modules.Events;
using nDiscUtils.Options;

using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.nConsole;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class Clone
    {

        private static string mSourcePath;
        private static Stream mSourceStream;

        private static string mDestinationPath;
        private static Stream mDestinationStream;

        private static long mBufferSize;
        private static bool mForceExactCloning;
        private static bool mFullClone;
        
        private static long mTaskIdCounter;
        
        private static event CloneProgressEventHandler CloneProgressEvent;

        public static int Run(Options opts)
        {
            RunHelpers(opts);
            OpenNewConsoleBuffer();
            InitializeConsole();
            ResetColor();

            // print source/destination
            WriteFormat(ContentLeft, ContentTop,     "Source:      {0}", opts.Source);
            WriteFormat(ContentLeft, ContentTop + 1, "Destination: {0}", opts.Destination);

            // print progress placeholder
            Write(ContentLeft, ContentTop + 3, "Partition: -");
            Write(ContentLeft + 20, ContentTop + 3, "Task: -");
            Write(ContentLeft, ContentTop + 4, '[');
            Write(ContentLeft + ContentWidth, ContentTop + 4, ']');

            // register progress handler
            var lastPartition = -1;
            var lastTaskId = -1L;
            var lastSpeedMeasure = DateTime.Now;
            var lastSpeedCurrent = 0L;
            var lastProgressString = "";
            var lastSpeedString = "";

            // update advanced logging
            Logger.SetAdvancedLoggingOffset(7);

            CloneProgressEvent += (e) =>
            {
                var needToClearProgress = false;
                var relativeProgressWidth = ContentWidth - 1;

                // infos about current progress + prevent unneeded updating
                if (lastPartition != e.Partition)
                {
                    ResetColor();
                    WriteFormat(ContentLeft, ContentTop + 3,
                        "Partition: #{0}", e.Partition);

                    lastPartition = e.Partition;
                    needToClearProgress = true;
                    lastSpeedMeasure = DateTime.Now;
                }

                if (lastTaskId != e.TaskID)
                {
                    ResetColor();
                    WriteFormat(ContentLeft + 20, ContentTop + 3,
                        "Task: #{0}", e.TaskID);

                    lastTaskId = e.TaskID;
                    needToClearProgress = true;
                    lastSpeedMeasure = DateTime.Now;
                }

                if (needToClearProgress)
                    Write(ContentLeft + 1, ContentTop + 4, ' ', relativeProgressWidth);

                // the actual progress
                var speedMeasureNow = DateTime.Now;
                var speedMeasureDiff = DateTime.Now.Subtract(lastSpeedMeasure);
                if (speedMeasureDiff.TotalSeconds >= 1.0 || (e.Current >= e.Total) || opts.FastRefresh)
                {
                    var progress = ((double)e.Current / e.Total) * 100;
                    var widthProgress = (int)Math.Min(((double)e.Current / e.Total)
                        * relativeProgressWidth, relativeProgressWidth);

                    var currentDelta = e.Current - lastSpeedCurrent;
                    var averageSpeed = (currentDelta == 0 ? 0.0 :
                        currentDelta / speedMeasureDiff.TotalSeconds);

                    var estimatedEnd = (averageSpeed == 0 ? TimeSpan.MaxValue :
                    TimeSpan.FromSeconds((e.Total - e.Current) / averageSpeed));

                    ResetColor();

                    Write(ContentLeft + 1, ContentTop + 4, '|', widthProgress);
                    WriteFormat(ContentLeft, ContentTop + 5, "{0:0.00} %  ", progress);

                    var progressString = string.Format(
                        "{0} / {1}",
                        FormatBytes(e.Current, 3), FormatBytes(e.Total, 3));

                    var progressPadding = "";
                    if (progressString.Length < lastProgressString.Length)
                        progressPadding = new string(' ', lastProgressString.Length - progressString.Length);
                    lastProgressString = progressString;

                    WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 3,
                        "{0}{1}", progressPadding, progressString);

                    var speedString = string.Format(
                        "ETA: {0:hh\\:mm\\:ss}  @  {1}/s",
                        estimatedEnd, FormatBytes(averageSpeed, 3));

                    var speedPadding = "";
                    if (speedString.Length < lastSpeedString.Length)
                        speedPadding = new string(' ', lastSpeedString.Length - speedString.Length);
                    lastSpeedString = speedString;

                    WriteFormatRight(ContentLeft + ContentWidth, ContentTop + 5,
                        "{0}{1}", speedPadding, speedString);

                    lastSpeedCurrent = e.Current;
                    lastSpeedMeasure = speedMeasureNow;
                }
            };

            mSourcePath = opts.Source;
            mDestinationPath = opts.Destination;

            mSourceStream = OpenPathUncached(opts.Source, FileMode.Open, FileAccess.Read, FileShare.Read);
            mDestinationStream = OpenPathUncached(opts.Destination, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            
            mTaskIdCounter = 0;

            mBufferSize = opts.BufferSize;
            mForceExactCloning = opts.ForceExactCloning;
            mFullClone = opts.FullClone;

            var returnCode = StartInternal();

            if (returnCode == SUCCESS)
                Logger.Fine("Finished!");
            else
                Logger.Error("One or more errors occurred...");

            UpdateBackgroundIfRequired(returnCode);
            WaitForUserExit();
            RestoreOldConsoleBuffer();
            return returnCode;
        }
        
        public static void Dispose()
        {
            mSourceStream.Flush();
            mSourceStream.Close();
            mSourceStream.Dispose();
            mSourceStream = null;

            mDestinationStream.Flush();
            mDestinationStream.Close();
            mDestinationStream.Dispose();
            mDestinationStream = null;

            mSourcePath = "";
            mDestinationPath = "";
        }

        private static int StartInternal()
        {
            if (!GuidPartitionTable.Detect(mSourceStream))
                throw new InvalidDataException(
                    "Source does not contain a supported partitioning scheme");

            Logger.Debug("Detecting disc geometry of source...");
            var geometry = GuidPartitionTable.DetectGeometry(mSourceStream);

            Logger.Debug("Reading GPT partition table from source...");
            var partitionTable = new GuidPartitionTable(mSourceStream, geometry);

            if (partitionTable.Count <= 0)
                return SUCCESS; // nothing to clone here

            // adjust source length
            if (FixedLengthStream.IsFixedDiskStream(mSourceStream))
            {
                mSourceStream.SetLength(geometry.Capacity);
            }

            Geometry destGeometry = null;

            if (FixedLengthStream.IsFixedDiskStream(mDestinationStream))
            {
                // If we write to a disk, we need our own geometry for that destination
                destGeometry = Geometry.FromCapacity(mDestinationStream.Length,
                    geometry.BytesPerSector);
            }
            else
            {
                // If we are just writing to a file, we take the most exact copy we get
                destGeometry = geometry;
            }

            // Set the new size of the destination stream
            Logger.Verbose("Updating length of destination: {0} -> {1}",
                mDestinationStream.Length, destGeometry.Capacity);
            mDestinationStream.SetLength(destGeometry.Capacity);

            if (mFullClone)
            {
                Logger.Warn("Starting full clone...");
                CloneStream(0, 0, mSourceStream, mDestinationStream);
                return SUCCESS;
            }

            Logger.Debug("Initializing new GPT partition table on destination...");
            var destPartitionTable = GuidPartitionTable.Initialize(mDestinationStream,
                destGeometry);
            
            var availableSpace = destGeometry.Capacity;
            var usedSpace = 0L;

            // correct available space by remove unusable sectors
            // heading sectors
            availableSpace -= (destPartitionTable.FirstUsableSector
                * destGeometry.BytesPerSector);

            // ending sectors
            availableSpace -= ((destGeometry.TotalSectorsLong
                - destPartitionTable.FirstUsableSector
                - destPartitionTable.LastUsableSector)
                * destGeometry.BytesPerSector);

            // 1. calculate unresizeable space if we are required too
            if (destGeometry.Capacity < geometry.Capacity)
            {
                Logger.Info("Calculating space for new destination partitions...");

                var unresizeableSpace = 0L;
                for (int i = 0; i < partitionTable.Count; i++)
                {
                    var srcPartition = (GuidPartitionInfo)partitionTable[i];
                    var srcPartitionStream = srcPartition.Open();
                    var srcPartitionType = GetWellKnownPartitionType(srcPartition);
                    var srcPartitionSize = srcPartition.SectorCount *
                            geometry.BytesPerSector;

                    var isResizeable = true;

                    // If the file system is not support, we have to perform
                    // a byte-for-byte cloning and cannot resize this partition
                    if (!DiscHandler.IsStreamSupportedFileSystem(srcPartitionStream))
                    {
                        Logger.Verbose("Partition{0}: contains no or unsupported file system");
                        isResizeable = false;
                    }

                    // If we have a critical partition, we shouldn't resize that either
                    if (!IsSupportedPartitionType(srcPartitionType))
                    {
                        Logger.Verbose("Partition{0}: unsupported partition type");
                        isResizeable = false;
                    }

                    // if caller want to do a byte-for-byte clone, mark every partition
                    // as unresizeable
                    if (mForceExactCloning)
                        isResizeable = false;

                    Logger.Debug("Partition #{0} on source: isResizeable={1}", i,
                            isResizeable);

                    if (!isResizeable)
                    {
                        // if it's not resizeable, account the space and continue
                        unresizeableSpace += srcPartitionSize;
                        Logger.Debug("Partition #{0} on source: unresizeable, size is {1} " + 
                            "(total unresizable {2}/used {3})", i,
                                srcPartitionSize, unresizeableSpace, usedSpace);
                    }
                    else
                    {
                        // if it is resizeable, we have to report how much space we need
                        var srcPartitionFS = DiscHandler.GetFileSystem(srcPartitionStream);
                        usedSpace += srcPartitionFS.UsedSpace;

                        Logger.Debug("Partition #{0} on source: resizeable, space is {1} " + 
                            "(total unresizable {2}/used {3})", i,
                                usedSpace, unresizeableSpace, usedSpace);
                    }
                }

                // reduce the dynamically available space ...
                availableSpace -= unresizeableSpace;

                // ... and assert it
                if (availableSpace < 0)
                {
                    throw new InvalidOperationException("Cannot clone, destination is " +
                        String.Format("{0:#,##0}", -availableSpace) + " Bytes too small");
                }

                // assert the used space too
                if (availableSpace - usedSpace < 0)
                {
                    throw new InvalidOperationException("Cannot clone, destination is " +
                        String.Format("{0:#,##0}", availableSpace - usedSpace) +
                        " Bytes too small");
                }
            }
            else
            {
                Logger.Info("Destination can contain source, no need to resize partitions!");
            }

            // 2. calculate the size for each new partition
            var destPartitionSizes = new long[partitionTable.Count];
            for (int i = 0; i < partitionTable.Count; i++)
            {
                var srcPartition = (GuidPartitionInfo)partitionTable[i];
                var srcPartitionStream = srcPartition.Open();
                var srcPartitionType = GetWellKnownPartitionType(srcPartition);
                var srcPartitionSize = srcPartition.SectorCount *
                        geometry.BytesPerSector;

                // if the destination can take more data, skip every check
                if (geometry.Capacity <= destGeometry.Capacity)
                {
                    Logger.Debug("Partition{0}: Destination can contain full partition, continue...", i);

                    destPartitionSizes[i] = srcPartitionSize;
                    availableSpace -= srcPartitionSize;
                    continue;
                }

                var isResizeable = true;

                // If the device-systme is not support, we have to perform
                // a byte-for-byte cloning and cannot resize this partition
                if (!DiscHandler.IsStreamSupportedFileSystem(srcPartitionStream))
                    isResizeable = false;

                // If we have a critical partition, we shouldn't resize that either
                if (!IsSupportedPartitionType(srcPartitionType))
                    isResizeable = false;

                // if caller want to do a byte-for-byte clone, mark every partition
                // as unresizeable
                if (mForceExactCloning)
                    isResizeable = false;

                Logger.Debug("Partition #{0} on source: isResizeable={1}", i,
                        isResizeable);

                // If our friend is not resizeable, set size and stop processing it
                if (!isResizeable)
                {
                    destPartitionSizes[i] = srcPartitionSize;
                    Logger.Debug("Partition #{0} on source: unresizeable, size is {1} " +
                        "(total available {2}/used {3})", i,
                            usedSpace, availableSpace, usedSpace);

                    // DO NOT ALIGN <availableSpace> HERE!!!
                    // Has already been done in step 1
                    continue;
                }

                //
                // OK. If we are here, a resizeable partition awaits processing
                //
                var srcPartitionFS = DiscHandler.GetFileSystem(srcPartitionStream);

                // First we need to check if we need to reqsize the current partition.
                // For that, calculate the space that is left for future partitions. If the
                // result is OK (less or equal to the available size), we can skip
                // resizing the the current one.
                // Make sure to remove the factor of this partition from usedSpace first
                if (((usedSpace - srcPartitionFS.UsedSpace) + srcPartitionSize) <= availableSpace)
                {
                    // update usedSpace
                    usedSpace -= srcPartitionFS.UsedSpace;

                    // align availableSpace
                    availableSpace -= srcPartitionSize;

                    // we are good to skip resizing this one. assign size and early exit
                    destPartitionSizes[i] = srcPartitionSize;
                    Logger.Debug(
                        "Partition #{0} on source: resizeable, space is {1}, size is {2} " +
                        "(total available {3}/used {4})", i,
                        srcPartitionFS.UsedSpace, srcPartitionSize, availableSpace, usedSpace);

                    continue;
                }

                // So this partition is too large, let's resize it to the biggest size possible
                var maxPartitionSize = Math.Max(
                    // Occupied space is the absolute minimal space we need
                    srcPartitionFS.UsedSpace,

                    // This is the largest space possible. Take the still available space
                    // and remove still required space, while also remove the factor for
                    // the current partition
                    availableSpace - (usedSpace - srcPartitionFS.UsedSpace)
                );

                Logger.Debug(
                    "Partition #{0} on source: resizeable, max. space is {1} " +
                    "(total available {2}/used {3})", i,
                    maxPartitionSize, availableSpace, usedSpace);

                // update usedSpace
                usedSpace -= srcPartitionFS.UsedSpace;

                // align availableSpace
                availableSpace -= maxPartitionSize;

                destPartitionSizes[i] = maxPartitionSize;
            }

            // a last assert of the available space, just to be sure
            if (availableSpace < 0)
            {
                throw new InvalidOperationException("Cannot clone, destination is " +
                    String.Format("{0:#.##0}", -availableSpace) + " Bytes too small");
            }

            // 2. create the new partitions with the aligned sizes
            for (int i = 0; i < partitionTable.Count; i++)
            {
                var srcPartition = (GuidPartitionInfo)partitionTable[i];
                var srcPartitionStream = srcPartition.Open();
                var srcPartitionType = GetWellKnownPartitionType(srcPartition);

                // manueal type adjusting
                if (NtfsFileSystem.Detect(srcPartitionStream))
                    srcPartitionType = WellKnownPartitionType.WindowsNtfs;
                else if (FatFileSystem.Detect(srcPartitionStream))
                    srcPartitionType = WellKnownPartitionType.WindowsFat;

                var destPartitionSize = destPartitionSizes[i];

                Logger.Debug("Creating partition table: #{0}; {1}/{2}@0x{3}-{4}", i,
                        srcPartition.Name, srcPartition.Identity,
                        srcPartition.FirstSector.ToString("X2"), destPartitionSize);
                destPartitionTable.Create(
                    destPartitionSize,
                    srcPartitionType,
                    true //doesn't matter on GPT tables
                );
            }

            // 3. make sure the count of partitions is the same
            if (partitionTable.Count != destPartitionTable.Count)
                throw new InvalidOperationException(
                    "Failed to create proper GUID partition table");

            // 4. do the real cloning
            for (int i = 0; i < destPartitionTable.Count; i++)
            {
                var srcPartition = (GuidPartitionInfo)partitionTable[i];
                var srcPartitionStream = srcPartition.Open();
                var srcPartitionType = GetWellKnownPartitionType(srcPartition);

                var destPartition = (GuidPartitionInfo)destPartitionTable[i];
                var destPartitionStream = destPartition.Open();
                var destPartitionSize = destPartitionSizes[i];

                var requiresExactCloning = false;

                // To support all possible file-systems, perform a byte-for-byte
                // cloning if the file-system is not supported by us.
                if (!DiscHandler.IsStreamSupportedFileSystem(srcPartitionStream))
                    requiresExactCloning = true;

                // If we have a critical partition, we should skip that one too
                if (srcPartitionType == WellKnownPartitionType.BiosBoot ||
                        srcPartitionType == WellKnownPartitionType.EfiSystem ||
                        srcPartitionType == WellKnownPartitionType.MicrosoftReserved)
                    requiresExactCloning = true;

                // if caller want to do a byte-for-byte clone, let's enable it
                if (mForceExactCloning)
                    requiresExactCloning = true;

                if (requiresExactCloning)
                {
                    var taskId = (mTaskIdCounter++);
                    Logger.Info("[{0}/{1}] cp-fs  : {2}/{3}@0x{4}", i, taskId,
                        srcPartition.Name, srcPartition.Identity,
                        srcPartition.FirstSector.ToString("X2"));

                    CloneStream(i, taskId, srcPartitionStream, destPartitionStream);
                }
                else
                {
                    var srcPartitionFS = DiscHandler.GetFileSystem(srcPartitionStream);
                    var srcPartitionBootCode = srcPartitionFS.ReadBootCode();

                    var destPartitionFS = DiscHandler.FormatFileSystemWithTemplate(
                        srcPartitionStream, destPartitionStream, destPartition.FirstSector,
                        destPartition.SectorCount, destPartitionSize
                    );

                    // Tracks all NTFS file IDs for hard link recognition
                    // <Source FS File ID, Destination FS File ID>
                    var ntfsHardlinkTracker = new Dictionary<uint, uint>();

                    // last clone each single with here
                    Action<DiscDirectoryInfo, DiscDirectoryInfo> cloneSrcFsToDestFs = null;
                    cloneSrcFsToDestFs = new Action<DiscDirectoryInfo, DiscDirectoryInfo>(
                        (sourceDir, destDir) =>
                    {
                        // recursive enumeration. save to create directory without checks for
                        // parent directories.
                        var taskId = (mTaskIdCounter++);
                        Logger.Info("[{0}/{1}] mk-dir : {2}", i, taskId, destDir.FullName);

                        destDir.Create();

                        // copy files if there are any
                        foreach (var sourceFile in sourceDir.GetFiles())
                        {
                            var skipCopying = false;
                            taskId = (mTaskIdCounter++);

                            var sourceFileStream = sourceFile.Open(FileMode.Open,
                                FileAccess.Read);

                            var destFileName = Path.Combine(destDir.FullName, sourceFile.Name);
                            Logger.Info("[{0}/{1}] mk-file: {2}", i, taskId,
                                destFileName);

                            var destFileStream = destPartitionFS.OpenFile(
                                destFileName,
                                FileMode.Create, FileAccess.Write);
                            var destFile = new DiscFileInfo(destPartitionFS, destFileName);

                            // NTFS hard link handling
                            if (destPartitionFS is NtfsFileSystem)
                            {
                                var ntfsSourceFS = (NtfsFileSystem)srcPartitionFS;
                                var ntfsDestFS = (NtfsFileSystem)destPartitionFS;

                                var sourceFileNtfsEntry = ntfsSourceFS.GetDirectoryEntry(
                                    sourceFile.FullName);
                                var sourceFileNtfsRef = ntfsSourceFS.GetFile(
                                    sourceFileNtfsEntry.Reference);

                                var destFileNtfsEntry = ntfsDestFS.GetDirectoryEntry(
                                    destFile.FullName);
                                var destFileNtfsRef = ntfsDestFS.GetFile(
                                    destFileNtfsEntry.Reference);

                                var sourceFileNtfsId = sourceFileNtfsRef.IndexInMft;

                                // check if this files was already processed once
                                if (ntfsHardlinkTracker.ContainsKey(sourceFileNtfsId))
                                {
                                    var trackedDestFileNtfsRef = ntfsDestFS.GetFile(
                                        ntfsHardlinkTracker[sourceFileNtfsId]);

                                    // if we have a hardlink-match, close the old stream
                                    // and delete the file
                                    destFileStream.Close();
                                    destFile.Delete();

                                    // then create the hardlink and mention that we don't
                                    // want/need to copy the content anymore
                                    Logger.Info("[{0}/{1}] mk-lnk : {2} => {3}", i, taskId, 
                                        sourceFileNtfsRef.Names[0], destFile.FullName);
                                    
                                    ntfsDestFS.CreateHardLink(
                                        trackedDestFileNtfsRef.DirectoryEntry,
                                        destFile.FullName);
                                    skipCopying = true;
                                }
                                else
                                {
                                    Logger.Verbose("[{0}/{1}] rg-lnk : {2}#{3} -> {4}#{5}", i, taskId,
                                        sourceFileNtfsRef.Names[0], sourceFileNtfsRef.IndexInMft,
                                        destFileNtfsRef.Names[0], destFileNtfsRef.IndexInMft);
                                    
                                    // if not, track it
                                    ntfsHardlinkTracker.Add(sourceFileNtfsRef.IndexInMft,
                                        destFileNtfsRef.IndexInMft);
                                }
                            }

                            if (!skipCopying)
                            {
                                Logger.Info("[{0}/{1}] cp-file: {2}", i, taskId, destFile.FullName);
                                CloneStream(i, taskId, sourceFileStream, destFileStream);
                            }

                            // clone basic file informationsdestFile
                            Logger.Verbose("[{0}/{1}] cp-meta: {2}", i, taskId, destFile.FullName);
                            destFile.Attributes = sourceFile.Attributes;
                            destFile.CreationTime = sourceFile.CreationTime;
                            destFile.CreationTimeUtc = sourceFile.CreationTimeUtc;
                            destFile.IsReadOnly = sourceFile.IsReadOnly;
                            destFile.LastAccessTime = sourceFile.LastAccessTime;
                            destFile.LastAccessTimeUtc = sourceFile.LastAccessTimeUtc;
                            destFile.LastWriteTime = sourceFile.LastWriteTime;
                            destFile.LastWriteTimeUtc = sourceFile.LastWriteTimeUtc;

                            // file-system based cloning
                            if (destPartitionFS is NtfsFileSystem)
                            {
                                Logger.Verbose("[{0}/{1}] cp-fsec: {2}", i, taskId, destFile.FullName);
                                var ntfsSourceFS = (NtfsFileSystem)srcPartitionFS;
                                var ntfsDestFS = (NtfsFileSystem)destPartitionFS;

                                var destFileNtfsEntry = ntfsDestFS.GetDirectoryEntry(
                                    destFile.FullName);
                                var destFileNtfsRef = ntfsDestFS.GetFile(
                                    destFileNtfsEntry.Reference);

                                // clone security descriptors
                                var sourceNtfsSecurity = ntfsSourceFS.GetSecurity(
                                    sourceFile.FullName);
                                ntfsDestFS.SetSecurity(destFile.FullName, sourceNtfsSecurity);

                                // clone short names if destination file is not a hard link
                                if (destFileNtfsEntry.Details.FileNameNamespace !=
                                    FileNameNamespace.Posix || !destFileNtfsRef.HasWin32OrDosName)
                                {
                                    Logger.Verbose("[{0}/{1}] cp-shrt: {2}", i, taskId, destFile.FullName);
                                    var sourceNtfsShortName = ntfsSourceFS.GetShortName(
                                        sourceFile.FullName);

                                    if (sourceNtfsShortName != null)
                                    {
                                        ntfsSourceFS.SetShortName(destFile.FullName,
                                            sourceNtfsShortName);
                                    }
                                }
                            }
                        }

                        // advance recursion into directories
                        foreach (var sourceDirectory in sourceDir.GetDirectories())
                        {
                            if (srcPartitionFS is FatFileSystem)
                            {
                                // Don't copy SYSTEM~1 on FAT. Just don't do it...
                                if (sourceDirectory.Name.Equals("SYSTEM~1"))
                                    continue;
                            }

                            cloneSrcFsToDestFs(sourceDirectory,
                                new DiscDirectoryInfo(
                                    destPartitionFS,
                                    Path.Combine(destDir.FullName, sourceDirectory.Name)
                                )
                            );
                        }
                    });

                    cloneSrcFsToDestFs(srcPartitionFS.Root, destPartitionFS.Root);
                }
            }

            return SUCCESS;
        }

        private static void CloneStream(int partition, long taskId, Stream source, Stream destination)
        {
            var total = source.Length;
            var buffer = new byte[mBufferSize];

            if (source.Length == 0)
                return;

            source.Seek(0, SeekOrigin.Begin);
            destination.Seek(0, SeekOrigin.Begin);

            do
            {
                var buffSizeDiff = (total - source.Position);
                if (buffSizeDiff < buffer.LongLength && buffSizeDiff > 0)
                    buffer = new byte[buffSizeDiff];

                var read = source.Read(buffer, 0, buffer.Length);
                if (read <= 0 && source.Position < total)
                {
                    Logger.Error("[{0}/{1}] read-err @ 0x{2:X}+{3}", partition, taskId,
                        source.Position, buffer.Length);
                    continue;
                }

                destination.Write(buffer, 0, read);

                CloneProgressEvent?.Invoke(new CloneProgressEventArgs(partition, taskId,
                    total, source.Position));
            } while (source.Position < total);

            destination.Flush();
        }
        
        [Verb("clone", HelpText = "Performs intelligent or full clone")]
        public sealed class Options : BaseOptions
        {

            [Value(0, Default = null, HelpText = "Path to the name of drive which the data should be cloned from", Required = true)]
            public string Source { get; set; }

            [Value(1, Default = null, HelpText = "Path to the name of drive which the data should be cloned to", Required = true)]
            public string Destination { get; set; }

            [Option('b', "buffer-size", Default = "64K", HelpText = "Size of I/O-buffer used for copying data", Required = true)]
            public string BufferSizeString { get; set; }

            public long BufferSize
            {
                get => ParseSizeString(BufferSizeString);
            }

            [Option('e', "force-exact", Default = false, HelpText = "Forces a byte-for-byte cloning of all data. Disables the possibility of fitting data onto smaller targets.")]
            public bool ForceExactCloning { get; set; }

            [Option('f', "full-clone", Default = false, HelpText = "Forces a byte-for-byte for the full source, including partition tables")]
            public bool FullClone { get; set; }

            [Option('g', "fast-refresh", Default = false, HelpText = "Fast-refresh the outputted progress. May slow down the erase process.", Required = false)]
            public bool FastRefresh { get; set; }

        }

    }

}
