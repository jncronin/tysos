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

namespace tydisasm.arm
{
    partial class arm_disasm
    {
        const uint cond_EQ = 0;
        const uint cond_NE = 1;
        const uint cond_CS = 2;
        const uint cond_CC = 3;
        const uint cond_MI = 4;
        const uint cond_PL = 5;
        const uint cond_VS = 6;
        const uint cond_VC = 7;
        const uint cond_HI = 8;
        const uint cond_LS = 9;
        const uint cond_GE = 10;
        const uint cond_LT = 11;
        const uint cond_GT = 12;
        const uint cond_LE = 13;
        const uint cond_AL = 14;
        const uint cond_uncond = 15;

        internal enum SRType { SRType_LSL, SRType_LSR, SRType_ASR, SRType_ROR, SRType_RRX };
        internal class ImmShiftRet
        {
            internal SRType type;
            internal uint amount;
        }
        ImmShiftRet DecodeImmShift(uint type, uint imm5)
        {
            ImmShiftRet ret = new ImmShiftRet();
            switch (type)
            {
                case 0:
                    ret.type = SRType.SRType_LSL;
                    ret.amount = imm5;
                    break;
                case 1:
                    ret.type = SRType.SRType_LSR;
                    if (imm5 == 0)
                        ret.amount = 32;
                    else
                        ret.amount = imm5;
                    break;
                case 2:
                    ret.type = SRType.SRType_ASR;
                    if (imm5 == 0)
                        ret.amount = 32;
                    else
                        ret.amount = imm5;
                    break;
                case 3:
                    if (imm5 == 0)
                    {
                        ret.type = SRType.SRType_RRX;
                        ret.amount = 1;
                    }
                    else
                    {
                        ret.type = SRType.SRType_ROR;
                        ret.amount = imm5;
                    }
                    break;
            }
            if (ret.amount == 0)
                return null;
            return ret;
        }

        internal static string[] cond_names = { "eq", "ne", "cs", "cc", "mi", "pl", "vs", "vc", "hi", "ls", "ge", "lt", "gt", "le", "", "" };

        uint ExtractCond(uint opcode)
        { return (opcode >> 28) & 0xf; }
        uint ExtractOp1(uint opcode)
        { return (opcode >> 25) & 0x7; }
        uint ExtractOp(uint opcode)
        { return (opcode >> 4) & 0x1; }
        uint ExtractR16(uint opcode)
        { return (opcode >> 16) & 0xf; }
        uint ExtractR8(uint opcode)
        { return (opcode >> 8) & 0xf; }
        uint Extract5at20(uint opcode)
        { return (opcode >> 20) & 0x1f; }
        uint Extract5at7(uint opcode)
        { return (opcode >> 7) & 0x1f; }
        uint ExtractR12(uint opcode)
        { return (opcode >> 12) & 0xf; }
        uint ExtractR4(uint opcode)
        { return (opcode >> 4) & 0xf; }
        uint ExtractR0(uint opcode)
        { return opcode & 0xf; }
        uint ExtractImm12(uint opcode)
        { return opcode & 0xfff; }

        bool IsSet(uint opcode, int bit)
        {
            return ((opcode >> bit) & 0x1) == 0x1;
        }
        bool IsUnset(uint opcode, int bit)
        {
            return ((opcode >> bit) & 0x1) == 0;
        }
        bool Match(uint opcode, string match)
        {
            int shift = 0;
            while(shift < match.Length)
            {
                char m_c = match[match.Length - shift - 1];

                if(m_c == '0')
                {
                    if(!IsUnset(opcode, shift))
                        return false;
                }
                else if(m_c == '1')
                {
                    if(!IsSet(opcode, shift))
                        return false;
                }
                else if(m_c != 'x')
                    throw new NotSupportedException();
                shift++;
            }
            opcode >>= shift;
            if(opcode != 0)
                return false;
            return true;
        }

