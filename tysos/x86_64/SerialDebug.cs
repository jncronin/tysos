/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace tysos.x86_64
{
    class SerialDebug : IDebugOutput
    {
        const ushort port = 0x3f8;

        static bool _is_setup = false;

        [libsupcs.Profile(false)]
        static void _setup()
        {
            if (_is_setup)
                return;

            // from the osdev wiki page
            libsupcs.IoOperations.PortOut(port + 1, 0x00);
            libsupcs.IoOperations.PortOut(port + 3, 0x80);
            libsupcs.IoOperations.PortOut(port + 0, 0x03);
            libsupcs.IoOperations.PortOut(port + 1, 0x00);
            libsupcs.IoOperations.PortOut(port + 3, 0x03);
            libsupcs.IoOperations.PortOut(port + 2, 0xc7);
            libsupcs.IoOperations.PortOut(port + 4, 0x0b);

            _is_setup = true;
        }

        [libsupcs.Profile(false)]
        static byte is_transmit_empty()
        {
            return (byte)(libsupcs.IoOperations.PortInb(port + 5) & 0x20);
        }

        public void Write(char ch) { write(ch); }
        [libsupcs.Profile(false)]
        public static void write(char ch)
        {
            _setup();
            while (is_transmit_empty() == 0x00) ;

            if (((ushort)ch) > 0xff)
            {
                write('#');
                writeString(((ushort)ch).ToString("X4"));
            }
            else
                libsupcs.IoOperations.PortOut(port, (byte)ch);
        }

        public virtual void Write(string s) { writeString(s); }
        public void WriteString(string s) { writeString(s); }
        [libsupcs.Profile(false)]
        public static void writeString(string s)
        {
            for (int i = 0; i < s.Length; i++)
                write(s[i]);
        }

        public void Flush() { }
    }
}
