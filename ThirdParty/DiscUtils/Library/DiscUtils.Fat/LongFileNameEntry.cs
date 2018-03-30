//
// Copyright (c) 2018, Lukas Berger
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text;

namespace DiscUtils.Fat
{

    internal sealed class LongFileNameEntry
    {
        /*
         * Long File Name entry
         * 
         * A LFN entry is built up as a seperate directory entry with either a signature of
         * [0x42] for the first and [0x01] for every following one.
         *
         * Each LFN-entry has the attribute-byte of 0x0F at 0x0C, which consists of the attributes
         * ReadOnly, Hidden, System and VolumeId
         * 
         * A single LFN entry conists of three LFN blocks which all hold the long file name
         * in the correct order and encoded in Unicode, thus taking up 2 bytes per character
         * 
         * Block 1: 0x01+10
         * Block 2: 0x0E+12
         * Block 3: 0x1C+4
         */

        private byte[] _data;
        private int _offset;
        private bool _isFirstEntry;
        private byte[] _lfnData;
        private string _lfnString;

        internal LongFileNameEntry(byte[] data, int offset, bool isFirstEntry)
        {
            _data = data;
            _offset = offset;
            _isFirstEntry = isFirstEntry;
            _lfnData = new byte[26];
            LoadSegments();
        }

        public byte[] Data { get { return _data; } }

        public int Offset { get { return _offset; } }

        public bool IsFirstEntry { get { return _isFirstEntry; } }

        public byte[] LFNData { get { return _lfnData; } }

        public string LFNString { get { return _lfnString; } }

        private void LoadSegments()
        {
            Array.Copy(Data, _offset + 0x01, _lfnData, 0, 10);
            Array.Copy(Data, _offset + 0x0E, _lfnData, 10, 12);
            Array.Copy(Data, _offset + 0x1C, _lfnData, 22, 4);

            _lfnString = Encoding.Unicode.GetString(_lfnData)
                .Trim((char)0x00, (char)0xFF, (char)0xFFFF);
        }

        internal void WriteTo(Stream output)
        {
            output.Write(Data, 0, Data.Length);
        }

    }

}
