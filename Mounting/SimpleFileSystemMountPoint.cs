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
using System.Security.AccessControl;

using DokanNet;

using nDiscUtils.IO.FileSystem;
using nDiscUtils.Options;

namespace nDiscUtils.Mounting
{

    public sealed class SimpleFileSystemMountPoint : IDokanOperations, IDisposable
    {

        public const int VERSION_MAJOR = 1;
        public const int VERSION_MINOR = 0;
        public const int VERSION = (VERSION_MAJOR << 8) | (VERSION_MINOR << 0);

        private bool mIsReadOnly;
        private string mVolumeLabel;

        private BaseMountOptions mOptions;
        private ISimpleFileSystem mFileSystem;
        private bool mFileSystemSupportsRW;

        private const DokanNet.FileAccess DataAccess = DokanNet.FileAccess.ReadData | DokanNet.FileAccess.WriteData | DokanNet.FileAccess.AppendData |
                                                       DokanNet.FileAccess.Execute |
                                                       DokanNet.FileAccess.GenericExecute | DokanNet.FileAccess.GenericWrite |
                                                       DokanNet.FileAccess.GenericRead;

        private const DokanNet.FileAccess DataWriteAccess = DokanNet.FileAccess.WriteData | DokanNet.FileAccess.AppendData |
                                                            DokanNet.FileAccess.Delete |
                                                            DokanNet.FileAccess.GenericWrite;

        public bool IsReadOnly
        {
            get => mIsReadOnly;
        }

        public string VolumeLabel
        {
            get => mVolumeLabel;
            set => mVolumeLabel = value;
        }

        public SimpleFileSystemMountPoint(ISimpleFileSystem fileSystem, BaseMountOptions options)
        {
            mFileSystem = fileSystem;
            mOptions = options;
            mFileSystemSupportsRW = !mFileSystem.IsReadOnly;
            mIsReadOnly = (options.ReadOnly || !mFileSystemSupportsRW);

            VolumeLabel = fileSystem.FriendlyName;

            Logger.Info("Detected file system: {0}", mFileSystem.GetType().FullName);
            Logger.Info("File system R/W support: {0}", mFileSystemSupportsRW);
            Logger.Info("File system R/W enabled: {0}", !mIsReadOnly);
            Logger.Info("File system volume label: {0}", VolumeLabel);
        }

        public void Dispose()
        {

        }
        
        private NtStatus HandleException(Exception ex, string path)
        {
            Logger.Exception("FATAL ERROR - {0} - \"{1}\"", ex.Message, path ?? "NO_FILE");
            Logger.Exception(ex);
            return NtStatus.Error;
        }

        private NtStatus HandleIOException(IOException ex, string path)
        {
            Logger.Exception("FATAL INTERNAL ERROR - {0} - \"{1}\"", ex.Message, path ?? "NO_FILE");
            Logger.Exception(ex);
            return NtStatus.InternalError;
        }

        private FileInformation GetFileInformation(string path)
        {
            SimpleFileSystemInfo pathInfo = null;
            if (mFileSystem.DirectoryExists(path))
                pathInfo = mFileSystem.GetDirectoryInfo(path);
            else if (mFileSystem.FileExists(path))
                pathInfo = mFileSystem.GetFileInfo(path);

            return new FileInformation
            {
                Attributes = pathInfo.Attributes,
                CreationTime = pathInfo.CreationTime,
                FileName = pathInfo.Name,
                LastAccessTime = pathInfo.LastAccessTime,
                LastWriteTime = pathInfo.LastWriteTime,
                Length = (mFileSystem.FileExists(path) ? ((SimpleFileInfo)pathInfo).Length : 0)
            };
        }

        public void Cleanup(string fileName, DokanFileInfo info)
        {
            try
            {
                (info.Context as Stream)?.Dispose();
                info.Context = null;

                if (info.DeleteOnClose)
                {
                    if (info.IsDirectory)
                    {
                        mFileSystem.DeleteDirectory(fileName);
                    }
                    else
                    {
                        mFileSystem.DeleteFile(fileName);
                    }
                }
            }
            catch (Exception) { }
        }

        public void CloseFile(string fileName, DokanFileInfo info)
        {
            try
            {
                (info.Context as Stream)?.Dispose();
                info.Context = null;
            }
            catch (Exception) { }
        }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return DokanResult.InvalidName;

