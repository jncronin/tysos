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

namespace tydisasm.x86_64
{
    public class x86_64s_disasm : x86_64_disasm { }
    public class i586_disasm : x86_64_disasm
    {
        public i586_disasm()
        {
            bitness = 32;
        }
    }

    public partial class x86_64_disasm : tydisasm
    {
        const byte rex_b = 0x1;
        const byte rex_x = 0x2;
        const byte rex_r = 0x4;
        const byte rex_w = 0x8;

        protected int bitness = 64;

        public override int Bitness
        {
            get { return bitness; }
        }

        public x86_64_disasm()
        {
            init_regs();
            init_opcodes();
        }

        public override line GetNextLine(ByteProvider bp)
        {
            x86_64_line l = new x86_64_line();
            List<byte> os = new List<byte>();
            l.offset_start = bp.Offset;

            byte rex = 0x0;
            bool has_prefix_66 = false;
            byte b = bp.GetNextByte();
            os.Add(b);
            
            // read any prefixes
            bool reading_prefixes = true;
            List<ulong> prefixes = new List<ulong>();
            while (reading_prefixes)
            {
                switch (b)
                {
                    case 0xf0:
                    case 0xf2:
                    case 0xf3:
                    case 0x2e:
                    case 0x36:
                    case 0x3e:
                    case 0x26:
                    case 0x64:
                    case 0x65:
                    case 0x66:
                    case 0x67:
                        prefixes.Add(b);
                        if (b == 0x66)
                            has_prefix_66 = true;
                        b = bp.GetNextByte();
                        os.Add(b);
                        break;
                    default:
                        reading_prefixes = false;
                        break;
                }
            }
            l.prefixes = prefixes.ToArray();

            // read any rex prefix
            if ((b >= 0x40) && (b <= 0x4f))
            {
                prefixes.Add(b);
                l.prefixes = prefixes.ToArray();
                rex = b;
                b = bp.GetNextByte();
                os.Add(b);
            }

            // read the opcode
            if (b == 0x0f)
            {
                // two byte opcode

                /* read any rex prefix (in multi-byte opcodes, the rex
                 * prefix comes after the 0x0f byte)
                 */
                byte b2 = bp.GetNextByte();
                os.Add(b2);
                if ((b2 >= 0x40) && (b2 <= 0x4f))
                {
                    prefixes.Add(b2);
                    l.prefixes = prefixes.ToArray();
                    rex = b;
                    b2 = bp.GetNextByte();
                    os.Add(b2);
                }

                if ((b2 == 0x38) || (b2 == 0x3a))
                {
                    byte b3 = bp.GetNextByte();
                    os.Add(b3);
                    l.opcode = (ulong)((b << 16) + (b2 << 8) + b3);
                }
                else
                    l.opcode = (ulong)((b << 8) + b2);
            }
            else l.opcode = (ulong)b;

            opcode o = null;
            if (opcodes.ContainsKey(l.opcode))
                o = opcodes[l.opcode];

            if (o == null)
            {
                l.offset_end = bp.Offset;
                return l;
            }
            else
            {
                byte modrm;
                byte sib = 0;
                long disp = 0;
                ulong imm = 0;

                if (o.has_rm)
                {
                    // read the rm byte
                    modrm = bp.GetNextByte();
                    os.Add(modrm);

                    byte mod = (byte)(modrm >> 6);
                    byte rm = (byte)(modrm & 7);
                    byte reg = (byte)((modrm >> 3) & 7);

                    if (o.reinterpret_after_r)
                    {
                        l.opcode = (((ulong)reg) << 24) + l.opcode + 0x10000000;
                        if (opcodes.ContainsKey(l.opcode))
                            o = opcodes[l.opcode];
                        else
                        {
                            l.offset_end = bp.Offset;
                            return l;
                        }
                    }

                    // decide if we need an sib byte
                    if ((mod != 3) && (rm == 4))
                    {
                        sib = bp.GetNextByte();
                        os.Add(sib);
                    }

                    // decide if we need a displacement
                    if (((mod == 0) && (rm == 5)) || (mod == 2))
                    {
                        // 4 byte displacement
                        byte d1 = bp.GetNextByte();
                        os.Add(d1);
                        byte d2 = bp.GetNextByte();
                        os.Add(d2);
                        byte d3 = bp.GetNextByte();
                        os.Add(d3);
                        byte d4 = bp.GetNextByte();
                        os.Add(d4);

                        byte[] dval = new byte[] { d1, d2, d3, d4 };
                        disp = BitConverter.ToInt32(dval, 0);

                        //disp = (ulong)d1 + (((ulong)d2) << 8) + (((ulong)d3) << 16) + (((ulong)d4) << 24);
                    }
                    else if (mod == 1)
                    {
                        // 1 byte displacement
                        byte d1 = bp.GetNextByte();
                        os.Add(d1);

                        byte extend = 0;
                        if ((d1 & 0x80) == 0x80)
                            extend = 0xff;

                        byte[] dval = new byte[] { d1, extend, extend, extend };
                        disp = BitConverter.ToInt32(dval, 0);
                    }

                    // load a immediate value if necessary
                    int imm_length = o.immediate_length;
                    if ((imm_length == 4) && (o.immediate_extends_on_rexw) && ((rex & 0x08) == 0x08))
                        imm_length = 8;
                    for (int i = 0; i < imm_length; i++)
                    {
                        byte cur_b = bp.GetNextByte();
                        os.Add(cur_b);
                        imm += ((ulong)cur_b) << (i * 8);
                    }

                    // work out the opcodes
                    List<location> args = new List<location>();
                    foreach (opcode.operand_source osrc in o.operand_sources)
                    {
                        switch (osrc.type)
                        {
                            case opcode.operand_source.src_type.Fixed:
                                if (osrc.Fixed_Location.type == location.location_type.Register)
                                {
                                    ulong reg_no = osrc.Fixed_Location.reg_no;
                                    reg_no = reg_no % 8;

                                    if (osrc.extends_on_rexb && ((rex & 0x01) == 0x01))
                                        reg_no += reg_nos["r8"];
                                    else if ((rex & 0x08) == 0x08)
                                        reg_no += reg_nos["rax"];
                                    else
                                        reg_no = osrc.Fixed_Location.reg_no;
                                    args.Add(new location { type = location.location_type.Register, reg_no = reg_no });
                                }
                                else
                                    args.Add(osrc.Fixed_Location);
                                break;
                            case opcode.operand_source.src_type.Imm:
                                args.Add(new location { type = location.location_type.Immediate, immediate = imm, is_pc_relative = osrc.is_pc_relative });
                                break;
                            case opcode.operand_source.src_type.ModRM_Reg:
                                {
                                    ulong base_reg = 0;
                                    switch (osrc.length)
                                    {
                                        case opcode.operand_source.reg_length.r8:
                                            base_reg = reg_nos["al"];
                                            break;
                                        case opcode.operand_source.reg_length.r16:
                                            base_reg = reg_nos["ax"];

                                            break;
                                        case opcode.operand_source.reg_length.r32:
                                            base_reg = reg_nos["eax"];
                                            if (has_prefix_66)
                                                base_reg = reg_nos["ax"];
                                            else
                                            {
                                                if (((rex & rex_w) == rex_w) && osrc.extends_on_rexw)
                                                    base_reg = reg_nos["rax"];
                                                if ((rex & 0x4) == 0x4)
                                                    base_reg = reg_nos["r8"];
                                            }
                                            break;
                                        case opcode.operand_source.reg_length.r64:
                                            base_reg = reg_nos["rax"];
                                            if ((rex & 0x4) == 0x4)
                                                base_reg = reg_nos["r8"];
                                            break;
                                        case opcode.operand_source.reg_length.mm:
                                            base_reg = reg_nos["mm0"];
                                            break;
                                        case opcode.operand_source.reg_length.xmm:
                                            base_reg = reg_nos["xmm0"];
                                            if ((rex & 0x4) == 0x4)
                                                base_reg = reg_nos["xmm8"];
                                            break;
                                        case opcode.operand_source.reg_length.cr:
                                            base_reg = reg_nos["cr0"];
                                            break;
                                    }

                                    args.Add(new location { type = location.location_type.Register, reg_no = base_reg + (ulong)reg });
                                }
                                break;
                            case opcode.operand_source.src_type.ModRM_RM:
                                {
                                    if (mod == 3)
                                    {
                                        ulong base_reg = 0;

                                        switch (osrc.length)
                                        {
                                            case opcode.operand_source.reg_length.r8:
                                                base_reg = reg_nos["al"];
                                                break;
                                            case opcode.operand_source.reg_length.r16:
                                                base_reg = reg_nos["ax"];

                                                break;
                                            case opcode.operand_source.reg_length.r32:
                                                base_reg = reg_nos["eax"];
                                                if (has_prefix_66)
                                                    base_reg = reg_nos["ax"];
                                                else
                                                {
                                                    if (((rex & rex_w) == rex_w) && osrc.extends_on_rexw)
                                                        base_reg = reg_nos["rax"];
                                                    if ((rex & 0x1) == 0x1)
                                                        base_reg = reg_nos["r8"];
                                                }
                                                break;
                                            case opcode.operand_source.reg_length.r64:
                                                base_reg = reg_nos["rax"];
                                                if ((rex & 0x1) == 0x1)
                                                    base_reg = reg_nos["r8"];
                                                break;
                                            case opcode.operand_source.reg_length.mm:
                                                base_reg = reg_nos["mm0"];
                                                break;
                                            case opcode.operand_source.reg_length.xmm:
                                                base_reg = reg_nos["xmm0"];
                                                if ((rex & 0x1) == 0x1)
                                                    base_reg = reg_nos["xmm8"];
                                                break;
                                            case opcode.operand_source.reg_length.cr:
                                                base_reg = reg_nos["cr0"];
                                                break;
                                        }

                                        args.Add(new location { type = location.location_type.Register, reg_no = base_reg + (ulong)rm });
                                    }
                                    else
                                    {
                                        ulong base_reg = reg_nos["rax"];

                                        switch (rm)
                                        {
                                            case 0:
                                                base_reg = reg_nos["rax"];
                                                break;
                                            case 1:
                                                base_reg = reg_nos["rcx"];
                                                break;
                                            case 2:
                                                base_reg = reg_nos["rdx"];
                                                break;
                                            case 3:
                                                base_reg = reg_nos["rbx"];
                                                break;
                                            case 4:
                                                {
                                                    // interpret sib byte
                                                    byte s_scale = (byte)(sib >> 6);
                                                    byte s_base = (byte)(sib & 7);
                                                    byte s_index = (byte)((sib >> 3) & 7);

                                                    List<location> sib_args = new List<location>();
                                                    bool need_plus = false;

                                                    // the special combination of base = 5, mod = 0, rex_b = 0 means no base
                                                    if (!((s_base == 5) && (mod == 0) && ((rex & rex_b) == rex_b)))
                                                    {
                                                        // otherwise we have a base
                                                        ulong s_base_base_reg = reg_nos["rax"];
                                                        if ((rex & rex_b) == rex_b)
                                                            s_base_base_reg = reg_nos["r8"];

                                                        need_plus = true;
                                                        sib_args.Add(new location { type = location.location_type.Register, reg_no = s_base_base_reg + s_base });
                                                    }

                                                    // if s_index != 4, then we have a index register
                                                    if (!((s_index == 4) && ((rex & rex_x) != rex_x)))
                                                    {
                                                        location.scale_func scale = location.scale_func.None;
                                                        if (need_plus)
                                                            scale = location.scale_func.Plus;

                                                        ulong s_index_base_reg = reg_nos["rax"];
                                                        if ((rex & rex_x) == rex_x)
                                                            s_index_base_reg = reg_nos["r8"];

                                                        need_plus = true;
                                                        sib_args.Add(new location { type = location.location_type.Register, reg_no = s_index_base_reg + s_index, scale = scale });

                                                        if (s_scale == 1)
                                                            sib_args.Add(new location { type = location.location_type.Const, immediate = 2, scale = location.scale_func.Multiply });
                                                        else if (s_scale == 2)
                                                            sib_args.Add(new location { type = location.location_type.Const, immediate = 4, scale = location.scale_func.Multiply });
                                                        else if (s_scale == 3)
                                                            sib_args.Add(new location { type = location.location_type.Const, immediate = 8, scale = location.scale_func.Multiply });
                                                    }

                                                    if ((mod == 1) || (mod == 2) || ((mod == 0) && (s_base == 5) && ((rex & rex_b) == rex_b)))
                                                    {
                                                        // add displacement
                                                        location.scale_func scale = location.scale_func.None;
                                                        if (need_plus)
                                                            scale = ((disp >= 0) ? location.scale_func.Plus : location.scale_func.Minus);

                                                        sib_args.Add(new location { type = location.location_type.Immediate, immediate = ((disp >= 0) ? (ulong)disp : (ulong)(-disp)), scale = scale });
                                                    }

                                                    args.Add(new location { type = location.location_type.ContentsOf, args = sib_args.ToArray() });
                                                    continue;
                                                }
                                            case 5:
                                                {
                                                    if (mod == 0)
                                                    {
                                                        args.Add(new location { type = location.location_type.ContentsOf, args = new location[] { new location { type = location.location_type.Register, reg_no = reg_nos["rip"] }, new location { type = location.location_type.Immediate, scale = ((disp >= 0) ? location.scale_func.Plus : location.scale_func.Minus), immediate = ((disp >= 0) ? (ulong)disp : (ulong)(-disp)) } } });
                                                        continue;
                                                    }
                                                    else
                                                        base_reg = reg_nos["rbp"];
                                                }
                                                break;
                                            case 6:
                                                base_reg = reg_nos["rsi"];
                                                break;
                                            case 7:
                                                base_reg = reg_nos["rdi"];
                                                break;
                                        }
                                        if ((rex & 0x1) == 0x1)
                                            base_reg = base_reg - reg_nos["rax"] + reg_nos["r8"];

                                        location loc = new location { type = location.location_type.ContentsOf, args = new location[] { new location { type = location.location_type.Register, reg_no = base_reg } } };
                                        if ((mod == 1) || (mod == 2))
                                            loc.args = new location[] { loc.args[0], new location { type = location.location_type.Immediate, scale = ((disp >= 0) ? location.scale_func.Plus : location.scale_func.Minus), immediate = ((disp >= 0) ? (ulong)disp : (ulong)(-disp)) } };
                                        args.Add(loc);
                                    }

                                }
                                break;
                        }
                    }
                    l.arguments = args.ToArray();
                }
                else
                {
                    // no mod_rm

                    // load a immediate value if necessary
                    int imm_length = o.immediate_length;
                    if ((imm_length == 4) && (o.immediate_extends_on_rexw) && ((rex & 0x08) == 0x08))
                        imm_length = 8;
                    for (int i = 0; i < imm_length; i++)
                    {
                        byte cur_b = bp.GetNextByte();
                        os.Add(cur_b);
                        imm += ((ulong)cur_b) << (i * 8);
                    }

                    List<location> args = new List<location>();

                    foreach (opcode.operand_source osrc in o.operand_sources)
                    {
                        switch (osrc.type)
                        {
                            case opcode.operand_source.src_type.Fixed:
                                if (osrc.Fixed_Location.type == location.location_type.Register)
                                {
                                    ulong reg_no = osrc.Fixed_Location.reg_no;
                                    reg_no = reg_no % 8;

                                    if (osrc.extends_on_rexb && ((rex & 0x01) == 0x01))
                                        reg_no += reg_nos["r8"];
                                    else if ((rex & 0x08) == 0x08)
                                        reg_no += reg_nos["rax"];
                                    else
                                        reg_no = osrc.Fixed_Location.reg_no;
                                    args.Add(new location { type = location.location_type.Register, reg_no = reg_no });
                                }
                                else
                                    args.Add(osrc.Fixed_Location);
                                break;
                            case opcode.operand_source.src_type.Imm:
                                args.Add(new location { type = location.location_type.Immediate, immediate = imm, is_pc_relative = osrc.is_pc_relative });
                                break;
                        }
                    }

                    l.arguments = args.ToArray();
                }

            }

            l.o = o;
            l.opcodes = os.ToArray();
            l.bp = bp;
            l.offset_end = bp.Offset;

            return l;
        }

