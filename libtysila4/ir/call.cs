/* Copyright (C) 2016 by John Cronin
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
using libtysila4.cil;

namespace libtysila4.ir
{
    partial class IrGraph
    {
        static Opcode[] call(cil.CilNode start, target.Target t)
        {
            metadata.MetadataStream m;
            uint token;
            start.GetToken(out m, out token);
            int table_id, row;
            m.InterpretToken(token, out table_id, out row);


            metadata.MethodSpec ms;
            if (m.GetMethodDefRow(table_id, row, out ms,
                start.n.g.ms.gtparams,
                start.n.g.ms.gmparams) == false)
                throw new MissingMethodException();

            return call(start, ms, t);
        }

        static Opcode[] call(cil.CilNode start, metadata.MethodSpec ms,
            target.Target t)
        {
            bool is_virt = false;
            int vtbl_offset = 0;
            if (start.opcode.opcode1 == cil.Opcode.SingleOpcodes.callvirt)
            {
                vtbl_offset = layout.Layout.GetVTableOffset(ms);

                if (vtbl_offset == 0)
                    throw new MissingMethodException();
                else if (vtbl_offset > 0)
                {
                    is_virt = true;
                }
            }
            else if (start.opcode.opcode1 == cil.Opcode.SingleOpcodes.calli)
                throw new NotImplementedException();

            int param_count = ms.m.GetMethodDefSigParamCountIncludeThis(ms.msig);
            int ret_idx = ms.m.GetMethodDefSigRetTypeIndex(ms.msig);
            var ret_ts = ms.m.GetTypeSpec(ref ret_idx, ms.gtparams,
                ms.gmparams);

            bool is_void_ret = false;
            if (ret_ts == null)
                is_void_ret = true;

            Param[] defs;
            if (is_void_ret)
                defs = new Param[] { };
            else
                defs = new Param[] { new Param { t = Opcode.vl_stack, v = 0, ud = Param.UseDefType.Def } };

            Param[] uses = new Param[param_count + 1];
            if (is_virt)
                uses[0] = new Param { t = Opcode.vl_c, ct = Opcode.ct_intptr, v = vtbl_offset, ms = ms, m = ms.m };
            else
                uses[0] = new Param { t = Opcode.vl_call_target, ms = ms, m = ms.m };
            for (int i = 0; i < param_count; i++)
                uses[param_count - i] = new Param { t = Opcode.vl_stack, v = i, ud = Param.UseDefType.Use };

            /* Is this an internal call? */
            var mname = ms.m.MangleMethod(ms);
            var intcall = ir.intcall.intcall.do_intcall(mname,
                defs, uses, start, t);
            if (intcall != null)
                return intcall;

            t.r.MethodRequestor.Request(ms);

            int oc = Opcode.oc_call;
            if (is_virt)
                oc = Opcode.oc_callvirt;

            return new Opcode[] { new Opcode { oc = oc, defs = defs, uses = uses } };
        }

        static Opcode[] call_stloc(cil.CilNode start, target.Target t)
        {
            var cops = call(start, t);
            if (cops == null)
                return null;

            var lv = GetParam((cil.CilNode)start.n.Next1.c);

            foreach (var cop in cops)
            {
                if (cop.oc == Opcode.oc_call)
                {
                    if (cop.defs.Length == 1)
                    {
                        cop.defs[0] = lv;
                        return cops;
                    }
                    return null;
                }
            }
            return null;
        }
    }
}
