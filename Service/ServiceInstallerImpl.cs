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
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace nDiscUtils.Service
{

    [RunInstaller(true)]
    public class ServiceInstallerImpl : Installer
    {
        
        public ServiceInstallerImpl()
        {
            // Instantiate installers for process and services.
            var processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem,
                Username = null,
                Password = null
            };

            var serviceInstall = new ServiceInstaller
            {
                StartType = ServiceStartMode.Manual,
                ServiceName = CommonServiceData.FullName,
                DisplayName = "nDiscUtils Privilege Elevation Service",
                Description = "Background privilege elevation service for nDiscUtils"
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstall);
        }

    }

}
