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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nDiscUtils.BuildTools
{

    public static class Program
    {

        private const int IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20;

        public static void Main(string[] args)
        {
            if (args[0] == "largeaddressaware")
            {
                var path = args[1];
                if (!File.Exists(path))
                    return;

                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var peHeader = new PEHeader(fileStream);
                    if (!peHeader.ReadFileHeader())
                        return;

                    peHeader.Characteristics |= PECharacteristics.IMAGE_FILE_LARGE_ADDRESS_AWARE;
                    peHeader.Characteristics |= PECharacteristics.IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP;
                    peHeader.Characteristics |= PECharacteristics.IMAGE_FILE_NET_RUN_FROM_SWAP;

                    peHeader.WriteFileHeader();
                }
            }
        }

    }

}
