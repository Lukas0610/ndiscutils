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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace nDiscUtils.IO.SoftRaid
{

    public sealed class SoftRaid1Stream : AbstractSoftRaidStream
    {

        private object mLock;
        private long mLength;

        public SoftRaid1Stream()
            : base()
        {
            mLock = new object();
        }

        public override long Length
        {
            get => mLength;
        }

        public override long Position
        {
            get
            {
                lock (mLock)
                {
                    AssertPositions();
                    return SubStreams[0].Length;
                }
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override void Flush()
        {
            lock (mLock)
            {
                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    SubStreams[i].Flush();
                });
                AssertPositions();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (mLock)
            {
                // TODO: implement validating reading across all streams
                var result = SubStreams[0].Read(buffer, offset, count);

                // after the read, the position got unaligned. ensure it is
                // aligned on all stream
                SeekSecondaryStreams(SubStreams[0].Position, SeekOrigin.Begin);

                AssertPositions();
                return result;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (mLock)
            {
                var results = new long[SubStreams.Length];

                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    results[i] = SubStreams[i].Seek(offset, origin);
                });

                if (results.Any(t => t != results[0]))
                    throw new IOException("Soft-RAID streams out of sync: Different positions after seeking");

                return results[0];
            }
        }

        public override void SetLength(long value)
        {
            lock (mLock)
            {
                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    SubStreams[i].SetLength(value);
                });
                InvalidateLength();
            }
        }

        public override void InvalidateLength()
        {
            lock (mLock)
            {
                var results = new long[SubStreams.Length];

                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    results[i] = SubStreams[i].Length;
                });

                mLength = results.Min();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (mLock)
            {
                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    SubStreams[i].Write(buffer, offset, count);
                });
                InvalidateLength();
                AssertPositions();
            }
        }

        private void AssertPositions()
        {
            var results = new long[SubStreams.Length];

            Parallel.For(0, SubStreams.Length, (i) =>
            {
                results[i] = SubStreams[i].Position;
            });

            if (results.Any(t => t != results[0]))
                throw new IOException("Soft-RAID streams out of sync: Different positions");
        }

        private void SeekSecondaryStreams(long offset, SeekOrigin origin)
        {
            var results = new long[SubStreams.Length - 1];

            Parallel.For(1, SubStreams.Length, (i) =>
            {
                results[i - 1] = SubStreams[i].Seek(offset, origin);
            });

            if (results.Any(t => t != offset))
                throw new IOException("Soft-RAID streams out of sync: Different positions after seeking");
        }

    }

}
