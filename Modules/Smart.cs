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
using System.IO;
using System.Runtime.InteropServices;

using CommandLine;

using nDiscUtils.IO;
using nDiscUtils.Options;

using static nDiscUtils.ModuleHelpers;
using static nDiscUtils.NativeMethods;
using static nDiscUtils.ReturnCodes;

namespace nDiscUtils.Modules
{

    public static class Smart
    {

        public static int Run(Options opts)
        {
            RunHelpers(opts);
            return ReadAndDumpSmart(opts.Drive);
        }

        private static int ReadAndDumpSmart(string path)
        {
            var error = 0;
            var returnCode = SUCCESS;
            var ioctlFlag = false;

            var smartAttributes = new byte[516];
            var smartAttributesPtr = IntPtr.Zero;

            unsafe
            {
                fixed (byte* p = smartAttributes)
                    smartAttributesPtr = (IntPtr)p;
            }

            Logger.Info("Trying to open \"{0}\" at disk layer...", path);
            var diskHandle = 
                CreateFile(
                    path,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    IntPtr.Zero,
                    FileMode.Open,
                    0,
                    IntPtr.Zero
                );

            error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                Logger.Error("Failed to open disk to read S.M.A.R.T. values: Error {0}", error);
                returnCode = ERROR;
                goto exit;
            }

            Logger.Info("Trying to read S.M.A.R.T. attributes...");
            ioctlFlag =
                DeviceIoControl(
                    diskHandle,
                    IOCTL_STORAGE_PREDICT_FAILURE,
                    IntPtr.Zero,
                    0,
                    smartAttributesPtr,
                    (uint)smartAttributes.Length,
                    out var dummy1,
                    IntPtr.Zero
                );

            error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                Logger.Error("Failed to read S.M.A.R.T. values: Error {0}", error);
                returnCode = ERROR;
                goto exit;
            }

            diskHandle.Close();
            diskHandle.Dispose();
            diskHandle = null;

            Logger.Info("");

            var predictFailure = (BitConverter.ToInt32(smartAttributes, 0) != 0);
            if (predictFailure)
            {
                Logger.Warn("****************************************************");
                Logger.Warn("****************************************************");
                Logger.Warn("***                                              ***");
                Logger.Warn("***      IMMINENT  FAILURE  PREDICTED:  YES      ***");
                Logger.Warn("***                                              ***");
                Logger.Warn("****************************************************");
                Logger.Warn("****************************************************");
            }
            else
            {
                Logger.Info("Imminent failure predicted: No");
            }

            Logger.Info("");

            Logger.Info(" {1,-4}{0}{2,-40}{0}{3,-5}{0}{4,-5}{0}{5,-15}", 
                " | ", "ID", "Name", "Value", "Worst", "Data");
            Logger.Info("={1,-4}{0}{2,-40}{0}{3,-5}{0}{4,-5}{0}{5,-15}", 
                "===", 
                new string('=', 4), new string('=', 40), new string('=', 5), 
                new string('=', 5), new string('=', 15));

            for (int i = 0; i < 30; i++)
            {
                var offset = i * 12 + 6;

                var attributeId = smartAttributes[offset + 0];

                var attribute = SmartAttribute.GetAttribute(attributeId, HardDiskType.HDD);
                if (attribute == null)
                    continue;

                var value = smartAttributes[offset + 3];
                var worst = smartAttributes[offset + 4];

                var raw = new byte[8];
                Array.Copy(smartAttributes, offset + 5, raw, 0, 7);
                var data = BitConverter.ToInt64(raw, 0);

                Logger.Info(" 0x{1,-2:X2}{0}{2,-40}{0}{3,-5}{0}{4,-5}{0}0x{5,-15:X12}", 
                    " | ", attributeId, attribute.Name, value, worst, data);
            }

exit:
            return returnCode;
        }

        [Verb("smart", HelpText = "Read and print S.M.A.R.T. values of hard drives")]
        public sealed class Options : BaseOptions
        {

            [Value(0, Default = null, HelpText = "Name of the hard drive from which S.M.A.R.T. values should be read from", Required = false)]
            public string Drive { get; set; }

        }

    }

}
