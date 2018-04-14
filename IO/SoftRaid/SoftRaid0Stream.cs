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
using System.Threading.Tasks;

namespace nDiscUtils.IO.SoftRaid
{

    public sealed class SoftRaid0Stream : AbstractSoftRaidStream
    {

        private object mLock;

        private long mLength;
        private long mPosition;

        public SoftRaid0Stream()
            : base()
        {
            mLock = new object();
            mLength = 0;
        }

        public override long Length
        {
            get => mLength;
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

            while (readCount < count)
            {
                var bufferOffset = offset + readCount;
                var readSize = Math.Min(count - readCount, StripeSize);

                var currentIndex = GetStripeIndex(mPosition);
                var currentBegin = GetStripeOffset(currentIndex);
                var currentEnd = GetStripeOffset(currentIndex + 1);

                var beginOffset = (mPosition - currentBegin);

                if (mPosition + readSize > currentEnd)
                    readSize = (int)((mPosition + readSize) - currentEnd);

                var stream = GetStripeStream(currentIndex);
                var streamOffset = GetStreamOffset(currentIndex);

                stream.Seek(streamOffset + beginOffset, SeekOrigin.Begin);
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
            lock (mLock)
            {
                if (value % SubStreams.Length != 0)
                    throw new ArgumentException("Cannot set length on substreams: Requested length is not divisible by count of disks");

                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    SubStreams[i].SetLength(value / SubStreams.Length);
                });
                InvalidateLength();
            }
        }

        public override void InvalidateLength()
        {
            lock (mLock)
            {
                mLength = 0;
                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    mLength += SubStreams[i].Length;
                });
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var writeCount = 0;

            while (writeCount < count)
            {
                var bufferOffset = offset + writeCount;
                var writeSize = Math.Min(count - writeCount, StripeSize);

                var currentIndex = GetStripeIndex(mPosition);
                var currentBegin = GetStripeOffset(currentIndex);
                var currentEnd = GetStripeOffset(currentIndex + 1);

                var beginOffset = (mPosition - currentBegin);

                if (mPosition + writeSize > currentEnd)
                    writeSize = (int)((mPosition + writeSize) - currentEnd);

                var stream = GetStripeStream(currentIndex);
                var streamOffset = GetStreamOffset(currentIndex);

                stream.Seek(streamOffset + beginOffset, SeekOrigin.Begin);
                stream.Write(buffer, bufferOffset, writeSize);

                mPosition += writeSize;
                writeCount += writeSize;
            }

            InvalidateLength();
        }

        private long GetStripeIndex(long offset)
        {
            return (long)Math.Floor((double)offset / StripeSize);
        }

        private long GetStripeOffset(long index)
        {
            return index * StripeSize;
        }

        public long GetStreamOffset(long index)
        {
            return (long)Math.Floor((double)index / SubStreams.Length) * StripeSize;
        }

        private Stream GetStripeStream(long index)
        {
            return SubStreams[index % SubStreams.Length];
        }

    }

}
