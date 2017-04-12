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
using libtysila5.cil;

namespace libtysila5.target.x86
{
    partial class x86_Assembler
    {
        public override List<MCInst> ChooseInstruction(List<CilNode.IRNode> nodes, int start, int count, Code c)
        {
            if (count == 1)
            {
                var n = nodes[start];
                var n_ct = n.ct;
                var n_ct2 = n.ct2;
                string fc_override = null;
                switch (n_ct)
                {
                    case ir.Opcode.ct_intptr:
                    case ir.Opcode.ct_object:
                    case ir.Opcode.ct_ref:
                        n_ct = ir.Opcode.ct_int32;
                        break;
                }
                switch (n.opcode)
                {
                    case ir.Opcode.oc_ldc:
                        if (n_ct == ir.Opcode.ct_int32)
                            return new List<MCInst> { inst(x86_mov_rm32_imm32, n.stack_after.Peek(n.res_a).reg, (int)n.imm_l, n) };
                        break;

                    case ir.Opcode.oc_stackcopy:
                        {
                            var src = n.stack_before.Peek(n.arg_a).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            var r = new List<MCInst>();
                            if (n_ct == ir.Opcode.ct_int32)
                            {
                                handle_move(dest, src, r, n, c);
                                return r;
                            }
                            else if (n_ct == ir.Opcode.ct_int64)
                            {
                                var drs = src as DoubleReg;
                                var drd = dest as DoubleReg;

                                handle_move(drd.a, drs.a, r, n, c);
                                handle_move(drd.b, drs.b, r, n, c);
                                return r;
                            }
                        }
                        break;

                    case ir.Opcode.oc_stloc:
                        {
                            var src = n.stack_before.Peek(n.arg_a).reg;
                            var dest = c.lv_locs[(int)n.imm_l];

                            var r = new List<MCInst>();

                            if (n_ct == ir.Opcode.ct_int32)
                            {
                                handle_move(dest, src, r, n, c);
                                return r;
                            }
                            else if (n_ct == ir.Opcode.ct_int64)
                            {
                                var srca = src.SubReg(0, 4);
                                var srcb = src.SubReg(4, 4);
                                var desta = dest.SubReg(0, 4);
                                var destb = dest.SubReg(4, 4);

                                handle_move(desta, srca, r, n, c);
                                handle_move(destb, srcb, r, n, c);

                                return r;
                            }
                        }
                        break;

                    case ir.Opcode.oc_ldloc:
                        {
                            var src = c.lv_locs[(int)n.imm_l];
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            var r = new List<MCInst>();
                            if (n_ct == ir.Opcode.ct_int32)
                            {
                                handle_move(dest, src, r, n, c);
                                return r;
                            }
                            else if (n_ct == ir.Opcode.ct_int64)
                            {
                                var desta = dest.SubReg(0, 4);
                                var destb = dest.SubReg(4, 4);
                                var srca = src.SubReg(0, 4);
                                var srcb = src.SubReg(4, 4);

                                handle_move(desta, srca, r, n, c);
                                handle_move(destb, srcb, r, n, c);

                                return r;
                            }
                        }
                        break;

                    case ir.Opcode.oc_ldarg:
                        {
                            var src = c.la_locs[(int)n.imm_l];
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            var r = new List<MCInst>();
                            if (n_ct == ir.Opcode.ct_int32 ||
                                n_ct == ir.Opcode.ct_float)
                            {
                                handle_move(dest, src, r, n, c);
                                return r;
                            }
                            else if (n_ct == ir.Opcode.ct_int64)
                            {
                                var drd = dest as DoubleReg;

                                var crs = src as ContentsReg;
                                var crsa = new ContentsReg { basereg = crs.basereg, disp = crs.disp, size = 4 };
                                var crsb = new ContentsReg { basereg = crs.basereg, disp = crs.disp + 4, size = 4 };

                                handle_move(drd.a, crsa, r, n, c);
                                handle_move(drd.b, crsb, r, n, c);

                                return r;
                            }
                        }
                        break;

                    case ir.Opcode.oc_add:
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            List<MCInst> r = new List<MCInst>();

                            switch (n_ct)
                            {
                                case ir.Opcode.ct_int32:
                                    handle_add(srca, srcb, dest, r, n);
                                    return r;

                                case ir.Opcode.ct_int64:
                                    {
                                        var srcaa = srca.SubReg(0, 4);
                                        var srcab = srca.SubReg(4, 4);
                                        var srcba = srcb.SubReg(0, 4);
                                        var srcbb = srcb.SubReg(4, 4);
                                        var desta = dest.SubReg(0, 4);
                                        var destb = dest.SubReg(4, 4);
                                        handle_add(srcaa, srcba, desta, r, n);
                                        handle_add(srcab, srcbb, destb, r, n, true);
                                        return r;
                                    }
                            }
                        }
                        break;

                    case ir.Opcode.oc_sub:
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            List<MCInst> r = new List<MCInst>();

                            switch (n_ct)
                            {
                                case ir.Opcode.ct_int32:
                                    handle_sub(srca, srcb, dest, r, n);
                                    return r;

                                case ir.Opcode.ct_int64:
                                    {
                                        var dra = srca as DoubleReg;
                                        var drb = srcb as DoubleReg;
                                        var drd = dest as DoubleReg;
                                        handle_sub(dra.a, drb.a, drd.a, r, n);
                                        handle_sub(dra.b, drb.b, drd.b, r, n, true);
                                        return r;
                                    }
                            }
                        }
                        break;

                    case ir.Opcode.oc_and:
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            List<MCInst> r = new List<MCInst>();

                            switch (n_ct)
                            {
                                case ir.Opcode.ct_int32:
                                    handle_and(srca, srcb, dest, r, n);
                                    return r;

                                case ir.Opcode.ct_int64:
                                    {
                                        var dra = srca as DoubleReg;
                                        var drb = srcb as DoubleReg;
                                        var drd = dest as DoubleReg;
                                        handle_and(dra.a, drb.a, drd.a, r, n);
                                        handle_and(dra.b, drb.b, drd.b, r, n);
                                        return r;
                                    }
                            }
                        }
                        break;

                    case ir.Opcode.oc_or:
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            List<MCInst> r = new List<MCInst>();

                            switch (n_ct)
                            {
                                case ir.Opcode.ct_int32:
                                    handle_or(srca, srcb, dest, r, n);
                                    return r;

                                case ir.Opcode.ct_int64:
                                    {
                                        var dra = srca as DoubleReg;
                                        var drb = srcb as DoubleReg;
                                        var drd = dest as DoubleReg;
                                        handle_or(dra.a, drb.a, drd.a, r, n);
                                        handle_or(dra.b, drb.b, drd.b, r, n);
                                        return r;
                                    }
                            }
                        }
                        break;

                    case ir.Opcode.oc_shl:
                    case ir.Opcode.oc_shr:
                    case ir.Opcode.oc_shr_un:
                        if (n_ct == ir.Opcode.ct_int32)
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            List<MCInst> r = new List<MCInst>();

                            bool cl_in_use_before = false;
                            bool cl_in_use_after = false;

                            if (!srca.Equals(dest) || srca.Equals(r_ecx))
                            {
                                // if either of the above is true, we need to move
                                //  the source to eax
                                handle_move(r_eax, srca, r, n, c);
                                srca = r_eax;
                            }

                            if (!srcb.Equals(r_ecx))
                            {
                                // need to assign the number to move to cl
                                // first determine if it is in use

                                ulong defined = 0;
                                foreach (var si in n.stack_before)
                                    defined |= si.reg.mask;
                                if ((defined & r_ecx.mask) != 0)
                                    cl_in_use_before = true;
                                defined = 0;
                                foreach (var si in n.stack_after)
                                    defined |= si.reg.mask;
                                if ((defined & r_ecx.mask) != 0)
                                    cl_in_use_after = true;
                            }

                            if (cl_in_use_before && cl_in_use_after)
                            {
                                handle_move(r_ecx, srcb, r, n, c);
                                srcb = r_ecx;
                            }

                            int oc = 0;
                            switch (n.opcode)
                            {
                                case ir.Opcode.oc_shl:
                                    oc = x86_sal_rm32_cl;
                                    break;
                                case ir.Opcode.oc_shr:
                                    oc = x86_sar_rm32_cl;
                                    break;
                                case ir.Opcode.oc_shr_un:
                                    oc = x86_shr_rm32_cl;
                                    break;
                            }

                            r.Add(inst(oc, srca, srcb, n));

                            if (!srca.Equals(dest))
                                handle_move(dest, srca, r, n, c);

