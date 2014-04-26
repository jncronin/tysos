/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace libtysila
{
    partial class x86_64_Assembler
    {
        const int HIGH_REG_START = 8;

        public static x86_64_gpr Rax { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rax }; } }
        public static x86_64_gpr Rbx { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rbx }; } }
        public static x86_64_gpr Rcx { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rcx }; } }
        public static x86_64_gpr Rdx { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rdx }; } }
        public static x86_64_gpr Rsi { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rsi }; } }
        public static x86_64_gpr Rdi { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rdi }; } }
        public static x86_64_gpr Rsp { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rsp }; } }
        public static x86_64_gpr Rbp { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rbp }; } }

        public static x86_64_gpr R8 { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.r8 }; } }
        public static x86_64_gpr R9 { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.r9 }; } }
        public static x86_64_gpr R10 { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.r10 }; } }
        public static x86_64_gpr R11 { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.r11 }; } }
        public static x86_64_gpr R12 { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.r12 }; } }
        public static x86_64_gpr R13 { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.r13 }; } }
        public static x86_64_gpr R14 { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.r14 }; } }
        public static x86_64_gpr R15 { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.r15 }; } }

        public static x86_64_xmm Xmm0 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm0 }; } }
        public static x86_64_xmm Xmm1 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm1 }; } }
        public static x86_64_xmm Xmm2 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm2 }; } }
        public static x86_64_xmm Xmm3 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm3 }; } }
        public static x86_64_xmm Xmm4 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm4 }; } }
        public static x86_64_xmm Xmm5 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm5 }; } }
        public static x86_64_xmm Xmm6 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm6 }; } }
        public static x86_64_xmm Xmm7 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm7 }; } }
        public static x86_64_xmm Xmm8 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm8 }; } }
        public static x86_64_xmm Xmm9 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm9 }; } }
        public static x86_64_xmm Xmm10 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm10 }; } }
        public static x86_64_xmm Xmm11 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm11 }; } }
        public static x86_64_xmm Xmm12 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm12 }; } }
        public static x86_64_xmm Xmm13 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm13 }; } }
        public static x86_64_xmm Xmm14 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm14 }; } }
        public static x86_64_xmm Xmm15 { get { return new x86_64_xmm { xmm = x86_64_xmm.XmmId.xmm15 }; } }

        hloc_constraint CRax { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = Rax }; } }
        hloc_constraint CRbx { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = Rbx }; } }
        hloc_constraint CRcx { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = Rcx }; } }
        hloc_constraint CRdx { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = Rdx }; } }
        hloc_constraint CRsi { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = Rsi }; } }
        hloc_constraint CRdi { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = Rdi }; } }
        hloc_constraint CRsp { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = Rsp }; } }
        hloc_constraint CRbp { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = Rbp }; } }
        hloc_constraint CR8 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R8 }; } }
        hloc_constraint CR9 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R9 }; } }
        hloc_constraint CR10 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R10 }; } }
        hloc_constraint CR11 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R11 }; } }
        hloc_constraint CR12 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R12 }; } }
        hloc_constraint CR13 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R13 }; } }
        hloc_constraint CR14 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R14 }; } }
        hloc_constraint CR15 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = R15 }; } }

        hloc_constraint CXmm0 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = Xmm0 }; } }

        hloc_constraint CGpr { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new x86_64_gpr() }; } }
        hloc_constraint C2Gpr { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new multiple_hardware_location { hlocs = new x86_64_gpr[2] } }; } }
        hloc_constraint CXmm { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new x86_64_xmm() }; } }
        hloc_constraint CMem { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_stackloc() }; } }
        hloc_constraint CMem32 { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_stackloc { size = 4 } }; } }
        hloc_constraint CMem64 { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_stackloc { size = 8 } }; } }
        hloc_constraint COp1 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Operand1 }; } }
        hloc_constraint COp2 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Operand2 }; } }
        hloc_constraint CNone { get { return new hloc_constraint { constraint = hloc_constraint.c_.None }; } }
        hloc_constraint CConst { get { return new hloc_constraint { constraint = hloc_constraint.c_.Immediate }; } }
        hloc_constraint CConstByte { get { return new hloc_constraint { constraint = hloc_constraint.c_.Immediate, const_bitsize = 8 }; } }
        hloc_constraint CConstInt32 { get { return new hloc_constraint { constraint = hloc_constraint.c_.Immediate, const_bitsize = 32 }; } }
        hloc_constraint CGprMem { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem } }; } }
        hloc_constraint CGprMem32 { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem32 } }; } }
        hloc_constraint CGprMem32ExceptRbx { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGprExceptRbx, CMem32 } }; } }
        hloc_constraint CGprMem64 { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem64 } }; } }
        hloc_constraint CGprPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_contentsof { base_loc = new x86_64_gpr() } }; } }
        hloc_constraint CStackPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_addressof { base_loc = new hardware_stackloc() } }; } }
        hloc_constraint CAddrOfGprPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_addressof { base_loc = new hardware_contentsof { base_loc = new x86_64_gpr() } } }; } }
        hloc_constraint CAnyPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CStackPtr, CAddrOfGprPtr } }; } }
        hloc_constraint CLabel { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_addressoflabel() }; } }
        hloc_constraint CGprMemPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem, CGprPtr } }; } }
        hloc_constraint CGprMemConst { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem, CConst } }; } }
        hloc_constraint CXmmMem { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CXmm, CMem64 } }; } }
        hloc_constraint CXmmPtrMem { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CXmm, CGprPtr, CMem } }; } }
        hloc_constraint CMemPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CMem, CGprPtr } }; } }

        hloc_constraint CGprExceptRax { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRbx, CRcx, CRdx, CRdi, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRbx { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRcx, CRdx, CRdi, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRcx { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRbx, CRdx, CRdi, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRdx { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRbx, CRcx, CRdi, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRsi { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRbx, CRcx, CRdx, CRdi,  CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRdi { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRbx, CRcx, CRdx, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        
        internal override hloc_constraint GetConstraintFromSemantic(var_semantic vs)
        {
            if (vs.needs_memloc || vs.needs_vtype || vs.needs_virtftnptr)
            {
                hloc_constraint ret = CMem;
                ((hardware_stackloc)ret.specific).size = vs.vtype_size;
                return ret;
            }
            if (vs.needs_float)
                return CXmmMem;
            if (vs.needs_int32)
            {
                if (ia == IA.x86_64)
                    return CGprMem32;
                else
                    return CGprMem32ExceptRbx;
            }
            if (vs.needs_intptr)
            {
                if (ia == IA.x86_64)
                    return CGprMem64;
                else
                    return CGprMem32ExceptRbx;
            }
            if(vs.needs_int64 && (ia == IA.x86_64))
                return CGprMem64;
            if (vs.needs_int64 && (ia == IA.i586))
                return C2Gpr;
            throw new NotSupportedException();
        }

        internal override void arch_init_opcodes()
        {
            // ia may not be initialized yet so we have to do it instead
            if (Arch.InstructionSet == "i586")
                ia = IA.i586;
            else
                ia = IA.x86_64;

            output_opcodes.Add(ThreeAddressCode.Op.nop, new output_opcode
            {
                op = ThreeAddressCode.Op.nop,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_nop,
                        op1 = CNone, op2 = CNone, result = CNone
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.add_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.add_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_add_i4_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_add_i4_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_add_i4_gprmem_const,
                        op1 = CGprMem, op2 = CConst, result = COp1
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.add_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.add_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_add_i8_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_add_i8_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_add_i8_gprmem_const,
                        op1 = CGprMem, op2 = CConstInt32, result = COp1,
                    },
                    new opcode_choice { code_emitter = x86_64_add_i8_gpr_gprptr,
                        op1 = CGpr, op2 = CGprPtr, result = COp1
                    }
                }
            });

            if (ia == IA.x86_64)
            {
                output_opcodes.Add(ThreeAddressCode.Op.add_i, new output_opcode
                {
                    op = ThreeAddressCode.Op.add_i,
                    opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_add_i8_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_add_i8_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_add_i8_gprmem_const,
                        op1 = CGprMem, op2 = CConstInt32, result = COp1,
                    },
                    new opcode_choice { code_emitter = x86_64_add_i8_gpr_gprptr,
                        op1 = CGpr, op2 = CGprPtr, result = COp1
                    }
                }
                });
            }
            else
                output_opcodes.Add(ThreeAddressCode.Op.add_i, output_opcodes[ThreeAddressCode.Op.add_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.sub_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.sub_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_sub_i4_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_sub_i4_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.sub_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.sub_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_sub_i8_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_sub_i8_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_sub_i8_gpr_gprptr,
                        op1 = CGpr, op2 = CGprPtr, result = COp1
                    }
                }
            });
            if(ia == IA.x86_64)
                output_opcodes.Add(ThreeAddressCode.Op.sub_i, output_opcodes[ThreeAddressCode.Op.sub_i8]);
            else
                output_opcodes.Add(ThreeAddressCode.Op.sub_i, output_opcodes[ThreeAddressCode.Op.sub_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.and_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.and_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_and_i4_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_and_i4_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.and_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.and_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_and_i8_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_and_i8_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    }
                }
            });

            if (ia == IA.x86_64)
            {
                output_opcodes.Add(ThreeAddressCode.Op.and_i, new output_opcode
                {
                    op = ThreeAddressCode.Op.and_i,
                    opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_and_i8_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_and_i8_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    }
                }
                });
            }
            else
                output_opcodes.Add(ThreeAddressCode.Op.and_i, output_opcodes[ThreeAddressCode.Op.and_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.or_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.or_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_or_i4_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_or_i4_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.or_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.or_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_or_i8_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_or_i8_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    }
                }
            });
            if(ia == IA.x86_64)
                output_opcodes.Add(ThreeAddressCode.Op.or_i, output_opcodes[ThreeAddressCode.Op.or_i8]);
            else
                output_opcodes.Add(ThreeAddressCode.Op.or_i, output_opcodes[ThreeAddressCode.Op.or_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.xor_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.xor_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_xor_i48_do1gpr_o2gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_xor_i48_do1gprmem_o2gpr,
                        op1 = CGprMem, op2 = CGpr, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.xor_i8, output_opcodes[ThreeAddressCode.Op.xor_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.xor_i, output_opcodes[ThreeAddressCode.Op.xor_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.cmp_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.cmp_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_cmp_i4_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = CNone
                    },
                    new opcode_choice { code_emitter = x86_64_cmp_i4_gprmem_const,
                        op1 = CGprMem, op2 = CConst, result = CNone
                    },
                    new opcode_choice { code_emitter = x86_64_cmp_i4_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = CNone
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.cmp_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.cmp_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_cmp_i8_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = CNone
                    },
                    new opcode_choice { code_emitter = x86_64_cmp_i8_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = CNone
                    }
                }
            });

            if (ia == IA.x86_64)
            {
                output_opcodes.Add(ThreeAddressCode.Op.cmp_i, new output_opcode
                {
                    op = ThreeAddressCode.Op.cmp_i,
                    opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_cmp_i8_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = CNone
                    },
                    new opcode_choice { code_emitter = x86_64_cmp_i8_gprmem_gpr,
                        op1 = CGprMem, op2 = CGpr, result = CNone
                    }
                }
                });
            }
            else
                output_opcodes.Add(ThreeAddressCode.Op.cmp_i, output_opcodes[ThreeAddressCode.Op.cmp_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.br, new output_opcode
            {
                op = ThreeAddressCode.Op.br,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_br,
                        op1 = CNone, op2 = CNone, result = CNone
                    },
                    new opcode_choice { code_emitter = x86_64_brlabel,
                        op1 = CLabel, op2 = CNone, result = CNone
                    },
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
                    new opcode_choice { code_emitter = x86_64_br_ehclause,
                        op1 = CNone, op2 = CNone, result = CNone
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.ret_void, new output_opcode
            {
                op = ThreeAddressCode.Op.ret_void,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_ret,
                        op1 = CNone, op2 = CNone, result = CNone
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.endfinally, new output_opcode
            {
                op = ThreeAddressCode.Op.endfinally,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_endfinally,
                        op1 = CNone, op2 = CNone, result = CNone
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.ret_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.ret_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_ret,
                        op1 = CRax, op2 = CNone, result = CNone
                    }
                },
                recommended_O1 = Rax
            });

            output_opcodes.Add(ThreeAddressCode.Op.ret_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.ret_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_ret,
                        op1 = CRax, op2 = CNone, result = CNone
                    }
                },
                recommended_O1 = Rax
            });

            if (ia == IA.x86_64)
            {
                output_opcodes.Add(ThreeAddressCode.Op.ret_i, new output_opcode
                {
                    op = ThreeAddressCode.Op.ret_i,
                    opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_ret,
                        op1 = CRax, op2 = CNone, result = CNone
                    }
                },
                    recommended_O1 = Rax
                });
            }
            else
                output_opcodes.Add(ThreeAddressCode.Op.ret_i, output_opcodes[ThreeAddressCode.Op.ret_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.ret_r8, new output_opcode
            {
                op = ThreeAddressCode.Op.ret_r8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_ret,
                        op1 = CXmm0, op2 = CNone, result = CNone
                    }
                },
                recommended_O1 = Xmm0
            });

            output_opcodes.Add(ThreeAddressCode.Op.call_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.call_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_call,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CLabel, CGpr, CGprPtr } },
                        op2 = CNone, result = CRax,
                        clobber_list = new hardware_location[] { Rbx, Rcx, Rdx, Rdi, Rsi, R8, R9, R10, R11, R12, R13, R14, R15,
                            Xmm0, Xmm1, Xmm2, Xmm3, Xmm4, Xmm5, Xmm6, Xmm7, Xmm8, Xmm9, Xmm10, Xmm11, Xmm12, Xmm13, Xmm14, Xmm15 }
                    }
                },
                recommended_R = Rax
            });
            output_opcodes.Add(ThreeAddressCode.Op.call_i8, output_opcodes[ThreeAddressCode.Op.call_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.call_i, output_opcodes[ThreeAddressCode.Op.call_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.call_vt, new output_opcode
            {
                op = ThreeAddressCode.Op.call_vt,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_call,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CLabel, CGpr, CGprPtr } },
                        op2 = CNone, result = CMem,
                        clobber_list = new hardware_location[] { Rax, Rbx, Rcx, Rdx, Rdi, Rsi, R8, R9, R10, R11, R12, R13, R14, R15,
                            Xmm0, Xmm1, Xmm2, Xmm3, Xmm4, Xmm5, Xmm6, Xmm7, Xmm8, Xmm9, Xmm10, Xmm11, Xmm12, Xmm13, Xmm14, Xmm15 }
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.call_r4, new output_opcode
            {
                op = ThreeAddressCode.Op.call_r4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_call,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CLabel, CGpr, CGprPtr }},
                            op2 = CNone, result = CXmm0,
                        clobber_list = new hardware_location[] { Rax, Rbx, Rcx, Rdx, Rdi, Rsi, R8, R9, R10, R11, R12, R13, R14, R15,
                            Xmm1, Xmm2, Xmm3, Xmm4, Xmm5, Xmm6, Xmm7, Xmm8, Xmm9, Xmm10, Xmm11, Xmm12, Xmm13, Xmm14, Xmm15 }
                    }
                },
                recommended_R = Xmm0
            });
            output_opcodes.Add(ThreeAddressCode.Op.call_r8, output_opcodes[ThreeAddressCode.Op.call_r4]);

            output_opcodes.Add(ThreeAddressCode.Op.call_void, new output_opcode
            {
                op = ThreeAddressCode.Op.call_void,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_call,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CLabel, CGpr, CGprPtr } },
                        op2 = CNone, result = CNone,
                        clobber_list = new hardware_location[] { Rax, Rbx, Rcx, Rdx, Rdi, Rsi, R8, R9, R10, R11, R12, R13, R14, R15,
                            Xmm0, Xmm1, Xmm2, Xmm3, Xmm4, Xmm5, Xmm6, Xmm7, Xmm8, Xmm9, Xmm10, Xmm11, Xmm12, Xmm13, Xmm14, Xmm15 }
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.mul_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.mul_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_mul_i4_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_mul_i4_gpr_const,
                        op1 = CGpr, op2 = CConst, result = COp1
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.mul_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.mul_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_mul_i8_gpr_gprmem,
                        op1 = CGpr, op2 = CGprMem, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_mul_i8_gpr_const,
                        op1 = CGpr, op2 = CConst, result = COp1, clobber_list = new hardware_location[] { Rdi }
                    }
                }
            });
            if (ia == IA.x86_64)
                output_opcodes.Add(ThreeAddressCode.Op.mul_i, output_opcodes[ThreeAddressCode.Op.mul_i8]);
            else
                output_opcodes.Add(ThreeAddressCode.Op.mul_i, output_opcodes[ThreeAddressCode.Op.mul_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.mul_un_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.mul_un_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_mul_un_i48_o1rax_o2gprmem,
                        op1 = CRax, op2 = CGprMem, result = CRax, clobber_list = new hardware_location[] { Rdx }
                    }
                },
                recommended_O1 = Rax,
                recommended_R = Rax
            });
            output_opcodes.Add(ThreeAddressCode.Op.mul_un_i, output_opcodes[ThreeAddressCode.Op.mul_un_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.mul_un_i8, output_opcodes[ThreeAddressCode.Op.mul_un_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.div_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.div_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_divrem_srax_s2gprmem_draxrdx,
                        op1 = CRax, op2 = CGprMem, result = CRax, clobber_list = new hardware_location[] { Rdx }
                    }
                },
                recommended_O1 = Rax,
                recommended_R = Rax
            });
            output_opcodes.Add(ThreeAddressCode.Op.div_i, output_opcodes[ThreeAddressCode.Op.div_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.div_i8, output_opcodes[ThreeAddressCode.Op.div_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.div_u, output_opcodes[ThreeAddressCode.Op.div_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.div_u4, output_opcodes[ThreeAddressCode.Op.div_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.div_u8, output_opcodes[ThreeAddressCode.Op.div_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.rem_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.rem_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_divrem_srax_s2gprmem_draxrdx,
                        op1 = CRax, op2 = CGprMem, result = CRdx,
                    }
                },
                recommended_O1 = Rax,
                recommended_R = Rdx
            });
            output_opcodes.Add(ThreeAddressCode.Op.rem_i, output_opcodes[ThreeAddressCode.Op.rem_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.rem_i8, output_opcodes[ThreeAddressCode.Op.rem_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.rem_un_i, output_opcodes[ThreeAddressCode.Op.rem_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.rem_un_i4, output_opcodes[ThreeAddressCode.Op.rem_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.rem_un_i8, output_opcodes[ThreeAddressCode.Op.rem_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.shl_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.shl_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_shl_i4_gprmem_const,
                        op1 = CGprMem, op2 = CConst, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_shl_i4_gprmem_cl,
                        op1 = CGprMem, op2 = CRcx, result = COp1
                    }
                },
                recommended_O2 = Rcx
            });
            output_opcodes.Add(ThreeAddressCode.Op.shl_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.shl_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_shl_i8_gprmem_const,
                        op1 = CGprMem, op2 = CConst, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_shl_i8_gprmem_cl,
                        op1 = CGprMem, op2 = CRcx, result = COp1
                    }
                },
                recommended_O2 = Rcx
            });
            if (ia == IA.x86_64)
                output_opcodes.Add(ThreeAddressCode.Op.shl_i, output_opcodes[ThreeAddressCode.Op.shl_i8]);
            else
                output_opcodes.Add(ThreeAddressCode.Op.shl_i, output_opcodes[ThreeAddressCode.Op.shl_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.shr_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.shr_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_shr_i4_gprmem_const,
                        op1 = CGprMem, op2 = CConst, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_shr_i4_gprmem_cl,
                        op1 = CGprMem, op2 = CRcx, result = COp1
                    }
                },
                recommended_O2 = Rcx
            });
            output_opcodes.Add(ThreeAddressCode.Op.shr_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.shr_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_shr_i8_gprmem_const,
                        op1 = CGprMem, op2 = CConst, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_shr_i8_gprmem_cl,
                        op1 = CGprMem, op2 = CRcx, result = COp1
                    }
                },
                recommended_O2 = Rcx
            });
            if (ia == IA.x86_64)
                output_opcodes.Add(ThreeAddressCode.Op.shr_i, output_opcodes[ThreeAddressCode.Op.shr_i8]);
            else
                output_opcodes.Add(ThreeAddressCode.Op.shr_i, output_opcodes[ThreeAddressCode.Op.shr_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.shr_un_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.shr_un_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_shr_un_i48_sgprmem_const,
                        op1 = CGprMem, op2 = CConst, result = COp1
                    },
                    new opcode_choice { code_emitter = x86_64_shr_un_i48_sgprmem_cl,
                        op1 = CGprMem, op2 = CRcx, result = COp1
                    }
                },
                recommended_O2 = Rcx
            });
            output_opcodes.Add(ThreeAddressCode.Op.shr_un_i, output_opcodes[ThreeAddressCode.Op.shr_un_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.shr_un_i8, output_opcodes[ThreeAddressCode.Op.shr_un_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_i8sx, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_i4i8sx_gpr_gprmem,
                        op1 = CGprMem, op2 = CNone, result = CGpr
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_isx, output_opcodes[ThreeAddressCode.Op.conv_i4_i8sx]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_i4sx, output_opcodes[ThreeAddressCode.Op.conv_i4_i8sx]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_i4sx, output_opcodes[ThreeAddressCode.Op.conv_i4_i8sx]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_i2sx, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_i48_i2sx_dgpr_sgprmem,
                        op1 = CGprMem, op2 = CNone, result = CGpr
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_i2sx, output_opcodes[ThreeAddressCode.Op.conv_i4_i2sx]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_i2sx, output_opcodes[ThreeAddressCode.Op.conv_i4_i2sx]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_i1sx, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_i48_i2sx_dgpr_sgprmem,
                        op1 = CGprMem, op2 = CNone, result = CGpr
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_i1sx, output_opcodes[ThreeAddressCode.Op.conv_i4_i1sx]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_i1sx, output_opcodes[ThreeAddressCode.Op.conv_i4_i1sx]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_u2zx, new output_opcode
            {
                op = ThreeAddressCode.Op.conv_i4_u2zx,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_i4u2zx_gpr_gprmem,
                        op1 = CGprMem, op2 = CNone, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_u2zx, output_opcodes[ThreeAddressCode.Op.conv_i4_u2zx]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_u2zx, output_opcodes[ThreeAddressCode.Op.conv_i4_u2zx]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_u1zx, new output_opcode
            {
                op = ThreeAddressCode.Op.conv_i4_u1zx,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_i4u1zx_gpr_gprmem,
                        op1 = CGprMem, op2 = CNone, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_u1zx, output_opcodes[ThreeAddressCode.Op.conv_i4_u1zx]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_u1zx, output_opcodes[ThreeAddressCode.Op.conv_i4_u1zx]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_u8zx, new output_opcode
            {
                op = ThreeAddressCode.Op.conv_i4_u8zx,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_i4u8zx_sgprmem_dgpr,
                        op1 = CGprMem, op2 = CNone, result = CGpr
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_uzx, output_opcodes[ThreeAddressCode.Op.conv_i4_u8zx]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_u4zx, new output_opcode
            {
                op = ThreeAddressCode.Op.conv_i8_u4zx,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_i4_gpr_gprmem,     // mov to a 32 bit register automatically zero-extends
                        op1 = CGprMemPtr, op2 = CNone, result = CGpr
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_u4zx, output_opcodes[ThreeAddressCode.Op.conv_i8_u4zx]);

            if (ia == IA.x86_64)
            {
                output_opcodes.Add(ThreeAddressCode.Op.assign_i, new output_opcode
                {
                    op = ThreeAddressCode.Op.assign_i,
                    opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_i8_gpr_gprmem,
                        op1 = CGprMemPtr, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },                    
                    new opcode_choice { code_emitter = x86_64_assign_i8_gprmem_gpr,
                        op1 = CGpr, op2 = CNone, result = CGprMemPtr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_gprptr_label,
                        op1 = CLabel, op2 = CNone, result = CMemPtr, clobber_list = new hardware_location[] { Rdi }
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_gprptr_label,
                        op1 = CLabel, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_sptr_dgpr,
                        op1 = CAnyPtr, op2 = CNone, result = CGpr
                    }
                }
                });
            }
            else
            {
                output_opcodes.Add(ThreeAddressCode.Op.assign_i, new output_opcode
                {
                    op = ThreeAddressCode.Op.assign_i,
                    opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_i4_gpr_gprmem,
                        op1 = CGprMemPtr, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i4_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },                    
                    new opcode_choice { code_emitter = x86_64_assign_i4_gprmem_gpr,
                        op1 = CGpr, op2 = CNone, result = CGprMemPtr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_gprptr_label,
                        op1 = CLabel, op2 = CNone, result = CMemPtr, clobber_list = new hardware_location[] { Rdi }
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_gprptr_label,
                        op1 = CLabel, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i4_sptr_dgpr,
                        op1 = CAnyPtr, op2 = CNone, result = CGpr
                    }
                }
                });
            }

            output_opcodes.Add(ThreeAddressCode.Op.assign_i8, new output_opcode
            {
                op = ThreeAddressCode.Op.assign_i8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_i8_gpr_gprmem,
                        op1 = CGprMemPtr, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },                    
                    new opcode_choice { code_emitter = x86_64_assign_i8_gprmem_gpr,
                        op1 = CGpr, op2 = CNone, result = CGprMemPtr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_gprptr_label,
                        op1 = CLabel, op2 = CNone, result = CMemPtr, clobber_list = new hardware_location[] { Rdi }
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_gprptr_label,
                        op1 = CLabel, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i8_sptr_dgpr,
                        op1 = CStackPtr, op2 = CNone, result = CGpr
                    }

                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.assign_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.assign_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_i4_gpr_gprmem,
                        op1 = CGprMemPtr, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i4_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },                    
                    new opcode_choice { code_emitter = x86_64_assign_i4_gprmem_const,
                        op1 = CConst, op2 = CNone, result = CGprMemPtr
                    },
                    new opcode_choice { code_emitter = x86_64_assign_i4_gprmem_gpr,
                        op1 = CGpr, op2 = CNone, result = CGprMemPtr
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.assign_v_i, output_opcodes[ThreeAddressCode.Op.assign_i]);
            output_opcodes.Add(ThreeAddressCode.Op.assign_v_i4, output_opcodes[ThreeAddressCode.Op.assign_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.assign_v_i8, output_opcodes[ThreeAddressCode.Op.assign_i8]);

            output_opcodes.Add(ThreeAddressCode.Op.enter, new output_opcode
            {
                op = ThreeAddressCode.Op.enter,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_enter,
                        op1 = CLabel, op2 = CNone, result = CNone
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.seteq, new output_opcode
            {
                op = ThreeAddressCode.Op.seteq,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_set,
                        op1 = CNone, op2 = CNone, result = CGprMem
                    },
                    new opcode_choice { code_emitter = x86_64_set,
                        op1 = CNone, op2 = CNone, result = CGprPtr
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.seta, output_opcodes[ThreeAddressCode.Op.seteq]);
            output_opcodes.Add(ThreeAddressCode.Op.setae, output_opcodes[ThreeAddressCode.Op.seteq]);
            output_opcodes.Add(ThreeAddressCode.Op.setb, output_opcodes[ThreeAddressCode.Op.seteq]);
            output_opcodes.Add(ThreeAddressCode.Op.setbe, output_opcodes[ThreeAddressCode.Op.seteq]);
            output_opcodes.Add(ThreeAddressCode.Op.setg, output_opcodes[ThreeAddressCode.Op.seteq]);
            output_opcodes.Add(ThreeAddressCode.Op.setge, output_opcodes[ThreeAddressCode.Op.seteq]);
            output_opcodes.Add(ThreeAddressCode.Op.setl, output_opcodes[ThreeAddressCode.Op.seteq]);
            output_opcodes.Add(ThreeAddressCode.Op.setle, output_opcodes[ThreeAddressCode.Op.seteq]);
            output_opcodes.Add(ThreeAddressCode.Op.setne, output_opcodes[ThreeAddressCode.Op.seteq]);

            output_opcodes.Add(ThreeAddressCode.Op.peek_u1, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_u1,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_peek_u1_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_peek_u1_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.peek_u2, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_u2,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_peek_u2_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_peek_u2_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.peek_i1, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_u1,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_peek_i1_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_peek_i1_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.peek_i2, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_u2,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_peek_i2_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_peek_i2_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr
                    }
                }
            });


            output_opcodes.Add(ThreeAddressCode.Op.peek_u4, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_u4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_peek_u4_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_peek_u_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.peek_u8, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_u8,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_peek_u8_gpr_const,
                        op1 = CConst, op2 = CNone, result = CGpr
                    },
                    new opcode_choice { code_emitter = x86_64_peek_u_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.peek_u, output_opcodes[ThreeAddressCode.Op.peek_u8]);

            output_opcodes.Add(ThreeAddressCode.Op.peek_r4, new output_opcode
            {
                op = ThreeAddressCode.Op.peek_r4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_peek_r48_dxmm_sgpr,
                        op1 = CGpr, op2 = CNone, result = CXmm
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.peek_r8, output_opcodes[ThreeAddressCode.Op.peek_r4]);

            output_opcodes.Add(ThreeAddressCode.Op.ldobj_i4, new output_opcode
            {
                op = ThreeAddressCode.Op.ldobj_i4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_peek_u_gpr_gpr,
                        op1 = CGpr, op2 = CNone, result = CGpr
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.ldobj_i, output_opcodes[ThreeAddressCode.Op.ldobj_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.ldobj_i8, output_opcodes[ThreeAddressCode.Op.ldobj_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.poke_u1, new output_opcode
            {
                op = ThreeAddressCode.Op.poke_u1,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_poke_u1_gprconst_gprmemconst,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CGpr, CMem, CConstInt32 } },
                        op2 = new hloc_constraint { constraint = hloc_constraint.c_.List,
                            specific_list = new List<hloc_constraint> { CGpr, CConstInt32 } }, result = CNone,
                        clobber_list = new hardware_location[] { Rdi }
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.poke_u2, output_opcodes[ThreeAddressCode.Op.poke_u1]);
            output_opcodes.Add(ThreeAddressCode.Op.poke_u4, output_opcodes[ThreeAddressCode.Op.poke_u1]);
            output_opcodes.Add(ThreeAddressCode.Op.poke_u8, output_opcodes[ThreeAddressCode.Op.poke_u1]);
            output_opcodes.Add(ThreeAddressCode.Op.poke_u, output_opcodes[ThreeAddressCode.Op.poke_u1]);

            output_opcodes.Add(ThreeAddressCode.Op.poke_r4, new output_opcode
            {
                op = ThreeAddressCode.Op.poke_r4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_poke_r48_o1gpr_o2xmm,
                        op1 = CGpr, op2 = CXmm, result = CNone
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.poke_r8, output_opcodes[ThreeAddressCode.Op.poke_r4]);

            output_opcodes.Add(ThreeAddressCode.Op.portout_u2_u1, new output_opcode
            {
                op = ThreeAddressCode.Op.portout_u2_u1,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_portout_u2_u1_dx_rax,
                        op1 = CRdx, op2 = CRax, result = CNone
                    },
                    new opcode_choice { code_emitter = x86_64_portout_u2_u1_const_rax,
                        op1 = CConstByte, op2 = CRax, result = CNone
                    }
                },
                recommended_O1 = Rdx
            });

            output_opcodes.Add(ThreeAddressCode.Op.portout_u2_u2, new output_opcode
            {
                op = ThreeAddressCode.Op.portout_u2_u2,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_portout_u2_u2_dx_rax,
                        op1 = CRdx, op2 = CRax, result = CNone
                    }
                },
                recommended_O1 = Rdx
            });

            output_opcodes.Add(ThreeAddressCode.Op.portout_u2_u4, new output_opcode
            {
                op = ThreeAddressCode.Op.portout_u2_u4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_portout_u2_u4_dx_rax,
                        op1 = CRdx, op2 = CRax, result = CNone
                    }
                },
                recommended_O1 = Rdx
            });

            output_opcodes.Add(ThreeAddressCode.Op.portin_u2_u1, new output_opcode
            {
                op = ThreeAddressCode.Op.portin_u2_u1,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_portin_u2_u1_o1dx_drax,
                        op1 = CRdx, op2 = CNone, result = CRax
                    }
                },
                recommended_O1 = Rdx
            });

            output_opcodes.Add(ThreeAddressCode.Op.portin_u2_u2, new output_opcode
            {
                op = ThreeAddressCode.Op.portin_u2_u2,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_portin_u2_u2_o1dx_drax,
                        op1 = CRdx, op2 = CNone, result = CRax
                    }
                },
                recommended_O1 = Rdx
            });

            output_opcodes.Add(ThreeAddressCode.Op.portin_u2_u4, new output_opcode
            {
                op = ThreeAddressCode.Op.portin_u2_u4,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_portin_u2_u4_o1dx_drax,
                        op1 = CRdx, op2 = CNone, result = CRax
                    }
                },
                recommended_O1 = Rdx
            });

            output_opcodes.Add(ThreeAddressCode.Op.throw_, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_throw,
                        op1 = CGprMemConst, op2 = CNone, result = CNone
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.throweq, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throwne, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throw_ovf, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throw_ovf_un, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throwge_un, output_opcodes[ThreeAddressCode.Op.throw_]);
            output_opcodes.Add(ThreeAddressCode.Op.throwg_un, output_opcodes[ThreeAddressCode.Op.throw_]);

            output_opcodes.Add(ThreeAddressCode.Op.cmp_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_cmp_r8_xmm_xmmmem,
                        op1 = CXmm, op2 = CXmmMem, result = CNone
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.cmp_r8_un, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_cmp_r8_un_xmm_xmmmem,
                        op1 = CXmm, op2 = CXmmMem, result = CNone
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.cmp_r4, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_cmp_r4_xmm_xmmmem,
                        op1 = CXmm, op2 = CXmmMem, result = CNone
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.cmp_r4_un, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_cmp_r4_un_xmm_xmmmem,
                        op1 = CXmm, op2 = CXmmMem, result = CNone
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.assign_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_r8_xmm_xmmmem,
                        op1 = CXmmPtrMem, op2 = CNone, result = CXmm
                    },
                    new opcode_choice { code_emitter = x86_64_assign_r8_xmmmem_xmm,
                        op1 = CXmm, op2 = CNone, result = CXmmPtrMem
                    },
                    new opcode_choice { code_emitter = x86_64_assign_r8_simm_dxmm,
                        op1 = CConst, op2 = CNone, result = CXmm, clobber_list = new hardware_location[] { Rdi }
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.assign_r4, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_r4_xmm_xmmmem,
                        op1 = CXmmPtrMem, op2 = CNone, result = CXmm
                    },
                    new opcode_choice { code_emitter = x86_64_assign_r4_xmmmem_xmm,
                        op1 = CXmm, op2 = CNone, result = CXmmPtrMem
                    },
                    new opcode_choice { code_emitter = x86_64_assign_r4_simm_dxmm,
                        op1 = CConst, op2 = CNone, result = CXmm, clobber_list = new hardware_location[] { Rdi }
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_r4, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_i48r48_gprmem_xmm,
                        op1 = CGprMem, op2 = CNone, result = CXmm
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.conv_i4_r8, output_opcodes[ThreeAddressCode.Op.conv_i4_r4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_r4, output_opcodes[ThreeAddressCode.Op.conv_i4_r4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_r8, output_opcodes[ThreeAddressCode.Op.conv_i4_r4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_r4, output_opcodes[ThreeAddressCode.Op.conv_i4_r4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_r8, output_opcodes[ThreeAddressCode.Op.conv_i4_r4]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_r4_i4, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_r48i48_dgpr_sxmmmem,
                        op1 = CXmmPtrMem, op2 = CNone, result = CGpr
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.conv_r4_i8, output_opcodes[ThreeAddressCode.Op.conv_r4_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_r4_i, output_opcodes[ThreeAddressCode.Op.conv_r4_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_r8_i4, output_opcodes[ThreeAddressCode.Op.conv_r4_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_r8_i8, output_opcodes[ThreeAddressCode.Op.conv_r4_i4]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_r8_i, output_opcodes[ThreeAddressCode.Op.conv_r4_i4]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_r4_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_r4r8_dxmm_sxmmmem,
                        op1 = CXmmPtrMem, op2 = CNone, result = CXmm
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.conv_r8_r4, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_conv_r8r4_dxmm_sxmmmem,
                        op1 = CXmmPtrMem, op2 = CNone, result = CXmm
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.sqrt_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_sqrt_r8_dxmm_sxmmmem,
                        op1 = CXmmPtrMem, op2 = CNone, result = CXmm
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.assign_vt, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_memcpy_srsi_drdi,
                        op1 = CRsi, op2 = CNone, result = CRdi, clobber_list = new hardware_location[] { Rcx }
                    },
                    new opcode_choice { code_emitter = x86_64_memcpy_srsi_dmem,
                        op1 = CRsi, op2 = CNone, result = CMemPtr, clobber_list = new hardware_location[] { Rdi, Rcx }
                    },
                    new opcode_choice { code_emitter = x86_64_memcpy_smem_drdi,
                        op1 = CMemPtr, op2 = CNone, result = CRdi, clobber_list = new hardware_location[] { Rsi, Rcx }
                    },
                    new opcode_choice { code_emitter = x86_64_memcpy_smem_dmem,
                        op1 = CMemPtr, op2 = CNone, result = CMemPtr, clobber_list = new hardware_location[] { Rsi, Rdi, Rcx }
                    }
                },
                recommended_O1 = Rsi,
                recommended_R = Rdi
            });
            output_opcodes.Add(ThreeAddressCode.Op.ldobj_vt, output_opcodes[ThreeAddressCode.Op.assign_vt]);

            output_opcodes.Add(ThreeAddressCode.Op.stobj_vt, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_stobj_vt,
                        op1 = CRdi, op2 = CMemPtr, result = CNone, clobber_list = new hardware_location[] { Rcx, Rsi }
                    },
                    new opcode_choice { code_emitter = x86_64_stobj_vt,
                        op1 = CRdi, op2 = CRsi, result = CNone, clobber_list = new hardware_location[] { Rcx }
                    }
                },
                recommended_O1 = Rdi,
                recommended_O2 = Rsi
            });
            
            output_opcodes.Add(ThreeAddressCode.Op.ldobj_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_ldobj_r8_sgprptr_dxmm,
                        op1 = CGpr, op2 = CNone, result = CXmm
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.ldobj_r4, output_opcodes[ThreeAddressCode.Op.ldobj_r8]);

            output_opcodes.Add(ThreeAddressCode.Op.neg_i8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_negi48_sgprmem_dop1,
                        op1 = CGprMem, op2 = CNone, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.neg_i4, output_opcodes[ThreeAddressCode.Op.neg_i8]);
            output_opcodes.Add(ThreeAddressCode.Op.neg_i, output_opcodes[ThreeAddressCode.Op.neg_i8]);

            output_opcodes.Add(ThreeAddressCode.Op.not_i8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_noti48_sgprmem_dop1,
                        op1 = CGprMem, op2 = CNone, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.not_i4, output_opcodes[ThreeAddressCode.Op.not_i8]);
            output_opcodes.Add(ThreeAddressCode.Op.not_i, output_opcodes[ThreeAddressCode.Op.not_i8]);

            output_opcodes.Add(ThreeAddressCode.Op.alloca_i, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_alloca_i48_sgpr,
                        op1 = CGpr, op2 = CNone, result = CGprMem
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.alloca_i4, output_opcodes[ThreeAddressCode.Op.alloca_i]);

            output_opcodes.Add(ThreeAddressCode.Op.conv_i_i8sx, output_opcodes[ThreeAddressCode.Op.assign_i8]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_isx, output_opcodes[ThreeAddressCode.Op.assign_i8]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_u8zx, output_opcodes[ThreeAddressCode.Op.assign_i8]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i_uzx, output_opcodes[ThreeAddressCode.Op.assign_i8]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_isx, output_opcodes[ThreeAddressCode.Op.assign_i8]);
            output_opcodes.Add(ThreeAddressCode.Op.conv_i8_uzx, output_opcodes[ThreeAddressCode.Op.assign_i8]);

            output_opcodes.Add(ThreeAddressCode.Op.zeromem, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_zeromem_o1rdi_o2const,
                        op1 = CRdi, op2 = CConst, result = CNone, clobber_list = new hardware_location[] { Rax, Rcx }
                    }
                },
                recommended_O1 = Rdi
            });

            output_opcodes.Add(ThreeAddressCode.Op.add_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_add_r8_ds1xmm_s2_xmmmem,
                        op1 = CXmm, op2 = CXmmPtrMem, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.add_r4, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_add_r4_ds1xmm_s2_xmmmem,
                        op1 = CXmm, op2 = CXmmPtrMem, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.sub_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_sub_r8_ds1xmm_s2_xmmmem,
                        op1 = CXmm, op2 = CXmmPtrMem, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.sub_r4, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_sub_r4_ds1xmm_s2_xmmmem,
                        op1 = CXmm, op2 = CXmmPtrMem, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.mul_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_mul_r8_ds1xmm_s2_xmmmem,
                        op1 = CXmm, op2 = CXmmPtrMem, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.mul_r4, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_mul_r4_ds1xmm_s2_xmmmem,
                        op1 = CXmm, op2 = CXmmPtrMem, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.div_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_div_r8_ds1xmm_s2_xmmmem,
                        op1 = CXmm, op2 = CXmmPtrMem, result = COp1
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.div_r4, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_div_r4_ds1xmm_s2_xmmmem,
                        op1 = CXmm, op2 = CXmmPtrMem, result = COp1
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.neg_r8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_neg_r48_dop1_sxmm,
                        op1 = CXmm, op2 = CNone, result = COp1, clobber_list = new hardware_location[] { Rdi }
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.neg_r4, output_opcodes[ThreeAddressCode.Op.neg_r8]);

            output_opcodes.Add(ThreeAddressCode.Op.assign_virtftnptr, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_virtftnptr_smem_dmem,
                        op1 = CMemPtr, op2 = CNone, result = CMemPtr, clobber_list = new hardware_location[] { Xmm15 }
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.assign_to_virtftnptr, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_to_virtftnptr_o1gprmem_o2_gprmem_dmem,
                        op1 = CGprMemPtr, op2 = CGprMemPtr, result = CMemPtr, clobber_list = new hardware_location[] { Rdi, Rcx }
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.assign_from_virtftnptr_ptr, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_from_virtftn_ptr_o1mem_dgprmem,
                        op1 = CMemPtr, op2 = CNone, result = CGprMemPtr, clobber_list = new hardware_location[] { Rdi, Rcx }
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.assign_from_virtftnptr_thisadjust, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_assign_from_virtftn_thisadjust_o1mem_dgprmem,
                        op1 = CMemPtr, op2 = CNone, result = CGprMemPtr, clobber_list = new hardware_location[] { Rdi, Rcx }
                    }
                }
            });

            /*output_opcodes.Add(ThreeAddressCode.Op.try_acquire_i8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_try_acquire_i8_dgpr_s1gpr_s2gpr,
                        op1 = CGpr, op2 = CGpr, result = CGpr, clobber_list = new hardware_location[] { Rax, Rdi }
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.release_i8, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_release_i8_s1gpr_s2rax,
                        op1 = CGpr, op2 = CRax, result = CNone, clobber_list = new hardware_location[] { Rsi, Rdi }
                    }
                }
            });*/

            output_opcodes.Add(ThreeAddressCode.Op.ldcatchobj, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_ldcatchobj_gprmem,
                        op1 = CNone, op2 = CNone, result = CGprMemPtr
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.ldmethinfo, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_ldmethinfo,
                        op1 = CNone, op2 = CNone, result = CGprMemPtr
                    }
                }
            });

            output_opcodes.Add(ThreeAddressCode.Op.break_, new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_break,
                        op1 = CNone, op2 = CNone, result = CNone
                    }
                }
            });


            // Arch specific opcodes
            misc_opcodes.Add("set_RSP", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_set_RSP,
                        op1 = CGpr, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("set_RBP", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_set_RBP,
                        op1 = CGpr, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("set_Cr0", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_set_Cr0,
                        op1 = CGpr, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("set_Cr2", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_set_Cr2,
                        op1 = CGpr, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("set_Cr3", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_set_Cr3,
                        op1 = CGpr, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("set_Cr4", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_set_Cr4,
                        op1 = CGpr, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("get_RSP", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_get_RSP,
                        op1 = CNone, op2 = CNone, result = CGpr
                    }
                }
            });

            misc_opcodes.Add("get_RBP", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_get_RBP,
                        op1 = CNone, op2 = CNone, result = CGpr
                    }
                }
            });

            misc_opcodes.Add("get_Cr0", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_get_Cr0,
                        op1 = CNone, op2 = CNone, result = CGpr
                    }
                }
            });

            misc_opcodes.Add("get_Cr2", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_get_Cr2,
                        op1 = CNone, op2 = CNone, result = CGpr
                    }
                }
            });

            misc_opcodes.Add("get_Cr3", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_get_Cr3,
                        op1 = CNone, op2 = CNone, result = CGpr
                    }
                }
            });

            misc_opcodes.Add("get_Cr4", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_get_Cr4,
                        op1 = CNone, op2 = CNone, result = CGpr
                    }
                }
            });

            misc_opcodes.Add("Sti", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_Sti,
                        op1 = CNone, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("Cli", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_Cli,
                        op1 = CNone, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("Break", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_Break,
                        op1 = CNone, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("Lidt", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_Lidt,
                        op1 = CGpr, op2 = CNone, result = CNone
                    }
                }
            });

            misc_opcodes.Add("round_double", new output_opcode
            {
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = x86_64_round_double_xmm_xmm,
                        op1 = CXmm, op2 = CNone, result = CXmm, clobber_list = new hardware_location[] { Rax }
                    }
                }
            });
        }

        IEnumerable<OutputBlock> x86_64_enter(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();
            CodeBlock enter_save = new CodeBlock();
            ret.Add(enter_save);
            state.CalleePreserveRegistersSaves.Add(enter_save);
            if (state.methinfo_pointer != null)
            {
                x86_64_assign(state.methinfo_pointer, op1.hardware_loc, ret);
                state.UsedLocations.Add(state.methinfo_pointer);
            }
            return ret;
        }

        IEnumerable<OutputBlock> x86_64_endfinally(ThreeAddressCode.Op op, var result, var op1, var op2, ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(new CodeBlock(new byte[] { 0xc3 }, new x86_64_Instruction { opcode = "ret" }));
        }

        IEnumerable<OutputBlock> x86_64_ret(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> blocks = new List<OutputBlock>();

            // If an ISR we need to restore the registers
            if (((x86_64_AssemblerState)state).isr)
            {
                if (ia == IA.x86_64)
                {
                    blocks.Add(new CodeBlock(EncAddOpcode(R15, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = R15 }));
                    blocks.Add(new CodeBlock(EncAddOpcode(R14, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = R14 }));
                    blocks.Add(new CodeBlock(EncAddOpcode(R13, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = R13 }));
                    blocks.Add(new CodeBlock(EncAddOpcode(R12, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = R12 }));
                    blocks.Add(new CodeBlock(EncAddOpcode(R11, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = R11 }));
                    blocks.Add(new CodeBlock(EncAddOpcode(R10, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = R10 }));
                    blocks.Add(new CodeBlock(EncAddOpcode(R9, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = R9 }));
                    blocks.Add(new CodeBlock(EncAddOpcode(R8, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = R8 }));
                }
                blocks.Add(new CodeBlock(EncAddOpcode(Rsi, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = Rsi }));
                blocks.Add(new CodeBlock(EncAddOpcode(Rdi, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = Rdi }));
                blocks.Add(new CodeBlock(EncAddOpcode(Rdx, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = Rdx }));
                blocks.Add(new CodeBlock(EncAddOpcode(Rcx, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = Rcx }));
                blocks.Add(new CodeBlock(EncAddOpcode(Rbx, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = Rbx }));
                blocks.Add(new CodeBlock(EncAddOpcode(Rax, false, 0x58), new x86_64_Instruction { opcode = "pop", Operand1 = Rax }));
            }

            // If a syscall or uninterruptible, we need to restore rflags from the image on the stack
            if (state.uninterruptible_method)
                blocks.Add(new CodeBlock(new byte[] { 0x9d }, new x86_64_Instruction { opcode = "popfq" }));
            else if (state.syscall)
                blocks.Add(new CodeBlock(new byte[] { 0x9d }, new x86_64_Instruction { opcode = "popfq" }));

            // If we pushed ebx (in i586 mode) at the start then restore it
            if ((ia == IA.i586) && (((x86_64_AssemblerState)state).i586_stored_ebp != null))
            {
                // store previous ebx
                x86_64_assign(Rbx, ((x86_64_AssemblerState)state).i586_stored_ebp, blocks);
            }

            // Restore callee preserved registers
            CodeBlock callee_restore = new CodeBlock();
            blocks.Add(callee_restore);
            state.CalleePreserveRegistersRestores.Add(callee_restore);
            
            // leave, 0xc3
            blocks.Add(new CodeBlock(new List<byte> { 0xc9 }, new x86_64_Instruction { opcode = "leave" }));

            // If an ISR, pop the error code (if any) and iret, else just ret
            if (((x86_64_AssemblerState)state).isr)
            {
                if (state.local_args.Count > 1)
                    throw new Exception("Interrupt service routine with > 1 argument!");
                else if (state.local_args.Count == 1)
                    blocks.Add(new CodeBlock(EncOpcode(0, Rsp, 3, true, 0, 0x83), new byte[] { 0x08 }, new x86_64_Instruction[] { new x86_64_Instruction { opcode = "add", Operand1 = Rsp, Operand2 = new const_location { c = 0x08 } } }));

                blocks.Add(new CodeBlock(new byte[] { 0x48, 0xcf }, new x86_64_Instruction { opcode = "iret" }));
            }
            else
                blocks.Add(new CodeBlock(new byte[] { 0xc3 }, new x86_64_Instruction { opcode = "ret" }));

            return blocks;
        }

        IEnumerable<OutputBlock> x86_64_alloca_i48_sgpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Allocate some space on the stack
            /* Do: sub rsp, size; mov dest, rsp
             * This works because all other local variables are addressed in reference to rbp
             */

            return OBList(EncOpcode(Rsp, op1.hardware_loc, 3, (ia == IA.x86_64), 0, 0x2b),
                EncOpcode(Rsp, result.hardware_loc, 3, (ia == IA.x86_64), 0, 0x89));
        }

        IEnumerable<OutputBlock> x86_64_nop(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            if (emitNop)
                return OBList(new byte[] { 0x90 });
            else
                return new List<OutputBlock>();
        }

        IEnumerable<OutputBlock> x86_64_break(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(new CodeBlock(new byte[] { 0xcc }, new x86_64_Instruction { opcode = "int3" }));
        }
    }
}
