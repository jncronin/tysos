/* Copyright (C) 2008 - 2014 by John Cronin
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

namespace libtysila.frontend.cil
{
    public class InstructionLine
    {
        public class PrefixesClass
        {
            public bool constrained = false;
            public Token constrained_tok = null;
            public bool no_typecheck = false;
            public bool no_rangecheck = false;
            public bool no_nullcheck = false;
            public bool read_only = false;
            public bool tail = false;
            public bool unaligned = false;
            public int unaligned_alignment = 0;
            public bool volatile_ = false;
        }

        public PrefixesClass Prefixes = new PrefixesClass();

        public int il_offset;
        public Opcode opcode;
        public int inline_int;
        public uint inline_uint;
        public long inline_int64;
        public ulong inline_uint64;
        public double inline_dbl;
        public float inline_sgl;
        public Token inline_tok;
        public List<int> inline_array = new List<int>();
        public byte[] inline_val;

        public bool from_cil = false;

        public int stack_before_adjust = 0;

        public bool start_block = false;
        public bool end_block = false;

        public bool int_array = false;

        public bool allow_obj_numop = false;

        public List<int> il_offsets_after = new List<int>();

        public Assembler.MethodToCompile int_call_mtc;
        
        /** <summary>The immediate offset after the current line.  The instruction at this offset may not actually
         * be executed next (see il_offsets_after instead), it it purely for purposes of calculating jumps that are
         * relative to the next instruction</summary> */
        public int il_offset_after;

        public Signature.Param pushes;
        public var pushes_variable = var.Null;
        public int pop_count;

        public List<var> node_global_vars = new List<var>();

        //public List<PseudoStack> stack_before, stack_after, lv_before, lv_after, la_before, la_after;
        public List<timple.TreeNode> tacs;
        public List<tybel.Node> tybel;
        public util.Stack<Signature.Param> stack_before, stack_after;
        public util.Stack<vara> stack_vars_before, stack_vars_after;

        public override string ToString()
        {
            if (opcode != null)
                return opcode.ToString();
            return base.ToString();
        }

        public static implicit operator CilNode(InstructionLine il)
        {
            return new CilNode { il = il };
        }   
    }
}
