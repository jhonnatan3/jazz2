﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Jazz2
{
    public class ConsoleUtils
    {
        private static bool? isOutputRedirected;
        private static bool? isShared;
        private static bool supportsUnicode;

        public static bool IsOutputRedirected
        {
            get
            {
                if (isOutputRedirected == null) {
                    try {
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                            uint mode;
                            IntPtr hConsole = GetStdHandle(-11 /*STD_OUTPUT_HANDLE*/);
                            isOutputRedirected = GetFileType(hConsole) != 0x02 /*FILE_TYPE_CHAR*/ || !GetConsoleMode(hConsole, out mode);
                        } else {
                            isOutputRedirected = (isatty(1 /*stdout*/) == 0);
                        }
                    } catch {
                        // Nothing to do...
                        isOutputRedirected = false;
                    }
                }

                return isOutputRedirected == true;
            }
        }

        public static unsafe bool IsShared
        {
            get
            {
                if (isShared == null) {
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                        const int dwProcessCountMax = 1;

                        int* lpdwProcessList = stackalloc int[dwProcessCountMax];
                        int n = GetConsoleProcessList(lpdwProcessList, dwProcessCountMax);
                        isShared = (n > 1);
                    } else {
                        // Always shared on non-Windows platforms
                        isShared = true;
                    }
                }

                return (isShared == true);
            }
        }

        public static bool SupportsUnicode
        {
            get
            {
                return supportsUnicode;
            }
        }

        public static void TryEnableUnicode()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && !IsOutputRedirected) {
                var prevEncoding = Console.OutputEncoding;
                int startLeft = Console.CursorLeft;
                Console.OutputEncoding = Encoding.UTF8;
                Console.Write("≠");
                if (Console.CursorLeft == startLeft + 1) {
                    // One character displayed
                } else {
                    // Multiple characters displayed, Unicode not supported
                    Console.OutputEncoding = prevEncoding;
                }

                // Clean up (with safety guard in case of line wrapping)
                int len = Math.Max(Console.CursorLeft - startLeft, 0);
                Console.CursorLeft -= len;
                Console.Write(new string(' ', len));
                Console.CursorLeft -= len;
            } else {
                supportsUnicode = true;
            }
        }

        #region Native Methods
        [DllImport("libc")]
        private static extern int isatty(int desc);

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32")]
        private static extern uint GetFileType(IntPtr hFile);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsole, out uint lpMode);

        [DllImport("kernel32", SetLastError = true)]
        private static extern unsafe int GetConsoleProcessList(int* lpdwProcessList, int dwProcessCount);
        #endregion
    }
}