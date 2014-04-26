﻿/* Copyright (C) 2014 by John Cronin
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

namespace libtysila.x86_64
{
    class x86_64_asm
    {
        public bool rex_w;
        public bool prefix_0f;
        public byte opcode_ext;
        public bool opcode_adds;
        public bool has_rm;
        public bool prefix_66;
        public byte pri_opcode;
        public bool is_move;

        public string int_name;

        public libasm.hardware_location[] inputs;
        public libasm.hardware_location[] outputs;
        public optype[] ops;

        public enum optype
        {
            R8, R16, R32, R64, RM8, RM16, RM32, RM64, Imm8, Imm16, Imm32, Imm64,
            Rel8, Rel16, Rel32, Rel64,
            rax, rbx, rcx, rdx, rdi, rsi, rbp, rsp, r8, r9, r10, r11, r12, r13, r14, r15,
        }

        public class op_loc : libasm.hardware_location
        {
            public int op_idx;
            public op_loc(int OpIdx) { op_idx = OpIdx; }
            public override string ToString()
            {
                return "O" + op_idx.ToString();
            }
        }

        public enum opcode
        {
            ADDL, ADDQ, PUSH, POP, OR, ADC, SBB, AND, ES,
            DAA, SUB, CS, NTAKEN, DAS, XOR, SS, AAA,
            CMPL, CMPQ, DS, TAKEN, AAS, INC, REX, REXB, REXX,
            REXXB, REXR, REXRB, REXRX, REXRXB, DEC, REXW, REXWB,
            REXWX, REXWXB, REXWR, REXWRB, REXWRX, REXWRXB, PUSHA, PUSHAD,
            POPA, POPAD, BOUND, ARPL, MOVSXD, FS, ALTER, GS,
            IMUL, INS, OUTS, JO, JNO, JB, JNB, JZ,
            JNZ, JBE, JNBE, JS, JNS, JP, JNP, JL,
            JNL, JLE, JNLE, TEST, XCHG, MOVL, MOVQ, LEA, NOP,
            PAUSE, CBW, CWDE, CWD, CDQ, CALLF, FWAIT, PUSHF,
            PUSHFD, POPF, POPFD, SAHF, LAHF, MOVS, CMPS, STOS,
            LODS, SCAS, ROL, ROR, RCL, RCR, SHL, SHR,
            SAL, SAR, RETN, LES, LDS, ENTER, LEAVE, RETF,
            INT, INTO, IRET, IRETD, AAM, AMX, AAD, ADX,
            SALC, XLAT, FADD, FMUL, FCOM, FCOMP, FSUB, FSUBR,
            FDIV, FDIVR, FLD, FXCH, FST, FNOP, FSTP, FSTP1,
            FLDENV, FCHS, FABS, FTST, FXAM, FLDCW, FLD1, FLDL2T,
            FLDL2E, FLDPI, FLDLG2, FLDLN2, FLDZ, FNSTENV, FSTENV, F2XM1,
            FYL2X, FPTAN, FPATAN, FXTRACT, FPREM1, FDECSTP, FINCSTP, FNSTCW,
            FSTCW, FPREM, FYL2XP1, FSQRT, FSINCOS, FRNDINT, FSCALE, FSIN,
            FCOS, FIADD, FCMOVB, FIMUL, FCMOVE, FICOM, FCMOVBE, FICOMP,
            FCMOVU, FISUB, FISUBR, FUCOMPP, FIDIV, FIDIVR, FILD, FCMOVNB,
            FISTTP, FCMOVNE, FIST, FCMOVNBE, FISTP, FCMOVNU, FNENI, FENI,
            FNDISI, FDISI, FNCLEX, FCLEX, FNINIT, FINIT, FNSETPM, FSETPM,
            FUCOMI, FCOMI, FCOM2, FCOMP3, FFREE, FXCH4, FRSTOR, FUCOM,
            FUCOMP, FNSAVE, FSAVE, FNSTSW, FSTSW, FADDP, FMULP, FCOMP5,
            FCOMPP, FSUBRP, FSUBP, FDIVRP, FDIVP, FFREEP, FXCH7, FSTP8,
            FSTP9, FBLD, FUCOMIP, FBSTP, FCOMIP, LOOPNZ, LOOPZ, LOOP,
            JCXZ, JECXZ, IN, OUT, CALL, JMP, JMPF, LOCK,
            INT1, REPNZ, REP, REPZ, HLT, CMC, NOT, NEG,
            MUL, DIV, IDIV, CLC, STC, CLI, STI, CLD,
            STD, SLDT, STR, LLDT, LTR, VERR, VERW, JMPE,
            SGDT, VMCALL, VMLAUNCH, VMRESUME, VMXOFF, SIDT, MONITOR, MWAIT,
            LGDT, XGETBV, XSETBV, LIDT, SMSW, LMSW, INVLPG, SWAPGS,
            RDTSCP, LAR, LSL, LOADALL, SYSCALL, CLTS, SYSRET, INVD,
            WBINVD, UD2, MOVUPS, MOVSS, MOVUPD, MOVSD, MOVHLPS, MOVLPS,
            MOVLPD, MOVDDUP, MOVSLDUP, UNPCKLPS, UNPCKLPD, UNPCKHPS, UNPCKHPD, MOVLHPS,
            MOVHPS, MOVHPD, MOVSHDUP, HINT_NOP, PREFETCHNTA, PREFETCHT0, PREFETCHT1, PREFETCHT2,
            MOVAPS, MOVAPD, CVTPI2PS, CVTSI2SS, CVTPI2PD, CVTSI2SD, MOVNTPS, MOVNTPD,
            CVTTPS2PI, CVTTSS2SI, CVTTPD2PI, CVTTSD2SI, CVTPS2PI, CVTSS2SI, CVTPD2PI, CVTSD2SI,
            UCOMISS, UCOMISD, COMISS, COMISD, WRMSR, RDTSC, RDMSR, RDPMC,
            SYSENTER, SYSEXIT, GETSEC, PSHUFB, PHADDW, PHADDD, PHADDSW, PMADDUBSW,
            PHSUBW, PHSUBD, PHSUBSW, PSIGNB, PSIGNW, PSIGND, PMULHRSW, PBLENDVB,
            BLENDVPS, BLENDVPD, PTEST, PABSB, PABSW, PABSD, PMOVSXBW, PMOVSXBD,
            PMOVSXBQ, PMOVSXWD, PMOVSXWQ, PMOVSXDQ, PMULDQ, PCMPEQQ, MOVNTDQA, PACKUSDW,
            PMOVZXBW, PMOVZXBD, PMOVZXBQ, PMOVZXWD, PMOVZXWQ, PMOVZXDQ, PCMPGTQ, PMINSB,
            PMINSD, PMINUW, PMINUD, PMAXSB, PMAXSD, PMAXUW, PMAXUD, PMULLD,
            PHMINPOSUW, INVEPT, INVVPID, MOVBE, CRC32, ROUNDPS, ROUNDPD, ROUNDSS,
            ROUNDSD, BLENDPS, BLENDPD, PBLENDW, PALIGNR, PEXTRB, PEXTRW, PEXTRD,
            EXTRACTPS, PINSRB, INSERTPS, PINSRD, DPPS, DPPD, MPSADBW, PCMPESTRM,
            PCMPESTRI, PCMPISTRM, PCMPISTRI, CMOVO, CMOVNO, CMOVB, CMOVNB, CMOVZ,
            CMOVNZ, CMOVBE, CMOVNBE, CMOVS, CMOVNS, CMOVP, CMOVNP, CMOVL,
            CMOVNL, CMOVLE, CMOVNLE, MOVMSKPS, MOVMSKPD, SQRTPS, SQRTSS, SQRTPD,
            SQRTSD, RSQRTPS, RSQRTSS, RCPPS, RCPSS, ANDPS, ANDPD, ANDNPS,
            ANDNPD, ORPS, ORPD, XORPS, XORPD, ADDPS, ADDSS, ADDPD,
            ADDSD, MULPS, MULSS, MULPD, MULSD, CVTPS2PD, CVTPD2PS, CVTSS2SD,
            CVTSD2SS, CVTDQ2PS, CVTPS2DQ, CVTTPS2DQ, SUBPS, SUBSS, SUBPD, SUBSD,
            MINPS, MINSS, MINPD, MINSD, DIVPS, DIVSS, DIVPD, DIVSD,
            MAXPS, MAXSS, MAXPD, MAXSD, PUNPCKLBW, PUNPCKLWD, PUNPCKLDQ, PACKSSWB,
            PCMPGTB, PCMPGTW, PCMPGTD, PACKUSWB, PUNPCKHBW, PUNPCKHWD, PUNPCKHDQ, PACKSSDW,
            PUNPCKLQDQ, PUNPCKHQDQ, MOVD, MOVDQA, MOVDQU, PSHUFW, PSHUFLW,
            PSHUFHW, PSHUFD, PSRLW, PSRAW, PSLLW, PSRLD, PSRAD, PSLLD,
            PSRLQ, PSRLDQ, PSLLQ, PSLLDQ, PCMPEQB, PCMPEQW, PCMPEQD, EMMS,
            VMREAD, VMWRITE, HADDPD, HADDPS, HSUBPD, HSUBPS, SETO, SETNO,
            SETB, SETNB, SETZ, SETNZ, SETBE, SETNBE, SETS, SETNS,
            SETP, SETNP, SETL, SETNL, SETLE, SETNLE, CPUID, BT,
            SHLD, RSM, BTS, SHRD, FXSAVE, FXRSTOR, LDMXCSR, STMXCSR,
            XSAVE, LFENCE, XRSTOR, MFENCE, SFENCE, CLFLUSH, CMPXCHG, LSS,
            BTR, LFS, LGS, MOVZX, POPCNT, UD, BTC, BSF,
            BSR, MOVSX, XADD, CMPPS, CMPSS, CMPPD, CMPSD, MOVNTI,
            PINSRW, SHUFPS, SHUFPD, CMPXCHG8B, VMPTRLD, VMCLEAR, VMXON, VMPTRST,
            BSWAP, ADDSUBPD, ADDSUBPS, PADDQ, PMULLW, MOVQ2DQ, MOVDQ2Q, PMOVMSKB,
            PSUBUSB, PSUBUSW, PMINUB, PAND, PADDUSB, PADDUSW, PMAXUB, PANDN,
            PAVGB, PAVGW, PMULHUW, PMULHW, CVTPD2DQ, CVTTPD2DQ, CVTDQ2PD, MOVNTQ,
            MOVNTDQ, PSUBSB, PSUBSW, PMINSW, POR, PADDSB, PADDSW, PMAXSW,
            PXOR, LDDQU, PMULUDQ, PMADDWD, PSADBW, MASKMOVQ, MASKMOVDQU, PSUBB,
            PSUBW, PSUBD, PSUBQ, PADDB, PADDW, PADDD
        };
    
        public static Dictionary<opcode, List<x86_64_asm>> Opcodes = null;

        public static void InitOpcodes(Assembler.Bitness bitness)
        {
            Opcodes = new Dictionary<opcode, List<x86_64_asm>>();

            foreach (opcode o in (opcode[])Enum.GetValues(typeof(opcode)))
            {
                Opcodes[o] = new List<x86_64_asm>();
            }

            Opcodes[opcode.MOVL].Add(new x86_64_asm { int_name = "mov_r32_imm32", pri_opcode = 0xb8, opcode_adds = true, is_move = true, ops = new optype[] { optype.R32, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVL].Add(new x86_64_asm { int_name = "mov_rm32_imm32", pri_opcode = 0xc7, has_rm = true, opcode_ext = 0, is_move = true, ops = new optype[] { optype.RM32, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVL].Add(new x86_64_asm { int_name = "mov_r32_rm32", pri_opcode = 0x8b, has_rm = true, is_move = true, ops = new optype[] { optype.R32, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVL].Add(new x86_64_asm { int_name = "mov_rm32_r32", pri_opcode = 0x89, has_rm = true, is_move = true, ops = new optype[] { optype.RM32, optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_r64_imm64", pri_opcode = 0xb8, rex_w = true, opcode_adds = true, is_move = true, ops = new optype[] { optype.R64, optype.Imm64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_rm64_imm32", pri_opcode = 0xc7, rex_w = true, has_rm = true, opcode_ext = 0, is_move = true, ops = new optype[] { optype.RM64, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_r64_rm64", pri_opcode = 0x8b, rex_w = true, has_rm = true, is_move = true, ops = new optype[] { optype.R64, optype.RM64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_rm64_r64", pri_opcode = 0x89, rex_w = true, has_rm = true, is_move = true, ops = new optype[] { optype.RM64, optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_r64_rm32", pri_opcode = 0x8b, has_rm = true, is_move = true, ops = new optype[] { optype.R64, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_rm64_r32", pri_opcode = 0x89, has_rm = true, is_move = true, ops = new optype[] { optype.RM64, optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_r32_rm64", pri_opcode = 0x8b, has_rm = true, is_move = true, ops = new optype[] { optype.R32, optype.RM64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_rm32_r64", pri_opcode = 0x89, has_rm = true, is_move = true, ops = new optype[] { optype.RM32, optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.ADDL].Add(new x86_64_asm { int_name = "add_eax_imm32", pri_opcode = 0x05, ops = new optype[] { optype.rax, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDL].Add(new x86_64_asm { int_name = "add_rm32_imm32", pri_opcode = 0x81, has_rm = true, opcode_ext = 0, ops = new optype[] { optype.RM32, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDL].Add(new x86_64_asm { int_name = "add_rm32_r32", pri_opcode = 0x01, has_rm = true, ops = new optype[] { optype.RM32, optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDL].Add(new x86_64_asm { int_name = "add_r32_rm32", pri_opcode = 0x03, has_rm = true, ops = new optype[] { optype.R32, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.CALL].Add(new x86_64_asm { int_name = "call_void_rel32", pri_opcode = 0xe8, ops = new optype[] { optype.Rel32 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CALL].Add(new x86_64_asm { int_name = "call_eax_rel32", pri_opcode = 0xe8, ops = new optype[] { optype.rax, optype.Rel32 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.CMPL].Add(new x86_64_asm { int_name = "cmp_eax_imm32", pri_opcode = 0x3d, ops = new optype[] { optype.rax, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPL].Add(new x86_64_asm { int_name = "cmp_rm32_imm32", pri_opcode = 0x81, has_rm = true, opcode_ext = 7, ops = new optype[] { optype.RM32, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPL].Add(new x86_64_asm { int_name = "cmp_rm32_r32", pri_opcode = 0x39, has_rm = true, ops = new optype[] { optype.RM32, optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPL].Add(new x86_64_asm { int_name = "cmp_r32_rm32", pri_opcode = 0x3b, has_rm = true, ops = new optype[] { optype.R32, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.JLE].Add(new x86_64_asm { int_name = "jle_rel8", pri_opcode = 0x7e, ops = new optype[] { optype.Rel8 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.JLE].Add(new x86_64_asm { int_name = "jle_rel32", pri_opcode = 0x8e, prefix_0f = true, ops = new optype[] { optype.Rel32 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.LEAVE].Add(new x86_64_asm { int_name = "leave", pri_opcode = 0xc9, ops = new optype[] { }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.POP].Add(new x86_64_asm { int_name = "pop_r32", pri_opcode = 0x58, opcode_adds = true, ops = new optype[] { optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.POP].Add(new x86_64_asm { int_name = "pop_r64", pri_opcode = 0x58, opcode_adds = true, ops = new optype[] { optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.PUSH].Add(new x86_64_asm { int_name = "push_r32", pri_opcode = 0x50, opcode_adds = true, ops = new optype[] { optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.PUSH].Add(new x86_64_asm { int_name = "push_r64", pri_opcode = 0x50, opcode_adds = true, ops = new optype[] { optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.RETN].Add(new x86_64_asm { int_name = "retn", pri_opcode = 0xc3, ops = new optype[] { }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.RETN].Add(new x86_64_asm { int_name = "retn_rax", pri_opcode = 0xc3, ops = new optype[] { optype.rax }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });
        }
    }
}
