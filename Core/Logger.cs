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
using System.Diagnostics;
using System.IO;
using System.Linq;

using static nDiscUtils.Core.nConsole;

namespace nDiscUtils.Core
{

    public sealed class Logger
    {

        private static string mLoggingFile = null;
        private static FileStream mLoggingStream = null;
        private static StreamWriter mLoggingWriter = null;

        private static bool mVerbose = false;
        private static bool mDebug = false;
        private static int mAdvancedLoggingOffset = 0;

        private static LinkedList<ConsoleLogEntry> mConsoleLogList
            = new LinkedList<ConsoleLogEntry>();

        internal static void SetVerbose(bool flag)
            => mVerbose = flag;

        public static bool IsVerbose => mVerbose;

        internal static void SetDebug(bool flag)
            => mDebug = flag;

        public static bool IsDebug => mDebug;

        internal static void SetAdvancedLoggingOffset(int offset)
            => mAdvancedLoggingOffset = offset;

        internal static void OpenFile(string loggingFile)
        {
            mLoggingFile = loggingFile;

            mLoggingStream = new FileStream(loggingFile, FileMode.Create,
                FileAccess.Write, FileShare.Read)
            {
                Position = 0
            };
            mLoggingStream.SetLength(0);

            mLoggingWriter = new StreamWriter(mLoggingStream)
            {
                AutoFlush = true
            };
        }

        private static void Log(LogType logType, string format, 
            params object[] args)
        {
            var caller = new StackTrace(true).GetFrame(2);

            var consoleText = string.Format("{0} [{1}]: {2}",
                DateTime.Now.ToString(),
                GetPrefix(logType),
                string.Format(format, args));

            var fileText = string.Format("{0} [{1}]: {2}:{3}: {4}",
                DateTime.Now.ToString(),
                GetPrefix(logType),
                Path.GetFileName(caller.GetFileName()),
                caller.GetFileLineNumber(),
                string.Format(format, args));
            
            // console
            if (UseAdvancedConsoleLogging)
            {
                var consoleColor = GetConsoleColor(logType);
                var maxLogHeight = ContentHeight - mAdvancedLoggingOffset;
                var loggingContentTop = ContentTop + mAdvancedLoggingOffset;
                if (mConsoleLogList.Count == maxLogHeight)
                    mConsoleLogList.RemoveLast();

                // console
                mConsoleLogList.AddFirst(new ConsoleLogEntry()
                { Color = consoleColor, Content = consoleText });

                for (int i = 0; i < maxLogHeight && i < mConsoleLogList.Count; i++)
                {
                    var logEntry = mConsoleLogList.ElementAt(i);
                    var text = logEntry.Content;

                    if (text.Length > ContentWidth)
                        text = text.Substring(0, ContentWidth - 3) + "...";

                    var padding = "";
                    if (ContentWidth - text.Length > 0)
                        padding = new string(' ', ContentWidth - text.Length);

                    ForegroundColor = logEntry.Color;
                    WriteFormat(ContentLeft, loggingContentTop + i,
                        "{0}{1}", text, padding);
                }
            }
            else
            {
                Console.WriteLine(consoleText);
            }

            // file
            if (mLoggingWriter != null)
                mLoggingWriter.WriteLine(fileText);
        }

        public static void Verbose(string format, params object[] args)
        {
            if (mVerbose)
                Log(LogType.Verbose, format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            if (mDebug)
                Log(LogType.Debug, format, args);
        }

        public static void Fine(string format, params object[] args)
            => Log(LogType.Fine, format, args);

        public static void Info(string format, params object[] args)
            => Log(LogType.Info, format, args);

        public static void Warn(string format, params object[] args)
            => Log(LogType.Warn, format, args);

        public static void Error(string format, params object[] args)
            => Log(LogType.Error, format, args);

        public static void Exception(string format, params object[] args)
            => Log(LogType.Exception, format, args);

        public static void Exception(Exception ex)
            => Log(LogType.Exception, "Unhandled {0} at {1}", ex.ToString(), ex.Source);

        private static ConsoleColor GetConsoleColor(LogType type)
        {
            switch (type)
            {
                case LogType.Verbose:
                    return ConsoleColor.DarkGray;
                case LogType.Debug:
                    return ConsoleColor.DarkBlue;
                case LogType.Fine:
                    return ConsoleColor.DarkGreen;
                case LogType.Info:
                    return ConsoleColor.Black;
                case LogType.Warn:
                    return ConsoleColor.DarkYellow;
                case LogType.Error:
                case LogType.Exception:
                    return ConsoleColor.DarkRed;
            }
            return ConsoleColor.Black;
        }

        private static string GetPrefix(LogType type)
        {
            switch (type)
            {
                case LogType.Verbose:
                    return "VERB";
                case LogType.Debug:
                    return "DEBG";
                case LogType.Fine:
                    return "FINE";
                case LogType.Info:
                    return "INFO";
                case LogType.Warn:
                    return "WARN";
                case LogType.Error:
                case LogType.Exception:
                    return "FAIL";
            }
            return "UNKN";
        }

        private class ConsoleLogEntry
        {

            public ConsoleColor Color { get; set; }

            public string Content { get; set; }

        }

    }

}
