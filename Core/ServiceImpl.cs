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
using System.IO;
using System.Linq;
#if STANDALONE
using System.Reflection;
#endif
using System.ServiceProcess;
#if STANDALONE
using Microsoft.Win32;
#endif
using nDiscUtils.Service;

namespace nDiscUtils.Core
{

    public static class ServiceImpl
    {

        public static void ServiceEarlyParseArguments(ref string[] args)
        {
            if (args.Length >= 1 && args[args.Length - 1] == "/SVCR")
            {
                if (!File.Exists(CommonServiceData.ServicePath))
                    return;

                var svcArgCount = 1;

                if (args.Length >= 2)
                {
                    if (args[args.Length - 2].StartsWith("/CHWND:"))
                    {
                        Program.CHWND = long.Parse(args[args.Length - 2].Substring(7));
                        svcArgCount++;
                    }
                }

                if (args.Length >= 3)
                {
                    if (args[args.Length - 3].StartsWith("/PPID:"))
                    {
                        Program.PPID = int.Parse(args[args.Length - 3].Substring(6));

                        NativeMethods.FreeConsole();
                        NativeMethods.SetParent(Process.GetCurrentProcess().MainWindowHandle, new IntPtr(Program.CHWND));
                        NativeMethods.AttachConsole((uint)Program.PPID);

                        svcArgCount++;
                    }
                }

                if (Program.CHWND > 0)
                {
                    // reassign system console buffer
                    nConsole.InitializeSystemConsole(new IntPtr(Program.CHWND));
                }

                // remove /SVCR key-param from argument list
                var argsList = new List<string>(args);
                argsList.RemoveRange(args.Length - svcArgCount, svcArgCount);
                args = argsList.ToArray();

                // tell program services launched this instance
                ModuleHelpers.IsServiceEnvironment = true;
            }
        }

#if STANDALONE
        private static bool IsValidServiceControlMode(string[] args)
        {
            return args.Length == 2 && args[0] == "/SVCC" && (args[1] == "/I" || args[1] == "/U" || args[1] == "/C");
        }
#endif

        private static bool IsValidServiceValidationMode(string[] args)
        {
            return args.Length == 1 && args[0] == "/SVCV";
        }
        
        public static void ServiceEarlyMain(string[] args)
        {
#if STANDALONE
            if (IsValidServiceControlMode(args))
            {
                nConsole.WriteLine("*** ENTERING SERVICE CONTROL MODE !!!");
                nConsole.WriteLine();
            }
            else
#endif
            if (IsValidServiceValidationMode(args))
            {
                nConsole.WriteLine("*** ENTERING SERVICE VALIDATION MODE !!!");
                nConsole.WriteLine();
            }
        }

        public static bool ServiceMain(string[] args)
        {
#if STANDALONE
            if (IsValidServiceControlMode(args))
            {
                ServiceControlMain(args[1]);
                return false;
            }
            else 
#endif
            if (IsValidServiceValidationMode(args))
            {
                var service = TryFindService();

                if (service == null)
                {
                    var hasOutdatedService = FindOutdatedService((outdatedService) =>
                    {
                        nConsole.WriteLine("*** OUTDATED SERVICE INSTALLED: {0} !!!", outdatedService.ServiceName);
                        nConsole.WriteLine("*** LATEST SERVICE IS: {0} ---", CommonServiceData.FullName);
                        return true;
                    });

                    if (hasOutdatedService)
                        goto validationExit;

                    nConsole.WriteLine("*** SERVICE NOT INSTALLED !!!");
                }
                else
                {
                    nConsole.WriteLine("*** LATEST SERVICE INSTALLED: {0} ---", service.ServiceName);
                }

validationExit:
                nConsole.WriteLine();
                return false;
            }
            else if (ModuleHelpers.IsServiceEnvironment)
            {
                return true;
            }
            else
            {
                if (!File.Exists(CommonServiceData.ServicePath))
                    return true;

                var service = TryFindService();

                if (service == null)
                {
                    var hasOutdatedService = FindOutdatedService((outdatedService) =>
                    {
                        nConsole.WriteLine("*** FOUND OUTDATED SERVICE: {0} !!!", outdatedService.ServiceName);
#if !STANDALONE
                        while (true) ;
#else
                        nConsole.WriteLine("*** UPDATING SERVICE !!!");

                        nConsole.WriteLine("    *** STOPPING SERVICE ...");
                        if (outdatedService.Status == ServiceControllerStatus.Running)
                        {
                            outdatedService.Stop();
                            outdatedService.WaitForStatus(ServiceControllerStatus.Stopped);
                        }
                        nConsole.WriteLine("        *** SERVICE STOPPED ---");

                        nConsole.WriteLine("    *** UNINSTALLING SERVICE ...");
                        if (UninstallService())
                        {
                            nConsole.WriteLine("        *** SERVICE UNINSTALLED ---");
                        }
                        else
                        {
                            nConsole.WriteLine("*** FAILED TO UNINSTALL SERVICE !!!");

                            nConsole.WriteLine("*** PRESS ANY KEY TO EXIT ...");
                            Console.ReadKey(true);

                            return false;
                        }

                        nConsole.WriteLine("    *** UPDATING SERVICE ...");

                        ExtractService();

                        nConsole.WriteLine("        *** SERVICE UPDATED ---");

                        nConsole.WriteLine("    *** INSTALLING SERVICE ...");
                        if (InstallService())
                        {
                            nConsole.WriteLine("        *** SERVICE INSTALLED ---");
                        }
                        else
                        {
                            nConsole.WriteLine("*** FAILED TO INSTALL SERVICE !!!");

                            nConsole.WriteLine("*** PRESS ANY KEY TO EXIT ...");
                            Console.ReadKey(true);

                            return false;
                        }

                        nConsole.WriteLine("*** SERVICE SUCCESSFULLY UPDATED ---");

                        nConsole.WriteLine("*** PRESS ANY KEY TO CONTINUE ...");
                        Console.ReadKey(true);

                        return true;
#endif
                    });

                    if (hasOutdatedService)
                        goto start;

                        return true;
                }

#pragma warning disable CS0164
start:
#pragma warning restore CS0164

                nConsole.WriteLine("Attempting to re-launch nDiscUtils with privilege elevation service...");
                nConsole.WriteLine();

                var currentProc = Process.GetCurrentProcess();

                service.Start(new string[]
                {
                    $"{Environment.CommandLine} /PPID:{currentProc.Id.ToString()} /CHWND:{currentProc.MainWindowHandle.ToInt64()}",
                    Directory.GetCurrentDirectory()
                });
                
                while (true) { }
            }
        }
        private static ServiceController TryFindService()
        {
            var services = ServiceController.GetServices();
            return services
                .Where(s => s.ServiceName == CommonServiceData.FullName)
                .FirstOrDefault();
        }

