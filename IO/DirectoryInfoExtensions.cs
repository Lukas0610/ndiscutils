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

namespace nDiscUtils.IO
{

    public static class DirectoryInfoExtensions
    {

        public static void CreateRecursive(this DirectoryInfo directoryInfo)
        {
            var entries = directoryInfo.FullName
                .Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries)
                .RemoveFirst();

            var currentPath = directoryInfo.Root.FullName;

            foreach (var entry in entries)
            {
                currentPath = Path.Combine(currentPath, entry);
                if (!Directory.Exists(currentPath))
                    Directory.CreateDirectory(currentPath);
            }
        }

    }

}
