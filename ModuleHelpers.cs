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
using System.Reflection;
using DiscUtils;
using DiscUtils.Fat;
using DiscUtils.Ntfs;
using DiscUtils.Partitions;
using DokanNet;
using nDiscUtils.IO;
using nDiscUtils.Mounting;
using nDiscUtils.Options;

namespace nDiscUtils
{

    public static class ModuleHelpers
    {

        private static int osVersionPlatform = (int)Environment.OSVersion.Platform;

        public static bool IsLinux
        {
            get
            {
                return (osVersionPlatform == 4) ||
                       (osVersionPlatform == 6) ||
                       (osVersionPlatform == 128);
            }
        }

        public static bool UseConsoleBuffers
        {
            get
            {
                return !IsLinux
#if DISABLE_CONSOLE_BUFFERS
                    && false
#endif
                    ;
            }
        }

        public static void RunHelpers(BaseOptions opts)
        {
            if (opts.LogFile != null)
                Logger.OpenFile(opts.LogFile);

            Logger.SetVerbose(opts.Verbose);
            Logger.SetDebug(opts.Debug);
        }

        public static int ParseSizeString(string sizeString)
        {
            var sizeChar = sizeString[sizeString.Length - 1];
            var size = 0;

            if (char.IsLetter(sizeChar))
                size = int.Parse(sizeString.Substring(0, sizeString.Length - 1));
            else
                return int.Parse(sizeString.Substring(0, sizeString.Length));

            switch (sizeChar)
            {
                case 'B': return size;
                case 'K': return size * 1024;
                case 'M': return size * 1024 * 1024;
                case 'G': return size * 1024 * 1024 * 1024;
                default: // larger buffer? Damn...
                    throw new ArgumentOutOfRangeException("sizeString", "Not parsing size characters larger than 'G'");
            }
        }

        public static Stream OpenPath(string path, FileMode mode, System.IO.FileAccess access, FileShare share)
        {
            if (!IsLinux && path.StartsWith("\\\\.\\"))
            {
                return PlatformFileHandler.OpenDisk(path, access, share);
            }
            else if (IsLinux && path.StartsWith("/dev/"))
            {
                // TODO: implement block device handling on Unix systems
                return null;
            }
            else
            {
                if (!System.IO.File.Exists(path) && !(mode == FileMode.Create || 
                    mode == FileMode.OpenOrCreate || mode == FileMode.CreateNew))
                {
                    Logger.Info("Could not find image \"{0}\"", path);
                    return null;
                }

                return new FileStream(path, mode, access, share);
            }
        }

        public static void MountStream(Stream stream, BaseMountOptions opts)
        {
            using (var mountPoint = new DokanMountPoint(stream, opts))
            {
                Logger.Info("Preparing mount point at {0}:\\ (read-only={1})", opts.Letter, opts.ReadOnly);

                DokanOptions mountOptions = DokanOptions.FixedDrive | DokanOptions.MountManager;

                if (mountPoint.IsReadOnly)
                    mountOptions |= DokanOptions.WriteProtection;

                Logger.Info("Attributes: 0x{0:X} ({1})", mountOptions, mountOptions.ToString());

                try
                {
                    Logger.Info("Attempting to mount stream at {0}:\\ with {1} thread(s)", opts.Letter, opts.Threads);
                    mountPoint.Mount($"{opts.Letter}:", mountOptions, opts.Threads,
                        DokanMountPoint.VERSION, TimeSpan.FromSeconds(5),
                        $"nDiscUtils\\{opts.Letter}", new DokanNullLogger());
                }
                catch (DokanException dex)
                {
                    Logger.Exception("Internal Dokan Exception: {0}", dex.Message);
                    Logger.Exception(dex);
                }
            }
        }

        public static PartitionTable FindPartitionTable(Stream stream)
        {
            PartitionTable partitionTable = null;

            // Always check GPT first as it as an MBR-encapsulated partition scheme
            if (GuidPartitionTable.Detect(stream))
            {
                partitionTable = new GuidPartitionTable(
                    stream,
                    GuidPartitionTable.DetectGeometry(stream));

                Logger.Info("Detected partition table: GPT");
            }
            else if (BiosPartitionTable.IsValid(stream))
            {
                partitionTable = new BiosPartitionTable(
                    stream,
                    BiosPartitionTable.DetectGeometry(stream));

                Logger.Info("Detected partition table: MBR");
            }

            if (partitionTable == null)
            {
                Logger.Info("Could not find valid partition table");
                return null;
            }

            return partitionTable;
        }

        public static Geometry FindGeometry(Stream stream)
        {
            var geometryType = "UNKN";
            Geometry geometry = null;

            // Always check GPT first as it as an MBR-encapsulated partition scheme
            if (GuidPartitionTable.Detect(stream))
            {
                geometry = GuidPartitionTable.DetectGeometry(stream);
                geometryType = "GPT";
            }
            else if (BiosPartitionTable.IsValid(stream))
            {
                geometry = BiosPartitionTable.DetectGeometry(stream);
                geometryType = "MBR";
            }

            if (geometry == null)
            {
                Logger.Info("Could not find valid geometry");
                return null;
            }

            Logger.Info("{0}/BPS:{1}; SPT:{2}; HPC:{3}; CL:{4}; TS:{5}; CP:{6}", geometryType,
                geometry.BytesPerSector, geometry.SectorsPerTrack, geometry.HeadsPerCylinder,
                geometry.Cylinders, geometry.TotalSectorsLong, geometry.Capacity);

            return geometry;
        }

