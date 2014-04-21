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
    class util
    {    
        /** <summary>Aligns a value to a multiple of a particular value</summary>
         */
        public static int align(int input, int factor)
        {
            int i_rem_f = input % factor;
            if (i_rem_f == 0)
                return input;
            return input - i_rem_f + factor;
        }

        /** <summary>Aligns a value to a multiple of a particular value</summary>
         */
        public static ulong align(ulong input, ulong factor)
        {
            ulong i_rem_f = input % factor;
            if (i_rem_f == 0)
                return input;
            return input - i_rem_f + factor;
        }

        /** <summary>Adds a long to a ulong (or subtracts if the value is negative)</summary> */
        public static ulong Add(ulong a, long b)
        {
            if (b < 0)
                return a - (ulong)(-b);
            else
                return a + (ulong)b;
        }
    }
}
