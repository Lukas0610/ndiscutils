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

using DiscUtils;
using DiscUtils.Btrfs;
using DiscUtils.Ext;
using DiscUtils.Fat;
using DiscUtils.HfsPlus;
using DiscUtils.Iso9660;
using DiscUtils.Ntfs;
using DiscUtils.SquashFs;
using DiscUtils.Udf;
using DiscUtils.Xfs;

using DokanNet;

using nDiscUtils.Options;

namespace nDiscUtils.Mounting
{

    public sealed class DokanMountPoint : IDokanOperations, IDisposable
	{

        public const int VERSION_MAJOR = 1;
        public const int VERSION_MINOR = 0;
        public const int VERSION = (VERSION_MAJOR << 8) | (VERSION_MINOR << 0);

        private bool mIsReadOnly;
		private string mVolumeLabel;

        private Stream mStream;
        private BaseMountOptions mOptions;
        private IFileSystem mFileSystem;
        private bool mFileSystemSupportsRW;

        private bool mFileSystemIsNTFS;

        private const DokanNet.FileAccess DataAccess = DokanNet.FileAccess.ReadData | DokanNet.FileAccess.WriteData | DokanNet.FileAccess.AppendData |
                                                       DokanNet.FileAccess.Execute |
                                                       DokanNet.FileAccess.GenericExecute | DokanNet.FileAccess.GenericWrite |
                                                       DokanNet.FileAccess.GenericRead;

        private const DokanNet.FileAccess DataWriteAccess = DokanNet.FileAccess.WriteData | DokanNet.FileAccess.AppendData |
                                                            DokanNet.FileAccess.Delete |
                                                            DokanNet.FileAccess.GenericWrite;

        private readonly string[] kNtfsFileSecurityBlacklist =
        {
            "\\$AttrDef",
            "\\$BadClus",
            "\\$Bitmap",
            "\\$Boot",
            "\\$Extend",
            "\\$LogFile",
            "\\$MFT",
            "\\$MFTMirr",
            "\\$UpCase",
            "\\$Volume",
            "\\System Volume Information\\", // keep trailing slash
        };

        // Things which the file system doesn't want you to access
        private readonly string[] kNtfsFileSecurityHardBlacklist =
        {
            "\\$Secure",
        };

        public bool IsReadOnly
        {
            get => mIsReadOnly;
        }

		public string VolumeLabel
        {
            get => mVolumeLabel;
            set => mVolumeLabel = value;
        }

        public DokanMountPoint(Stream stream, BaseMountOptions options)
        {
            mStream = stream;
            mFileSystem = GetFileSystem(stream);
            mOptions = options;
            mFileSystemSupportsRW = !IsFileSystemReadOnly(mFileSystem);
            mIsReadOnly = (options.ReadOnly || !mFileSystemSupportsRW);

            mFileSystemIsNTFS = (mFileSystem is NtfsFileSystem);
            if (mFileSystemIsNTFS)
            {
                var ntfsFileSystem = (NtfsFileSystem)mFileSystem;
                ntfsFileSystem.NtfsOptions.HideHiddenFiles = false;
                ntfsFileSystem.NtfsOptions.HideSystemFiles = false;
                ntfsFileSystem.NtfsOptions.HideMetafiles = !options.ShowHiddenFiles;
            }

            if (mFileSystem is DiscFileSystem discFileSystem)
                VolumeLabel = discFileSystem.VolumeLabel;

            Logger.Info("Detected file system: {0}", mFileSystem.GetType().FullName);
            Logger.Info("File system R/W support: {0}", mFileSystemSupportsRW);
            Logger.Info("File system R/W enabled: {0}", !mIsReadOnly);
            Logger.Info("File system volume label: {0}", VolumeLabel);
        }

        public void Flush()
        {
            mStream.Flush();
        }

        public void Dispose()
        {
            this.Flush();
            (mFileSystem as IDisposable)?.Dispose();
            mFileSystem = null;
        }

