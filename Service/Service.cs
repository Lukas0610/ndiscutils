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
            if (args.Length != 1)
            {
                EventLog.WriteEntry($"Invalid count of arguments received", EventLogEntryType.Error);
                this.Stop();
                return;
            }

            var registryValidation = false;
            var registryValidationData = "";

            RegistryKey subkey = Registry.LocalMachine.OpenSubKey(CommonServiceData.RegistrySubKeyPath, true);
            if (subkey != null)
            {
                var executableValue = subkey.GetValue(CommonServiceData.RegistryExecutableValue, null);

                if (executableValue != null)
                {
                    registryValidationData = $"\"{executableValue}\"";
                    registryValidation = args[0].StartsWith(registryValidationData);
                }

                subkey.Close();
                subkey.Dispose();
            }

            EventLog.WriteEntry($"Received startup command:\n\"{args[0]}\"\n\n" +
                $"Validated against whitelisted executable path:\n\"{registryValidationData}\"\n\n" + 
                $"Validated succeeded: {registryValidation}", EventLogEntryType.Information);

            if (registryValidation)
            {
                if (!ApplicationLoader.StartProcessAndBypassUAC(args[0] + " /SVCR", out var procInfo))
                {
                    EventLog.WriteEntry($"Failed to launch process with elevated permissions", EventLogEntryType.Error);
                }
            }

            this.Stop();
        }

        protected override void OnStop()
        {
        }

    }

}