        public static DiscFileSystem FormatStream(string fileSystemName, Stream stream, long size, string label)
        {
            switch (fileSystemName)
            {
                case "NTFS":
                {
                    var geometry = Geometry.FromCapacity(size);

                    Logger.Info("Formatting stream as NTFS");
                    return NtfsFileSystem.Format(stream, label, geometry, 0, geometry.TotalSectorsLong);
                }

                case "FAT":
                {
                    var geometry = Geometry.FromCapacity(size);

                    Logger.Info("Formatting stream as FAT");
#pragma warning disable CS0618 // Typ oder Element ist veraltet
                    return FatFileSystem.FormatPartition(stream, label, geometry, 0, geometry.TotalSectors, 0);
#pragma warning restore CS0618 // Typ oder Element ist veraltet
                }

                default:
                {
                    Logger.Error("Requested file system is not supported (Requested {0}, supported: NTFS, FAT)",
                        fileSystemName);
                    Environment.Exit(1);
                    break;
                }
            }

            return null;
        }

        public static WellKnownPartitionType GetWellKnownPartitionType(
            GuidPartitionInfo partition)
        {
            if (partition.GuidType.Equals(GuidPartitionTypes.BiosBoot))
                return WellKnownPartitionType.BiosBoot;
            else if (partition.GuidType.Equals(GuidPartitionTypes.EfiSystem))
                return WellKnownPartitionType.EfiSystem;
            else if (partition.GuidType.Equals(GuidPartitionTypes.LinuxLvm))
                return WellKnownPartitionType.LinuxLvm;
            else if (partition.GuidType.Equals(GuidPartitionTypes.LinuxSwap))
                return WellKnownPartitionType.LinuxSwap;
            else if (partition.GuidType.Equals(GuidPartitionTypes.MicrosoftReserved))
                return WellKnownPartitionType.MicrosoftReserved;
            else if (partition.GuidType.Equals(GuidPartitionTypes.WindowsBasicData))
                return WellKnownPartitionType.WindowsNtfs; // + WindowsFat & Linux
            else if (partition.GuidType.Equals(GuidPartitionTypes.WindowsLdmData))
                return WellKnownPartitionType.WindowsLdmData;
            else if (partition.GuidType.Equals(GuidPartitionTypes.WindowsLdmMetadata))
                return WellKnownPartitionType.WindowsLdmMetadata;

            return 0;
        }

        public static bool IsSupportedPartitionType(WellKnownPartitionType partitionType)
        {
            // Exactly clone critical partitions like BIOS/EFI boot
            if (partitionType == WellKnownPartitionType.BiosBoot ||
                    partitionType == WellKnownPartitionType.EfiSystem)
                return false;

            return true;
        }

        private static string[] BYTE_SUFFIXES =
            { "Byte", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string FormatBytes(double input, int decimals)
        {
            int suffix = 0;

            while (input / 1024.0 >= 0.9)
            {
                input /= 1024.0;
                suffix++;
            }

            return string.Format("{0:0." + new string('0', decimals) + "} {1}",
                input, BYTE_SUFFIXES[suffix]);
        }

        // https://stackoverflow.com/a/1600990
        public static DateTime GetLinkerTime(this Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, System.IO.FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        // https://stackoverflow.com/a/33307903
        public static unsafe bool EqualBytesLongUnrolled(byte[] data1, byte[] data2)
        {
            if (data1 == data2)
                return true;

            if (data1.Length != data2.Length)
                return false;

            fixed (byte* bytes1 = data1, bytes2 = data2)
            {
                int len = data1.Length;
                int rem = len % (sizeof(long) * 16);
                long* b1 = (long*)bytes1;
                long* b2 = (long*)bytes2;
                long* e1 = (long*)(bytes1 + len - rem);

                while (b1 < e1)
                {
                    if (*(b1) != *(b2) || *(b1 + 1) != *(b2 + 1) ||
                        *(b1 + 2) != *(b2 + 2) || *(b1 + 3) != *(b2 + 3) ||
                        *(b1 + 4) != *(b2 + 4) || *(b1 + 5) != *(b2 + 5) ||
                        *(b1 + 6) != *(b2 + 6) || *(b1 + 7) != *(b2 + 7) ||
                        *(b1 + 8) != *(b2 + 8) || *(b1 + 9) != *(b2 + 9) ||
                        *(b1 + 10) != *(b2 + 10) || *(b1 + 11) != *(b2 + 11) ||
                        *(b1 + 12) != *(b2 + 12) || *(b1 + 13) != *(b2 + 13) ||
                        *(b1 + 14) != *(b2 + 14) || *(b1 + 15) != *(b2 + 15))
                        return false;
                    b1 += 16;
                    b2 += 16;
                }

                for (int i = 0; i < rem; i++)
                    if (data1[len - 1 - i] != data2[len - 1 - i])
                        return false;

                return true;
            }
        }

        public static void Cleanup(Stream stream)
        {
            if (stream != null)
            {
                stream.Flush();
                stream.Close();
                stream.Dispose();
            }                   
        }

    }

}
