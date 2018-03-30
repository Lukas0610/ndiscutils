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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DiscUtils.Fat
{

    internal sealed class LongFileName
    {

        public static LongFileName Empty = new LongFileName(true);

        private bool _readOnly;
        private List<LongFileNameEntry> _entries;

        internal LongFileName()
            : this(false)
        { }

        private LongFileName(bool readOnly)
        {
                _readOnly = readOnly;
            _entries = new List<LongFileNameEntry>();
        }

        internal static LongFileName FromPath(string path)
        {
            byte[] pathData = Encoding.Unicode.GetBytes(path);
            LongFileName lfn = new LongFileName();

            for (int offset = 0; offset < pathData.Length; offset += 26)
            {
                byte[] segmentData = new byte[32];

                segmentData[0x00] = (byte)(offset == 0 ? 0x42 : 0x01); /* Reserved */
                // 0x01+10 - LFS Name
                segmentData[0x0B] = (byte)0x0F; /* Attributes (ReadOnly, Hidden, System and VolumeId) */
                segmentData[0x0C] = (byte)0x00; /* Reserved */
                segmentData[0x0D] = (byte)0x00; /* Checksum */
                // 0x0E+12 - LFS Name
                segmentData[0x1B] = (byte)0x00; /* Reserved */
                // 0x1C+4 - LFS Name

                Array.Copy(pathData, offset + 0, segmentData, 0x01, 10);
                Array.Copy(pathData, offset + 10, segmentData, 0x0E, 12);
                Array.Copy(pathData, offset + 22, segmentData, 0x1C, 4);

                lfn.AddEntry(segmentData, 0);
            }

            return lfn;
        }

        internal void AddEntry(DirectoryEntry directoryEntry)
        {
            AddEntry(directoryEntry.Buffer, 0);
        }

        internal void AddEntry(byte[] data, int offset)
        {
            if (_readOnly) return;
            _entries.Add(new LongFileNameEntry(data, offset, (_entries.Count == 0)));
        }

        internal void WriteTo(Stream stream)
        {
            foreach (LongFileNameEntry entry in _entries)
            {
                entry.WriteTo(stream);
            }
        }

        public void Clear()
        {
            if (_readOnly) return;
            _entries.Clear();
        }

        public void CopyTo(LongFileName target)
        {
            if (_readOnly || target._readOnly) return;
            foreach (LongFileNameEntry entry in _entries)
            {
                byte[] copyData = new byte[entry.Data.Length];
                Array.Copy(entry.Data, 0, copyData, 0, copyData.Length);

                target._entries.Add(new LongFileNameEntry(copyData, entry.Offset, entry.IsFirstEntry));
            }
        }

        public override string ToString()
        {
            StringBuilder lfnBuilder = new StringBuilder();

            // go through LFN entry list in reversed order
            for(int i = _entries.Count - 1; i >= 0; i--)
                lfnBuilder.Append(_entries[i].LFNString);

            return lfnBuilder.ToString();
        }

    }

}
