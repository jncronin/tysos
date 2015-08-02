/* Copyright (C) 2011 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.lib
{
    public class ConsoleDriver
    {
        /* The following signals are taken from the linux implementation in termios.h */
        internal const int VINTR = 0;
        internal const int VQUIT = 1;
        internal const int VERASE = 2;
        internal const int VKILL = 3;
        internal const int VEOF = 4;
        internal const int VTIME = 5;
        internal const int VMIN = 6;
        internal const int VSWTC = 7;
        internal const int VSTART = 8;
        internal const int VSTOP = 9;
        internal const int VSUSP = 10;
        internal const int VEOL = 11;
        internal const int VREPRINT = 12;
        internal const int VDISCARD = 13;
        internal const int VWERASE = 14;
        internal const int VLNEXT = 15;

        static byte[] c_cc = new byte[] { 0x03, 0x1c, 0x7f, 0x15, 0x04, 0x00, 0x01, 0x00, 0x11, 0x13, 0x1a, 0x00, 0x12, 0x0f, 0x17, 0x16 };

        public interface IConsole
        {
            int GetWidth();
            int GetHeight();
        }

        [libsupcs.MethodAlias("_ZW6System13ConsoleDriverM_0_8TtySetup_Rb_P4u1SRhRhRh")]
        [libsupcs.AlwaysCompile]
        static bool TtySetup(string teardown, out byte verase, out byte vsusp, out byte intr)
        {
            verase = c_cc[VERASE];
            vsusp = c_cc[VSUSP];
            intr = c_cc[VINTR];
            return true;
        }

        [libsupcs.MethodAlias("_ZW6System13ConsoleDriverM_0_7SetEcho_Rb_P1b")]
        [libsupcs.AlwaysCompile]
        static bool SetEcho(bool want_echo)
        {
            return true;
        }

        [libsupcs.MethodAlias("_ZW6System13ConsoleDriverM_0_10GetTtySize_Rb_P3u1IRiRi")]
        [libsupcs.AlwaysCompile]
        static bool GetTtySize(IntPtr handle, out int width, out int height)
        {
            long h = (long)handle;

            if (h == MonoIO.STDOUT)
            {
                if (Program.arch.CurrentCpu.CurrentThread.owning_process.stdout is IConsole)
                {
                    width = ((IConsole)Program.arch.CurrentCpu.CurrentThread.owning_process.stdout).GetWidth();
                    height = ((IConsole)Program.arch.CurrentCpu.CurrentThread.owning_process.stdout).GetHeight();
                    return true;
                }
            }
            else if (h == MonoIO.STDERR)
            {
                if (Program.arch.CurrentCpu.CurrentThread.owning_process.stderr is IConsole)
                {
                    width = ((IConsole)Program.arch.CurrentCpu.CurrentThread.owning_process.stderr).GetWidth();
                    height = ((IConsole)Program.arch.CurrentCpu.CurrentThread.owning_process.stderr).GetHeight();
                    return true;
                }
            }

            width = 0;
            height = 0;
            return false;
        }

        [libsupcs.MethodAlias("_ZW6System13ConsoleDriverM_0_20InternalKeyAvailable_Ri_P1i")]
        [libsupcs.AlwaysCompile]
        static int InternalKeyAvailable(int ms_timeout)
        {
            if (Program.arch.CurrentCpu.CurrentThread.owning_process.stdin != null)
                return (Program.arch.CurrentCpu.CurrentThread.owning_process.stdin.DataAvailable(ms_timeout * 10000)) ? 1 : 0;
            else
                return 0;
        }
    }
}
