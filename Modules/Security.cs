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
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

using CommandLine;

using nDiscUtils.Core;
using nDiscUtils.Options;

using ProcessPrivileges;

using static nDiscUtils.Core.ModuleHelpers;
using static nDiscUtils.Core.nConsole;
using static nDiscUtils.Core.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class Security
    {

        private static PrincipalContext mPrincipalContext;

        private static UserPrincipal mUserPrincipalQuery;
        private static PrincipalSearcher mUserPrincipalSearcher;
        private static PrincipalSearchResult<Principal> mUserPrincipals;

        private static GroupPrincipal mGroupPrincipalQuery;
        private static PrincipalSearcher mGroupPrincipalSearcher;
        private static PrincipalSearchResult<Principal> mGroupPrincipals;

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            Logger.Info("Initializing global principal context");
            if (opts.Domain != null)
            {
                mPrincipalContext = new PrincipalContext(ContextType.Domain, opts.Domain);
            }
            else
            {
                mPrincipalContext = new PrincipalContext(ContextType.Machine);
            }

            Logger.Info("Looking for all user principals");
            mUserPrincipalQuery = new UserPrincipal(mPrincipalContext);
            mUserPrincipalSearcher = new PrincipalSearcher(mUserPrincipalQuery);
            mUserPrincipals = mUserPrincipalSearcher.FindAll();

            Logger.Info("Looking for all group principals");
            mGroupPrincipalQuery = new GroupPrincipal(mPrincipalContext);
            mGroupPrincipalSearcher = new PrincipalSearcher(mGroupPrincipalQuery);
            mGroupPrincipals = mGroupPrincipalSearcher.FindAll();
            
            if (opts.Owner != null)
            {
                Logger.Info("Trying to look up owner identifier");
                if (!TryLookupUserID(opts.Owner))
                {
                    Logger.Info("Could not find any principal for the owner-ID \"{0}\"", opts.Owner);
                    return INVALID_ARGUMENT;
                }
            }

            if (opts.Group != null)
            {
                Logger.Info("Trying to look up group identifier");
                if (!TryLookupGroupID(opts.Group))
                {
                    Logger.Info("Could not find any principal for the group-ID \"{0}\"", opts.Group);
                    return INVALID_ARGUMENT;
                }
            }
            
            var privilegeEnabler = new PrivilegeEnabler(Process.GetCurrentProcess(),
                Privilege.Audit, Privilege.Backup, Privilege.EnableDelegation,
                Privilege.Restore, Privilege.Security, Privilege.TakeOwnership);

            var returnCode = StartInternal(opts);

            privilegeEnabler.Dispose();

            if (returnCode == SUCCESS)
                Logger.Fine("Finished!");
            else
                Logger.Error("One or more errors occurred...");
            
            WaitForUserExit();
            return SUCCESS;
        }

        private static int StartInternal(Options opts)
        {
            if (Directory.Exists(opts.Target))
                return StartInternalDirectory(opts);
            else
                return StartInternalFile(opts);
        }

        private static int StartInternalFile(Options opts)
        {
            if (!File.Exists(opts.Target))
            {
                Logger.Error("Could not find file \"{0}\"", opts.Target);
                return INVALID_ARGUMENT;
            }

            var file = new FileInfo(opts.Target);
            if (!AlterFileSecurity(opts, file))
                return ERROR;

            return SUCCESS;
        }

        private static int StartInternalDirectory(Options opts)
        {
            long fileSize = 0, fileCount = 0, directoryCount = 0;
            List<FileInfo> fileList = null;
            List<DirectoryInfo> directoryList = null;

            WriteFormatRight(ContentLeft + ContentWidth, ContentTop, "Files: {0,10} / {1,10}", 0, 0);

            var baseDirectory = new DirectoryInfo(opts.Target);
            IndexDirectory(baseDirectory, opts.Threads, ref fileList, ref directoryList, ref fileSize, ref fileCount, ref directoryCount,
                (iFileCount, iDirectoryCount) =>
                {
                    WriteFormatRight(ContentLeft + ContentWidth, ContentTop, "Files: {0,10} / {1,10}", 0, iFileCount);
                });

            Logger.Info("Updating security of indexed directories");
            for (int i = 0; i < directoryCount; i++)
            {
                var directory = directoryList[i];

                PrintLimitedName(directory.FullName);
                if (!AlterDirectorySecurity(opts, directory))
                    return ERROR;
            }

            Logger.Info("Updating security of indexed files");
            for (int i = 0; i < fileCount; i++)
            {
                var file = fileList[i];

                PrintLimitedName(file.FullName);
                if (!AlterFileSecurity(opts, file))
                    return ERROR;
            }

            return SUCCESS;
        }

        private static bool TryLookupUserID(string id)
        {
            Principal principal = null;

            if (IsValidSID(id))
                principal = SearchUserBySID(id);
            else
                principal = SearchUserByName(id);

            return (principal != null);
        }

        private static bool TryLookupGroupID(string id)
        {
            Principal principal = null;

            if (IsValidSID(id))
                principal = SearchGroupBySID(id);
            else
                principal = SearchGroupByName(id);

            return (principal != null);
        }

        private static void PrintLimitedName(string name)
        {
            if (Logger.IsVerbose)
            {
                Logger.Verbose(name);
            }
            else
            {
                if (name.Length < BufferWidth)
                {
                    Console.Write("{0}\r", name);
                }
                else
                {
                    Console.Write("{0}...\r", name.Substring(0, BufferWidth - 3));
                }
            }
        }

        private static bool AlterDirectorySecurity(Options opts, DirectoryInfo entry)
        {
            var accessControl = entry.GetAccessControl(AccessControlSections.All);
            accessControl = AlterSecurityImpl(opts, accessControl);

            if (accessControl == null)
                return false;

            entry.SetAccessControl(accessControl);
            return true;
        }

        private static bool AlterFileSecurity(Options opts, FileInfo entry)
        {
            var accessControl = entry.GetAccessControl(AccessControlSections.All);
            accessControl = AlterSecurityImpl(opts, accessControl);

            if (accessControl == null)
                return false;

            entry.SetAccessControl(accessControl);
            return true;
        }

        private static T AlterSecurityImpl<T>(Options opts, T security) where T : FileSystemSecurity
        {
            if (opts.Owner != null)
            {
                Principal ownerPrincipal = null;

                if (IsValidSID(opts.Owner))
                    ownerPrincipal = SearchUserBySID(opts.Owner);
                else
                    ownerPrincipal = SearchUserByName(opts.Owner);

                if (ownerPrincipal == null)
                {
                    Logger.Error("Failed to find principal for \"{0}\"", opts.Owner);
                    return null;
                }

                security.SetOwner(ownerPrincipal.Sid);
            }

            if (opts.Group != null)
            {
                Principal groupPrincipal = null;

                if (IsValidSID(opts.Group))
                    groupPrincipal = SearchGroupBySID(opts.Group);
                else
                    groupPrincipal = SearchGroupByName(opts.Group);

                if (groupPrincipal == null)
                {
                    Logger.Error("Failed to find principal for \"{0}\"", opts.Owner);
                    return null;
                }

                security.SetGroup(groupPrincipal.Sid);
            }
            
            return security;
        }

        private static bool IsValidSID(string input)
        {
            return Regex.IsMatch(input, @"^S-\d-\d+-(\d+-){1,14}\d+$");
        }

        private static Principal SearchUserBySID(string sid)
        {
            foreach (var pr in mUserPrincipals)
            {
                if (pr.Sid.Value.Equals(sid, StringComparison.OrdinalIgnoreCase))
                {
                    return pr;
                }
            }

            return null;
        }

        private static Principal SearchUserByName(string name)
        {
            foreach (var pr in mUserPrincipals)
            {
                if (pr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    pr.SamAccountName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return pr;
                }
            }

            return null;
        }

        private static Principal SearchGroupBySID(string sid)
        {
            foreach (var pr in mGroupPrincipals)
            {
                if (pr.Sid.Value.Equals(sid, StringComparison.OrdinalIgnoreCase))
                {
                    return pr;
                }
            }

            return null;
        }

        private static Principal SearchGroupByName(string name)
        {
            foreach (var pr in mGroupPrincipals)
            {
                if (pr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    pr.SamAccountName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return pr;
                }
            }

            return null;
        }

        [Verb("security", HelpText = "Alter the security-settings of single files or whole directories")]
        public sealed class Options : BaseOptions
        {
            
            [Value(0, Default = null, HelpText = "Target whose security should be updated. May be a file or a directory", Required = true)]
            public string Target { get; set; }

            [Option('l', "domain", Default = null, HelpText = "Full path of the active directory domain to search for user/group entries in", Required = false)]
            public string Domain { get; set; }

            [Option('o', "owner", Default = null, HelpText = "The new owner of the target, may be a SID or a username. Keep unspecific to not alter.", Required = false)]
            public string Owner { get; set; }

            [Option('g', "group", Default = null, HelpText = "The new group of the target, may be a SID or a group name. Keep unspecific to not alter.", Required = false)]
            public string Group { get; set; }

            [Option('t', "threads", Default = 2, HelpText = "If target is directory: Count of threads used to index files", Required = false)]
            public int Threads { get; set; }

        }

    }

}
