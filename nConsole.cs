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
using System.Text;

using Mischel.ConsoleDotNet;

using static nDiscUtils.ModuleHelpers;

namespace nDiscUtils
{

    public static class nConsole
    {

        private static ConsoleScreenBuffer
            kPrimaryBuffer = null,
            kPrivateBuffer = null;

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
                if (!UseConsoleBuffers)
                {
                    return Console.ForegroundColor;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        return buffer.ForegroundColor;
                }
            }
            set
            {
                if (!UseConsoleBuffers)
                {
                    Console.ForegroundColor = value;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        buffer.ForegroundColor = value;
                }
            }
        }

        public static ConsoleColor BackgroundColor
        {
            get
            {
                if (!UseConsoleBuffers)
                {
                    return Console.BackgroundColor;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        return buffer.BackgroundColor;
                }
            }
            set
            {
                if (!UseConsoleBuffers)
                {
                    Console.BackgroundColor = value;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        buffer.BackgroundColor = value;
                }
            }
        }

        public static int CursorTop
        {
            get
            {
                if (!UseConsoleBuffers)
                {
                    return Console.CursorTop;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        return buffer.CursorTop;
                }
            }
            set
            {
                if (!UseConsoleBuffers)
                {
                    Console.CursorTop = value;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        buffer.CursorTop = value;
                }
            }
        }

        public static int CursorLeft
        {
            get
            {
                if (!UseConsoleBuffers)
                {
                    return Console.CursorLeft;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        return buffer.CursorLeft;
                }
            }
            set
            {
                if (!UseConsoleBuffers)
                {
                    Console.CursorLeft = value;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        buffer.CursorLeft = value;
                }
            }
        }

        public static bool CursorVisible
        {
            get
            {
                if (!UseConsoleBuffers)
                {
                    return Console.CursorVisible;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        return buffer.CursorVisible;
                }
            }
            set
            {
                if (!UseConsoleBuffers)
                {
                    Console.CursorVisible = value;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        buffer.CursorVisible = value;
                }
            }
        }

        public static int BufferWidth
        {
            get
            {
                if (!UseConsoleBuffers)
                {
                    return Console.BufferWidth;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        return buffer.Width;
                }
            }
            set
            {
                if (!UseConsoleBuffers)
                {
                    Console.BufferWidth = value;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        buffer.Width = value;
                }
            }
        }

        public static int BufferHeight
        {
            get
            {
                if (!UseConsoleBuffers)
                {
                    return Console.BufferHeight;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        return buffer.Height;
                }
            }
            set
            {
                if (!UseConsoleBuffers)
                {
                    Console.BufferHeight = value;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        buffer.Height = value;
                }
            }
        }

        public static int WindowWidth
        {
            get
            {
                if (!UseConsoleBuffers)
                {
                    return Console.WindowWidth;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        return buffer.WindowWidth;
                }
            }
            set
            {
                if (!UseConsoleBuffers)
                {
                    Console.WindowWidth = value;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        buffer.WindowWidth = value;
                }
            }
        }

        public static int WindowHeight
        {
            get
            {
                if (!UseConsoleBuffers)
                {
                    return Console.WindowHeight;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        return buffer.WindowHeight;
                }
            }
            set
            {
                if (!UseConsoleBuffers)
                {
                    Console.WindowHeight = value;
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                        buffer.WindowHeight = value;
                }
            }
        }

        public static void OpenNewConsoleBuffer()
        {
            if (UseConsoleBuffers)
            {
                kPrimaryBuffer = JConsole.GetActiveScreenBuffer();
                kPrivateBuffer = new ConsoleScreenBuffer();

                JConsole.SetActiveScreenBuffer(kPrivateBuffer);
            }
        }

        public static void InitializeSystemConsole()
        {
            Console.WindowWidth = Math.Max(Console.WindowWidth, 120);
            Console.WindowHeight = Math.Max(Console.WindowHeight, 30);
            Console.BufferHeight = Math.Max(Console.BufferHeight, 3000);
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

        public static void WaitForUserExit()
        {
            ForegroundColor = ConsoleColor.Black;
            Write(Left + 6, Top + Height - 2, " Press any key to exit... ");
            ReadKey(true);
        }

        public static void RestoreOldConsoleBuffer()
        {
            if (UseConsoleBuffers)
            {
                JConsole.SetActiveScreenBuffer(kPrimaryBuffer);
                kPrivateBuffer.Dispose();
            }
        }

        public static void Clear()
        {
            if (!UseConsoleBuffers)
            {
                Console.Clear();
            }
            else
            {
                using (var buffer = JConsole.GetActiveScreenBuffer())
                    buffer.Clear();
            }
        }

        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            return Console.ReadKey(intercept);
        }

        public static void ResetColor()
        {
            if (!UseConsoleBuffers)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Gray;
            }
            else
            {
                using (var buffer = JConsole.GetActiveScreenBuffer())
                {
                    buffer.ForegroundColor = ConsoleColor.Black;
                    buffer.BackgroundColor = ConsoleColor.Gray;
                }
            }
        }

        public static void Write(int x, int y, string str)
        {
            lock(kWriteLock)
            {
                if (!UseConsoleBuffers)
                {
                    Console.SetCursorPosition(x, y);
                    Console.Write(str);
                }
                else
                {
                    using (var buffer = JConsole.GetActiveScreenBuffer())
                    {
                        buffer.SetCursorPosition(x, y);
                        buffer.Write(str);
                    }
                }
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
            nConsole.ResetColor();

            // top
            nConsole.Write(Left + 4, Top + 2, topLeft);
            nConsole.Write(Left + Width - 4, Top + 2, topRight);

            // bottom
            nConsole.Write(Left + 4, Top + Height - 2, bottomLeft);
            nConsole.Write(Left + Width - 4, Top + Height - 2, bottomRight);

            // horizontal lines
            nConsole.Write(Left + 5, Top + 2, horizontal, Width - 9);
            nConsole.Write(Left + 5, Top + Height - 2, horizontal, Width - 9);

            // vertical lines
            nConsole.WriteVertical(Left + 4, Top + 3, char.ToString(vertical), Height - 5);
            nConsole.WriteVertical(Left + Width - 4, Top + 3, char.ToString(vertical), Height - 5);

            // content
            nConsole.WriteVertical(Left + 5, Top + 3, new string(' ', Width - 9), Height - 5);
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

            if (UseConsoleBuffers)
            {
                outputMode = kPrivateBuffer.OutputMode;
                kPrivateBuffer.OutputMode = ConsoleOutputModeFlags.Processed;
            }

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

            if (UseConsoleBuffers)
                kPrivateBuffer.OutputMode = outputMode;

            ResetColor();
        }

    }

}

