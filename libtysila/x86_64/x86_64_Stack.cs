/* Copyright (C) 2008 - 2012 by John Cronin
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

namespace libtysila
{
    partial class x86_64_Assembler
    {
        internal override libtysila.Stack GetStack(libasm.hardware_stackloc.StackType stack_type)
        {
            switch (stack_type)
            {
                case libasm.hardware_stackloc.StackType.Arg:
                case libasm.hardware_stackloc.StackType.LocalVar:
                    return new DefaultStack(stack_type);

                case libasm.hardware_stackloc.StackType.Var:
                    return new x86_64_Stack(this);

                default:
                    throw new NotSupportedException();
            }
        }

        internal class x86_64_Stack : libtysila.Stack
        {
            util.Stack<libasm.hardware_location> cur_locs;
            int next_loc = 0;
            libasm.hardware_stackloc.StackType stype = libasm.hardware_stackloc.StackType.Var;
            Assembler a;

            //static libasm.hardware_location[] usuable_64_int_locs = new libasm.hardware_location[] { Rdi, Rsi, R8, R9, R10, R11, R12, R13, R14, R15 };
            static libasm.hardware_location[] usuable_64_int_locs = new libasm.hardware_location[] { R10, R11, R12, R13, R14, R15 };
            static libasm.hardware_location[] usuable_32_int_locs = new libasm.hardware_location[] { Rdi, Rsi };
            static libasm.hardware_location[] usuable_64_float_locs = new libasm.hardware_location[] { Xmm4, Xmm5, Xmm6, Xmm7, Xmm8, Xmm9, Xmm10, Xmm11, Xmm12, Xmm13, Xmm14, Xmm15 };
            static libasm.hardware_location[] usuable_32_float_locs = new libasm.hardware_location[] { Xmm4, Xmm5, Xmm6, Xmm7 };

            ICollection<libasm.hardware_location> usuable_int_locs
            {
                get
                {
                    if (a.GetBitness() == Bitness.Bits32)
                        return usuable_32_int_locs;
                    else
                        return usuable_64_int_locs;
                }
            }

            ICollection<libasm.hardware_location> usuable_float_locs
            {
                get
                {
                    if (a.GetBitness() == Bitness.Bits32)
                        return usuable_32_float_locs;
                    else
                        return usuable_64_float_locs;
                }
            }

            util.Stack<libasm.hardware_location> free_int_locs;
            util.Stack<libasm.hardware_location> free_float_locs;

            public x86_64_Stack(Assembler ass) { a = ass; }

            public override libasm.hardware_location GetAddressFor(Signature.Param p, Assembler ass)
            {
                if (cur_locs == null)
                    cur_locs = new util.Stack<libasm.hardware_location>();
                if (free_int_locs == null)
                    free_int_locs = new util.Stack<libasm.hardware_location>(usuable_int_locs);
                if (free_float_locs == null)
                    free_float_locs = new util.Stack<libasm.hardware_location>(usuable_float_locs);

                int obj_size = ass.GetSizeOf(p);

                bool is_int = false;
                bool is_float = false;

                switch (p.CliType(ass))
                {
                    case CliType.int32:
                    case CliType.int64:
                    case CliType.native_int:
                    case CliType.O:
                    case CliType.reference:
                        is_int = true;
                        break;

                    case CliType.F32:
                    case CliType.F64:
                        is_float = true;
                        break;
                }

                int max_reg_size = 4;
                if (a.GetBitness() == Bitness.Bits64)
                    max_reg_size = 8;
                int max_float_reg_size = 8;

                libasm.hardware_location l;
                if (is_int && obj_size <= max_reg_size && free_int_locs.Count > 0)
                {
                    l = free_int_locs.Pop();
                }
                else if (is_float && obj_size <= max_float_reg_size && free_float_locs.Count > 0)
                {
                    l = free_float_locs.Pop();
                }
                else
                {
                    l = new libasm.hardware_stackloc { loc = next_loc, size = obj_size, stack_type = stype };
                    next_loc += util.align(obj_size, ass.GetStackAlign());
                }
                cur_locs.Push(l);

                return l;
            }

            public override libasm.hardware_location GetAddressOf(int idx, Assembler ass)
            {
                if (cur_locs == null || idx < 0 || idx >= cur_locs.Count)
                    throw new IndexOutOfRangeException();
                return cur_locs[idx];
            }

            public override libasm.hardware_location Pop(Assembler ass)
            {
                if (cur_locs == null || cur_locs.Count == 0)
                    throw new IndexOutOfRangeException();

                libasm.hardware_location l = cur_locs.Pop();
                if (usuable_int_locs.Contains(l))
                    free_int_locs.Add(l);
                else if (usuable_float_locs.Contains(l))
                    free_float_locs.Add(l);
                else
                    next_loc = ((libasm.hardware_stackloc)l).loc;
                
                return l;
            }

            public override Stack Clone()
            {
                return new x86_64_Stack(a)
                {
                    cur_locs = cur_locs == null ? null : new util.Stack<libasm.hardware_location>(cur_locs),
                    next_loc = next_loc,
                    stype = stype,
                    free_int_locs = free_int_locs == null ? null : new util.Stack<libasm.hardware_location>(free_int_locs),
                    free_float_locs = free_float_locs == null ? null : new util.Stack<libasm.hardware_location>(free_float_locs)
                };
            }

            public override int ByteSize
            {
                get { return next_loc; }
            }

            public override ICollection<libasm.hardware_location> UsedLocations
            {
                get {
                    if (cur_locs == null)
                        cur_locs = new util.Stack<libasm.hardware_location>();
                    return cur_locs;
                }
            }

            public override void MarkUsed(libasm.hardware_location loc)
            {
                if (cur_locs == null)
                    cur_locs = new util.Stack<libasm.hardware_location>();
                if (!cur_locs.Contains(loc))
                    cur_locs.Add(loc);
            }

            public override void Clear()
            {
                cur_locs = new util.Stack<libasm.hardware_location>();
                next_loc = 0;
            }
        }
    }
}
