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

namespace tyasm
{
    partial class x86_Assembler
    {
        class Reg
        {
            public int id;
            public bool is_high = false;
        }

        const string R8 = "gpr8";
        const string R16 = "gpr16";
        const string R32 = "gpr32";
        const string R64 = "gpr64";
        const string p8 = "[gpr8]";
        const string p16 = "[gpr16]";
        const string p32 = "[gpr32]";
        const string p64 = "[gpr64]";
        const string Creg = "creg";

        static List<string> R32s = new List<string> { "eax", "ebx", "ecx", "edx", "esi", "edi", "esp", "ebp" };
        static List<string> R64s = new List<string> { "rax", "rbx", "rcx", "rdx", "rsi", "rdi", "rsp", "rbp" };

        static List<string> Cregs = new List<string> { "cr0", "cr1", "cr2", "cr3", "cr4", "cr5", "cr6", "cr7", "cr8" };

        Dictionary<string, Reg> regs = new Dictionary<string, Reg>();

        static ParameterConstraint CR8 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R8 } };
        static ParameterConstraint CR16 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R16 } };
        static ParameterConstraint CR32 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R32 } };
        static ParameterConstraint CR64 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R64 } };
        static ParameterConstraint CR3264 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R32, R64 } };
        static ParameterConstraint Cp8 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { p8 } };
        static ParameterConstraint Cp16 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { p16 } };
        static ParameterConstraint Cp32 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { p32 } };
        static ParameterConstraint Cp64 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { p64 } };
        static ParameterConstraint Cp3264 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { p32, p64 } };
        static ParameterConstraint CRp8 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R8, p8 } };
        static ParameterConstraint CRp16 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R16, p16 } };
        static ParameterConstraint CRp32 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R32, p32 } };
        static ParameterConstraint CRp64 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R64, p64 } };
        static ParameterConstraint CRp3264 = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { R32, p32, R64, p64 } };
        static ParameterConstraint CCreg = new ParameterConstraint { Type = ParameterConstraint.ConstraintType.AnyOfType, Value = new string[] { Creg } };

        static string[] NoPrefixes = new string[] {};
        static List<ParameterConstraint> NoParams = new List<ParameterConstraint>();

        protected override void InitOpcodes()
        {
            opcodes["mov"] = new List<OpcodeImplementation> {
                new OpcodeImplementation { AllowedPrefixes = NoPrefixes, emitter = mov_rm3264_r3264,
                    ParamConstraints = new List<ParameterConstraint> { CRp3264, CR3264 } },
                new OpcodeImplementation { AllowedPrefixes = NoPrefixes, emitter = mov_r3264_rm3264,
                    ParamConstraints = new List<ParameterConstraint> { CR3264, CRp3264 } },
                new OpcodeImplementation { AllowedPrefixes = NoPrefixes, emitter = mov_r3264_creg,
                    ParamConstraints = new List<ParameterConstraint> { CR3264, CCreg }},
                new OpcodeImplementation { AllowedPrefixes = NoPrefixes, emitter = mov_creg_r3264,
                    ParamConstraints = new List<ParameterConstraint> { CCreg, CR3264 }},
            };

            opcodes["ret"] = new List<OpcodeImplementation> {
                new OpcodeImplementation { AllowedPrefixes = NoPrefixes, emitter = ret,
                    ParamConstraints = NoParams }
            };

            opcodes["retf"] = new List<OpcodeImplementation> {
                new OpcodeImplementation { AllowedPrefixes = NoPrefixes, emitter = retf,
                    ParamConstraints = NoParams }
            };
        }

        protected override bool MatchConstraint(ParameterConstraint constraint, Location param)
        {
            // Match various param types
            string param_as_any_constraint = null;
            string param_as_spec_constraint = null;

            switch (param.ModifierType)
            {
                case Location.LocationModifierType.ValueOf:
                    if ((param.AType == Location.LocationType.Register) && (param.NumOp == Location.NumOpType.None))
                    {
                        string r = param.A as string;
                        if (R32s.Contains(r))
                            param_as_any_constraint = R32;
                        else if (R64s.Contains(r))
                            param_as_any_constraint = R64;
                        else if (Cregs.Contains(r))
                            param_as_any_constraint = Creg;
                        param_as_spec_constraint = r;
                    }
                    break;

                case Location.LocationModifierType.ContentsOf:
                    if ((param.AType == Location.LocationType.Register) && ((param.NumOp == Location.NumOpType.None) ||
                        (((param.NumOp == Location.NumOpType.Plus) || (param.NumOp == Location.NumOpType.Minus) &&
                        ((param.BType == Location.LocationType.Number) || (param.BType == Location.LocationType.Label))))))
                    {
                        string r = param.A as string;
                        if (R32s.Contains(r))
                            param_as_any_constraint = p32;
                        else if (R64s.Contains(r))
                            param_as_any_constraint = p64;
                        param_as_spec_constraint = "[" + r + "]";
                    }
                    break;
            }

            if (constraint.Type == ParameterConstraint.ConstraintType.AnyOfType)
            {
                if (constraint.Value.Contains(param_as_any_constraint))
                    return true;
            }
            if (constraint.Type == ParameterConstraint.ConstraintType.Specific)
            {
                if (constraint.Value.Contains(param_as_spec_constraint))
                    return true;
            }

            return base.MatchConstraint(constraint, param);
        }

        void AppendRex(List<OutputBlock> ret, byte rex)
        {
            if (rex != 0)
                ret.Add(new CodeBlock { Code = new byte[] { rex } });
        }

        byte GenerateRex(bool rex_w, Reg r, Reg rm)
        {
            return GenerateRex(rex_w, r.is_high, false, rm.is_high);
        }

        byte GenerateRex(bool rex_w, bool rex_r, bool rex_x, bool rex_b)
        {
            byte ret = 0;
            if (rex_w)
                ret |= 0x48;
            if (rex_r)
                ret |= 0x44;
            if (rex_x)
                ret |= 0x42;
            if (rex_b)
                ret |= 0x41;
            return ret;
        }

        void AppendModRM(List<OutputBlock> ret, Reg r, Reg rm, AssemblerState state)
        {
            AppendModRM(ret, (byte)r.id, (byte)rm.id, 3, 0, state);
        }

        void AppendModRM(List<OutputBlock> ret, byte r, byte rm, byte mod, long disp, AssemblerState state)
        {
            bool need_sib = false;
            bool need_disp32 = false;
            bool need_disp8 = false;
            bool absolute_disp = false;
            bool is_ptr = true;

            // Decide on addressing mode
            switch (state.cur_bitness)
            {
                case binary_library.Bitness.Bits32:
                case binary_library.Bitness.Bits64:
                    // OK
                    break;
                case binary_library.Bitness.Bits16:
                    // Not done yet
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException();
            }

            if (mod == 3)
                is_ptr = false;
            if ((rm == regs["rsp"].id) && (mod != 3))
                need_sib = true;
            if((rm == regs["rbp"].id) && (mod == 0))
            {
                absolute_disp = true;
                need_disp32 = true;
            }

            if (disp != 0)
            {
                if ((disp > Int32.MaxValue) || (disp < Int32.MinValue))
                    throw new NotSupportedException();
                else if ((disp > SByte.MaxValue) || (disp < SByte.MinValue))
                    need_disp32 = true;
                else
                    need_disp8 = true;
            }

            if (absolute_disp)
                mod = 0;
            else if (need_disp32)
                mod = 2;
            else if (need_disp8)
                mod = 1;
            else if (is_ptr)
                mod = 0;
            else
                mod = 3;

            // Encode the ModRM byte
            byte modrm = (byte)((byte)(rm & 0x7) | (byte)((r & 0x7) << 3) | (byte)((mod & 0x3) << 6));
            ret.Add(new CodeBlock { Code = new byte[] { modrm } });

            // Encode the SIB
            if (need_sib)
                throw new NotImplementedException();

            // Encode the displacement
            if (need_disp32 | need_disp8)
            {
                byte[] val = BitConverter.GetBytes(disp);

                if (need_disp32)
                    ret.Add(new CodeBlock { Code = new byte[] { val[0], val[1], val[2], val[3] } });
                else
                    ret.Add(new CodeBlock { Code = new byte[] { val[0] } });
            }
        }

        bool mov_rm3264_r3264(List<OutputBlock> ret, AsmParser.ParseOutput op, List<AsmParser.ParseOutput> prefixes,
            List<AsmParser.ParseOutput> parameters, AssemblerState state)
        {
            return true;
        }

        bool mov_r3264_rm3264(List<OutputBlock> ret, AsmParser.ParseOutput op, List<AsmParser.ParseOutput> prefixes,
            List<AsmParser.ParseOutput> parameters, AssemblerState state)
        {
            return true;
        }

        bool mov_r3264_creg(List<OutputBlock> ret, AsmParser.ParseOutput op, List<AsmParser.ParseOutput> prefixes,
            List<AsmParser.ParseOutput> parameters, AssemblerState state)
        {
            Reg r = regs[parameters[1].Name];
            Reg rm = regs[parameters[0].Name];
            AppendRex(ret, GenerateRex(false, r, rm));
            ret.Add(new CodeBlock { Code = new byte[] { 0x0f, 0x20 } });
            AppendModRM(ret, r, rm, state);
            return true;
        }

        bool mov_creg_r3264(List<OutputBlock> ret, AsmParser.ParseOutput op, List<AsmParser.ParseOutput> prefixes,
            List<AsmParser.ParseOutput> parameters, AssemblerState state)
        {
            return true;
        }

        bool ret(List<OutputBlock> ret, AsmParser.ParseOutput op, List<AsmParser.ParseOutput> prefixes,
            List<AsmParser.ParseOutput> parameters, AssemblerState state)
        {
            ret.Add(new CodeBlock { Code = new byte[] { 0xc3 } });
            return true;
        }

        bool retf(List<OutputBlock> ret, AsmParser.ParseOutput op, List<AsmParser.ParseOutput> prefixes,
            List<AsmParser.ParseOutput> parameters, AssemblerState state)
        {
            ret.Add(new CodeBlock { Code = new byte[] { 0xcb } });
            return true;
        }
    }
}
