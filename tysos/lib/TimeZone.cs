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
    class TimeZone
    {
        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("_ZW6System21CurrentSystemTimeZoneM_0_15GetTimeZoneData_Rb_P3iRu1ZxRu1Zu1S")]
        static bool GetTimeZoneData(int year, out long[] data, out string[] names)
        {
            /* data[0] =        start of DST (in ticks)
             * data[1] =        end of DST (in ticks)
             * data[2] =        offset from UTC (in ticks)
             * data[3] =        additional offset when in DST (in ticks)
             * names[0] =       name of timezone when not in DST
             * names[1] =       name of timezone when in DST
             */

            data = new long[4];
            data[0] = 0;
            data[1] = 0;
            data[2] = 0;
            data[3] = 0;

            names = new string[2];
            names[0] = "UTC";
            names[1] = "UTC";

            return true;
        }
    }
}
