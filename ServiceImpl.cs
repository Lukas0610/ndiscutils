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
using System.Reflection;
using System.ServiceProcess;
using Microsoft.Win32;
using nDiscUtils.Properties;
using nDiscUtils.Service;

namespace nDiscUtils
{

    public static class ServiceImpl
    {

        private static bool IsValidServiceControlMode(string[] args)
        {
            return args.Length == 2 && args[0] == "/SVCC" && (args[1] == "/I" || args[1] == "/U" || args[1] == "/C");
        }

        private static bool IsValidServiceValidationMode(string[] args)
        {
            return args.Length == 1 && args[0] == "/SVCV";
        }

        public static void ServiceEarlyMain(string[] args)
        {            
            if (IsValidServiceControlMode(args))
            {
                Console.WriteLine("*** ENTERING SERVICE CONTROL MODE !!!");
                Console.WriteLine();
            }
            else if (IsValidServiceValidationMode(args))
            {
                Console.WriteLine("*** ENTERING SERVICE VALIDATION MODE !!!");
                Console.WriteLine();
            }
        }

        public static bool ServiceMain(ref string[] args)
        {
            if (IsValidServiceControlMode(args))
            {
                ServiceControlMain(args[1]);
                return false;
            }
            else if (IsValidServiceValidationMode(args))
            {
                var service = TryFindService();

                if (service == null)
                {
                    var hasOutdatedService = FindOutdatedService((outdatedService) =>
                    {
                        Console.WriteLine("*** OUTDATED SERVICE INSTALLED: {0} !!!", outdatedService.ServiceName);
                        Console.WriteLine("*** LATEST SERVICE IS: {0} ---", CommonServiceData.FullName);
                        return true;
                    });

                    if (hasOutdatedService)
                        goto validationExit;

                    Console.WriteLine("*** SERVICE NOT INSTALLED !!!");
                }
                else
                {
                    Console.WriteLine("*** LATEST SERVICE INSTALLED: {0} ---", service.ServiceName);
                }

validationExit:
                Console.WriteLine();
                return false;
            }
            else if (args.Length != 0 && args[args.Length - 1] == "/SVCR")
            {
                // remove /SVCR key-param from argument list
                var argsList = new List<string>(args);
                argsList.RemoveAt(args.Length - 1);
                args = argsList.ToArray();

                // fall through service-barrier as this run is intended
                return true;
            }
            else
            {
                var service = TryFindService();

                if (service == null)
                {
                    var hasOutdatedService = FindOutdatedService((outdatedService) =>
                    {
                        Console.WriteLine("*** FOUND OUTDATED SERVICE: {0} !!!", outdatedService.ServiceName);

                        Console.WriteLine("*** PRESS ANY KEY TO UPDATE SERVICE ...");
                        Console.ReadKey(true);

                        Console.WriteLine("*** UPDATING SERVICE !!!");

                        Console.WriteLine("    *** STOPPING SERVICE ...");
                        if (outdatedService.Status == ServiceControllerStatus.Running)
                        {
                            outdatedService.Stop();
                            outdatedService.WaitForStatus(ServiceControllerStatus.Stopped);
                        }
                        Console.WriteLine("        *** SERVICE STOPPED ---");

                        Console.WriteLine("    *** UNINSTALLING SERVICE ...");
                        if (UninstallService())
                        {
                            Console.WriteLine("        *** SERVICE UNINSTALLED ---");
                        }
                        else
                        {
                            Console.WriteLine("*** FAILED TO UNINSTALL SERVICE !!!");

                            Console.WriteLine("*** PRESS ANY KEY TO EXIT ...");
                            Console.ReadKey(true);

                            return false;
                        }

                        Console.WriteLine("    *** UPDATING SERVICE ...");

                        if (File.Exists(CommonServiceData.ServicePath))
                            File.Decrypt(CommonServiceData.ServicePath);

                        File.WriteAllBytes(CommonServiceData.ServicePath, Resources.ndiscutilsprivsvc);

                        Console.WriteLine("        *** SERVICE UPDATED ---");

                        Console.WriteLine("    *** INSTALLING SERVICE ...");
                        if (InstallService())
                        {
                            Console.WriteLine("        *** SERVICE INSTALLED ---");
                        }
                        else
                        {
                            Console.WriteLine("*** FAILED TO INSTALL SERVICE !!!");

                            Console.WriteLine("*** PRESS ANY KEY TO EXIT ...");
                            Console.ReadKey(true);

                            return false;
                        }

                        Console.WriteLine("*** SERVICE SUCCESSFULLY UPDATED ---");

                        Console.WriteLine("*** PRESS ANY KEY TO CONTINUE ...");
                        Console.ReadKey(true);

                        return true;
                    });

                    if (hasOutdatedService)
                        goto start;

                    return true;
                }

start:
                Console.WriteLine("Attempting to launch nDiscUtils through privilege elevation service...");
                
                service.Start(new string[] { Environment.CommandLine, Directory.GetCurrentDirectory() });
                return false;
            }
        }
        
        private static void ServiceControlMain(string mode)
        {
            if (mode == "/I")
            {
                Console.WriteLine("*** INSTALLING SERVICE ...");
                if (InstallService())
                    Console.WriteLine("*** SERVICE INSTALLED ---");
                else
                    Console.WriteLine("*** FAILED TO INSTALL SERVICE !!!");
            }
            else if (mode == "/U")
            {
                Console.WriteLine("*** UNINSTALLING SERVICE ...");
                if (UninstallService())
                    Console.WriteLine("*** SERVICE UNINSTALLED ---");
                else
                    Console.WriteLine("*** FAILED TO UNINSTALL SERVICE !!!");
            }
            else if (mode == "/C")
            {
                Console.WriteLine("*** UNINSTALLING SERVICE ...");
                if (UninstallService())
                {
                    Console.WriteLine("*** SERVICE UNINSTALLED ---");

                    Console.WriteLine("*** INSTALLING SERVICE ...");
                    if (InstallService())
                        Console.WriteLine("*** SERVICE INSTALLED ---");
                    else
                        Console.WriteLine("*** FAILED TO INSTALL SERVICE !!!");
                }
                else
                {
                    Console.WriteLine("*** FAILED TO UNINSTALL SERVICE !!!");
                }
            }

            Console.WriteLine();
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

        private static bool InstallService()
        {
            if (!File.Exists(CommonServiceData.ServicePath))
            {
                File.WriteAllBytes(CommonServiceData.ServicePath, Resources.ndiscutilsprivsvc);
            }

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

    }

}