        class x86_64_line : line
        {
            public override string ToDisassembledString(tydisasm disasm)
            {
                StringBuilder sb = new StringBuilder();
                if (o == null)
                {
                    ulong reg = opcode >> 24;
                    opcode = opcode & 0xffffff;

                    sb.Append("Unknown opcode: ");

                    foreach (ulong prefix in prefixes)
                    {
                        sb.Append(prefix.ToString("X2"));
                        sb.Append(" ");
                    }

                    if ((opcode & 0xff0000) != 0)
                    {
                        sb.Append(((opcode >> 16) & 0xff).ToString("X2"));
                        sb.Append(" ");
                    }
                    if ((opcode & 0xff00) != 0)
                    {
                        sb.Append(((opcode >> 8) & 0xff).ToString("X2"));
                        sb.Append(" ");
                    }
                    sb.Append((opcode & 0xff).ToString("X2"));

                    if ((reg & 0x10) == 0x10)
                    {
                        sb.Append(" /");
                        sb.Append(reg.ToString("X"));
                    }

                    return sb.ToString();
                }

                string name = o.name;

                foreach (ulong p in prefixes)
                {
                    if (p == 0xf0)
                        sb.Append("lock ");
                    else if (p == 0xf2)
                    {
                        if (o.f2_rename == null)
                            sb.Append("repne ");
                        else
                            name = o.f2_rename;
                    }
                    else if (p == 0xf3)
                        sb.Append("rep ");
                }

                sb.Append(o.name);

                for (int i = 0; i < arguments.Length; i++)
                {
                    if (i == 0)
                        sb.Append(" ");
                    else
                        sb.Append(", ");
                    sb.Append(arguments[i].ToDisassembledString(this, disasm));
                }
                return sb.ToString();
            }

