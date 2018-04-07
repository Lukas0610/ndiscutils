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
using System.Diagnostics;
using System.Text;

using Mischel.ConsoleDotNet;

using static nDiscUtils.Core.ModuleHelpers;

namespace nDiscUtils.Core
{

    public static class nConsole
    {

        private static ConsoleScreenBuffer
            kPrimaryBuffer = null,
            kPrivateBuffer = null;

        private static ConsoleScreenBuffer
            kCurrentBuffer = null;

        public static bool UseAdvancedConsoleLogging = false;

        public static int Width = 0;
        public static int Height = 0;
        public static int Top = 0;
        public static int Left = 0;

        public static int ContentWidth = 0;
        public static int ContentHeight = 0;
        public static int ContentTop = 0;
        public static int ContentLeft = 0;

        private static object kWriteLock = new object();

        public static ConsoleColor ForegroundColor
        {
            get
            {
                return kCurrentBuffer.ForegroundColor;
            }
            set
            {
                kCurrentBuffer.ForegroundColor = value;
            }
        }

        public static ConsoleColor BackgroundColor
        {
            get
            {
                return kCurrentBuffer.BackgroundColor;
            }
            set
            {
                kCurrentBuffer.BackgroundColor = value;
            }
        }

        public static int CursorTop
        {
            get
            {
                return kCurrentBuffer.CursorTop;
            }
            set
            {
                kCurrentBuffer.CursorTop = value;
            }
        }

        public static int CursorLeft
        {
            get
            {
                return kCurrentBuffer.CursorLeft;
            }
            set
            {
                kCurrentBuffer.CursorLeft = value;
            }
        }

        public static bool CursorVisible
        {
            get
            {
                return kCurrentBuffer.CursorVisible;
            }
            set
            {
                kCurrentBuffer.CursorVisible = value;
            }
        }

        public static int BufferWidth
        {
            get
            {
                return kCurrentBuffer.Width;
            }
            set
            {
                kCurrentBuffer.Width = value;
            }
        }

        public static int BufferHeight
        {
            get
            {
                return kCurrentBuffer.Height;
            }
            set
            {
                kCurrentBuffer.Height = value;
            }
        }

        public static int WindowWidth
        {
            get
            {
                return kCurrentBuffer.WindowWidth;
            }
            set
            {
                kCurrentBuffer.WindowWidth = value;
            }
        }

        public static int WindowHeight
        {
            get
            {
                return kCurrentBuffer.WindowHeight;
            }
            set
            {
                kCurrentBuffer.WindowHeight = value;
            }
        }

        public static IntPtr PrimaryBufferHandle
        {
            get => kPrimaryBuffer.Handle;
        }

        public static bool PrivateConsoleBufferInUse
        {
            get => (kPrivateBuffer != null);
        }

        private static void AttachCurrentConsoleBuffer()
        {
            if (Program.PPID > 0)
            {
                NativeMethods.FreeConsole();
                NativeMethods.SetParent(kCurrentBuffer.Handle, new IntPtr(Program.CHWND));
                NativeMethods.AttachConsole((uint)Program.PPID);
            }
        }

        public static void OpenNewConsoleBuffer()
        {
            kPrivateBuffer = new ConsoleScreenBuffer();

            kCurrentBuffer = kPrivateBuffer;
            AttachCurrentConsoleBuffer();

            JConsole.SetActiveScreenBuffer(kCurrentBuffer);
        }

        public static void ExchangeBuffers()
        {
            if (kCurrentBuffer == kPrimaryBuffer)
            {
                kCurrentBuffer = kPrivateBuffer;
            }
            else if (kCurrentBuffer == kPrivateBuffer)
            {
                kCurrentBuffer = kPrimaryBuffer;
            }

            AttachCurrentConsoleBuffer();
            JConsole.SetActiveScreenBuffer(kCurrentBuffer);
        }

        public static void InitializeSystemConsole()
        {
            kPrimaryBuffer = JConsole.GetActiveScreenBuffer();
            kCurrentBuffer = kPrimaryBuffer;
            WindowWidth = Math.Max(WindowWidth, 120);
            WindowHeight = Math.Max(WindowHeight, 30);
            BufferHeight = Math.Max(BufferHeight, 3000);
        }

        public static void InitializeSystemConsole(IntPtr hwnd)
        {
            kPrimaryBuffer = new ConsoleScreenBuffer(hwnd);
            kCurrentBuffer = kPrimaryBuffer;
            WindowWidth = Math.Max(WindowWidth, 120);
            WindowHeight = Math.Max(WindowHeight, 30);
            BufferHeight = Math.Max(BufferHeight, 3000);
        }

        public static void InitializeConsole()
        {
            // reset everything
            ForegroundColor = ConsoleColor.White;
            BackgroundColor = ConsoleColor.DarkBlue;

            // clear everything
            Clear();

            // update some things
            CursorVisible = false;
            UseAdvancedConsoleLogging = true;

            // set minimal window size
            WindowWidth = Math.Max(WindowWidth, 120);
            WindowHeight = Math.Max(WindowHeight, 30);

            // adjust buffers
            BufferWidth = WindowWidth;
            BufferHeight = WindowHeight;

            // set global variables
            Width = Math.Min(BufferWidth, 180);
            Height = Math.Min(BufferHeight, 50) - 1;
            Top = Math.Max((BufferHeight - 50) / 2, 0);
            Left = Math.Max((BufferWidth - 180) / 2, 0);

            // set global content variables
            ContentWidth = Width - 13;
            ContentHeight = Height - 7;
            ContentTop = Top + 4;
            ContentLeft = Left + 7;

            // draw main CLI windows
            DrawWindow();
        }

