/* Copyright (C) 2017 by John Cronin
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
using libtysila5.target;
using libtysila5.util;
using metadata;
using libtysila5.ir;
using libtysila5.cil;

namespace libtysila5.ir
{
    public partial class ConvertToIR
    {
        internal static Code CreateArrayGet(MethodSpec ms,
            Target t)
        {
            Code c = new Code { t = t, ms = ms };
            t.AllocateLocalVarsArgs(c);
            cil.CilNode n = new cil.CilNode(ms, 0);

            List<cil.CilNode.IRNode> ret = new List<cil.CilNode.IRNode>();
            util.Stack<StackItem> stack_before = new util.Stack<StackItem>();

            // Get return type
            var sig_idx = ms.m.GetMethodDefSigRetTypeIndex(ms.msig);
            var ret_ts = ms.m.GetTypeSpec(ref sig_idx, ms.gtparams, ms.gmparams);
            c.ret_ts = ret_ts;

            if(!ret_ts.Equals(ms.type.other))
            {
                throw new Exception("Array Get return type not the same as element type");
            }

            // Enter
            n.irnodes.Add(new cil.CilNode.IRNode { parent = n, opcode = Opcode.oc_enter, stack_before = stack_before, stack_after = stack_before });

            // Get offset to the data item
            stack_before = ArrayGetDataItemPtr(n, c, stack_before, ms);

            // Load it
            stack_before = ldind(n, c, stack_before, ret_ts);

            // Ret
            n.irnodes.Add(new cil.CilNode.IRNode { parent = n, opcode = Opcode.oc_ret, ct = ((ret_ts == null) ? ir.Opcode.ct_unknown : Opcode.GetCTFromType(ret_ts)), stack_before = stack_before, stack_after = stack_before });

            c.cil = new List<cil.CilNode> { n };
            c.ir = n.irnodes;

            c.starts = new List<cil.CilNode> { n };

            return c;
        }

        private static util.Stack<StackItem> ArrayGetDataItemPtr(CilNode n, Code c, util.Stack<StackItem> stack_before, MethodSpec ms)
        {
            var t = c.t;

            // Get array index
            stack_before = ArrayGetIndex(c, n, ms, stack_before, t);

            // Multiply by elem size
            var esize = layout.Layout.GetTypeSize(ms.type.other, t);
            if (esize > 1)
            {
                stack_before = ldc(n, c, stack_before, esize);
                stack_before = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.mul, Opcode.ct_int32);
            }

            // Add data pointer address
            stack_before = ldarg(n, c, stack_before, 0);
            stack_before = ldc(n, c, stack_before, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.DataArrayPointer, t));
            stack_before = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_before = ldind(n, c, stack_before, ms.m.SystemIntPtr);
            stack_before = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);

            return stack_before;
        }

        private static util.Stack<StackItem> ArrayGetIndex(Code c, cil.CilNode n, MethodSpec ms, util.Stack<StackItem> stack_before, Target t)
        {
            /* For a rank 4 array this simplifies to:
             * 
             * (r4-r4lb) + r4len[(r3-r3lb) + r3len[(r2-r2lb) + r2len[(r1-r1lb)]]]
             * 
             * where len is the 'size' member and lb is the lobounds
             * 
             * in ir:
             * 
             * ldc r1
             * sub r1lb
             * 
             * mul r2len
             * add r2
             * sub r2lb
             * 
             * mul r3len
             * add r3
             * sub r3lb
             * 
             * mul r4len
             * add r4
             * sub r4lb
             * 
             * and we can simplify lobounds/sizes lookups if we know them
             */

            var stack_after = new util.Stack<StackItem>(stack_before);

            // ldc r1
            stack_after = ldarg(n, c, stack_after, 1);

            // sub r1lb
            bool lbzero, szone;    // optimize away situation where lobounds is zero
            stack_after = ArrayGetLobounds(n, c, stack_after, ms, 1, out lbzero);
            if(!lbzero)
                stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.sub, ir.Opcode.ct_int32);

            for(int rank = 2; rank <= ms.type.arr_rank; rank++)
            {
                // mul rnlen
                stack_after = ArrayGetSize(n, c, stack_after, ms, rank, out szone);
                if(!szone)
                    stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.mul, Opcode.ct_int32);

                // add rn
                stack_after = ldarg(n, c, stack_after, rank);
                stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_int32);

                // sub rnlb
                stack_after = ArrayGetLobounds(n, c, stack_after, ms, rank, out lbzero);
                if(!lbzero)
                    stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.sub, ir.Opcode.ct_int32);
            }

            return stack_after;
        }

        private static util.Stack<StackItem> ArrayGetLobounds(CilNode n, Code c, util.Stack<StackItem> stack_before, MethodSpec ms, int rank,
            out bool is_zero)
        {
            var stack_after = new util.Stack<StackItem>(stack_before);
            is_zero = false;
            var t = c.t;

            // if we have the lobounds in the signature we can use that, else do a run-time lookup
            if(ms.type.arr_lobounds != null &&
                ms.type.arr_lobounds.Length >= rank)
            {
                var lb = ms.type.arr_lobounds[rank - 1];
                if (lb == 0)
                    is_zero = true;
                else
                    stack_after = ldc(n, c, stack_after, ms.type.arr_lobounds[rank - 1]);
            }
            else
            {
                // Load pointer to lobounds array
                stack_after = ldarg(n, c, stack_after, 0);
                stack_after = ldc(n, c, stack_after, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.LoboundsPointer, t));
                stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
                stack_after = ldind(n, c, stack_after, ms.m.SystemIntPtr);

                // it's a simple array of int32s, so size is 4
                if (rank > 1)
                {
                    stack_after = ldc(n, c, stack_after, 4 * (rank - 1), 0x18);
                    stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
                }

                stack_after = ldind(n, c, stack_after, ms.m.SystemInt32);
            }

            return stack_after;
        }

        private static util.Stack<StackItem> ArrayGetSize(CilNode n, Code c, util.Stack<StackItem> stack_before, MethodSpec ms, int rank,
            out bool isone)
        {
            var stack_after = new util.Stack<StackItem>(stack_before);
            isone = false;
            var t = c.t;

            // if we have the size in the signature we can use that, else do a run-time lookup
            if (ms.type.arr_sizes != null &&
                ms.type.arr_sizes.Length >= rank)
            {
                var sz = ms.type.arr_sizes[rank - 1];
                if (sz == 1)
                    isone = true;
                else
                    stack_after = ldc(n, c, stack_after, ms.type.arr_sizes[rank - 1]);
            }
            else
            {
                // Load pointer to sizes array
                stack_after = ldarg(n, c, stack_after, 0);
                stack_after = ldc(n, c, stack_after, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.SizesPointer, t));
                stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
                stack_after = ldind(n, c, stack_after, ms.m.SystemIntPtr);

                // it's a simple array of int32s, so size is 4
                if (rank > 1)
                {
                    stack_after = ldc(n, c, stack_after, 4 * (rank - 1), 0x18);
                    stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
                }

                stack_after = ldind(n, c, stack_after, ms.m.SystemInt32);
            }

            return stack_after;
        }
    }
}
