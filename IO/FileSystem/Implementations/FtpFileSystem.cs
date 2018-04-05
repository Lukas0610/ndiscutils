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
using FluentFTP;

namespace nDiscUtils.IO.FileSystem.Implementations
{

    public sealed class FtpFileSystem : ISimpleFileSystem
    {

        private const int CACHE_TIMEOUT = 1000;

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
        {
            var cacheEntry = GetCacheEntry(path);
            if (!cacheEntry.DirectoryExistsInvalidate)
                return cacheEntry.DirectoryExists;

            var result = mClient.DirectoryExists(path);
            cacheEntry.DirectoryExists = result;
            cacheEntry.DirectoryExistsSet = DateTime.Now;
            return result;
        }

        public bool Exists(string path)
            => FileExists(path) || DirectoryExists(path);

        public bool FileExists(string path)
        {
            var cacheEntry = GetCacheEntry(path);
            if (!cacheEntry.FileExistsInvalidate)
                return cacheEntry.FileExists;

            var result = mClient.FileExists(path);
            cacheEntry.FileExists = result;
            cacheEntry.FileExistsSet = DateTime.Now;
            return result;
        }

        public IEnumerable<SimpleDirectoryInfo> GetDirectories(string path)
        {
            FtpListItem[] result = null;

            var cacheEntry = GetCacheEntry(path);
            if (cacheEntry.DirectoryListingInvalidate)
            {
                result = mClient.GetListing(path, FtpListOption.AllFiles);
                cacheEntry.DirectoryListing = result;
                cacheEntry.DirectoryListingSet = DateTime.Now;
            }
            else
                result = cacheEntry.DirectoryListing;

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
            var cacheEntry = GetCacheEntry(path);
            if (!cacheEntry.ListItemInvalidate)
                return GetDirectoryInfoImpl(path, cacheEntry.ListItem);

            var entry = mClient.GetObjectInfo(path, true);
            cacheEntry.ListItem = entry;
            cacheEntry.ListItemSet = DateTime.Now;
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
            var cacheEntry = GetCacheEntry(path);
            if (!cacheEntry.ListItemInvalidate)
                return GetFileInfoImpl(path, cacheEntry.ListItem);

            var entry = mClient.GetObjectInfo(path, true);
            cacheEntry.ListItem = entry;
            cacheEntry.ListItemSet = DateTime.Now;
            return GetFileInfoImpl(path, entry);
        }

        private SimpleFileInfo GetFileInfoImpl(string path, FtpListItem entry)
        {
            return new SimpleFileInfo()
            {
                Attributes = FileAttributes.Normal,
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
            FtpListItem[] result = null;

            var cacheEntry = GetCacheEntry(path);
            if (cacheEntry.FileListingInvalidate)
            {
                result = mClient.GetListing(path, FtpListOption.AllFiles);
                cacheEntry.FileListing = result;
                cacheEntry.FileListingSet = DateTime.Now;
            }
            else
                result = cacheEntry.FileListing;

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
            FtpListItem[] result = null;

            var cacheEntry = GetCacheEntry(path);
            if (cacheEntry.FileSystemListingInvalidate)
            {
                result = mClient.GetListing(path, FtpListOption.AllFiles);
                cacheEntry.FileSystemListing = result;
                cacheEntry.FileSystemListingSet = DateTime.Now;
            }
            else
                result = cacheEntry.FileSystemListing;

            foreach (var entry in result)
                yield return GetFileSystemInfoImpl(path, entry);
        }

        public SimpleFileSystemInfo GetFileSystemInfo(string path)
        {
            var cacheEntry = GetCacheEntry(path);
            if (!cacheEntry.ListItemInvalidate)
                return GetFileSystemInfoImpl(path, cacheEntry.ListItem);

            var entry = mClient.GetObjectInfo(path, true);
            cacheEntry.ListItem = entry;
            cacheEntry.ListItemSet = DateTime.Now;
            return GetFileSystemInfoImpl(path, entry);
        }

        public SimpleFileSystemInfo GetFileSystemInfoImpl(string path, FtpListItem entry)
        {
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

        //
        // EXPERIMENTAL CACHING SYSTEM
        //

        private Dictionary<string, FtpCacheEntry> mCache
            = new Dictionary<string, FtpCacheEntry>();

        private FtpCacheEntry GetCacheEntry(string path)
        {
            if (!mCache.ContainsKey(path))
                mCache.Add(path, new FtpCacheEntry());
            return mCache[path];
        }

        private class FtpCacheEntry
        {
            public bool ListItemInvalidate { get => DateTime.Now.Subtract(ListItemSet).TotalMilliseconds >= CACHE_TIMEOUT; }
            public DateTime ListItemSet = DateTime.MinValue;
            public FtpListItem ListItem;

            public bool DirectoryExistsInvalidate { get => DateTime.Now.Subtract(DirectoryExistsSet).TotalMilliseconds >= CACHE_TIMEOUT; }
            public DateTime DirectoryExistsSet = DateTime.MinValue;
            public bool DirectoryExists;

            public bool FileExistsInvalidate { get => DateTime.Now.Subtract(FileExistsSet).TotalMilliseconds >= CACHE_TIMEOUT; }
            public DateTime FileExistsSet = DateTime.MinValue;
            public bool FileExists;

            public bool FileListingInvalidate { get => DateTime.Now.Subtract(FileListingSet).TotalMilliseconds >= CACHE_TIMEOUT; }
            public DateTime FileListingSet = DateTime.MinValue;
            public FtpListItem[] FileListing;

            public bool DirectoryListingInvalidate { get => DateTime.Now.Subtract(DirectoryListingSet).TotalMilliseconds >= CACHE_TIMEOUT; }
            public DateTime DirectoryListingSet = DateTime.MinValue;
            public FtpListItem[] DirectoryListing;

            public bool FileSystemListingInvalidate { get => DateTime.Now.Subtract(FileSystemListingSet).TotalMilliseconds >= CACHE_TIMEOUT; }
            public DateTime FileSystemListingSet = DateTime.MinValue;
            public FtpListItem[] FileSystemListing;
        }

    }

}
