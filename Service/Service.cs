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
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace nDiscUtils.Service
{

    public class Service : ServiceBase
    {

        public Service()
        {
            this.CanHandlePowerEvent = false;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanShutdown = false;
            this.CanStop = true;

            this.AutoLog = true;
            this.ServiceName = CommonServiceData.FullName;
        }

        protected override void OnStart(string[] args)
        {
            if (args.Length != 2)
            {
                EventLog.WriteEntry($"Invalid count of arguments received", EventLogEntryType.Error);
                this.Stop();
                return;
            }

            var commandLine = args[0];
            var workingDirectory = args[1];

            var registryValidation = false;
            var registryValidationData = "";

            RegistryKey subkey = Registry.LocalMachine.OpenSubKey(CommonServiceData.RegistrySubKeyPath, true);
            if (subkey != null)
            {
                var executableValue = subkey.GetValue(CommonServiceData.RegistryExecutableValue, null);

                if (executableValue != null)
                {
                    registryValidationData = $"\"{executableValue}\"";
                    registryValidation = commandLine.StartsWith(registryValidationData);
                }

                subkey.Close();
                subkey.Dispose();
            }

            var arguments = commandLine.Substring(registryValidationData.Length);
            arguments = $"{arguments} /SVCR";

            EventLog.WriteEntry($"Received startup command:\n\"{commandLine}]>\n\n" +
                $"Validated against whitelisted executable path:\n<[{registryValidationData}]>\n\n" +
                $"Validated succeeded: {registryValidation}\n\n" + 
                $"Parsed command line arguments: <[{arguments}]>", EventLogEntryType.Information);

            if (registryValidation)
            {
                if (commandLine.EndsWith(" --"))
                {
                    var procInfo = new ProcessStartInfo
                    {
                        FileName = registryValidationData,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory
                    };
                    Process.Start(procInfo);
                    EventLog.WriteEntry($"Started background process with (<[{registryValidationData}]>, <[{arguments}]>)");
                }
                else
                {
                    if (!ApplicationLoader.StartProcessAndBypassUAC($"{commandLine} /SVCR", workingDirectory, out var procInfo))
                    {
                        EventLog.WriteEntry($"Failed to launch process with elevated permissions", EventLogEntryType.Error);
                    }
                    else
                    {
                        EventLog.WriteEntry($"Started visible process with (<[{commandLine} /SVCR]>)");
                    }
                }
            }

            this.Stop();
        }

        protected override void OnStop()
        {
        }

    }

}
