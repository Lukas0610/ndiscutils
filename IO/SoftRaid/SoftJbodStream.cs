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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace nDiscUtils.IO.SoftRaid
{

    public sealed class SoftJbodStream : AbstractSoftRaidStream
    {

        private object mLock;

        private long mPosition;

        private Dictionary<int, long> mStreamLengths;

        public SoftJbodStream()
            : base()
        {
            mStreamLengths = new Dictionary<int, long>();
            mLock = new object();
        }

        protected override void OnOpened()
        {
            for (var i = 0; i < SubStreams.Length; i++)
            {
                mStreamLengths.Add(i, SubStreams[i].Length);
            }
        }

        public override long Position
        {
            get => mPosition;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush()
        {
            lock (mLock)
            {
                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    SubStreams[i].Flush();
                });
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readCount = 0;

            while (readCount < count && mPosition < Length)
            {
                var bufferOffset = offset + readCount;
                var readSize = count - readCount;

                var stream = GetPositionStream(mPosition);
                var streamEnd = GetPositionStreamEnd(mPosition);

                if (streamEnd < mPosition + readCount)
                    streamEnd = (mPosition + readCount) - streamEnd;

                SeekStreamRelative();
                var read = stream.Read(buffer, bufferOffset, readSize);

                mPosition += read;
                readCount += read;
            }

            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (mLock)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin: mPosition = offset; break;
                    case SeekOrigin.Current: mPosition += offset; break;
                    case SeekOrigin.End: mPosition = Length - offset; break;
                }

                return mPosition;
            }
        }

        public override void SetLength(long value)
        {
            base.SetLength(value);
            // Invalid Operation
        }

        public override long GetEffectiveLength(long value)
        {
            return value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var writeCount = 0;

            while (writeCount < count && mPosition < Length)
            {
                var bufferOffset = offset + writeCount;
                var writeSize = count - writeCount;

                var stream = GetPositionStream(mPosition);
                var streamEnd = GetPositionStreamEnd(mPosition);

                if (streamEnd < mPosition + writeCount)
                    streamEnd = (mPosition + writeCount) - streamEnd;

                SeekStreamRelative();
                stream.Write(buffer, bufferOffset, writeSize);

                mPosition += writeSize;
                writeCount += writeSize;
            }
        }

        private Stream GetPositionStream(long position)
        {
            var end = 0L;
            for (int i = 0; i < SubStreams.Length; i++)
            {
                if (position < end + SubStreams[i].Length)
                    return SubStreams[i];

                end += SubStreams[i].Length;
            }
            return null;
        }

        private long GetPositionStreamEnd(long position)
        {
            var end = 0L;
            for (int i = 0; i < SubStreams.Length; i++)
            {
                if (position < end + SubStreams[i].Length)
                    return end + SubStreams[i].Length;

                end += SubStreams[i].Length;
            }
            return end;
        }

        private void SeekStreamRelative()
        {
            var end = 0L;
            for (int i = 0; i < SubStreams.Length; i++)
            {
                if (mPosition < end + SubStreams[i].Length)
                {
                    SubStreams[i].Position = mPosition - end;
                    return;
                }

                end += SubStreams[i].Length;
            }
        }

    }

}
