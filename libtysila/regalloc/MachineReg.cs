/* Copyright (C) 2014 by John Cronin
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

/* Mapping between hardware registers of type hardware_location and reg alloc 
 * registers of type int */

using System;
using System.Collections.Generic;

namespace libtysila.regalloc
{
    partial class RegAlloc
    {
        public Dictionary<libasm.hardware_location, int> MRToReg = new Dictionary<libasm.hardware_location, int>();
        public Dictionary<int, libasm.hardware_location> RegToMR = new Dictionary<int, libasm.hardware_location>();
        int next_reg = 0;

        public int AddMachineRegister(libasm.hardware_location mreg)
        {
            MRToReg[mreg] = next_reg;
            RegToMR[next_reg] = mreg;
            return next_reg++;
        }
    }
}
