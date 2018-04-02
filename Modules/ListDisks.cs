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
using System.Management;
using CommandLine;
using nDiscUtils.Options;
using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class ListDisks
    {

        public static int Run(Options opts)
        {
            RunHelpers(opts);

            var printedDisks = new List<string>();

            const string valueFormat = " {1,-7}{0}{2,-60}{0}{3,-10}{0}{4,-36} ";

            Logger.Info(" {1,-7}{0}{2,-60}{0}{3,-10}{0}{4,36} ",
                " | ", "State", "Name", "Alias", "Size");
            Logger.Info("={1,-7}{0}{2,-60}{0}{3,-10}{0}{4,-36}=",
                "===",
                new string('=', 7), new string('=', 60), 
                new string('=', 10), new string('=', 36));

            var printDisk = new Action<bool, string, string, ulong>((ready, name, alias, size) =>
            {
                if (ready)
                    Logger.Info(valueFormat, " | ", "Ready", name, alias, string.Format("{0,30:N0} Bytes", size));
                else
                    Logger.Info(valueFormat, " | ", "Offline", name, alias, string.Format("{0,36}", "-"));
            });

            // 1. Partitions
            foreach (var drive in DriveInfo.GetDrives())
            {
                printedDisks.Add(drive.Name);
                printDisk(drive.IsReady, drive.Name, "-", (drive.IsReady ? (ulong)drive.TotalSize : 0UL));
            }

            // 2. Volumes
            {
                var managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Volume");
                var managementObjectCollection = managementObjectSearcher.Get();

                foreach (var managementObject in managementObjectCollection)
                {
                    var deviceID = (managementObject["DeviceID"] as string);
                    var name = (managementObject["Name"] as string);
                    var size = (managementObject["Capacity"] as ulong?);

                    if (printedDisks.Contains(deviceID))
                        continue;

                    printedDisks.Add(deviceID);
                    printDisk(size.HasValue, deviceID, (name == deviceID ? "-" : name), (size ?? 0));
                }
            }

            // 3. Disk Drives
            {
                var managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                var managementObjectCollection = managementObjectSearcher.Get();

                foreach (var managementObject in managementObjectCollection)
                {
                    var name = (managementObject["Name"] as string);
                    var size = (managementObject["Size"] as ulong?);

                    if (printedDisks.Contains(name))
                        continue;

                    printedDisks.Add(name);
                    printDisk(size.HasValue, name, "-", (size ?? 0));
                }
            }

            return SUCCESS;
        }

        [Verb("lsdisks", HelpText = "Lists all partitions, volumes and disks available to the system")]
        public sealed class Options : BaseOptions { }

    }

}
