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
using System.Linq;
using System.Threading;

namespace nDiscUtils.IO.FileSystem
{

    public sealed class MetaDataCache
    {

        private long mLifetime;
        private bool mThreadSafe;
        private Dictionary<string, Tuple<DateTime, dynamic>> mCache;

        private Mutex @lock;

        public char PathSeperator { get; set; }

        public MetaDataCache()
            : this(long.MaxValue, false) { }

        public MetaDataCache(long lifetime)
            : this(lifetime, false) { }

        public MetaDataCache(long lifetime, bool threadSafe)
        {
            mLifetime = lifetime;
            mThreadSafe = threadSafe;
            mCache = new Dictionary<string, Tuple<DateTime, dynamic>>();

            if (threadSafe)
                @lock = new Mutex();
            else
                @lock = null;
        }

        public void Clear(string prefix, string path)
        {
            @lock?.WaitOne();

            try
            {
                var key = BuildKey(prefix, path);
                if (mCache.ContainsKey(key))
                    mCache.Remove(key);
            }
            finally
            {
                @lock?.ReleaseMutex();
            }
        }

        public void ClearAll(string path)
        {
            @lock?.WaitOne();

            try
            {
                var key = BuildKey("", path);
                var cachedKeys = mCache.Keys.ToArray();
                foreach (var cachedKey in cachedKeys)
                {
                    if (cachedKey.EndsWith(key))
                        mCache.Remove(cachedKey);
                }
            }
            finally
            {
                @lock?.ReleaseMutex();
            }
        }

        public void ClearAllChildren(string path)
        {
            @lock?.WaitOne();

            try
            {
                var key = BuildKey("", path);
                var cachedKeys = mCache.Keys.ToArray();
                foreach (var cachedKey in cachedKeys)
                {
                    if (cachedKey.Contains(key))
                        mCache.Remove(cachedKey);
                }
            }
            finally
            {
                @lock?.ReleaseMutex();
            }
        }

        public void Add(string prefix, string path, object data)
        {
            @lock?.WaitOne();

            try
            {
                var key = BuildKey(prefix, path);
                if (mCache.ContainsKey(key))
                    mCache.Remove(key);

                mCache.Add(key, new Tuple<DateTime, dynamic>(DateTime.Now, data));
            }
            finally
            {
                @lock?.ReleaseMutex();
            }
        }

        public bool IsValid(string prefix, string path)
        {
            @lock?.WaitOne();

            try
            {
                var key = BuildKey(prefix, path);
                return IsValid(key);
            }
            finally
            {
                @lock?.ReleaseMutex();
            }
        }

        private bool IsValid(string key)
        {
            if (!mCache.ContainsKey(key))
                return false;

            var cacheData = mCache[key];
            if (DateTime.Now.Subtract(cacheData.Item1).TotalMilliseconds >= mLifetime)
                return false;

            return true;
        }

        public object Get(string prefix, string path)
        {
            @lock?.WaitOne();

            try
            {
                var key = BuildKey(prefix, path);
                if (!mCache.ContainsKey(key))
                    throw new ArgumentException("No cache entry with such a key");

                var cacheData = mCache[key];
                if (DateTime.Now.Subtract(cacheData.Item1).TotalMilliseconds >= mLifetime)
                    throw new InvalidOperationException("Cache entry has exceeded lifetime");

                return cacheData.Item2;
            }
            finally
            {
                @lock?.ReleaseMutex();
            }
        }

        public object TryGet(string prefix, string path)
        {
            @lock?.WaitOne();

            try
            {
                var key = BuildKey(prefix, path);

                if (!IsValid(key))
                    return null;

                return (object)(mCache[key].Item2);
            }
            finally
            {
                @lock?.ReleaseMutex();
            }
        }

        private string BuildKey(string prefix, string path)
        {
            return $"{prefix}??{path}";
        }

    }

}
