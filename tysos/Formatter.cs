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

namespace tysos
{
    class Formatter
    {
        [libsupcs.Profile(false)]
        public static void Write(char ch, IDebugOutput s)
        {
            if (s != null)
            {
                s.Write(ch);
                s.Flush();
            }
        }

        [libsupcs.Profile(false)]
        public static void WriteLine(IDebugOutput s)
        {
            if (s != null)
            {
                s.Write('\n');
                s.Flush();
            }
        }

        [libsupcs.Profile(false)]
        public static void WriteLine(string str, IDebugOutput s)
        {
            if (s != null)
            {
                Write(str, s);
                s.Write('\n');
                s.Flush();
            }
        }

        [libsupcs.Profile(false)]
        public static void Write(string str, IDebugOutput s)
        {
            if (s != null)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    var c = str[i];
                    s.Write(c);
                }
                s.Flush();
            }
        }

        [libsupcs.Profile(false)]
        public static void Write(string fmt, IDebugOutput s, params object[] p)
        {
            if (s == null)
                return;

            // define the current state of the iteration
            bool in_escape = false;
            bool in_index = false;
            bool in_alignment = false;
            bool in_format = false;
            int index_start = 0;
            int alignment_start = 0;
            int format_start = 0;
            int index_end = 0;
            int alignment_end = 0;
            int format_end = 0;
            bool has_alignment = false;
            bool has_format = false;

            for (int i = 0; i < fmt.Length; i++)
            {
                if (in_escape)
                {
                    if (fmt[i] == '\\')
                        s.Write('\\');
                    else if (fmt[i] == 'n')
                        s.Write('\n');

                    in_escape = false;
                }
                else if ((in_format || in_alignment || in_index) && (fmt[i] == '}'))
                {
                    if ((i < (fmt.Length - 1)) && (fmt[i + i] == '}'))
                    {
                        s.Write('}');
                        i++;
                    }
                    else
                    {
                        if (in_format)
                            format_end = i;
                        if (in_alignment)
                            alignment_end = i;
                        if (in_index)
                            index_end = i;
                        in_format = in_alignment = in_index = false;

                        // interpret the format, alignment and index and output the appropriate value

                        // index is a integer in decimal format
                        int mult = 1;
                        int index = 0;
                        for (int j = index_end - 1; j >= index_start; j++)
                        {
                            int diff = (int)fmt[j] - (int)'0';
                            if ((diff >= 0) && (diff <= 9))
                                index += (diff * mult);
                            if (fmt[j] == '-')
                            {
                                index = -index;
                                break;
                            }
                            mult *= 10;
                        }
                        // as is alignment
                        int alignment = 0;
                        mult = 1;
                        if (has_alignment)
                        {
                            for (int j = alignment_end - 1; j >= alignment_start; j++)
                            {
                                int diff = (int)fmt[j] - (int)'0';
                                if ((diff >= 0) && (diff <= 9))
                                    alignment += (diff * mult);
                                if (fmt[j] == '-')
                                {
                                    alignment = -alignment;
                                    break;
                                }
                                mult *= 10;
                            }
                        }

                        // TODO: interpret format string
                        // Use RTTI to determine what p[index] is and display it using the specified alignment
                        //  and format string

                        has_alignment = has_format = false;
                    }
                }
                else if (in_format)
                {
                    // Nothing to do
                }
                else if (in_alignment)
                {
                    if (fmt[i] == ':')
                    {
                        in_alignment = false;
                        in_format = true;
                        alignment_end = i;
                        format_start = i + 1;
                        has_format = true;
                    }
                }
                else if (in_index)
                {
                    if (fmt[i] == ',')
                    {
                        in_index = false;
                        in_alignment = true;
                        index_end = i;
                        alignment_start = i + 1;
                        has_alignment = true;
                    }
                    if (fmt[i] == ':')
                    {
                        in_index = false;
                        in_format = true;
                        index_end = i;
                        format_start = i + 1;
                        has_format = true;
                    }
                }
                else
                {
                    if (fmt[i] == '{')
                    {
                        if ((i < (fmt.Length - 1)) && (fmt[i + i] == '{'))
                        {
                            s.Write('{');
                            i++;
                        }
                        else
                        {
                            index_start = i + 1;
                            in_index = true;
                        }
                    }
                    else
                    {
                        if (fmt[i] == '\\')
                            in_escape = true;
                        else
                            s.Write(fmt[i]);
                    }
                }
            }
            s.Flush();
        }

        [libsupcs.Profile(false)]
        public static void Write(ulong v, IDebugOutput s)
        { if(s != null) Write(v, null, s); }

        [libsupcs.Profile(false)]
        public static void Write(ulong v, string fmt, IDebugOutput s)
        {
            if (s == null)
                return;
            if (fmt == null)
                _Write(v, 10, s, false, 1);
            else
            {
                if (fmt.Length == 0)
                    _Write(v, 10, s, false, 1);
                else
                {
                    if ((fmt[0] == 'D') || (fmt[0] == 'd'))
                        _Write(v, 10, s, false, 1);
                    else if (fmt[0] == 'X')
                        _Write(v, 16, s, true, 16);
                    else if (fmt[0] == 'x')
                        _Write(v, 16, s, false, 16);
                    else if ((fmt[0] == 'b') || (fmt[0] == 'B'))
                        _Write(v, 2, s, false, 64);
                }
            }
            s.Flush();
        }

        const string UppercaseDigits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string LowercaseDigits = "0123456789abcdefghijklmnopqrstuvwxyz";

        static byte[] digits = new byte[64];

        [libsupcs.Profile(false)]
        private static void _Write(ulong v, int Base, IDebugOutput s, bool uppercase, int min_digits)
        {
            int cur_digit = 0;

            while (v != 0)
            {
                digits[cur_digit] = (byte)(v % (ulong)Base);
                v /= (ulong)Base;
                cur_digit++;
            }

            while (cur_digit < min_digits)
            {
                digits[cur_digit] = 0;
                cur_digit++;
            }

            for (int i = (cur_digit - 1); i >= 0; i--)
            {
                if (uppercase)
                    s.Write(UppercaseDigits[digits[i]]);
                else
                    s.Write(LowercaseDigits[digits[i]]);
            }           
        }
    }
}
