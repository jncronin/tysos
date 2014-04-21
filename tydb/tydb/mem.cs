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

namespace tydb
{
    class mem
    {
        internal static ulong get_mem(ulong address, int size)
        {
            comm.gdb_send_message("m" + address.ToString("x") + "," + size.ToString());
            string s = comm.gdb_read_message();

            if (size == 1)
                return ulong.Parse(s, System.Globalization.NumberStyles.HexNumber);
            else if (size == 2)
                return (ulong)await.read_machine_byte_order_ushort(s);
            else if (size == 4)
                return (ulong)await.read_machine_byte_order_uint(s);
            else if (size == 8)
                return await.read_machine_byte_order_ulong(s);
            else
                throw new NotSupportedException();
        }

        internal static ulong get_mem(ulong address)
        { return get_mem(address, Program.arch.address_size); }

        internal static string get_symbol(ulong address, out ulong offset)
        {
            bool found_remote_symbol = false;
            string symbol_name = null;
            ulong p_counter = address;
            offset = 0;

            if (comm.remote_supports("qXfer:symbol_from_address:read"))
            {
                string query_string = null;
                if (Program.arch.address_size == 8)
                    query_string = address.ToString("x16");
                else if (Program.arch.address_size == 4)
                    query_string = address.ToString("x8");
                comm.gdb_send_message("qXfer:symbol_from_address:read:" + query_string + ":");
                string[] response = comm.gdb_read_message().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (response[0] != "unknown")
                {
                    symbol_name = response[0];
                    offset = ulong.Parse(response[1], System.Globalization.NumberStyles.HexNumber);
                    found_remote_symbol = true;
                }
            }

            if (!found_remote_symbol)
            {
                // load from the local symbol table

                if (Program.addr_to_sym.ContainsKey(p_counter))
                {
                    offset = 0;
                    symbol_name = Program.addr_to_sym[p_counter];
                }
                else
                {
                    Program.addr_to_sym.Add(p_counter, "probe");
                    int idx = Program.addr_to_sym.IndexOfKey(p_counter);
                    Program.addr_to_sym.RemoveAt(idx);

                    if (idx == 0)
                    {
                        offset = p_counter;
                        symbol_name = "offset_0";
                    }
                    else
                    {
                        ulong sym_addr = Program.addr_to_sym.Keys[idx - 1];
                        offset = p_counter - sym_addr;
                        symbol_name = Program.addr_to_sym[sym_addr];
                    }
                }
            }

            return symbol_name;
        }
    }
}
