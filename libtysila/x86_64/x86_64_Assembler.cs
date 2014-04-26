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

        private List<ThreeAddressCode.Op> exclude_from_twoaddress = new List<ThreeAddressCode.Op>
        {
            ThreeAddressCode.Op.rem_i,
            ThreeAddressCode.Op.rem_i4,
            ThreeAddressCode.Op.rem_i8,
            ThreeAddressCode.Op.rem_un_i,
            ThreeAddressCode.Op.rem_un_i4,
            ThreeAddressCode.Op.rem_un_i8,
            ThreeAddressCode.Op.div_i,
            ThreeAddressCode.Op.div_i4,
            ThreeAddressCode.Op.div_i8,
            ThreeAddressCode.Op.div_u,
            ThreeAddressCode.Op.div_u4,
            ThreeAddressCode.Op.div_u8
        };

        internal override void ArchSpecific(List<ThreeAddressCode> ir, List<cfg_node> nodes, AssemblerState state, MethodToCompile mtc)
        {
            foreach (cfg_node node in nodes)
            {
                ConvertToTwoAddress(node.optimized_ir, exclude_from_twoaddress);
                node.live_vars_at_end.Clear();
                node.live_vars_done = false;

                if (node.optimized_ir != null)
                {
                    for (int i = 0; i < node.optimized_ir.Count; i++)
                    {
                        ThreeAddressCode inst = node.optimized_ir[i];
                        // insert workarounds for instructions which we cannot encode

                        switch (inst.Operator)
                        {
                            case ThreeAddressCode.Op.conv_u4_r8:
                                node.optimized_ir[i] = new CallEx(inst.Result, new var[] { inst.Operand1 }, "_conv_u4_r8", callconv_conv_u4_r8);
                                break;
                            case ThreeAddressCode.Op.conv_u8_r8:
                            case ThreeAddressCode.Op.conv_u_r8:
                                node.optimized_ir[i] = new CallEx(inst.Result, new var[] { inst.Operand1 }, "_conv_u8_r8", callconv_conv_u8_r8);
                                break;
                            case ThreeAddressCode.Op.zeromem:
                                {
                                    switch ((int)inst.Operand2.constant_val)
                                    {
                                        case 1:
                                            node.optimized_ir[i] = new ThreeAddressCode(ThreeAddressCode.Op.poke_u1, var.Null, inst.Operand1, var.Const((byte)0));
                                            break;
                                        case 2:
                                            node.optimized_ir[i] = new ThreeAddressCode(ThreeAddressCode.Op.poke_u2, var.Null, inst.Operand1, var.Const((UInt16)0));
                                            break;
                                        case 4:
                                            node.optimized_ir[i] = new ThreeAddressCode(ThreeAddressCode.Op.poke_u4, var.Null, inst.Operand1, var.Const((UInt32)0));
                                            break;
                                        case 8:
                                            if (ia == IA.i586)
                                            {
                                                node.optimized_ir[i] = new ThreeAddressCode(ThreeAddressCode.Op.poke_u4, var.Null, inst.Operand1, var.Const((UInt32)0));
                                                var v2 = inst.Operand1;
                                                v2.constant_offset += 4;
                                                node.optimized_ir.Insert(i + 1, new ThreeAddressCode(ThreeAddressCode.Op.poke_u4, var.Null, v2, var.Const((UInt32)0)));
                                            }
                                            else
                                                node.optimized_ir[i] = new ThreeAddressCode(ThreeAddressCode.Op.poke_u8, var.Null, inst.Operand1, var.Const((UInt64)0));
                                            break;
                                    }
                                }
                                break;
                        }

                        //Un-nest instructions that reference [la/v1 + x] to a = la/v1, b = [a + x]
                        if ((inst.Operand1.type == var.var_type.ContentsOf || inst.Operand1.type == var.var_type.ContentsOfPlusConstant) &&
                            (inst.Operand1.base_var.v.type == var.var_type.LocalArg || inst.Operand1.base_var.v.type == var.var_type.LocalVar))
                        {
                            var intermediate = state.next_variable++;
                            node.optimized_ir.Insert(i, new ThreeAddressCode(ThreeAddressCode.Op.assign_i, intermediate, inst.Operand1.base_var.v, var.Null));
                            inst.Operand1.base_var.v = intermediate;
                            i++;
                        }
                        if ((inst.Operand2.type == var.var_type.ContentsOf || inst.Operand2.type == var.var_type.ContentsOfPlusConstant) &&
                            (inst.Operand2.base_var.v.type == var.var_type.LocalArg || inst.Operand2.base_var.v.type == var.var_type.LocalVar))
                        {
                            var intermediate = state.next_variable++;
                            node.optimized_ir.Insert(i, new ThreeAddressCode(ThreeAddressCode.Op.assign_i, intermediate, inst.Operand2.base_var.v, var.Null));
                            inst.Operand2.base_var.v = intermediate;
                            i++;
                        }
                        if ((inst.Result.type == var.var_type.ContentsOf || inst.Result.type == var.var_type.ContentsOfPlusConstant) &&
                            (inst.Result.base_var.v.type == var.var_type.LocalArg || inst.Result.base_var.v.type == var.var_type.LocalVar))
                        {
                            var intermediate = state.next_variable++;
                            node.optimized_ir.Insert(i, new ThreeAddressCode(ThreeAddressCode.Op.assign_i, intermediate, inst.Result.base_var.v, var.Null));
                            inst.Result.base_var.v = intermediate;
                            i++;
                        }
                        if (inst is CallEx)
                        {
                            CallEx ce = inst as CallEx;
                            for (int j = 0; j < ce.Var_Args.Length; j++)
                            {
                                if ((ce.Var_Args[j].type == var.var_type.ContentsOf || ce.Var_Args[j].type == var.var_type.ContentsOfPlusConstant) &&
                                    (ce.Var_Args[j].base_var.v.type == var.var_type.LocalArg || ce.Var_Args[j].base_var.v.type == var.var_type.LocalVar))
                                {
                                    var intermediate = state.next_variable++;
                                    node.optimized_ir.Insert(i, new ThreeAddressCode(ThreeAddressCode.Op.assign_i, intermediate, ce.Var_Args[j].base_var.v, var.Null));
                                    ce.Var_Args[j].base_var.v = intermediate;
                                    i++;
                                }
                            }
                        }
                    }
                }
            }

            // Repeat liveness analysis for new code
            LivenessAnalysis(InsertPseudoEnd(nodes, state, mtc), nodes);

            ir.Clear();
            foreach (cfg_node node in nodes)
            {
                if(node.optimized_ir != null)
                    ir.AddRange(node.optimized_ir);
            }
        }

        private void ConvertToTwoAddress(List<ThreeAddressCode> ir, List<ThreeAddressCode.Op> excludes)
        {
            int i = 0;
            if (ir == null)
                return;
            while (i < ir.Count)
            {
                ThreeAddressCode.OpType optype = ir[i].GetOpType();

                if (((optype == ThreeAddressCode.OpType.BinNumOp) || (optype == ThreeAddressCode.OpType.UnNumOp)) && (!excludes.Contains(ir[i].Operator)))
                {
                    // we need to convert a line of the form v1 = v2 op v3 to:
                    //  v1 = v2, v1 = v1 op v3

                    if (ir[i].Result.logical_var != ir[i].Operand1.logical_var)
                    {
                        var v1 = ir[i].Result.CloneVar();
                        var v2 = ir[i].Operand1.CloneVar();
                        var v3 = ir[i].Operand2.CloneVar();

                        ir.Insert(i, new ThreeAddressCode
                        {
                            Operator = GetAssignTac(ir[i].GetResultType()),
                            Result = v1,
                            Operand1 = v2,
                            Operand2 = var.Undefined
                        });

                        ir[i + 1].Operand1 = v1;
                        i++;
                    }
                }
                i++;
            }
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
            List<OutputBlock> blocks = new List<OutputBlock>();
            // Insert an x86_64 prolog

            int offset = 0;
            blocks.Insert(offset++, new CodeBlock(new byte[] { 0x55 }, new x86_64_Instruction { opcode = "push", Operand1 = Rbp }));
            if(ia == IA.x86_64)
                blocks.Insert(offset++, new CodeBlock(new byte[] { 0x48, 0x89, 0xe5 }, new x86_64_Instruction { opcode = "mov", Operand1 = Rsp, Operand2 = Rbp }));
            else
                blocks.Insert(offset++, new CodeBlock(new byte[] { 0x89, 0xe5 }, new x86_64_Instruction { opcode = "mov", Operand1 = Rsp, Operand2 = Rbp }));

            if (state.stack_space_used > 0)
            {
                if (FitsSByte(state.stack_space_used))
                {
                    blocks.Insert(offset++, new CodeBlock(EncOpcode(5, Rsp, 3, (ia == IA.x86_64), 0, 0x83), ToByteArraySignExtend(state.stack_space_used, 1),
                        new x86_64_Instruction[] { new x86_64_Instruction { opcode = "sub", Operand1 = Rsp, Operand2 = new const_location { c = state.stack_space_used } } }));
                }
                else if (FitsInt32(state.stack_space_used))
                {
                    blocks.Insert(offset++, new CodeBlock(EncOpcode(5, Rsp, 3, (ia == IA.x86_64), 0, 0x81), ToByteArraySignExtend(state.stack_space_used, 4),
                        new x86_64_Instruction[] { new x86_64_Instruction { opcode = "sub", Operand1 = Rsp, Operand2 = new const_location { c = state.stack_space_used } } }));
                }
                else
                    throw new NotSupportedException();
            }

            if ((ia == IA.i586) && (((x86_64_AssemblerState)state).i586_stored_ebp != null))
            {
                List<OutputBlock> temp = new List<OutputBlock>();
                // store previous ebx
                x86_64_assign(((x86_64_AssemblerState)state).i586_stored_ebp, Rbx, temp);
                foreach (OutputBlock t in temp)
                    blocks.Insert(offset++, t);
            }

            if (((x86_64_AssemblerState)state).isr)
            {
                // For an ISR we need to save all the registers (there is no pushad command on x86_64)
                blocks.Insert(offset++, new CodeBlock(EncAddOpcode(Rax, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = Rax }));
                blocks.Insert(offset++, new CodeBlock(EncAddOpcode(Rbx, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = Rbx }));
                blocks.Insert(offset++, new CodeBlock(EncAddOpcode(Rcx, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = Rcx }));
                blocks.Insert(offset++, new CodeBlock(EncAddOpcode(Rdx, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = Rdx }));
                blocks.Insert(offset++, new CodeBlock(EncAddOpcode(Rdi, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = Rdi }));
                blocks.Insert(offset++, new CodeBlock(EncAddOpcode(Rsi, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = Rsi }));

                if (ia == IA.x86_64)
                {
                    blocks.Insert(offset++, new CodeBlock(EncAddOpcode(R8, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = R8 }));
                    blocks.Insert(offset++, new CodeBlock(EncAddOpcode(R9, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = R9 }));
                    blocks.Insert(offset++, new CodeBlock(EncAddOpcode(R10, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = R10 }));
                    blocks.Insert(offset++, new CodeBlock(EncAddOpcode(R11, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = R11 }));
                    blocks.Insert(offset++, new CodeBlock(EncAddOpcode(R12, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = R12 }));
                    blocks.Insert(offset++, new CodeBlock(EncAddOpcode(R13, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = R13 }));
                    blocks.Insert(offset++, new CodeBlock(EncAddOpcode(R14, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = R14 }));
                    blocks.Insert(offset++, new CodeBlock(EncAddOpcode(R15, false, 0x50), new x86_64_Instruction { opcode = "push", Operand1 = R15 }));
                }
            }

            if (state.syscall)
            {
                // Syscalls on the x86_64 platform are currently non-interruptible
                blocks.Insert(offset++, new CodeBlock(new byte[] { 0x9c }, new x86_64_Instruction { opcode = "pushfq" }));
                blocks.Insert(offset++, new CodeBlock(new byte[] { 0xfa }, new x86_64_Instruction { opcode = "cli" }));
            }
            else if (state.uninterruptible_method)
            {
                blocks.Insert(offset++, new CodeBlock(new byte[] { 0x9c }, new x86_64_Instruction { opcode = "pushfq" }));
                blocks.Insert(offset++, new CodeBlock(new byte[] { 0xfa }, new x86_64_Instruction { opcode = "cli" }));
            }

            int stack_space_to_clear = state.stack_space_used;
            if (Options.EnableRTTI)
                stack_space_to_clear -= GetSizeOfPointer();
            if ((ia == IA.i586) && (((x86_64_AssemblerState)state).i586_stored_ebp != null))
                stack_space_to_clear -= 4;

            if (stack_space_to_clear > 0)
            {
                /* Initialise the local variables 
                 * 
                 * Mov rcx, stack space / 8
                 * clear rax
                 * lea rdi, [rbp - stack_space]
                 * rep stosq
                 */

                // If the arguments are passed in registers we may need to save rdi and rcx
                bool save_rdi = false;
                bool save_rcx = false;

                if (state.cc != null)
                {
                    foreach (CallConv.ArgumentLocation arg in state.cc.Arguments)
                    {
                        if (arg.ValueLocation.Equals(Rdi))
                            save_rdi = true;
                        if (arg.ValueLocation.Equals(Rcx))
                            save_rcx = true;
                    }
                }

                if (save_rdi)
                    blocks.Insert(offset++, new CodeBlock(SaveLocation(Rdi)));
                if (save_rcx)
                    blocks.Insert(offset++, new CodeBlock(SaveLocation(Rcx)));

                if (ia == IA.x86_64)
                {
                    blocks.Insert(offset++, new CodeBlock(EncOpcode(0, Rcx, 3, true, 0, 0xc7), ToByteArraySignExtend((stack_space_to_clear / 8), 4), new CodeBlock.CompiledInstruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = new const_location { c = stack_space_to_clear / 8 } } }));
                    blocks.Insert(offset++, new CodeBlock(EncOpcode(Rax, Rax, 3, true, 0, 0x31), new x86_64_Instruction { opcode = "xor", Operand1 = Rax, Operand2 = Rax }));
                    blocks.Insert(offset++, new CodeBlock(EncOpcode(Rdi, new hardware_contentsof { base_loc = Rbp, const_offset = -state.stack_space_used }, 0, true, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rdi, Operand2 = new hardware_contentsof { base_loc = Rbp, const_offset = -state.stack_space_used } }));
                    blocks.Insert(offset++, new CodeBlock(new byte[] { 0xf3, 0x48, 0xab }, new x86_64_Instruction { opcode = "rep stosq" }));
                }
                else
                {
                    blocks.Insert(offset++, new CodeBlock(EncOpcode(0, Rcx, 3, false, 0, 0xc7), ToByteArraySignExtend((stack_space_to_clear / 4), 4), new CodeBlock.CompiledInstruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = new const_location { c = stack_space_to_clear / 4 } } }));
                    blocks.Insert(offset++, new CodeBlock(EncOpcode(Rax, Rax, 3, false, 0, 0x31), new x86_64_Instruction { opcode = "xor", Operand1 = Rax, Operand2 = Rax }));
                    blocks.Insert(offset++, new CodeBlock(EncOpcode(Rdi, new hardware_contentsof { base_loc = Rbp, const_offset = -state.stack_space_used }, 0, false, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rdi, Operand2 = new hardware_contentsof { base_loc = Rbp, const_offset = -state.stack_space_used } }));
                    blocks.Insert(offset++, new CodeBlock(new byte[] { 0xf3, 0xab }, new x86_64_Instruction { opcode = "rep stosd" }));
                }

                if (save_rcx)
                    blocks.Insert(offset++, new CodeBlock(RestoreLocation(Rcx)));
                if (save_rdi)
                    blocks.Insert(offset++, new CodeBlock(RestoreLocation(Rdi)));
            }

            if ((ia == IA.i586) && (((x86_64_AssemblerState)state).i586_stored_ebp != null))
            {
                // set up the GOT pointer

                /* code is:
                    * 
                    * e8 00 00 00 00     call 0 (i.e. call start of next instruction)
                    * 5b                 pop ebx (ebx = address of current function)
                    * 81 c3 03 00 00 00  add ebx, [_GLOBAL_OFFSET_TABLE_ + 3] <- ebx = absolute GOT address
                    */
                blocks.Insert(offset++, new CodeBlock(new byte[] { 0xe8, 0x00, 0x00, 0x00, 0x00, 0x5b, 0x81, 0xc3 }));
                switch (OType)
                {
                    case OutputType.i586_elf:
                        blocks.Insert(offset++, new RelocationBlock { RelType = x86_64.x86_64_elf32.R_386_GOTPC, Target = "_GLOBAL_OFFSET_TABLE_", Value = 0x03, Size = 4 });
                        break;
                    case OutputType.i586_elf64:
                        blocks.Insert(offset++, new RelocationBlock { RelType = x86_64.x86_64_elf64.R_X86_64_GOTPC32, Target = "_GLOBAL_OFFSET_TABLE_", Value = 0x03, Size = 4 });
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            return blocks;
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

        public override util.Set<hardware_location> MachineRegistersForDataType(CliType dt)
        {
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
    }
}
