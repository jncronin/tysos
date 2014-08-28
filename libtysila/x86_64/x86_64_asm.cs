/* Copyright (C) 2014 by John Cronin
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
        public byte pri_opcode;
        public bool is_move;
        public bool op_size_prefix;
        public bool addr_size_prefix;
        public bool seg_override_prefix;
        public byte seg_override;
        public bool grp1_prefix;
        public byte grp1;

        public string int_name;

        public libasm.hardware_location[] inputs;
        public libasm.hardware_location[] outputs;
        public optype[] ops;
        public OverrideFuncType OverrideFunc = null;

        public delegate IEnumerable<libasm.OutputBlock> OverrideFuncType(x86_64.x86_64_asm n, Assembler.MethodAttributes attrs);

        public enum optype
        {
            R8, R16, R32, R64, RM8, RM16, RM32, RM64, Imm8, Imm16, Imm32, Imm64,
            R8163264, RM8163264,
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
            DAA, SUBL, SUBQ, CS, NTAKEN, DAS, XORL, XORQ, SS, AAA,
            CMPL, CMPQ, DS, TAKEN, AAS, INC, REX, REXB, REXX,
            REXXB, REXR, REXRB, REXRX, REXRXB, DEC, REXW, REXWB,
            REXWX, REXWXB, REXWR, REXWRB, REXWRX, REXWRXB, PUSHA, PUSHAD,
            POPA, POPAD, BOUND, ARPL, MOVSXB, MOVSXW, MOVSXD, MOVZXB, MOVZXW, FS, ALTER, GS,
            IMUL, INS, OUTS, JO, JNO, JB, JNB, JZ,
            JNZ, JBE, JNBE, JS, JNS, JP, JNP, JL,
            JNL, JLE, JNLE, TEST, XCHG, MOVB, MOVW, MOVL, MOVQ, LEAL, LEAQ, NOP,
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
            PSUBW, PSUBD, PSUBQ, PADDB, PADDW, PADDD,

            METHPREFIX
        };
    
        public static Dictionary<opcode, List<x86_64_asm>> Opcodes = null;

        public static void InitOpcodes(Assembler.Bitness bitness)
        {
            Opcodes = new Dictionary<opcode, List<x86_64_asm>>();

            foreach (opcode o in (opcode[])Enum.GetValues(typeof(opcode)))
            {
                Opcodes[o] = new List<x86_64_asm>();
            }

            Opcodes[opcode.METHPREFIX].Add(new x86_64_asm { int_name = "methprefix", OverrideFunc = MethPrefixOverride, ops = new optype[] { }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.MOVB].Add(new x86_64_asm { int_name = "mov_r8_rm8", pri_opcode = 0x8a, has_rm = true, ops = new optype[] { optype.R8163264, optype.RM8 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVB].Add(new x86_64_asm { int_name = "mov_rm8_r8", pri_opcode = 0x88, has_rm = true, ops = new optype[] { optype.RM8, optype.R8163264 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVB].Add(new x86_64_asm { int_name = "mov_r8_imm8", pri_opcode = 0xb0, opcode_adds = true, ops = new optype[] { optype.R8163264, optype.Imm8 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVB].Add(new x86_64_asm { int_name = "mov_rm8_imm8", pri_opcode = 0xc6, has_rm = true, opcode_ext = 0, ops = new optype[] { optype.RM8163264, optype.Imm8 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.MOVL].Add(new x86_64_asm { int_name = "mov_r32_imm32", pri_opcode = 0xb8, opcode_adds = true, is_move = true, ops = new optype[] { optype.R32, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVL].Add(new x86_64_asm { int_name = "mov_rm32_imm32", pri_opcode = 0xc7, has_rm = true, opcode_ext = 0, is_move = true, ops = new optype[] { optype.RM32, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVL].Add(new x86_64_asm { int_name = "mov_r32_rm32", pri_opcode = 0x8b, has_rm = true, is_move = true, ops = new optype[] { optype.R32, optype.RM8163264 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVL].Add(new x86_64_asm { int_name = "mov_rm32_r32", pri_opcode = 0x89, has_rm = true, is_move = true, ops = new optype[] { optype.RM32, optype.R8163264 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_r64_imm64", pri_opcode = 0xb8, rex_w = true, opcode_adds = true, is_move = true, ops = new optype[] { optype.R64, optype.Imm64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_rm64_imm32", pri_opcode = 0xc7, rex_w = true, has_rm = true, opcode_ext = 0, is_move = true, ops = new optype[] { optype.RM64, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_r64_rm64", pri_opcode = 0x8b, rex_w = true, has_rm = true, is_move = true, ops = new optype[] { optype.R64, optype.RM64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_rm64_r64", pri_opcode = 0x89, rex_w = true, has_rm = true, is_move = true, ops = new optype[] { optype.RM64, optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_r64_rm32", pri_opcode = 0x8b, has_rm = true, is_move = true, ops = new optype[] { optype.R64, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_rm64_r32", pri_opcode = 0x89, has_rm = true, is_move = true, ops = new optype[] { optype.RM64, optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_r32_rm64", pri_opcode = 0x8b, has_rm = true, is_move = true, ops = new optype[] { optype.R32, optype.RM64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVQ].Add(new x86_64_asm { int_name = "mov_rm32_r64", pri_opcode = 0x89, has_rm = true, is_move = true, ops = new optype[] { optype.RM32, optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.MOVZXB].Add(new x86_64_asm { int_name = "movzxb_r32_rm8", prefix_0f = true, pri_opcode = 0xb6, has_rm = true, ops = new optype[] { optype.R32, optype.RM8163264 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVZXB].Add(new x86_64_asm { int_name = "movzxb_r64_rm8", prefix_0f = true, pri_opcode = 0xb6, rex_w = true, has_rm = true, ops = new optype[] { optype.R64, optype.RM8163264 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVZXW].Add(new x86_64_asm { int_name = "movzxw_r32_rm16", prefix_0f = true, pri_opcode = 0xb7, has_rm = true, ops = new optype[] { optype.R32, optype.RM8163264 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVZXW].Add(new x86_64_asm { int_name = "movzxw_r64_rm16", prefix_0f = true, pri_opcode = 0xb7, rex_w = true, has_rm = true, ops = new optype[] { optype.R64, optype.RM8163264 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.MOVSXB].Add(new x86_64_asm { int_name = "movsxb_r32_rm8", prefix_0f = true, pri_opcode = 0xbe, has_rm = true, ops = new optype[] { optype.R32, optype.RM8 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVSXB].Add(new x86_64_asm { int_name = "movsxb_r64_rm8", prefix_0f = true, pri_opcode = 0xbe, rex_w = true, has_rm = true, ops = new optype[] { optype.R64, optype.RM8 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVSXW].Add(new x86_64_asm { int_name = "movsxw_r32_rm16", prefix_0f = true, pri_opcode = 0xbf, has_rm = true, ops = new optype[] { optype.R32, optype.RM8 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVSXW].Add(new x86_64_asm { int_name = "movsxw_r64_rm16", prefix_0f = true, pri_opcode = 0xbf, rex_w = true, has_rm = true, ops = new optype[] { optype.R64, optype.RM8 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.MOVSXD].Add(new x86_64_asm { int_name = "movsxd_r64_rm32", pri_opcode = 0x63, rex_w = true, ops = new optype[] { optype.R64, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.MOVSXD].Add(new x86_64_asm { int_name = "mov_rm64_imm32", pri_opcode = 0xc7, rex_w = true, ops = new optype[] { optype.RM64, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.ADDL].Add(new x86_64_asm { int_name = "add_rm32_imm8", pri_opcode = 0x83, has_rm = true, opcode_ext = 0, ops = new optype[] { optype.RM32, optype.Imm8 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDL].Add(new x86_64_asm { int_name = "add_eax_imm32", pri_opcode = 0x05, ops = new optype[] { optype.rax, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDL].Add(new x86_64_asm { int_name = "add_rm32_imm32", pri_opcode = 0x81, has_rm = true, opcode_ext = 0, ops = new optype[] { optype.RM32, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDL].Add(new x86_64_asm { int_name = "add_rm32_r32", pri_opcode = 0x01, has_rm = true, ops = new optype[] { optype.RM32, optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDL].Add(new x86_64_asm { int_name = "add_r32_rm32", pri_opcode = 0x03, has_rm = true, ops = new optype[] { optype.R32, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.ADDQ].Add(new x86_64_asm { int_name = "add_rm64_imm8", pri_opcode = 0x83, rex_w = true, has_rm = true, opcode_ext = 0, ops = new optype[] { optype.RM64, optype.Imm8 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDQ].Add(new x86_64_asm { int_name = "add_rax_imm32", pri_opcode = 0x05, rex_w = true, ops = new optype[] { optype.rax, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDQ].Add(new x86_64_asm { int_name = "add_rm64_imm32", pri_opcode = 0x81, rex_w = true, has_rm = true, opcode_ext = 0, ops = new optype[] { optype.RM64, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDQ].Add(new x86_64_asm { int_name = "add_rm64_r64", pri_opcode = 0x01, rex_w = true, has_rm = true, ops = new optype[] { optype.RM64, optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.ADDQ].Add(new x86_64_asm { int_name = "add_r64_rm64", pri_opcode = 0x03, rex_w = true, has_rm = true, ops = new optype[] { optype.R64, optype.RM64 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.CALL].Add(new x86_64_asm { int_name = "call_void_rel32", pri_opcode = 0xe8, ops = new optype[] { optype.Rel32 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CALL].Add(new x86_64_asm { int_name = "call_eax_rel32", pri_opcode = 0xe8, ops = new optype[] { optype.rax, optype.Rel32 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.CALL].Add(new x86_64_asm { int_name = "call_void_rm32", pri_opcode = 0xff, has_rm = true, opcode_ext = 2, ops = new optype[] { optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CALL].Add(new x86_64_asm { int_name = "call_eax_rm32", pri_opcode = 0xff, has_rm = true, opcode_ext = 2, ops = new optype[] { optype.rax, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.CALL].Add(new x86_64_asm { int_name = "call_void_rm64", pri_opcode = 0xff, has_rm = true, opcode_ext = 2, ops = new optype[] { optype.RM64 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CALL].Add(new x86_64_asm { int_name = "call_eax_rm64", pri_opcode = 0xff, has_rm = true, opcode_ext = 2, ops = new optype[] { optype.rax, optype.RM64 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.CMPL].Add(new x86_64_asm { int_name = "cmp_rm32_imm8", pri_opcode = 0x83, has_rm = true, opcode_ext = 7, ops = new optype[] { optype.RM32, optype.Imm8 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPL].Add(new x86_64_asm { int_name = "cmp_eax_imm32", pri_opcode = 0x3d, ops = new optype[] { optype.rax, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPL].Add(new x86_64_asm { int_name = "cmp_rm32_imm32", pri_opcode = 0x81, has_rm = true, opcode_ext = 7, ops = new optype[] { optype.RM32, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPL].Add(new x86_64_asm { int_name = "cmp_rm32_r32", pri_opcode = 0x39, has_rm = true, ops = new optype[] { optype.RM32, optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPL].Add(new x86_64_asm { int_name = "cmp_r32_rm32", pri_opcode = 0x3b, has_rm = true, ops = new optype[] { optype.R32, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.CMPQ].Add(new x86_64_asm { int_name = "cmp_rm64_imm8", pri_opcode = 0x83, rex_w = true, has_rm = true, opcode_ext = 7, ops = new optype[] { optype.RM64, optype.Imm8 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPQ].Add(new x86_64_asm { int_name = "cmp_rax_imm32", pri_opcode = 0x3d, rex_w = true, ops = new optype[] { optype.rax, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPQ].Add(new x86_64_asm { int_name = "cmp_rm64_imm32", pri_opcode = 0x81, rex_w = true, has_rm = true, opcode_ext = 7, ops = new optype[] { optype.RM64, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPQ].Add(new x86_64_asm { int_name = "cmp_rm64_r64", pri_opcode = 0x39, rex_w = true, has_rm = true, ops = new optype[] { optype.RM64, optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.CMPQ].Add(new x86_64_asm { int_name = "cmp_r64_rm64", pri_opcode = 0x3b, rex_w = true, has_rm = true, ops = new optype[] { optype.R64, optype.RM64 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.JMP].Add(new x86_64_asm { int_name = "jmp_rel8", pri_opcode = 0xeb, ops = new optype[] { optype.Rel8 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.JMP].Add(new x86_64_asm { int_name = "jmp_rel32", pri_opcode = 0xe9, ops = new optype[] { optype.Rel32 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.JLE].Add(new x86_64_asm { int_name = "jle_rel8", pri_opcode = 0x7e, ops = new optype[] { optype.Rel8 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.JLE].Add(new x86_64_asm { int_name = "jle_rel32", pri_opcode = 0x8e, prefix_0f = true, ops = new optype[] { optype.Rel32 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.JNZ].Add(new x86_64_asm { int_name = "jnz_rel8", pri_opcode = 0x75, ops = new optype[] { optype.Rel8 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.JNZ].Add(new x86_64_asm { int_name = "jnz_rel32", pri_opcode = 0x85, prefix_0f = true, ops = new optype[] { optype.Rel32 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.JZ].Add(new x86_64_asm { int_name = "jz_rel8", pri_opcode = 0x74, ops = new optype[] { optype.Rel8 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.JZ].Add(new x86_64_asm { int_name = "jz_rel32", pri_opcode = 0x84, prefix_0f = true, ops = new optype[] { optype.Rel32 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.LEAL].Add(new x86_64_asm { int_name = "lea_r32", pri_opcode = 0x8d, has_rm = true, ops = new optype[] { optype.R32, optype.RM8163264 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.LEAQ].Add(new x86_64_asm { int_name = "lea_r64", pri_opcode = 0x8d, rex_w = true, has_rm = true, ops = new optype[] { optype.R64, optype.RM8163264 }, inputs = new libasm.hardware_location[] { new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.LEAVE].Add(new x86_64_asm { int_name = "leave", pri_opcode = 0xc9, ops = new optype[] { }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.POP].Add(new x86_64_asm { int_name = "pop_r32", pri_opcode = 0x58, opcode_adds = true, ops = new optype[] { optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.POP].Add(new x86_64_asm { int_name = "pop_r64", pri_opcode = 0x58, opcode_adds = true, ops = new optype[] { optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.PUSH].Add(new x86_64_asm { int_name = "push_r32", pri_opcode = 0x50, opcode_adds = true, ops = new optype[] { optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.PUSH].Add(new x86_64_asm { int_name = "push_r64", pri_opcode = 0x50, opcode_adds = true, ops = new optype[] { optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.RETN].Add(new x86_64_asm { int_name = "retn", pri_opcode = 0xc3, ops = new optype[] { }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { } });
            Opcodes[opcode.RETN].Add(new x86_64_asm { int_name = "retn_rax", pri_opcode = 0xc3, ops = new optype[] { optype.rax }, inputs = new libasm.hardware_location[] { new op_loc(0) }, outputs = new libasm.hardware_location[] { } });

            Opcodes[opcode.SETL].Add(new x86_64_asm { int_name = "setl_rm8", pri_opcode = 0x9c, prefix_0f = true, has_rm = true, opcode_ext = 2, ops = new optype[] { optype.RM8163264 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.SETZ].Add(new x86_64_asm { int_name = "setz_rm8", pri_opcode = 0x94, prefix_0f = true, has_rm = true, opcode_ext = 2, ops = new optype[] { optype.RM8163264 }, inputs = new libasm.hardware_location[] { }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.SUBL].Add(new x86_64_asm { int_name = "sub_rm32_imm8", pri_opcode = 0x83, has_rm = true, opcode_ext = 5, ops = new optype[] { optype.RM32, optype.Imm8 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.SUBL].Add(new x86_64_asm { int_name = "sub_eax_imm32", pri_opcode = 0x2d, ops = new optype[] { optype.rax, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.SUBL].Add(new x86_64_asm { int_name = "sub_rm32_imm32", pri_opcode = 0x81, has_rm = true, opcode_ext = 5, ops = new optype[] { optype.RM32, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.SUBL].Add(new x86_64_asm { int_name = "sub_rm32_r32", pri_opcode = 0x29, has_rm = true, ops = new optype[] { optype.RM32, optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.SUBL].Add(new x86_64_asm { int_name = "sub_r32_rm32", pri_opcode = 0x2b, has_rm = true, ops = new optype[] { optype.R32, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.SUBQ].Add(new x86_64_asm { int_name = "sub_rm64_imm8", pri_opcode = 0x83, rex_w = true, has_rm = true, opcode_ext = 5, ops = new optype[] { optype.RM64, optype.Imm8 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.SUBQ].Add(new x86_64_asm { int_name = "sub_rax_imm32", pri_opcode = 0x2d, rex_w = true, ops = new optype[] { optype.rax, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.SUBQ].Add(new x86_64_asm { int_name = "sub_rm64_imm32", pri_opcode = 0x81, rex_w = true, has_rm = true, opcode_ext = 5, ops = new optype[] { optype.RM64, optype.Imm32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.SUBQ].Add(new x86_64_asm { int_name = "sub_rm64_r64", pri_opcode = 0x29, rex_w = true, has_rm = true, ops = new optype[] { optype.RM64, optype.R64 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.SUBQ].Add(new x86_64_asm { int_name = "sub_r64_rm64", pri_opcode = 0x2b, rex_w = true, has_rm = true, ops = new optype[] { optype.R64, optype.RM64 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });

            Opcodes[opcode.XORL].Add(new x86_64_asm { int_name = "xor_rm32_r32", pri_opcode = 0x31, has_rm = true, ops = new optype[] { optype.RM32, optype.R32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
            Opcodes[opcode.XORL].Add(new x86_64_asm { int_name = "xor_r32_rm32", pri_opcode = 0x33, has_rm = true, ops = new optype[] { optype.R32, optype.RM32 }, inputs = new libasm.hardware_location[] { new op_loc(0), new op_loc(1) }, outputs = new libasm.hardware_location[] { new op_loc(0) } });
        }

        public static opcode GetCTOpcode(Assembler.CliType ct, Assembler.Bitness bt, opcode int32, opcode int64)
        {
            switch (ct)
            {
                case Assembler.CliType.int32:
                    return int32;
                case Assembler.CliType.int64:
                    return int64;
                case Assembler.CliType.native_int:
                case Assembler.CliType.O:
                case Assembler.CliType.reference:
                    return GetBitnessOpcode(bt, int32, int64);

                default:
                    throw new NotSupportedException();
            }
        }

        public static opcode GetBitnessOpcode(Assembler.Bitness bt, opcode bits32, opcode bits64)
        {
            switch (bt)
            {
                case Assembler.Bitness.Bits32:
                    return bits32;
                case Assembler.Bitness.Bits64:
                    return bits64;
                default:
                    throw new NotSupportedException();
            }
        }

        public static IEnumerable<libasm.OutputBlock> MethPrefixOverride(x86_64.x86_64_asm n, Assembler.MethodAttributes attrs)
        {
            return new List<libasm.OutputBlock> { new x86_64_Prefix { attrs = attrs } };
        }

        public class x86_64_Prefix : libasm.PrefixBlock
        {
            internal Assembler.MethodAttributes attrs;

            public override IList<byte> Code
            {
                get
                {
                    x86_64_Assembler.x86_64_TybelNode n = null;
                    switch(attrs.ass.GetBitness())
                    {
                        case Assembler.Bitness.Bits32:
                            n = new x86_64_Assembler.x86_64_TybelNode(null, FindOpcode(Opcodes[opcode.SUBL], "sub_rm32_imm8"), vara.MachineReg(x86_64_Assembler.Rsp), vara.Const(attrs.lv_stack_space));
                            break;
                       case Assembler.Bitness.Bits64:
                            n = new x86_64_Assembler.x86_64_TybelNode(null, FindOpcode(Opcodes[opcode.SUBQ], "sub_rm64_imm8"), vara.MachineReg(x86_64_Assembler.Rsp), vara.Const(attrs.lv_stack_space));
                            break;
                    }
                    List<byte> ret = new List<byte>();
                    IEnumerable<libasm.OutputBlock> obs = n.Assemble(attrs.ass, attrs);
                    foreach (libasm.OutputBlock ob in obs)
                    {
                        if (!(ob is libasm.CodeBlock))
                            throw new NotSupportedException();
                        ret.AddRange(((libasm.CodeBlock)ob).Code);
                    }
                    return ret;
                }
            }

            private x86_64_asm FindOpcode(List<x86_64_asm> list, string p)
            {
                foreach (x86_64_asm asm in list)
                {
                    if (asm.int_name == p)
                        return asm;
                }
                throw new NotImplementedException(p + " is not currently implemented");
            }
        }
    }
}
