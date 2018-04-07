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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace nDiscUtils.BuildTools
{

    public static class Program
    {

        private const int IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20;

        public static void Main(string[] args)
        {
            if (args[0] == "update-pe-header")
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
            else if (args[0] == "mkdir")
            {
                Directory.CreateDirectory(args[1]);
            }
            else if (args[0] == "copy")
            {
                File.Copy(args[1], args[2], true);
            }
            else if (args[0] == "sha512sum")
            {
                using (var sha = SHA512.Create())
                using (var input = new FileStream(args[1], FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var hash = sha.ComputeHash(input);
                    var hashString = new StringBuilder();

                    foreach (byte b in hash)
                        hashString.AppendFormat("{0:x2}", b);

                    hashString.AppendFormat(" *{0}\r\n", args[1]);
                    File.WriteAllText($"{args[1]}.sha512sum", hashString.ToString());
                }
            }
        }

    }

}
