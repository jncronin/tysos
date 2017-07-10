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

//#define GDB_DEBUG

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace tysos.x86_64
{
    partial class Arch
    {
        static Dictionary<ulong, byte[]> saved_bps;
        const ushort com2 = 0x2f8;
        static bool in_stop_command = false;
        static bool previous_was_step = false;
        static bool interrupts_were_enabled = false;

        internal override bool InitGDBStub()
        {
            /* The gdb stub is predominantly interrupt driven - it is entered by:
             * 
             * A #DB exception (INT 1)
             * A #BP exception (INT 3)
             */

            /* Set up the COM port */
            // COM2 base port is 0x2f8
            libsupcs.IoOperations.PortOut((ushort)(com2 + 1), (byte)0);      // Disable interrupts
            libsupcs.IoOperations.PortOut((ushort)(com2 + 3), (byte)0x80);   // Set DLAB bit to allow us to set the baud rate
            libsupcs.IoOperations.PortOut((ushort)(com2 + 0), (byte)0x03);   // Set divisor to 3 (baud = 115200 / 3 = 38400), low byte
            libsupcs.IoOperations.PortOut((ushort)(com2 + 1), (byte)0);      // Divisor high byte
            libsupcs.IoOperations.PortOut((ushort)(com2 + 3), (byte)0x3);    // 8 data bits, no parity bit, 1 stop bit, clear DLAB
            libsupcs.IoOperations.PortOut((ushort)(com2 + 2), (byte)0xc7);   // Enable and clear the FIFOs
            libsupcs.IoOperations.PortOut((ushort)(com2 + 4), (byte)0x0b);   // Enable IRQs, set RTS/DSR

            /* Register interrupt handlers */
            unsafe
            {
                Interrupts.InstallHandler(1, new Interrupts.ISR(GDB_DB_handler));
                Interrupts.InstallHandler(3, new Interrupts.ISR(GDB_BP_handler));
            }

            return true;
        }

        [libsupcs.CallingConvention("isr")]
        [libsupcs.AlwaysCompile]
        private static unsafe void GDB_DB_handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regsa)
        {
            ulong rbp = libsupcs.x86_64.Cpu.RBP;

            ulong *ret_rbp = (ulong*)rbp;
            ulong* ret_rip = (ulong*)(rbp + 8);
            uint* ret_rflags = (uint*)(rbp + 24);
            ulong* ret_rsp = (ulong*)(rbp + 32);

            gdb_loop(regsa, ret_rbp, ret_rip, ret_rflags, ret_rsp, 5);      // signal 5 is SIGTRAP
        }

        [libsupcs.CallingConvention("isr")]
        [libsupcs.AlwaysCompile]
        private static unsafe void GDB_BP_handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regsa)
        {
            ulong rbp = libsupcs.x86_64.Cpu.RBP;

            ulong* ret_rbp = (ulong*)rbp;
            ulong* ret_rip = (ulong*)(rbp + 8);
            uint* ret_rflags = (uint*)(rbp + 24);
            ulong* ret_rsp = (ulong*)(rbp + 32);

            gdb_loop(regsa, ret_rbp, ret_rip, ret_rflags, ret_rsp, 5);      // signal 5 is SIGTRAP
        }

        static ulong bp_to_reset = 0;

        static unsafe void gdb_loop(libsupcs.x86_64.Cpu.InterruptRegisters64* regs, 
            ulong *rbp, ulong *rip, uint *rflags, ulong *rsp, int sig_no)
        {
            if (saved_bps == null)
                saved_bps = new Dictionary<ulong, byte[]>(new Program.MyGenericEqualityComparer<ulong>());

#if GDB_DEBUG
            Formatter.Write("gdb_stub: entering main loop, RIP: ", Program.arch.DebugOutput);
            Formatter.Write(*rip, "x", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);
#endif
            bool cont = true;

            if (bp_to_reset != 0)
            {
#if GDB_DEBUG
                Formatter.Write("gdb_stub: resetting breakpoint at ", Program.arch.DebugOutput);
                Formatter.Write(bp_to_reset, "x", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
#endif
                set_bp(bp_to_reset, 1);
                bp_to_reset = 0;
            }
            if (saved_bps.ContainsKey(*rip - 1))
            {
                bp_to_reset = *rip - 1;
#if GDB_DEBUG
                Formatter.Write("gdb_stub: hit breakpoint at ", Program.arch.DebugOutput);
                Formatter.Write(bp_to_reset, "x", Program.arch.DebugOutput);
                Formatter.WriteLine(", temporarily unsetting", Program.arch.DebugOutput);
#endif
                unset_bp(bp_to_reset, 1);

                if(!previous_was_step)
                    *rip = bp_to_reset;
            }

            if(in_stop_command)
                gdb_send_message("T" + sig_no.ToString("x2") + gdb_reg_string(regs, rbp, rip, rflags, rsp, true));

            while (cont)
            {
                string message = gdb_read_message();

#if GDB_DEBUG
                Formatter.WriteLine("gdb_stub: received message: " + message, Program.arch.DebugOutput);
#endif
                if (message == "?")
                {
                    gdb_send_message("T" + sig_no.ToString("x2") + gdb_reg_string(regs, rbp, rip, rflags, rsp, true));
                }
                else if (message.StartsWith("m"))
                {
                    string rest_msg = message.Substring(1);
                    string[] msg_parts = rest_msg.Split(new char[] { ',' }, StringSplitOptions.None);

                    ulong addr = ulong.Parse(msg_parts[0], System.Globalization.NumberStyles.HexNumber);
                    int length = int.Parse(msg_parts[1], System.Globalization.NumberStyles.HexNumber);

#if GDB_DEBUG
                    Formatter.WriteLine("gdb_stub: memory read at " + addr.ToString("X2") + " (" + length.ToString() + " bytes)", Program.arch.DebugOutput);
#endif

                    StringBuilder ret = new StringBuilder();

                    for (int i = 0; i < length; i++, addr++)
                        ret.Append(libsupcs.MemoryOperations.PeekU1((UIntPtr)addr).ToString("x2"));

                    gdb_send_message(ret.ToString());
                }
                else if (message == "g")
                    gdb_send_message(gdb_reg_string(regs, rbp, rip, rflags, rsp, false));
                else if (message.StartsWith("G"))
                {
                    int idx = 1;
                    int reg_no = 0;

                    while (reg_no < 18)
                    {
                        int length = 8;
                        if (reg_no == 17)
                            length = 4;

                        ulong val = 0;
                        for (int i = 0; i < length; i++)
                        {
                            ulong bval = ulong.Parse(message.Substring(idx, 2), System.Globalization.NumberStyles.HexNumber);
                            val |= (bval << (i * 8));
                            idx += 2;
                        }

                        *gdb_get_reg_address(regs, rbp, rip, rflags, rsp, reg_no) = val;

                        reg_no++;
                    }

                    gdb_send_message("OK");
                }
                else if (message.StartsWith("c"))
                {
                    if (message.Length > 1)
                    {
                        ulong cont_addr = ulong.Parse(message.Substring(1), System.Globalization.NumberStyles.HexNumber);
                        *rip = cont_addr;
                    }
                    cont = false;
                    in_stop_command = true;
                    previous_was_step = false;

                    // TODO: have rflags also be a pointer
                    *rflags &= 0xfffffeff;   // clear trap flag
                    if (interrupts_were_enabled)
                    {
                        *rflags |= 0x200;    // restore IF if it was on previously
                    }
                }
                else if (message.StartsWith("Z"))
                {
                    string rest_msg = message.Substring(1);
                    string[] msg_parts = rest_msg.Split(new char[] { ',' });

                    ulong addr = ulong.Parse(msg_parts[1], System.Globalization.NumberStyles.HexNumber);
                    int kind = int.Parse(msg_parts[2], System.Globalization.NumberStyles.HexNumber);

                    if (msg_parts[0] == "0")
                    {
                        // memory break point
#if GDB_DEBUG
                        Formatter.WriteLine("gdb_stub: memory breakpoint at " + addr.ToString("X2") + " kind " + kind.ToString(), Program.arch.DebugOutput);
#endif

                        set_bp(addr, kind);

                        gdb_send_message("OK");
                    }
                    else
                        gdb_send_message(string.Empty);
                }
                else if (message.StartsWith("z"))
                {
                    string rest_msg = message.Substring(1);
                    string[] msg_parts = rest_msg.Split(new char[] { ',' });

                    ulong addr = ulong.Parse(msg_parts[1], System.Globalization.NumberStyles.HexNumber);
                    int kind = int.Parse(msg_parts[2], System.Globalization.NumberStyles.HexNumber);

                    if (msg_parts[0] == "0")
                    {
                        // remove memory break point

#if GDB_DEBUG
                        Formatter.WriteLine("gdb_stub: remove memory breakpoint at " + addr.ToString("X2") + " kind " + kind.ToString(), Program.arch.DebugOutput);
#endif

                        if (bp_to_reset == addr)
                            bp_to_reset = 0;
                        
                        if (!saved_bps.ContainsKey(addr))
                        {
#if GDB_DEBUG
                            Formatter.WriteLine("gdb_stub: memory breakpoint not found", Program.arch.DebugOutput);
#endif
                            gdb_send_message(string.Empty);
                        }
                        else
                        {
                            for (int i = 0; i < kind; i++)
                                libsupcs.MemoryOperations.Poke((UIntPtr)(addr + (ulong)i), saved_bps[addr][i]);
                            gdb_send_message("OK");

                            saved_bps.Remove(addr);
                        }
                    }
                    else
                        gdb_send_message(string.Empty);
                }
                else if (message.StartsWith("s"))
                {
                    if (message.Length > 1)
                    {
                        ulong cont_addr = ulong.Parse(message.Substring(1), System.Globalization.NumberStyles.HexNumber);
                        *rip = cont_addr;
                    }
                    cont = false;
                    in_stop_command = true;

                    unsafe
                    {
                        *rflags |= 0x100;        // set trap flag

                        if (!previous_was_step)
                        {
                            if ((*rflags & 0x200) == 0x200)
                                interrupts_were_enabled = true;
                            else
                                interrupts_were_enabled = false;
                        }

                        *rflags &= 0xfffffdff;   // clear interrupt flag
                    }

                    previous_was_step = true;
                }
                else if (message.StartsWith("qSupported"))
                {
                    // report that we support the qXfer:symbol_from_address:read packet
                    gdb_send_message("qXfer:symbol_from_address:read+");
                }
                else if (message.StartsWith("qXfer:symbol_from_address:read"))
                {
                    string addr = message.Substring("qXfer:symbol_from_address:read:".Length, 16);
                    ulong u_addr = ulong.Parse(addr, System.Globalization.NumberStyles.HexNumber);
                    if (Program.stab == null)
                        gdb_send_message("unknown");
                    else
                    {
                        ulong offset;
                        string sym = Program.stab.GetSymbolAndOffset(u_addr, out offset);
                        if (offset >= 0x100000)    // arbritrary large value for upper limit of function
                            gdb_send_message("unknown");
                        else
                            gdb_send_message(sym + ";" + offset.ToString("X16"));
                    }
                }
                else
                {
                    // packet not supported
                    gdb_send_message(string.Empty);
                }
            }
        }

        private static void set_bp(ulong addr, int kind)
        {
            // store what was saved in the instruction previously
            byte[] saved = new byte[kind];
            for (int i = 0; i < kind; i++)
                saved[i] = libsupcs.MemoryOperations.PeekU1((UIntPtr)(addr + (ulong)i));
            saved_bps[addr] = saved;

            if (kind >= 1)
            {
#if GDB_DEBUG
                Formatter.Write("gdb_stub: set_bp: write ", Program.arch.DebugOutput);
                Formatter.Write(0xcc, "X", Program.arch.DebugOutput);
                Formatter.Write(" to ", Program.arch.DebugOutput);
                Formatter.Write(addr, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
#endif
                libsupcs.MemoryOperations.Poke((UIntPtr)addr, (byte)0xcc);        // write int3
            }
            for (int i = 1; i < kind; i++)
            {
#if GDB_DEBUG
                Formatter.Write("gdb_stub: set_bp: write ", Program.arch.DebugOutput);
                Formatter.Write(0x90, "X", Program.arch.DebugOutput);
                Formatter.Write(" to ", Program.arch.DebugOutput);
                Formatter.Write(addr + (ulong)i, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
#endif
                libsupcs.MemoryOperations.Poke((UIntPtr)(addr + (ulong)i), (byte)0x90);    // pad with nop
            }
        }

        private static void unset_bp(ulong addr, int kind)
        {
            if (!saved_bps.ContainsKey(addr))
            {
#if GDB_DEBUG
                Formatter.Write("gdb_stub: breakpoint not found at ", Program.arch.DebugOutput);
                Formatter.Write(addr, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
#endif
                return;
            }

            for (int i = 0; i < kind; i++)
            {
#if GDB_DEBUG
                Formatter.Write("gdb_stub: unset_bp: write ", Program.arch.DebugOutput);
                Formatter.Write(saved_bps[addr][i], "X", Program.arch.DebugOutput);
                Formatter.Write(" to ", Program.arch.DebugOutput);
                Formatter.Write(addr + (ulong)i, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
#endif
                libsupcs.MemoryOperations.Poke((UIntPtr)(addr + (ulong)i), saved_bps[addr][i]);
            }

            saved_bps.Remove(addr);
        }

        private static unsafe string gdb_reg_string(libsupcs.x86_64.Cpu.InterruptRegisters64* regs,
            ulong* rbp, ulong* rip, uint* rflags, ulong* rsp, bool stop_response)
        {
            StringBuilder sb = new StringBuilder();

            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 0, stop_response);
            if(stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 1, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 2, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 3, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 4, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 5, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 6, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 7, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 8, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 9, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 10, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 11, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 12, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 13, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 14, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 15, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 16, stop_response);
            if (stop_response)
                sb.Append(";");
            gdb_append_reg_string(sb, regs, rbp, rip, rflags, rsp, 17, stop_response);
            if (stop_response)
                sb.Append(";");

            return sb.ToString();
        }

        private static unsafe void gdb_append_reg_string(StringBuilder sb,
            libsupcs.x86_64.Cpu.InterruptRegisters64* regs,
            ulong* rbp, ulong* rip, uint* rflags, ulong* rsp, int reg_no, bool stop_response)
        {
            if (stop_response)
            {
                sb.Append(reg_no.ToString("x"));
                sb.Append(":");
            }

            // RFLAGS is passed as a 32 bit value
            if (reg_no == 17)
                gdb_append_uint_lsb_value(sb, (uint)*gdb_get_reg_address(regs, rbp, rip, rflags, rsp, reg_no));
            else
                gdb_append_ulong_lsb_value(sb, *gdb_get_reg_address(regs, rbp, rip, rflags, rsp, reg_no));
        }

        private static void gdb_append_uint_lsb_value(StringBuilder sb, uint val)
        {
            for (int i = 0; i < 4; i++)
            {
                sb.Append((val & 0xff).ToString("x2"));
                val = val >> 8;
            }
        }

        private static void gdb_append_ulong_lsb_value(StringBuilder sb, ulong val)
        {
            for (int i = 0; i < 8; i++)
            {
                sb.Append((val & 0xff).ToString("x2"));
                val = val >> 8;
            }
        }

        private static unsafe ulong* gdb_get_reg_address(libsupcs.x86_64.Cpu.InterruptRegisters64* regs,
            ulong* rbp, ulong* rip, uint* rflags, ulong* rsp, int reg_no)
        {
            switch (reg_no)
            {
                case 0:
                    return &regs->rax;
                case 1:
                    return &regs->rbx;
                case 2:
                    return &regs->rcx;
                case 3:
                    return &regs->rdx;
                case 4:
                    return &regs->rsi;
                case 5:
                    return &regs->rdi;
                case 6:
                    return rbp;
                case 7:
                    return rsp;
                case 8:
                    return &regs->r8;
                case 9:
                    return &regs->r9;
                case 10:
                    return &regs->r10;
                case 11:
                    return &regs->r11;
                case 12:
                    return &regs->r12;
                case 13:
                    return &regs->r13;
                case 14:
                    return &regs->r14;
                case 15:
                    return &regs->r15;
                case 16:
                    return rip;
                case 17:
                    return (ulong*)rflags;
                default:
                    throw new Exception("gdb_stub: gdb_get_reg_address: unsupported register number: " + reg_no.ToString());
            }
        }

        private static void gdb_send_message(string s)
        {
#if GDB_DEBUG
            Formatter.Write("gdb_stub: sending message: ", Program.arch.DebugOutput);
            Formatter.WriteLine(s, Program.arch.DebugOutput);
#endif
            gdb_send_message(gdb_encode(s));
        }

        private static byte[] gdb_encode(string s)
        {
            List<byte> ret = new List<byte>();

            foreach (char c in s)
            {
                byte b = (byte)c;

                if ((b == 0x23) || (b == 0x24) || (b == 0x7d))
                {
                    ret.Add(0x7d);
                    ret.Add((byte)(0x20 ^ b));
                }
                else
                    ret.Add(b);
            }

            return ret.ToArray();
        }

        private static void gdb_send_message(byte[] s)
        {
#if GDB_DEBUG
            Formatter.Write("gdb_stub: sending message: ", Program.arch.DebugOutput);
            foreach (byte b in s)
                Formatter.Write((char)b, Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);
#endif

            while (true)
            {
                gdb_send_char((byte)'$');
                // calculate a checksum and send the original packet
                int checksum = 0;

                for (int i = 0; i < s.Length; i++)
                {
                    checksum += (int)s[i];
                    gdb_send_char(s[i]);
                }

                string checksum_s = (checksum % 256).ToString("x2");

                // send the checksum
                gdb_send_char((byte)'#');
                gdb_send_char((byte)checksum_s[0]);
                gdb_send_char((byte)checksum_s[1]);

                // wait for an acknowledgement
                char ch = gdb_read_char();
                if (ch == '+')
                    return;
                if (ch != '-')
                {
#if GDB_DEBUG
                    Formatter.WriteLine("gdb_stub: invalid acknowledgement: " + ch, Program.arch.DebugOutput);
#endif
                    return;
                }
            }
        }

        private static string gdb_read_message()
        {
            bool wait_start = true;

            while (true)
            {
                bool cont = true;
                int checksum = 0;
                int checksum_read = 0;
                bool read_package = true;
                bool valid_package = true;

                StringBuilder package = new StringBuilder();
                StringBuilder cs = new StringBuilder();

                // Wait for a start character
                if (wait_start)
                {
                    char start_ch;
                    do
                    {
                        start_ch = gdb_read_char();
                    } while (start_ch != '$');
                }
                wait_start = true;

                while (cont)
                {
                    char ch = gdb_read_char();

                    if (ch == '#')
                        read_package = false;       // start reading checksum
                    else if (ch == '$')
                    {
                        /* This is an invalid package - ignore it */
                        wait_start = false;
                        cont = false;
                        valid_package = false;
                    }
                    else
                    {
                        if (read_package)
                        {
                            package.Append(ch);
                            checksum += (int)ch;
                        }
                        else
                        {
                            cs.Append(ch);
                            checksum_read++;
                            if (checksum_read == 2)
                                cont = false;
                        }
                    }
                }

                if (valid_package)
                {
                    int checksum_i = Int32.Parse(cs.ToString(), System.Globalization.NumberStyles.HexNumber);
                    if (checksum_i != (checksum % 256))
                    {
#if GDB_DEBUG
                        Formatter.WriteLine("gdb_stub: checksum incorrect for received packet: " + package.ToString(), Program.arch.DebugOutput);
#endif
                        gdb_send_char((byte)'-');
                    }
                    else
                    {
                        gdb_send_char((byte)'+');
                        return package.ToString();
                    }
                }
            }
        }

        private static void gdb_send_char(byte ch)
        {
            while (is_transmit_empty() == 0) ;
            libsupcs.IoOperations.PortOut(com2, ch);
        }

        private static char gdb_read_char()
        {
            while (true)
            {
                uint x = gdb_get_char();
                if (x == 0)
                    continue;
                if ((x & 0x100) == 0x100)
                    return (char)(x & 0xff);
                if ((x & 0xe00) == 0)
                    continue;
                if ((x & 0x200) == 0x200)
                    Formatter.WriteLine("com2: Overrun error", Program.arch.DebugOutput);
                if ((x & 0x400) == 0x400)
                    Formatter.WriteLine("com2: Parity error", Program.arch.DebugOutput);
                if ((x & 0x800) == 0x800)
                    Formatter.WriteLine("com2: Framing error", Program.arch.DebugOutput);
            }
        }

        private static uint gdb_get_char()
        {
            uint x;
            x = ((uint)(libsupcs.IoOperations.PortInb(com2 + 5) & 0x9f)) << 8;
            if ((x & 0x100) == 0x100)
                x |= (uint)(libsupcs.IoOperations.PortInb(com2) & 0xff);
            return x;
        }

        static int serial_received()
        {
            return libsupcs.IoOperations.PortInb((ushort)(com2 + 5)) & 0x1;
        }

        static int is_transmit_empty()
        {
            return libsupcs.IoOperations.PortInb((ushort)(com2 + 5)) & 0x20;
        }

        internal override void Breakpoint()
        {
            libsupcs.x86_64.Cpu.Break();
        }
    }
}

namespace tysos.Debug.x86_64
{
    class Debug
    {
        internal static void InterrogateLock(object obj)
        {
            ulong obj_addr = libsupcs.CastOperations.ReinterpretAsUlong(obj);
            int mutex_lock_offset = 12;

            unsafe
            {
                ushort* lock_addr = (ushort*)(obj_addr + (ulong)mutex_lock_offset);
                ushort* nest_addr = (ushort*)(obj_addr + (ulong)mutex_lock_offset + 2);
                int* thread_id_addr = (int*)(obj_addr + (ulong)mutex_lock_offset + 4);

                Formatter.WriteLine("InterrogateLock: lock: " + obj_addr.ToString() + " lock_val: " + (*lock_addr).ToString() + " nest_val: " + (*nest_addr).ToString() + " thread_id: " + (*thread_id_addr).ToString(),
                    Program.arch.DebugOutput);
            }
        }
    }
}
