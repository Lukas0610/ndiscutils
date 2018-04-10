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
using System.Reflection;

using CommandLine;
using DokanNet;
using FluentFTP;
using nDiscUtils.Core;
using nDiscUtils.IO.FileSystem.Implementations;
using nDiscUtils.Modules;
using nDiscUtils.Mounting;
using nDiscUtils.Options;
using static nDiscUtils.Core.ModuleHelpers;
using static nDiscUtils.Core.nConsole;
using static nDiscUtils.Core.ReturnCodes;

namespace nDiscUtils
{

    public static class Program
    {

        public static int PPID = 0;
        public static long CHWND = 0;

#if STANDALONE
        static Program()
        {
            CosturaUtility.Initialize();
        }
#endif

        private class MountOptionsImpl : BaseMountOptions
        {

        }

        public static int Main(string[] args)
        {
            InitializeSystemConsole();
            var ftp = new FtpClient("localhost", 21, "admin", "");
            ftp.Connect();
            var fs = new FtpFileSystem(ftp);
            var fsMount = new SimpleFileSystemMountPoint(fs, new MountOptionsImpl() { Letter = 'T', FullAccess = true, ShowHiddenFiles = true, Threads = 2 });
            fsMount.Mount("T:", DokanOptions.FixedDrive, 1, 110, TimeSpan.FromSeconds(5), "nDiscUtils\\T", new DokanNullLogger());
            return 0;

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var buildArch = (Is64BitBuild ? 64 : 86);

            if (!IsLinux)
            {
                ServiceImpl.ServiceEarlyParseArguments(ref args);
                InitializeSystemConsole();
                ServiceImpl.ServiceEarlyMain(args);
            }

            if (!IsServiceEnvironment)
            {
                WriteLine("{0} {1}.{2}.{3}-{4}  {5:dd-MM-yyyy HH\\:mm\\:ss}  [x{6}-built]",
                    assembly.GetCustomAttribute<AssemblyProductAttribute>().Product,
                    version.Major, version.Minor, version.Build, version.Revision,
                    assembly.GetLinkerTime(), buildArch);

                WriteLine("{0}. Licensed under GPLv3.",
                    assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
            }

            if (!IsLinux && !ServiceImpl.ServiceMain(args))
            {
                CloseParent();
                return 0;
            }

            WriteLine("Running as {0}.", Environment.UserName);
            WriteLine();

            var result = Parser.Default.ParseArguments<
                Benchmark.Options,
                Clone.Options,
                Compare.Options,
                Erase.Options,
                ListDisks.Options,
                ListPartitions.Options,
                MakeDirectoryImage.Options,
                MakeImage.Options,
                MountImage.Options,
                MountPartition.Options,
                Ramdisk.Options,
                Security.Options,
                Smart.Options,
                Sync.Options
            >(args).MapResult(
                    (Benchmark.Options opts) => Benchmark.Run(opts),
                    (Clone.Options opts) => Clone.Run(opts),
                    (Compare.Options opts) => Compare.Run(opts),
                    (Erase.Options opts) => Erase.Run(opts),
                    (ListDisks.Options opts) => ListDisks.Run(opts),
                    (ListPartitions.Options opts) => ListPartitions.Run(opts),
                    (MakeDirectoryImage.Options opts) => MakeDirectoryImage.Run(opts),
                    (MakeImage.Options opts) => MakeImage.Run(opts),
                    (MountImage.Options opts) => MountImage.Run(opts),
                    (MountPartition.Options opts) => MountPartition.Run(opts),
                    (Ramdisk.Options opts) => Ramdisk.Run(opts),
                    (Security.Options opts) => Security.Run(opts),
                    (Smart.Options opts) => Smart.Run(opts),
                    (Sync.Options opts) => Sync.Run(opts),
                    (errcode) => -INVALID_ARGUMENT
                );
            
            if (!IsLinux)
                CloseParent();

            return result;
        }

        private static void CloseParent()
        {
            if (PPID > 0)
            {
                // ugly hack...
                NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_C_EVENT, (uint)PPID);
            }
        }

    }

}
