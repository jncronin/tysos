/* Copyright (C) 2008 - 2013 by John Cronin
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
        public static arm_gpr R0 { get { return new arm_gpr { reg = arm_gpr.RegId.r0 }; } }
        public static arm_gpr R1 { get { return new arm_gpr { reg = arm_gpr.RegId.r1 }; } }
        public static arm_gpr R2 { get { return new arm_gpr { reg = arm_gpr.RegId.r2 }; } }
        public static arm_gpr R3 { get { return new arm_gpr { reg = arm_gpr.RegId.r3 }; } }
        public static arm_gpr R4 { get { return new arm_gpr { reg = arm_gpr.RegId.r4 }; } }
        public static arm_gpr R5 { get { return new arm_gpr { reg = arm_gpr.RegId.r5 }; } }
        public static arm_gpr R6 { get { return new arm_gpr { reg = arm_gpr.RegId.r6 }; } }
        public static arm_gpr R7 { get { return new arm_gpr { reg = arm_gpr.RegId.r7 }; } }
        public static arm_gpr R8 { get { return new arm_gpr { reg = arm_gpr.RegId.r8 }; } }
        public static arm_gpr R9 { get { return new arm_gpr { reg = arm_gpr.RegId.r9 }; } }
        public static arm_gpr R10 { get { return new arm_gpr { reg = arm_gpr.RegId.r10 }; } }
        public static arm_gpr R11 { get { return new arm_gpr { reg = arm_gpr.RegId.r11 }; } }
        public static arm_gpr R12 { get { return new arm_gpr { reg = arm_gpr.RegId.r12 }; } }
        public static arm_gpr SP { get { return new arm_gpr { reg = arm_gpr.RegId.sp }; } }
        public static arm_gpr LR { get { return new arm_gpr { reg = arm_gpr.RegId.lr }; } }
        public static arm_gpr PC { get { return new arm_gpr { reg = arm_gpr.RegId.pc }; } }
        public static multiple_hardware_location R0R1 { get { return new multiple_hardware_location { hlocs = new arm_gpr[] { R0, R1 } }; } }
        public static arm_gpr SL { get { return R10; } }
        public static arm_gpr FP { get { return R11; } }
        public static arm_gpr SCRATCH { get { return R12; } }

        public static arm_gpr Rx(int x) { return new arm_gpr { reg = (arm_gpr.RegId)x }; }

        hloc_constraint CR0 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R0 }; } }
        hloc_constraint CR1 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R1 }; } }
        hloc_constraint CR2 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R2 }; } }
        hloc_constraint CR3 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R3 }; } }
        hloc_constraint CR4 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R4 }; } }
        hloc_constraint CR5 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R5 }; } }
        hloc_constraint CR6 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R6 }; } }
        hloc_constraint CR7 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R7 }; } }
        hloc_constraint CR8 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R8 }; } }
        hloc_constraint CR9 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R9 }; } }
        hloc_constraint CR10 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R10 }; } }
        hloc_constraint CR11 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R11 }; } }
        hloc_constraint CR12 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R12 }; } }
        hloc_constraint CSP { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = SP }; } }
        hloc_constraint CLR { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = LR }; } }
        hloc_constraint CPC { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = PC }; } }
        hloc_constraint CR0R1 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R0R1 }; } }

        hloc_constraint CGpr { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new arm_gpr() }; } }
        hloc_constraint C2Gpr { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new multiple_hardware_location { hlocs = new arm_gpr[2] } }; } }
        hloc_constraint CMem { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_stackloc() }; } }
        hloc_constraint CMem32 { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_stackloc { size = 4 } }; } }
        hloc_constraint CMem64 { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_stackloc { size = 8 } }; } }
        hloc_constraint COp1 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Operand1 }; } }
        hloc_constraint COp2 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Operand2 }; } }
        hloc_constraint CNone { get { return new hloc_constraint { constraint = hloc_constraint.c_.None }; } }
        hloc_constraint CConst { get { return new hloc_constraint { constraint = hloc_constraint.c_.Immediate }; } }
        hloc_constraint CConstByte { get { return new hloc_constraint { constraint = hloc_constraint.c_.Immediate, const_bitsize = 8 }; } }
        hloc_constraint CConstInt12 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Immediate, const_bitsize = 12 }; } }
        hloc_constraint CConstInt16 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Immediate, const_bitsize = 16 }; } }
        hloc_constraint CConstInt32 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Immediate, const_bitsize = 32 }; } }
        hloc_constraint CConstInt32Label { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CConstInt32, CLabel } }; } }
        hloc_constraint CGprMem { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem } }; } }
        hloc_constraint CGprMem32 { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem32 } }; } }
        hloc_constraint CGprConst { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CConst } }; } }

        hloc_constraint CGprPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_contentsof { base_loc = new arm_gpr() } }; } }
        hloc_constraint CStackPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_addressof { base_loc = new hardware_stackloc() } }; } }
        hloc_constraint CLabel { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_addressoflabel() }; } }
        hloc_constraint CGprMemPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem, CGprPtr } }; } }
        hloc_constraint CGprMemConst { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem, CConst } }; } }
        hloc_constraint CMemPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CMem, CGprPtr } }; } }

        hloc_constraint CAny { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem, CGprPtr, CStackPtr, CConst, C2Gpr } }; } }

        enum cond
        {
            Equal = 0, NotEqual = 1, CarrySet = 2, CarryClear = 3, MinusNegative = 4, PlusPositiveZero = 5, Overflow = 6,
            NoOverflow = 7, UnsignedHigher = 8, UnsignedLowerSame = 9, SignedGreaterEqual = 10, SignedLess = 11, SignedGreater = 12,
            SignedLessEqual = 13, Always = 14, Unspecified = 15
        };

        OutputBlock EncDPROpcode(cond c, uint op1, hardware_location rn, hardware_location rd, uint imm5, uint op2, hardware_location rm)
        {
            uint ret = ((uint)c << 28) | (op1 << 20) | ((uint)((arm_gpr)rn).reg << 16) | ((uint)((arm_gpr)rd).reg << 12) | (imm5 << 7)
                | (op2 << 4) | ((uint)((arm_gpr)rm).reg);
            return new CodeBlock(ToByteArray(ret));
        }
        OutputBlock EncRdRtRmRnOpcode(cond c, uint op1, hardware_location rd, hardware_location rt, hardware_location rm, uint imm4, hardware_location rn)
        {
            uint ret = ((uint)c << 28) | (op1 << 20) | ((uint)((arm_gpr)rd).reg << 16) | ((uint)((arm_gpr)rt).reg << 12) |
                ((uint)((arm_gpr)rm).reg << 8) | (imm4 << 4) | ((uint)((arm_gpr)rn).reg);
            return new CodeBlock(ToByteArray(ret));
        }
        OutputBlock EncImmOpcode(cond c, uint op1, uint imm4, hardware_location rd, uint imm12)
        {
            uint ret = ((uint)c << 28) | (1 << 25) | (op1 << 20) | (imm4 << 16) | ((uint)((arm_gpr)rd).reg << 12) | imm12;
            return new CodeBlock(ToByteArray(ret));
        }
        OutputBlock EncImmOpcode(cond c, uint op1, hardware_location rn, hardware_location rd, uint imm12)
        {
            uint ret = ((uint)c << 28) | (1 << 25) | (op1 << 20) | ((uint)((arm_gpr)rn).reg << 16) | ((uint)((arm_gpr)rd).reg << 12) | imm12;
            return new CodeBlock(ToByteArray(ret));
        }
        OutputBlock EncRegListOpcode(cond c, uint op1, hardware_location rn, hardware_location[] regs)
        {
            uint ret = ((uint)c << 28) | (4 << 25) | (op1 << 20) | ((uint)((arm_gpr)rn).reg << 16);
            foreach (hardware_location r in regs)
                ret |= (uint)(1 << ((int)((arm_gpr)r).reg));
            return new CodeBlock(ToByteArray(ret));
        }
        CodeBlock EncSingleRegListOpcode(cond c, uint op1, hardware_location rn, hardware_location rt)
        {
            uint ret = ((uint)c << 28) | (2 << 25) | (op1 << 20) | ((uint)((arm_gpr)rn).reg << 16) |
                ((uint)((arm_gpr)rt).reg << 12) | 4;
            return new CodeBlock(ToByteArray(ret));
        }
        OutputBlock EncRnRtOpcode(cond c, uint op1, hardware_location rn, hardware_location rt, uint imm12)
        {
            uint ret = ((uint)c << 28) | (2 << 25) | (op1 << 20) | ((uint)((arm_gpr)rn).reg << 16) |
                ((uint)((arm_gpr)rt).reg << 12) | imm12;
            return new CodeBlock(ToByteArray(ret));
        }
        OutputBlock EncRnRtOpcode25To27Are0(cond c, uint op1, hardware_location rn, hardware_location rt, uint imm12)
        {
            uint ret = ((uint)c << 28) | (0 << 25) | (op1 << 20) | ((uint)((arm_gpr)rn).reg << 16) |
                ((uint)((arm_gpr)rt).reg << 12) | imm12;
            return new CodeBlock(ToByteArray(ret));
        }
                
        internal override void arch_init_opcodes()
        {
            output_opcodes.Add(ThreeAddressCode.Op.add_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.add_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_add_i4_gpr_gpr,
                        op1 = CGpr, op2 = CGpr, result = CGpr },
                    new opcode_choice { code_emitter = arm_add_i4_gpr_gpr_imm,
                        op1 = CGpr, op2 = CConstInt12, result = CGpr },
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.add_i, output_opcodes[ThreeAddressCode.Op.add_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.alloca_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.alloca_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_alloca,
                        op1 = CGpr, op2 = CNone, result = CGpr },
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.alloca_i, output_opcodes[ThreeAddressCode.Op.alloca_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.and_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.and_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_and_i4_gpr_gpr,
                        op1 = CGpr, op2 = CGpr, result = CGpr },
                    new opcode_choice { code_emitter = arm_and_i4_gpr_gpr_imm,
                        op1 = CGpr, op2 = CConstInt12, result = CGpr },
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.and_i, output_opcodes[ThreeAddressCode.Op.and_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.add_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.add_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_add_i8_2gpr_2gpr,
                        op1 = C2Gpr, op2 = C2Gpr, result = C2Gpr },
                    new opcode_choice { code_emitter = arm_add_i8_2gpr_2gpr_imm,
                        op1 = C2Gpr, op2 = CConstInt12, result = C2Gpr },
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.assign_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.assign_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_assign_i4_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr },
                    new opcode_choice { code_emitter = arm_assign_i4_imm12_gpr,
                        op1 = CConstInt12, op2 = CNone, result = CGpr },
                    new opcode_choice { code_emitter = arm_assign_i4_imm16_gpr,
                        op1 = CConstInt16, op2 = CNone, result = CGpr },
                    new opcode_choice { code_emitter = arm_assign_i4_gprptr_gpr,
                        op1 = CGprPtr, op2 = CNone, result = CGpr },
                    new opcode_choice { code_emitter = arm_assign_i4_gpr_gprptr,
                        op1 = CGpr, op2 = CNone, result = CGprPtr },
                    new opcode_choice { code_emitter = arm_assign_i4_imm32_gpr,
                        op1 = CConstInt32, op2 = CNone, result = CGpr },
                    new opcode_choice { code_emitter = arm_assign_i4_gpr_mem,
                        op1 = CGpr, op2 = CNone, result = CMem32 },
                    new opcode_choice { code_emitter = arm_assign_i4_mem_gpr,
                        op1 = CMem32, op2 = CNone, result = CGpr },
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.assign_i, output_opcodes[ThreeAddressCode.Op.assign_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.assign_i8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_assign_i8_2gpr_2gpr,
                        op1 = C2Gpr, op2 = CNone, result = C2Gpr },
                    new opcode_choice { code_emitter = arm_assign_i8_2gpr_gprptr,
                        op1 = C2Gpr, op2 = CNone, result = CGprPtr },
                    new opcode_choice { code_emitter = arm_assign_i8_gprptr_2gpr,
                        op1 = CGprPtr, op2 = CNone, result = C2Gpr }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.assign_v_i, output_opcodes[ThreeAddressCode.Op.assign_i]);
            output_opcodes.Add(ThreeAddressCode.Op.assign_v_i4, output_opcodes[ThreeAddressCode.Op.assign_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.assign_v_i8, output_opcodes[ThreeAddressCode.Op.assign_i8]);

            output_opcodes.Add(ThreeAddressCode.Op.assign_vt, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_assign_vt,
                        op1 = CAny, op2 = CNone, result = CAny }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.br, new output_opcode
            {
                op = ThreeAddressCode.Op.br,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_br,
                        op1 = CNone, op2 = CNone, result = CNone }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.bb, output_opcodes[ThreeAddressCode.Op.br]);
            output_opcodes.Add(ThreeAddressCode.Op.bl, output_opcodes[ThreeAddressCode.Op.br]);
            output_opcodes.Add(ThreeAddressCode.Op.beq, output_opcodes[ThreeAddressCode.Op.br]);
            output_opcodes.Add(ThreeAddressCode.Op.ba, output_opcodes[ThreeAddressCode.Op.br]);
            output_opcodes.Add(ThreeAddressCode.Op.bae, output_opcodes[ThreeAddressCode.Op.br]);
            output_opcodes.Add(ThreeAddressCode.Op.bbe, output_opcodes[ThreeAddressCode.Op.br]);
            output_opcodes.Add(ThreeAddressCode.Op.bg, output_opcodes[ThreeAddressCode.Op.br]);
            output_opcodes.Add(ThreeAddressCode.Op.bge, output_opcodes[ThreeAddressCode.Op.br]);
            output_opcodes.Add(ThreeAddressCode.Op.ble, output_opcodes[ThreeAddressCode.Op.br]);
            output_opcodes.Add(ThreeAddressCode.Op.bne, output_opcodes[ThreeAddressCode.Op.br]);

            output_opcodes.Add(ThreeAddressCode.Op.br_ehclause, new output_opcode
            {
                op = ThreeAddressCode.Op.br_ehclause,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_brehclause,
                        op1 = CNone, op2 = CNone, result = CNone }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.call_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.call_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_call,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CLabel, CGpr } },
                        op2 = CNone, result = CR0,
                        clobber_list = new hardware_location[] { R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, R11, R12 }
                    }
                },
                recommended_R = R0
            });
            output_opcodes.Add(ThreeAddressCode.Op.call_i, output_opcodes[ThreeAddressCode.Op.call_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.call_void, new output_opcode
            {
                op = ThreeAddressCode.Op.call_void,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_call,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CLabel, CGpr } },
                        op2 = CNone, result = CNone,
                        clobber_list = new hardware_location[] { R0, R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, R11, R12 }
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.call_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.call_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_call,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CLabel, CGpr } },
                        op2 = CNone, result = CR0R1,
                        clobber_list = new hardware_location[] { R2, R3, R4, R5, R6, R7, R8, R9, R10, R11, R12 }
                    }
                },
                recommended_R = R0R1
            });
            output_opcodes.Add(ThreeAddressCode.Op.call_vt, new output_opcode
            {
                op = ThreeAddressCode.Op.call_vt,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_call,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CLabel, CGpr }},
                        op2 = CNone, result = CMemPtr,
                        clobber_list = new hardware_location[] { R0, R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, R11, R12 }
                    }
                },
            });


            output_opcodes.Add(ThreeAddressCode.Op.cmp_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.cmp_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_cmp_i4_gpr_gpr,
                        op1 = CGpr, op2 = CGpr, result = CNone },
                    new opcode_choice { code_emitter = arm_cmp_i4_gpr_imm12,
                        op1 = CGpr, op2 = CConstInt12, result = CNone },
                },
            });
            output_opcodes.Add(ThreeAddressCode.Op.cmp_i, output_opcodes[ThreeAddressCode.Op.cmp_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_i_i4sx, output_opcodes[ThreeAddressCode.Op.assign_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_isx, output_opcodes[ThreeAddressCode.Op.assign_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_uzx, output_opcodes[ThreeAddressCode.Op.assign_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_uzx, output_opcodes[ThreeAddressCode.Op.assign_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_u1zx, new output_opcode
            {
                op = ThreeAddressCode.Op.conv_i4_u1zx,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_conv_i4_u1zx,
                        op1 = CGpr, op2 = CNone, result = CGpr }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.endfinally, new output_opcode
            {
                op = ThreeAddressCode.Op.endfinally,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_endfinally,
                        op1 = CNone, op2 = CNone, result = CNone }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.enter, new output_opcode
            {
                op = ThreeAddressCode.Op.enter,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_enter,
                        op1 = CLabel, op2 = CNone, result = CNone }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.ldmethinfo, new output_opcode
            {
                op = ThreeAddressCode.Op.ldmethinfo,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_ldmethinfo,
                        op1 = CNone, op2 = CNone, result = CGprMemPtr },
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.ldobj_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.ldobj_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_peek_u4_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr },
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.ldobj_i, output_opcodes[ThreeAddressCode.Op.ldobj_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.mul_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.mul_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_mul_i4_gpr_gpr,
                        op1 = CGpr, op2 = CGpr, result = CGpr }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.mul_i, output_opcodes[ThreeAddressCode.Op.mul_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.not_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.not_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_not_i4_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr },
                    new opcode_choice { code_emitter = arm_not_i4_gpr_gpr_imm,
                        op1 = CConstInt12, op2 = CNone, result = CGpr },
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.not_i, output_opcodes[ThreeAddressCode.Op.not_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.or_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.or_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_or_i4_gpr_gpr,
                        op1 = CGpr, op2 = CGpr, result = CGpr },
                    new opcode_choice { code_emitter = arm_or_i4_gpr_gpr_imm,
                        op1 = CGpr, op2 = CConstInt12, result = CGpr },
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.or_i, output_opcodes[ThreeAddressCode.Op.or_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.peek_i1, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_i1,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_peek_i1_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.peek_i2, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_i2,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_peek_i2_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.peek_u1, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_u1,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_peek_u1_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.peek_u2, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_u2,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_peek_u2_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.peek_u4, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_u4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_peek_u4_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.peek_u, output_opcodes[ThreeAddressCode.Op.peek_u4]);

            output_opcodes.Add(ThreeAddressCode.Op.poke_u1, new output_opcode
            {
                op = ThreeAddressCode.Op.poke_u1,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_poke_u1_gpr_gpr,
                        op1 = CGpr, op2 = CGpr, result = CNone }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.poke_u2, new output_opcode
            {
                op = ThreeAddressCode.Op.poke_u2,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_poke_u2_gpr_gpr,
                        op1 = CGpr, op2 = CGpr, result = CNone }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.poke_u4, new output_opcode
            {
                op = ThreeAddressCode.Op.poke_u4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_poke_u4_gpr_gpr,
                        op1 = CGpr, op2 = CGpr, result = CNone }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.poke_u, output_opcodes[ThreeAddressCode.Op.poke_u4]);

            output_opcodes.Add(ThreeAddressCode.Op.ret_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.ret_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_ret_i4_gpr,
                        op1 = CR0, op2 = CNone, result = CNone }
                },
                recommended_O1 = R0
            });
            output_opcodes.Add(ThreeAddressCode.Op.ret_i, output_opcodes[ThreeAddressCode.Op.ret_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.ret_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.ret_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_ret_i4_gpr,
                        op1 = CR0R1, op2 = CNone, result = CNone }
                },
                recommended_O1 = R0R1
            });
            output_opcodes.Add(ThreeAddressCode.Op.ret_void, new output_opcode
            {
                op = ThreeAddressCode.Op.ret_void,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_ret_i4_gpr,
                        op1 = CNone, op2 = CNone, result = CNone }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.seteq, new output_opcode
            {
                op = ThreeAddressCode.Op.seteq,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_seteq_i4_gpr,
                        op1 = CNone, op2 = CNone, result = CGpr }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.sub_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.sub_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_sub_i4_gpr_gpr,
                        op1 = CGpr, op2 = CGpr, result = CGpr },
                    new opcode_choice { code_emitter = arm_sub_i4_gpr_gpr_imm,
                        op1 = CGpr, op2 = CConstInt12, result = CGpr }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.throw_, new output_opcode
            {
                op = ThreeAddressCode.Op.throw_,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = arm_throw,
                        op1 = CGprConst, op2 = CNone, result = CNone }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.throw_ovf, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throw_ovf_un, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throweq, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throwg_un, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throwge_un, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throwne, output_opcodes[ThreeAddressCode.Op.throw_]);          
        }
    }
}
