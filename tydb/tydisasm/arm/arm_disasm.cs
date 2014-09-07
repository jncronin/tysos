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

namespace tydisasm.arm
{
    class arm_line : line
    {
        public uint cond = 15;
        public string name = null;

        public override string OpcodeString
        {
            get
            {
                return opcode.ToString("x8");
            }
        }

        public override string ToDisassembledString(tydisasm disasm)
        {
            StringBuilder sb = new StringBuilder();
            if(name == null)
                return "Unknown opcode";

            sb.Append(name);
            sb.Append(arm_disasm.cond_names[cond]);

            if (arguments != null)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (i == 0)
                        sb.Append(" ");
                    else
                        sb.Append(", ");
                    sb.Append(arguments[i].ToDisassembledString(this, disasm));
                }
            }

            return sb.ToString();
        }
    }

    public partial class arm_disasm : tydisasm
    {
        public arm_disasm()
        {
            init_regs();
        }

        public override int Bitness
        {
            get { return 32; }
        }

        public override line GetNextLine(ByteProvider bp)
        {
            byte[] l = new byte[4];
            l[0] = bp.GetNextByte();
            l[1] = bp.GetNextByte();
            l[2] = bp.GetNextByte();
            l[3] = bp.GetNextByte();
            uint l2 = BitConverter.ToUInt32(l, 0);

            arm_line ret = new arm_line();
            ret.opcode = l2;
            ret.opcodes = l;

            try
            {
                InterpretOpcode(l2, ret);
            }
            catch (NotImplementedException)
            { }

            ret.bp = bp;

            return ret;
        }

        void init_regs()
        {
            location.register_names = new List<string>();
            location.register_names.Add("r0");
            location.register_names.Add("r1");
            location.register_names.Add("r2");
            location.register_names.Add("r3");
            location.register_names.Add("r4");
            location.register_names.Add("r5");
            location.register_names.Add("r6");
            location.register_names.Add("r7");
            location.register_names.Add("r8");
            location.register_names.Add("r9");
            location.register_names.Add("r10");
            location.register_names.Add("fp");
            location.register_names.Add("r12");
            location.register_names.Add("sp");
            location.register_names.Add("lr");
            location.register_names.Add("pc");
        }
    }

    class arm_location : location
    {
        internal arm_disasm.ImmShiftRet immshift;
        internal enum arm_type { None, Offset, Preindex, Postindex };
        internal arm_type arm_const_index_type;
        internal bool reg_list = false;

        public override string ToDisassembledString(line l, tydisasm disasm)
        {
            StringBuilder sb = new StringBuilder();
            
            string base_str = base.ToDisassembledString(l, disasm);

            switch (type)
            {
                case location_type.Register:
                    if (reg_list)
                    {
                        int added = 0;
                        sb.Append("{");
                        for (int i = 0; i < 16; i++)
                        {
                            if (((reg_no >> i) & 0x1) == 0x1)
                            {
                                if (added != 0)
                                    sb.Append(",");
                                sb.Append(new location { type = location_type.Register, reg_no = (uint)i }.ToDisassembledString(l, disasm));
                                added++;
                            }
                        }
                        sb.Append("}");
                    }
                    else
                    {
                        sb.Append(base_str);
                        if (immshift != null)
                        {
                            switch (immshift.type)
                            {
                                case arm_disasm.SRType.SRType_LSL:
                                    sb.Append(", lsl #");
                                    sb.Append(immshift.amount.ToString());
                                    break;
                                case arm_disasm.SRType.SRType_LSR:
                                    sb.Append(", lsr #");
                                    sb.Append(immshift.amount.ToString());
                                    break;
                                case arm_disasm.SRType.SRType_ASR:
                                    sb.Append(", asr #");
                                    sb.Append(immshift.amount.ToString());
                                    break;
                                case arm_disasm.SRType.SRType_ROR:
                                    sb.Append(", ror #");
                                    sb.Append(immshift.amount.ToString());
                                    break;
                                case arm_disasm.SRType.SRType_RRX:
                                    sb.Append("rrx");
                                    break;
                            }
                            break;
                        }
                    }
                    break;
                case location_type.Immediate:
                    sb.Append("#");
                    sb.Append(base_str);
                    break;
                case location_type.ContentsOf:
                    switch(arm_const_index_type)
                    {
                        case arm_type.None:
                            sb.Append(base_str);
                            break;
                        case arm_type.Offset:
                            sb.Append("[");
                            sb.Append(register_names[(int)reg_no]);

                            if(signed_immediate != 0)
                            {
                                sb.Append(", #");
                                sb.Append(signed_immediate.ToString());
                            }
                            sb.Append("]");
                            break;
                        case arm_type.Preindex:
                            sb.Append("[");
                            sb.Append(register_names[(int)reg_no]);

                            if (signed_immediate != 0)
                            {
                                sb.Append(", #");
                                sb.Append(signed_immediate.ToString());
                            }
                            sb.Append("]!");
                            break;
                        case arm_type.Postindex:
                            sb.Append("[");
                            sb.Append(register_names[(int)reg_no]);
                            sb.Append("], #");
                            sb.Append(signed_immediate.ToString());
                            break;
                    }
                    break;
                default:
                    sb.Append(base_str);
                    break;
            }

            return sb.ToString();
        }
    }
}
