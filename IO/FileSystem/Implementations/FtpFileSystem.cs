﻿/*
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
using FluentFTP;

namespace nDiscUtils.IO.FileSystem.Implementations
{

    public sealed class FtpFileSystem : ISimpleFileSystem
    {

        private FtpClient mClient;

        public bool IsReadOnly => false;

        public string FriendlyName => "FTP Network File System";

        public long AvailableSpace => -1;

        public long Size => -1;

        public FtpFileSystem(FtpClient client)
        {
            if (!client.IsConnected)
                throw new ArgumentException("Cannot use unconnected FTP client", "client");

            mClient = client;
            mClient.EnableThreadSafeDataConnections = true;
        }

        public void CopyFile(string oldPath, string newPath)
            => CopyFile(oldPath, newPath, false);

        public void CopyFile(string oldPath, string newPath, bool replace)
        {
            if (!replace && mClient.FileExists(newPath))
                return;
        }

        public void CreateDirectory(string path)
            => mClient.CreateDirectory(path);

        public void CreateFile(string path)
        {
            using (var stream = mClient.OpenWrite(path, FtpDataType.ASCII, true)) { }
            mClient.GetReply();
        }

        public void DeleteDirectory(string path)
            => mClient.DeleteDirectory(path);

        public void DeleteFile(string path)
            => mClient.DeleteFile(path);

        public bool DirectoryExists(string path)
            => mClient.DirectoryExists(path);

        public bool Exists(string path)
            => FileExists(path) || DirectoryExists(path);

        public bool FileExists(string path)
            => mClient.FileExists(path);

        public IEnumerable<SimpleDirectoryInfo> GetDirectories(string path)
        {
            var result = mClient.GetListing(path, FtpListOption.AllFiles);
            foreach (var entry in result)
            {
                if (entry.Type == FtpFileSystemObjectType.Directory)
                {
                    yield return GetDirectoryInfoImpl(path, entry);
                }
            }
        }

        public SimpleDirectoryInfo GetDirectoryInfo(string path)
        {
            var entry = mClient.GetObjectInfo(path, true);
            return GetDirectoryInfoImpl(path, entry);
        }

        private SimpleDirectoryInfo GetDirectoryInfoImpl(string path, FtpListItem entry)
        {
            return new SimpleDirectoryInfo()
            {
                Attributes = FileAttributes.Directory,
                CreationTime = entry.Created,
                Directory = path,
                FullName = entry.FullName,
                LastAccessTime = entry.Modified,
                LastWriteTime = entry.Modified,
                Name = entry.Name
            };
        }

        public SimpleFileInfo GetFileInfo(string path)
        {
            var entry = mClient.GetObjectInfo(path, true);
            return GetFileInfoImpl(path, entry);
        }

        private SimpleFileInfo GetFileInfoImpl(string path, FtpListItem entry)
        {
            return new SimpleFileInfo()
            {
                Attributes = FileAttributes.Directory,
                CreationTime = entry.Created,
                Directory = path,
                FullName = entry.FullName,
                LastAccessTime = null,
                LastWriteTime = entry.Modified,
                Name = entry.Name,
                Length = entry.Size
            };
        }

        public IEnumerable<SimpleFileInfo> GetFiles(string path)
        {
            var result = mClient.GetListing(path, FtpListOption.AllFiles);
            foreach (var entry in result)
            {
                if (entry.Type != FtpFileSystemObjectType.Directory)
                {
                    yield return GetFileInfoImpl(path, entry);
                }
            }
        }

        public IEnumerable<SimpleFileSystemInfo> GetFileSystemEntries(string path)
        {
            var result = mClient.GetListing(path, FtpListOption.AllFiles);
            foreach (var entry in result)
            {
                if (entry.Type == FtpFileSystemObjectType.Directory)
                {
                    yield return GetDirectoryInfoImpl(path, entry);
                }
                else
                {
                    yield return GetFileInfoImpl(path, entry);
                }
            }
        }

        public SimpleFileSystemInfo GetFileSystemInfo(string path)
        {
            var entry = mClient.GetObjectInfo(path, true);
            if (entry.Type == FtpFileSystemObjectType.Directory)
            {
                return GetDirectoryInfoImpl(path, entry);
            }
            else
            {
                return GetFileInfoImpl(path, entry);
            }
        }

        public void MoveDirectory(string oldPath, string newPath)
            => mClient.MoveDirectory(oldPath, newPath, FtpExists.Skip);

        public void MoveFile(string oldPath, string newPath)
            => MoveFile(oldPath, newPath, false);

        public void MoveFile(string oldPath, string newPath, bool replace)
            => mClient.MoveFile(oldPath, newPath, (replace ? FtpExists.Overwrite : FtpExists.Skip));

        public Stream Open(string path, FileMode mode)
            => Open(path, mode, FileAccess.ReadWrite);

        public Stream Open(string path, FileMode mode, FileAccess access)
        {
            switch (mode)
            {
                case FileMode.Append:
                    return mClient.OpenAppend(path);
                default:
                    if (mode == FileMode.Truncate)
                        mClient.DeleteFile(path);

                    if (access == FileAccess.Read)
                        return mClient.OpenRead(path);
                    else if (access == FileAccess.Write)
                        return mClient.OpenWrite(path);
                    else // if (access == FileAccess.ReadWrite)
                        return new DualStream(mClient.OpenRead(path), mClient.OpenWrite(path));
            }
        }

        public void SetAttributes(string path, FileAttributes attributes)
        {
            // not supported
        }

        public void SetCreationTime(string path, DateTime dateTime)
        {
            // not supported
        }

        public void SetLastAccessTime(string path, DateTime dateTime)
        {
            // not supported
        }

        public void SetLastWriteTime(string path, DateTime dateTime)
            => mClient.SetModifiedTime(path, dateTime);

        public string TransformPath(string input)
            => input.Replace('\\', '/');

    }

}
