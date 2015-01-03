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
using libasm;
using libtysila.frontend.cil;

namespace libtysila
{
    public partial class x86_64_Assembler : LSB_Assembler
    {
        public bool emitNop = false;

        enum OutputType { x86_64_large_elf64, x86_64_small_elf64, x86_64_jit, i586_elf64, i586_elf, i586_jit };
        internal enum IA { x86_64, i586 };
        internal enum CModel { ia32, small, kernel, large };
        OutputType OType;
        internal IA ia;
        internal CModel cm;

        const int HIGH_REG_START = 8;

        public static x86_64_gpr Rax { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rax }; } }
        public static x86_64_gpr Rbx { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rbx }; } }
        public static x86_64_gpr Rcx { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rcx }; } }
        public static x86_64_gpr Rdx { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rdx }; } }
        public static x86_64_gpr Rsi { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rsi }; } }
        public static x86_64_gpr Rdi { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rdi }; } }
        public static x86_64_gpr Rsp { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rsp }; } }
        public static x86_64_gpr Rbp { get { return new x86_64_gpr { reg = x86_64_gpr.RegId.rbp }; } }

        public static libasm.multiple_hardware_location RaxRdx { get { return new multiple_hardware_location(Rax, Rdx); } }

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
        hloc_constraint CLabel { get { return new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = new hardware_addressoflabel("", false) }; } }
        hloc_constraint CGprMemPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem, CGprPtr } }; } }
        hloc_constraint CGprMemConst { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CGpr, CMem, CConst } }; } }
        hloc_constraint CXmmMem { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CXmm, CMem64 } }; } }
        hloc_constraint CXmmPtrMem { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CXmm, CGprPtr, CMem } }; } }
        hloc_constraint CMemPtr { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CMem, CGprPtr } }; } }

        hloc_constraint CGprExceptRax { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRbx, CRcx, CRdx, CRdi, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRbx { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRcx, CRdx, CRdi, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRcx { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRbx, CRdx, CRdi, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRdx { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRbx, CRcx, CRdi, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRsi { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRbx, CRcx, CRdx, CRdi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }
        hloc_constraint CGprExceptRdi { get { return new hloc_constraint { constraint = hloc_constraint.c_.List, specific_list = new List<hloc_constraint> { CRax, CRbx, CRcx, CRdx, CRsi, CR8, CR9, CR10, CR11, CR12, CR13, CR14, CR15 } }; } }

        public class x86_64_RelocationType : RelocationBlock.RelocationType
        {
            internal string name;
            public override string ToString()
            {
                return name;
            }
        }

        internal static x86_64_RelocationType R_X86_64_64 { get { return new x86_64_RelocationType { name = "R_X86_64_64", type = 1 }; } }
        internal static x86_64_RelocationType R_X86_64_PC32 { get { return new x86_64_RelocationType { name = "R_X86_64_PC32", type = 2 }; } }
        internal static x86_64_RelocationType R_X86_64_GOT32 { get { return new x86_64_RelocationType { name = "R_X86_64_GOT32", type = 3 }; } }
        internal static x86_64_RelocationType R_X86_64_PLT32 { get { return new x86_64_RelocationType { name = "R_X86_64_PLT32", type = 4 }; } }
        internal static x86_64_RelocationType R_X86_64_COPY { get { return new x86_64_RelocationType { name = "R_X86_64_COPY", type = 5 }; } }
        internal static x86_64_RelocationType R_X86_64_GLOB_DAT { get { return new x86_64_RelocationType { name = "R_X86_64_GLOB_DAT", type = 6 }; } }
        internal static x86_64_RelocationType R_X86_64_JUMP_SLOT { get { return new x86_64_RelocationType { name = "R_X86_64_JUMP_SLOT", type = 7 }; } }
        internal static x86_64_RelocationType R_X86_64_RELATIVE { get { return new x86_64_RelocationType { name = "R_X86_64_RELATIVE", type = 8 }; } }
        internal static x86_64_RelocationType R_X86_64_GOTPCREL { get { return new x86_64_RelocationType { name = "R_X86_64_GOTPCREL", type = 9 }; } }
        internal static x86_64_RelocationType R_X86_64_32 { get { return new x86_64_RelocationType { name = "R_X86_64_32", type = 10 }; } }
        internal static x86_64_RelocationType R_X86_64_32S { get { return new x86_64_RelocationType { name = "R_X86_64_32S", type = 11 }; } }
        internal static x86_64_RelocationType R_X86_64_16 { get { return new x86_64_RelocationType { name = "R_X86_64_16", type = 12 }; } }
        internal static x86_64_RelocationType R_X86_64_PC16 { get { return new x86_64_RelocationType { name = "R_X86_64_PC16", type = 13 }; } }
        internal static x86_64_RelocationType R_X86_64_8 { get { return new x86_64_RelocationType { name = "R_X86_64_8", type = 14 }; } }
        internal static x86_64_RelocationType R_X86_64_PC8 { get { return new x86_64_RelocationType { name = "R_X86_64_PC8", type = 15 }; } }

        internal static x86_64_RelocationType R_386_32 { get { return new x86_64_RelocationType { name = "R_386_32", type = 1 }; } }
        internal static x86_64_RelocationType R_386_PC32 { get { return new x86_64_RelocationType { name = "R_386_PC32", type = 2 }; } }
        internal static x86_64_RelocationType R_386_GOT32 { get { return new x86_64_RelocationType { name = "R_386_GOT32", type = 3 }; } }
        internal static x86_64_RelocationType R_386_PLT32 { get { return new x86_64_RelocationType { name = "R_386_PLT32", type = 4 }; } }
        internal static x86_64_RelocationType R_386_COPY { get { return new x86_64_RelocationType { name = "R_386_COPY", type = 5 }; } }
        internal static x86_64_RelocationType R_386_GLOB_DAT { get { return new x86_64_RelocationType { name = "R_386_GLOB_DAT", type = 6 }; } }
        internal static x86_64_RelocationType R_386_JMP_SLOT { get { return new x86_64_RelocationType { name = "R_386_JMP_SLOT", type = 7 }; } }
        internal static x86_64_RelocationType R_386_RELATIVE { get { return new x86_64_RelocationType { name = "R_386_RELATIVE", type = 8 }; } }
        internal static x86_64_RelocationType R_386_GOTOFF { get { return new x86_64_RelocationType { name = "R_386_GOTOFF", type = 9 }; } }
        internal static x86_64_RelocationType R_386_GOTPC { get { return new x86_64_RelocationType { name = "R_386_GOTPC", type = 10 }; } }


        public override RelocationBlock.RelocationType GetCodeToCodeRelocType()
        {
            switch (cm)
            {
                case CModel.kernel:
                case CModel.large:
                case CModel.small:
                    return R_X86_64_PC32;
                case CModel.ia32:
                    return R_386_PC32;
            }
            throw new NotSupportedException();
        }

        public override RelocationBlock.RelocationType GetCodeToDataRelocType()
        {
            if (Options.PIC)
            {
                switch (cm)
                {
                    case CModel.kernel:
                    case CModel.large:
                    case CModel.small:
                        return R_X86_64_GOTPCREL;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (cm)
                {
                    case CModel.kernel:
                        return R_X86_64_32S;
                    case CModel.small:
                        return R_X86_64_32;
                    case CModel.large:
                        return R_X86_64_64;
                    case CModel.ia32:
                        return R_386_32;
                }
            }
            throw new NotSupportedException();
        }

        public override RelocationBlock.RelocationType GetDataToCodeRelocType()
        {
            switch (ia)
            {
                case IA.x86_64:
                    return R_X86_64_64;
                case IA.i586:
                    return R_386_32;
                default:
                    throw new NotSupportedException();
            }
        }

        public override RelocationBlock.RelocationType GetDataToDataRelocType()
        {
            switch (ia)
            {
                case IA.x86_64:
                    return R_X86_64_64;
                case IA.i586:
                    return R_386_32;
                default:
                    throw new NotSupportedException();
            }
        }

        internal override void arch_init_opcodes()
        {
            // ia may not be initialized yet so we have to do it instead
            if (Arch.InstructionSet == "i586")
            {
                ia = IA.i586;
                cm = CModel.ia32;
            }
            else
            {
                ia = IA.x86_64;
                if (Arch.InstructionSet == "x86_64s")
                    cm = CModel.kernel;
                else
                    cm = CModel.large;
            }
        }

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
            if (vs.needs_int64 && (ia == IA.x86_64))
                return CGprMem64;
            if (vs.needs_int64 && (ia == IA.i586))
                return C2Gpr;
            throw new NotSupportedException();
        }

        public x86_64_Assembler(Architecture arch, FileLoader fileLoader, MemberRequestor memberRequestor, AssemblerOptions options) : base(arch, fileLoader, memberRequestor, options)
        {
            if (((arch.InstructionSet == "x86_64l") || (arch.InstructionSet == "x86_64")) && (arch.OutputFormat == "elf64"))
            {
                OType = OutputType.x86_64_large_elf64;
                ia = IA.x86_64;
                cm = CModel.large;
            }
            else if ((arch.InstructionSet == "x86_64s") && (arch.OutputFormat == "elf64"))
            {
                OType = OutputType.x86_64_small_elf64;
                ia = IA.x86_64;
                cm = CModel.kernel;
            }
            else if ((arch.InstructionSet == "x86_64") && (arch.OutputFormat == "jit"))
            {
                OType = OutputType.x86_64_jit;
                ia = IA.x86_64;
                cm = CModel.large;
            }
            else if ((arch.InstructionSet == "i586") && (arch.OutputFormat == "elf64"))
            {
                OType = OutputType.i586_elf64;
                ia = IA.i586;
                cm = CModel.ia32;
            }
            else if ((arch.InstructionSet == "i586") && (arch.OutputFormat == "elf"))
            {
                OType = OutputType.i586_elf;
                ia = IA.i586;
                cm = CModel.ia32;
            }
            else if ((arch.InstructionSet == "i586") && (arch.OutputFormat == "jit"))
            {
                OType = OutputType.i586_jit;
                ia = IA.i586;
                cm = CModel.ia32;
            }
            else
                throw new Exception("Invalid architecture: " + arch.ToString());

            if (arch.OutputFormat == "jit")
                is_jit = true;

            if (arch.OperatingSystem != "tysos")
                throw new Exception("Invalid operating system: " + arch.OperatingSystem);

            options.RegAlloc = AssemblerOptions.RegisterAllocatorType.graphcolour;

            InitTybelOpcodes();
        }

        private void InitTybelOpcodes()
        {
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldloca_s)].TybelEncoder = x86_64.cil.loc.tybel_ldloca;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.ldloca)].TybelEncoder = x86_64.cil.loc.tybel_ldloca;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_0)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_1)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_2)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_3)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_4)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_5)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_6)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_7)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_8)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_m1)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i4_s)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_i8)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_i8;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_r4)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_r4;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldc_r8)].TybelEncoder = x86_64.cil.ldc.tybel_ldc_r8;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldnull)].TybelEncoder = x86_64.cil.ldc.tybel_ldnull;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ret)].TybelEncoder = x86_64.cil.ret.tybel_ret;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.ceq)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.cgt)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.cgt_un)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.clt)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.clt_un)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.stloc_0)].TybelEncoder = x86_64.cil.loc.tybel_stloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.stloc_1)].TybelEncoder = x86_64.cil.loc.tybel_stloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.stloc_2)].TybelEncoder = x86_64.cil.loc.tybel_stloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.stloc_3)].TybelEncoder = x86_64.cil.loc.tybel_stloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.stloc_s)].TybelEncoder = x86_64.cil.loc.tybel_stloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.stloc)].TybelEncoder = x86_64.cil.loc.tybel_stloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldloc_0)].TybelEncoder = x86_64.cil.loc.tybel_ldloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldloc_1)].TybelEncoder = x86_64.cil.loc.tybel_ldloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldloc_2)].TybelEncoder = x86_64.cil.loc.tybel_ldloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldloc_3)].TybelEncoder = x86_64.cil.loc.tybel_ldloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldloc_s)].TybelEncoder = x86_64.cil.loc.tybel_ldloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.ldloc)].TybelEncoder = x86_64.cil.loc.tybel_ldloc;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.brfalse)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.brfalse_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.brtrue)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.brtrue_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.beq)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.beq_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bge)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bge_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bge_un)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bge_un_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bgt)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bgt_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bgt_un)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bgt_un_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ble)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ble_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ble_un)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ble_un_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.blt)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.blt_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.blt_un)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.blt_un_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bne_un)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.bne_un_s)].TybelEncoder = x86_64.cil.brset.tybel_brset;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.br)].TybelEncoder = x86_64.cil.brset.tybel_br;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.br_s)].TybelEncoder = x86_64.cil.brset.tybel_br;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.starg_s)].TybelEncoder = x86_64.cil.arg.tybel_starg;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.starg)].TybelEncoder = x86_64.cil.arg.tybel_starg;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_0)].TybelEncoder = x86_64.cil.arg.tybel_ldarg;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_1)].TybelEncoder = x86_64.cil.arg.tybel_ldarg;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_2)].TybelEncoder = x86_64.cil.arg.tybel_ldarg;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_3)].TybelEncoder = x86_64.cil.arg.tybel_ldarg;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_s)].TybelEncoder = x86_64.cil.arg.tybel_ldarg;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.ldarg)].TybelEncoder = x86_64.cil.arg.tybel_ldarg;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarga_s)].TybelEncoder = x86_64.cil.arg.tybel_ldarga;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.ldarga)].TybelEncoder = x86_64.cil.arg.tybel_ldarga;
            OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.endfinally)].TybelEncoder = x86_64.cil.exceptions.tybel_endfinally;
        }

        public static Assembler.Architecture[] ListAssemblerArchitectures()
        {
            return new Assembler.Architecture[] {
                new Assembler.Architecture { _instruction_set = "x86_64", _oformat = "elf64", _os = "tysos" },
                new Assembler.Architecture { _instruction_set = "x86_64s", _oformat = "elf64", _os = "tysos" },
                new Assembler.Architecture { _instruction_set = "x86_64", _oformat = "jit", _os = "tysos" },
                new Assembler.Architecture { _instruction_set = "i586", _oformat = "elf64", _os = "tysos", _extra_ops = new List<string> { "addend_in_code" } },
                new Assembler.Architecture { _instruction_set = "i586", _oformat = "elf", _os = "tysos" , _extra_ops = new List<string> { "addend_in_code" } },
                new Assembler.Architecture { _instruction_set = "i586", _oformat = "jit", _os = "tysos" }
            };
        }

        public override Bitness GetBitness()
        {
            switch (ia)
            {
                case IA.i586:
                    return Bitness.Bits32;
                case IA.x86_64:
                    return Bitness.Bits64;
                default:
                    throw new Exception();
            }
        }

        internal class x86_64_NegHardwareStackAllocator : HardwareStackAllocator, IStackAllocator
        {
            protected override bool AllocatesDownwards()
            { return true; }
            protected override int GetStackAlign()
            { return 8; }
        }

        internal x86_64_NegHardwareStackAllocator stack_alloc = new x86_64_NegHardwareStackAllocator();

        internal override IEnumerable<hardware_location> GetAllHardwareLocationsOfType(Type type, hardware_location example)
        {
            if (type == typeof(x86_64_gpr))
            {
                yield return x86_64_Assembler.Rax;
                yield return x86_64_Assembler.Rbx;
                yield return x86_64_Assembler.Rcx;
                yield return x86_64_Assembler.Rdx;
                yield return x86_64_Assembler.Rsi;
                yield return x86_64_Assembler.Rdi;
                if (ia == IA.x86_64)
                {
                    yield return x86_64_Assembler.R8;
                    yield return x86_64_Assembler.R9;
                    yield return x86_64_Assembler.R10;
                    yield return x86_64_Assembler.R11;
                    yield return x86_64_Assembler.R12;
                    yield return x86_64_Assembler.R13;
                    yield return x86_64_Assembler.R14;
                    yield return x86_64_Assembler.R15;
                }
                yield break;
            }
            else if (type == typeof(x86_64_xmm))
            {
                yield return x86_64_Assembler.Xmm0;
                yield return x86_64_Assembler.Xmm1;
                yield return x86_64_Assembler.Xmm2;
                yield return x86_64_Assembler.Xmm3;
                yield return x86_64_Assembler.Xmm4;
                yield return x86_64_Assembler.Xmm5;
                yield return x86_64_Assembler.Xmm6;
                yield return x86_64_Assembler.Xmm7;
                yield return x86_64_Assembler.Xmm8;
                yield return x86_64_Assembler.Xmm9;
                yield return x86_64_Assembler.Xmm10;
                yield return x86_64_Assembler.Xmm11;
                yield return x86_64_Assembler.Xmm12;
                yield return x86_64_Assembler.Xmm13;
                yield return x86_64_Assembler.Xmm14;
                yield return x86_64_Assembler.Xmm15;
                yield break;
            }
            else if (type == typeof(hardware_stackloc))
            {
                yield break;
            }
            else
                yield break;
        }

        internal override bool IsLocationAllowed(hardware_location hloc)
        {
            if ((ia == IA.i586) && (hloc is x86_64_gpr) && (((x86_64_gpr)hloc).is_extended))
                return false;
            return true;
        }

        internal override int GetSizeOf(Signature.Param p)
        {
            switch (p.CliType(this))
            {
                case CliType.F64:
                    return 8;
                case CliType.F32:
                    return 4;
                case CliType.int32:
                    return 4;
                case CliType.int64:
                    return 8;
                case CliType.native_int:
                case CliType.O:
                case CliType.reference:
                case CliType.void_:
                case CliType.virtftnptr:
                    if(ia == IA.x86_64)
                        return 8;
                    else
                        return 4;                    
                case CliType.vt:
                    return GetSizeOfType(p);
                    /*
                case CliType.virtftnptr:
                    if (ia == IA.x86_64)
                        return 16;
                    else
                        return 8;*/
                default:
                    throw new NotSupportedException();
            }
        }
        public override int GetPackedSizeOf(Signature.Param p)
        {
            if (p.Type is Signature.BaseType)
            {
                Signature.BaseType bt = p.Type as Signature.BaseType;
                switch (bt.Type)
                {
                    case BaseType_Type.I1:
                    case BaseType_Type.Byte:
                    case BaseType_Type.Boolean:
                    case BaseType_Type.U1:
                        return 1;
                    case BaseType_Type.Char:
                    case BaseType_Type.I2:
                    case BaseType_Type.U2:
                        return 2;
                }
            }
            return GetSizeOf(p);
        }
        private List<byte> EncOpcode(int r, int rm, int mod, byte sib, bool rexw, int disp, params byte[] opcodes)
        { return EncOpcode(r, rm, mod, sib, rexw, disp, false, opcodes); }

        private List<byte> EncOpcode(int r, int rm, int mod, byte sib, bool rexw, int disp, bool rm8, params byte[] opcodes)
        {
            if (opcodes.Length == 0)
                throw new Exception("opcodes is empty");

            List<byte> ret = new List<byte>();
            byte rex = 0x0;
            if (rm8 && (ia == IA.x86_64))
            {
                if ((rm == (int)Rdi.reg) || (rm == (int)Rsi.reg))
                    rex |= Rex(true);
            }
            if(rexw)
                rex |= RexW(true);

            if (r >= 8)
                rex |= RexR(true);
            if (rm >= 8)
                rex |= RexB(true);

            /* 0xf2,0xf3,0x66,0xf0 prefixes comes before rex */
            if ((opcodes.Length > 0) && ((opcodes[0] == 0xf2) || (opcodes[0] == 0xf3) || (opcodes[0] == 0x66) || (opcodes[0] == 0xf0)))
                ret.Add(opcodes[0]);
            if (rex != 0x0)
            {
                if (Arch.InstructionSet == "i586")
                    throw new Exception("REX prefix invalid in i586 mode");
                ret.Add(rex);
            }
            foreach (byte b in opcodes)
            {
                if ((b != 0xf2) && (b != 0xf3) && (b != 0x66) && (b != 0xf0))
                    ret.Add(b);
            }
            if ((mod == 0x0) || (mod == 0x1) || (mod == 0x2))
            {
                if (disp == 0)       
                    mod = 0x0;
                else if ((disp < SByte.MinValue) || (disp > SByte.MaxValue))
                    mod = 0x2;
                else
                    mod = 0x1;
            }
            ret.Add(ModRM((byte)mod, (byte)(r % 8), (byte)(rm % 8)));
            if (((mod == 0) || (mod == 0x1) || (mod == 0x2)) && (rm == 0x4))    // if rm == 4 we need an SIB
                ret.Add(sib);

            if (mod == 0x1)
                ret.AddRange(ToByteArray(Convert.ToSByte(disp)));
            else if (mod == 0x2)
                ret.AddRange(ToByteArray(Convert.ToInt32(disp)));
            return ret;
        }
        private List<byte> EncOpcode(object r, object rm, int mod, bool rexw, int disp,
            params byte[] opcodes)
        { return EncOpcode(r, rm, mod, rexw, disp, false, opcodes); }

        private List<byte> EncOpcode(object r, object rm, int mod, bool rexw, int disp, bool rm8,
            params byte[] opcodes)
        {
            int rmval = 0;
            int rval = 0;
            byte sib = 0;
            if (rm is hardware_stackloc)
            {
                mod = 1;
                uint align_val = 0xfffffff8;
                if (ia == IA.i586)
                    align_val = 0xfffffffc;
                disp = (int)((uint)(-((hardware_stackloc)rm).loc - ((hardware_stackloc)rm).size) & align_val);
                rm = new x86_64_gpr { reg = x86_64_gpr.RegId.rbp };
                rmval = (int)((x86_64_gpr)rm).reg;
            }
            else if ((rm is hardware_contentsof) || ((rm is x86_64_gpr) && (mod != 3)))
            {
                hardware_contentsof hco;
                if (rm is hardware_contentsof)
                    hco = rm as hardware_contentsof;
                else
                    hco = new hardware_contentsof { base_loc = rm as hardware_location, const_offset = disp };

                if (hco.base_loc is hardware_stackloc)
                {
                    //throw new Exception("Shouldn't get here");
                    mod = 1;
                    disp = -((hardware_stackloc)hco.base_loc).loc - ((hardware_stackloc)hco.base_loc).size + hco.const_offset;
                    rm = Rbp;
                    rmval = (int)((x86_64_gpr)rm).reg;
                }
                else if (hco.base_loc is hardware_contentsof)
                {
                    throw new Exception("Shouldn't get here");
                    /*mod = 1;
                    disp = ((hardware_contentsof)hco.base_loc).const_offset + hco.const_offset;
                    rmval = (int)((x86_64_gpr)(((hardware_contentsof)hco.base_loc).base_loc)).reg;*/
                }
                else
                {
                    mod = 1;
                    disp = hco.const_offset;
                    rm = hco.base_loc;
                    rmval = (int)((x86_64_gpr)rm).reg;
                }

                if (rmval == 4)
                    sib = 0x24;
            }
            else if (rm.GetType() == typeof(int))
            {
                rmval = (int)rm;
            }
            else if (rm is x86_64_gpr)
                rmval = (int)((x86_64_gpr)rm).reg;
            else if (rm is x86_64_xmm)
                rmval = (int)((x86_64_xmm)rm).xmm;
            else
                throw new NotSupportedException();

            if (r is int)
                rval = (int)r;
            else if (r is x86_64_gpr)
                rval = (int)((x86_64_gpr)r).reg;
            else if (r is x86_64_xmm)
                rval = (int)((x86_64_xmm)r).xmm;
            else
                throw new NotSupportedException();

            return EncOpcode(rval, rmval, mod, sib, rexw, disp, rm8, opcodes);
        }
        private List<byte> EncAddOpcode(x86_64_gpr reg, bool rexw, byte opcode, params byte[] immediates)
        {
            List<byte> ret = new List<byte>();
            byte rex = RexW(rexw);
            rex |= RexB(reg.is_extended);
            if (rex != 0)
                ret.Add(rex);
            ret.Add((byte)(opcode + reg.base_val));
            foreach (byte b in immediates)
                ret.Add(b);
            return ret;
        }

        private byte ModRM(byte mod, byte reg, byte rm)
        { return (byte)((mod << 6) | (reg << 3) | (rm)); }

        private byte Rex(bool present)
        {
            if (present)
                return 0x40;
            else
                return 0x00;
        }

        private byte RexW(bool present)
        {
            if (present)
                return 0x48;
            else
                return 0x00;
        }

        private byte RexR(bool present)
        {
            if (present)
                return 0x44;
            else
                return 0x00;
        }

        private byte RexX(bool present)
        {
            if (present)
                return 0x42;
            else
                return 0x00;
        }

        private byte RexB(bool present)
        {
            if (present)
                return 0x41;
            else
                return 0x00;
        }

        internal override List<byte> SwapLocation(hardware_location a, hardware_location b)
        {
            List<byte> r = new List<byte>();
            if ((a is x86_64_gpr) && (b is x86_64_gpr))
            {
                // XCHG
                r.AddRange(EncOpcode(a, b, 3, true, 0, 0x87));
            }
            else
                throw new NotImplementedException();

            return r;
        }

        internal override List<byte> SaveLocation(hardware_location loc)
        {
            List<byte> b = new List<byte>();
            if (loc is x86_64_gpr)
            {
                x86_64_gpr gpr = loc as x86_64_gpr;
                if (gpr.is_extended)
                    b.Add(RexB(true));
                b.Add((byte)(0x50 + gpr.base_val));
            }
            else if (loc is x86_64_xmm)
            {
                // decrement stack counter then store to [rsp]
                b.AddRange(new byte[] { 0x48, 0x83, 0xec, 0x08 });
                b.AddRange(EncOpcode(loc, new hardware_contentsof { base_loc = Rsp }, 0, false, 0, 0x66, 0x0f, 0xd6));  // MOVQ
            }
            else
                throw new NotImplementedException();

            return b;
        }

        internal override List<byte> RestoreLocation(hardware_location loc)
        {
            List<byte> b = new List<byte>();
            if (loc is x86_64_gpr)
            {
                x86_64_gpr gpr = loc as x86_64_gpr;
                if (gpr.is_extended)
                    b.Add(RexB(true));
                b.Add((byte)(0x58 + gpr.base_val));
            }
            else if (loc is x86_64_xmm)
            {
                // restore from [rsp] then increment stack counter
                b.AddRange(EncOpcode(loc, new hardware_contentsof { base_loc = Rsp }, 0, false, 0, 0xf3, 0x0f, 0x7e));  // MOVQ
                b.AddRange(new byte[] { 0x48, 0x83, 0xc4, 0x08 });
            }
            else
                throw new NotImplementedException();

            return b;
        }

        public override uint DataToDataRelocType()
        {
            switch (OType)
            {
                case OutputType.x86_64_large_elf64:
                case OutputType.x86_64_small_elf64:
                case OutputType.x86_64_jit:
                    return x86_64.x86_64_elf64.R_X86_64_64;
                case OutputType.i586_elf64:
                    return x86_64.x86_64_elf64.R_X86_64_32;
                case OutputType.i586_elf:
                    return x86_64.x86_64_elf32.R_386_32;
                default:
                    throw new Exception("Unknown output type");
            }
        }

        internal override byte[] IntPtrByteArray(object v)
        {
            switch (OType)
            {
                case OutputType.i586_elf:
                case OutputType.i586_elf64:
                case OutputType.i586_jit:
                    return ToByteArray(Convert.ToInt32(v));
                default:
                    return ToByteArray(Convert.ToInt64(v));
            }
        }

        internal override object ConvertToI(object v)
        {
            switch (OType)
            {
                case OutputType.i586_elf:
                case OutputType.i586_elf64:
                case OutputType.i586_jit:
                    return Convert.ToInt32(v);
                default:
                    return Convert.ToInt64(v);
            }
        }

        internal override object ConvertToU(object v)
        {
            switch (OType)
            {
                case OutputType.i586_elf:
                case OutputType.i586_elf64:
                case OutputType.i586_jit:
                    return Convert.ToUInt32(v);
                default:
                    return Convert.ToUInt64(v);
            }
        }

        public override string GetCType(BaseType_Type baseType_Type)
        {
            switch (baseType_Type)
            {
                case BaseType_Type.Byte:
                case BaseType_Type.U1:
                case BaseType_Type.Boolean:
                    return "uint8_t";

                case BaseType_Type.Char:
                case BaseType_Type.U2:
                    return "uint16_t";

                case BaseType_Type.U4:
                    return "uint32_t";

                case BaseType_Type.I1:
                    return "int8_t";

                case BaseType_Type.I2:
                    return "int16_t";

                case BaseType_Type.I4:
                    return "int32_t";

                case BaseType_Type.I:
                case BaseType_Type.I8:
                    return "int64_t";

                case BaseType_Type.Object:
                case BaseType_Type.String:
                    {
                        switch (OType)
                        {
                            case OutputType.i586_elf:
                            case OutputType.i586_elf64:
                            case OutputType.i586_jit:
                                return "uint32_t";
                            default:
                                return "uint64_t";
                        }
                    }
                case BaseType_Type.U:
                case BaseType_Type.U8:
                    return "uint64_t";

                default:
                    throw new NotImplementedException();
            }
        }

        internal override int GetSizeOfUncondBr()
        {
            return 5;
        }

        public override IntPtr FromByteArrayI(IList<byte> v)
        {
            switch (OType)
            {
                case OutputType.i586_elf:
                case OutputType.i586_elf64:
                case OutputType.i586_jit:
                    return new IntPtr(FromByteArrayI4(v));
                default:
                    return new IntPtr(FromByteArrayI8(v));
            }
        }
        public override IntPtr FromByteArrayI(IList<byte> v, int offset)
        {
            switch (OType)
            {
                case OutputType.i586_elf:
                case OutputType.i586_elf64:
                case OutputType.i586_jit:
                    return new IntPtr(FromByteArrayI4(v, offset));
                default:
                    return new IntPtr(FromByteArrayI8(v, offset));
            }
        }
        public override UIntPtr FromByteArrayU(IList<byte> v)
        {
            switch (OType)
            {
                case OutputType.i586_elf:
                case OutputType.i586_elf64:
                case OutputType.i586_jit:
                    return new UIntPtr(FromByteArrayU4(v));
                default:
                    return new UIntPtr(FromByteArrayU8(v));
            }
        }
        public override UIntPtr FromByteArrayU(IList<byte> v, int offset)
        {
            switch (OType)
            {
                case OutputType.i586_elf:
                case OutputType.i586_elf64:
                case OutputType.i586_jit:
                    return new UIntPtr(FromByteArrayU4(v, offset));
                default:
                    return new UIntPtr(FromByteArrayU8(v, offset));
            }
        }

        internal override List<OutputBlock> ArchSpecificProlog(AssemblerState state)
        {
            return new List<OutputBlock>();
        }

        internal class x86_64_AssemblerState : AssemblerState
        {
            public bool isr = false;
            public hardware_location i586_stored_ebp = null;
        }

        internal override AssemblerState GetNewAssemblerState()
        {
            return new x86_64_AssemblerState();
        }

        internal override void ArchSpecificStackSetup(Assembler.AssemblerState state, ref int next_lv_loc)
        {
            // The 32 bit architecture needs a register to use in PIC mode to store the address of the GOT
            // We use rbx and store the previous value of rbx here (from the proceeding function)
            if ((ia == IA.i586) && Options.PIC)
            {
                ((x86_64_AssemblerState)state).i586_stored_ebp = new hardware_stackloc { loc = next_lv_loc, size = GetSizeOfPointer() };
                next_lv_loc += GetSizeOfPointer();
            }
        }

        internal override void InterpretMethodCustomAttribute(MethodToCompile mtc, Metadata.CustomAttributeRow car, AssemblerState state)
        {
            Assembler.MethodToCompile camtc = Metadata.GetMTC(car.Type, new TypeToCompile(), null, this);
            
            string caname = Mangler2.MangleMethod(camtc, this);
            if (caname == "_ZX12ISRAttributeM_0_7#2Ector_Rv_P1u1t")
            {
                ((x86_64_AssemblerState)state).isr = true;
                state.call_conv = "isr";
            }
        }

        internal override uint GetTysosFlagsForMethod(MethodToCompile mtc)
        {
            uint ret = base.GetTysosFlagsForMethod(mtc);

            bool isr = false;

            foreach (Metadata.CustomAttributeRow car in mtc.meth.CustomAttrs)
            {
                AssemblerState new_state = GetNewAssemblerState();
                InterpretMethodCustomAttribute(mtc, car, new_state);
                if (((x86_64_AssemblerState)new_state).isr)
                {
                    isr = true;
                    break;
                }
            }

            if (isr)
            {
                ret |= libsupcs.TysosMethod.TF_X86_ISR;

                if (mtc.msig.Method.Params.Count != 0)
                    ret |= libsupcs.TysosMethod.TF_X86_ISREC;
            }

            return ret;
        }

        //tydisasm.x86_64.x86_64_disasm disasm = null;

        internal override tydb.Location GetDebugLocation(hardware_location loc)
        {
            //if (disasm == null)
            //    disasm = new tydisasm.x86_64.x86_64_disasm();

            if (loc == null)
                throw new Exception("loc is null");
            if (loc is x86_64_gpr)
            {
                x86_64_gpr gpr = loc as x86_64_gpr;

                tydb.Location ret = new tydb.Location { Type = tydb.Location.LocationType.Register };
                ret.RegisterName = gpr.ToString();
                return ret;
            }
            else if (loc is hardware_contentsof)
            {
                hardware_contentsof co = loc as hardware_contentsof;

                tydb.Location ret = new tydb.Location { Type = tydb.Location.LocationType.ContentsOfLocation };
                ret.ContentsOf = GetDebugLocation(co.base_loc);
                ret.Offset = co.const_offset;
                return ret;
            }
            else if (loc is x86_64_xmm)
            {
                x86_64_xmm xmm = loc as x86_64_xmm;

                tydb.Location ret = new tydb.Location { Type = tydb.Location.LocationType.Register };
                ret.RegisterName = xmm.ToString();
                return ret;
            }
            else if (loc is hardware_stackloc)
            {
                hardware_stackloc sl = loc as hardware_stackloc;

                tydb.Location ret = new tydb.Location { Type = tydb.Location.LocationType.ContentsOfLocation };
                ret.ContentsOf = new tydb.Location { Type = tydb.Location.LocationType.Register, RegisterName = "rbp" };
                ret.Offset = (int)((uint)(-sl.loc - sl.size) & 0xfffffff8);
                return ret;
            }
            throw new NotSupportedException();
        }

        public override MiniAssembler GetMiniAssembler()
        {
            return new x86_64.x86_64_MiniAssembler();
        }

        internal override hardware_location GetMethinfoPointerLocation()
        {
            return new hardware_stackloc { size = 0 };      // assign the next stack location for the methinfo pointer
        }

        public override util.Set<hardware_location> MachineRegisters
        {
            get
            {
                util.Set<hardware_location> ret = new util.Set<hardware_location>();
                ret.Add(Rax); ret.Add(Rcx); ret.Add(Rdx); ret.Add(Rbx); ret.Add(Rdi); ret.Add(Rsi);
                if (GetBitness() == Bitness.Bits64)
                {
                    ret.Add(R8); ret.Add(R9); ret.Add(R10); ret.Add(R11); ret.Add(R12); ret.Add(R13); ret.Add(R14); ret.Add(R15);
                }
                ret.Add(Xmm0); ret.Add(Xmm1); ret.Add(Xmm2); ret.Add(Xmm3); ret.Add(Xmm4); ret.Add(Xmm5); ret.Add(Xmm6); ret.Add(Xmm7);
                if (GetBitness() == Bitness.Bits64)
                {
                    ret.Add(Xmm8); ret.Add(Xmm9); ret.Add(Xmm10); ret.Add(Xmm11); ret.Add(Xmm12); ret.Add(Xmm13); ret.Add(Xmm14); ret.Add(Xmm15);
                }

                return ret;
            }
        }

        public override util.Set<hardware_location> MachineRegistersForDataType(CliType dt, bool needs_memloc, Assembler.MachineRegisterList mrl)
        {
            if (needs_memloc)
            {
                util.Set<hardware_location> memlocs = new util.Set<hardware_location>();
                int size = GetSizeOf(new Signature.Param(dt));
                memlocs.Add(new hardware_stackloc { loc = mrl.next_stackloc++, size = size, container = mrl });
                mrl.StackLocSizes[mrl.next_stackloc - 1] = size;
                return memlocs;
            }

            if (dt == CliType.native_int)
            {
                if (GetBitness() == Bitness.Bits32)
                    dt = CliType.int32;
                else
                    dt = CliType.int64;
            }

            util.Set<hardware_location> ret = new util.Set<hardware_location>();

            switch (dt)
            {
                case CliType.F32:
                case CliType.F64:
                    ret.Add(Xmm0);
                    ret.Add(Xmm1);
                    ret.Add(Xmm2);
                    ret.Add(Xmm3);
                    ret.Add(Xmm4);
                    ret.Add(Xmm5);
                    ret.Add(Xmm6);
                    ret.Add(Xmm7);
                    ret.Add(Xmm8);
                    ret.Add(Xmm9);
                    ret.Add(Xmm10);
                    ret.Add(Xmm11);
                    ret.Add(Xmm12);
                    ret.Add(Xmm13);
                    ret.Add(Xmm14);
                    ret.Add(Xmm15);
                    break;

                case CliType.int32:
                case CliType.int64:
                case CliType.O:
                case CliType.reference:
                    ret.Add(Rax);
                    ret.Add(Rbx);
                    ret.Add(Rcx);
                    ret.Add(Rdx);
                    ret.Add(Rdi);
                    ret.Add(Rsi);
                    ret.Add(R8);
                    ret.Add(R9);
                    ret.Add(R10);
                    ret.Add(R11);
                    ret.Add(R12);
                    ret.Add(R13);
                    ret.Add(R14);
                    ret.Add(R15);
                    break;

                default:
                    throw new NotImplementedException();
            }

            return ret;
        }

        internal override List<hardware_location> GetLocalVarsLocations(List<Signature.Param> lvs_p, Assembler.MethodAttributes attrs)
        {
            /* Local vars at rbp - offset */
            List<hardware_location> ret = new List<hardware_location>();
            int cur_offset = 0;

            foreach (Signature.Param p in lvs_p)
            {
                int size = GetSizeOf(p);
                int memsize = util.align(size, ia == IA.i586 ? 4 : 8);
                cur_offset -= memsize;
                ret.Add(new hardware_contentsof { base_loc = Rbp, const_offset = cur_offset, size = size });
            }

            attrs.lv_stack_space = -cur_offset;

            return ret;
        }

        internal override Dictionary<hardware_location, hardware_location> AllocateStackLocations(MethodAttributes attrs)
        {
            Dictionary<hardware_location, hardware_location> ret = new Dictionary<hardware_location, hardware_location>();

            int cur_offset = -attrs.lv_stack_space;

            foreach (int sl in attrs.LVStackLocs.StackLocSizes.Keys)
            {
                int size = attrs.LVStackLocs.StackLocSizes[sl];
                int memsize = util.align(size, ia == IA.i586 ? 4 : 8);
                cur_offset -= memsize;
                hardware_stackloc hsl = new hardware_stackloc { loc = sl, size = size, container = attrs.LVStackLocs };
                ret[hsl] = new hardware_contentsof { base_loc = Rbp, const_offset = cur_offset, size = size };
            }

            attrs.lv_stack_space = -cur_offset;

            foreach (int sl in attrs.SpillStackLocs.StackLocSizes.Keys)
            {
                int size = attrs.SpillStackLocs.StackLocSizes[sl];
                int memsize = util.align(size, ia == IA.i586 ? 4 : 8);
                cur_offset -= memsize;
                hardware_stackloc hsl = new hardware_stackloc { loc = sl, size = size, container = attrs.SpillStackLocs };
                ret[hsl] = new hardware_contentsof { base_loc = Rbp, const_offset = cur_offset, size = size };
            }

            attrs.spill_stack_space = (-cur_offset) - attrs.lv_stack_space;

            return ret;
        }

        internal CliType GetCliType(System.Type t)
        {
            if (t == typeof(int))
                return CliType.int32;
            if (t == typeof(uint))
                return CliType.int32;
            if (t == typeof(long))
                return CliType.int64;
            if (t == typeof(ulong))
                return CliType.int64;
            throw new NotImplementedException();
        }

        internal void ChooseInstruction(x86_64.x86_64_asm.opcode op, List<tybel.Node> ret, params vara[] vars)
        {
            int nv = 0;
            bool s = true;

            for (int i = 0; i < vars.Length; i++)
            {
                if (vars[i].MachineRegVal is libasm.const_location)
                    vars[i] = vara.Const(((libasm.const_location)vars[i].MachineRegVal).c, GetCliType(((libasm.const_location)vars[i].MachineRegVal).c.GetType()));
                if (vars[i].MachineRegVal is libasm.hardware_addressoflabel)
                {
                    libasm.hardware_addressoflabel aol = vars[i].MachineRegVal as libasm.hardware_addressoflabel;
                    vars[i] = vara.Label(aol.label, aol.const_offset, aol.is_object);
                }
            }

            ChooseInstruction(op, ret, null, ref nv, ref s, vars);
            if (s == false)
            {
                StringBuilder sb = new StringBuilder("Unable to encode ");
                sb.Append(op.ToString());
                for (int i = 0; i < vars.Length; i++)
                {
                    if (i == 0)
                        sb.Append(" ");
                    else
                        sb.Append(", ");
                    sb.Append(vars[i].ToString());
                }
                throw new Exception(sb.ToString());
            }
        }

        internal static void EncLea(x86_64_Assembler ass, libtysila.frontend.cil.Encoder.EncoderState state, libasm.hardware_location dest, libasm.hardware_location src, List<tybel.Node> ret)
        {
            libasm.hardware_location act_dest = dest;
            if (!(dest is x86_64_gpr))
                dest = Rax;

            src = ResolveStackLoc(ass, state, src);

            switch (ass.ia)
            {
                case IA.i586:
                    ass.ChooseInstruction(x86_64.x86_64_asm.opcode.LEAL, ret, vara.MachineReg(dest), vara.MachineReg(src));
                    break;
                case IA.x86_64:
                    ass.ChooseInstruction(x86_64.x86_64_asm.opcode.LEAQ, ret, vara.MachineReg(dest), vara.MachineReg(src));
                    break;
            }

            if (!act_dest.Equals(dest))
                EncMov(ass, state, act_dest, dest, ret);
        }

        internal static hardware_location ResolveStackLoc(x86_64_Assembler ass, frontend.cil.Encoder.EncoderState state, hardware_location loc)
        {
            if (!(loc is hardware_stackloc))
                return loc;
            hardware_stackloc sl = loc as hardware_stackloc;

            /* local args are relative to rbp - offset - len - 8 (size for temp 3 on stack)
             * local vars are relative to rbp - la_size - offset - len - 8
             * vars are relative to rbp - la_size - lv_size - offset - len - 8
             */

            switch (sl.stack_type)
            {
                case hardware_stackloc.StackType.Arg:
                    return new hardware_contentsof { base_loc = Rbp, const_offset = -(sl.loc + sl.size + 8), size = sl.size };
                case hardware_stackloc.StackType.LocalVar:
                    return new hardware_contentsof { base_loc = Rbp, const_offset = -(state.la_stack.ByteSize + sl.loc + sl.size + 8), size = sl.size };
                case hardware_stackloc.StackType.Var:
                    return new hardware_contentsof { base_loc = Rbp, const_offset = -(state.la_stack.ByteSize + state.lv_stack.ByteSize + sl.loc + sl.size + 8), size = sl.size };
            }
            throw new NotImplementedException();
        }

        internal static libasm.multiple_hardware_location mhl_split(x86_64_Assembler ass, libtysila.frontend.cil.Encoder.EncoderState state, 
            libasm.hardware_location src, int n, int stack_split_size)
        {
            if (src is libasm.multiple_hardware_location)
                return src as libasm.multiple_hardware_location;
            else if (src is libasm.hardware_contentsof)
            {
                libasm.hardware_location[] locs = new hardware_location[n];
                libasm.hardware_contentsof hco = src as libasm.hardware_contentsof;
                for (int i = 0; i < n; i++)
                    locs[i] = new hardware_contentsof { base_loc = hco.base_loc, const_offset = hco.const_offset + i * stack_split_size, size = stack_split_size };

                return new multiple_hardware_location { hlocs = locs };
            }
            else if (src is libasm.hardware_stackloc)
                return mhl_split(ass, state, x86_64_Assembler.ResolveStackLoc(ass, state, src), n, stack_split_size);
            else
                throw new Exception("mhl_split with invalid src type: " + src.ToString());
        }

        internal static void EncMov(x86_64_Assembler ass, libtysila.frontend.cil.Encoder.EncoderState state, libasm.hardware_location dest, libasm.hardware_location src, List<tybel.Node> ret)
        { EncMov(ass, state, dest, src, CliType.native_int, ret); }

        internal static void EncMov(x86_64_Assembler ass, libtysila.frontend.cil.Encoder.EncoderState state, libasm.hardware_location dest, libasm.hardware_location src, CliType dt, List<tybel.Node> ret)
        {
            if ((dest is libasm.multiple_hardware_location) || (src is libasm.multiple_hardware_location) || (dt == CliType.int64 && ass.ia == IA.i586))
            {
                int locs = 0;
                if (dest is libasm.multiple_hardware_location)
                    locs = ((libasm.multiple_hardware_location)dest).hlocs.Length;
                if (src is libasm.multiple_hardware_location)
                {
                    int src_locs = ((libasm.multiple_hardware_location)src).hlocs.Length;
                    if (locs != 0 && src_locs != locs)
                        throw new Exception("cannot move between " + src.ToString() + " and " + dest.ToString());
                    locs = src_locs;
                }
                if (locs == 0)
                    locs = 2;
                libasm.multiple_hardware_location mhl_dest = mhl_split(ass, state, dest, locs, ass.ia == IA.i586 ? 4 : 8);
                libasm.multiple_hardware_location mhl_src = mhl_split(ass, state, src, locs, ass.ia == IA.i586 ? 4 : 8);

                for (int i = 0; i < locs; i++)
                {
                    EncMov(ass, state, mhl_dest.hlocs[i], mhl_src.hlocs[i], ret);
                }
                return;
            }


            libasm.hardware_location act_dest = dest;
            switch (dt)
            {
                case CliType.int32:
                case CliType.int64:
                case CliType.native_int:
                case CliType.O:
                case CliType.reference:
                case CliType.virtftnptr:
                    if (!(dest is libasm.multiple_hardware_location) && !(src is libasm.multiple_hardware_location))
                    {
                        if (!(dest is x86_64_gpr) && !(src is x86_64_gpr))
                            dest = Rax;
                    }
                    break;
                case CliType.F32:
                case CliType.F64:
                    if (!(dest is x86_64_xmm) && !(src is x86_64_xmm))
                        dest = Xmm0;
                    break;
            }

            src = ResolveStackLoc(ass, state, src);
            dest = ResolveStackLoc(ass, state, dest);
            act_dest = ResolveStackLoc(ass, state, act_dest);

            dt = ass.ResolveNativeInt(dt);
            x86_64.x86_64_asm.opcode op = x86_64.x86_64_asm.opcode.MOVL;
            switch (dt)
            {
                case CliType.int32:
                    break;
                case CliType.int64:
                    if (ass.ia == IA.x86_64)
                        op = x86_64.x86_64_asm.opcode.MOVQ;
                    else
                        throw new NotImplementedException();
                    break;
                case CliType.F64:
                    op = x86_64.x86_64_asm.opcode.MOVSD;
                    break;
                case CliType.F32:
                    op = x86_64.x86_64_asm.opcode.MOVSS;
                    break;
                default:
                    throw new NotImplementedException();
            }

            ass.ChooseInstruction(op, ret, vara.MachineReg(dest), vara.MachineReg(src));

            if (!act_dest.Equals(dest))
                EncMov(ass, state, act_dest, dest, dt, ret);
        }

        bool is_unencodable_r8_rm8(libasm.hardware_location l)
        {
            /* Return true if the argument cannot be encoded as an 8 bit register in ia32 mode
             * In partucular, SIL and DIL are unencodable here */
            if (!(l is x86_64_gpr))
                return false;
            if (ia == IA.x86_64)
                return false;
            x86_64_gpr gpr = l as x86_64_gpr;
            if (gpr.reg == x86_64_gpr.RegId.rsi || gpr.reg == x86_64_gpr.RegId.rdi)
                return true;
            return false;
        }

        internal override void MemSet(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location c, hardware_location n, List<tybel.Node> ret)
        {
            int max_move = ia == IA.i586 ? 4 : 8;
            int max_inline = max_move * 4;

            if (n is const_location && c is const_location && dest is x86_64_gpr && !is_unencodable_r8_rm8(dest) &&
                (int)((const_location)n).c <= max_inline)
            {
                int to_move = (int)((const_location)n).c;
                int _c = (int)((const_location)c).c & 0xff;

                uint v_4 = (uint)_c + (((uint)_c) << 8) + (((uint)_c) << 16) + (((uint)_c) << 24);
                ushort v_2 = (ushort)((ushort)_c + (((ushort)_c) << 8));
                byte v_1 = (byte)_c;

                int cur_offset = 0;
                if (ia == IA.x86_64)
                {
                    ulong v_8 = (ulong)_c + (((ulong)_c) << 8) + (((ulong)_c) << 16) + (((ulong)_c) << 24) +
                        (((ulong)_c) << 32) + (((ulong)_c) << 40) + (((ulong)_c) << 48) + (((ulong)_c) << 56);
                    while (to_move >= 8)
                    {
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, vara.MachineReg(new hardware_contentsof { base_loc = dest, size = 8, const_offset = cur_offset }), vara.MachineReg(new const_location { c = v_8 }));
                        to_move -= 8;
                        cur_offset += 8;
                    }
                }
                while (to_move >= 4)
                {
                    ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, vara.MachineReg(new hardware_contentsof { base_loc = dest, size = 4, const_offset = cur_offset }), vara.MachineReg(new const_location { c = v_4 }));
                    to_move -= 4;
                    cur_offset += 4;
                }
                while (to_move >= 2)
                {
                    ChooseInstruction(x86_64.x86_64_asm.opcode.MOVW, ret, vara.MachineReg(new hardware_contentsof { base_loc = dest, size = 2, const_offset = cur_offset }), vara.MachineReg(new const_location { c = v_2 }));
                    to_move -= 2;
                    cur_offset += 2;
                }
                while (to_move >= 1)
                {
                    ChooseInstruction(x86_64.x86_64_asm.opcode.MOVB, ret, vara.MachineReg(new hardware_contentsof { base_loc = dest, size = 1, const_offset = cur_offset }), vara.MachineReg(new const_location { c = v_1 }));
                    to_move -= 1;
                    cur_offset += 1;
                }

                return;
            }

            Call(state, regs_in_use, new hardware_addressoflabel("memset", false), null, new hardware_location[] { dest, c, n }, callconv_memset, ret);
        }

        internal override void RunOnce(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location flags, List<tybel.Node> ret)
        {
            int blk_id = state.next_blk++;
            string blk_l = "L" + blk_id.ToString();

            ChooseInstruction(x86_64.x86_64_asm.opcode.TESTL, ret, flags, vara.Const(1));
            ChooseInstruction(x86_64.x86_64_asm.opcode.JZ, ret, vara.Label(blk_l, false));
            ChooseInstruction(x86_64.x86_64_asm.opcode.RETN, ret);
            ret.Add(new tybel.LabelNode(blk_l, true));
            ChooseInstruction(x86_64.x86_64_asm.opcode.ORL, ret, flags, vara.Const(1));
        }

        internal override void WMemSet(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location c, hardware_location n, List<tybel.Node> ret)
        {
            Call(state, regs_in_use, new hardware_addressoflabel("wmemset", false), null, new hardware_location[] { dest, c, n }, callconv_wmemset, ret);
        }

        internal override void MemCpy(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location src, hardware_location n, List<tybel.Node> ret)
        {
            /* We can inline this if:
             * n is known at compile time
             * one of src/dest is a gpr (we need to use one of rax/rdx as an intermediate)
             * n is sufficiently small (? <= 4x word size)
             */
            int ptr_size = ia == IA.i586 ? 4 : 8;
            if ((n is libasm.const_location) && ((src is x86_64_gpr) || (dest is x86_64_gpr)) &&
                (int)((libasm.const_location)n).c <= (4 * ptr_size))
            {
                int size = (int)((libasm.const_location)n).c;
                if (!(src is x86_64_gpr))
                {
                    Assign(state, regs_in_use, Rax, src, CliType.native_int, ret);
                    src = Rax;
                }
                else if (!(dest is x86_64_gpr))
                {
                    Assign(state, regs_in_use, Rax, dest, CliType.native_int, ret);
                    dest = Rax;
                }
                for (int cur_ptr = 0; cur_ptr < size; cur_ptr += ptr_size)
                {
                    Assign(state, regs_in_use, Rdx, new libasm.hardware_contentsof { base_loc = src, const_offset = cur_ptr, size = ptr_size },
                        CliType.native_int, ret);
                    Assign(state, regs_in_use, new libasm.hardware_contentsof { base_loc = dest, const_offset = cur_ptr, size = ptr_size }, Rdx,
                        CliType.native_int, ret);
                }
            }
            else
            {
                Call(state, regs_in_use, new libasm.hardware_addressoflabel("memcpy", false), null,
                    new hardware_location[] { dest, src, n }, callconv_memcpy, ret);
            }
        }

        internal override void Call(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location retval, hardware_location[] p, CallConv cc, List<tybel.Node> ret)
        {
            bool returns = true;
            if (cc.MethodSig != null)
                returns = cc.MethodSig.Method.Returns;

            // Save used locations
            List<libasm.hardware_location> save_regs = null;
            if (returns)
            {
                save_regs = new List<hardware_location>(util.Intersect<libasm.hardware_location>(regs_in_use.UsedLocations, cc.CallerPreservesLocations));
                for (int i = 0; i < save_regs.Count; i++)
                    Save(state, regs_in_use, save_regs[i], ret);
            }

            // Push arguments
            if (cc.StackSpaceUsed != 0)
                ChooseInstruction(ia == IA.i586 ? x86_64.x86_64_asm.opcode.SUBL : x86_64.x86_64_asm.opcode.SUBQ, ret, vara.MachineReg(Rsp), vara.Const(cc.StackSpaceUsed));
            if (p.Length != cc.Arguments.Count)
                throw new Exception("Supplied arguments do not match those in the calling convention");

            // Is fptr also a argument location?  If so, use rbx instead
            for (int i = 0; i < p.Length; i++)
            {
                if (dest.Equals(cc.Arguments[i].ValueLocation))
                {
                    Assign(state, regs_in_use, Rbx, dest, CliType.native_int, ret);
                    Stack temp_stack = regs_in_use.Clone();
                    temp_stack.MarkUsed(Rbx);
                    dest = Rbx;
                    regs_in_use = temp_stack;
                    state.used_locs.Add(Rbx);
                    break;
                }
            }

            if (cc.HiddenRetValArgument != null)
                LoadAddress(state, regs_in_use, cc.HiddenRetValArgument, retval, ret);
            for (int i = 0; i < p.Length; i++)
                Assign(state, regs_in_use, cc.Arguments[i].ValueLocation, p[i], cc.Arguments[i].Type.CliType(this), ret);

            // Execute call
            ChooseInstruction(x86_64.x86_64_asm.opcode.CALL, ret, vara.MachineReg(dest));

            if (returns)
            {
                // Store return value
                if (retval != null && cc.HiddenRetValArgument == null)
                    Assign(state, regs_in_use, retval, cc.ReturnValue, cc.MethodSig.Method.RetType.CliType(this), ret);

                // Restore stack
                if (cc.StackSpaceUsed != 0)
                    ChooseInstruction(ia == IA.i586 ? x86_64.x86_64_asm.opcode.ADDL : x86_64.x86_64_asm.opcode.ADDQ, ret, vara.MachineReg(Rsp), vara.Const(cc.StackSpaceUsed));

                // Restore saved args
                for (int i = save_regs.Count - 1; i >= 0; i--)
                {
                    if (save_regs[i].Equals(retval))
                    {
                        /* don't trash the return value */
                        ChooseInstruction(ia == IA.i586 ? x86_64.x86_64_asm.opcode.ADDL : x86_64.x86_64_asm.opcode.ADDQ, ret, vara.MachineReg(Rsp), vara.Const(GetSizeOfIntPtr()));
                    }
                    else
                        Restore(state, regs_in_use, save_regs[i], ret);
                }
            }
        }

        internal override void Assign(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location src, CliType dt, List<tybel.Node> ret)
        {
            if (dest.Equals(src))
                return;
            /* Get the size of a value-type argument */
            if (dt == CliType.vt)
            {
                dest = ResolveStackLoc(this, state, dest);
                src = ResolveStackLoc(this, state, src);
                int d_size = 0, s_size = 0;
                if (dest is hardware_contentsof)
                    d_size = ((hardware_contentsof)dest).size;
                if (src is hardware_contentsof)
                    s_size = ((hardware_contentsof)src).size;

                int size = d_size;
                if (s_size > d_size)
                    size = s_size;

                if (size > GetSizeOfPointer())
                {
                    int mov_size;
                    CliType mov_dt;
                    if (ia == IA.x86_64 && size % 8 == 0)
                    {
                        mov_size = 8;
                        mov_dt = CliType.int64;
                    }
                    else
                    {
                        mov_size = 4;
                        mov_dt = CliType.int32;
                    }

                    if ((dest is hardware_contentsof) && (src is hardware_contentsof))
                    {
                        hardware_contentsof d_hco = dest as hardware_contentsof;
                        hardware_contentsof s_hco = src as hardware_contentsof;

                        for (int i = 0; i < size; i += mov_size)
                        {
                            hardware_contentsof d_hco2 = new hardware_contentsof { base_loc = d_hco.base_loc, const_offset = d_hco.const_offset + i, size = mov_size };
                            hardware_contentsof s_hco2 = new hardware_contentsof { base_loc = s_hco.base_loc, const_offset = s_hco.const_offset + i, size = mov_size };
                            EncMov(this, state, d_hco2, s_hco2, mov_dt, ret);
                        }
                        return;
                    }
                    else
                        throw new NotSupportedException();
                }
                else
                {
                    if (size > 4)
                        dt = CliType.int64;
                    else
                        dt = CliType.int32;
                }
            }

            EncMov(this, state, dest, src, dt, ret);
        }

        internal override void Add(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location a, hardware_location b, CliType dt, List<tybel.Node> ret)
        {
            libasm.hardware_location act_dest = dest;
            if (!(dest is x86_64_gpr))
                act_dest = Rax;

            dest = ResolveStackLoc(this, state, dest);
            a = ResolveStackLoc(this, state, a);
            b = ResolveStackLoc(this, state, b);
            act_dest = ResolveStackLoc(this, state, act_dest);

            if (!act_dest.Equals(a))
            {
                EncMov(this, state, act_dest, a, ret);
                a = act_dest;
            }

            if (!(b is x86_64_gpr))
            {
                EncMov(this, state, Rdx, b, ret);
                b = Rdx;
            }

            x86_64.x86_64_asm.opcode op = x86_64.x86_64_asm.opcode.ADDL;
            dt = ResolveNativeInt(dt);
            switch (dt)
            {
                case CliType.int32:
                    break;
                case CliType.int64:
                    if (ia == IA.x86_64)
                        op = x86_64.x86_64_asm.opcode.ADDQ;
                    else
                        throw new NotImplementedException();
                    break;
                default:
                    throw new NotImplementedException();
            }
            ChooseInstruction(op, ret, vara.MachineReg(act_dest), vara.MachineReg(b));

            if (!act_dest.Equals(dest))
                EncMov(this, state, dest, act_dest, ret);
        }

        public override hardware_location GetTemporary(libtysila.frontend.cil.Encoder.EncoderState state, CliType ct)
        {
            switch (ct)
            {
                case CliType.int32:
                case CliType.int64:
                case CliType.native_int:
                    state.used_locs.Add(Rcx);
                    return Rcx;

                case CliType.F32:
                case CliType.F64:
                    state.used_locs.Add(Xmm2);
                    return Xmm2;

                default:
                    throw new NotSupportedException();
            }
        }

        public override hardware_location GetTemporary2(libtysila.frontend.cil.Encoder.EncoderState state, CliType ct)
        {
            switch (ct)
            {
                case CliType.int32:
                case CliType.int64:
                case CliType.native_int:
                    state.used_locs.Add(Rbx);
                    return Rbx;

                case CliType.F32:
                case CliType.F64:
                    state.used_locs.Add(Xmm3);
                    return Xmm3;

                default:
                    throw new NotSupportedException();
            }
        }

        public override hardware_location GetTemporary3(frontend.cil.Encoder.EncoderState state, CliType ct)
        {
            /* Return RBP - pointer size */
            if (ct == CliType.vt)
                throw new Exception("x86_64: cannot allocate temporary 3 as value type");

            return new libasm.hardware_contentsof { base_loc = Rbp, const_offset = -8, size = GetSizeOf(new Signature.Param(ct)) };
        }

        internal override void Peek(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location src_addr, int size, List<tybel.Node> ret)
        {
            src_addr = ResolveStackLoc(this, state, src_addr);
            if (!(src_addr is x86_64_gpr))
            {
                EncMov(this, state, Rax, src_addr, ret);
                src_addr = Rax;
            }

            dest = ResolveStackLoc(this, state, dest);
            libasm.hardware_location act_dest = dest;

            if (dest is x86_64_xmm)
            {
                switch (size)
                {
                    case 4:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSS, ret, dest, new libasm.hardware_contentsof { base_loc = src_addr, size = 4 });
                        break;

                    case 8:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSD, ret, dest, new libasm.hardware_contentsof { base_loc = src_addr, size = 8 });
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                if (size > 8)
                {
                    LoadAddress(state, regs_in_use, Rdx, dest, ret);
                    MemCpy(state, regs_in_use, Rdx, src_addr, size, ret);
                    return;
                }

                if (!(dest is x86_64_gpr))
                    act_dest = Rdx;

                switch (size)
                {
                    case 1:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVB, ret, vara.MachineReg(act_dest), vara.MachineReg(new libasm.hardware_contentsof { base_loc = src_addr, size = size }));
                        ChooseInstruction(x86_64.x86_64_asm.opcode.ANDL, ret, vara.MachineReg(act_dest), vara.Const(0xffUL, CliType.int32));
                        break;

                    case 2:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVW, ret, vara.MachineReg(act_dest), vara.MachineReg(new libasm.hardware_contentsof { base_loc = src_addr, size = size }));
                        ChooseInstruction(x86_64.x86_64_asm.opcode.ANDL, ret, vara.MachineReg(act_dest), vara.Const(0xffffUL, CliType.int32));
                        break;

                    case 4:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, vara.MachineReg(act_dest), vara.MachineReg(new libasm.hardware_contentsof { base_loc = src_addr, size = size }));
                        break;

                    case 8:
                        if (ia == IA.i586)
                        {
                            libasm.multiple_hardware_location mhl_dest = mhl_split(this, state, dest, 2, 4);

                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, Rdx, new libasm.hardware_contentsof { base_loc = src_addr, size = 4, const_offset = 0 });
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, mhl_dest[0], Rdx);
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, Rdx, new libasm.hardware_contentsof { base_loc = src_addr, size = 4, const_offset = 4 });
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, mhl_dest[1], Rdx);
                            return;
                        }
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, vara.MachineReg(act_dest), vara.MachineReg(new libasm.hardware_contentsof { base_loc = src_addr, size = size }));
                        break;

                    default:
                        throw new NotImplementedException();
                }

                if (!dest.Equals(act_dest))
                    EncMov(this, state, dest, act_dest, ret);
            }
        }

        internal override void LocAlloc(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest_loc, int size, List<tybel.Node> ret)
        {
            dest_loc = ResolveStackLoc(this, state, dest_loc);

            ChooseInstruction(ia == IA.i586 ? x86_64.x86_64_asm.opcode.SUBL : x86_64.x86_64_asm.opcode.SUBQ, ret, vara.MachineReg(Rsp), vara.Const(size));
            Assign(state, regs_in_use, dest_loc, Rsp, CliType.native_int, ret);
        }

        internal override void LocDeAlloc(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, int size, List<tybel.Node> ret)
        {
            ChooseInstruction(ia == IA.i586 ? x86_64.x86_64_asm.opcode.ADDL : x86_64.x86_64_asm.opcode.ADDQ, ret, vara.MachineReg(Rsp), vara.Const(size));
        }

        internal override void Poke(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest_addr, hardware_location src, int size, List<tybel.Node> ret)
        {
            dest_addr = ResolveStackLoc(this, state, dest_addr);

            if(!(dest_addr is x86_64_gpr))
            {
                EncMov(this, state, Rdx, dest_addr, ret);
                dest_addr = Rdx;
            }

            src = ResolveStackLoc(this, state, src);
            libasm.hardware_location act_src = src;
            if (src is x86_64_xmm)
            {
                switch (size)
                {
                    case 4:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSS, ret, new libasm.hardware_contentsof { base_loc = dest_addr, size = 4 }, src);
                        break;

                    case 8:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSD, ret, new libasm.hardware_contentsof { base_loc = dest_addr, size = 8 }, src);
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                if (size > 8)
                {
                    LoadAddress(state, regs_in_use, Rax, src, ret);
                    MemCpy(state, regs_in_use, dest_addr, Rax, size, ret);
                    return;
                }

                if (!(src is x86_64_gpr))
                {
                    EncMov(this, state, Rax, src, ret);
                    act_src = Rax;
                }

                switch (size)
                {
                    case 1:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVB, ret, vara.MachineReg(new libasm.hardware_contentsof { base_loc = dest_addr, size = size }), vara.MachineReg(act_src));
                        break;

                    case 2:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVW, ret, vara.MachineReg(new libasm.hardware_contentsof { base_loc = dest_addr, size = size }), vara.MachineReg(act_src));
                        break;

                    case 4:
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, vara.MachineReg(new libasm.hardware_contentsof { base_loc = dest_addr, size = size }), vara.MachineReg(act_src));
                        break;

                    case 8:
                        if (ia == IA.i586)
                        {
                            libasm.multiple_hardware_location mhl_src = mhl_split(this, state, src, 2, 4);

                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, Rdx, mhl_src[0]);
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, new libasm.hardware_contentsof { base_loc = dest_addr, size = 4, const_offset = 0 }, Rdx);
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, Rdx, mhl_src[1]);
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, new libasm.hardware_contentsof { base_loc = dest_addr, size = 4, const_offset = 4 }, Rdx);
                            return;
                        }
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, vara.MachineReg(new libasm.hardware_contentsof { base_loc = dest_addr, size = size }), vara.MachineReg(act_src));
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal override void LoadAddress(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location obj, List<tybel.Node> ret)
        {
            EncLea(this, state, dest, obj, ret);
        }

        internal override void Enter(frontend.cil.Encoder.EncoderState state, MethodAttributes attrs, List<tybel.Node> ret)
        {
            bool is_isr = false;
            if (attrs.attrs.ContainsKey("libsupcs.ISR"))
                is_isr = true;

            /* if (is_isr)
                throw new NotImplementedException();
            else */
            {
                /* Set up frame pointer */
                ChooseInstruction(x86_64.x86_64_asm.opcode.PUSH, ret, vara.MachineReg(Rbp));
                ChooseInstruction(ia == IA.i586 ? x86_64.x86_64_asm.opcode.MOVL : x86_64.x86_64_asm.opcode.MOVQ, ret,
                    vara.MachineReg(Rbp), vara.MachineReg(Rsp));

                /* Reserve stack space for args, vars and temporaries */
                int stack_reserve = state.la_stack.ByteSize + state.lv_stack.ByteSize + state.largest_var_stack + 8;
                if (stack_reserve > 0)
                {
                    ChooseInstruction(ia == IA.i586 ? x86_64.x86_64_asm.opcode.SUBL : x86_64.x86_64_asm.opcode.SUBQ,
                        ret, vara.MachineReg(Rsp), vara.Const(stack_reserve));
                }

                /* Copy arguments to stack arg space */
                for (int i = 0; i < state.cc.Arguments.Count; i++)
                {
                    Assign(state, null, state.la_stack.GetAddressOf(i, this), state.cc.Arguments[i].ValueLocation,
                        state.cc.Arguments[i].Type.CliType(this), ret);
                    //EncMov(this, state, state.la_stack.GetAddressOf(i, this), state.cc.Arguments[i].ValueLocation,
                    //    state.cc.Arguments[i].Type.CliType(this), ret);
                }
                if (state.cc.HiddenRetValArgument != null)
                    EncMov(this, state, state.la_stack.GetAddressOf(state.cc.Arguments.Count, this), state.cc.HiddenRetValArgument, ret);
            }
        }

        internal override void NumOp(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location a, hardware_location b, ThreeAddressCode.Op op, List<tybel.Node> ret)
        {
            dest = ResolveStackLoc(this, state, dest);
            a = ResolveStackLoc(this, state, a);
            if(b != null)
                b = ResolveStackLoc(this, state, b);

            op = ResolveNativeIntOp(op);

            x86_64.x86_64_asm.opcode opc1 = x86_64.x86_64_asm.opcode.NOP;
            x86_64.x86_64_asm.opcode opc2 = x86_64.x86_64_asm.opcode.NOP;
            x86_64.x86_64_asm.opcode mov = x86_64.x86_64_asm.opcode.MOVL;
            libasm.hardware_location idiv_ret = null;
            libasm.hardware_location temp = Rax;
            bool int64_backwards = false;
            bool b_in_cl_if_not_const = false;
            bool b_is_int32 = false;

            if (op.Type == CliType.int64 && ia == IA.x86_64)
                mov = x86_64.x86_64_asm.opcode.MOVQ;
            else if (op.Type == CliType.F32)
            {
                mov = x86_64.x86_64_asm.opcode.MOVSS;
                temp = Xmm0;
            }
            else if (op.Type == CliType.F64)
            {
                mov = x86_64.x86_64_asm.opcode.MOVSD;
                temp = Xmm0;
            }

            switch (op.Operator)
            {
                case ThreeAddressCode.OpName.add:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.ADDL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                opc1 = x86_64.x86_64_asm.opcode.ADDL;
                                opc2 = x86_64.x86_64_asm.opcode.ADCL;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.ADDQ;
                            break;
                        case CliType.F32:
                            opc1 = x86_64.x86_64_asm.opcode.ADDSS;
                            break;
                        case CliType.F64:
                            opc1 = x86_64.x86_64_asm.opcode.ADDSD;
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.and:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.ANDL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                opc1 = x86_64.x86_64_asm.opcode.ANDL;
                                opc2 = x86_64.x86_64_asm.opcode.ANDL;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.ANDQ;
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.div:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.XORL, ret, vara.MachineReg(Rdx), vara.MachineReg(Rdx));
                            opc1 = x86_64.x86_64_asm.opcode.IDIVL;
                            idiv_ret = Rax;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                Call(state, regs_in_use, new libasm.hardware_addressoflabel("__divdi3", false), dest,
                                    new hardware_location[] { a, b }, callconv_numop_q_qq, ret);
                                return;
                            }
                            else
                            {
                                ChooseInstruction(x86_64.x86_64_asm.opcode.XORQ, ret, vara.MachineReg(Rdx), vara.MachineReg(Rdx));
                                opc1 = x86_64.x86_64_asm.opcode.IDIVQ;
                                idiv_ret = Rax;
                            }                                
                            break;
                        case CliType.F32:
                            opc1 = x86_64.x86_64_asm.opcode.DIVSS;
                            break;
                        case CliType.F64:
                            opc1 = x86_64.x86_64_asm.opcode.DIVSD;
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.div_un:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.XORL, ret, vara.MachineReg(Rdx), vara.MachineReg(Rdx));
                            opc1 = x86_64.x86_64_asm.opcode.DIVL;
                            idiv_ret = Rax;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                Call(state, regs_in_use, new libasm.hardware_addressoflabel("__udivdi3", false), dest,
                                    new hardware_location[] { a, b }, callconv_numop_q_qq, ret);
                                return;
                            }
                            else
                            {
                                ChooseInstruction(x86_64.x86_64_asm.opcode.XORQ, ret, vara.MachineReg(Rdx), vara.MachineReg(Rdx));
                                opc1 = x86_64.x86_64_asm.opcode.DIVQ;
                                idiv_ret = Rax;
                            }
                            break;
                        case CliType.F32:
                        case CliType.F64:
                            throw new NotImplementedException();
                    }
                    break;

                case ThreeAddressCode.OpName.mul:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.IMULL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                Call(state, regs_in_use, new libasm.hardware_addressoflabel("__mulvdi3", false), dest,
                                    new hardware_location[] { a, b }, callconv_numop_q_qq, ret);
                                return;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.IMULQ;
                            break;
                        case CliType.F32:
                            opc1 = x86_64.x86_64_asm.opcode.MULSS;
                            break;
                        case CliType.F64:
                            opc1 = x86_64.x86_64_asm.opcode.MULSD;
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.mul_un:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.MULL;
                            idiv_ret = Rax;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                Call(state, regs_in_use, new hardware_addressoflabel("__muldi3", false), dest,
                                    new hardware_location[] { a, b }, callconv_numop_q_qq, ret);
                                return;
                            }
                            else
                            {
                                opc1 = x86_64.x86_64_asm.opcode.MULQ;
                                idiv_ret = Rax;
                            }
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.neg:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.NEGL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                Call(state, regs_in_use, new libasm.hardware_addressoflabel("__negdi3", false), dest,
                                    new hardware_location[] { a }, callconv_numop_q_q, ret);
                                return;
                            }
                            opc1 = x86_64.x86_64_asm.opcode.NEGQ;
                            break;
                        case CliType.F32:
                            Call(state, regs_in_use, new libasm.hardware_addressoflabel("__negsf2", false), dest,
                                new hardware_location[] { a }, callconv_numop_s_s, ret);
                            return;

                        case CliType.F64:
                            Call(state, regs_in_use, new libasm.hardware_addressoflabel("__negdf2", false), dest,
                                new hardware_location[] { a }, callconv_numop_d_d, ret);
                            return;
                    }
                    break;

                case ThreeAddressCode.OpName.not:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.NOTL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                opc1 = x86_64.x86_64_asm.opcode.NOTL;
                                opc2 = x86_64.x86_64_asm.opcode.NOTL;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.NOTQ;
                            break;
                        case CliType.F32:
                        case CliType.F64:
                            throw new NotImplementedException();
                    }
                    break;

                case ThreeAddressCode.OpName.or:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.ORL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                opc1 = x86_64.x86_64_asm.opcode.ORL;
                                opc2 = x86_64.x86_64_asm.opcode.ORL;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.ORQ;
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.rem:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.XORL, ret, vara.MachineReg(Rdx), vara.MachineReg(Rdx));
                            opc1 = x86_64.x86_64_asm.opcode.IDIVL;
                            idiv_ret = Rdx;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                Call(state, regs_in_use, new libasm.hardware_addressoflabel("__moddi3", false), dest,
                                    new hardware_location[] { a, b }, callconv_numop_q_qq, ret);
                                return;
                            }
                            else
                            {
                                ChooseInstruction(x86_64.x86_64_asm.opcode.XORQ, ret, vara.MachineReg(Rdx), vara.MachineReg(Rdx));
                                opc1 = x86_64.x86_64_asm.opcode.IDIVQ;
                                idiv_ret = Rdx;
                            }
                            break;
                        case CliType.F32:
                        case CliType.F64:
                            throw new NotSupportedException();
                    }
                    break;

                case ThreeAddressCode.OpName.rem_un:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.XORL, ret, vara.MachineReg(Rdx), vara.MachineReg(Rdx));
                            opc1 = x86_64.x86_64_asm.opcode.DIVL;
                            idiv_ret = Rdx;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                Call(state, regs_in_use, new libasm.hardware_addressoflabel("__umoddi3", false), dest,
                                    new hardware_location[] { a, b }, callconv_numop_q_qq, ret);
                                return;
                            }
                            else
                            {
                                ChooseInstruction(x86_64.x86_64_asm.opcode.XORQ, ret, vara.MachineReg(Rdx), vara.MachineReg(Rdx));
                                opc1 = x86_64.x86_64_asm.opcode.DIVQ;
                                idiv_ret = Rdx;
                            }
                            break;
                        case CliType.F32:
                        case CliType.F64:
                            throw new NotSupportedException();
                    }
                    break;

                case ThreeAddressCode.OpName.shl:
                    b_in_cl_if_not_const = true;
                    b_is_int32 = true;
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.SALL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                opc1 = x86_64.x86_64_asm.opcode.SALL;
                                opc2 = x86_64.x86_64_asm.opcode.RCLL;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.SALQ;
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.shr:
                    b_in_cl_if_not_const = true;
                    b_is_int32 = true;
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.SARL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                opc1 = x86_64.x86_64_asm.opcode.RCRL;
                                opc2 = x86_64.x86_64_asm.opcode.SARL;
                                int64_backwards = true;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.SARQ;
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.shr_un:
                    b_in_cl_if_not_const = true;
                    b_is_int32 = true;
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.SHRL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                opc1 = x86_64.x86_64_asm.opcode.RCRL;
                                opc2 = x86_64.x86_64_asm.opcode.SHRL;
                                int64_backwards = true;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.SHRQ;
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.sub:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.SUBL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                opc1 = x86_64.x86_64_asm.opcode.SUBL;
                                opc2 = x86_64.x86_64_asm.opcode.SBBL;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.SUBQ;
                            break;
                        case CliType.F32:
                            opc1 = x86_64.x86_64_asm.opcode.SUBSS;
                            break;
                        case CliType.F64:
                            opc1 = x86_64.x86_64_asm.opcode.SUBSD;
                            break;
                    }
                    break;

                case ThreeAddressCode.OpName.xor:
                    switch (op.Type)
                    {
                        case CliType.int32:
                            opc1 = x86_64.x86_64_asm.opcode.XORL;
                            break;
                        case CliType.int64:
                            if (ia == IA.i586)
                            {
                                opc1 = x86_64.x86_64_asm.opcode.XORL;
                                opc2 = x86_64.x86_64_asm.opcode.XORL;
                            }
                            else
                                opc1 = x86_64.x86_64_asm.opcode.XORQ;
                            break;
                    }
                    break;
                    

                default:
                    throw new NotImplementedException("x86 binary number op '" + op.Operator.ToString() + "' not implemented");
            }

            if (opc1 == x86_64.x86_64_asm.opcode.NOP)
                throw new NotImplementedException("x86 binary number op '" + op.Operator.ToString() + "' not implemented");

            if (b_in_cl_if_not_const)
            {
                if (!(b is libasm.const_location))
                {
                    Assign(state, regs_in_use, Rcx, b, CliType.int32, ret);
                    b = Rcx;
                }
            }

            if (opc2 != x86_64.x86_64_asm.opcode.NOP)
            {
                libasm.multiple_hardware_location mhl_a = mhl_split(this, state, a, 2, 4);
                libasm.multiple_hardware_location mhl_dest = mhl_split(this, state, dest, 2, 4);
                libasm.hardware_location b_0, b_1;
                if (b_is_int32)
                {
                    b_0 = b;
                    b_1 = b;
                }
                else if (b == null)
                {
                    b_0 = null;
                    b_1 = null;
                }
                else
                {
                    libasm.multiple_hardware_location mhl_b = mhl_split(this, state, b, 2, 4);
                    b_0 = mhl_b[0];
                    b_1 = mhl_b[1];
                }

                ChooseInstruction(mov, ret, vara.MachineReg(Rax), vara.MachineReg(mhl_a.hlocs[0]));
                ChooseInstruction(mov, ret, vara.MachineReg(Rdx), vara.MachineReg(mhl_a.hlocs[1]));

                if (b != null)
                {
                    if (int64_backwards == false)
                    {
                        ChooseInstruction(opc1, ret, vara.MachineReg(Rax), vara.MachineReg(b_0));
                        ChooseInstruction(opc2, ret, vara.MachineReg(Rdx), vara.MachineReg(b_1));
                    }
                    else
                    {
                        ChooseInstruction(opc2, ret, vara.MachineReg(Rdx), vara.MachineReg(b_1));
                        ChooseInstruction(opc1, ret, vara.MachineReg(Rax), vara.MachineReg(b_0));
                    }
                }
                else
                {
                    if (int64_backwards == false)
                    {
                        ChooseInstruction(opc1, ret, vara.MachineReg(Rax));
                        ChooseInstruction(opc2, ret, vara.MachineReg(Rdx));
                    }
                    else
                    {
                        ChooseInstruction(opc2, ret, vara.MachineReg(Rdx));
                        ChooseInstruction(opc1, ret, vara.MachineReg(Rax));
                    }
                }
                ChooseInstruction(mov, ret, vara.MachineReg(mhl_dest.hlocs[0]), vara.MachineReg(Rax));
                ChooseInstruction(mov, ret, vara.MachineReg(mhl_dest.hlocs[1]), vara.MachineReg(Rdx));
            }
            else
            {
                if (opc1 == x86_64.x86_64_asm.opcode.IDIVL || opc1 == x86_64.x86_64_asm.opcode.IDIVQ ||
                    opc1 == x86_64.x86_64_asm.opcode.DIVL || opc1 == x86_64.x86_64_asm.opcode.DIVQ ||
                    opc1 == x86_64.x86_64_asm.opcode.MULL || opc1 == x86_64.x86_64_asm.opcode.MULQ)
                {
                    ChooseInstruction(mov, ret, Rax, a);
                    ChooseInstruction(opc1, ret, b);
                    ChooseInstruction(mov, ret, dest, idiv_ret);
                }
                else if ((dest is x86_64_reg) && dest.Equals(a))
                {
                    if (b == null)
                        ChooseInstruction(opc1, ret, vara.MachineReg(a));
                    else
                        ChooseInstruction(opc1, ret, vara.MachineReg(a), vara.MachineReg(b));
                }
                else
                {
                    if (dest is x86_64_gpr)
                    {
                        if (b == null)
                        {
                            ChooseInstruction(mov, ret, dest, a);
                            ChooseInstruction(opc1, ret, dest);
                        }
                        else
                        {
                            ChooseInstruction(mov, ret, dest, a);
                            ChooseInstruction(opc1, ret, dest, b);
                        }
                    }
                    else
                    {
                        if (b == null)
                        {
                            ChooseInstruction(mov, ret, temp, a);
                            ChooseInstruction(opc1, ret, temp);
                            ChooseInstruction(mov, ret, dest, temp);
                        }
                        else
                        {
                            ChooseInstruction(mov, ret, vara.MachineReg(temp), vara.MachineReg(a));
                            ChooseInstruction(opc1, ret, vara.MachineReg(temp), vara.MachineReg(b));
                            ChooseInstruction(mov, ret, vara.MachineReg(dest), vara.MachineReg(temp));
                        }
                    }
                }
            }
        }

        internal override void Mul(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location a, hardware_location b, CliType dt, List<tybel.Node> ret)
        {
            dest = ResolveStackLoc(this, state, dest);
            a = ResolveStackLoc(this, state, a);
            b = ResolveStackLoc(this, state, b);

            libasm.hardware_location act_dest = dest;
            if (!(dest is x86_64_gpr))
                act_dest = Rax;

            if (!a.Equals(act_dest))
                Assign(state, regs_in_use, act_dest, a, dt, ret);

            if (!(b is x86_64_gpr) && !(b is hardware_contentsof))
            {
                Assign(state, regs_in_use, Rdx, b, dt, ret);
                b = Rdx;
            }

            x86_64.x86_64_asm.opcode op = x86_64.x86_64_asm.opcode.IMULL;
            dt = ResolveNativeInt(dt);
            switch (dt)
            {
                case CliType.int32:
                    break;
                case CliType.int64:
                    if (ia == IA.x86_64)
                        op = x86_64.x86_64_asm.opcode.IMULQ;
                    else
                        throw new NotImplementedException();
                    break;
                default:
                    throw new NotImplementedException();
            }

            ChooseInstruction(op, ret, vara.MachineReg(act_dest), vara.MachineReg(b));

            if (!dest.Equals(act_dest))
                Assign(state, regs_in_use, dest, act_dest, dt, ret);
        }

        internal override void ThrowIf(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location a, hardware_location b, hardware_location throw_dest, hardware_location throw_obj, CliType dt, ThreeAddressCode.OpName op, List<tybel.Node> ret)
        {
            a = ResolveStackLoc(this, state, a);
            b = ResolveStackLoc(this, state, b);
            throw_dest = ResolveStackLoc(this, state, throw_dest);
            throw_obj = ResolveStackLoc(this, state, throw_obj);

            libasm.hardware_location act_b = b;
            if (!(a is x86_64_gpr) && !(b is x86_64_gpr))
            {
                act_b = Rax;
                EncMov(this, state, Rax, b, ret);
            }

            dt = ResolveNativeInt(dt);

            x86_64.x86_64_asm.opcode cmp_op = x86_64.x86_64_asm.opcode.CMPL;
            switch (dt)
            {
                case CliType.int32:
                    break;
                case CliType.int64:
                    if (ia == IA.x86_64)
                        cmp_op = x86_64.x86_64_asm.opcode.CMPQ;
                    else
                        throw new NotImplementedException();
                    break;
                default:
                    throw new NotImplementedException();
            }

            ChooseInstruction(cmp_op, ret, vara.MachineReg(a), vara.MachineReg(act_b));

            x86_64.x86_64_asm.opcode jmp_op = x86_64.x86_64_asm.opcode.NOP;
            switch (op)
            {
                case ThreeAddressCode.OpName.throwge_un:
                    jmp_op = x86_64.x86_64_asm.opcode.JB;
                    break;
                case ThreeAddressCode.OpName.throweq:
                    jmp_op = x86_64.x86_64_asm.opcode.JNZ;
                    break;
                case ThreeAddressCode.OpName.throwne:
                    jmp_op = x86_64.x86_64_asm.opcode.JZ;
                    break;
                default:
                    throw new NotImplementedException();
            }

            int success_block = state.next_blk++;
            ChooseInstruction(jmp_op, ret, vara.Label("L" + success_block.ToString(), false));

            Call(state, regs_in_use, throw_dest, null, new hardware_location[] { throw_obj }, callconv_throw, ret);

            ret.Add(new tybel.LabelNode("L" + success_block.ToString(), true));
        }

        internal override void Br(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, List<tybel.Node> ret)
        {
            if (!(dest is hardware_addressoflabel))
                throw new Exception("dest is not hardware_addressoflabel");
            hardware_addressoflabel aol = dest as hardware_addressoflabel;
            ChooseInstruction(x86_64.x86_64_asm.opcode.JMP, ret, vara.Label(aol.label, aol.is_object));
        }

        internal override void BrEhclause(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, List<tybel.Node> ret)
        {
            if (!(dest is hardware_addressoflabel))
                throw new Exception("dest is not hardware_addressoflabel");
            hardware_addressoflabel aol = dest as hardware_addressoflabel;
            ChooseInstruction(x86_64.x86_64_asm.opcode.CALL, ret, vara.Label(aol.label, aol.is_object));
        }

        internal override void Save(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location loc, List<tybel.Node> ret)
        {
            if (loc is x86_64_gpr)
                ChooseInstruction(x86_64.x86_64_asm.opcode.PUSH, ret, vara.MachineReg(loc));
            else if (loc is x86_64_xmm)
            {
                ChooseInstruction(ia == IA.i586 ? x86_64.x86_64_asm.opcode.SUBL : x86_64.x86_64_asm.opcode.SUBQ, ret, Rsp,
                    new libasm.const_location { c = 8 });
                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, new libasm.hardware_contentsof { base_loc = Rsp, size = 8 }, loc);
            }
            else
                throw new NotImplementedException();
        }

        internal override void Restore(frontend.cil.Encoder.EncoderState state, Stack regs_in_use, hardware_location loc, List<tybel.Node> ret)
        {
            if (loc is x86_64_gpr)
                ChooseInstruction(x86_64.x86_64_asm.opcode.POP, ret, vara.MachineReg(loc));
            else if (loc is x86_64_xmm)
            {
                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, loc, new libasm.hardware_contentsof { base_loc = Rsp, size = 8 } );
                ChooseInstruction(ia == IA.i586 ? x86_64.x86_64_asm.opcode.ADDL : x86_64.x86_64_asm.opcode.ADDQ, ret, Rsp,
                    new libasm.const_location { c = 8 });
            }
            else
                throw new NotImplementedException();
        }
    }
}
