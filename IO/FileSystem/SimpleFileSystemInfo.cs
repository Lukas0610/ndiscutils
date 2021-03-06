﻿/*
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

namespace nDiscUtils.IO.FileSystem
{

    public abstract class SimpleFileSystemInfo
    {

        public string Name { get; set; }

        public string Directory { get; set; }

        public string FullName { get; set; }

        public FileAttributes Attributes { get; set; }

        public DateTime? CreationTime { get; set; }

        public DateTime? LastAccessTime { get; set; }

        public DateTime? LastWriteTime { get; set; }

    }

}