            public opcode o = null;
        }

        class opcode
        {
            // define an opcode
            public string name;

            public class operand_source
            {
                public enum src_type { Fixed, ModRM_Reg, ModRM_RM, Imm };
                public enum reg_length { r8, r16, r32, r64, mm, xmm, cr };
                public src_type type;
                public reg_length length = reg_length.r32;
                public location Fixed_Location;
                public bool extends_on_rexb = false;
                public bool extends_on_rexw = true;
                public bool is_pc_relative = false;
            }

            public List<operand_source> operand_sources;
            public bool reinterpret_after_r = false;
            public bool has_rm = false;
            public string f2_rename = null;
            public int immediate_length = 0;
            public bool immediate_extends_on_rexw = false;
        }

        Dictionary<ulong, opcode> opcodes = new Dictionary<ulong, opcode>();
        List<string> reg_names = new List<string>();
        Dictionary<string, ulong> reg_nos = new Dictionary<string, ulong>();
        public ulong GetRegisterNumber(string name) { return reg_nos[name]; }

        void init_regs()
        {
            reg_names.Add("al");
            reg_names.Add("cl");
            reg_names.Add("dl");
            reg_names.Add("bl");
            reg_names.Add("ah");
            reg_names.Add("ch");
            reg_names.Add("dh");
            reg_names.Add("bh");

            reg_names.Add("ax");
            reg_names.Add("cx");
            reg_names.Add("dx");
            reg_names.Add("bx");
            reg_names.Add("sp");
            reg_names.Add("bp");
            reg_names.Add("si");
            reg_names.Add("di");

            reg_names.Add("eax");
            reg_names.Add("ecx");
            reg_names.Add("edx");
            reg_names.Add("ebx");
            reg_names.Add("esp");
            reg_names.Add("ebp");
            reg_names.Add("esi");
            reg_names.Add("edi");

            reg_names.Add("rax");
            reg_names.Add("rcx");
            reg_names.Add("rdx");
            reg_names.Add("rbx");
            reg_names.Add("rsp");
            reg_names.Add("rbp");
            reg_names.Add("rsi");
            reg_names.Add("rdi");

            reg_names.Add("r8");
            reg_names.Add("r9");
            reg_names.Add("r10");
            reg_names.Add("r11");
            reg_names.Add("r12");
            reg_names.Add("r13");
            reg_names.Add("r14");
            reg_names.Add("r15");

            reg_names.Add("mm0");
            reg_names.Add("mm1");
            reg_names.Add("mm2");
            reg_names.Add("mm3");
            reg_names.Add("mm4");
            reg_names.Add("mm5");
            reg_names.Add("mm6");
            reg_names.Add("mm7");

            reg_names.Add("xmm0");
            reg_names.Add("xmm1");
            reg_names.Add("xmm2");
            reg_names.Add("xmm3");
            reg_names.Add("xmm4");
            reg_names.Add("xmm5");
            reg_names.Add("xmm6");
            reg_names.Add("xmm7");

            reg_names.Add("xmm8");
            reg_names.Add("xmm9");
            reg_names.Add("xmm10");
            reg_names.Add("xmm11");
            reg_names.Add("xmm12");
            reg_names.Add("xmm13");
            reg_names.Add("xmm14");
            reg_names.Add("xmm15");

            reg_names.Add("cr0");
            reg_names.Add("cr1");
            reg_names.Add("cr2");
            reg_names.Add("cr3");
            reg_names.Add("cr4");
            reg_names.Add("cr5");
            reg_names.Add("cr6");
            reg_names.Add("cr7");
            reg_names.Add("cr8");
            reg_names.Add("cr9");
            reg_names.Add("cr10");
            reg_names.Add("cr11");
            reg_names.Add("cr12");
            reg_names.Add("cr13");
            reg_names.Add("cr14");
            reg_names.Add("cr15");

            reg_names.Add("rip");

            for (ulong i = 0; i < (ulong)reg_names.Count; i++)
                reg_nos.Add(reg_names[(int)i], i);

            location.register_names = reg_names;
        }
    }
}
