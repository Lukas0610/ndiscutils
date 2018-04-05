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

namespace nDiscUtils.IO.FileSystem
{

    public interface ISimpleFileSystem
    {

        bool IsReadOnly { get; }

        string FriendlyName { get; }

        long AvailableSpace { get; }

        long Size { get; }

        string TransformPath(string input);

        SimpleFileSystemInfo GetFileSystemInfo(string path);

        SimpleFileInfo GetFileInfo(string path);

        SimpleDirectoryInfo GetDirectoryInfo(string path);

        IEnumerable<SimpleFileSystemInfo> GetFileSystemEntries(string path);

        IEnumerable<SimpleFileInfo> GetFiles(string path);

        IEnumerable<SimpleDirectoryInfo> GetDirectories(string path);

        void SetAttributes(string path, FileAttributes attributes);

        void SetCreationTime(string path, DateTime dateTime);

        void SetLastAccessTime(string path, DateTime dateTime);

        void SetLastWriteTime(string path, DateTime dateTime);

        bool Exists(string path);

        void CreateFile(string path);

        void CopyFile(string oldPath, string newPath);

        void CopyFile(string oldPath, string newPath, bool replace);

        void MoveFile(string oldPath, string newPath);

        void MoveFile(string oldPath, string newPath, bool replace);

        void DeleteFile(string path);

        bool FileExists(string path);

        void CreateDirectory(string path);

        void MoveDirectory(string oldPath, string newPath);

        void DeleteDirectory(string path);

        bool DirectoryExists(string path);

        Stream Open(string path, FileMode mode);

        Stream Open(string path, FileMode mode, FileAccess access);

    }

}