        void InterpretOpcode(uint opcode, arm_line line)
        {
            switch (ExtractCond(opcode))
            {
                case cond_uncond:
                    InterpretUnconditionalInstruction(opcode, line);
                    break;
                default:
                    InterpretConditionalInstruction(opcode, line);
                    break;
            }
        }

        private void InterpretConditionalInstruction(uint opcode, arm_line line)
        {
            line.cond = ExtractCond(opcode);

            switch (ExtractOp1(opcode))
            {
                case 0:
                case 1:
                    InterpretDataProcessingInstruction(opcode, line);
                    break;
                case 2:
                    InterpretLoadStoreInstruction(opcode, line);
                    break;
                case 3:
                    switch (ExtractOp(opcode))
                    {
                        case 0:
                            InterpretLoadStoreInstruction(opcode, line);
                            break;
                        case 1:
                            InterpretMediaInstruction(opcode, line);
                            break;
                    }
                    break;
                case 4:
                case 5:
                    InterpretBranchInstruction(opcode, line);
                    break;
                case 6:
                case 7:
                    InterpretCoprocessorInstruction(opcode, line);
                    break;
            }
        }

        private void InterpretCoprocessorInstruction(uint opcode, arm_line line)
        {
            throw new NotImplementedException();
        }

        private void InterpretBranchInstruction(uint opcode, arm_line line)
        {
            uint bit25 = (opcode >> 25) & 0x1;
            uint op = Extract5at20(opcode);
            uint Rn = ExtractR16(opcode);
            uint R = (opcode >> 15) & 0x1;
            uint reg_list = opcode & 0xffff;
            arm_location rl = new arm_location { type = location.location_type.Register, reg_list = true, reg_no = reg_list };

            switch (bit25)
            {
                case 0:
                    switch (op)
                    {
                        case 0:
                        case 2:
                            // STMDA
                            throw new NotImplementedException();
                        case 1:
                        case 3:
                            // LDMDA/LDMFA
                            throw new NotImplementedException();
                        case 0x8:
                        case 0xa:
                            // STM (STMIA/STMEA)
                            line.name = "stmia";
                            uint wback = (opcode >> 21) & 0x1;
                            arm_location.arm_type at = arm_location.arm_type.Offset;
                            if(wback == 1)
                                at = arm_location.arm_type.Preindex;

                            line.arguments = new location[] { new arm_location { type = location.location_type.ContentsOf, reg_no = Rn,
                                arm_const_index_type = at, signed_immediate = 0, args = new location[] { new arm_location {
                                    type = location.location_type.Register, reg_no = Rn }}}, rl };
                            break;
                        case 0x9:
                            // LDM/LDMIA/LDMFD (ARM)
                            line.name = "ldmia";
                            line.arguments = new location[] { new arm_location { type = location.location_type.ContentsOf, reg_no = Rn,
                                arm_const_index_type = arm_location.arm_type.Offset, signed_immediate = 0, args = new location[] { new arm_location {
                                    type = location.location_type.Register, reg_no = Rn }}}, rl };
                            break;
                        case 0xb:
                            switch (Rn)
                            {
                                case 0xb:
                                    // POP multiple (ARM)
                                    throw new NotImplementedException();
                                default:
                                    // LDM/LDMIA/LDMFD (ARM)
                                    line.name = "ldmia";
                                    line.arguments = new location[] { new arm_location { type = location.location_type.ContentsOf, reg_no = Rn,
                                        arm_const_index_type = arm_location.arm_type.Preindex, signed_immediate = 0, args = new location[] { new arm_location {
                                            type = location.location_type.Register, reg_no = Rn }}}, rl };
                                    break;
                            }
                            break;
                        case 0x10:
                            // STMBD (STMFD)
                            throw new NotImplementedException();
                        case 0x12:
                            switch (Rn)
                            {
                                case 0xb:
                                    // PUSH multiple (ARM)
                                    throw new NotImplementedException();
                                default:
                                    // STMDB (STMFD)
                                    throw new NotImplementedException();
                            }
                            //break;
                        case 0x11:
                        case 0x13:
                            // LDMDB/LDMEA
                            throw new NotImplementedException();
                        case 0x18:
                        case 0x1a:
                            // STMIB (STMFA)
                            throw new NotImplementedException();
                        case 0x19:
                        case 0x1b:
                            // LDMIB/LDMED
                            throw new NotImplementedException();
                        case 0x4:
                        case 0x6:
                        case 0xc:
                        case 0xe:
                        case 0x14:
                        case 0x16:
                        case 0x1c:
                        case 0x1e:
                            // STM (user registers)
                            throw new NotImplementedException();
                        case 0x5:
                        case 0x7:
                        case 0xd:
                        case 0xf:
                        case 0x15:
                        case 0x17:
                        case 0x1d:
                        case 0x1f:
                            switch (R)
                            {
                                case 0:
                                    // LDM (user registers)
                                    throw new NotImplementedException();
                                case 1:
                                    // LDM (exception return)
                                    throw new NotImplementedException();
                            }
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;
                case 1:
                    // B/BL
                    InterpretBBLInstruction(opcode, line);
                    break;
            }
        }

        private void InterpretBBLInstruction(uint opcode, arm_line line)
        {
            uint bit24 = (opcode >> 24) & 0x1;
            uint imm24 = (opcode & 0xffffff);
            uint cond = ExtractCond(opcode);

            uint imm_val = 0;
            uint imm_se = 0xffffffff << 26;

            if (cond == cond_uncond)
            {
                // BLX
                line.name = "blx";
                imm_val = (imm24 << 2) | (bit24 << 1) | 0x0;
            }
            else if (bit24 == 1)
            {
                // BL
                line.name = "bl";
                imm_val = imm24 << 2;
            }
            else
            {
                // B
                line.name = "b";
                imm_val = imm24 << 2;
            }

            // Sign extend imm_val
            if (((imm_val >> 26) & 0x1) == 0x1)
                imm_val |= imm_se;

            int offset = BitConverter.ToInt32(BitConverter.GetBytes(imm_val), 0);
            offset += 4;

            line.arguments = new location[] { new arm_location { type = location.location_type.SignedImmediate, signed_immediate = offset, is_pc_relative = true } };
        }

        private void InterpretMediaInstruction(uint opcode, arm_line line)
        {
            throw new NotImplementedException();
        }

        private void InterpretLoadStoreInstruction(uint opcode, arm_line line)
        {
            uint B = ExtractOp(opcode);
            uint A = ExtractOp1(opcode) & 0x1;
            uint Rn = ExtractR16(opcode);
            uint op1 = Extract5at20(opcode);
            uint W = (op1 >> 1) & 0x1;
            uint U = (op1 >> 3) & 0x1;
            uint P = (op1 >> 4) & 0x1;

            switch (op1)
            {
                case 0:
                case 0x8:
                case 0x10:
                case 0x12:
                case 0x18:
                case 0x1a:
                    line.name = "str";
                    switch (A)
                    {
                        case 0:
                            // STR (immediate, ARM)
                            uint Rt = ExtractR12(opcode);
                            uint imm12 = ExtractImm12(opcode);
                            bool add = (U == 1);
                            int simm12 = (int)imm12;
                            if(!add)
                                simm12 = -simm12;
                            bool index = (P == 1);
                            bool wback = (P == 0) || (W == 1);


                            if ((Rn == 0xd) && (P == 1) && (U == 0) && (W == 1) && (imm12 == 4))
                            {
                                line.name = "push";
                                line.arguments = new location[] { new location { reg_no = Rt, type = location.location_type.Register } };
                            }
                            else
                            {
                                line.name = "str";
                                arm_location.arm_type at = arm_location.arm_type.None;

                                if (index && !wback)
                                {
                                    // offset
                                    at = arm_location.arm_type.Offset;
                                }
                                else if (index && wback)
                                {
                                    // pre-indexed
                                    at = arm_location.arm_type.Preindex;
                                }
                                else
                                {
                                    // post-indexed
                                    at = arm_location.arm_type.Postindex;
                                }
                                line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rt },
                                            new arm_location { type = location.location_type.ContentsOf, arm_const_index_type = at,
                                                reg_no = Rn, signed_immediate = simm12, args = new location[] { new arm_location { type = location.location_type.Register,
                                                reg_no = Rn }}}};
                            }
                            break;
                        case 1:
                            // STR (register)
                            throw new NotImplementedException();
                        default:
                            throw new NotSupportedException();
                    }
                    break;
                case 0x2:
                case 0xa:
                    // STRT
                    throw new NotImplementedException();
                case 0x1:
                case 0x9:
                case 0x11:
                case 0x13:
                case 0x19:
                case 0x1b:
                    line.name = "ldr";
                    switch (A)
                    {
                        case 0:
                            uint Rt = ExtractR12(opcode);
                            uint imm12 = ExtractImm12(opcode);
                            bool add = (U == 1);
                            int simm12 = (int)imm12;
                            if(!add)
                                simm12 = -simm12;

                            switch (Rn)
                                {
                                case 0xf:
                                    // LDR (literal)
                                    line.name = "ldr";
                                    line.arguments = new location[] { new arm_location { reg_no = Rt, type = location.location_type.Register },
                                        new arm_location { type = location.location_type.ContentsOf, reg_no = 0xf, arm_const_index_type = arm_location.arm_type.Offset,
                                            signed_immediate = simm12, args = new location[] { new arm_location { type = location.location_type.Register,
                                                reg_no = 0xf }}}};
                                    break;                                    
                                default:
                                    // LDR (immediate)

                                    if ((Rn == 0xd) && (P == 0) && (U == 1) && (W == 0) && (imm12 == 4))
                                    {
                                        line.name = "pop";
                                        line.arguments = new location[] { new location { reg_no = Rt, type = location.location_type.Register } };
                                    }
                                    else
                                    {
                                        bool index = (P == 1);
                                        bool wback = (P == 0) || (W == 1);

                                        line.name = "ldr";
                                        arm_location.arm_type at = arm_location.arm_type.None;

                                        if (index && !wback)
                                        {
                                            // offset
                                            at = arm_location.arm_type.Offset;
                                        }
                                        else if(index && wback)
                                        {
                                            // pre-indexed
                                            at = arm_location.arm_type.Preindex;
                                        }
                                        else
                                        {
                                            // post-indexed
                                            at = arm_location.arm_type.Postindex;
                                        }
                                        line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rt },
                                            new arm_location { type = location.location_type.ContentsOf, arm_const_index_type = at,
                                                reg_no = Rn, signed_immediate = simm12, args = new location[] { new arm_location { type = location.location_type.Register,
                                                reg_no = Rn }}}};
                                    }
                                    break;
                            }
                            break;
                        case 1:
                            // LDR (register, ARM)
                            throw new NotImplementedException();
                    }
                    break;
                case 0x3:
                case 0xb:
                    // LDRT
                    throw new NotImplementedException();
                case 0x4:
                case 0xc:
                case 0x14:
                case 0x16:
                case 0x1c:
                case 0x1e:
                    switch (A)
                    {
                        case 0:
                            // STRB (immediate, ARM)
                            throw new NotImplementedException();
                        case 1:
                            // STRB (register)
                            throw new NotImplementedException();
                    }
                    break;
                case 0x6:
                case 0xe:
                    // STRBT
                    throw new NotImplementedException();
                case 0x5:
                case 0xd:
                case 0x15:
                case 0x17:
                case 0x1d:
                case 0x1f:
                    switch (A)
                    {
                        case 0:
                            switch (Rn)
                            {
                                case 0xf:
                                    // LDRB (literal)
                                    throw new NotImplementedException();
                                default:
                                    // LDRB (immediate, ARM)
                                    throw new NotImplementedException();
                            }
                            //break;
                        case 1:
                            // LDRB (register)
                            throw new NotImplementedException();
                    }
                    break;
                case 0x7:
                case 0xf:
                    // LDRBT
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException();
            }
        }

        private void InterpretDataProcessingInstruction(uint opcode, arm_line line)
        {
            uint op1 = Extract5at20(opcode);
            uint op2 = ExtractR4(opcode);
            uint op = ExtractOp1(opcode) & 0x1;
            //uint Rn = ExtractR16(opcode);
            //uint W = (op1 >> 1) & 0x1;
            //uint U = (op1 >> 3) & 0x1;
            //uint P = (op1 >> 4) & 0x1;

            switch (op)
            {
                case 0:
                    if (!Match(op1, "10xx0"))
                    {
                        if (Match(op2, "xxx0"))
                        {
                            InterpretDataProcessingRegisterInstruction(opcode, line);
                            return;
                        }
                        else if (Match(op2, "0xx1"))
                        {
                            InterpretDataProcessingRegisterShiftedRegisterInstruction(opcode, line);
                            return;
                        }
                    }
                    if (Match(op1, "10xx0"))
                    {
                        if (Match(op2, "0xxx"))
                        {
                            InterpretMiscellaneousInstruction(opcode, line);
                            return;
                        }
                        else if (Match(op2, "1xx0"))
                        {
                            InterpretHalfwordMultiplyInstruction(opcode, line);
                            return;
                        }
                    }
                    if (Match(op1, "0xxxx"))
                    {
                        if (Match(op2, "1001"))
                        {
                            InterpretMultiplyInstruction(opcode, line);
                            return;
                        }
                    }
                    if (Match(op1, "1xxxx"))
                    {
                        if (Match(op2, "1001"))
                        {
                            InterpretSynchronizationPrimitiveInstruction(opcode, line);
                            return;
                        }
                    }
                    if (!Match(op1, "0xx1x"))
                    {
                        if (Match(op2, "1011"))
                        {
                            InterpretExtraLoadStoreInstruction(opcode, line);
                            return;
                        }
                        else if (Match(op2, "11x1"))
                        {
                            InterpretExtraLoadStoreInstruction(opcode, line);
                            return;
                        }
                    }
                    if (Match(op1, "0xx1x"))
                    {
                        if (Match(op2, "1011"))
                        {
                            InterpretExtraLoadStoreInstruction(opcode, line);
                            return;
                        }
                        else if (Match(op2, "11x1"))
                        {
                            InterpretExtraLoadStoreInstruction(opcode, line);
                            return;
                        }
                    }
                    break;
                case 1:
                    if (!Match(op1, "10xx0"))
                    {
                        InterpretDataProcessingImmediateInstruction(opcode, line);
                        return;
                    }
                    if (Match(op1, "10000"))
                    {
                        // 16 bit immediate load MOV (immediate)
                        throw new NotImplementedException();
                    }
                    if (Match(op1, "10100"))
                    {
                        // High halfword 16-bit immediate load MOVT
                        throw new NotImplementedException();
                    }
                    if (Match(op1, "10x10"))
                    {
                        // MSR (immediate) and hints
                        throw new NotImplementedException();
                    }
                    break;
            }
            throw new NotSupportedException();                    
        }

        private void InterpretDataProcessingImmediateInstruction(uint opcode, arm_line line)
        {
            uint op = Extract5at20(opcode);
            uint Rn = ExtractR16(opcode);
            uint S = op & 0x1;
            uint imm12 = ExtractImm12(opcode);
            uint Rd = ExtractR12(opcode);

            switch (op)
            {
                case 0:
                case 1:
                    // AND (immediate)
                    line.name = "and";
                    if (S == 1)
                        line.name = "ands";
                    line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rd },
                        new arm_location { type = location.location_type.Register, reg_no = Rn },
                        new arm_location { type = location.location_type.Immediate, immediate = imm12 }};
                    break;
                case 2:
                case 3:
                    // EOR (immediate)
                    throw new NotImplementedException();
                case 4:
                case 5:
                    switch (Rn)
                    {
                        case 0xf:
                            // ADR
                            throw new NotImplementedException();
                        default:
                            // SUB (immediate, ARM)
                            line.name = "sub";
                            if (S == 1)
                                line.name = "subs";
                            line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rd },
                                new arm_location { type = location.location_type.Register, reg_no = Rn },
                                new arm_location { type = location.location_type.Immediate, immediate = imm12 }};
                            break;
                    }
                    break;
                case 6:
                case 7:
                    // RSB (immediate)
                    throw new NotImplementedException();
                case 8:
                case 9:
                    switch (Rn)
                    {
                        case 0xf:
                            // ADR
                            line.name = "adr";
                            if (S == 1)
                                throw new NotSupportedException();
                            line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rd },
                                new arm_location { type = location.location_type.Immediate, immediate = imm12 + 4, is_pc_relative = true }};
                            break;
                        default:
                            // ADD (immediate, ARM)
                            line.name = "add";
                            if (S == 1)
                                line.name = "adds";
                            line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rd },
                                new arm_location { type = location.location_type.Register, reg_no = Rn },
                                new arm_location { type = location.location_type.Immediate, immediate = imm12 }};
                            break;
                    }
                    break;
                case 10:
                case 11:
                    // ADC (immediate)
                    line.name = "adc";
                    if (S == 1)
                        line.name = "adcs";
                    line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rd },
                        new arm_location { type = location.location_type.Register, reg_no = Rn },
                        new arm_location { type = location.location_type.Immediate, immediate = imm12 }};
                    break;
                case 12:
                case 13:
                    // SBC (immediate)
                    throw new NotImplementedException();
                case 14:
                case 15:
                    // RSC (immediate)
                    throw new NotImplementedException();
                case 17:
                    // TST (immediate)
                    throw new NotImplementedException();
                case 19:
                    // TEQ (immediate)
                    throw new NotImplementedException();
                case 21:
                    // CMP (immediate)
                    line.name = "cmp";
                    line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rn },
                        new arm_location { type = location.location_type.Immediate, immediate = imm12 }};
                    break;
                case 23:
                    // CMN (immediate)
                    throw new NotImplementedException();
                case 24:
                case 25:
                    // ORR (immediate)
                    line.name = "orr";
                    if (S == 1)
                        line.name = "orrs";
                    line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rd },
                        new arm_location { type = location.location_type.Register, reg_no = Rn },
                        new arm_location { type = location.location_type.Immediate, immediate = imm12 }};
                    break;
                case 26:
                case 27:
                    // MOV (immediate)
                    if (S == 0)
                        line.name = "mov";
                    else
                        line.name = "movs";

                    line.arguments = new location[] { new location { type = location.location_type.Register, reg_no = Rd },
                        new arm_location { type = location.location_type.Immediate, immediate = imm12 }};
                    break;
                case 28:
                case 29:
                    // BIC (immediate)
                    throw new NotImplementedException();
                case 30:
                case 31:
                    // MVN (immediate)
                    if (S == 0)
                        line.name = "mvn";
                    else
                        line.name = "mvns";

                    line.arguments = new location[] { new location { type = location.location_type.Register, reg_no = Rd },
                        new arm_location { type = location.location_type.Immediate, immediate = imm12 }};
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void InterpretExtraLoadStoreInstruction(uint opcode, arm_line line)
        {
            throw new NotImplementedException();
        }

        private void InterpretSynchronizationPrimitiveInstruction(uint opcode, arm_line line)
        {
            throw new NotImplementedException();
        }

        private void InterpretMultiplyInstruction(uint opcode, arm_line line)
        {
            uint op = Extract5at20(opcode);
            uint S = (opcode >> 20) & 0x1;
            uint Rd = ExtractR16(opcode);
            uint Rm = ExtractR8(opcode);
            uint Rn = ExtractR0(opcode);

            switch (op)
            {
                case 0:
                case 0x1:
                    // MUL
                    line.name = "mul";
                    if (S == 1)
                        line.name = "muls";
                    line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rd },
                        new arm_location { type = location.location_type.Register, reg_no = Rn },
                        new arm_location { type = location.location_type.Register, reg_no = Rm }};
                    break;                    
                case 0x2:
                case 0x3:
                    // MLA
                    throw new NotImplementedException();
                case 0x4:
                    // UMAAL
                    throw new NotImplementedException();
                case 0x6:
                    // MLS
                    throw new NotImplementedException();
                case 0x8:
                case 0x9:
                    // UMULL
                    throw new NotImplementedException();
                case 0xa:
                case 0xb:
                    // UMLAL
                    throw new NotImplementedException();
                case 0xc:
                case 0xd:
                    // SMULL
                    throw new NotImplementedException();
                case 0xe:
                case 0xf:
                    // SMLAL
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException();
            }
        }

        private void InterpretHalfwordMultiplyInstruction(uint opcode, arm_line line)
        {
            throw new NotImplementedException();
        }

        private void InterpretMiscellaneousInstruction(uint opcode, arm_line line)
        {
            uint op = (opcode >> 21) & 0x3;
            uint op1 = ExtractR16(opcode);
            uint B = (opcode >> 9) & 0x1;
            uint op2 = ExtractR4(opcode);

            switch (op2)
            {
                case 0:
                    switch (B)
                    {
                        case 1:
                            switch (op)
                            {
                                case 0:
                                case 2:
                                    // MRS (banked register)
                                    throw new NotImplementedException();
                                case 1:
                                case 3:
                                    // MSR (banked register)
                                    throw new NotImplementedException();
                            }
                            break;
                        case 0:
                            switch (op)
                            {
                                case 0:
                                case 2:
                                    // MRS
                                    throw new NotImplementedException();
                                case 1:
                                case 3:
                                    // MSR (register)
                                    throw new NotImplementedException();
                            }
                            break;
                    }
                    break;
                case 1:
                    switch (op)
                    {
                        case 1:
                            // BX
                            throw new NotImplementedException();
                        case 3:
                            // CLZ
                            throw new NotImplementedException();
                        default:
                            throw new NotSupportedException();
                    }
                    //break;
                case 2:
                    switch (op)
                    {
                        case 1:
                            // BXJ
                            throw new NotImplementedException();
                        default:
                            throw new NotSupportedException();
                    }
                    //break;
                case 3:
                    switch (op)
                    {
                        case 1:
                            // BLX (register)
                            line.name = "blx";
                            line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = ExtractR0(opcode) } };
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;
                case 5:
                    // Saturating add and subtraction
                    throw new NotImplementedException();
                case 6:
                    switch (op)
                    {
                        case 3:
                            // ERET
                            throw new NotImplementedException();
                        default:
                            throw new NotSupportedException();
                    }
                    //break;
                case 7:
                    switch (op)
                    {
                        case 1:
                            // BKPT
                            throw new NotImplementedException();
                        case 2:
                            // HVC
                            throw new NotImplementedException();
                        case 3:
                            // SMC
                            throw new NotImplementedException();
                        default:
                            throw new NotSupportedException();
                    }
                    //break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void InterpretDataProcessingRegisterShiftedRegisterInstruction(uint opcode, arm_line line)
        {
            throw new NotImplementedException();
        }

        private void InterpretDataProcessingRegisterInstruction(uint opcode, arm_line line)
        {
            uint op = Extract5at20(opcode);
            uint imm5 = Extract5at7(opcode);
            uint op2 = (ExtractR4(opcode) >> 1) & 0x3;
            ImmShiftRet shift = DecodeImmShift(op2, imm5);
            uint rm = ExtractR0(opcode);
            uint rn = ExtractR16(opcode);
            uint S = op & 0x1;
            uint rd = ExtractR12(opcode);

            switch (op)
            {
                case 0:
                case 1:
                    // AND (register)
                    line.name = "and";
                    if (S == 1)
                        line.name = "ands";
                    line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = rd },
                        new arm_location { type = location.location_type.Register, reg_no = rn },
                        new arm_location { type = location.location_type.Register, reg_no = rm, immshift = shift }};
                    break;
                case 2:
                case 3:
                    // EOR (register)
                    throw new NotImplementedException();
                case 4:
                case 5:
                    // SUB (register)
                    line.name = "sub";
                    if (S == 1)
                        line.name = "subs";
                    line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = rd },
                        new arm_location { type = location.location_type.Register, reg_no = rn },
                        new arm_location { type = location.location_type.Register, reg_no = rm, immshift = shift }};
                    break;
                case 6:
                case 7:
                    // RSB (register)
                    throw new NotImplementedException();
                case 8:
                case 9:
                    // ADD (register, ARM)
                    line.name = "add";
                    if (S == 1)
                        line.name = "adds";
                    line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = rd },
                        new arm_location { type = location.location_type.Register, reg_no = rn },
                        new arm_location { type = location.location_type.Register, reg_no = rm, immshift = shift }};
                    break;
                case 10:
                case 11:
                    // ADC (register)
                    throw new NotImplementedException();
                case 12:
                case 13:
                    // SBC (register)
                    throw new NotImplementedException();
                case 14:
                case 15:
                    // RSC (register)
                    throw new NotImplementedException();
                case 17:
                    // TST (register)
                    throw new NotImplementedException();
                case 19:
                    // TEQ (register)
                    throw new NotImplementedException();
                case 21:
                    // CMP (register)
                    line.name = "cmp";
                    line.arguments = new location[] { new location { type = location.location_type.Register, reg_no = rn },
                        new arm_location { type = location.location_type.Register, reg_no = rm, immshift = shift }};
                    break;
                case 23:
                    // CMN (register)
                    throw new NotImplementedException();
                case 24:
                case 25:
                    // ORR (register)
                    throw new NotImplementedException();
                case 26:
                case 27:
                    switch (op2)
                    {
                        case 0:
                            switch (imm5)
                            {
                                case 0:
                                    // MOV (register, ARM)
                                    {
                                        uint Rd = ExtractR12(opcode);
                                        line.name = "mov";
                                        if (S == 1)
                                            line.name = "movs";

                                        line.arguments = new location[] { new arm_location { type = location.location_type.Register, reg_no = Rd },
                                            new arm_location { type = location.location_type.Register, reg_no = rm }};
                                    }
                                    break;
                                default:
                                    // LSL (immediate)
                                    throw new NotImplementedException();
                            }
                            break;
                        case 1:
                            // LSR (immediate)
                            throw new NotImplementedException();
                        case 2:
                            // ASR (immediate)
                            throw new NotImplementedException();
                        case 3:
                            switch (imm5)
                            {
                                case 0:
                                    // RRX
                                    throw new NotImplementedException();
                                default:
                                    // ROR (immediate)
                                    throw new NotImplementedException();
                            }
                            //break;
                    }
                    break;
                case 28:
                case 29:
                    // BIC (register)
                    throw new NotImplementedException();
                case 30:
                case 31:
                    // MVN (register)
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException();
            }
        }

        private void InterpretUnconditionalInstruction(uint opcode, arm_line line)
        {
            throw new NotImplementedException();
        }
    }
}
