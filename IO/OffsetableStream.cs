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
using System.Threading;
using System.Threading.Tasks;

namespace nDiscUtils.IO
{

    public sealed class OffsetableStream : Stream
    {


        private Stream mStream;
        private long mOffset;

        public OffsetableStream(Stream stream, long offset)
        {
            mStream = stream;
            mOffset = offset;
        }

        public static bool IsOffsetableStream(Stream stream)
        {
            return stream is OffsetableStream;
        }

        public long Offset
        {
            get => mOffset;
            set => mOffset = 0;
        }

        public override bool CanRead
        {
            get => mStream.CanRead;
        }

        public override bool CanSeek
        {
            get => mStream.CanSeek;
        }

        public override bool CanTimeout
        {
            get => mStream.CanTimeout;
        }

        public override bool CanWrite
        {
            get => mStream.CanWrite;
        }

        public override long Length
        {
            get => mStream.Length;
        }

        public override long Position
        {
            get => mStream.Position - mOffset;
            set => mStream.Position = mOffset + value;
        }

        public override int ReadTimeout
        {
            get => mStream.ReadTimeout;
            set => mStream.ReadTimeout = value;
        }

        public override int WriteTimeout
        {
            get => mStream.WriteTimeout;
            set => mStream.WriteTimeout = value;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => mStream.BeginRead(buffer, offset, count, callback, state);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => mStream.BeginWrite(buffer, offset, count, callback, state);

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => mStream.CopyToAsync(destination, bufferSize, cancellationToken);

        protected override void Dispose(bool disposing)
            => mStream.Dispose();

        public override int EndRead(IAsyncResult asyncResult)
            => mStream.EndRead(asyncResult);

        public override void EndWrite(IAsyncResult asyncResult)
            => mStream.EndWrite(asyncResult);

        public override void Flush()
            => mStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
            => mStream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count)
            => mStream.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => mStream.ReadAsync(buffer, offset, count, cancellationToken);

        public override int ReadByte()
            => mStream.ReadByte();

        public override long Seek(long offset, SeekOrigin origin)
            => mStream.Seek(mOffset + offset, origin);

        public override void SetLength(long value)
            => mStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
            => mStream.Write(buffer, offset, count);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => mStream.WriteAsync(buffer, offset, count, cancellationToken);

        public override void WriteByte(byte value)
            => mStream.WriteByte(value);

    }

}
