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
using System.Reflection;

using CommandLine;

using nDiscUtils.Core;
using nDiscUtils.Modules;
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
        
        public static int Main(string[] args)
        {
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

            var options = new[]
            {
                typeof(Benchmark.Options),
                typeof(Clone.Options),
                typeof(Compare.Options),
                typeof(DeepScan.Options),
                typeof(Erase.Options),
                typeof(ListDisks.Options),
                typeof(ListPartitions.Options),
                typeof(MakeDirectoryImage.Options),
                typeof(MakeImage.Options),
                typeof(MakeRaidImage.Options),
                typeof(MountImage.Options),
                typeof(MountPartition.Options),
                typeof(MountRaidImage.Options),
                typeof(PhysicalCheck.Options),
                typeof(Ramdisk.Options),
                typeof(Security.Options),
                typeof(Smart.Options),
                typeof(Sync.Options),
            };

            var result = INVALID_ARGUMENT;
            var parserResult = Parser.Default.ParseArguments(args, options);

            if (parserResult is Parsed<object> parsed && parsed != null)
            {
                if (parsed.Value is Benchmark.Options) result = Benchmark.Run((Benchmark.Options)parsed.Value);
                else if (parsed.Value is Clone.Options) result = Clone.Run((Clone.Options)parsed.Value);
                else if (parsed.Value is Compare.Options) result = Compare.Run((Compare.Options)parsed.Value);
                else if (parsed.Value is DeepScan.Options) result = DeepScan.Run((DeepScan.Options)parsed.Value);
                else if (parsed.Value is Erase.Options) result = Erase.Run((Erase.Options)parsed.Value);
                else if (parsed.Value is ListDisks.Options) result = ListDisks.Run((ListDisks.Options)parsed.Value);
                else if (parsed.Value is ListPartitions.Options) result = ListPartitions.Run((ListPartitions.Options)parsed.Value);
                else if (parsed.Value is MakeDirectoryImage.Options) result = MakeDirectoryImage.Run((MakeDirectoryImage.Options)parsed.Value);
                else if (parsed.Value is MakeImage.Options) result = MakeImage.Run((MakeImage.Options)parsed.Value);
                else if (parsed.Value is MakeRaidImage.Options) result = MakeRaidImage.Run((MakeRaidImage.Options)parsed.Value);
                else if (parsed.Value is MountImage.Options) result = MountImage.Run((MountImage.Options)parsed.Value);
                else if (parsed.Value is MountPartition.Options) result = MountPartition.Run((MountPartition.Options)parsed.Value);
                else if (parsed.Value is MountRaidImage.Options) result = MountRaidImage.Run((MountRaidImage.Options)parsed.Value);
                else if (parsed.Value is Ramdisk.Options) result = Ramdisk.Run((Ramdisk.Options)parsed.Value);
                else if (parsed.Value is PhysicalCheck.Options) result = PhysicalCheck.Run((PhysicalCheck.Options)parsed.Value);
                else if (parsed.Value is Security.Options) result = Security.Run((Security.Options)parsed.Value);
                else if (parsed.Value is Smart.Options) result = Smart.Run((Smart.Options)parsed.Value);
                else if (parsed.Value is Sync.Options) result = Sync.Run((Sync.Options)parsed.Value);
            }

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
