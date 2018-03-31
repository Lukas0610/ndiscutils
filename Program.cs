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
using CommandLine;
using nDiscUtils.Modules;

namespace nDiscUtils
{

    public static class Program
    {

        public static int Main(string[] args)
        {                        
            return Parser.Default.ParseArguments<
                ListPartitions.Options,
                MountPartition.Options,
                MountImage.Options,
                Ramdisk.Options,
                MakeImage.Options,
                MakeDirectoryImage.Options>(args)
                .MapResult(
                    (ListPartitions.Options opts) => ListPartitions.Run(opts),
                    (MountPartition.Options opts) => MountPartition.Run(opts),
                    (MountImage.Options opts) => MountImage.Run(opts),
                    (Ramdisk.Options opts) => Ramdisk.Run(opts),
                    (MakeImage.Options opts) => MakeImage.Run(opts),
                    (MakeDirectoryImage.Options opts) => MakeDirectoryImage.Run(opts),
                    (errcode) => 1
                );
        }


    }

}