        public static void WaitForUserExitImpl()
        {
            ForegroundColor = ConsoleColor.Black;
            Write(Left + 6, Top + Height - 2, " Press any key to exit... ");
            ReadKey(true);
        }

        public static void RestoreOldConsoleBuffer()
        {
            kCurrentBuffer = kPrimaryBuffer;

            AttachCurrentConsoleBuffer();
            JConsole.SetActiveScreenBuffer(kPrimaryBuffer);

            kPrivateBuffer.Dispose();
            kPrivateBuffer = null;
        }

        public static void Cleanup()
        {
            if (kPrivateBuffer != null)
                RestoreOldConsoleBuffer();

            kCurrentBuffer.Dispose();
            kCurrentBuffer = null;
        }

        public static void Clear()
        {
            kCurrentBuffer.Clear();
        }

        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            return Console.ReadKey(intercept);
        }

        public static void ResetColor()
        {
            kCurrentBuffer.ForegroundColor = ConsoleColor.Black;
            kCurrentBuffer.BackgroundColor = ConsoleColor.Gray;
        }

        public static void Write(int x, int y, string str)
        {
            lock(kWriteLock)
            {
                kCurrentBuffer.SetCursorPosition(x, y);
                kCurrentBuffer.Write(str);
            }
        }

        public static void Write(int x, int y, char ch)
        {
            Write(x, y, char.ToString(ch));
        }

        public static void Write(int x, int y, char ch, int count)
        {
            Write(x, y, new string(ch, count));
        }

        public static void Write(int x, int y, string str, int count)
        {
            var stringBuilder = new StringBuilder();
            for (int i = 0; i < count; i++)
                stringBuilder.Append(str);

            Write(x, y, stringBuilder.ToString());
        }

        public static void WriteFormat(int x, int y, string format, params object[] args)
        {
            Write(x, y, string.Format(format, args));
        }

        public static void WriteFormatRight(int x, int y, string format, params object[] args)
        {
            var str = string.Format(format, args);
            Write(x - str.Length + 1, y, str);
        }

        public static void WriteVertical(int x, int y, string str, int count)
        {
            for (int i = 0; i < count; i++)
                Write(x, y + i, str);
        }

        public static void Write(string format, params object[] args)
        {
            lock (kWriteLock)
                kCurrentBuffer.Write(string.Format(format, args));
        }

        public static void WriteLine()
        {
            lock (kWriteLock)
                kCurrentBuffer.WriteLine("");
        }

        public static void WriteLine(string format, params object[] args)
        {
            lock (kWriteLock)
                kCurrentBuffer.WriteLine(string.Format(format, args));
        }

        private static void DrawWindow()
        {
            if (UseConsoleBuffers)
            {
                // JConsole Buffer Window
                DrawWindowImpl('É', '»', 'È', '¼', 'Í', 'º');
            }
            else
            {
                // Legacy Console Window
                DrawWindowImpl('╔', '╗', '╚', '╝', '═', '║');
            }
        }

        private static void DrawWindowImpl(char topLeft, char topRight, char bottomLeft,
            char bottomRight, char horizontal, char vertical)
        {
            ResetColor();

            // top
            Write(Left + 4, Top + 2, topLeft);
            Write(Left + Width - 4, Top + 2, topRight);

            // bottom
            Write(Left + 4, Top + Height - 2, bottomLeft);
            Write(Left + Width - 4, Top + Height - 2, bottomRight);

            // horizontal lines
            Write(Left + 5, Top + 2, horizontal, Width - 9);
            Write(Left + 5, Top + Height - 2, horizontal, Width - 9);

            // vertical lines
            WriteVertical(Left + 4, Top + 3, char.ToString(vertical), Height - 5);
            WriteVertical(Left + Width - 4, Top + 3, char.ToString(vertical), Height - 5);

            // content
            WriteVertical(Left + 5, Top + 3, new string(' ', Width - 9), Height - 5);
        }

        public static void UpdateBackgroundIfRequired(int returnCode)
        {
            var color = ForegroundColor;
            var outputMode = ConsoleOutputModeFlags.WrapAtEol;

            if (returnCode == ReturnCodes.SUCCESS)
                color = ConsoleColor.DarkGreen;
            else
                color = ConsoleColor.DarkRed;

            ForegroundColor = color;
            BackgroundColor = color;

            outputMode = kCurrentBuffer.OutputMode;
            kCurrentBuffer.OutputMode = ConsoleOutputModeFlags.Processed;

            for (int y = 0; y < BufferHeight; y++)
            {
                if (y < (ContentTop - 2) || y > (ContentTop + ContentHeight + 1))
                    Write(0, y, ' ', BufferWidth - (!UseConsoleBuffers && y == Height ? 1 : 0));
                else
                {
                    Write(0, y, ' ', ContentLeft - 3);
                    Write(ContentLeft + ContentWidth + 3, y, ' ', BufferWidth - (ContentLeft + ContentWidth) - 3);
                }
            }

            kCurrentBuffer.OutputMode = outputMode;

            ResetColor();
        }

    }

}

