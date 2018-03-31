/*
 * nClone - Platform independent drive cloning tool
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

namespace nDiscUtils.Modules.Events
{

    public sealed class CloneProgressEventArgs : EventArgs
    {

        private int mPartition;
        private long mTaskId;
        private long mTotal;
        private long mCurrent;

        public int Partition
            => mPartition;

        public long TaskID
            => mTaskId;

        public long Total
            => mTotal;

        public long Current
            => mCurrent;

        internal CloneProgressEventArgs(int partition, long taskId, long total, long current)
        {
            mPartition = partition;
            mTaskId = taskId;
            mTotal = total;
            mCurrent = current;
        }

    }

}
