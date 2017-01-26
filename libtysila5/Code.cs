﻿/* Copyright (C) 2017 by John Cronin
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

namespace libtysila5
{
    public class Code
    {
        public metadata.MethodSpec ms;
        public List<cil.CilNode> cil;
        public List<cil.CilNode.IRNode> ir;
        public List<target.MCInst> mc;
        public int lvar_sig_tok;

        public List<cil.CilNode> starts;

        public target.Target.Reg[] lv_locs;
        public target.Target.Reg[] la_locs;
        public int[] lv_sizes;
        public int[] la_sizes;
        public int lv_total_size;
        public metadata.TypeSpec[] lv_types;
        public metadata.TypeSpec[] la_types;
        public target.Target.Reg[] incoming_args;
        public bool[] la_needs_assign;

        public target.Target t;

        public List<int> offset_order = new List<int>();
        public Dictionary<int, cil.CilNode> offset_map =
                new Dictionary<int, cil.CilNode>(new libtysila5.GenericEqualityComparer<int>());
    }
}