                fileName = mFileSystem.TransformPath(fileName);

                var readWriteAttributes = (access & DataAccess) == 0;
                var readAccess = (access & DataWriteAccess) == 0;

                var fileExists = mFileSystem.FileExists(fileName);
                var directoryExists = mFileSystem.DirectoryExists(fileName);
                var pathExists = fileExists || directoryExists;

                if (info.IsDirectory)
                {
                    switch (mode)
                    {
                        case FileMode.Open:
                            if (!directoryExists && fileName != "\\")
                            {
                                if (fileExists)
                                    return NtStatus.NotADirectory;
                                else
                                    return DokanResult.PathNotFound;
                            }
                            break;

                        case FileMode.CreateNew:
                            if (pathExists)
                                return DokanResult.AlreadyExists;

                            mFileSystem.CreateDirectory(fileName);
                            break;
                    }
                }
                else
                {
                    switch (mode)
                    {
                        case FileMode.Open:
                            if (directoryExists)
                            {
                                info.IsDirectory = true;
                                info.Context = new object();
                                return DokanResult.Success;
                            }
                            else if (!fileExists)
                            {
                                return DokanResult.FileNotFound;
                            }

                            break;

                        case FileMode.CreateNew:
                            if (pathExists)
                                return DokanResult.AlreadyExists;
                            break;

                        case FileMode.Truncate:
                            if (!fileExists)
                                return DokanResult.FileNotFound;
                            break;
                    }

                    // ensure recycle bin exists if requested
                    var parentDirectory = Path.GetDirectoryName(fileName);
                    if (!mFileSystem.DirectoryExists(Path.GetDirectoryName(fileName)))
                    {
                        mFileSystem.CreateDirectory(parentDirectory);
                    }

                    if (directoryExists && (mode == FileMode.OpenOrCreate || mode == FileMode.Create))
                        return DokanResult.AlreadyExists;

                    info.Context = mFileSystem.Open(fileName, mode,
                        readAccess ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite);
                }

                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus DeleteDirectory(string fileName, DokanFileInfo info)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return DokanResult.InvalidName;

                fileName = mFileSystem.TransformPath(fileName);

                if (mFileSystem.FileExists(fileName))
                    return NtStatus.NotADirectory;
                else if (!mFileSystem.DirectoryExists(fileName))
                    return DokanResult.FileNotFound;

                if (mFileSystem.GetFileSystemEntries(fileName).Count() != 0)
                    return NtStatus.DirectoryNotEmpty;

                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return DokanResult.InvalidName;

                fileName = mFileSystem.TransformPath(fileName);

                if (mFileSystem.DirectoryExists(fileName))
                    return DokanResult.AccessDenied;

                if (!mFileSystem.FileExists(fileName))
                    return DokanResult.FileNotFound;
                
                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            return FindFilesWithPattern(fileName, "*.*", out files, info);
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info)
        {
            files = new List<FileInformation>();

            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return DokanResult.InvalidName;

                fileName = mFileSystem.TransformPath(fileName);

                var realfiles = mFileSystem
                    .GetFileSystemEntries(fileName)
                    .Where(finfo => DokanHelper.DokanIsNameInExpression(searchPattern, finfo.Name, true) && !string.IsNullOrWhiteSpace(finfo.Name))
                    .Select(finfo => GetFileInformation(finfo.FullName)).ToArray();

                ((List<FileInformation>)files).AddRange(realfiles);
                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            streams = new FileInformation[0];

            return DokanResult.Error;
        }