        private static bool FindOutdatedService(Func<ServiceController, bool> callback)
        {
            var services = ServiceController.GetServices();

            for (int i = CommonServiceData.Version - 1; i > 0; i--)
            {
                var outdatedService = services
                    .Where(s => s.ServiceName == (CommonServiceData.BaseName + i))
                    .FirstOrDefault();

                if (outdatedService != null)
                {
                    return callback(outdatedService);
                }
            }

            return false;
        }
        
#if STANDALONE

        private static void ServiceControlMain(string mode)
        {
            if (mode == "/I")
            {
                nConsole.WriteLine("*** INSTALLING SERVICE ...");
                if (InstallService())
                    nConsole.WriteLine("*** SERVICE INSTALLED ---");
                else
                    nConsole.WriteLine("*** FAILED TO INSTALL SERVICE !!!");
            }
            else if (mode == "/U")
            {
                nConsole.WriteLine("*** UNINSTALLING SERVICE ...");
                if (UninstallService())
                    nConsole.WriteLine("*** SERVICE UNINSTALLED ---");
                else
                    nConsole.WriteLine("*** FAILED TO UNINSTALL SERVICE !!!");
            }
            else if (mode == "/C")
            {
                nConsole.WriteLine("*** UNINSTALLING SERVICE ...");
                if (UninstallService())
                {
                    nConsole.WriteLine("*** SERVICE UNINSTALLED ---");

                    nConsole.WriteLine("*** INSTALLING SERVICE ...");
                    if (InstallService())
                        nConsole.WriteLine("*** SERVICE INSTALLED ---");
                    else
                        nConsole.WriteLine("*** FAILED TO INSTALL SERVICE !!!");
                }
                else
                {
                    nConsole.WriteLine("*** FAILED TO UNINSTALL SERVICE !!!");
                }
            }

            nConsole.WriteLine();
        }

        private static bool InstallService()
        {
           ExtractService();

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = CommonServiceData.ServicePath,
                    Arguments = "--install"
                }
            };

            proc.Start();
            proc.WaitForExit();

            var result = ServiceController.GetServices().Any(s => s.ServiceName == CommonServiceData.FullName);
            if (result)
            {
                RegistryKey subkey = Registry.LocalMachine.OpenSubKey(CommonServiceData.RegistrySubKeyPath, true);
                if (subkey == null)
                    subkey = Registry.LocalMachine.CreateSubKey(CommonServiceData.RegistrySubKeyPath, true);

                subkey.SetValue(CommonServiceData.RegistryExecutableValue,
                    Path.GetFullPath(Assembly.GetExecutingAssembly().Location));

                subkey.Close();
                subkey.Dispose();
            }

            return result;
        }

        private static bool UninstallService()
        {
            if (!File.Exists(CommonServiceData.ServicePath))
            {
                nConsole.WriteLine("*** SERVICE EXECUTABLE NOT FOUND !!!");
                return false;
            }

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = CommonServiceData.ServicePath,
                    Arguments = "--uninstall"
                }
            };

            proc.Start();
            proc.WaitForExit();

            var result = !(ServiceController.GetServices().Any(s => s.ServiceName == CommonServiceData.FullName));
            if (result)
            {
                RegistryKey subkey = Registry.LocalMachine.OpenSubKey(CommonServiceData.RegistrySubKeyPath, true);
                if (subkey != null)
                {
                    subkey.DeleteValue(CommonServiceData.RegistryExecutableValue);

                    subkey.Close();
                    subkey.Dispose();
                }

                if (File.Exists(CommonServiceData.ServicePath))
                    File.Delete(CommonServiceData.ServicePath);
            }

            return result;
        }

        private static void ExtractService()
        {
            if (File.Exists(CommonServiceData.ServicePath))
                File.Delete(CommonServiceData.ServicePath);

#if __x64__
            const string resource = "nDiscUtils.bin.x64.Standalone.nDiscUtilsPrivSvc.exe";
#elif __x86__
            const string resource = "nDiscUtils.bin.x86.Standalone.nDiscUtilsPrivSvc.exe";
#endif

            using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            using (var fileStream = new FileStream(CommonServiceData.ServicePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                resourceStream.CopyTo(fileStream);
            }
        }

#endif

    }

}
