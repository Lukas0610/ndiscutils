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
using CommandLine;

namespace nDiscUtils.Options
{

    public abstract class BaseMountOptions : BaseOptions
    {

        [Value(0, HelpText = "The letter at which the file system will be mounted", Required = true)]
        public char Letter { get; set; }

        [Option('r', "read-only", Default = false, HelpText = "Mount the file system as read-only", Required = false)]
        public bool ReadOnly { get; set; }

        [Option('t', "threads", Default = 1, HelpText = "Count of threads used to power the mount point", Required = false)]
        public int Threads { get; set; }

        [Option('h', "show-hidden", Default = false, HelpText = "Shows all hidden files the file system uses, includes system- and meta-files", Required = false)]
        public bool ShowHiddenFiles { get; set; }

        [Option("full-access", Default = false, HelpText = "Gives full access to the file system, includes system- and meta-files", Required = false, Hidden = true)]
        public bool FullAccess { get; set; }

    }

}
