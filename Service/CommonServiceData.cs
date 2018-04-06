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

namespace nDiscUtils.Service
{

    public static class CommonServiceData
    {

        public static readonly string BaseName = "nDiscUtilsPrivSvc";

        public static readonly int Version = 1;

        public static readonly string FullName = BaseName + Version;

        public static readonly string ServiceDirectory = "C:\\ProgramData\\nDiscUtils\\";

        public static readonly string ServicePath = ServiceDirectory + "ndiscutilsprivsvc.exe";

        public static readonly string RegistrySubKeyPath = "SOFTWARE\\Lukas Berger\\nDiscUtils";

        public static readonly string RegistryExecutableValue = "PrivilegeElevationExecutable";

        static CommonServiceData()
        {
            if (!Directory.Exists(ServiceDirectory))
                Directory.CreateDirectory(ServiceDirectory);

            if (BaseName + Version != FullName)
                throw new ArgumentException("FullName and Version are out of sync");
        }

    }

}
