/* Copyright (C) 2013 by John Cronin
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
using libasm;

namespace libtysila.arm
{
    partial class arm_Assembler
    {
        IEnumerable<OutputBlock> arm_assign_i4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i4_gpr_gpr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            return OBList(EncDPROpcode(c, 0x1a, R0, result.hardware_loc, 0, 0, op1.hardware_loc));
        }

        IEnumerable<OutputBlock> arm_assign_i8_2gpr_2gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i8_2gpr_2gpr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i8_2gpr_2gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            // Assign from one 2gpr pair to another
            // label the pairs as (A, B) -> (C, D)
            multiple_hardware_location pair1 = op1.hardware_loc as multiple_hardware_location;
            multiple_hardware_location pair2 = result.hardware_loc as multiple_hardware_location;
            hardware_location A = pair1.hlocs[0];
            hardware_location B = pair1.hlocs[1];
            hardware_location C = pair2.hlocs[0];
            hardware_location D = pair2.hlocs[1];

            // Ensure we do not overwrite anything during the transfer
            // If B != C then transfer A to C then B to D
            List<OutputBlock> ret = new List<OutputBlock>();
            if (!B.Equals(C))
            {
                arm_assign(C, A, ret, state, c);
                arm_assign(D, B, ret, state, c);
            }
            else
            {
                // B == C

                // If A != D then transfer B to D then A to C
                if (!A.Equals(D))
                {
                    arm_assign(D, B, ret, state, c);
                    arm_assign(C, A, ret, state, c);
                }
                else
                {
                    // A == D and B == C
                    // Transfer via a temporary register (r12)
                    arm_assign(R12, A, ret, state, c);
                    arm_assign(D, B, ret, state, c);
                    arm_assign(C, R12, ret, state, c);
                }
            }
            return ret;
        }


        IEnumerable<OutputBlock> arm_assign_i4_imm12_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i4_imm12_gpr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i4_imm12_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            uint val = FromByteArrayU4(ToByteArrayZeroExtend(op1.constant_val, 4));
            return OBList(EncImmOpcode(c, 0x1a, 0, result.hardware_loc, val));
        }

        IEnumerable<OutputBlock> arm_assign_i4_imm16_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i4_imm16_gpr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i4_imm16_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            uint val = FromByteArrayU4(ToByteArrayZeroExtend(op1.constant_val, 4));
            return OBList(EncImmOpcode(c, 0x10, (val >> 12) & 0xf, result.hardware_loc, val & 0xfff));
        }

        IEnumerable<OutputBlock> arm_assign_i4_imm32_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i4_imm32_gpr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i4_imm32_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            /* This assembles to:
             *  ldr result, [pc]        Load the value 8 bytes ahead of this instruction to result
             *                              (remember due to prefetching PC is 8 bytes ahead of this instruction)
             *  b 0                     Jump 8 bytes ahead of current instruction
             *  .word imm_value         The immediate value
             *  ...                     Where we jump to
             */

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(EncRnRtOpcode(c, 0x19, PC, result.hardware_loc, 0));
            ret.Add(new CodeBlock { Code = new byte[] { 0x00, 0x00, 0x00, 0xea } });
            ret.Add(new LocalSymbol("$d", false));
            if (op1.address_of)
                ret.Add(new RelocationBlock { RelType = 2, Offset = op1.constant_offset, Target = op1.label, Size = 4, Value = 0 });
            else
            {
                byte[] val = ToByteArrayZeroExtend(op1.constant_val, 4);
                ret.Add(new CodeBlock { Code = val });
            }
            ret.Add(new LocalSymbol("$a", false));
            return ret;
        }

        IEnumerable<OutputBlock> arm_assign_i8_imm_2gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i8_imm_2gpr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i8_imm_2gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            byte[] val = ToByteArrayZeroExtend(op1.constant_val, 8);
            uint u1 = FromByteArrayU4(val, 0);
            uint u2 = FromByteArrayU4(val, 4);
            multiple_hardware_location d = result.hardware_loc as multiple_hardware_location;
            List<OutputBlock> ret = new List<OutputBlock>();
            arm_assign(d.hlocs[0], new const_location { c = u1 }, ret, state, c);
            arm_assign(d.hlocs[1], new const_location { c = u2 }, ret, state, c);
            return ret;
        }

        IEnumerable<OutputBlock> arm_assign_i4_gprptr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i4_gprptr_gpr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i4_gprptr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            int add = 1;
            int cval = op1.constant_offset;
            if (cval == 0)
                cval = ((hardware_contentsof)op1.hardware_loc).const_offset;
            if (cval < 0)
            {
                add = 0;
                cval = -cval;
            }
            uint imm = ((uint)cval);
            if (imm > 0xfff)
                throw new Exception("Constant offset too large");

            hardware_contentsof src = op1.hardware_loc as hardware_contentsof;
            if (src.base_loc is hardware_stackloc)
            {
                /* Instruction is of form like a = [stack(b) + c]
                 * 
                 * We need to do:
                 * scratch = stack(b)
                 * a = [scratch + c]
                 */

                List<OutputBlock> ret = new List<OutputBlock>();
                arm_assign(SCRATCH, src.base_loc, ret, state);
                ret.AddRange(arm_assign_i4_gprptr_gpr(ThreeAddressCode.Op.assign_i4, result,
                    new var { hardware_loc = new hardware_contentsof { base_loc = SCRATCH, size = src.size, const_offset = src.const_offset },
                     constant_offset = op1.constant_offset }, var.Null, null, state));
                return ret;
            }

            return OBList(EncRnRtOpcode(c, (uint)((add == 1) ? 0x19 : 0x11),
                ((hardware_contentsof)op1.hardware_loc).base_loc, result.hardware_loc, imm));
        }

        IEnumerable<OutputBlock> arm_assign_i4_gpr_gprptr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i4_gpr_gprptr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i4_gpr_gprptr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            int add = 1;
            int cval = result.constant_offset;
            if (cval == 0)
                cval = ((hardware_contentsof)result.hardware_loc).const_offset;
            if (cval < 0)
            {
                add = 0;
                cval = -cval;
            }
            uint imm = ((uint)cval);
            if (imm > 0xfff)
                throw new Exception("Constant offset too large");

            return OBList(EncRnRtOpcode(c, (uint)((add == 1) ? 0x18 : 0x10),
                ((hardware_contentsof)result.hardware_loc).base_loc, op1.hardware_loc, imm));
        }

        IEnumerable<OutputBlock> arm_assign_i8_2gpr_gprptr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i8_2gpr_gprptr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i8_2gpr_gprptr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            int cval = result.constant_offset;
            arm_gpr dest_address = ((hardware_contentsof)result.hardware_loc).base_loc as arm_gpr;
            if (dest_address == null)
                throw new Exception();
            multiple_hardware_location src = op1.hardware_loc as multiple_hardware_location;
            if(src == null)
                throw new Exception();

            List<OutputBlock> ret = new List<OutputBlock>();

            if (cval != 0)
            {
                // First create the address in r12
                if(cval > 0)
                    ret.AddRange(arm_add_i4_gpr_gpr_imm(ThreeAddressCode.Op.add_i4, new var { hardware_loc = R12 },
                        new var { hardware_loc = dest_address }, var.Const(cval), null, state));
                else
                    ret.AddRange(arm_sub_i4_gpr_gpr_imm(ThreeAddressCode.Op.sub_i4, new var { hardware_loc = R12 },
                        new var { hardware_loc = dest_address }, var.Const(-cval), null, state));

                dest_address = R12;
            }

            // Now do STMIA
            uint src_array = 0;
            foreach(arm_gpr s in src.hlocs)
            {
                if (s == null)
                    throw new Exception();
                src_array |= (uint)(0x1 << (int)s.reg);
            }
            ret.Add(new CodeBlock(ToByteArray((uint)(((uint)c << 28) | ((uint)0x88 << 20) | ((uint)dest_address.reg << 16) | src_array))));

            return ret;
        }

        IEnumerable<OutputBlock> arm_assign_i8_gprptr_2gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i8_gprptr_2gpr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i8_gprptr_2gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        {
            int cval = op1.constant_offset;
            arm_gpr src_address = ((hardware_contentsof)op1.hardware_loc).base_loc as arm_gpr;
            if (src_address == null)
                throw new Exception();
            multiple_hardware_location dest = result.hardware_loc as multiple_hardware_location;
            if (dest == null)
                throw new Exception();

            List<OutputBlock> ret = new List<OutputBlock>();

            if (cval != 0)
            {
                // First create the address in r12
                if (cval > 0)
                    ret.AddRange(arm_add_i4_gpr_gpr_imm(ThreeAddressCode.Op.add_i4, new var { hardware_loc = R12 },
                        new var { hardware_loc = src_address }, var.Const(cval), null, state));
                else
                    ret.AddRange(arm_sub_i4_gpr_gpr_imm(ThreeAddressCode.Op.sub_i4, new var { hardware_loc = R12 },
                        new var { hardware_loc = src_address }, var.Const(-cval), null, state));

                src_address = R12;
            }

            // Now do LDMIA
            uint dest_array = 0;
            foreach (arm_gpr d in dest.hlocs)
            {
                if (d == null)
                    throw new Exception();
                dest_array |= (uint)(0x1 << (int)d.reg);
            }
            ret.Add(new CodeBlock(ToByteArray((uint)(((uint)c << 28) | ((uint)0x89 << 20) | ((uint)src_address.reg << 16) | dest_array))));

            return ret;
        }

        IEnumerable<OutputBlock> arm_assign_i4_gpr_mem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i4_gpr_mem(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i4_gpr_mem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        { return arm_assign_i4_gpr_gprptr(op, new var { hardware_loc = InterpretStackLocation(result.hardware_loc as hardware_stackloc, state) }, op1, op2, tac, state, c); }

        IEnumerable<OutputBlock> arm_assign_i4_mem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return arm_assign_i4_mem_gpr(op, result, op1, op2, tac, state, cond.Always); }
        IEnumerable<OutputBlock> arm_assign_i4_mem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, cond c)
        { return arm_assign_i4_gprptr_gpr(op, result, new var { hardware_loc = InterpretStackLocation(op1.hardware_loc as hardware_stackloc, state) }, op2, tac, state, c); }   

        private void arm_assign(hardware_location dest, hardware_location src, List<OutputBlock> ret, AssemblerState state)
        { arm_assign(dest, src, ret, state, cond.Always); }

        internal override void Assign(hardware_location dest, hardware_location from, List<OutputBlock> ret, AssemblerState state)
        {
            arm_assign(dest, from, ret, state);
        }
        private void arm_assign(hardware_location dest, hardware_location src, List<OutputBlock> ret, AssemblerState state, cond c)
        {
            if (dest is arm_gpr)
            {
                if (src is arm_gpr)
                {
                    ret.AddRange(arm_assign_i4_gpr_gpr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest },
                        new var { hardware_loc = src }, var.Null, null, state, c));
                }
                else if (src is const_location)
                {
                    const_location cloc = src as const_location;
                    if (FitsInt12(cloc.c))
                        ret.AddRange(arm_assign_i4_imm12_gpr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest },
                            new var { hardware_loc = src, constant_val = cloc.c }, var.Null, null, state, c));
                    else if (FitsInt16(cloc.c))
                        ret.AddRange(arm_assign_i4_imm16_gpr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest },
                            new var { hardware_loc = src, constant_val = cloc.c }, var.Null, null, state, c));
                    else if (FitsInt32(cloc.c))
                        ret.AddRange(arm_assign_i4_imm32_gpr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest },
                            new var { hardware_loc = src, constant_val = cloc.c }, var.Null, null, state, c));
                    else
                        throw new NotSupportedException();
                }
                else if (src is hardware_contentsof)
                {
                    hardware_contentsof hco = src as hardware_contentsof;

                    if (hco.base_loc is arm_gpr)
                    {
                        ret.AddRange(arm_assign_i4_gprptr_gpr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest },
                            new var { hardware_loc = src }, var.Null, null, state, c));
                    }
                    else if (hco.base_loc is hardware_stackloc)
                    {
                        /* 1 = [stackloc + a]
                         * 
                         * SCRATCH = stackloc
                         * 1 = [SCRATCH + a]
                         */

                        arm_assign(SCRATCH, hco.base_loc, ret, state);
                        arm_assign(dest, new hardware_contentsof { base_loc = SCRATCH, const_offset = hco.const_offset, size = hco.size }, ret, state);
                    }
                    else
                        throw new NotSupportedException();
                }
                else if (src is hardware_addressoflabel)
                {
                    hardware_addressoflabel aol = src as hardware_addressoflabel;
                    
                    // we cannot guarantee the size of the label, therefore default to 32 bit length
                    // in future we may support different code models where more efficient code can be output
                    ret.AddRange(arm_assign_i4_imm32_gpr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest },
                        new var { hardware_loc = src, label = aol.label, constant_offset = aol.const_offset, address_of = true }, var.Null, null,
                        state, c));
                }
                else if (src is hardware_stackloc)
                {
                    ret.AddRange(arm_assign_i4_mem_gpr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest }, new var { hardware_loc = src },
                        var.Null, null, state, c));
                }
                else if (src is hardware_addressof)
                {
                    hardware_addressof ao = src as hardware_addressof;
                    hardware_location bloc = ao.base_loc;

                    if (bloc is hardware_stackloc)
                    {
                        hardware_contentsof hco = InterpretStackLocation(bloc as hardware_stackloc, state) as hardware_contentsof;
                        /* We need to do:
                         * add dest, hco->base_loc, hco->const_offset
                         */

                        if (hco.const_offset >= 0)
                        {
                            ret.AddRange(arm_add_i4_gpr_gpr_imm(ThreeAddressCode.Op.add_i4, new var { hardware_loc = dest }, new var { hardware_loc = hco.base_loc },
                                new var { hardware_loc = new const_location { c = hco.const_offset }, constant_val = hco.const_offset }, null, state));
                        }
                        else
                        {
                            ret.AddRange(arm_sub_i4_gpr_gpr_imm(ThreeAddressCode.Op.add_i4, new var { hardware_loc = dest }, new var { hardware_loc = hco.base_loc },
                                new var { hardware_loc = new const_location { c = -hco.const_offset }, constant_val = -hco.const_offset }, null, state));
                        }
                    }
                    else
                        throw new NotImplementedException();
                }
                else
                    throw new NotSupportedException();
            }
            else if ((dest is multiple_hardware_location) && (((multiple_hardware_location)dest).hlocs.Length == 2) &&
                ((multiple_hardware_location)dest).hlocs[0].GetType() == typeof(arm_gpr))
            {
                // Dest is { gpr, gpr }
                multiple_hardware_location d = dest as multiple_hardware_location;

                if ((src is multiple_hardware_location) && (((multiple_hardware_location)src).hlocs.Length == 2) &&
                    ((multiple_hardware_location)src).hlocs[0].GetType() == typeof(arm_gpr))
                {
                    // src is { gpr, gpr }
                    multiple_hardware_location s = src as multiple_hardware_location;
                    ret.AddRange(arm_assign_i8_2gpr_2gpr(ThreeAddressCode.Op.assign_i8, new var { hardware_loc = d },
                        new var { hardware_loc = s }, var.Null, null, state, c));
                }
                else if (src is const_location)
                {
                    const_location cloc = src as const_location;
                    ret.AddRange(arm_assign_i8_imm_2gpr(ThreeAddressCode.Op.assign_i8, new var { hardware_loc = d },
                        new var { hardware_loc = src, constant_val = cloc.c }, var.Null, null, state, c));
                }
                else if (src is hardware_stackloc)
                {
                    hardware_location s = InterpretStackLocation(src as hardware_stackloc, state);
                    ret.AddRange(arm_assign_i8_gprptr_2gpr(ThreeAddressCode.Op.assign_i8, new var { hardware_loc = d },
                        new var { hardware_loc = s }, var.Null, null, state, c));
                }
                else
                    throw new NotImplementedException();
            }
            else if (dest is hardware_contentsof)
            {
                hardware_contentsof dest_hco = dest as hardware_contentsof;

                if (dest_hco.base_loc is arm_gpr)
                {
                    // dest is [ gpr ]

                    if (src is arm_gpr)
                    {
                        // src is gpr
                        ret.AddRange(arm_assign_i4_gpr_gprptr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest, constant_offset = dest_hco.const_offset },
                            new var { hardware_loc = src }, var.Null, null, state, c));
                    }
                    else if ((src is multiple_hardware_location) && (((multiple_hardware_location)src).hlocs.Length == 2) &&
                        ((multiple_hardware_location)src).hlocs[0].GetType() == typeof(arm_gpr))
                    {
                        // src is { gpr, gpr }
                        ret.AddRange(arm_assign_i8_2gpr_gprptr(ThreeAddressCode.Op.assign_i8, new var { hardware_loc = dest, constant_offset = dest_hco.const_offset },
                            new var { hardware_loc = src }, var.Null, null, state, c));
                    }
                    else if (src is const_location)
                    {
                        if (dest_hco.size == 4)
                        {
                            // Assign via R12
                            arm_assign(SCRATCH, src, ret, state, c);
                            arm_assign(dest, SCRATCH, ret, state, c);
                        }
                        else if (dest_hco.size == 8)
                        {
                            // First, split the value into two
                            const_location src_cl = src as const_location;
                            byte[] src_arr;
                            if (IsSigned(src_cl.c))
                                src_arr = ToByteArraySignExtend(src_cl.c, 8);
                            else
                                src_arr = ToByteArrayZeroExtend(src_cl.c, 8);

                            byte[] ls_word = new byte[4];
                            byte[] ms_word = new byte[4];
                            Array.Copy(src_arr, 0, ls_word, 0, 4);
                            Array.Copy(src_arr, 4, ms_word, 0, 4);
                            uint ls_val = FromByteArrayU4(ls_word);
                            uint ms_val = FromByteArrayU4(ms_word);
                            const_location ls_src = new const_location { c = ls_val };
                            const_location ms_src = new const_location { c = ms_val };

                            // Determine the source arguments
                            hardware_contentsof ls_dest = new hardware_contentsof { base_loc = dest_hco.base_loc, size = 4, const_offset = dest_hco.const_offset };
                            hardware_contentsof ms_dest = new hardware_contentsof { base_loc = dest_hco.base_loc, size = 4, const_offset = dest_hco.const_offset + 4 };

                            // Do the assignments
                            arm_assign(ls_dest, ls_src, ret, state, c);
                            arm_assign(ms_dest, ms_src, ret, state, c);
                        }
                        else
                            throw new NotImplementedException();
                    }
                    else if ((src is hardware_stackloc) && (((hardware_stackloc)src).size == 4))
                    {
                        // src is word-sized stack location
                        hardware_stackloc s = src as hardware_stackloc;

                        // assign through scratch reg
                        ret.AddRange(arm_assign_i4_mem_gpr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = SCRATCH },
                            new var { hardware_loc = src }, var.Null, null, state, c));
                        ret.AddRange(arm_assign_i4_gpr_gprptr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest },
                            new var { hardware_loc = SCRATCH }, var.Null, null, state, c));
                    }
                    else if ((src is hardware_stackloc) && (((hardware_stackloc)src).size == 8))
                    {
                        hardware_contentsof s = InterpretStackLocation(src as hardware_stackloc, state) as hardware_contentsof;
                        hardware_location s1 = increment_offset(s, 0, 4);
                        hardware_location s2 = increment_offset(s, 4, 4);

                        hardware_location d1 = increment_offset(dest_hco, 0, 4);
                        hardware_location d2 = increment_offset(dest_hco, 4, 4);

                        /* Do:
                         * 
                         * s1 -> SCRATCH -> d1
                         * s2 -> SCRATCH -> d2
                         */

                        arm_assign(SCRATCH, s1, ret, state);
                        arm_assign(d1, SCRATCH, ret, state);
                        arm_assign(SCRATCH, s2, ret, state);
                        arm_assign(d2, SCRATCH, ret, state);
                    }
                    else
                        throw new NotImplementedException();
                }
                else if (dest_hco.base_loc is hardware_stackloc)
                {
                    /* [stackloc + a] = 1
                     * 
                     * do stackloc -> SCRATCH
                     * 1 -> [SCRATCH + a]
                     */

                    arm_assign(SCRATCH, dest_hco.base_loc, ret, state);
                    arm_assign(new hardware_contentsof { base_loc = SCRATCH, const_offset = dest_hco.const_offset, size = dest_hco.size }, src, ret, state);
                }
                else
                    throw new NotImplementedException();
            }
            else if ((dest is hardware_stackloc) && (((hardware_stackloc)dest).size == 4))
            {
                // dest is word-sized stack location
                hardware_stackloc d = dest as hardware_stackloc;

                if (src is arm_gpr)
                {
                    ret.AddRange(arm_assign_i4_gpr_mem(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = dest },
                        new var { hardware_loc = src }, var.Null, null, state, c));
                }
                else
                    throw new NotImplementedException();
            }
            else if ((dest is hardware_stackloc) && (((hardware_stackloc)dest).size == 8))
            {
                // dest is 8 byte stack location
                hardware_stackloc d = dest as hardware_stackloc;

                if ((src is multiple_hardware_location) && (((multiple_hardware_location)src).hlocs.Length == 2) &&
                        ((multiple_hardware_location)src).hlocs[0].GetType() == typeof(arm_gpr))
                {
                    hardware_location d2 = InterpretStackLocation(d, state);
                    ret.AddRange(arm_assign_i8_2gpr_gprptr(ThreeAddressCode.Op.assign_i8, new var { hardware_loc = d2 },
                        new var { hardware_loc = src }, var.Null, null, state, c));
                }
                else
                    throw new NotImplementedException();
            }
            else
                throw new NotImplementedException();
        }

        IEnumerable<OutputBlock> arm_ldmethinfo(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            hardware_location mip = GetMethinfoPointerLocation();
            List<OutputBlock> ret = new List<OutputBlock>();
            arm_assign(result.hardware_loc, mip, ret, state);
            return ret;
        }

        IEnumerable<OutputBlock> arm_peek_u1_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncRnRtOpcode(cond.Always, 0x15, op1.hardware_loc, result.hardware_loc, 0));  // ldrb
        }

        IEnumerable<OutputBlock> arm_peek_u2_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncRnRtOpcode25To27Are0(cond.Always, 0x15, op1.hardware_loc, result.hardware_loc, 0xb0));  // ldrh
        }

        IEnumerable<OutputBlock> arm_peek_i1_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncRnRtOpcode25To27Are0(cond.Always, 0x15, op1.hardware_loc, result.hardware_loc, 0xf0));  // ldrsh
        }

        IEnumerable<OutputBlock> arm_peek_i2_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncRnRtOpcode25To27Are0(cond.Always, 0x15, op1.hardware_loc, result.hardware_loc, 0));  // ldrh
        }

        IEnumerable<OutputBlock> arm_peek_u4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            hardware_contentsof hco = new hardware_contentsof { base_loc = op1.hardware_loc, size = 4, const_offset = 0 };
            return arm_assign_i4_gprptr_gpr(ThreeAddressCode.Op.assign_i4, result, new var { hardware_loc = hco }, var.Null, null, state);
        }

        IEnumerable<OutputBlock> arm_poke_u1_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncRnRtOpcode(cond.Always, 0x14, result.hardware_loc, op1.hardware_loc, 0));  // strb
        }

        IEnumerable<OutputBlock> arm_poke_u2_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncRnRtOpcode25To27Are0(cond.Always, 0x14, result.hardware_loc, op1.hardware_loc, 0xb0));  // strh
        }

        IEnumerable<OutputBlock> arm_poke_u4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            hardware_contentsof hco = new hardware_contentsof { base_loc = op1.hardware_loc, size = 4, const_offset = 0 };
            return arm_assign_i4_gpr_gprptr(ThreeAddressCode.Op.assign_i4, new var { hardware_loc = hco }, op2, var.Null, null, state);
        }

        // The maximum sized VT move to perform before invoking memcpy
        const int MAX_INLINE_MOV = 16;
        IEnumerable<OutputBlock> arm_assign_vt(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            if (tac.VTSize > MAX_INLINE_MOV)
            {
                // TODO: invoke memcpy here (need to calculate addresses first though)
                throw new NotImplementedException();
            }
            else
            {
                // Break the move into 4 byte chunks if small
                List<OutputBlock> ret = new List<OutputBlock>();
                for (int i = 0; i < tac.VTSize; i += 4)
                    arm_assign(increment_offset(result.hardware_loc, i, 4), increment_offset(op1.hardware_loc, i, 4), ret, state);

                return ret;
            }            
        }

        private hardware_location increment_offset(hardware_location hardware_location, int i, int size)
        {
            if (hardware_location is hardware_contentsof)
            {
                hardware_contentsof hco = hardware_location as hardware_contentsof;
                hardware_contentsof new_hco = new hardware_contentsof();
                new_hco.base_loc = hco.base_loc;
                new_hco.const_offset = hco.const_offset + i;
                new_hco.size = size;
                return new_hco;
            }
            else if (hardware_location is hardware_stackloc)
            {
                hardware_stackloc hsl = hardware_location as hardware_stackloc;
                hardware_stackloc new_hsl = new hardware_stackloc();
                new_hsl.loc = hsl.loc;
                new_hsl.offset_within_loc = i;
                new_hsl.size = size;
                return new_hsl;
            }
            else
                throw new NotImplementedException();
        }
    }
}
