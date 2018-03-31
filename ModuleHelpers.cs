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

        public static void RunHelpers(BaseOptions opts)
        {
            if (opts.LogFile != null)
                Logger.OpenFile(opts.LogFile);
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
            if (path.StartsWith("\\\\.\\"))
            {
                return PlatformFileHandler.OpenDisk(path, access, share);
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
