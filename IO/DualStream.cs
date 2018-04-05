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

namespace nDiscUtils.IO
{

    public class DualStream : Stream
    {

        private Stream mReadStream;
        private Stream mWriteStream;
        private Stream[] mParallelField;

        public override bool CanRead => true;

        public override bool CanSeek => mReadStream.CanSeek && mWriteStream.CanSeek;

        public override bool CanWrite => true;

        public override bool CanTimeout => mReadStream.CanTimeout || mWriteStream.CanTimeout;

        public override long Length => Math.Min(mReadStream.Length, mWriteStream.Length);

        public override long Position
        {
            get
            {
                if (mReadStream.Position != mWriteStream.Position)
                    throw new IOException("Streams out of sync");

                return mReadStream.Position;
            }
            set
            {
                mReadStream.Position = value;
                mWriteStream.Position = value;
            }
        }

        public DualStream(Stream readStream, Stream writeStream)
        {
            if (!mReadStream.CanRead)
                throw new ArgumentException("Cannot read from readable stream", "readStream");

            if (!writeStream.CanWrite)
                throw new ArgumentException("Cannot write to writeable stream", "writeStream");

            mReadStream = readStream;
            mWriteStream = writeStream;

            mParallelField = new Stream[2]
            {
                mReadStream,
                mWriteStream
            };
        }

        public override void Close()
        {
            mReadStream.Close();
            mWriteStream.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            mReadStream.Dispose();
            mWriteStream.Dispose();
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            Parallel.ForEach(mParallelField, async i => await i.FlushAsync());
        }

        public override int Read(byte[] buffer, int offset, int count)
            => mReadStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
        {
            var readSeeked = mReadStream.Seek(offset, origin);
            var writeSeeked = mWriteStream.Seek(offset, origin);

            if (readSeeked != writeSeeked)
                throw new IOException("Streams out of sync");

            return readSeeked;
        }

        public override void SetLength(long value)
        {
            var readSetLengthHadException = false;

            try
            {
                mReadStream.SetLength(value);
            }
            catch (Exception)
            {
                readSetLengthHadException = true;
            }

            try
            {
                mWriteStream.SetLength(value);
            }
            catch (Exception)
            {
                if (readSetLengthHadException)
                    throw;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
            => mWriteStream.Write(buffer, offset, count);

    }

}
