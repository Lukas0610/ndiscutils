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

using DiscUtils.Iso9660;
using DiscUtils.SquashFs;

using nDiscUtils.Core;
using nDiscUtils.IO;
using nDiscUtils.Options;

using static nDiscUtils.Core.ModuleHelpers;
using static nDiscUtils.Core.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class MakeDirectoryImage
    {

        private const string LABEL = "nDiscUtils Image";

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            switch (opts.FileSystem)
            {
                case "SquashFS":
                case "ISO":
                    break;

                default:
                    Logger.Error("Requested file system is not supported (Requested {0}, supported: ISO, SquashFS)", opts.FileSystem);
                    WaitForUserExit();
                    return INVALID_ARGUMENT;
            }

            Logger.Info("Gathering files image \"{0}\"", opts.Path);
            var fileList = new LinkedList<FileInfo>();
            var fileSize = 0L;

            Action<DirectoryInfo> recursiveFileIndexer = null;
            recursiveFileIndexer = new Action<DirectoryInfo>((parentDir) =>
            {
                try
                {
                    foreach (var file in parentDir.GetFiles())
                    {
                        fileSize += file.Length + ((file.Length % 4096) == 0 ? 0 : 4096 - (file.Length % 4096));
                        fileList.AddLast(file);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                try
                {
                    foreach (var subDir in parentDir.GetDirectories())
                    {
                        Logger.Verbose("Advancing recursion into \"{0}\"", subDir.FullName);
                        recursiveFileIndexer(subDir);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            });

            recursiveFileIndexer(new DirectoryInfo(opts.Directory));

            Logger.Info("Creating image \"{0}\"", opts.Path);
            var imageStream = OpenPath(opts.Path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None);
            
            if (opts.Offset > 0)
                imageStream = new OffsetableStream(imageStream, opts.Offset);

            switch (opts.FileSystem)
            {
                case "SquashFS":
                {
                    var builder = new SquashFileSystemBuilder();
                    LoopFiles((path, stream) => builder.AddFile(path, stream), opts.Directory, fileList);

                    Logger.Info("Finishing building SquashFS image...");
                    builder.Build(imageStream);
                    break;
                }
                
                case "ISO":
                {
                    var builder = new CDBuilder();
                    LoopFiles((path, stream) => builder.AddFile(path, stream), opts.Directory, fileList);

                    Logger.Info("Finishing building ISO image...");
                    builder.Build(imageStream);
                    break;
                }

                default:
                    return INVALID_ARGUMENT;
            }

            Logger.Info("Done!");

            Cleanup(imageStream);
            WaitForUserExit();
            return SUCCESS;
        }

        private static void LoopFiles(Action<string, Stream> action, string baseDirectory, LinkedList<FileInfo> files)
        {
            var currentNode = files.First;

            baseDirectory = Path.GetFullPath(baseDirectory.Trim('\\'));

            Logger.Info("Starting to transfer files into image");
            while (currentNode != null)
            {
                var fileInfo = currentNode.Value;
                var relativePath = "\\" + (fileInfo.FullName.Substring(baseDirectory.Length).Trim('\\'));

                Logger.Info("Transfering {0}... ({1} Bytes)", relativePath, fileInfo.Length);

                var fileStream = fileInfo.OpenRead();
                action(relativePath, fileStream);

                currentNode = currentNode.Next;
            }
        }

        [Verb("mkdirimg", HelpText = "Create a file system image out of a directory")]
        public sealed class Options : BaseOptions
        {

            [Value(0, Default = null, HelpText = "Path to the image or the disk to which the image will be written to", Required = true)]
            public string Path { get; set; }

            [Value(1, Default = null, HelpText = "Directory whch will be copied into the image or disk", Required = true)]
            public string Directory { get; set; }

            [Option('f', "fs", Default = "NTFS", HelpText = "Type of the filesystem the new image should be formatted with")]
            public string FileSystem { get; set; }

            [Option('o', "offset", Default = "0", HelpText = "Offset in bytes at which the image will be written to the target")]
            public string OffsetString { get; set; }

            public long Offset
            {
                get => ParseSizeString(OffsetString);
            }

        }

    }

}
