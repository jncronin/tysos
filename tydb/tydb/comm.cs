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

namespace tydb
{
    class comm
    {
        private static void gdb_append_uint_lsb_value(StringBuilder sb, uint val)
        {
            for (int i = 0; i < 4; i++)
            {
                sb.Append((val & 0xff).ToString("x2"));
                val = val >> 8;
            }
        }

        private static void gdb_append_ulong_lsb_value(StringBuilder sb, ulong val)
        {
            for (int i = 0; i < 8; i++)
            {
                sb.Append((val & 0xff).ToString("x2"));
                val = val >> 8;
            }
        }

        internal static void gdb_send_message(string s)
        {
            while (true)
            {
                gdb_send_char('$');
                // calculate a checksum and send the original packet
                int checksum = 0;

                for (int i = 0; i < s.Length; i++)
                {
                    checksum += (int)s[i];
                    gdb_send_char(s[i]);
                }

                string checksum_s = (checksum % 256).ToString("x2");

                // send the checksum
                gdb_send_char('#');
                gdb_send_char(checksum_s[0]);
                gdb_send_char(checksum_s[1]);

                // wait for an acknowledgement
                char ch = gdb_read_char();
                if (ch == '+')
                    return;
                if (ch != '-')
                {
                    System.Diagnostics.Debug.WriteLine("gdb_stub: invalid acknowledgement: " + ch);
                    return;
                }
            }
        }

        internal static string gdb_read_message()
        {
            bool wait_start = true;

            while (true)
            {
                bool cont = true;
                int checksum = 0;
                int checksum_read = 0;
                bool read_package = true;
                bool valid_package = true;

                StringBuilder package = new StringBuilder();
                StringBuilder cs = new StringBuilder();

                // Wait for a start character
                if (wait_start)
                {
                    char start_ch;
                    do
                    {
                        start_ch = gdb_read_char();
                    } while (start_ch != '$');
                }
                wait_start = true;

                while (cont)
                {
                    char ch = gdb_read_char();

                    if (ch == '#')
                        read_package = false;       // start reading checksum
                    else if (ch == '$')
                    {
                        /* This is an invalid package - ignore it */
                        wait_start = false;
                        cont = false;
                        valid_package = false;
                    }
                    else
                    {
                        if (read_package)
                        {
                            package.Append(ch);
                            checksum += (int)ch;
                        }
                        else
                        {
                            cs.Append(ch);
                            checksum_read++;
                            if (checksum_read == 2)
                                cont = false;
                        }
                    }
                }

                if (valid_package)
                {
                    int checksum_i = Int32.Parse(cs.ToString(), System.Globalization.NumberStyles.HexNumber);
                    if (checksum_i != (checksum % 256))
                    {
                        System.Diagnostics.Debug.WriteLine("gdb_stub: checksum incorrect for received packet: " + package.ToString());
                        gdb_send_char('-');
                    }
                    else
                    {
                        gdb_send_char('+');
                        return gdb_decode(package.ToString());
                    }
                }
            }
        }

        private static string gdb_decode(string p)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < p.Length; i++)
            {
                byte b = (byte)p[i];

                if (b == 0x7d)
                {
                    i++;
                    sb.Append((char)(byte)(0x20 ^ (byte)p[i]));
                }
                else
                    sb.Append(p[i]);
            }

            return sb.ToString();
        }

        private static char gdb_read_char()
        {
            int b = -1;
            while(b == -1)
                b = Program.remote.ReadByte();
            return (char)(b & 0xff);
        }

        private static void gdb_send_char(char p)
        {
            Program.remote.WriteByte((byte)(p & 0xff));
        }

        internal static bool remote_supports(string p)
        {
            comm.gdb_send_message("qSupported");
            string supports = comm.gdb_read_message();
            if ((supports == null) || (supports == ""))
                return false;

            string[] supported_vals = supports.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in supported_vals)
            {
                string test = s;
                if (test.EndsWith("+"))
                    test = test.Substring(0, test.Length - 1);
                else if (test.EndsWith("-"))
                    continue;
                if (test == p)
                    return true;
            }
            return false;
        }
    }
}