        private IFileSystem GetFileSystem(Stream stream)
        {
            if (BtrfsFileSystem.Detect(stream))
                return new BtrfsFileSystem(stream);
            else if (FatFileSystem.Detect(stream))
                return new FatFileSystem(stream);
            else if (ExtFileSystem.Detect(stream))
                return new ExtFileSystem(stream);
            else if (HfsPlusFileSystem.Detect(stream))
                return new HfsPlusFileSystem(stream);
            else if (CDReader.Detect(stream)) /* Iso9660 */
                return new CDReader(stream, true);
            else if (NtfsFileSystem.Detect(stream))
                return new NtfsFileSystem(stream);
            else if (SquashFileSystemReader.Detect(stream))
                return new SquashFileSystemReader(stream);
            else if (UdfReader.Detect(stream))
                return new UdfReader(stream);
            else if (XfsFileSystem.Detect(stream))
                return new XfsFileSystem(stream);

            throw new InvalidDataException("Failed to find supported file system");
        }

        private bool IsFileSystemReadOnly(IFileSystem fs)
        {
            return fs is ReadOnlyDiscFileSystem ||
                fs is BtrfsFileSystem ||
                fs is ExtFileSystem ||
                fs is HfsPlusFileSystem ||
                fs is CDReader /* Iso9660 */ || 
                fs is SquashFileSystemReader ||
                fs is UdfReader ||
                fs is XfsFileSystem;
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
            DiscFileSystemInfo pathInfo = null;

            if (mFileSystem.FileExists(path))
                pathInfo = mFileSystem.GetFileInfo(path);
            else if (mFileSystem.DirectoryExists(path))
                pathInfo = mFileSystem.GetDirectoryInfo(path);
            else
                throw new IOException("Could not find file");

            var attributes = pathInfo.Attributes;
            var parentIsRoot = (pathInfo.Parent != null && pathInfo.Parent.FullName == "\\");

            if ((pathInfo is DiscDirectoryInfo) && parentIsRoot && 
                (pathInfo.Name == "$RECYCLE.BIN" || pathInfo.Name == "System Volume Information"))
			{
				attributes |= FileAttributes.Hidden;
				attributes |= FileAttributes.System;
			}

			return new FileInformation
			{
				Attributes = attributes,
				CreationTime = pathInfo.CreationTime,
				FileName = pathInfo.Name,
				LastAccessTime = pathInfo.LastAccessTime,
				LastWriteTime = pathInfo.LastWriteTime,
				Length = (mFileSystem.FileExists(path) ? mFileSystem.GetFileLength(path) : 0)
			};
		}

        private bool IsBlacklisted(string file, bool skipFlag = false)
        {
            if (string.IsNullOrWhiteSpace(file))
                return false;

            if (mFileSystemIsNTFS)
                return kNtfsFileSecurityHardBlacklist.Any(t => file.StartsWith(t));

            if (mOptions.FullAccess)
                return false;

            if (skipFlag)
                return false;

            if (mFileSystemIsNTFS)
                return kNtfsFileSecurityBlacklist.Any(t => file.StartsWith(t));

            return false;
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

                this.Flush();
            }
            catch (Exception) { }
        }

        public void CloseFile(string fileName, DokanFileInfo info)
		{
			try
            {
                (info.Context as Stream)?.Dispose();
                info.Context = null;

                this.Flush();
            }
			catch (Exception) { }
		}