        public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            try
            {
                (info.Context as Stream)?.Flush();
                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, DokanFileInfo info)
        {
            freeBytesAvailable = 0;
            totalNumberOfFreeBytes = 0;
            totalNumberOfBytes = 0;

            try
            {
                freeBytesAvailable = mFileSystem.AvailableSpace;
                totalNumberOfFreeBytes = mFileSystem.AvailableSpace;
                totalNumberOfBytes = mFileSystem.Size;
                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, null);
            }
            catch (Exception ex)
            {
                return HandleException(ex, null);
            }
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info)
        {
            fileInfo = new FileInformation();

            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return DokanResult.InvalidName;

                fileName = mFileSystem.TransformPath(fileName);

                if (!mFileSystem.Exists(fileName))
                    return DokanResult.FileNotFound;

                fileInfo = GetFileInformation(fileName);
                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            security = null;
            return NtStatus.NotImplemented;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, DokanFileInfo info)
        {
            volumeLabel = "Unknown";
            features = FileSystemFeatures.None;
            fileSystemName = "Unknown";

            try
            {
                volumeLabel = string.IsNullOrWhiteSpace(mVolumeLabel) ? "nDiscUtils Volume" : mVolumeLabel;

                if (mIsReadOnly)
                    features |= FileSystemFeatures.ReadOnlyVolume;

                fileSystemName = mFileSystem.FriendlyName;

                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, null);
            }
            catch (Exception ex)
            {
                return HandleException(ex, null);
            }
        }

        public NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus Mounted(DokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            try
            {
                oldName = mFileSystem.TransformPath(oldName);
                newName = mFileSystem.TransformPath(newName);

                (info.Context as Stream)?.Dispose();
                info.Context = null;

                if (!mFileSystem.Exists(newName))
                {
                    info.Context = null;
                    if (info.IsDirectory)
                        mFileSystem.MoveDirectory(oldName, newName);
                    else
                        mFileSystem.MoveFile(oldName, newName);
                    
                    return DokanResult.Success;
                }
                else if (replace)
                {
                    info.Context = null;
                    if (info.IsDirectory)
                        return DokanResult.AccessDenied;

                    mFileSystem.MoveFile(oldName, newName, replace);                    
                    return DokanResult.Success;
                }

                return DokanResult.AlreadyExists;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, string.Format("<{0}> -> <{1}>", oldName, newName));
            }
            catch (Exception ex)
            {
                return HandleException(ex, string.Format("<{0}> -> <{1}>", oldName, newName));
            }
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            bytesRead = 0;

            try
            {
                fileName = mFileSystem.TransformPath(fileName);

                var memoryMapped = (info.Context == null);
                if (memoryMapped)
                    info.Context = mFileSystem.Open(fileName, FileMode.Open, System.IO.FileAccess.Read);

                var stream = info.Context as Stream;
                lock (stream)
                {
                    stream.Position = offset;
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                }

                if (memoryMapped)
                {
                    stream.Flush();
                    stream.Dispose();
                    info.Context = null;
                }

                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            try
            {
                (info.Context as Stream)?.SetLength(length);
                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            try
            {
                (info.Context as Stream)?.SetLength(length);
                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            try
            {
                fileName = mFileSystem.TransformPath(fileName);

                if (info.IsDirectory)
                {
                    if (!mFileSystem.DirectoryExists(fileName))
                        return DokanResult.PathNotFound;
                }
                else
                {
                    if (!mFileSystem.FileExists(fileName))
                        return DokanResult.FileNotFound;
                }

                mFileSystem.SetAttributes(fileName, attributes);
                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info)
        {
            try
            {
                fileName = mFileSystem.TransformPath(fileName);

                if (info.IsDirectory)
                {
                    if (!mFileSystem.DirectoryExists(fileName))
                        return DokanResult.PathNotFound;
                }
                else
                {
                    if (!mFileSystem.FileExists(fileName))
                        return DokanResult.FileNotFound;
                }

                if (creationTime.HasValue)
                    mFileSystem.SetCreationTime(fileName, creationTime.Value);

                if (lastAccessTime.HasValue)
                    mFileSystem.SetLastAccessTime(fileName, lastAccessTime.Value);

                if (lastWriteTime.HasValue)
                    mFileSystem.SetLastWriteTime(fileName, lastWriteTime.Value);
                
                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus Unmounted(DokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            bytesWritten = 0;

            try
            {
                fileName = mFileSystem.TransformPath(fileName);

                var memoryMapped = (info.Context == null);
                if (memoryMapped)
                    info.Context = mFileSystem.Open(fileName, FileMode.Open, System.IO.FileAccess.ReadWrite);

                var stream = info.Context as Stream;
                lock (stream)
                {
                    stream.Position = offset;
                    stream.Write(buffer, 0, buffer.Length);
                    bytesWritten = buffer.Length;
                }

                if (memoryMapped)
                {
                    stream.Flush();
                    stream.Dispose();
                    info.Context = null;
                }

                return DokanResult.Success;
            }
            catch (IOException ioex)
            {
                return HandleIOException(ioex, fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, fileName);
            }
        }

    }

}
