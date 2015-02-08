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
using System.IO;

namespace tydb
{
    class await
    {
        internal class state
        {
            internal int stop_reason;
            internal Dictionary<int, ulong?> regs = new Dictionary<int, ulong?>();
            internal string sym_name;
            internal ulong offset;
            internal libtysila.tydb.Function ty_f;
            internal int il_offset;
            internal tydisasm.line disasm;
            internal string source;
            internal string pefile;
            internal uint m_tok;
            internal ulong? pc;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (pc.HasValue)
                {
                    sb.Append("Next at " + pc.Value.ToString("x" + (Program.arch.address_size * 2).ToString()));
                    sb.Append(Environment.NewLine);

                    if (source != null)
                    {
                        sb.Append(source);
                        sb.Append(Environment.NewLine);
                    }

                    if (sym_name != null)
                    {
                        sb.Append(sym_name);
                        sb.Append(" + ");
                        sb.Append(offset.ToString("x" + (Program.arch.address_size * 2).ToString()));

                        if (disasm != null)
                        {
                            sb.Append(": ");
                            sb.Append(disasm.ToDisassembledString(Program.arch.disasm));
                        }
                    }
                }
                else
                    sb.Append("Next at unknown address");
                return sb.ToString();
            }
        }

        internal static state s = null;

        internal static state get_state()
        {
            bool cont = true;
            s = new state();

            while (cont)
            {
                string msg = comm.gdb_read_message();

                if (msg.StartsWith("T"))
                {
                    interpret_t_message(msg, s);
                    cont = false;
                }
                else if (msg.StartsWith("S"))
                {
                    interpret_s_message(msg, s);
                    cont = false;
                }
                else
                    System.Diagnostics.Debug.WriteLine("Invalid message: " + msg);
            }

            ulong? pc = get_register(s, Program.arch.PC_id);
            if (pc.HasValue)
            {
                s.pc = pc.Value;

                string symbol_name;
                ulong offset;
                symbol_name = mem.get_symbol(pc.Value, out offset);

                s.offset = offset;
                s.sym_name = symbol_name;

                // provide a dissassembly
                if (Program.arch.disasm != null)
                    s.disasm = Program.arch.disasm.GetNextLine(new RemoteByteProvider(pc.Value));

                // get the source
                libtysila.tydb.Function t_f = GetFunction(ref s.sym_name, pc.Value, ref s.offset);
                if (t_f != null)
                {
                    s.pefile = t_f.MetadataFileName;
                    int il_offset = t_f.GetILOffsetFromCompiledOffset((int)s.offset);
                    uint m_tok = t_f.MetadataToken;

                    s.ty_f = t_f;
                    s.il_offset = il_offset;
                    s.m_tok = m_tok;
                    if(Program.cci_int != null)
                        s.source = Program.cci_int.GetSourceLineFromToken(m_tok, (uint)il_offset);
                }
            }

            return s;
        }

        private static libtysila.tydb.Function GetFunction(ref string symbol_name, ulong pc, ref ulong offset)
        {
            if (Program.functions.ContainsKey(symbol_name) && (Program.functions[symbol_name].GetILOffsetFromCompiledOffset((int)offset) != -1))
                return Program.functions[symbol_name];

            if (Program.functions_from_text_offset.ContainsKey(pc))
                return Program.functions_from_text_offset[pc];

            Program.functions_from_text_offset.Add(pc, new libtysila.tydb.Function());
            int idx = Program.functions_from_text_offset.IndexOfKey(pc);
            Program.functions_from_text_offset.RemoveAt(idx);

            if (idx == 0)
                return null;

            libtysila.tydb.Function ret = Program.functions_from_text_offset.Values[idx - 1];
            offset = offset - ret.TextOffset;
            symbol_name = ret.MangledName;
            return ret;
        }

        internal static ulong? get_register(state s, int p)
        {
            if (s.regs.ContainsKey(p) && s.regs[p].HasValue)
                return s.regs[p].Value;

            // we do not know the register value, therefore request it from the remote stub
            comm.gdb_send_message("g");
            string regs = comm.gdb_read_message();

            int x = 0;
            for (int i = 0; i < Program.arch.registers.Length; i++)
                read_t_reg(ref x, regs, s, false, i);

            return s.regs[p];
        }

