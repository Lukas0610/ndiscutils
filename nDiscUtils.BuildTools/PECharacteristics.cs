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

namespace nDiscUtils.BuildTools
{

    public static class PECharacteristics
    {

        public const ushort IMAGE_FILE_RELOCS_STRIPPED = 0x0001;

        public const ushort IMAGE_FILE_EXECUTABLE_IMAGE = 0x0002;

        public const ushort IMAGE_FILE_LINE_NUMS_STRIPPED = 0x0004;

        public const ushort IMAGE_FILE_LOCAL_SYMS_STRIPPED = 0x0008;

        public const ushort IMAGE_FILE_AGGRESSIVE_WS_TRIM = 0x0010;

        public const ushort IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x0020;

        // public const ushort ... = 0x0040;

        public const ushort IMAGE_FILE_BYTES_REVERSED_LO = 0x0080;

        public const ushort IMAGE_FILE_32BIT_MACHINE = 0x0100;

        public const ushort IMAGE_FILE_DEBUG_STRIPPED = 0x0200;

        public const ushort IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP = 0x0400;

        public const ushort IMAGE_FILE_NET_RUN_FROM_SWAP = 0x0800;

        public const ushort IMAGE_FILE_SYSTEM = 0x1000;

        public const ushort IMAGE_FILE_DLL = 0x2000;

        public const ushort IMAGE_FILE_UP_SYSTEM_ONLY = 0x4000;

        public const ushort IMAGE_FILE_BYTES_REVERSED_HI = 0x8000;

    }

}
