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

namespace nDiscUtils.BuildTools
{

    public sealed class PEHeader
    {

        private Stream mStream;
        private BinaryReader mReader;
        private BinaryWriter mWriter;

        private bool? mHasPeFileHeader = null;
        private long mPeHeaderPosition = 0;

        private ushort mMachine;
        private ushort mNumberOfSections;
        private uint mTimeDateStamp;
        private uint mPointerToSymbolTable;
        private uint mNumberOfSymbols;
        private ushort mSizeOfOptionalHeader;
        private ushort mCharacteristics;

        public ushort Machine
        {
            get => mMachine;
            set => mMachine = value;
        }

        public ushort NumberOfSections
        {
            get => mNumberOfSections;
            set => mNumberOfSections = value;
        }

        public uint TimeDateStamp
        {
            get => mTimeDateStamp;
            set => mTimeDateStamp = value;
        }

        public uint PointerToSymbolTable
        {
            get => mPointerToSymbolTable;
            set => mPointerToSymbolTable = value;
        }

        public uint NumberOfSymbols
        {
            get => mNumberOfSymbols;
            set => mNumberOfSymbols = value;
        }

        public ushort SizeOfOptionalHeader
        {
            get => mSizeOfOptionalHeader;
            set => mSizeOfOptionalHeader = value;
        }

        public ushort Characteristics
        {
            get => mCharacteristics;
            set => mCharacteristics = value;
        }

        public PEHeader(Stream stream)
        {
            mStream = stream;
            mReader = new BinaryReader(mStream);
            mWriter = new BinaryWriter(mStream);
        }

        public bool ReadFileHeader()
        {
            if (!mHasPeFileHeader.HasValue)
                mHasPeFileHeader = ReadFileHeaderInternal();

            return mHasPeFileHeader.Value;
        }

        private bool ReadFileHeaderInternal()
        {
            mStream.Position = 0;
            if (mReader.ReadInt16() != 0x5A4D)          // No MZ Header
                return false;

            mStream.Position = 0x3C;
            mPeHeaderPosition = mReader.ReadInt32();    // Get the PE header location.

            mStream.Position = mPeHeaderPosition;
            if (mReader.ReadInt32() != 0x4550)          // No PE header
                return false;

            mMachine = mReader.ReadUInt16();
            mNumberOfSections = mReader.ReadUInt16();
            mTimeDateStamp = mReader.ReadUInt32();
            mPointerToSymbolTable = mReader.ReadUInt32();
            mNumberOfSymbols = mReader.ReadUInt32();
            mSizeOfOptionalHeader = mReader.ReadUInt16();
            mCharacteristics = mReader.ReadUInt16();

            return true;
        }

        public void WriteFileHeader()
        {
            if (mHasPeFileHeader.HasValue && mHasPeFileHeader.Value)
            {
                mStream.Position = mPeHeaderPosition + 4 /* PE header magic */;

                mWriter.Write(mMachine);
                mWriter.Write(mNumberOfSections);
                mWriter.Write(mTimeDateStamp);
                mWriter.Write(mPointerToSymbolTable);
                mWriter.Write(mNumberOfSymbols);
                mWriter.Write(mSizeOfOptionalHeader);
                mWriter.Write(mCharacteristics);

                mStream.Flush();
            }
        }

    }

}
