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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace nDiscUtils.IO.SoftRaid
{

    public abstract class AbstractSoftRaidStream : Stream
    {

        private List<Stream> mStreamList;
        private Stream[] mStreams;
        private bool mOpened;

        private bool mCanRead;
        private bool mCanWrite;
        private bool mCanSeek;
        private bool mCanTimeout;

        public int StripeSize { get; set; }

        public Stream[] SubStreams { get => mStreams; }

        public override bool CanRead => mCanRead;

        public override bool CanSeek => mCanSeek;

        public override bool CanTimeout => mCanTimeout;

        public override bool CanWrite => mCanWrite;

        public override int ReadTimeout
        {
            get
            {
                var results = new int[SubStreams.Length];

                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    results[i] = SubStreams[i].ReadTimeout;
                });

                return results.Min();
            }
            set
            {
                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    SubStreams[i].ReadTimeout = value;
                });
            }
        }

        public override int WriteTimeout
        {
            get
            {
                var results = new int[SubStreams.Length];

                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    results[i] = SubStreams[i].WriteTimeout;
                });

                return results.Min();
            }
            set
            {
                Parallel.For(0, SubStreams.Length, (i) =>
                {
                    SubStreams[i].WriteTimeout = value;
                });
            }
        }

        public AbstractSoftRaidStream()
        {
            mStreamList = new List<Stream>();
            mStreams = null;
            mOpened = false;
            mCanRead = false;
            mCanSeek = false;
            mCanTimeout = false;
            mCanWrite = false;
        }

        public void AddStream(Stream stream)
        {
            mStreamList.Add(stream);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            for (int i = 0; i < mStreams.Length; i++)
            {
                if (mStreams[i] != null)
                {
                    mStreams[i].Flush();
                    mStreams[i].Close();
                    mStreams[i].Dispose();
                    mStreams[i] = null;
                }
            }
        }

        public void Open()
        {
            if (mOpened)
                throw new InvalidOperationException("Cannot open stream: Already opened");

            mStreams = mStreamList.ToArray();
            mStreamList.Clear();
            mOpened = true;

            mCanRead = true;
            mCanSeek = true;
            mCanTimeout = false;
            mCanWrite = true;

            for (int i = 0; i < mStreams.Length; i++)
            {
                mCanRead = mCanRead && mStreams[i].CanRead;
                mCanSeek = mCanSeek && mStreams[i].CanSeek;
                mCanTimeout = mCanTimeout || mStreams[i].CanTimeout;
                mCanWrite = mCanWrite && mStreams[i].CanWrite;
            }
        }

        public abstract void InvalidateLength();

    }

}
