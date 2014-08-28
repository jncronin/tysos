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
using System.Text;
using libasm;

namespace libtysila
{
    public partial class x86_64_Assembler : LSB_Assembler
    {
        public bool emitNop = false;

        enum OutputType { x86_64_large_elf64, x86_64_small_elf64, x86_64_jit, i586_elf64, i586_elf, i586_jit };
        internal enum IA { x86_64, i586 };
        OutputType OType;
        internal IA ia;

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

        public override RelocationBlock.RelocationType GetCodeToCodeRelocType()
        {
            return R_X86_64_PC32;
        }

        public override RelocationBlock.RelocationType GetCodeToDataRelocType()
        {
            if (Options.PIC)
                return R_X86_64_GOTPCREL;
            else
            {
                if (Arch.InstructionSet == "x86_64s")
                    return R_X86_64_32;
                else
                    return R_X86_64_64;
            }
        }

        public override RelocationBlock.RelocationType GetDataToCodeRelocType()
        {
            return R_X86_64_64;
        }

        public override RelocationBlock.RelocationType GetDataToDataRelocType()
        {
            return R_X86_64_64;
        }

        internal override void arch_init_opcodes()
        {
            // ia may not be initialized yet so we have to do it instead
            if (Arch.InstructionSet == "i586")
                ia = IA.i586;
            else
                ia = IA.x86_64;
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
            }
            else if ((arch.InstructionSet == "x86_64s") && (arch.OutputFormat == "elf64"))
            {
                OType = OutputType.x86_64_small_elf64;
                ia = IA.x86_64;
            }
            else if ((arch.InstructionSet == "x86_64") && (arch.OutputFormat == "jit"))
            {
                OType = OutputType.x86_64_jit;
                ia = IA.x86_64;
            }
            else if ((arch.InstructionSet == "i586") && (arch.OutputFormat == "elf64"))
            {
                OType = OutputType.i586_elf64;
                ia = IA.i586;
            }
            else if ((arch.InstructionSet == "i586") && (arch.OutputFormat == "elf"))
            {
                OType = OutputType.i586_elf;
                ia = IA.i586;
            }
            else if ((arch.InstructionSet == "i586") && (arch.OutputFormat == "jit"))
            {
                OType = OutputType.i586_jit;
                ia = IA.i586;
            }
            else
                throw new Exception("Invalid architecture: " + arch.ToString());

            if (arch.OutputFormat == "jit")
                is_jit = true;

            if (arch.OperatingSystem != "tysos")
                throw new Exception("Invalid operating system: " + arch.OperatingSystem);

            options.RegAlloc = AssemblerOptions.RegisterAllocatorType.graphcolour;
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

        internal override Bitness GetBitness()
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
                    if(ia == IA.x86_64)
                        return 8;
                    else
                        return 4;                    
                case CliType.vt:
                    return GetSizeOfType(p);
                case CliType.virtftnptr:
                    if (ia == IA.x86_64)
                        return 16;
                    else
                        return 8;
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
                if(GetBitness() == Bitness.Bits64)
                    ret.Add(R8); ret.Add(R9); ret.Add(R10); ret.Add(R11); ret.Add(R12); ret.Add(R13); ret.Add(R14); ret.Add(R15);
                ret.Add(Xmm0); ret.Add(Xmm1); ret.Add(Xmm2); ret.Add(Xmm3); ret.Add(Xmm4); ret.Add(Xmm5); ret.Add(Xmm6); ret.Add(Xmm7);
                if (GetBitness() == Bitness.Bits64)
                    ret.Add(Xmm8); ret.Add(Xmm9); ret.Add(Xmm10); ret.Add(Xmm11); ret.Add(Xmm12); ret.Add(Xmm13); ret.Add(Xmm14); ret.Add(Xmm15);

                return ret;
            }
        }

        public override util.Set<hardware_location> MachineRegistersForDataType(CliType dt, bool needs_memloc, Assembler.MethodAttributes attrs)
        {
            if (needs_memloc)
            {
                util.Set<hardware_location> memlocs = new util.Set<hardware_location>();
                int size = GetSizeOf(new Signature.Param(dt));
                memlocs.Add(new hardware_stackloc { loc = attrs.next_stackloc++, size = size });
                attrs.MachineRegistersStackLocSizes[attrs.next_stackloc - 1] = size;
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

            foreach (int sl in attrs.MachineRegistersStackLocSizes.Keys)
            {
                int size = attrs.MachineRegistersStackLocSizes[sl];
                int memsize = util.align(size, ia == IA.i586 ? 4 : 8);
                cur_offset -= memsize;
                hardware_stackloc hsl = new hardware_stackloc { loc = sl, size = size };
                ret[hsl] = new hardware_contentsof { base_loc = Rbp, const_offset = cur_offset, size = size };
            }

            attrs.lv_stack_space = -cur_offset;

            return ret;
        }
    }
}
