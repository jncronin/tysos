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

/* Extra functions to support the string class */

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos
{
    class TString
    {
        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("_Zu1SM_0_12InternalJoin_Ru1S_P4u1Su1Zu1Sii")]
        public static string InternalJoin(string separator, string[] value, int sIndex, int count)
        {
            StringBuilder sb = new StringBuilder();

            if (separator == null)
                separator = String.Empty;

            for (int i = 0; i < count; i++)
            {
                if (i != 0)
                    sb.Append(separator);

                sb.Append(value[i + sIndex]);
            }

            return sb.ToString();
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("_Zu1SM_0_13InternalSplit_Ru1Zu1S_P3u1tu1Zci")]
        static string[] InternalSplit(string s, char[] separator, int count)
        {
            List<int> start_chars = new List<int>();
            int str_len = s.Length;
            int sep_len = separator.Length;
            int num_parts = 0;

            /* Formatter.Write("InternalSplit: s: ", Program.arch.DebugOutput);
            Formatter.Write(s, Program.arch.DebugOutput);
            Formatter.Write(", separator.Length: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)separator.Length, Program.arch.DebugOutput);
            Formatter.Write(", count: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)count, Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput); */

            start_chars.Add(0);
            num_parts++;

            for (int i = 0; i < str_len; i++)
            {
                for (int j = 0; j < sep_len; j++)
                {
                    if (s[i] == separator[j])
                    {
                        /* Formatter.Write("InternalSplit: found separator at ", Program.arch.DebugOutput);
                        Formatter.Write((ulong)i, Program.arch.DebugOutput);
                        Formatter.WriteLine(Program.arch.DebugOutput); */

                        start_chars.Add(i + 1);
                        num_parts++;
                        break;
                    }
                }
            }

            /* Formatter.Write("InternalSplit: num_parts: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)num_parts, Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput); */

            int parts = num_parts;
            if (count < parts)
                parts = count;

            /* Formatter.Write("InternalSplit: parts: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)parts, Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput); */

            string[] ret = new string[parts];

            for (int i = 0; i < parts; i++)
            {
                int last_char;
                if(i == (parts - 1))
                    last_char = str_len - 1;
                else
                    last_char = start_chars[i + 1] - 2;

                ret[i] = s.Substring(start_chars[i], last_char - start_chars[i] + 1);

                /* Formatter.Write("InternalSplit: adding '", Program.arch.DebugOutput);
                Formatter.Write(ret[i], Program.arch.DebugOutput);
                Formatter.WriteLine("'", Program.arch.DebugOutput); */
            }

            return ret;
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("_Zu1SM_0_15InternalReplace_Ru1S_P4u1tu1Su1SW22System#2EGlobalization11CompareInfo")]
        static string InternalReplace(string str, string old_value, string new_value, System.Globalization.CompareInfo ci)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < old_value.Length; j++)
                {
                    if (((i + j) >= str.Length) || (str[i + j] != old_value[j]))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    sb.Append(new_value);
                    i += old_value.Length;
                    i--;        // the for loop adds a 1 for us
                }
                else
                    sb.Append(str[i]);
            }

            return sb.ToString();
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("_Zu1SM_0_12InternalTrim_Ru1S_P3u1tu1Zci")]
        static string InternalTrim(string str, char[] chars, int typ)
        {
            /* If typ = 0 then trim start and end
             * If typ = 1 then only trim start
             * If typ = 2 then only trim end */

            int start_char = 0;
            int end_char = str.Length - 1;

            if ((typ == 0) || (typ == 1))
            {
                /* Trim start */
                for (start_char = 0; start_char < str.Length; start_char++)
                {
                    bool contains = true;
                    foreach (char ch in chars)
                    {
                        if (ch != str[start_char])
                        {
                            contains = false;
                            break;
                        }
                    }

                    if (!contains)
                        break;
                }
            }

            if ((typ == 0) || (typ == 2))
            {
                /* Trim end */
                for (end_char = str.Length - 1; end_char >= 0; end_char--)
                {
                    bool contains = true;
                    foreach (char ch in chars)
                    {
                        if (ch != str[end_char])
                        {
                            contains = false;
                            break;
                        }
                    }

                    if (!contains)
                        break;
                }
            }

            /* Now start_char points to the first character of the returned string
             * and end_char points to the last char */
            int length = end_char - start_char + 1;
            if (length <= 0)
                return string.Empty;

            return str.Substring(start_char, length);
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("_Zu1SM_0_22InternalLastIndexOfAny_Ri_P4u1tu1Zcii")]
        static int InternalLastIndexOfAny(string str, char[] chars, int startIndex, int count)
        {
            for (int i = startIndex; (i > (startIndex - count)) && (i >= 0); i--)
            {
                foreach (char ch in chars)
                {
                    if (ch == str[i])
                        return i;
                }
            }
            return -1;
        }
    }

    class TChar
    {
        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("_ZcM_0_20GetDataTablePointers_Rv_P7RPhRPhRPdRPtRPtRPtRPt")]
        static unsafe void GetDataTablePointers(out byte* category_data, out byte* numeric_data,
            out double* numeric_data_values, out ushort* to_lower_data_low,
            out ushort* to_lower_data_high, out ushort* to_upper_data_low,
            out ushort* to_upper_data_high)
        {
            category_data = (byte *)libsupcs.MemoryOperations.GetInternalArray(CategoryData.cCategoryData.CategoryData);
            numeric_data = (byte*)libsupcs.MemoryOperations.GetInternalArray(unicode_support_output.DataTables.NumericData);
            numeric_data_values = (double*)libsupcs.MemoryOperations.GetInternalArray(unicode_support_output.DataTables.NumericDataValues);
            to_lower_data_low = (ushort*)libsupcs.MemoryOperations.GetInternalArray(unicode_support_output.DataTables.ToLowerDataLow);
            to_lower_data_high = (ushort*)libsupcs.MemoryOperations.GetInternalArray(unicode_support_output.DataTables.ToLowerDataHigh);
            to_upper_data_low = (ushort*)libsupcs.MemoryOperations.GetInternalArray(unicode_support_output.DataTables.ToUpperDataLow);
            to_upper_data_high = (ushort*)libsupcs.MemoryOperations.GetInternalArray(unicode_support_output.DataTables.ToUpperDataHigh);
        }
    }
}