        private static void interpret_t_message(string msg, state s)
        {
            interpret_s_message(msg, s);

            int x = 3;

            while (x < msg.Length)
                read_t_reg(ref x, msg, s, true, 0);
        }

        private static void read_t_reg(ref int x, string msg, state s, bool is_t_response, int reg_id)
        {
            if (((msg[x] >= '0') && (msg[x] <= '9')) || ((msg[x] >= 'a') && (msg[x] <= 'f')))
            {
                if (is_t_response)
                {
                    int reg_count = 1;
                    if ((msg[x + 1] >= '0') && (msg[x + 1] <= '9') || ((msg[x + 1] >= 'a') && (msg[x + 1] <= 'f')))
                        reg_count = 2;
                    string reg_id_str = msg.Substring(x, reg_count);
                    x += reg_count;
                    reg_id = Int32.Parse(reg_id_str, System.Globalization.NumberStyles.HexNumber);

                    string colon = msg.Substring(x, 1);
                    if (colon != ":")
                        throw new Exception("Invalid stop-reply response");
                    x++;
                }

                if ((reg_id < 0) || (reg_id >= Program.arch.registers.Length))
                    throw new Exception("Invalid register number: " + reg_id.ToString());

                dbgarch.register reg_def = Program.arch.registers[reg_id];

                if (reg_def.length == 8)
                {
                    if (msg.Substring(x, 16) == "xxxxxxxxxxxxxxxx")
                        s.regs[reg_id] = null;
                    else
                        s.regs[reg_id] = read_machine_byte_order_ulong(msg.Substring(x, 16));
                    x += 16;
                }
                else if (reg_def.length == 4)
                {
                    if (msg.Substring(x, 8) == "xxxxxxxxx")
                        s.regs[reg_id] = null;
                    else
                        s.regs[reg_id] = read_machine_byte_order_uint(msg.Substring(x, 8));
                    x += 8;
                }
                else
                    throw new Exception("Invalid register length for register " + reg_def.name + ": " + reg_def.length.ToString());

                if (is_t_response)
                    x++;    // semicolon at end
            }
            else
                throw new Exception("Unknown stop message: " + msg + " at index " + x.ToString());
        }

        private static void interpret_s_message(string msg, state s)
        {
            string stop_reason = msg.Substring(1, 2);
            s.stop_reason = Int32.Parse(stop_reason, System.Globalization.NumberStyles.HexNumber);

            s.regs = new Dictionary<int, ulong?>();

            if (Program.arch == null)
                if (!dbgarch.Create())
                    Environment.Exit(-1);
        }

        internal static ulong read_machine_byte_order_ulong(string s)
        {
            if (Program.arch.is_lsb)
                s = byte_swap(s);
            return ulong.Parse(s, System.Globalization.NumberStyles.HexNumber);
        }

        private static string byte_swap(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = s.Length - 2; i >= 0; i -= 2)
                sb.Append(s.Substring(i, 2));
            return sb.ToString();
        }

        internal static uint read_machine_byte_order_uint(string s)
        {
            if (Program.arch.is_lsb)
                s = byte_swap(s);
            return uint.Parse(s, System.Globalization.NumberStyles.HexNumber);
        }

        internal static ushort read_machine_byte_order_ushort(string s)
        {
            if (Program.arch.is_lsb)
                s = byte_swap(s);
            return ushort.Parse(s, System.Globalization.NumberStyles.HexNumber);
        }

        private class RemoteByteProvider : tydisasm.ByteProvider
        {
            public RemoteByteProvider(ulong start_pc)
            {
                pc = start_pc;
            }

            ulong pc;

            public override byte GetNextByte()
            {
                comm.gdb_send_message("m" + pc.ToString("x2") + ",1");
                string s = comm.gdb_read_message();
                if (s.Length != 2)
                    throw new Exception("Invalid length of returned memory contents");
                byte ret = byte.Parse(s, System.Globalization.NumberStyles.HexNumber);
                pc++;
                return ret;
            }

            public override ulong Offset
            {
                get { return pc; }
            }
        }
    }
}
