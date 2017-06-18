/* Copyright (C) 2012 by John Cronin
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
    class CompareInfo
    {
        [libsupcs.MethodAlias("_ZW22System#2EGlobalization11CompareInfo_14internal_index_Ri_P7u1tu1Siiu1SV14CompareOptionsb")]
        [libsupcs.AlwaysCompile]
        static int internal_index(System.Globalization.CompareInfo _this, string source, int sIndex, int count, string value, System.Globalization.CompareOptions options, bool first)
        {
            // if first == true find first incidence, else find last

            if (first)
            {
                for (int i = sIndex; i < (sIndex + count); i++)
                {
                    bool found = true;
                    for (int j = 0; j < value.Length; j++)
                    {
                        if (source[j + i] != value[j])
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found)
                        return i;
                }
            }
            else
            {
                for (int i = (sIndex + count - 1); i >= sIndex; i--)
                {
                    bool found = true;
                    for (int j = 0; j < value.Length; j++)
                    {
                        if (source[j + i] != value[j])
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found)
                        return i;
                }
            }
            return -1;
        }

        [libsupcs.MethodAlias("_ZW22System#2EGlobalization11CompareInfo_14internal_index_Ri_P7u1tu1SiicV14CompareOptionsb")]
        [libsupcs.AlwaysCompile]
        static int internal_index(System.Globalization.CompareInfo _this, string source, int sIndex, int count, char value, System.Globalization.CompareOptions options, bool first)
        {
            // if first == true find first incidence, else find last

            if (first)
            {
                for (int i = sIndex; i < (sIndex + count); i++)
                {
                    if (source[i] == value)
                        return i;
                }
            }
            else
            {
                for (int i = (sIndex + count - 1); i >= sIndex; i--)
                {
                    if (source[i] == value)
                        return i;
                }
            }
            return -1;
        }
    }
}
