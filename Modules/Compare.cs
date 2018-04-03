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
using System.Reflection;

using CommandLine;

using DiscUtils;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;

using nDiscUtils.IO;
using nDiscUtils.Options;

using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.nConsole;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class Compare
    {

        private static string mLeftPath;
        private static Stream mLeftStream;

        private static string mRightPath;
        private static Stream mRightStream;

        private static string mSummaryFile;
        private static Stream mSummaryStream;
        private static StreamWriter mSummaryWriter;

        private static long mBufferSize;

        private static long recordedWarnings;
        private static long recordedErrors;
        private static long processedChecks;

        public static int Run(Options opts)
        {
            RunHelpers(opts);
            OpenNewConsoleBuffer();
            InitializeConsole();

            mLeftPath = opts.Left;
            mRightPath = opts.Right;
            mSummaryFile = opts.SummaryFile;

            mLeftStream = OpenPath(mLeftPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            mRightStream = OpenPath(mRightPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            mSummaryStream = OpenPath(mSummaryFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            mSummaryWriter = new StreamWriter(mSummaryStream);

            mBufferSize = opts.BufferSize;

            recordedWarnings = 0;
            recordedErrors = 0;
            processedChecks = 0;

            var beginDateTime = DateTime.Now;
            WriteBegin(beginDateTime);

            var returnCode = StartInternal();

            var endDateTime = DateTime.Now;
            WriteEnd(beginDateTime, endDateTime);

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
            mLeftStream.Flush();
            mLeftStream.Close();
            mLeftStream.Dispose();
            mLeftStream = null;

            mRightStream.Flush();
            mRightStream.Close();
            mRightStream.Dispose();
            mRightStream = null;

            mLeftPath = "";
            mRightPath = "";
        }

        private static int StartInternal()
        {
            Info("====== Starting Partition Table Checks ======");

            DoingCheck();
            if (!GuidPartitionTable.Detect(mLeftStream))
            {
                Error("Could not find valid GUID Partition Table on left stream");
                return ERROR;
            }
            Info("Found valid GUID Partition Table on left stream");

            DoingCheck();
            if (!GuidPartitionTable.Detect(mRightStream))
            {
                Error("Could not find valid GUID Partition Table on right stream");
                return ERROR;
            }
            Info("Found valid GUID Partition Table on right stream");

            var leftGeometry = GuidPartitionTable.DetectGeometry(mLeftStream);
            var leftPartitionTable = new GuidPartitionTable(mLeftStream, leftGeometry);

            var rightGeometry = GuidPartitionTable.DetectGeometry(mRightStream);
            var rightPartitionTable = new GuidPartitionTable(mRightStream, rightGeometry);

            // adjust length
            if (FixedLengthStream.IsFixedDiskStream(mLeftStream))
                mLeftStream.SetLength(leftGeometry.Capacity);
            if (FixedLengthStream.IsFixedDiskStream(mRightStream))
                mRightStream.SetLength(leftGeometry.Capacity);

            DoingCheck();
            if (leftPartitionTable.Count != rightPartitionTable.Count)
            {
                Error("Non-equal count of partitions (Left: {0}, Right {1})",
                    leftPartitionTable.Count, rightPartitionTable.Count);
                return ERROR;
            }
            Info("Matching count of partitions ({0})", leftPartitionTable.Count);

            Info("======    Starting Partition Checks    ======");
            for (int i = 0; i < leftPartitionTable.Count; i++)
            {
                var leftPartition = leftPartitionTable[i];
                var rightPartition = rightPartitionTable[i];

                DoingCheck();
                if (leftPartition.BiosType != rightPartition.BiosType)
                {
                    Error("Partition{0}: Non-matching BIOS partition type " +
                        "(Left: 0x{1:X2}, Right: 0x{2:X2})", i,
                        leftPartition.BiosType, rightPartition.BiosType);
                    return ERROR;
                }
                Info("Partition{0}: Matching BIOS partition type (0x{1:X2})", i,
                    leftPartition.BiosType);

                DoingCheck();
                if (leftPartition.GuidType != rightPartition.GuidType)
                {
                    Error("Partition{0}: Non-matching GUID partition type " +
                        "(Left: {1}, Right: {2})", i,
                        leftPartition.GuidType, rightPartition.GuidType);
                    return ERROR;
                }
                Info("Partition{0}: Matching GUID partition type ({1})", i,
                    leftPartition.GuidType);

                DoingCheck();
                if (leftPartition.VolumeType != rightPartition.VolumeType)
                {
                    Error("Partition{0}: Non-matching physical partition type " +
                        "(Left: 0x{1:X}/{1}, Right: 0x{2:X}/{2})", i,
                        leftPartition.VolumeType, rightPartition.VolumeType);
                    return ERROR;
                }
                Info("Partition{0}: Matching physical partition type (0x{1:X}/{1})", i,
                    leftPartition.VolumeType);
            }

            Info("======   Starting File System Checks   ======");
            for (int i = 0; i < leftPartitionTable.Count; i++)
            {
                var leftPartition = leftPartitionTable[i];
                var leftPartitionStream = leftPartition.Open();
                var leftPartitionValidFS = 
                    DiscHandler.IsStreamSupportedFileSystem(leftPartitionStream);

                var rightPartition = rightPartitionTable[i];
                var rightPartitionStream = rightPartition.Open();
                var rightPartitionValidFS =
                    DiscHandler.IsStreamSupportedFileSystem(rightPartitionStream);

                if (!leftPartitionValidFS)
                    Info("Partition{0}: Left stream does not contain supported file system", i);

                if (!rightPartitionValidFS)
                    Info("Partition{0}: Right stream does not contain supported file system", i);

                if (leftPartitionValidFS && rightPartitionValidFS)
                {
                    Info("Partition{0}: Running checks on file-system layer", i);

                    var leftFileSystem = DiscHandler.GetFileSystem(leftPartitionStream);
                    var rightFileSystem = DiscHandler.GetFileSystem(rightPartitionStream);

                    if (leftFileSystem.UsedSpace != rightFileSystem.UsedSpace)
                        Warning("Partition{0}: Non-matching amount of used space " +
                            "(Left: 0x{1:X2}/{2}, Right: 0x{3:X2}/{4})", i,
                            leftFileSystem.UsedSpace,
                            FormatBytes(leftFileSystem.UsedSpace, 3),
                            rightFileSystem.UsedSpace,
                            FormatBytes(rightFileSystem.UsedSpace, 3));

                    Action<DiscDirectoryInfo, DiscDirectoryInfo> traverseFiles = null;
                    traverseFiles = new Action<DiscDirectoryInfo, DiscDirectoryInfo>(
                        (leftBaseDir, rightBaseDir) =>
                        {
                            var leftFiles = leftBaseDir.GetFiles();
                            var rightFiles = rightBaseDir.GetFiles();

                            DoingCheck();
                            if (leftFiles.Length != rightFiles.Length)
                            {
                                Warning("Partition{0}: Non-matching amount of files in {1} " +
                                    "(Left: {2}, Right: {3})", i, leftBaseDir.FullName,
                                    leftFiles.Length, rightFiles.Length);

                                var checkedFiles = new List<DiscFileInfo>();
                                foreach (var leftFile in leftFiles)
                                {
                                    var matchingRightFiles = rightFiles.Where(f =>
                                        f.Name == leftFile.Name);

                                    DoingCheck();
                                    if (matchingRightFiles.Count() >= 2)
                                    {
                                        Error("Partition{0}:{1}: Found invalid file entry in " +
                                            "file system, looked up file with {2} results",
                                            i, leftBaseDir.FullName, matchingRightFiles.Count());
                                    }

                                    DoingCheck();
                                    if (matchingRightFiles.Count() == 1)
                                    {
                                        checkedFiles.Add(leftFile);

                                        var rightFile = matchingRightFiles.First();
                                        var leftFileStream = leftFile.OpenRead();
                                        var rightFileStream = rightFile.OpenRead();

                                        DoByteComparison(leftFileStream, rightFileStream, i,
                                            leftFile.FullName);

                                        leftFileStream.Close();
                                        rightFileStream.Close();
                                        
                                        DoMetaDataComparison(i, leftFile, rightFile);

                                        if (leftFileSystem is NtfsFileSystem)
                                        {
                                            DoNtfsSecurityComparison(i,
                                                (NtfsFileSystem)leftFileSystem, leftFile,
                                                (NtfsFileSystem)rightFileSystem, rightFile);
                                        }
                                    }
                                    else // == 0
                                    {
                                        Warning("Partition{0}:{1}: Found file in left " +
                                            "file system only", i, leftFile.FullName);
                                    }
                                }

                                foreach (var rightFile in rightFiles)
                                {
                                    DoingCheck();
                                    if (!checkedFiles.Any(f => f.Name == rightFile.Name))
                                    {
                                        Warning("Partition{0}:{1}: Found file in right " +
                                            "file system only", i, rightFile.FullName);
                                    }
                                }
                            }
                            else
                            {
                                for (int j = 0; j < leftFiles.Length; j++)
                                {
                                    var leftFile = leftFiles[j];
                                    var leftFileStream = leftFile.OpenRead();
                                    var rightFile = rightFiles[j];
                                    var rightFileStream = rightFile.OpenRead();

                                    DoByteComparison(leftFileStream, rightFileStream, i,
                                        leftFile.FullName);

                                    leftFileStream.Close();
                                    rightFileStream.Close();

                                    DoMetaDataComparison(i, leftFile, rightFile);

                                    if (leftFileSystem is NtfsFileSystem)
                                    {
                                        DoNtfsSecurityComparison(i, 
                                            (NtfsFileSystem)leftFileSystem, leftFile,
                                            (NtfsFileSystem)rightFileSystem, rightFile);
                                    }
                                }
                            }

                            // advance recursion into directories
                            var leftDirectories = leftBaseDir.GetDirectories();
                            var rightDirectories = rightBaseDir.GetDirectories();

                            DoingCheck();
                            if (leftDirectories.Length != rightDirectories.Length)
                            {
                                Warning("Partition{0}: Non-matching amount of directories in {1} " +
                                    "(Left: {2}, Right: {3})", i, leftBaseDir.FullName,
                                    leftDirectories.Length, rightDirectories.Length);

                                var checkedDirectories = new List<DiscFileInfo>();
                                foreach (var leftDirectory in leftDirectories)
                                {
                                    var matchingRightFiles = rightDirectories.Where(f =>
                                        f.Name == leftDirectory.Name);

                                    DoingCheck();
                                    if (matchingRightFiles.Count() >= 2)
                                    {
                                        Error("Partition{0}:{1}: Found invalid directory entry in " +
                                            "file system, looked up file with {2} results",
                                            i, leftBaseDir.FullName, matchingRightFiles.Count());
                                    }

                                    DoingCheck();
                                    if (matchingRightFiles.Count() == 1)
                                    {
                                        var rightDirectory = matchingRightFiles.First();
                                        traverseFiles(leftDirectory, rightDirectory);
                                    }
                                    else // == 0
                                    {
                                        Warning("Partition{0}:{1}: Found directory in left " +
                                            "file system only", i, leftDirectory.FullName);
                                    }
                                }

                                foreach (var rightFile in rightDirectories)
                                {
                                    DoingCheck();
                                    if (!checkedDirectories.Any(f => f.Name == rightFile.Name))
                                    {
                                        Warning("Partition{0}:{1}: Found file in right " +
                                            "file system only", i, rightFile.FullName);
                                    }
                                }
                            }
                            else
                            {
                                for (int j = 0; j < leftDirectories.Length; j++)
                                {
                                    var leftDirectory = leftDirectories[j];
                                    var rightDirectory = leftDirectories[j];
                                    
                                    traverseFiles(leftDirectory, rightDirectory);
                                }
                            }
                        });

                    traverseFiles(leftFileSystem.Root, rightFileSystem.Root);
                }
                else
                {
                    Info("Partition{0}: Running checks on data layer", i);

                    // This can can be run because of the fact that we don't resize
                    // unsupported partitions but run byte-for-byte copy with them
                    DoByteComparison(leftPartitionStream, rightPartitionStream, i, null);
                }
            }

            return SUCCESS;
        }

        private static void DoMetaDataComparison(int partition, DiscFileInfo left, DiscFileInfo right)
        {
            Info("Partition{0}:{1}: Comparing file meta data", partition, left.FullName);

            DoingCheck();
            if (left.Attributes != right.Attributes)
            {
                Error("Partition{0}:{1}: Non-matching file attributes" +
                    "(Left: 0x{2:X2}, Right: 0x{3:X2})", partition,
                    left.FullName, left.Attributes, right.Attributes);
            }

            DoingCheck();
            if (left.CreationTime != right.CreationTime)
            {
                Error("Partition{0}:{1}: Non-matching creation time" +
                    "(Left: {2:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}, " +
                    "Right: {3:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff})",
                    partition, left.FullName, left.CreationTime, right.CreationTime);
            }

            DoingCheck();
            if (left.CreationTimeUtc != right.CreationTimeUtc)
            {
                Error("Partition{0}:{1}: Non-matching UTC creation time" +
                    "(Left: {2:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}, " +
                    "Right: {3:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff})",
                    partition, left.FullName, left.CreationTimeUtc, right.CreationTimeUtc);
            }

            DoingCheck();
            if (left.IsReadOnly != right.IsReadOnly)
            {
                Error("Partition{0}:{1}: Non-matching read-only flag" +
                    "(Left: {2}, Right: {3})",
                    partition, left.FullName, left.IsReadOnly, right.IsReadOnly);
            }

            DoingCheck();
            if (left.LastAccessTime != right.LastAccessTime)
            {
                Error("Partition{0}:{1}: Non-matching last access time" +
                    "(Left: {2:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}, " +
                    "Right: {3:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff})",
                    partition, left.FullName, left.LastAccessTime, right.LastAccessTime);
            }

            DoingCheck();
            if (left.LastAccessTimeUtc != right.LastAccessTimeUtc)
            {
                Error("Partition{0}:{1}: Non-matching UTC last access time" +
                    "(Left: {2:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}, " +
                    "Right: {3:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff})",
                    partition, left.FullName, left.LastAccessTimeUtc, right.LastAccessTimeUtc);
            }

            DoingCheck();
            if (left.LastWriteTime != right.LastWriteTime)
            {
                Error("Partition{0}:{1}: Non-matching last write time" +
                    "(Left: {2:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}, " +
                    "Right: {3:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff})",
                    partition, left.FullName, left.LastWriteTime, right.LastWriteTime);
            }

            DoingCheck();
            if (left.LastWriteTimeUtc != right.LastWriteTimeUtc)
            {
                Error("Partition{0}:{1}: Non-matching UTC last write time" +
                    "(Left: {2:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}, " +
                    "Right: {3:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff})",
                    partition, left.FullName, left.LastWriteTimeUtc, right.LastWriteTimeUtc);
            }
        }

        private static void DoNtfsSecurityComparison(int partition, 
            NtfsFileSystem leftFileSystem, DiscFileInfo left,
            NtfsFileSystem rightFileSystem, DiscFileInfo right)
        {
            var leftEntry = leftFileSystem.GetDirectoryEntry(left.FullName);
            var leftRef = leftFileSystem.GetFile(leftEntry.Reference);

            var rightEntry = rightFileSystem.GetDirectoryEntry(right.FullName);
            var rightRef = rightFileSystem.GetFile(rightEntry.Reference);

            Info("Partition{0}:{1}: Comparing NTFS meta data", partition, left.FullName);

            // Disable these checks there are problems with the short names DiscUtils assigns
            /* // hard links
            DoingCheck();
            var leftRefHardLinkCount = leftRef.HardLinkCount;
            if (leftRef.HasWin32OrDosName)
                leftRefHardLinkCount--; // remove the legacy short name

            var rightRefHardLinkCount = rightRef.HardLinkCount;
            if (rightRef.HasWin32OrDosName)
                rightRefHardLinkCount--; // remove the legacy short name

            if (leftRefHardLinkCount != rightRefHardLinkCount)
            {
                Error("Partition{0}:{1}: Non-matching hard link count" +
                    "(Left: {2}, Right: {3})",
                    partition, left.FullName, leftRefHardLinkCount, rightRefHardLinkCount);
            }

            // file names
            DoingCheck();
            var leftRefNamesCount = leftRef.Names.Count;
            if (leftRef.HasWin32OrDosName)
                leftRefNamesCount--; // remove the legacy short name

            var rightRefNamesCount = rightRef.Names.Count;
            if (rightRef.HasWin32OrDosName)
                rightRefNamesCount--; // remove the legacy short name

            if (leftRefNamesCount != rightRefNamesCount)
            {
                Error("Partition{0}:{1}: Non-matching file names count" +
                    "(Left: {2}, Right: {3})",
                    partition, left.FullName, leftRefNamesCount, rightRefNamesCount);
            } */

            // security
            var leftSecurity = leftFileSystem.GetSecurity(left.FullName);
            var rightSecurity = rightFileSystem.GetSecurity(right.FullName);

            DoingCheck();
            if (leftSecurity.BinaryLength != rightSecurity.BinaryLength)
            {
                Error("Partition{0}:{1}: Non-matching security binary length" +
                    "(Left: 0x{2:X}, Right: 0x{3:X})",
                    partition, left.FullName,
                    leftSecurity.BinaryLength, rightSecurity.BinaryLength);
            }

            DoingCheck();
            if (leftSecurity.ControlFlags != rightSecurity.ControlFlags)
            {
                Error("Partition{0}:{1}: Non-matching security control flags" +
                    "(Left: 0x{2:X}, Right: 0x{3:X})",
                    partition, left.FullName,
                    leftSecurity.ControlFlags, rightSecurity.ControlFlags);
            }

            DoingCheck();
            if (leftSecurity.Group != null && rightSecurity.Group != null)
            {
                if (leftSecurity.Group.Value != rightSecurity.Group.Value)
                {
                    Error("Partition{0}:{1}: Non-matching security group name" +
                        "(Left: {2}, Right: {3})",
                        partition, left.FullName,
                        leftSecurity.Group.Value, rightSecurity.Group.Value);
                }
            }
            else if ((leftSecurity.Group == null && rightSecurity.Group != null) ||
                (leftSecurity.Group != null && rightSecurity.Owner == null))
            {
                Warning("Partition{0}:{1}: No valid NTFS security group found",
                    partition, left.FullName);
            }

            DoingCheck();
            if (leftSecurity.Owner != null && rightSecurity.Owner != null)
            {
                if (leftSecurity.Owner.Value != rightSecurity.Owner.Value)
                {
                    Error("Partition{0}:{1}: Non-matching security owner name" +
                        "(Left: {2}, Right: {3})",
                        partition, left.FullName,
                        leftSecurity.Group.Value, rightSecurity.Group.Value);
                }
            }
            else if ((leftSecurity.Owner == null && rightSecurity.Owner != null) ||
                (leftSecurity.Owner != null && rightSecurity.Owner == null))
            {
                Warning("Partition{0}:{1}: No valid NTFS security owner found",
                    partition, left.FullName);
            }

            DoingCheck();
            if (leftSecurity.DiscretionaryAcl != null && rightSecurity.DiscretionaryAcl != null)
            {
                if (leftSecurity.DiscretionaryAcl.Count != rightSecurity.DiscretionaryAcl.Count)
                {
                    Error("Partition{0}:{1}: Non-matching count of Discretionary ACL entries" +
                        "(Left: {2}, Right: {3})",
                        partition, left.FullName,
                        leftSecurity.DiscretionaryAcl.Count, rightSecurity.DiscretionaryAcl.Count);
                }
            }
            else if ((leftSecurity.DiscretionaryAcl == null && rightSecurity.DiscretionaryAcl != null) ||
                (leftSecurity.DiscretionaryAcl != null && rightSecurity.DiscretionaryAcl == null))
            {
                Warning("Partition{0}:{1}: No valid NTFS security Discretionary ACL found",
                    partition, left.FullName);
            }

            DoingCheck();
            if (leftSecurity.SystemAcl != null && rightSecurity.SystemAcl != null)
            {
                if (leftSecurity.SystemAcl.Count != rightSecurity.SystemAcl.Count)
                {
                    Error("Partition{0}:{1}: Non-matching count of System ACL entries" +
                        "(Left: {2}, Right: {3})",
                        partition, left.FullName,
                        leftSecurity.DiscretionaryAcl.Count, rightSecurity.DiscretionaryAcl.Count);
                }
            }
            else if((leftSecurity.SystemAcl == null && rightSecurity.SystemAcl != null) ||
                (leftSecurity.SystemAcl != null && rightSecurity.SystemAcl == null))
            {
                Warning("Partition{0}:{1}: No valid NTFS security System ACL found",
                    partition, left.FullName);
            }
        }

        private static void DoByteComparison(Stream left, Stream right, int partition, string file = null)
        {
            var total = left.Length;
            var leftBuffer = new byte[mBufferSize];
            var rightBuffer = new byte[mBufferSize];
            var errored = false;

            var beginDateTime = DateTime.Now;

            DoingCheck();
            if (left.Length != right.Length)
            {
                if (file == null)
                    Error("Partition{0}: Non-matching stream length " +
                        "(Left: 0x{1:X2}/{2}, Right: 0x{3:X2}/{4})",
                        partition,
                        left.Length, FormatBytes(left.Length, 3),
                        right.Length, FormatBytes(right.Length, 3));
                else
                    Error("Partition{0}:{1}: Non-matching stream length " +
                        "(Left: 0x{2:X2}/{3}, Right: 0x{4:X2}/{5})",
                        partition, file,
                        left.Length, FormatBytes(left.Length, 3),
                        right.Length, FormatBytes(right.Length, 3));
                return;
            }

            if (file == null)
                Info("Partition{0}: Matching length (0x{1:X2}/{2})",
                    partition,
                    left.Length, FormatBytes(left.Length, 3));
            else
                Info("Partition{0}:{1}: Matching length (0x{2:X2}/{3})",
                    partition, file,
                    left.Length, FormatBytes(left.Length, 3));

            if (left.Length == 0)
                return;

            do
            {
                var leftBuffSizeDiff = (total - left.Position);
                if (leftBuffSizeDiff < leftBuffer.LongLength && leftBuffSizeDiff > 0)
                    leftBuffer = new byte[leftBuffSizeDiff];

                var leftRead = left.Read(leftBuffer, 0, leftBuffer.Length);

                DoingCheck();
                if (leftRead <= 0 && (left.Position < left.Length || 
                    (leftRead != 0 && left.Length + leftRead < left.Position)))
                {
                    if (file == null)
                        Error("Partition{0}: Failed to read from left stream @ " +
                            "(0x{1:X2}+{2})",
                            partition, left.Position, leftBuffer.Length);
                    else
                        Error("Partition{0}:{1}: Failed to read from left stream @ " +
                            "(0x{2:X2}+{3})",
                            partition, file, left.Position, leftBuffer.Length);

                    errored = true;
                    break;
                }

                var rightBuffSizeDiff = (total - right.Position);
                if (rightBuffSizeDiff < rightBuffer.LongLength && rightBuffSizeDiff > 0)
                    rightBuffer = new byte[rightBuffSizeDiff];

                var rightRead = right.Read(rightBuffer, 0, rightBuffer.Length);

                DoingCheck();
                if (rightRead <= 0 && (right.Position < right.Length || 
                    (rightRead != 0 && right.Length + rightRead < right.Position)))
                {
                    if (file == null)
                        Error("Partition{0}: Failed to read from right stream @ " +
                            "(0x{1:X2}+{2})",
                            partition, right.Position, rightBuffer.Length);
                    else
                        Error("Partition{0}:{1}: Failed to read from right stream @ " +
                            "(0x{2:X2}+{3})",
                            partition, file, right.Position, rightBuffer.Length);

                    errored = true;
                    break;
                }

                DoingCheck();
                if (leftRead != rightRead)
                {
                    if (file == null)
                        Error("Partition{0}: Non-matching data length @ " +
                            "(Left: {1}, Right: {2})",
                            partition, leftRead, rightRead);
                    else
                        Error("Partition{0}:{1}: Non-matching data length @ " +
                            "(Left: {2}, Right: {3})",
                            partition, file, leftRead, rightRead);

                    errored = true;
                    break;
                }

                DoingCheck();
                if (!EqualBytesLongUnrolled(leftBuffer, rightBuffer))
                {
                    if (file == null)
                        Error("Partition{0}: Non-matching data @ " +
                            "(Left: {1}, Right: {2})",
                            partition, leftRead, rightRead);
                    else
                        Error("Partition{0}:{1}: Non-matching data @ " +
                            "(Left: {2}, Right: {3})",
                            partition, file, leftRead, rightRead);

                    errored = true;
                    break;
                }

            } while (left.Position < left.Length && right.Position < right.Length);

            var endDateTime = DateTime.Now;

            if (!errored)
            {
                if (file == null)
                    Info("Partition{0}: Byte-check passed! ({2:hh\\:mm\\:ss\\.fffffff})",
                        partition, endDateTime.Subtract(beginDateTime));
                else
                    Info("Partition{0}:{1}: Byte-check passed! ({2:hh\\:mm\\:ss\\.fffffff})",
                        partition, file, endDateTime.Subtract(beginDateTime));
            }
        }

        private static void WriteBegin(DateTime begin)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            mSummaryWriter.WriteLine("{0} v{1}.{2}b{3}r{4} - Compiled on {5}",
                assemblyName.Name,
                assemblyName.Version.Major,
                assemblyName.Version.Minor,
                assemblyName.Version.Build,
                assemblyName.Version.Revision,
                assembly.GetLinkerTime());
            mSummaryWriter.WriteLine("");
            mSummaryWriter.WriteLine("Summary created on " + 
                "{0:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}", begin);
            mSummaryWriter.WriteLine("");
            mSummaryWriter.WriteLine(
                "=================   BEGIN OF COMPARISON   =================");
            mSummaryWriter.WriteLine("");
            mSummaryWriter.Flush();
        }

        private static void WriteEnd(DateTime begin, DateTime end)
        {
            mSummaryWriter.WriteLine("");
            mSummaryWriter.WriteLine(
                "=================    END OF COMPARISON    =================");
            mSummaryWriter.WriteLine("");
            if (recordedErrors == 0)
            {
                if (recordedWarnings == 0)
                {
                    mSummaryWriter.WriteLine("    <<<<< PASSED >>>>> (0 Errors, 0 Warnings)");
                }
                else
                {
                    mSummaryWriter.WriteLine("    <<<<< PASSED >>>>> " + 
                        "(With Warnings; 0 Errors, {0} Warning{1})",
                        recordedWarnings, (recordedWarnings == 1 ? "": "s"));
                }
            }
            else
            {
                mSummaryWriter.WriteLine("    <<<<< FAILED >>>>> ({0} Errors, {1} Warning{2})",
                    recordedErrors,
                    recordedWarnings, (recordedWarnings == 1 ? "" : "s"));
            }
            mSummaryWriter.WriteLine("");
            mSummaryWriter.WriteLine("For more information read the log and the summary.");
            mSummaryWriter.WriteLine("");

            mSummaryWriter.WriteLine("Started on {0:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}", 
                begin);
            mSummaryWriter.WriteLine("  Ended on {0:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}", 
                end);
            mSummaryWriter.WriteLine("==========================");
            mSummaryWriter.WriteLine("      Took {0:hh\\:mm\\:ss\\.fffffff}", end.Subtract(begin));
            mSummaryWriter.WriteLine("==========================");
            mSummaryWriter.WriteLine("");
            mSummaryWriter.WriteLine("============ STATISTICS ============");
            mSummaryWriter.WriteLine("");

            mSummaryWriter.WriteLine("Processed Checks:  {0}", processedChecks);

            mSummaryWriter.WriteLine("");

            mSummaryWriter.WriteLine("[WARN] Recorded Warnings: {0} ({1:0.000}%)", recordedWarnings,
                (recordedWarnings / processedChecks) * 100);

            mSummaryWriter.WriteLine("[FAIL] Recorded Errors:   {0} ({1:0.000}%)", recordedErrors,
                (recordedErrors / processedChecks) * 100);

            mSummaryWriter.WriteLine("");
            mSummaryWriter.Flush();
        }

        private static void DoingCheck()
            => processedChecks++;

        private static void Info(string format, params object[] args)
        {
            Logger.Info(format, args);
            mSummaryWriter.WriteLine(
               "{0:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}: INFO: {1}",
               DateTime.Now,
               string.Format(format, args)
            );
            mSummaryWriter.Flush();
        }

        private static void Warning(string format, params object[] args)
        {
            recordedWarnings++;
            Logger.Warn(format, args);
            mSummaryWriter.WriteLine(
               "{0:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}: WARN: {1}",
               DateTime.Now,
               string.Format(format, args)
            );
            mSummaryWriter.Flush();
        }

        private static void Error(string format, params object[] args)
        {
            recordedErrors++;
            Logger.Error(format, args);
            mSummaryWriter.WriteLine(
               "{0:dd.MM.yyyy hh\\:mm\\:ss\\.fffffff}: FAIL: {1}",
               DateTime.Now,
               string.Format(format, args)
            );
            mSummaryWriter.Flush();
        }

        [Verb("compare", HelpText = "Performs intelligent compare between two targets")]
        public sealed class Options : BaseOptions
        {
            
            [Value(0, Default = null, HelpText = "First/Left path of the disk/image which will be compared", Required = true)]
            public string Left { get; set; }
            
            [Value(1, Default = null, HelpText = "Second/Right path of the disk/image which will be compared", Required = true)]
            public string Right { get; set; }

            [Value(2, Default = null, HelpText = "File which will contain the final summary about the comparison", Required = true)]
            public string SummaryFile { get; set; }

            [Option('b', "buffer-size", Default = "64K", HelpText = "Size of I/O-buffer used for copying data", Required = true)]
            public string BufferSizeString { get; set; }

            public long BufferSize
            {
                get => ParseSizeString(BufferSizeString);
            }

        }

    }

}