                            return r;
                        }
                        break;

                    case ir.Opcode.oc_cmp:
                        if (n_ct == ir.Opcode.ct_int32)
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            List<MCInst> r = new List<MCInst>();
                            if (!(srca is ContentsReg))
                                r.Add(inst(x86_cmp_r32_rm32, srca, srcb, n));
                            else if (!(srcb is ContentsReg))
                                r.Add(inst(x86_cmp_rm32_r32, srca, srcb, n));
                            else
                            {
                                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                                r.Add(inst(x86_cmp_r32_rm32, r_eax, srcb, n));
                            }

                            r.Add(inst(x86_set_rm32, new ir.Param { t = ir.Opcode.vl_cc, v = (int)n.imm_ul }, r_eax, n));
                            if (dest is ContentsReg)
                            {
                                r.Add(inst(x86_movzxbd, r_eax, r_eax, n));
                                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
                            }
                            else
                                r.Add(inst(x86_movzxbd, dest, r_eax, n));

                            return r;
                        }
                        break;

                    case ir.Opcode.oc_brif:
                        if (n_ct == ir.Opcode.ct_int32)
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;

                            List<MCInst> r = new List<MCInst>();

                            var cc = (int)n.imm_ul;
                            var target = (int)n.imm_l;

                            handle_brifi32(srca, srcb, cc, target, r, n);

                            return r;
                        }
                        else if (n_ct == ir.Opcode.ct_int64)
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg as DoubleReg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg as DoubleReg;

                            List<MCInst> r = new List<MCInst>();

                            var cc = (int)n.imm_ul;
                            var target = (int)n.imm_l;

                            // the following is untested
                            var other_cc = ir.Opcode.cc_invert_map[cc];
                            var neq_path = c.next_mclabel--;
                            handle_brifi32(srca.b, srcb.b, other_cc, neq_path, r, n);
                            handle_brifi32(srca.a, srcb.a, cc, target, r, n);
                            r.Add(inst(Generic.g_mclabel, new ir.Param { t = ir.Opcode.vl_br_target, v = neq_path }, n));

                            return r;
                        }
                        else if (n_ct == ir.Opcode.ct_float)
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;

                            List<MCInst> r = new List<MCInst>();

                            var cc = (int)n.imm_ul;
                            var target = (int)n.imm_l;

                            handle_briff(srca, srcb, cc, target, r, n);

                            return r;
                        }
                        break;


                    case ir.Opcode.oc_conv:
                        bool is_un = (n.imm_l & 0x1) != 0;
                        bool is_ovf = (n.imm_l & 0x2) != 0;
                        if (n_ct == ir.Opcode.ct_int32)
                        {
                            var r = new List<MCInst>();

                            var si = n.stack_before.Peek(n.arg_a);
                            var di = n.stack_after.Peek(n.res_a);
                            var to_type = di.ts.SimpleType;

                            var sreg = si.reg;
                            var dreg = di.reg;

                            var actdreg = dreg;
                            if (dreg is ContentsReg)
                                dreg = r_edx;

                            switch (to_type)
                            {
                                case 0x04:
                                    if (sreg.Equals(r_esi) || sreg.Equals(r_edi))
                                    {
                                        r.Add(inst(x86_mov_r32_rm32, r_eax, sreg, n));
                                        sreg = r_eax;
                                    }
                                    r.Add(inst(x86_movsxbd, dreg, sreg, n));
                                    break;
                                case 0x05:
                                    if (sreg.Equals(r_esi) || sreg.Equals(r_edi))
                                    {
                                        r.Add(inst(x86_mov_r32_rm32, r_eax, sreg, n));
                                        sreg = r_eax;
                                    }
                                    r.Add(inst(x86_movzxbd, dreg, sreg, n));
                                    break;
                                case 0x06:
                                    // int16
                                    r.Add(inst(x86_movsxwd, dreg, sreg, n));
                                    break;
                                case 0x07:
                                    // uint16
                                    r.Add(inst(x86_movzxwd, dreg, sreg, n));
                                    break;
                                case 0x08:
                                case 0x09:
                                case 0x18:
                                case 0x19:
                                    // nop
                                    if (!sreg.Equals(dreg))
                                        handle_move(dreg, sreg, r, n, c);
                                    return r;
                                case 0x0a:
                                    // conv to i8
                                    {
                                        DoubleReg dr = dreg as DoubleReg;
                                        if (!(dr.a.Equals(sreg)))
                                        {
                                            handle_move(dr.a, sreg, r, n, c);
                                        }
                                        handle_move(dr.b, sreg, r, n, c);
                                        r.Add(inst(x86_sar_rm32_imm8, dr.b, 31, n));
                                    }
                                    break;
                                case 0x0b:
                                    // conv to u8
                                    {
                                        DoubleReg dr = dreg as DoubleReg;
                                        if (!(dr.a.Equals(sreg)))
                                        {
                                            handle_move(dr.a, sreg, r, n, c);
                                        }
                                        r.Add(inst(x86_mov_rm32_imm32, dr.b, new ir.Param { t = ir.Opcode.vl_c, v = 0 }, n));
                                    }
                                    break;
                                case 0x0c:
                                case 0x0d:
                                    // conv to r4/8 (internal representation is the same)
                                    {
                                        if (dreg == r_edx)
                                            dreg = r_xmm7;
                                        r.Add(inst(x86_cvtsi2sd_xmm_rm32, dreg, sreg, n));
                                        break;
                                    }
                                default:
                                    throw new NotImplementedException("Convert to " + to_type.ToString());
                            }

                            if (!dreg.Equals(actdreg))
                                handle_move(actdreg, dreg, r, n, c);

                            return r;
                        }
                        else if (n_ct == ir.Opcode.ct_int64)
                        {
                            var r = new List<MCInst>();

                            var si = n.stack_before.Peek(n.arg_a);
                            var di = n.stack_after.Peek(n.res_a);
                            var to_type = di.ts.SimpleType;

                            var sreg = si.reg;
                            var dreg = di.reg;

                            var srca = sreg.SubReg(0, 4);
                            var srcb = sreg.SubReg(4, 4);

                            var actdreg = dreg;
                            if (dreg is ContentsReg)
                                dreg = r_edx;

                            switch (to_type)
                            {
                                case 0x04:
                                    // i1
                                    if (dreg.Equals(r_edi) || dreg.Equals(r_esi))
                                        dreg = r_edx;
                                    handle_move(dreg, srca, r, n, c);
                                    r.Add(inst(x86_movsxbd, dreg, dreg, n));
                                    break;
                                case 0x05:
                                    // u1
                                    if (!srca.Equals(dreg))
                                        handle_move(dreg, srca, r, n, c);
                                    r.Add(inst(x86_movzxbd, dreg, dreg, n));
                                    break;
                                case 0x06:
                                    // i2
                                    handle_move(dreg, srca, r, n, c);
                                    r.Add(inst(x86_movsxwd, dreg, dreg, n));
                                    break;
                                case 0x07:
                                    // u2
                                    handle_move(dreg, srca, r, n, c);
                                    r.Add(inst(x86_movzxwd, dreg, dreg, n));
                                    break;
                                case 0x08:
                                case 0x09:
                                case 0x18:
                                case 0x19:
                                    if (!srca.Equals(dreg))
                                        handle_move(dreg, srca, r, n, c);
                                    // nop - ignore high 32 bits
                                    return r;
                                case 0x0a:
                                    // conv to i8
                                    throw new NotImplementedException();
                                    break;
                                case 0x0c:
                                case 0x0d:
                                    // conv to float
                                    if (is_un)
                                        fc_override = "__floatuntidf";
                                    else
                                        fc_override = "__floattidf";
                                    break;
                                default:
                                    throw new NotImplementedException("Convert to " + to_type.ToString());
                            }

                            if (!dreg.Equals(actdreg))
                                r.Add(inst(x86_mov_rm32_r32, actdreg, dreg, n));

                            return r;
                        }
                        else if (n_ct == ir.Opcode.ct_float)
                        {
                            var r = new List<MCInst>();

                            var si = n.stack_before.Peek(n.arg_a);
                            var di = n.stack_after.Peek(n.res_a);
                            var to_type = di.ts.SimpleType;

                            var sreg = si.reg;
                            var dreg = di.reg;
                            var act_dreg = dreg;

                            switch (to_type)
                            {
                                case 0x04:
                                    // int8
                                    if (dreg is ContentsReg)
                                        act_dreg = r_eax;
                                    r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                                    r.Add(inst(x86_movsxbd, act_dreg, act_dreg, n));
                                    handle_move(dreg, act_dreg, r, n, c);
                                    return r;
                                case 0x05:
                                    // uint8
                                    if (dreg is ContentsReg)
                                        act_dreg = r_eax;
                                    r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                                    r.Add(inst(x86_movzxbd, act_dreg, act_dreg, n));
                                    handle_move(dreg, act_dreg, r, n, c);
                                    return r;
                                case 0x06:
                                    // int16
                                    if (dreg is ContentsReg)
                                        act_dreg = r_eax;
                                    r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                                    r.Add(inst(x86_movsxwd, act_dreg, act_dreg, n));
                                    handle_move(dreg, act_dreg, r, n, c);
                                    return r;
                                case 0x07:
                                    // uint16
                                    if (dreg is ContentsReg)
                                        act_dreg = r_eax;
                                    r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                                    r.Add(inst(x86_movzxwd, act_dreg, act_dreg, n));
                                    handle_move(dreg, act_dreg, r, n, c);
                                    return r;
                                case 0x08:
                                    // int32
                                    if (dreg is ContentsReg)
                                        act_dreg = r_eax;
                                    r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                                    handle_move(dreg, act_dreg, r, n, c);
                                    return r;
                                case 0x09:
                                    // uint32
                                    fc_override = "__fixunsdfsi";
                                    break;
                                case 0x0a:
                                    // int64
                                    fc_override = "__fixdfti";
                                    break;
                                case 0x0b:
                                    // uint64
                                    fc_override = "__fixunsdfti";
                                    break;
                                case 0x0c:
                                case 0x0d:
                                    // float to float (all floats are 64-bit internally)
                                    if (!dreg.Equals(sreg))
                                        handle_move(dreg, sreg, r, n, c);
                                    return r;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        break;

                    case ir.Opcode.oc_stind:
                        if (n_ct2 == ir.Opcode.ct_int32)
                        {
                            var addr = n.stack_before.Peek(n.arg_a).reg;
                            var val = n.stack_before.Peek(n.arg_b).reg;

                            List<MCInst> r = new List<MCInst>();
                            if (addr is ContentsReg)
                            {
                                r.Add(inst(x86_mov_r32_rm32, r_eax, addr, n));
                                addr = r_eax;
                            }
                            if (val is ContentsReg || ((val.Equals(r_esi) || val.Equals(r_edi) && n.vt_size != 4)))
                            {
                                r.Add(inst(x86_mov_r32_rm32, r_edx, val, n));
                                val = r_edx;
                            }

                            switch (n.vt_size)
                            {
                                case 1:
                                    r.Add(inst(x86_mov_rm8disp_r32, addr, 0, val, n));
                                    break;
                                case 2:
                                    r.Add(inst(x86_mov_rm16disp_r32, addr, 0, val, n));
                                    break;
                                case 4:
                                    r.Add(inst(x86_mov_rm32disp_r32, addr, 0, val, n));
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }

                            return r;
                        }
                        else if (n_ct == ir.Opcode.ct_int64)
                        {
                            var addr = n.stack_before.Peek(n.arg_a).reg;
                            var val = n.stack_before.Peek(n.arg_b).reg;

                            var dr = val as DoubleReg;

                            List<MCInst> r = new List<MCInst>();
                            if (addr is ContentsReg)
                            {
                                r.Add(inst(x86_mov_r32_rm32, r_eax, addr, n));
                                addr = r_eax;
                            }

                            handle_stind(dr.a, addr, 0, 4, r, n);
                            handle_stind(dr.b, addr, 4, 4, r, n);
                            return r;
                        }

                        break;

                    case ir.Opcode.oc_ldind:
                        if (n_ct == ir.Opcode.ct_int32)
                        {
                            var addr = n.stack_before.Peek(n.arg_a).reg;
                            var val = n.stack_after.Peek(n.res_a).reg;

                            List<MCInst> r = new List<MCInst>();
                            handle_ldind(val, addr, 0, n.vt_size, r, n);

                            return r;
                        }
                        else if (n_ct == ir.Opcode.ct_int64)
                        {
                            var addr = n.stack_before.Peek(n.arg_a).reg;
                            var val = n.stack_after.Peek(n.res_a).reg;

                            List<MCInst> r = new List<MCInst>();

                            var dr = val as DoubleReg;

                            /* Do this here so its only done once.  We don't need the [esi]/[edi]
                             * check as the vt_size is guaranteed to be 4.  However, the address
                             * used in the second iteration may overwrite the return value in
                             * the first, so we need to check for that.  We do it backwards
                             * so that this is less likely (e.g. stack goes from
                             * ..., esi to ..., esi, edi thus assigning first to edi won't
                             * cause a problem */
                            if (addr is ContentsReg || addr.Equals(dr.b))
                            {
                                r.Add(inst(x86_mov_r32_rm32, r_eax, addr, n));
                                addr = r_eax;
                            }

                            handle_ldind(dr.b, addr, 4, 4, r, n);
                            handle_ldind(dr.a, addr, 0, 4, r, n);

                            return r;
                        }
                        break;


                    case ir.Opcode.oc_br:
                        return new List<MCInst> { inst_jmp(x86_jmp_rel32, (int)n.imm_l, n) };

                    case ir.Opcode.oc_ldlabaddr:
                        if (n_ct == ir.Opcode.ct_int32)
                        {
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            List<MCInst> r = new List<MCInst>();
                            if (dest is ContentsReg)
                            {
                                r.Add(inst(x86_mov_rm32_imm32, r_eax, new ir.Param { t = ir.Opcode.vl_str, str = n.imm_lab, v = n.imm_l }, n));
                                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
                            }
                            else
                                r.Add(inst(x86_mov_rm32_imm32, dest, new ir.Param { t = ir.Opcode.vl_str, str = n.imm_lab, v = n.imm_l }, n));
                            return r;
                        }
                        break;

                    case ir.Opcode.oc_call:
                        {
                            var r = handle_call(n, c, false);
                            if (r != null)
                                return r;
                        }
                        return null;

                    case ir.Opcode.oc_calli:
                        {
                            var r = handle_call(n, c, true);
                            if (r != null)
                                return r;
                        }
                        return null;

                    case ir.Opcode.oc_ret:
                        return handle_ret(n, c);

                    case ir.Opcode.oc_enter:
                        {
                            List<MCInst> r = new List<MCInst>();

                            var lv_size = c.lv_total_size;

                            if (ir.Opcode.GetCTFromType(c.ret_ts) == ir.Opcode.ct_vt)
                            {
                                r.Add(inst(x86_pop_r32, r_eax, n));
                                r.Add(inst(x86_xchg_r32_rm32, r_eax, new ContentsReg { basereg = r_esp, disp = 0, size = 4 }, n));
                                lv_size += 4;
                            }

                            r.Add(inst(x86_push_r32, r_ebp, n));
                            r.Add(inst(x86_mov_r32_rm32, r_ebp, r_esp, n));
                            if (lv_size != 0)
                            {
                                if (lv_size <= 127)
                                    r.Add(inst(x86_sub_rm32_imm8, r_esp, lv_size, n));
                                else
                                    r.Add(inst(x86_sub_rm32_imm32, r_esp, lv_size, n));
                            }

                            var regs_to_save = c.regs_used | c.t.cc_callee_preserves_map["sysv"];
                            var regs_set = new util.Set();
                            regs_set.Union(regs_to_save);
                            int x = 0;
                            while (regs_set.Empty == false)
                            {
                                var reg = regs_set.get_first_set();
                                regs_set.unset(reg);
                                var cur_reg = regs[reg];
                                handle_push(cur_reg, ref x, r, n, c);
                                c.regs_saved.Add(cur_reg);
                            }

                            if (ir.Opcode.GetCTFromType(c.ret_ts) == ir.Opcode.ct_vt)
                                r.Add(inst(x86_mov_rm32_r32, new ContentsReg { basereg = r_ebp, disp = -4, size = 4 }, r_eax, n));

                            return r;
                        }

                    case ir.Opcode.oc_enter_handler:
                        {
                            var r = new List<MCInst>();
                            r.Add(inst(Generic.g_label, c.ms.MangleMethod() + "EH" + n.imm_l.ToString(), n));
                            return r;
                        }

                    case ir.Opcode.oc_mul:
                        if (n_ct == ir.Opcode.ct_int32)
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            if (srca.Equals(dest) && !(srca is ContentsReg))
                            {
                                return new List<MCInst>
                                {
                                    inst(x86_imul_r32_rm32, dest, srcb, n)
                                };
                            }
                            else
                            {
                                var r = new List<MCInst>();
                                handle_move(r_eax, srca, r, n, c);
                                r.Add(inst(x86_imul_r32_rm32, r_eax, srcb, n));
                                handle_move(dest, r_eax, r, n, c);
                                return r;
                            }
                        }
                        else if (n_ct == ir.Opcode.ct_float)
                        {
                            var srca = n.stack_before.Peek(n.arg_a).reg;
                            var srcb = n.stack_before.Peek(n.arg_b).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            if (srca.Equals(dest) && !(srca is ContentsReg))
                            {
                                return new List<MCInst>
                                {
                                    inst(x86_mulsd_xmm_xmmm64, dest, srcb, n)
                                };
                            }
                            else
                            {
                                var r = new List<MCInst>();
                                handle_move(r_xmm7, srca, r, n, c);
                                r.Add(inst(x86_mulsd_xmm_xmmm64, r_xmm7, srcb, n));
                                handle_move(dest, r_xmm7, r, n, c);
                                return r;
                            }
                        }
                        else if (n_ct == ir.Opcode.ct_int64)
                        {
                            fc_override = "__multi3";
                            break;
                        }
                        break;

                    case ir.Opcode.oc_localloc:
                        {
                            var src = n.stack_before.Peek(n.arg_a).reg;
                            var dest = n.stack_after.Peek(n.res_a).reg;

                            return new List<MCInst>
                            {
                                inst(x86_sub_r32_rm32, r_esp, r_esp, src, n),
                                inst(x86_and_rm32_imm8, r_esp, new ir.Param { t = ir.Opcode.vl_c32, v = 0xfffffff0 }, n),
                                inst(x86_mov_rm32_r32, dest, r_esp, n),
                            };
                        }
                }

                // emit as a call
                StringBuilder sb = new StringBuilder();
                if (fc_override == null)
                {
                    sb.Append(ir.Opcode.oc_names[n.opcode]);
                    extern_append(sb, n.ctret);
                    extern_append(sb, n.ct);
                    extern_append(sb, n.ct2);
                    if (n.ct == ir.Opcode.ct_vt &&
                        n.vt_size != 0)
                        sb.Append(n.vt_size.ToString());
                }
                else
                    sb.Append(fc_override);

                var rts = ir.Opcode.GetTypeFromCT(n.ctret, c.ms.m);
                var ats = ir.Opcode.GetTypeFromCT(n.ct, c.ms.m);
                var bts = ir.Opcode.GetTypeFromCT(n.ct2, c.ms.m);

                metadata.TypeSpec[] p;
                if (ats != null && bts != null)
                    p = new metadata.TypeSpec[] { ats, bts };
                else if (ats != null)
                    p = new metadata.TypeSpec[] { ats };
                else
                    p = new metadata.TypeSpec[] { };

                var msig = c.special_meths.CreateMethodSignature(rts, p);
                n.imm_ms = new metadata.MethodSpec { mangle_override = sb.ToString(), m = c.special_meths, msig = msig };

                return handle_call(n, c, false);
            }
            else if (count == 2)
            {
                var n1 = nodes[start];
                var n2 = nodes[start + 1];

                if (n1.opcode == ir.Opcode.oc_ldc &&
                    n2.opcode == ir.Opcode.oc_stloc)
                {
                    if (n1.ct == ir.Opcode.ct_int32)
                        return new List<MCInst> { inst(x86_mov_rm32_imm32, c.lv_locs[(int)n2.imm_l], (int)n1.imm_l, n1) };
                }
                else if (n1.opcode == ir.Opcode.oc_stloc &&
                    n2.opcode == ir.Opcode.oc_ldloc)
                {
                    if (n1.ct == n2.ct &&
                        n1.vt_size == n2.vt_size &&
                        n1.imm_l == n2.imm_l)
                    {
                        return ChooseInstruction(nodes, start, 1, c);
                    }
                }
                else if (n1.opcode == ir.Opcode.oc_ldlabaddr &&
                    n2.opcode == ir.Opcode.oc_ldind)
                {
                    if (n2.vt_size <= 4)
                    {
                        List<MCInst> r = new List<MCInst>();

                        var dest = n2.stack_after.Peek(n1.res_a).reg;
                        var src = new ir.Param { t = ir.Opcode.vl_str, str = n1.imm_lab, v = n1.imm_l };
                        var actdest = dest;

                        if (dest is ContentsReg)
                            dest = r_edx;

                        switch (n2.vt_size)
                        {
                            case 1:
                                if (dest.Equals(r_edi) || dest.Equals(r_esi))
                                    dest = r_edx;
                                r.Add(inst(x86_mov_r8_rm8, dest, src, n1));
                                break;
                            case 2:
                                if (dest.Equals(r_edi) || dest.Equals(r_esi))
                                    dest = r_edx;
                                r.Add(inst(x86_mov_r16_rm16, dest, src, n1));
                                break;
                            case 4:
                                r.Add(inst(x86_mov_r32_rm32, dest, src, n1));
                                break;
                            default:
                                return null;
                        }

                        if (!dest.Equals(actdest))
                            r.Add(inst(x86_mov_rm32_r32, actdest, dest, n1));

                        return r;

                    }
                }
            }
            else if (count == 3)
            {
                var n1 = nodes[start];
                var n2 = nodes[start + 1];
                var n3 = nodes[start + 2];

                if (n1.opcode == ir.Opcode.oc_ldc &&
                    n2.opcode == ir.Opcode.oc_add &&
                    n3.opcode == ir.Opcode.oc_ldind)
                {
                    if (n3.vt_size <= 4)
                    {
                        List<MCInst> r = new List<MCInst>();
                        var src = n1.stack_before.Peek(n1.arg_a).reg;
                        if (src is ContentsReg)
                        {
                            r.Add(inst(x86_mov_r32_rm32, r_eax, src, n1));
                            src = r_eax;
                        }

                        var dest = n3.stack_after.Peek(n1.res_a).reg;

                        var actdest = dest;
                        if (dest is ContentsReg)
                            dest = r_edx;

                        switch (n3.vt_size)
                        {
                            case 1:
                                if (dest.Equals(r_edi) || dest.Equals(r_esi))
                                    dest = r_edx;
                                r.Add(inst(x86_mov_r32_rm8disp, dest, src, n1.imm_l, n1));
                                break;
                            case 2:
                                if (dest.Equals(r_edi) || dest.Equals(r_esi))
                                    dest = r_edx;
                                r.Add(inst(x86_mov_r32_rm16disp, dest, src, n1.imm_l, n1));
                                break;
                            case 4:
                                r.Add(inst(x86_mov_r32_rm32disp, dest, src, n1.imm_l, n1));
                                break;
                            default:
                                return null;
                        }

                        if (!dest.Equals(actdest))
                            r.Add(inst(x86_mov_rm32_r32, actdest, dest, n1));

                        return r;
                    }
                }
            }

            return null;
        }

        private static void handle_briff(Reg srca, Reg srcb, int cc, int target, List<MCInst> r, CilNode.IRNode n)
        {
            if (!(srca is ContentsReg))
                r.Add(inst(x86_comisd_xmm_xmmm64, srca, srcb, n));
            else if (!(srcb is ContentsReg))
            {
                r.Add(inst(x86_comisd_xmm_xmmm64, srcb, srca, n));
                cc = ir.Opcode.cc_invert_map[cc];
            }
            else
            {
                r.Add(inst(x86_movsd_xmm_xmmm64, r_xmm7, srca, n));
                r.Add(inst(x86_comisd_xmm_xmmm64, srca, srcb, n));
            }
            r.Add(inst_jmp(x86_jcc_rel32, target, cc, n));
        }

        private void extern_append(StringBuilder sb, int ct)
        {
            switch (ct)
            {
                case ir.Opcode.ct_int32:
                case ir.Opcode.ct_object:
                case ir.Opcode.ct_ref:
                case ir.Opcode.ct_intptr:
                    sb.Append("i");
                    break;
                case ir.Opcode.ct_int64:
                    sb.Append("l");
                    break;
                case ir.Opcode.ct_float:
                    sb.Append("f");
                    break;
            }
        }

        private static void handle_stind(Reg val, Reg addr, int disp, int vt_size, List<MCInst> r, CilNode.IRNode n)
        {
            if (addr is ContentsReg || ((addr.Equals(r_esi) || addr.Equals(r_edi)) && vt_size != 4))
            {
                r.Add(inst(x86_mov_r32_rm32, r_eax, addr, n));
                addr = r_eax;
            }

            var act_val = val;

            if (val is ContentsReg || ((val.Equals(r_esi) || val.Equals(r_edi)) && n.vt_size == 1))
            {
                val = r_edx;
                r.Add(inst(x86_mov_r32_rm32, val, act_val, n));
            }

            switch (vt_size)
            {
                case 1:
                    r.Add(inst(x86_mov_rm8disp_r32, addr, disp, val, n));
                    break;
                case 2:
                    r.Add(inst(x86_mov_rm16disp_r32, addr, disp, val, n));
                    break;
                case 4:
                    r.Add(inst(x86_mov_rm32disp_r32, addr, disp, val, n));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void handle_brifi32(Reg srca, Reg srcb, int cc, int target, List<MCInst> r, CilNode.IRNode n)
        {
            if (!(srca is ContentsReg))
                r.Add(inst(x86_cmp_r32_rm32, srca, srcb, n));
            else if (!(srcb is ContentsReg))
                r.Add(inst(x86_cmp_rm32_r32, srca, srcb, n));
            else
            {
                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                r.Add(inst(x86_cmp_r32_rm32, r_eax, srcb, n));
            }
            r.Add(inst_jmp(x86_jcc_rel32, target, cc, n));
        }

        private static void handle_brifi32(Reg srca, int v, int cc, int target, List<MCInst> r, CilNode.IRNode n)
        {
            if (v >= -128 && v < 127)
                r.Add(inst(x86_cmp_rm32_imm8, srca, v, n));
            else
                r.Add(inst(x86_cmp_rm32_imm32, srca, v, n));
            r.Add(inst_jmp(x86_jcc_rel32, target, cc, n));
        }

        private static void handle_move(Reg dest, Reg src, List<MCInst> r, CilNode.IRNode n, Code c, Reg temp_reg = null)
        {
            if (dest.Equals(src))
                return;
            if (src is ContentsReg && dest is ContentsReg)
            {
                var crs = src as ContentsReg;
                var crd = dest as ContentsReg;

                var vt_size = crs.size;
                vt_size = util.util.align(vt_size, 4);
                if (vt_size != crd.size)
                    throw new Exception("Differing size in move");

                if (temp_reg == null)
                {
                    if (crs.basereg.Equals(r_eax) || crd.basereg.Equals(r_eax))
                        temp_reg = r_edx;
                    else
                        temp_reg = r_eax;
                }

                if (vt_size > 16)
                {
                    // emit call to memcpy(dest, src, n)
                    r.AddRange(handle_call(n, c,
                        c.special_meths.GetMethodSpec(c.special_meths.memcpy),
                        new ir.Param[]
                        {
                            new ir.Param { t = ir.Opcode.vl_stack, mreg = dest },
                            new ir.Param { t = ir.Opcode.vl_stack, mreg = src },
                            vt_size
                        },
                        null, "memcpy", temp_reg));
                }
                else
                {
                    for (int i = 0; i < vt_size; i += 4)
                    {
                        var new_crs = new ContentsReg { basereg = crs.basereg, disp = crs.disp + i, size = 4 };
                        var new_crd = new ContentsReg { basereg = crd.basereg, disp = crd.disp + i, size = 4 };

                        // first store to rax
                        r.Add(inst(x86_mov_r32_rm32, temp_reg, new_crs, n));
                        r.Add(inst(x86_mov_rm32_r32, new_crd, temp_reg, n));
                    }
                }
            }
            else if (src is ContentsReg)
            {
                if (dest is DoubleReg)
                {
                    if (src.size != 8)
                        throw new Exception("invalid contentsreg size");

                    var drd = dest as DoubleReg;
                    var crs = src as ContentsReg;
                    var crsa = new ContentsReg { basereg = crs.basereg, disp = crs.disp, size = 4 };
                    var crsb = new ContentsReg { basereg = crs.basereg, disp = crs.disp + 4, size = 4 };
                    handle_move(drd.a, crsa, r, n, c, temp_reg);
                    handle_move(drd.b, crsb, r, n, c, temp_reg);
                }
                else
                {
                    if (dest.type == rt_gpr)
                        r.Add(inst(x86_mov_r32_rm32, dest, src, n));
                    else if (dest.type == rt_float)
                        r.Add(inst(x86_movsd_xmm_xmmm64, dest, src, n));
                    else
                        throw new NotImplementedException();
                }
            }
            else
            {
                if (src is DoubleReg)
                {
                    if (dest.size != 8)
                        throw new Exception("invalid contentsreg size");

                    var drs = src as DoubleReg;
                    var da = dest.SubReg(0, 4);
                    var db = dest.SubReg(4, 4);
                    handle_move(da, drs.a, r, n, c, temp_reg);
                    handle_move(db, drs.b, r, n, c, temp_reg);
                }
                else
                {
                    if (src.type == rt_gpr)
                        r.Add(inst(x86_mov_rm32_r32, dest, src, n));
                    else if (src.type == rt_float)
                        r.Add(inst(x86_movsd_xmmm64_xmm, dest, src, n));
                    else
                        throw new NotImplementedException();
                }
            }
        }

        private static void handle_sub(Reg srca, Reg srcb, Reg dest, List<MCInst> r, CilNode.IRNode n, bool with_borrow = false)
        {
            if (!(srca is ContentsReg) && srca.Equals(dest))
            {
                if (with_borrow)
                    r.Add(inst(x86_sbb_r32_rm32, srca, srcb, n));
                else
                    r.Add(inst(x86_sub_r32_rm32, srca, srcb, n));
            }
            else if (!(srcb is ContentsReg) && srca.Equals(dest))
            {
                if (with_borrow)
                    r.Add(inst(x86_sbb_rm32_r32, srca, srcb, n));
                else
                    r.Add(inst(x86_sub_rm32_r32, srca, srcb, n));
            }
            else
            {
                // complex way, do calc in rax, then store
                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                if (with_borrow)
                    r.Add(inst(x86_sbb_r32_rm32, r_eax, srcb, n));
                else
                    r.Add(inst(x86_sub_r32_rm32, r_eax, srcb, n));
                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
            }
        }

        private static void handle_add(Reg srca, Reg srcb, Reg dest, List<MCInst> r, CilNode.IRNode n, bool with_carry = false)
        {
            if (!(srca is ContentsReg) && srca.Equals(dest))
            {
                if (with_carry)
                    r.Add(inst(x86_adc_r32_rm32, srca, srcb, n));
                else
                    r.Add(inst(x86_add_r32_rm32, srca, srcb, n));
            }
            else if (!(srcb is ContentsReg) && srca.Equals(dest))
            {
                if (with_carry)
                    r.Add(inst(x86_adc_rm32_r32, srca, srcb, n));
                else
                    r.Add(inst(x86_add_rm32_r32, srca, srcb, n));
            }
            else
            {
                // complex way, do calc in rax, then store
                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                if (with_carry)
                    r.Add(inst(x86_adc_r32_rm32, r_eax, srcb, n));
                else
                    r.Add(inst(x86_add_r32_rm32, r_eax, srcb, n));
                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
            }
        }

        private static void handle_and(Reg srca, Reg srcb, Reg dest, List<MCInst> r, CilNode.IRNode n)
        {
            if (!(srca is ContentsReg) && srca.Equals(dest))
            {
                r.Add(inst(x86_and_r32_rm32, srca, srcb, n));
            }
            else if (!(srcb is ContentsReg) && srca.Equals(dest))
            {
                r.Add(inst(x86_and_rm32_r32, srca, srcb, n));
            }
            else
            {
                // complex way, do calc in rax, then store
                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                r.Add(inst(x86_and_r32_rm32, r_eax, srcb, n));
                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
            }
        }

        private static void handle_or(Reg srca, Reg srcb, Reg dest, List<MCInst> r, CilNode.IRNode n)
        {
            if (!(srca is ContentsReg) && srca.Equals(dest))
            {
                r.Add(inst(x86_or_r32_rm32, srca, srcb, n));
            }
            else if (!(srcb is ContentsReg) && srca.Equals(dest))
            {
                r.Add(inst(x86_or_rm32_r32, srca, srcb, n));
            }
            else
            {
                // complex way, do calc in rax, then store
                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                r.Add(inst(x86_or_r32_rm32, r_eax, srcb, n));
                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
            }
        }

        private static void handle_xor(Reg srca, Reg srcb, Reg dest, List<MCInst> r, CilNode.IRNode n)
        {
            if (!(srca is ContentsReg) && srca.Equals(dest))
            {
                r.Add(inst(x86_xor_r32_rm32, srca, srcb, n));
            }
            else if (!(srcb is ContentsReg) && srca.Equals(dest))
            {
                r.Add(inst(x86_xor_rm32_r32, srca, srcb, n));
            }
            else
            {
                // complex way, do calc in rax, then store
                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                r.Add(inst(x86_xor_r32_rm32, r_eax, srcb, n));
                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
            }
        }

        private static void handle_ldind(Reg val, Reg addr, int disp, int vt_size, List<MCInst> r, CilNode.IRNode n)
        {
            if (addr is ContentsReg)
            {
                r.Add(inst(x86_mov_r32_rm32, r_eax, addr, n));
                addr = r_eax;
            }

            var act_val = val;

            if (val is ContentsReg)
            {
                val = r_edx;
            }

            switch (vt_size)
            {
                case 1:
                    if (n.imm_l == 1)
                        r.Add(inst(x86_movsxbd_r32_rm8disp, val, addr, disp, n));
                    else
                        r.Add(inst(x86_movzxbd_r32_rm8disp, val, addr, disp, n));
                    break;
                case 2:
                    if (n.imm_l == 1)
                        r.Add(inst(x86_movsxwd_r32_rm16disp, val, addr, disp, n));
                    else
                        r.Add(inst(x86_movzxwd_r32_rm16disp, val, addr, disp, n));
                    break;
                case 4:
                    r.Add(inst(x86_mov_r32_rm32disp, val, addr, disp, n));
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (!val.Equals(act_val))
            {
                r.Add(inst(x86_mov_rm32_r32, act_val, val, n));
            }
        }

        private static List<MCInst> handle_ret(CilNode.IRNode n, Code c)
        {
            List<MCInst> r = new List<MCInst>();

            if (n.stack_before.Count == 1)
            {
                var reg = n.stack_before.Peek().reg;

                switch (n.ct)
                {
                    case ir.Opcode.ct_int32:
                    case ir.Opcode.ct_intptr:
                    case ir.Opcode.ct_object:
                    case ir.Opcode.ct_ref:
                        r.Add(inst(x86_mov_r32_rm32, r_eax, n.stack_before[0].reg, n));
                        break;

                    case ir.Opcode.ct_int64:
                        var dr = reg as DoubleReg;
                        r.Add(inst(x86_mov_r32_rm32, r_eax, dr.a, n));
                        r.Add(inst(x86_mov_r32_rm32, r_edx, dr.b, n));
                        break;

                    case ir.Opcode.ct_vt:
                        // move address to save to to eax
                        handle_move(r_eax, new ContentsReg { basereg = r_ebp, disp = -4, size = 4 },
                            r, n, c);

                        // move struct to [eax]
                        var vt_size = c.t.GetSize(c.ret_ts);
                        handle_move(new ContentsReg { basereg = r_eax, size = vt_size },
                            reg, r, n, c);

                        break;

                    case ir.Opcode.ct_float:
                        handle_move(r_xmm0, reg, r, n, c);
                        break;

                    default:
                        throw new NotImplementedException(ir.Opcode.ct_names[n.ct]);
                }
            }

            // Restore used regs
            int x = 0;
            for (int i = c.regs_saved.Count - 1; i >= 0; i--)
                handle_pop(c.regs_saved[i], ref x, r, n, c);

            r.Add(inst(x86_mov_r32_rm32, r_esp, r_ebp, n));
            r.Add(inst(x86_pop_r32, r_ebp, n));
            r.Add(inst(x86_ret, n));

            return r;
        }

        static List<Reg> get_push_list(CilNode.IRNode n, Code c,
            metadata.MethodSpec call_ms, out metadata.TypeSpec rt,
            out Reg dest, out int rct, bool want_return = true)
        {
            /* Determine which registers we need to save */
            var caller_preserves = c.t.cc_caller_preserves_map["sysv"];
            ulong defined = 0;
            foreach (var si in n.stack_after)
                defined |= si.reg.mask;

            var rt_idx = call_ms.m.GetMethodDefSigRetTypeIndex(call_ms.msig);
            rt = call_ms.m.GetTypeSpec(ref rt_idx, call_ms.gtparams, call_ms.gmparams);
            dest = null;
            if (rt != null && want_return)
            {
                defined &= ~n.stack_after.Peek().reg.mask;
                dest = n.stack_after.Peek().reg;
            }
            rct = ir.Opcode.GetCTFromType(rt);

            var to_push = new util.Set();
            to_push.Union(defined);
            to_push.Intersect(caller_preserves);
            List<Reg> push_list = new List<Reg>();
            while (!to_push.Empty)
            {
                var first_set = to_push.get_first_set();
                push_list.Add(c.t.regs[first_set]);
                to_push.unset(first_set);
            }

            return push_list;
        }

        private static List<MCInst> handle_call(CilNode.IRNode n,
            Code c, metadata.MethodSpec call_ms, ir.Param[] p,
            Reg dest, string target = null, Reg temp_reg = null)
        {
            /* used for handling calls to utility functions
             *  (e.g. memcpy/memset etc) whilst ensuring that
             *  all required registers are saved around the
             *  call */

            if (target == null)
                target = call_ms.MangleMethod();
            if (temp_reg == null)
                temp_reg = r_eax;

            Reg act_dest = null;
            metadata.TypeSpec rt;
            int rct;
            var push_list = get_push_list(n, c, call_ms,
                out rt, out act_dest, out rct, dest != null);

            List<MCInst> r = new List<MCInst>();

            // Save clobbered registers
            int x = 0;
            foreach (var push_reg in push_list)
                handle_push(push_reg, ref x, r, n, c);


            // Push arguments - only handles int32s
            x = 0;
            for(int i = p.Length - 1; i >= 0; i--)
            {
                var arg = p[i];
                if(arg.t == ir.Opcode.vl_mreg)
                {
                    if (arg.mreg is ContentsReg)
                    {
                        r.Add(inst(x86_push_rm32, arg.mreg, n));
                        x += 4;
                    }
                    else if (arg.mreg is DoubleReg)
                    {
                        r.Add(inst(x86_push_rm32, arg.mreg.SubReg(4, 4), n));
                        r.Add(inst(x86_push_rm32, arg.mreg.SubReg(0, 4), n));
                        x += 8;
                    }
                    else
                    {
                        r.Add(inst(x86_push_r32, arg.mreg, n));
                        x += 4;
                    }
                }
                else if(arg.t == ir.Opcode.vl_stack)
                {
                    // interpret this as lea eax, mreg; push eax;
                    r.Add(inst(x86_lea_r32, temp_reg, arg.mreg, n));
                    r.Add(inst(x86_push_r32, temp_reg, n));
                }
                else
                {
                    r.Add(inst(x86_push_imm32, arg.v, n));
                    x += 4;
                }
            }

            // emit call
            r.Add(inst(x86_call_rel32, new ir.Param { t = ir.Opcode.vl_call_target, str = "memcpy" }, n));

            // restore stack
            if(x != 0)
                r.Add(inst(x <= 127 ? x86_add_rm32_imm8 : x86_add_rm32_imm32, r_esp, x, n));

            // restore saved registers
            for (int i = push_list.Count - 1; i >= 0; i--)
                handle_pop(push_list[i], ref x, r, n, c);

            // get return value
            if (act_dest != null && dest != null)
                handle_move(dest, act_dest, r, n, c);

            return r;
        }

        private static List<MCInst> handle_call(CilNode.IRNode n, Code c, bool is_calli)
        {
            List<MCInst> r = new List<MCInst>();
            var call_ms = n.imm_ms;
            var target = call_ms.m.MangleMethod(call_ms);

            Reg act_dest = null;
            Reg dest;
            metadata.TypeSpec rt;
            int rct;
            var push_list = get_push_list(n, c, call_ms,
                out rt, out dest, out rct);

            int x = 0;
            foreach (var push_reg in push_list)
                handle_push(push_reg, ref x, r, n, c);

            /* Push arguments */
            int push_length = 0;
            int vt_push_length = 0;
            bool vt_dest_adjust = false;

            if (rct == ir.Opcode.ct_vt)
            {
                if (dest is ContentsReg)
                {
                    act_dest = dest;
                }
                else
                {
                    var rsize = c.t.GetSize(rt);
                    rsize = util.util.align(rsize, 4);
                    vt_dest_adjust = true;

                    act_dest = new ContentsReg { basereg = r_esp, disp = 0, size = rsize };
                    r.Add(inst(rsize <= 127 ? x86_sub_rm32_imm8 : x86_sub_rm32_imm32, r_esp, rsize, n));
                    vt_push_length += rsize;
                }
            }

            var sig_idx = call_ms.msig;
            var pcount = call_ms.m.GetMethodDefSigParamCountIncludeThis(sig_idx);
            sig_idx = call_ms.m.GetMethodDefSigRetTypeIndex(sig_idx);
            var rt2 = call_ms.m.GetTypeSpec(ref sig_idx, c.ms.gtparams, c.ms.gmparams);

            int calli_adjust = is_calli ? 1 : 0;

            metadata.TypeSpec[] push_tss = new metadata.TypeSpec[pcount];
            for (int i = 0; i < pcount; i++)
            {
                if (i == 0 && call_ms.m.GetMethodDefSigHasNonExplicitThis(call_ms.msig))
                    push_tss[i] = call_ms.type;
                else
                    push_tss[i] = call_ms.m.GetTypeSpec(ref sig_idx, call_ms.gtparams, call_ms.gmparams);
            }

            for (int i = 0; i < pcount; i++)
            {
                metadata.TypeSpec push_ts = push_tss[pcount - i - 1];
                var push_ct = ir.Opcode.GetCTFromType(push_ts);

                var stack_loc = i;
                if (n.arg_list != null)
                    stack_loc = n.arg_list[pcount - i - 1];

                var to_pass = n.stack_before.Peek(stack_loc + calli_adjust).reg;

                switch (push_ct)
                {
                    case ir.Opcode.ct_int32:
                    case ir.Opcode.ct_object:
                    case ir.Opcode.ct_ref:
                    case ir.Opcode.ct_intptr:
                    case ir.Opcode.ct_vt:
                    case ir.Opcode.ct_int64:
                    case ir.Opcode.ct_float:
                        handle_push(to_pass, ref push_length, r, n, c);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            /* Push value type address if required */
            if (rct == ir.Opcode.ct_vt)
            {
                if (vt_dest_adjust)
                {
                    var act_dest_cr = act_dest as ContentsReg;
                    act_dest_cr.disp += push_length;
                }

                r.Add(inst(x86_lea_r32, r_eax, act_dest, n));
                r.Add(inst(x86_push_r32, r_eax, n));
            }

            // Do the call
            if (is_calli)
            {
                var call_reg = n.stack_before.Peek().reg;
                /*if(call_reg is ContentsReg)
                {
                    handle_move(r_eax, call_reg, r, n);
                    call_reg = r_eax;
                }*/
                r.Add(inst(x86_call_rm32, call_reg, n));
            }
            else
                r.Add(inst(x86_call_rel32, new ir.Param { t = ir.Opcode.vl_call_target, str = target }, n));

            // Restore stack
            if (push_length != 0)
            {
                var add_oc = x86_add_rm32_imm32;
                if (push_length < 128)
                    add_oc = x86_add_rm32_imm8;
                r.Add(inst(add_oc, r_esp, push_length, n));
            }

            // Get vt return value
            if (rct == ir.Opcode.ct_vt && !act_dest.Equals(dest))
            {
                handle_move(dest, act_dest, r, n, c);
            }

            // Restore saved registers
            for (int i = push_list.Count - 1; i >= 0; i--)
                handle_pop(push_list[i], ref x, r, n, c);

            // Get other return value
            if (rt != null && rct != ir.Opcode.ct_vt)
            {
                var rt_size = c.t.GetSize(rt);
                if (rct == ir.Opcode.ct_float)
                {
                    if (!dest.Equals(r_xmm0))
                    {
                        r.Add(inst(x86_movsd_xmmm64_xmm, dest, r_xmm0, n));
                    }
                }
                else if (rt_size <= 4)
                    r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
                else if (rt_size == 8)
                {
                    var drd = dest as DoubleReg;
                    r.Add(inst(x86_mov_rm32_r32, drd.a, r_eax, n));
                    r.Add(inst(x86_mov_rm32_r32, drd.b, r_edx, n));
                }
                else
                    throw new NotImplementedException();
            }

            return r;
        }

        private static void handle_push(Reg reg, ref int push_length, List<MCInst> r, CilNode.IRNode n, Code c)
        {
            if (reg is ContentsReg)
            {
                ContentsReg cr = reg as ContentsReg;
                if (cr.size < 4)
                    throw new NotImplementedException();
                else if (cr.size == 4)
                    r.Add(inst(x86_push_rm32, reg, n));
                else
                {
                    var psize = util.util.align(cr.size, 4);
                    if (psize <= 127)
                        r.Add(inst(x86_sub_rm32_imm8, r_esp, psize, n));
                    else
                        r.Add(inst(x86_sub_rm32_imm32, r_esp, psize, n));
                    handle_move(new ContentsReg { basereg = r_esp, size = cr.size },
                        cr, r, n, c);
                }
                push_length += 4;
            }
            else if (reg is DoubleReg)
            {
                var dr = reg as DoubleReg;
                handle_push(dr.b, ref push_length, r, n, c);
                handle_push(dr.a, ref push_length, r, n, c);
            }
            else
            {
                if (reg.type == rt_gpr)
                {
                    r.Add(inst(x86_push_r32, reg, n));
                    push_length += 4;
                }
                else if (reg.type == rt_float)
                {
                    r.Add(inst(x86_sub_rm32_imm8, r_esp, 8, n));
                    r.Add(inst(x86_movsd_xmmm64_xmm, new ContentsReg { basereg = r_esp, size = 8 }, reg, n));
                    push_length += 8;
                }
            }
        }

        private static void handle_pop(Reg reg, ref int pop_length, List<MCInst> r, CilNode.IRNode n, Code c)
        {
            if (reg is ContentsReg)
            {
                ContentsReg cr = reg as ContentsReg;
                if (cr.size < 4)
                    throw new NotImplementedException();
                else if (cr.size == 4)
                    r.Add(inst(x86_pop_rm32, reg, n));
                else
                {
                    handle_move(cr, new ContentsReg { basereg = r_esp, size = cr.size },
                        r, n, c);
                    var psize = util.util.align(cr.size, 4);
                    if (psize <= 127)
                        r.Add(inst(x86_add_rm32_imm8, r_esp, psize, n));
                    else
                        r.Add(inst(x86_add_rm32_imm32, r_esp, psize, n));
                }
                pop_length += 4;
            }
            else if (reg is DoubleReg)
            {
                var dr = reg as DoubleReg;
                handle_pop(dr.b, ref pop_length, r, n, c);
                handle_pop(dr.a, ref pop_length, r, n, c);
            }
            else
            {
                if (reg.type == rt_gpr)
                {
                    r.Add(inst(x86_pop_r32, reg, n));
                    pop_length += 4;
                }
                else if (reg.type == rt_float)
                {
                    r.Add(inst(x86_movsd_xmm_xmmm64, reg, new ContentsReg { basereg = r_esp, size = 8 }, n));
                    r.Add(inst(x86_add_rm32_imm8, r_esp, 8, n));
                    pop_length += 8;
                }
            }
        }

        static private MCInst inst_jmp(int idx, int jmp_target, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    new ir.Param { t = ir.Opcode.vl_br_target, v = jmp_target },
                },
                parent = p
            };
        }

        static private MCInst inst_jmp(int idx, int jmp_target, int cc, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    new ir.Param { t = ir.Opcode.vl_cc, v = cc },
                    new ir.Param { t = ir.Opcode.vl_br_target, v = jmp_target }
                },
                parent = p
            };
        }

        static MCInst inst(int idx, ir.Param v1, ir.Param v2, ir.Param v3, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    v1,
                    v2,
                    v3
                },
                parent = p
            };
        }

        static MCInst inst(int idx, ir.Param v1, ir.Param v2, ir.Param v3, ir.Param v4, ir.Param v5, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    v1,
                    v2,
                    v3,
                    v4,
                    v5
                },
                parent = p
            };
        }


        static MCInst inst(int idx, ir.Param v1, ir.Param v2, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    v1,
                    v2
                },
                parent = p
            };
        }

        static MCInst inst(int idx, ir.Param v1, CilNode.IRNode p)
        {
            string str = null;
            insts.TryGetValue(idx, out str);
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = str },
                    v1
                },
                parent = p
            };
        }

        static MCInst inst(int idx, CilNode.IRNode p)
        {
            string str = null;
            insts.TryGetValue(idx, out str);
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = str },
                },
                parent = p
            };
        }

        internal static List<MCInst> handle_add(
            Target t,
            List<CilNode.IRNode> nodes,
            int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var srca = n.stack_before.Peek(n.arg_a).reg;
            var srcb = n.stack_before.Peek(n.arg_b).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;

            List<MCInst> r = new List<MCInst>();

            switch (n_ct)
            {
                case ir.Opcode.ct_int32:
                    handle_add(srca, srcb, dest, r, n);
                    return r;

                case ir.Opcode.ct_int64:
                    {
                        var srcaa = srca.SubReg(0, 4);
                        var srcab = srca.SubReg(4, 4);
                        var srcba = srcb.SubReg(0, 4);
                        var srcbb = srcb.SubReg(4, 4);
                        var desta = dest.SubReg(0, 4);
                        var destb = dest.SubReg(4, 4);
                        handle_add(srcaa, srcba, desta, r, n);
                        handle_add(srcab, srcbb, destb, r, n, true);
                        return r;
                    }

                case ir.Opcode.ct_float:
                    {
                        var actdest = dest;

                        if(dest is ContentsReg)
                            actdest = r_xmm7;

                        handle_move(actdest, srca, r, n, c);
                        r.Add(inst(x86_addsd_xmm_xmmm64,
                            actdest, srcb, n));
                        handle_move(dest, actdest, r, n, c);
                        return r;
                    }
            }
            return null;
        }

        internal static List<MCInst> handle_sub(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var srca = n.stack_before.Peek(n.arg_a).reg;
            var srcb = n.stack_before.Peek(n.arg_b).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;

            List<MCInst> r = new List<MCInst>();

            switch (n_ct)
            {
                case ir.Opcode.ct_int32:
                    handle_sub(srca, srcb, dest, r, n);
                    return r;

                case ir.Opcode.ct_int64:
                    {
                        var dra = srca as DoubleReg;
                        var drb = srcb as DoubleReg;
                        var drd = dest as DoubleReg;
                        handle_sub(dra.a, drb.a, drd.a, r, n);
                        handle_sub(dra.b, drb.b, drd.b, r, n, true);
                        return r;
                    }

                case ir.Opcode.ct_float:
                    {
                        var actdest = dest;

                        if (dest is ContentsReg)
                            actdest = r_xmm7;

                        handle_move(actdest, srca, r, n, c);
                        r.Add(inst(x86_subsd_xmm_xmmm64,
                            actdest, srcb, n));
                        handle_move(dest, actdest, r, n, c);
                        return r;
                    }
            }

            return null;
        }

        internal static List<MCInst> handle_mul(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            if (n_ct == ir.Opcode.ct_int32)
            {
                var srca = n.stack_before.Peek(n.arg_a).reg;
                var srcb = n.stack_before.Peek(n.arg_b).reg;
                var dest = n.stack_after.Peek(n.res_a).reg;

                if (srca.Equals(dest) && !(srca is ContentsReg))
                {
                    return new List<MCInst>
                    {
                        inst(x86_imul_r32_rm32, dest, srcb, n)
                    };
                }
                else
                {
                    var r = new List<MCInst>();
                    handle_move(r_eax, srca, r, n, c);
                    r.Add(inst(x86_imul_r32_rm32, r_eax, srcb, n));
                    handle_move(dest, r_eax, r, n, c);
                    return r;
                }
            }
            else if (n_ct == ir.Opcode.ct_float)
            {
                var srca = n.stack_before.Peek(n.arg_a).reg;
                var srcb = n.stack_before.Peek(n.arg_b).reg;
                var dest = n.stack_after.Peek(n.res_a).reg;

                if (srca.Equals(dest) && !(srca is ContentsReg))
                {
                    return new List<MCInst>
                    {
                        inst(x86_mulsd_xmm_xmmm64, dest, srcb, n)
                    };
                }
                else
                {
                    var r = new List<MCInst>();
                    handle_move(r_xmm7, srca, r, n, c);
                    r.Add(inst(x86_mulsd_xmm_xmmm64, r_xmm7, srcb, n));
                    handle_move(dest, r_xmm7, r, n, c);
                    return r;
                }
            }
            else if (n_ct == ir.Opcode.ct_int64)
            {
                return t.handle_external(t, nodes, start, count, c,
                    "__multi3");
            }
            return null;
        }

        internal static List<MCInst> handle_div(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var srca = n.stack_before.Peek(n.arg_a).reg;
            var srcb = n.stack_before.Peek(n.arg_b).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;
            var n_ct = n.ct;

            switch(n_ct)
            {
                case ir.Opcode.ct_int32:
                    {
                        List<MCInst> r = new List<MCInst>();
                        handle_move(r_eax, srca, r, n, c);
                        r.Add(inst(x86_xor_r32_rm32, r_edx, r_edx, n));
                        r.Add(inst(x86_idiv_rm32, srcb, n));
                        handle_move(dest, r_eax, r, n, c);
                        return r;
                    }

                case ir.Opcode.ct_float:
                    {
                        List<MCInst> r = new List<MCInst>();

                        if(!srca.Equals(dest) &&
                            !(dest is ContentsReg))
                        {
                            handle_move(dest, srca, r, n, c);
                            dest = srca;
                        }
                        var act_dest = dest;
                        if(dest is ContentsReg)
                        {
                            handle_move(r_xmm7, srca, r, n, c);
                            act_dest = r_xmm7;
                        }

                        r.Add(inst(x86_divsd_xmm_xmmm64, act_dest, srcb, n));
                        handle_move(dest, act_dest, r, n, c);

                        return r;
                    }

                case ir.Opcode.ct_int64:
                    return t.handle_external(t, nodes, start, count, c,
                    "__divti3");
            }

            throw new NotImplementedException();
        }

        internal static List<MCInst> handle_and(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var srca = n.stack_before.Peek(n.arg_a).reg;
            var srcb = n.stack_before.Peek(n.arg_b).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;

            List<MCInst> r = new List<MCInst>();

            switch (n_ct)
            {
                case ir.Opcode.ct_int32:
                    handle_and(srca, srcb, dest, r, n);
                    return r;

                case ir.Opcode.ct_int64:
                    {
                        var dra = srca as DoubleReg;
                        var drb = srcb as DoubleReg;
                        var drd = dest as DoubleReg;
                        handle_and(dra.a, drb.a, drd.a, r, n);
                        handle_and(dra.b, drb.b, drd.b, r, n);
                        return r;
                    }
            }
            return null;
        }

        internal static List<MCInst> handle_or(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var srca = n.stack_before.Peek(n.arg_a).reg;
            var srcb = n.stack_before.Peek(n.arg_b).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;

            List<MCInst> r = new List<MCInst>();

            switch (n_ct)
            {
                case ir.Opcode.ct_int32:
                    handle_or(srca, srcb, dest, r, n);
                    return r;

                case ir.Opcode.ct_int64:
                    {
                        var dra = srca as DoubleReg;
                        var drb = srcb as DoubleReg;
                        var drd = dest as DoubleReg;
                        handle_or(dra.a, drb.a, drd.a, r, n);
                        handle_or(dra.b, drb.b, drd.b, r, n);
                        return r;
                    }
            }
            return null;
        }

        internal static List<MCInst> handle_xor(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var srca = n.stack_before.Peek(n.arg_a).reg;
            var srcb = n.stack_before.Peek(n.arg_b).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;

            List<MCInst> r = new List<MCInst>();

            switch (n_ct)
            {
                case ir.Opcode.ct_int32:
                    handle_xor(srca, srcb, dest, r, n);
                    return r;

                case ir.Opcode.ct_int64:
                    {
                        var dra = srca as DoubleReg;
                        var drb = srcb as DoubleReg;
                        var drd = dest as DoubleReg;
                        handle_xor(dra.a, drb.a, drd.a, r, n);
                        handle_xor(dra.b, drb.b, drd.b, r, n);
                        return r;
                    }
            }
            return null;
        }

        internal static List<MCInst> handle_not(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var src = n.stack_before.Peek(n.arg_a).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;

            if (n_ct == ir.Opcode.ct_int32)
            {
                List<MCInst> r = new List<MCInst>();
                if (src != dest)
                    handle_move(dest, src, r, n, c);
                r.Add(inst(x86_not_rm32, dest, n));
                return r;
            }
            else if(n_ct == ir.Opcode.ct_int64)
            {
                var sa = src.SubReg(0, 4);
                var sb = src.SubReg(4, 4);
                var da = dest.SubReg(0, 4);
                var db = dest.SubReg(4, 4);

                List<MCInst> r = new List<MCInst>();
                if (!sa.Equals(da))
                    handle_move(da, sa, r, n, c);
                r.Add(inst(x86_not_rm32, da, n));
                if (!sb.Equals(db))
                    handle_move(db, sb, r, n, c);
                r.Add(inst(x86_not_rm32, db, n));
                return r;
            }
            throw new NotImplementedException();
        }

        internal static List<MCInst> handle_neg(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var src = n.stack_before.Peek(n.arg_a).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;

            if (n_ct == ir.Opcode.ct_int32)
            {
                List<MCInst> r = new List<MCInst>();
                if (src != dest)
                    handle_move(dest, src, r, n, c);
                r.Add(inst(x86_neg_rm32, dest, n));
                return r;
            }
            else if (n_ct == ir.Opcode.ct_int64)
                return t.handle_external(t, nodes, start, count,
                    c, "__negti3");
            throw new NotImplementedException();
        }

        internal static List<MCInst> handle_call(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            return handle_call(n, c, false);
        }

        internal static List<MCInst> handle_calli(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            return handle_call(n, c, true);
        }

        internal static List<MCInst> handle_ret(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            return handle_ret(n, c);
        }

        internal static List<MCInst> handle_cmp(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            if (n_ct == ir.Opcode.ct_int32)
            {
                var srca = n.stack_before.Peek(n.arg_a).reg;
                var srcb = n.stack_before.Peek(n.arg_b).reg;
                var dest = n.stack_after.Peek(n.res_a).reg;

                List<MCInst> r = new List<MCInst>();
                if (!(srca is ContentsReg))
                    r.Add(inst(x86_cmp_r32_rm32, srca, srcb, n));
                else if (!(srcb is ContentsReg))
                    r.Add(inst(x86_cmp_rm32_r32, srca, srcb, n));
                else
                {
                    r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                    r.Add(inst(x86_cmp_r32_rm32, r_eax, srcb, n));
                }

                r.Add(inst(x86_set_rm32, new ir.Param { t = ir.Opcode.vl_cc, v = (int)n.imm_ul }, r_eax, n));
                if (dest is ContentsReg)
                {
                    r.Add(inst(x86_movzxbd, r_eax, r_eax, n));
                    r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
                }
                else
                    r.Add(inst(x86_movzxbd, dest, r_eax, n));

                return r;
            }
            else if(n_ct == ir.Opcode.ct_int64)
            {
                var srca = n.stack_before.Peek(n.arg_a).reg;
                var srcb = n.stack_before.Peek(n.arg_b).reg;
                var dest = n.stack_after.Peek(n.res_a).reg;

                var sal = srca.SubReg(0, 4);
                var sah = srca.SubReg(4, 4);
                var sbl = srcb.SubReg(0, 4);
                var sbh = srcb.SubReg(4, 4);
                
                List<MCInst> r = new List<MCInst>();

                var cc = (int)n.imm_ul;

                /* For equal/not equal do a simple compare of
                 * low and high halves.
                 * 
                 * For other comparisons its a bit more complex:
                 * 
                 * cmp sah, sbh
                 * j1 L2            <- opposite, excluding equal of comparison
                 * j2 L1            <- original comparison, excluding equal
                 * cmp sal, sbl     <- this is only performed if sah == sbh
                 * j3 L2            <- opposite, including complement of equal, unsigned
                 * L1:              <- success path
                 * mov dest, 1
                 * jmp L3
                 * L2:              <- fail path
                 * mov dest, 0
                 * L3:              <- end
                 */

                var fail_path = c.next_mclabel--;
                var end_path = c.next_mclabel--;

                if (cc == ir.Opcode.cc_eq ||
                    cc == ir.Opcode.cc_ne)
                {
                    var other_cc = ir.Opcode.cc_invert_map[cc];

                    // invert the comparison
                    if ((sah is ContentsReg) && (sbh is ContentsReg))
                    {
                        handle_move(r_edx, sbh, r, n, c);
                        sbh = r_edx;
                    }
                    r.Add(inst(x86_cmp_rm32_r32, sah, sbh, n));
                    r.Add(inst_jmp(x86_jcc_rel32, fail_path, other_cc, n));

                    if ((sal is ContentsReg) && (sbl is ContentsReg))
                    {
                        handle_move(r_edx, sbl, r, n, c);
                        sbl = r_edx;
                    }
                    r.Add(inst(x86_cmp_rm32_r32, sal, sbl, n));
                    r.Add(inst_jmp(x86_jcc_rel32, fail_path, other_cc, n));

                    // success
                    r.Add(inst(x86_mov_rm32_imm32, dest, 1, n));
                    r.Add(inst_jmp(x86_jmp_rel32, end_path, n));

                    // fail
                    r.Add(inst(Generic.g_mclabel, new ir.Param { t = ir.Opcode.vl_br_target, v = fail_path }, n));
                    r.Add(inst(x86_mov_rm32_imm32, dest, 0, n));

                    // end
                    r.Add(inst(Generic.g_mclabel, new ir.Param { t = ir.Opcode.vl_br_target, v = end_path }, n));
                }
                else
                {
                    var success_path = c.next_mclabel--;
                    int j1_cc = 0;
                    int j2_cc = 0;
                    int j3_cc = 0;

                    switch(cc)
                    {
                        case ir.Opcode.cc_a:
                            j1_cc = ir.Opcode.cc_b;
                            j2_cc = ir.Opcode.cc_a;
                            j3_cc = ir.Opcode.cc_be;
                            break;
                        case ir.Opcode.cc_ae:
                            j1_cc = ir.Opcode.cc_b;
                            j2_cc = ir.Opcode.cc_a;
                            j3_cc = ir.Opcode.cc_b;
                            break;
                        case ir.Opcode.cc_b:
                            j1_cc = ir.Opcode.cc_a;
                            j2_cc = ir.Opcode.cc_b;
                            j3_cc = ir.Opcode.cc_ae;
                            break;
                        case ir.Opcode.cc_be:
                            j1_cc = ir.Opcode.cc_a;
                            j2_cc = ir.Opcode.cc_b;
                            j3_cc = ir.Opcode.cc_a;
                            break;
                        case ir.Opcode.cc_ge:
                            j1_cc = ir.Opcode.cc_lt;
                            j2_cc = ir.Opcode.cc_gt;
                            j3_cc = ir.Opcode.cc_b;
                            break;
                        case ir.Opcode.cc_gt:
                            j1_cc = ir.Opcode.cc_lt;
                            j2_cc = ir.Opcode.cc_gt;
                            j3_cc = ir.Opcode.cc_be;
                            break;
                        case ir.Opcode.cc_le:
                            j1_cc = ir.Opcode.cc_gt;
                            j2_cc = ir.Opcode.cc_lt;
                            j3_cc = ir.Opcode.cc_a;
                            break;
                        case ir.Opcode.cc_lt:
                            j1_cc = ir.Opcode.cc_gt;
                            j2_cc = ir.Opcode.cc_lt;
                            j3_cc = ir.Opcode.cc_ae;
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    if ((sah is ContentsReg) && (sbh is ContentsReg))
                    {
                        handle_move(r_edx, sbh, r, n, c);
                        sbh = r_edx;
                    }
                    r.Add(inst(x86_cmp_rm32_r32, sah, sbh, n));
                    r.Add(inst_jmp(x86_jcc_rel32, fail_path, j1_cc, n));
                    r.Add(inst_jmp(x86_jcc_rel32, success_path, j2_cc, n));

                    if ((sal is ContentsReg) && (sbl is ContentsReg))
                    {
                        handle_move(r_edx, sbl, r, n, c);
                        sbl = r_edx;
                    }
                    r.Add(inst(x86_cmp_rm32_r32, sal, sbl, n));
                    r.Add(inst_jmp(x86_jcc_rel32, fail_path, j3_cc, n));

                    // success
                    r.Add(inst(Generic.g_mclabel, new ir.Param { t = ir.Opcode.vl_br_target, v = success_path }, n));
                    r.Add(inst(x86_mov_rm32_imm32, dest, 1, n));
                    r.Add(inst_jmp(x86_jmp_rel32, end_path, n));

                    // fail
                    r.Add(inst(Generic.g_mclabel, new ir.Param { t = ir.Opcode.vl_br_target, v = fail_path }, n));
                    r.Add(inst(x86_mov_rm32_imm32, dest, 0, n));

                    // end
                    r.Add(inst(Generic.g_mclabel, new ir.Param { t = ir.Opcode.vl_br_target, v = end_path }, n));
                }

                return r;

            }
            return null;
        }

        internal static List<MCInst> handle_br(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            return new List<MCInst> { inst_jmp(x86_jmp_rel32, (int)n.imm_l, n) };
        }

        internal static List<MCInst> handle_brif(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            if (n_ct == ir.Opcode.ct_int32)
            {
                var srca = n.stack_before.Peek(n.arg_a).reg;
                var srcb = n.stack_before.Peek(n.arg_b).reg;

                List<MCInst> r = new List<MCInst>();

                var cc = (int)n.imm_ul;
                var target = (int)n.imm_l;

                handle_brifi32(srca, srcb, cc, target, r, n);

                return r;
            }
            else if (n_ct == ir.Opcode.ct_int64)
            {
                var srca = n.stack_before.Peek(n.arg_a).reg as DoubleReg;
                var srcb = n.stack_before.Peek(n.arg_b).reg as DoubleReg;

                List<MCInst> r = new List<MCInst>();

                var cc = (int)n.imm_ul;
                var target = (int)n.imm_l;

                // the following is untested
                var other_cc = ir.Opcode.cc_invert_map[cc];
                var neq_path = c.next_mclabel--;
                handle_brifi32(srca.b, srcb.b, other_cc, neq_path, r, n);
                handle_brifi32(srca.a, srcb.a, cc, target, r, n);
                r.Add(inst(Generic.g_mclabel, new ir.Param { t = ir.Opcode.vl_br_target, v = neq_path }, n));

                return r;
            }
            else if (n_ct == ir.Opcode.ct_float)
            {
                var srca = n.stack_before.Peek(n.arg_a).reg;
                var srcb = n.stack_before.Peek(n.arg_b).reg;

                List<MCInst> r = new List<MCInst>();

                var cc = (int)n.imm_ul;
                var target = (int)n.imm_l;

                handle_briff(srca, srcb, cc, target, r, n);

                return r;
            }
            return null;
        }

        internal static List<MCInst> handle_enter(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            List<MCInst> r = new List<MCInst>();
            var n = nodes[start];

            var lv_size = c.lv_total_size;

            if (ir.Opcode.GetCTFromType(c.ret_ts) == ir.Opcode.ct_vt)
            {
                r.Add(inst(x86_pop_r32, r_eax, n));
                r.Add(inst(x86_xchg_r32_rm32, r_eax, new ContentsReg { basereg = r_esp, disp = 0, size = 4 }, n));
                lv_size += 4;
            }

            r.Add(inst(x86_push_r32, r_ebp, n));
            r.Add(inst(x86_mov_r32_rm32, r_ebp, r_esp, n));
            if (lv_size != 0)
            {
                if (lv_size <= 127)
                    r.Add(inst(x86_sub_rm32_imm8, r_esp, lv_size, n));
                else
                    r.Add(inst(x86_sub_rm32_imm32, r_esp, lv_size, n));
            }

            var regs_to_save = c.regs_used | c.t.cc_callee_preserves_map["sysv"];
            var regs_set = new util.Set();
            regs_set.Union(regs_to_save);
            int y = 0;
            while (regs_set.Empty == false)
            {
                var reg = regs_set.get_first_set();
                regs_set.unset(reg);
                var cur_reg = registers[reg];
                handle_push(cur_reg, ref y, r, n, c);
                c.regs_saved.Add(cur_reg);
            }

            if (ir.Opcode.GetCTFromType(c.ret_ts) == ir.Opcode.ct_vt)
                r.Add(inst(x86_mov_rm32_r32, new ContentsReg { basereg = r_ebp, disp = -4, size = 4 }, r_eax, n));

            return r;
        }

        internal static List<MCInst> handle_enter_handler(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var r = new List<MCInst>();
            r.Add(inst(Generic.g_label, c.ms.MangleMethod() + "EH" + n.imm_l.ToString(), n));
            return r;
        }

        internal static List<MCInst> handle_conv(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            bool is_un = (n.imm_l & 0x1) != 0;
            bool is_ovf = (n.imm_l & 0x2) != 0;
            if (n_ct == ir.Opcode.ct_int32)
            {
                var r = new List<MCInst>();

                var si = n.stack_before.Peek(n.arg_a);
                var di = n.stack_after.Peek(n.res_a);
                var to_type = di.ts.SimpleType;

                var sreg = si.reg;
                var dreg = di.reg;

                var actdreg = dreg;
                if (dreg is ContentsReg)
                    dreg = r_edx;

                switch (to_type)
                {
                    case 0x04:
                        if (sreg.Equals(r_esi) || sreg.Equals(r_edi))
                        {
                            r.Add(inst(x86_mov_r32_rm32, r_eax, sreg, n));
                            sreg = r_eax;
                        }
                        r.Add(inst(x86_movsxbd, dreg, sreg, n));
                        break;
                    case 0x05:
                        if (sreg.Equals(r_esi) || sreg.Equals(r_edi))
                        {
                            r.Add(inst(x86_mov_r32_rm32, r_eax, sreg, n));
                            sreg = r_eax;
                        }
                        r.Add(inst(x86_movzxbd, dreg, sreg, n));
                        break;
                    case 0x06:
                        // int16
                        r.Add(inst(x86_movsxwd, dreg, sreg, n));
                        break;
                    case 0x07:
                        // uint16
                        r.Add(inst(x86_movzxwd, dreg, sreg, n));
                        break;
                    case 0x08:
                    case 0x09:
                    case 0x18:
                    case 0x19:
                        // nop
                        if (!sreg.Equals(dreg))
                            handle_move(dreg, sreg, r, n, c);
                        return r;
                    case 0x0a:
                        // conv to i8
                        {
                            DoubleReg dr = dreg as DoubleReg;
                            if (!(dr.a.Equals(sreg)))
                            {
                                handle_move(dr.a, sreg, r, n, c);
                            }
                            handle_move(dr.b, sreg, r, n, c);
                            r.Add(inst(x86_sar_rm32_imm8, dr.b, 31, n));
                        }
                        break;
                    case 0x0b:
                        // conv to u8
                        {
                            DoubleReg dr = dreg as DoubleReg;
                            if (!(dr.a.Equals(sreg)))
                            {
                                handle_move(dr.a, sreg, r, n, c);
                            }
                            r.Add(inst(x86_mov_rm32_imm32, dr.b, new ir.Param { t = ir.Opcode.vl_c, v = 0 }, n));
                        }
                        break;
                    case 0x0c:
                    case 0x0d:
                        // conv to r4/8 (internal representation is the same)
                        {
                            if (dreg == r_edx)
                                dreg = r_xmm7;
                            r.Add(inst(x86_cvtsi2sd_xmm_rm32, dreg, sreg, n));
                            break;
                        }
                    default:
                        throw new NotImplementedException("Convert to " + to_type.ToString());
                }

                if (!dreg.Equals(actdreg))
                    handle_move(actdreg, dreg, r, n, c);

                return r;
            }
            else if (n_ct == ir.Opcode.ct_int64)
            {
                var r = new List<MCInst>();

                var si = n.stack_before.Peek(n.arg_a);
                var di = n.stack_after.Peek(n.res_a);
                var to_type = di.ts.SimpleType;

                var sreg = si.reg;
                var dreg = di.reg;

                var srca = sreg.SubReg(0, 4);
                var srcb = sreg.SubReg(4, 4);

                var actdreg = dreg;
                if (dreg is ContentsReg)
                    dreg = r_edx;

                switch (to_type)
                {
                    case 0x04:
                        // i1
                        if (dreg.Equals(r_edi) || dreg.Equals(r_esi))
                            dreg = r_edx;
                        handle_move(dreg, srca, r, n, c);
                        r.Add(inst(x86_movsxbd, dreg, dreg, n));
                        break;
                    case 0x05:
                        // u1
                        if (!srca.Equals(dreg))
                            handle_move(dreg, srca, r, n, c);
                        r.Add(inst(x86_movzxbd, dreg, dreg, n));
                        break;
                    case 0x06:
                        // i2
                        handle_move(dreg, srca, r, n, c);
                        r.Add(inst(x86_movsxwd, dreg, dreg, n));
                        break;
                    case 0x07:
                        // u2
                        handle_move(dreg, srca, r, n, c);
                        r.Add(inst(x86_movzxwd, dreg, dreg, n));
                        break;
                    case 0x08:
                    case 0x09:
                    case 0x18:
                    case 0x19:
                        if (!srca.Equals(dreg))
                            handle_move(dreg, srca, r, n, c);
                        // nop - ignore high 32 bits
                        return r;
                    case 0x0a:
                        // conv to i8
                        throw new NotImplementedException();
                        break;
                    case 0x0c:
                    case 0x0d:
                        // conv to float
                        if (is_un)
                        {
                            return t.handle_external(t, nodes,
                                start, count, c, "__floatuntidf");
                        }
                        else
                        {
                            return t.handle_external(t, nodes,
                                start, count, c, "__floattidf");
                        }
                    default:
                        throw new NotImplementedException("Convert to " + to_type.ToString());
                }

                if (!dreg.Equals(actdreg))
                    r.Add(inst(x86_mov_rm32_r32, actdreg, dreg, n));

                return r;
            }
            else if (n_ct == ir.Opcode.ct_float)
            {
                var r = new List<MCInst>();

                var si = n.stack_before.Peek(n.arg_a);
                var di = n.stack_after.Peek(n.res_a);
                var to_type = di.ts.SimpleType;

                var sreg = si.reg;
                var dreg = di.reg;
                var act_dreg = dreg;

                switch (to_type)
                {
                    case 0x04:
                        // int8
                        if (dreg is ContentsReg)
                            act_dreg = r_eax;
                        r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                        r.Add(inst(x86_movsxbd, act_dreg, act_dreg, n));
                        handle_move(dreg, act_dreg, r, n, c);
                        return r;
                    case 0x05:
                        // uint8
                        if (dreg is ContentsReg)
                            act_dreg = r_eax;
                        r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                        r.Add(inst(x86_movzxbd, act_dreg, act_dreg, n));
                        handle_move(dreg, act_dreg, r, n, c);
                        return r;
                    case 0x06:
                        // int16
                        if (dreg is ContentsReg)
                            act_dreg = r_eax;
                        r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                        r.Add(inst(x86_movsxwd, act_dreg, act_dreg, n));
                        handle_move(dreg, act_dreg, r, n, c);
                        return r;
                    case 0x07:
                        // uint16
                        if (dreg is ContentsReg)
                            act_dreg = r_eax;
                        r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                        r.Add(inst(x86_movzxwd, act_dreg, act_dreg, n));
                        handle_move(dreg, act_dreg, r, n, c);
                        return r;
                    case 0x08:
                        // int32
                        if (dreg is ContentsReg)
                            act_dreg = r_eax;
                        r.Add(inst(x86_cvtsd2si_r32_xmmm64, act_dreg, sreg, n));
                        handle_move(dreg, act_dreg, r, n, c);
                        return r;
                    case 0x09:
                        // uint32
                        //fc_override = "__fixunsdfsi";
                        return t.handle_external(t, nodes, start, count, c,
                            "__fixunsdfsi");
                    case 0x0a:
                        // int64
                        //fc_override = "__fixdfti";
                        return t.handle_external(t, nodes, start, count, c,
                            "__fixdfti");
                    case 0x0b:
                        // uint64
                        //fc_override = "__fixunsdfti";
                        return t.handle_external(t, nodes, start, count, c,
                            "__fixunsdfti");
                    case 0x0c:
                    case 0x0d:
                        // float to float (all floats are 64-bit internally)
                        if (!dreg.Equals(sreg))
                            handle_move(dreg, sreg, r, n, c);
                        return r;
                    default:
                        throw new NotImplementedException();
                }
            }
            return null;
        }

        internal static List<MCInst> handle_stind(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct2 = n.ct2;
            var addr = n.stack_before.Peek(n.arg_a).reg;
            var val = n.stack_before.Peek(n.arg_b).reg;
            if (n_ct2 == ir.Opcode.ct_int32)
            {
                List<MCInst> r = new List<MCInst>();
                if (addr is ContentsReg)
                {
                    r.Add(inst(x86_mov_r32_rm32, r_eax, addr, n));
                    addr = r_eax;
                }
                if (val is ContentsReg || ((val.Equals(r_esi) || val.Equals(r_edi) && n.vt_size != 4)))
                {
                    r.Add(inst(x86_mov_r32_rm32, r_edx, val, n));
                    val = r_edx;
                }

                switch (n.vt_size)
                {
                    case 1:
                        r.Add(inst(x86_mov_rm8disp_r32, addr, 0, val, n));
                        break;
                    case 2:
                        r.Add(inst(x86_mov_rm16disp_r32, addr, 0, val, n));
                        break;
                    case 4:
                        r.Add(inst(x86_mov_rm32disp_r32, addr, 0, val, n));
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return r;
            }
            else if (n_ct2 == ir.Opcode.ct_int64 || 
                (n_ct2 == ir.Opcode.ct_vt && val is DoubleReg))
            {
                var dr = val as DoubleReg;

                List<MCInst> r = new List<MCInst>();
                if (addr is ContentsReg)
                {
                    r.Add(inst(x86_mov_r32_rm32, r_eax, addr, n));
                    addr = r_eax;
                }

                handle_stind(dr.a, addr, 0, 4, r, n);
                handle_stind(dr.b, addr, 4, 4, r, n);
                return r;
            }
            else if (n_ct2 == ir.Opcode.ct_vt)
            {
                if (n.vt_size <= 16 && val is ContentsReg)
                {
                    // handle as a bunch of moves
                    List<MCInst> r = new List<MCInst>();
                    if (addr is ContentsReg)
                    {
                        handle_move(r_edx, addr, r, n, c);
                        addr = r_edx;
                    }
                    addr = new ContentsReg { basereg = addr, size = n.vt_size };
                    handle_move(addr, val, r, n, c, r_eax);
                    return r;
                }
                else
                {
                    // emit call to memcpy(dest, src, n)
                    List<MCInst> r = new List<MCInst>();
                    r.AddRange(handle_call(n, c,
                        c.special_meths.GetMethodSpec(c.special_meths.memcpy),
                        new ir.Param[] {
                            addr,
                            new ir.Param { t = ir.Opcode.vl_stack, mreg = val },
                            n.vt_size },
                        null, "memcpy"));
                    return r;
                }
            }
            else if(n_ct2 == ir.Opcode.ct_float)
            {
                List<MCInst> r = new List<MCInst>();
                if (addr is ContentsReg)
                {
                    handle_move(r_edx, addr, r, n, c);
                    addr = r_edx;
                }
                if(val is ContentsReg)
                {
                    handle_move(r_xmm7, val, r, n, c);
                    val = r_xmm7;
                }

                addr = new ContentsReg { basereg = addr, size = n.vt_size };

                if (n.vt_size == 4)
                {
                    // convert to r4, then store
                    r.Add(inst(x86_cvtsd2ss_xmm_xmmm64, val, val, n));
                    r.Add(inst(x86_movss_xmmm32_xmm, addr, val, n));
                }
                else if (n.vt_size == 8)
                {
                    // direct store
                    r.Add(inst(x86_movsd_xmmm64_xmm, addr, val, n));
                }
                else
                    throw new NotSupportedException();
                return r;
            }

            return null;
        }

        internal static List<MCInst> handle_ldind(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ctret;
            var addr = n.stack_before.Peek(n.arg_a).reg;
            var val = n.stack_after.Peek(n.res_a).reg;
            if (n_ct == ir.Opcode.ct_int32)
            {
                List<MCInst> r = new List<MCInst>();
                handle_ldind(val, addr, 0, n.vt_size, r, n);

                return r;
            }
            else if (n_ct == ir.Opcode.ct_int64 ||
                (n_ct == ir.Opcode.ct_vt && val is DoubleReg))
            {
                List<MCInst> r = new List<MCInst>();

                var dr = val as DoubleReg;

                /* Do this here so its only done once.  We don't need the [esi]/[edi]
                 * check as the vt_size is guaranteed to be 4.  However, the address
                 * used in the second iteration may overwrite the return value in
                 * the first, so we need to check for that.  We do it backwards
                 * so that this is less likely (e.g. stack goes from
                 * ..., esi to ..., esi, edi thus assigning first to edi won't
                 * cause a problem */
                if (addr is ContentsReg || addr.Equals(dr.b))
                {
                    r.Add(inst(x86_mov_r32_rm32, r_eax, addr, n));
                    addr = r_eax;
                }

                handle_ldind(dr.b, addr, 4, 4, r, n);
                handle_ldind(dr.a, addr, 0, 4, r, n);

                return r;
            }
            else if(n_ct == ir.Opcode.ct_vt)
            {
                // handle as a bunch of moves
                List<MCInst> r = new List<MCInst>();
                if(addr is ContentsReg)
                {
                    handle_move(r_edx, addr, r, n, c);
                    addr = r_edx;
                }
                addr = new ContentsReg { basereg = addr, size = n.vt_size };
                handle_move(val, addr, r, n, c, r_eax);
                return r;                    
            }
            else if(n_ct == ir.Opcode.ct_float)
            {
                List<MCInst> r = new List<MCInst>();
                if(addr is ContentsReg)
                {
                    handle_move(r_edx, addr, r, n, c);
                    addr = r_edx;
                }
                var act_dest = val;
                if(act_dest is ContentsReg)
                {
                    act_dest = r_xmm7;
                }
                addr = new ContentsReg { basereg = addr, size = n.vt_size };
                switch(n.vt_size)
                {
                    case 4:
                        r.Add(inst(x86_cvtss2sd_xmm_xmmm32, act_dest, addr, n));
                        break;
                    case 8:
                        r.Add(inst(x86_movsd_xmm_xmmm64, act_dest, addr, n));
                        break;
                    default:
                        throw new NotSupportedException();
                }
                handle_move(val, act_dest, r, n, c);
                return r;
            }
            
            return null;
        }

        internal static List<MCInst> handle_ldlabaddr(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var dest = n.stack_after.Peek(n.res_a).reg;

            List<MCInst> r = new List<MCInst>();
            if (dest is ContentsReg)
            {
                r.Add(inst(x86_mov_rm32_imm32, r_eax, new ir.Param { t = ir.Opcode.vl_str, str = n.imm_lab, v = n.imm_l }, n));
                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
            }
            else
                r.Add(inst(x86_mov_rm32_imm32, dest, new ir.Param { t = ir.Opcode.vl_str, str = n.imm_lab, v = n.imm_l }, n));
            return r;
        }

        internal static List<MCInst> handle_zeromem(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var sizev = n.stack_before.Peek(n.arg_a);
            var dest = n.stack_before.Peek(n.arg_b).reg;

            int size = (int)n.imm_l;
            if(size == 0)
            {
                if (sizev.min_l == sizev.max_l)
                    size = (int)sizev.min_l;
            }
            if (size == 0)
            {
                // emit call to memset here
                throw new NotImplementedException();
            }

            List<MCInst> r = new List<MCInst>();
            handle_zeromem(dest, size, r, n, c);
            return r;
        }

        internal static List<MCInst> handle_ldc_zeromem(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n2 = nodes[start + 1];

            if (n2.arg_a != 0)
                return null;

            var dest = n2.stack_before.Peek(n.arg_b).reg;
            var size = n.imm_l;

            List<MCInst> r = new List<MCInst>();
            handle_zeromem(dest, (int)size, r, n2, c);
            return r;
        }

        private static void handle_zeromem(Reg dest, int size,
            List<MCInst> r, CilNode.IRNode n, Code c)
        {
            if (size > 16)
            {
                // emit call to memset(void *s, int c, size_t n) here
                r.AddRange(handle_call(n, c,
                    c.special_meths.GetMethodSpec(c.special_meths.memset),
                    new ir.Param[]
                    {
                            new ir.Param { t = ir.Opcode.vl_stack, mreg = dest },
                            0,
                            size
                    },
                    null, "memset"));
                return;
            }

            if(dest is ContentsReg)
            {
                handle_move(r_edx, dest, r, n, c);
                dest = r_edx;
            }

            size = util.util.align(size, 4);
            var cr = new ContentsReg { basereg = dest, size = size };

            for(int i = 0; i < size; i += 4)
            {
                var d = cr.SubReg(i, 4);
                r.Add(inst(x86_mov_rm32_imm32, d, 0, n));
            }
        }

        internal static List<MCInst> handle_ldc(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ctret;

            switch(n_ct)
            {
                case ir.Opcode.ct_int32:
                    return new List<MCInst> { inst(x86_mov_rm32_imm32, n.stack_after.Peek(n.res_a).reg, (int)n.imm_l, n) };

                case ir.Opcode.ct_float:
                    if (n.vt_size == 4)
                    {
                        var dest = n.stack_after.Peek(n.res_a).reg;
                        var act_dest = dest;
                        var r = new List<MCInst>();

                        if (dest is ContentsReg)
                            act_dest = r_xmm7;

                        r.Add(inst(x86_push_imm32, n.imm_l, n));
                        r.Add(inst(x86_cvtss2sd_xmm_xmmm32, act_dest,
                            new ContentsReg { basereg = r_esp, size = 4 }, n));
                        r.Add(inst(x86_add_rm32_imm8, r_esp, 4, n));
                        handle_move(dest, act_dest, r, n, c);
                        return r;
                    }
                    else if (n.vt_size == 8)
                    {
                        var high_val = BitConverter.ToInt32(n.imm_val, 4);
                        var low_val = BitConverter.ToInt32(n.imm_val, 0);

                        var dest = n.stack_after.Peek(n.res_a).reg;
                        var act_dest = dest;
                        var r = new List<MCInst>();

                        if (dest is ContentsReg)
                            act_dest = r_xmm7;

                        r.Add(inst(x86_push_imm32, high_val, n));
                        r.Add(inst(x86_push_imm32, low_val, n));
                        r.Add(inst(x86_movsd_xmm_xmmm64, act_dest,
                            new ContentsReg { basereg = r_esp, size = 8 }, n));
                        r.Add(inst(x86_add_rm32_imm8, r_esp, 8, n));
                        handle_move(dest, act_dest, r, n, c);
                        return r;
                    }
                    else
                        throw new NotSupportedException();

                case ir.Opcode.ct_int64:
                    {
                        var dest = n.stack_after.Peek(n.res_a).reg;
                        var da = dest.SubReg(0, 4);
                        var db = dest.SubReg(4, 4);

                        var sa = BitConverter.ToInt32(n.imm_val, 0);
                        var sb = BitConverter.ToInt32(n.imm_val, 4);

                        var r = new List<MCInst>();
                        r.Add(inst(x86_mov_rm32_imm32, da, sa, n));
                        r.Add(inst(x86_mov_rm32_imm32, db, sb, n));
                        return r;
                    }

                default:
                    throw new NotImplementedException();
            }

            return null;
        }

        internal static List<MCInst> handle_ldloc(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ctret;
            var src = c.lv_locs[(int)n.imm_l];
            var dest = n.stack_after.Peek(n.res_a).reg;

            var r = new List<MCInst>();
            if (n_ct == ir.Opcode.ct_int32 ||
                n_ct == ir.Opcode.ct_float ||
                n_ct == ir.Opcode.ct_vt)
            {
                handle_move(dest, src, r, n, c);
                return r;
            }
            else if (n_ct == ir.Opcode.ct_int64)
            {
                var desta = dest.SubReg(0, 4);
                var destb = dest.SubReg(4, 4);
                var srca = src.SubReg(0, 4);
                var srcb = src.SubReg(4, 4);

                handle_move(desta, srca, r, n, c);
                handle_move(destb, srcb, r, n, c);

                return r;
            }
            return null;
        }

        internal static List<MCInst> handle_stloc(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var src = n.stack_before.Peek(n.arg_a).reg;
            var dest = c.lv_locs[(int)n.imm_l];

            var r = new List<MCInst>();

            if (n_ct == ir.Opcode.ct_int32 ||
                n_ct == ir.Opcode.ct_float ||
                n_ct == ir.Opcode.ct_vt)
            {
                handle_move(dest, src, r, n, c);
                return r;
            }
            else if (n_ct == ir.Opcode.ct_int64)
            {
                var srca = src.SubReg(0, 4);
                var srcb = src.SubReg(4, 4);
                var desta = dest.SubReg(0, 4);
                var destb = dest.SubReg(4, 4);

                handle_move(desta, srca, r, n, c);
                handle_move(destb, srcb, r, n, c);

                return r;
            }
            return null;
        }

        internal static List<MCInst> handle_rem(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var srca = n.stack_before.Peek(n.arg_a).reg;
            var srcb = n.stack_before.Peek(n.arg_b).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;
            var n_ct = n.ct;

            switch(n_ct)
            {
                case ir.Opcode.ct_int32:
                    {
                        List<MCInst> r = new List<MCInst>();
                        handle_move(r_eax, srca, r, n, c);
                        r.Add(inst(x86_xor_r32_rm32, r_edx, r_edx, n));
                        r.Add(inst(x86_idiv_rm32, srcb, n));
                        handle_move(dest, r_edx, r, n, c);
                        return r;
                    }
                case ir.Opcode.ct_int64:
                    return t.handle_external(t, nodes, start, count, c, "__modti3");
            }

            throw new NotImplementedException();
        }

        internal static List<MCInst> handle_ldarga(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var src = c.la_locs[(int)n.imm_l];
            var dest = n.stack_after.Peek(n.res_a).reg;

            if (!(src is ContentsReg))
                throw new Exception("ldarga from " + src.ToString());

            var act_dest = dest;
            if (dest is ContentsReg)
                dest = r_eax;

            List<MCInst> r = new List<MCInst>();
            r.Add(inst(x86_lea_r32, dest, src, n));

            handle_move(act_dest, dest, r, n, c);

            return r;
        }

        internal static List<MCInst> handle_ldloca(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var src = c.lv_locs[(int)n.imm_l];
            var dest = n.stack_after.Peek(n.res_a).reg;

            if (!(src is ContentsReg))
                throw new Exception("ldloca from " + src.ToString());

            var act_dest = dest;
            if (dest is ContentsReg)
                dest = r_eax;

            List<MCInst> r = new List<MCInst>();
            r.Add(inst(x86_lea_r32, dest, src, n));

            handle_move(act_dest, dest, r, n, c);

            return r;
        }

        internal static List<MCInst> handle_starg(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var dest = c.la_locs[(int)n.imm_l];
            var src = n.stack_before.Peek(n.arg_a).reg;

            var r = new List<MCInst>();
            if (n_ct == ir.Opcode.ct_int32 ||
                n_ct == ir.Opcode.ct_float ||
                n_ct == ir.Opcode.ct_vt ||
                n_ct == ir.Opcode.ct_int64)
            {
                handle_move(dest, src, r, n, c);
                return r;
            }
            return null;
        }

        internal static List<MCInst> handle_ldarg(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ctret;
            var src = c.la_locs[(int)n.imm_l];
            var dest = n.stack_after.Peek(n.res_a).reg;

            var r = new List<MCInst>();
            if (n_ct == ir.Opcode.ct_int32 ||
                n_ct == ir.Opcode.ct_float ||
                n_ct == ir.Opcode.ct_vt)
            {
                handle_move(dest, src, r, n, c);
                return r;
            }
            else if (n_ct == ir.Opcode.ct_int64)
            {
                var drd = dest as DoubleReg;

                var crs = src as ContentsReg;
                var crsa = new ContentsReg { basereg = crs.basereg, disp = crs.disp, size = 4 };
                var crsb = new ContentsReg { basereg = crs.basereg, disp = crs.disp + 4, size = 4 };

                handle_move(drd.a, crsa, r, n, c);
                handle_move(drd.b, crsb, r, n, c);

                return r;
            }
            return null;
        }

        internal static List<MCInst> handle_stackcopy(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var src = n.stack_before.Peek(n.arg_a).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;

            List<MCInst> r = new List<MCInst>();
            handle_move(dest, src, r, n, c);
            return r;
        }

        internal static List<MCInst> handle_localloc(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var size = n.stack_before.Peek(n.arg_a).reg;
            var addr = n.stack_after.Peek(n.res_a).reg;

            var act_dest = addr;
            if (act_dest is ContentsReg)
                act_dest = r_edx;

            List<MCInst> r = new List<MCInst>();
            handle_sub(r_esp, size, r_esp, r, n);
            r.Add(inst(x86_and_rm32_imm8, r_esp, 0xfc, n));
            r.Add(inst(x86_lea_r32, act_dest,
                new ContentsReg { basereg = r_esp }, n));
            handle_move(addr, act_dest, r, n, c);
            return r;
        }

        internal static List<MCInst> handle_ldobja(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var src = n.stack_before.Peek(n.arg_a).reg;
            var addr = n.stack_after.Peek(n.res_a).reg;

            var act_addr = addr;
            if (addr is ContentsReg)
                addr = r_edx;

            List<MCInst> r = new List<MCInst>();
            r.Add(inst(x86_lea_r32, addr, src, n));
            handle_move(act_addr, addr, r, n, c);

            return r;
        }

        internal static List<MCInst> handle_shift(
           Target t,
           List<CilNode.IRNode> nodes,
           int start, int count, Code c)
        {
            var n = nodes[start];
            var n_ct = n.ct;
            var srca = n.stack_before.Peek(n.arg_a).reg;
            var srcb = n.stack_before.Peek(n.arg_b).reg;
            var dest = n.stack_after.Peek(n.res_a).reg;

            List<MCInst> r = new List<MCInst>();

            if (n_ct == ir.Opcode.ct_int32)
            {
                handle_shift_i32(srca, srcb, dest, r, n, c);

                return r;
            }
            else if (n_ct == ir.Opcode.ct_int64)
            {
                var sb = n.stack_before.Peek(n.arg_b);

                if (sb.min_l == sb.max_l &&
                    sb.min_l == 1)
                {
                    // handle as a shift through cf using two
                    //  calls to handle_shift_i32 - NB need
                    //  to change handle_shift_i32 to handle
                    //  use_cf = true
                    throw new NotImplementedException();
                }
                else
                {
                    // handle as a function call
                    string fname = null;
                    switch(n.opcode)
                    {
                        case ir.Opcode.oc_shl:
                            fname = "__ashlti3";
                            break;
                        case ir.Opcode.oc_shr:
                            fname = "__ashrti3";
                            break;
                        case ir.Opcode.oc_shr_un:
                            fname = "__lshrti3";
                            break;
                    }
                    return t.handle_external(t, nodes, start,
                        count, c, fname);
                }
            }
            return null;
        }

        private static void handle_shift_i32(Reg srca, Reg srcb,
            Reg dest, List<MCInst> r, CilNode.IRNode n, Code c,
            bool use_cf = false)
        {
            bool cl_in_use_before = false;
            bool cl_in_use_after = false;

            if (!srca.Equals(dest) || srca.Equals(r_ecx))
            {
                // if either of the above is true, we need to move
                //  the source to eax
                handle_move(r_eax, srca, r, n, c);
                srca = r_eax;
            }

            if (!srcb.Equals(r_ecx))
            {
                // need to assign the number to move to cl
                // first determine if it is in use

                ulong defined = 0;
                foreach (var si in n.stack_before)
                    defined |= si.reg.mask;
                if ((defined & r_ecx.mask) != 0)
                    cl_in_use_before = true;
                defined = 0;
                foreach (var si in n.stack_after)
                    defined |= si.reg.mask;
                if ((defined & r_ecx.mask) != 0)
                    cl_in_use_after = true;
            }

            if (cl_in_use_before && cl_in_use_after)
            {
                handle_move(r_ecx, srcb, r, n, c);
                srcb = r_ecx;
            }

            int oc = 0;
            switch (n.opcode)
            {
                case ir.Opcode.oc_shl:
                    oc = x86_sal_rm32_cl;
                    break;
                case ir.Opcode.oc_shr:
                    oc = x86_sar_rm32_cl;
                    break;
                case ir.Opcode.oc_shr_un:
                    oc = x86_shr_rm32_cl;
                    break;
            }

            r.Add(inst(oc, srca, srcb, n));

            if (!srca.Equals(dest))
                handle_move(dest, srca, r, n, c);
        }

        internal static List<MCInst> handle_switch(
            Target t,
            List<CilNode.IRNode> nodes,
            int start, int count, Code c)
        {
            var n = nodes[start];

            var src = n.stack_before.Peek(n.arg_a).reg;
            List<MCInst> r = new List<MCInst>();

            /* Implement as a series of brifs for now */
            for(int i = 0; i < n.arg_list.Count; i++)
                handle_brifi32(src, i, ir.Opcode.cc_eq, n.arg_list[i], r, n);

            return r;
        }

        internal static List<MCInst> handle_getCharSeq(
            Target t,
            List<CilNode.IRNode> nodes,
            int start, int count, Code c)
        {
            var n = nodes[start];
            var ldc_scale = nodes[start];
            var conv = nodes[start + 2];
            var ldc_offset = nodes[start + 4];
            var ldind = nodes[start + 6];

            var offset = ldc_offset.imm_l;
            var scale = ldc_scale.imm_l;

            // only handle valid scales
            switch(scale)
            {
                case 1:
                case 2:
                case 4:
                case 8:
                    break;
                default:
                    return null;
            }

            // ensure the types are valid
            if (conv.ctret != ir.Opcode.ct_int32)
                return null;
            
            // only handle valid data sizes
            switch(ldind.vt_size)
            {
                case 1:
                case 2:
                case 4:
                    break;
                default:
                    return null;
            }

            var obj = n.stack_before.Peek(1).reg;
            var idx = n.stack_before.Peek(0).reg;

            var dest = ldind.stack_after.Peek().reg;

            // emit as mov[sz]x[bw]d dest, [obj + offset + idx * scale]
            List<MCInst> r = new List<MCInst>();
            if(obj is ContentsReg)
            {
                handle_move(r_edx, obj, r, n, c);
                obj = r_edx;
            }
            if(idx is ContentsReg)
            {
                handle_move(r_eax, idx, r, n, c);
                idx = r_eax;
            }

            var act_dest = dest;
            if (dest is ContentsReg)
                dest = r_edx;

            switch(ldind.vt_size)
            {
                case 1:
                    if (ldind.imm_l == 1)
                        r.Add(inst(x86_movsxbd_r32_rm8sibscaledisp, dest, obj, idx, scale, offset, n));
                    else
                        r.Add(inst(x86_movzxbd_r32_rm8sibscaledisp, dest, obj, idx, scale, offset, n));
                    break;
                case 2:
                    if (ldind.imm_l == 1)
                        r.Add(inst(x86_movsxwd_r32_rm16sibscaledisp, dest, obj, idx, scale, offset, n));
                    else
                        r.Add(inst(x86_movzxwd_r32_rm16sibscaledisp, dest, obj, idx, scale, offset, n));
                    break;
                case 4:
                    r.Add(inst(x86_mov_r32_rm32sibscaledisp, dest, obj, idx, scale, offset, n));
                    break;
            }

            handle_move(act_dest, dest, r, n, c);

            return r;
        }
    }
}
