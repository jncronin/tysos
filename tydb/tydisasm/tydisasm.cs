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

namespace tydisasm
{
    public abstract class tydisasm
    {
        public abstract line GetNextLine(ByteProvider bp);

        public static tydisasm GetDisassembler(string arch)
        {
            System.Type[] types = typeof(tydisasm).Assembly.GetTypes();
            System.Type t = null;
            foreach (System.Type test in types)
            {
                if (test.Name == (arch + "_disasm"))
                {
                    t = test;
                    break;
                }
            }

            if (t == null)
                return null;
            System.Reflection.ConstructorInfo ctor = t.GetConstructor(System.Type.EmptyTypes);
            if (ctor == null)
                return null;
            tydisasm ret = ctor.Invoke(null) as tydisasm;
            return ret;
        }
    }

    public class line
    {
        public ulong opcode;
        public location[] arguments;
        public ulong[] prefixes;
        public ulong offset_start, offset_end;

        public byte[] opcodes;

        public virtual string OpcodeString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (opcodes != null)
                {
                    foreach (byte b in opcodes)
                        sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        internal ByteProvider bp;
    }

    public class location
    {
        internal static List<string> register_names;

        public enum location_type { Const, Register, Immediate, ContentsOf, SignedImmediate };
        public ulong reg_no;
        public ulong immediate;
        public long signed_immediate;
        public enum scale_func { None, Plus, Multiply };
        public scale_func scale;
        public location[] args;
        public location_type type;
        public bool is_pc_relative;

        public override string ToString()
        {
            return ToDisassembledString(null);
        }

        public virtual string ToDisassembledString(line l)
        {
            StringBuilder sb = new StringBuilder();
            if (scale == scale_func.Plus)
                sb.Append("+ ");
            else if (scale == scale_func.Multiply)
                sb.Append("* ");

            switch (type)
            {
                case location_type.Const:
                    sb.Append("0x" + immediate.ToString("x"));
                    if (is_pc_relative && (l != null) && (l.bp != null) && l.bp.ProvidesCurPC() && (l.opcodes != null))
                    {
                        ulong next_pc = l.bp.GetCurPC() + (ulong)l.opcodes.Length;
                        ulong dest_pc = next_pc + immediate;
                        sb.Append(" (0x" + dest_pc.ToString("x") + ")");
                    }
                    break;
                case location_type.ContentsOf:
                    {
                        sb.Append("[");

                        for (int i = 0; i < args.Length; i++)
                        {
                            if (i > 0)
                                sb.Append(" ");
                            sb.Append(args[i].ToString());
                        }

                        sb.Append("]");
                    }
                    break;
                case location_type.Immediate:
                    sb.Append("0x" + immediate.ToString("x"));
                    if (is_pc_relative && (l != null) && (l.bp != null) && l.bp.ProvidesCurPC() && (l.opcodes != null))
                    {
                        ulong next_pc = l.bp.GetCurPC() + (ulong)l.opcodes.Length;
                        ulong dest_pc = next_pc + immediate;
                        sb.Append(" (0x" + dest_pc.ToString("x") + ")");
                    }
                    break;
                case location_type.SignedImmediate:
                    {
                        long abs_val = signed_immediate;
                        if (abs_val < 0)
                        {
                            sb.Append("-");
                            abs_val = -signed_immediate;
                        }
                        sb.Append("0x" + abs_val.ToString("x"));
                        if (is_pc_relative && (l != null) && (l.bp != null) && l.bp.ProvidesCurPC() && (l.opcodes != null))
                        {
                            ulong next_pc = l.bp.GetCurPC() + (ulong)l.opcodes.Length;
                            ulong dest_pc = next_pc + (ulong)abs_val;
                            if(signed_immediate < 0)
                                dest_pc = next_pc - (ulong)abs_val;
                            sb.Append(" (0x" + dest_pc.ToString("x") + ")");
                        }
                        break;
                    }
                case location_type.Register:
                    if ((int)reg_no >= register_names.Count)
                        sb.Append("Unknown register (" + reg_no.ToString() + ")");
                    else
                        sb.Append(register_names[(int)reg_no]);
                    break;
            }
            return sb.ToString();
        }
    }
}