		public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
		{
			try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return DokanResult.InvalidName;

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

                            if (IsBlacklisted(fileName))
                                return DokanResult.AccessDenied;

                            mFileSystem.CreateDirectory(fileName);
                            this.Flush();
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

                    if (IsBlacklisted(fileName, readAccess))
                        return DokanResult.AccessDenied;

                    info.Context = mFileSystem.OpenFile(fileName, mode, 
                        readAccess ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite);
                    this.Flush();
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

                if (mFileSystem.FileExists(fileName))
                    return NtStatus.NotADirectory;
                else if (!mFileSystem.DirectoryExists(fileName))
                    return DokanResult.FileNotFound;

                if (mFileSystem.GetDirectoryInfo(fileName).GetFileSystemInfos().Length != 0)
                    return NtStatus.DirectoryNotEmpty;

                if (IsBlacklisted(fileName))
                    return DokanResult.AccessDenied;

                this.Flush();
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

                if (mFileSystem.DirectoryExists(fileName))
                    return DokanResult.AccessDenied;

                if (!mFileSystem.FileExists(fileName))
                    return DokanResult.FileNotFound;

                if (IsBlacklisted(fileName))
                    return DokanResult.AccessDenied;

                this.Flush();
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

                var realfiles = mFileSystem.GetDirectoryInfo(fileName)
                    .GetFileSystemInfos()
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
                this.Flush();
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

                if (fileName.StartsWith("\\$RECYCLE.BIN\\") || fileName.StartsWith("\\System Volume Information\\"))
                {
                    if (info.IsDirectory)
                        return DokanResult.PathNotFound;
                    else
                        return DokanResult.FileNotFound;
                }

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

            if (mFileSystemIsNTFS)
            {
                var ntfsFileSystem = (NtfsFileSystem)mFileSystem;

                try
                {
                    if (info.IsDirectory)
                    {
                        if (!mFileSystem.DirectoryExists(fileName))
                            return DokanResult.PathNotFound;

                        security = new DirectorySecurity();
                    }
                    else
                    {
                        if (!mFileSystem.FileExists(fileName))
                            return DokanResult.FileNotFound;

                        security = new FileSecurity();
                    }

                    var secDescriptor = ntfsFileSystem.GetSecurity(fileName);
                    var binaryForm = new byte[secDescriptor.BinaryLength];

                    secDescriptor.GetBinaryForm(binaryForm, 0);

                    if (sections == 0)
                        security.SetSecurityDescriptorBinaryForm(binaryForm);
                    else
                        security.SetSecurityDescriptorBinaryForm(binaryForm, sections);

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

                if (mFileSystemIsNTFS)
                {
                    features |= FileSystemFeatures.UnicodeOnDisk |
                        FileSystemFeatures.PersistentAcls;
                }

                if (!(mFileSystem is FatFileSystem))
                    features |= FileSystemFeatures.CasePreservedNames |
                        FileSystemFeatures.CaseSensitiveSearch;

                if (mFileSystem is DiscFileSystem discFileSystem)
                    fileSystemName = discFileSystem.FriendlyName;
                else
                    fileSystemName = mFileSystem.GetType().Name;

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
                if (IsBlacklisted(oldName) || IsBlacklisted(newName))
                    return DokanResult.AccessDenied;

                (info.Context as Stream)?.Dispose();
                info.Context = null;

                if (!mFileSystem.Exists(newName))
                {
                    info.Context = null;
                    if (info.IsDirectory)
                        mFileSystem.MoveDirectory(oldName, newName);
                    else
                        mFileSystem.MoveFile(oldName, newName);

                    this.Flush();
                    return DokanResult.Success;
                }
                else if (replace)
                {
                    info.Context = null;
                    if (info.IsDirectory)
                        return DokanResult.AccessDenied;
                    
                    mFileSystem.MoveFile(oldName, newName, replace);

                    this.Flush();
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
                var memoryMapped = (info.Context == null);
                if (memoryMapped)
                    info.Context = mFileSystem.OpenFile(fileName, FileMode.Open, System.IO.FileAccess.Read);

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
                if (IsBlacklisted(fileName))
                    return DokanResult.AccessDenied;

                (info.Context as Stream)?.SetLength(length);
                this.Flush();
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
                if (IsBlacklisted(fileName))
                    return DokanResult.AccessDenied;

                (info.Context as Stream)?.SetLength(length);
                this.Flush();
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

                if (IsBlacklisted(fileName))
                    return DokanResult.AccessDenied;

                mFileSystem.SetAttributes(fileName, attributes);
                this.Flush();
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
            if (mFileSystem is NtfsFileSystem ntfsFileSystem)
            {
                try
                {
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

                    if (IsBlacklisted(fileName))
                        return DokanResult.Success;

                    var securityDescriptor = new RawSecurityDescriptor(
                        security.GetSecurityDescriptorBinaryForm(), 0);
                    
                    ntfsFileSystem.SetSecurity(fileName, securityDescriptor);

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

            return NtStatus.NotImplemented;
		}

		public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info)
        {
            try
            {
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

                if (IsBlacklisted(fileName))
                    return DokanResult.AccessDenied;

                if (creationTime.HasValue)
                    mFileSystem.SetCreationTime(fileName, creationTime.Value);

                if (lastAccessTime.HasValue)
                    mFileSystem.SetLastAccessTime(fileName, lastAccessTime.Value);

                if (lastWriteTime.HasValue)
                    mFileSystem.SetLastWriteTime(fileName, lastWriteTime.Value);

                this.Flush();
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
                if (IsBlacklisted(fileName))
                    return DokanResult.AccessDenied;

                var memoryMapped = (info.Context == null);
                if (memoryMapped)
                    info.Context = mFileSystem.OpenFile(fileName, FileMode.Open, System.IO.FileAccess.ReadWrite);

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
