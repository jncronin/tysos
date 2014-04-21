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
    partial class Program
    {
        /* Boiler plate */
        const string version = "0.2.0";
        const string year = "2011";
        const string authors = "John Cronin <jncronin@tysos.org>";
        const string website = "http://www.tysos.org";
        const string nl = "\n";
        const string bplate = "tydb " + version + " (" + website + ")" + nl +
            "Copyright (C) " + year + " " + authors + nl +
            "This is free software.  Please see the source for copying conditions.  There is no warranty, " +
            "not even for merchantability or fitness for a particular purpose";

        /* Offset of .text section in debuggee */
        internal static UInt64 text_offset = 0;

        /* Symbol table */
        static List<string> symbol_filenames = new List<string>();
        internal static Dictionary<string, ulong> sym_to_addr = new Dictionary<string, ulong>();
        internal static SortedList<ulong, string> addr_to_sym = new SortedList<ulong, string>();
        internal static List<string> lib_dirs = new List<string>();

        /* The stream we use to communicate with the target */
        internal static Stream remote;

        /* serial port */
        static string serial_port;
        static int serial_baud = 38400;
        static System.IO.Ports.StopBits serial_stopbits = System.IO.Ports.StopBits.One;
        static System.IO.Ports.Parity serial_parity = System.IO.Ports.Parity.None;
        static int serial_databits = 8;

        /* The architecture we are using */
        static internal dbgarch arch;
        static internal string arch_name = "x86_64";
        static internal string feature_list = "";

        /* A mangled name to tydb function mapping */
        static internal Dictionary<string, libtysila.tydb.Function> functions = new Dictionary<string, libtysila.tydb.Function>();

        /* A text offset to tydb function mapping */
        static internal SortedList<ulong, libtysila.tydb.Function> functions_from_text_offset = new SortedList<ulong, libtysila.tydb.Function>();

        /* cci metadata interface */
        static string pefile = null;
        static List<string> tydbfiles = new List<string>();
        static internal CciInterface cci_int;

        static void Main(string[] args)
        {
            Console.WriteLine(bplate);
            if (!parse_args(args))
                return;
            if (!load_tydb_files())
                return;
            if (!load_pe_files())
                return;
            if (!load_sym_files())
                return;
            if (!init_stream())
                return;
            main_loop();
        }

        private static bool load_pe_files()
        {
            if ((pefile != null) && CciInterface.LibraryFound)
                cci_int = new CciInterface(pefile);
            return true;
        }

        private static bool load_tydb_files()
        {
            foreach (string tydb_file in tydbfiles)
            {
                tydbfile.TyDbFile t = tydbfile.TyDbFile.Read(new FileStream(tydb_file, FileMode.Open));
                foreach (libtysila.tydb.Function f in t.Functions)
                {
                    f.TextOffset += (uint)text_offset;

                    if (!functions.ContainsKey(f.MangledName))
                        functions.Add(f.MangledName, f);

                    if (!functions_from_text_offset.ContainsKey((ulong)f.TextOffset))
                        functions_from_text_offset.Add((ulong)f.TextOffset, f);
                }
            }
            return true;
        }

        private static bool init_stream()
        {
            if (serial_port != null)
            {
                /* Initialize a serial port */
                try
                {
                    System.IO.Ports.SerialPort sport = new System.IO.Ports.SerialPort(serial_port, serial_baud, serial_parity, serial_databits, serial_stopbits);
                    sport.Open();
                    remote = sport.BaseStream;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error opening serial port: " + e.ToString());
                    return false;
                }
            }

            if (remote == null)
            {
                Console.WriteLine("No suitable remote protocol selected");
                return false;
            }

            return true;
        }

        private static void main_loop()
        {
            Console.WriteLine("Synchronizing with target...");
            comm.gdb_send_message("?");
            await.state s = await.get_state();
            Console.WriteLine(s.ToString());

            bool cont = true;
            string[] prev_cmd = null;
            while (cont)
            {
                Console.Write("(tydb) ");
                string orig_cmd_line = Console.ReadLine();
                string[] cmd_line = orig_cmd_line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (cmd_line.Length == 0)
                {
                    if (prev_cmd != null)
                        cmd_line = prev_cmd;
                    else
                        continue;
                }

                string cmd = cmd_line[0];
                if ((cmd == "q") || (cmd == "quit") || (cmd == "exit"))
                    cont = false;
                else if ((cmd == "c") || (cmd == "cont") || (cmd == "continue"))
                {
                    comm.gdb_send_message("c");
                    s = await.get_state();
                    Console.WriteLine(s.ToString());
                }
                else if ((cmd == "si") || (cmd == "stepi"))
                {
                    comm.gdb_send_message("s");
                    s = await.get_state();
                    Console.WriteLine(s.ToString());
                }
                else if ((cmd == "s") || (cmd == "step"))
                {
                    do
                    {
                        comm.gdb_send_message("s");
                        s = await.get_state();
                    } while (s.source == null);
                    Console.WriteLine(s.ToString());
                }
                else if ((cmd == "r") || (cmd == "regs") || (cmd == "registers"))
                {
                    regs.dump_regs();
                }
                else if ((cmd == "bp") || (cmd == "break") || (cmd == "breakpoint"))
                {
                    string addr = cmd_line[1];
                    if (addr.StartsWith("0x"))
                        addr = addr.Substring(2);
                    ulong uaddr = ulong.Parse(addr, System.Globalization.NumberStyles.HexNumber);
                    comm.gdb_send_message("Z0," + uaddr.ToString("X16") + ",1");
                    if (comm.gdb_read_message() == "OK")
                        Console.WriteLine("Breakpoint set");
                    else
                        Console.WriteLine("Error setting breakpoint");
                }
                else if ((cmd == "x") || (cmd == "?"))
                {
                    //ulong addr = ulong.Parse(cmd_line[1], System.Globalization.NumberStyles.HexNumber);
                    //obj obj = obj.get_obj(addr);
                    if (s.ty_f != null)
                    {
                        libtysila.tydb.VarArg va = s.ty_f.GetVarArg(cmd_line[1]);

                        if (va != null)
                        {
                            if (va.Location.Type == libtysila.tydb.Location.LocationType.Register)
                                cmd_line[1] = va.Location.RegisterName;
                            else if (va.Location.Type == libtysila.tydb.Location.LocationType.ContentsOfLocation)
                            {
                                if (va.Location.ContentsOf.Type == libtysila.tydb.Location.LocationType.Register)
                                {
                                    ulong? val = await.get_register(s, Program.arch.get_reg(va.Location.ContentsOf.RegisterName).id);
                                    if (val.HasValue)
                                    {
                                        ulong mem_loc = val.Value;
                                        if (va.Location.Offset >= 0)
                                            mem_loc += (ulong)va.Location.Offset;
                                        else
                                            mem_loc -= (ulong)(-va.Location.Offset);

                                        cmd_line[1] = "*" + mem_loc.ToString("x" + (Program.arch.address_size * 2).ToString());
                                    }
                                }
                            }
                        }
                    }

                    dbgarch.register r = arch.get_reg(cmd_line[1]);
                    if (r != null)
                    {
                        ulong? val = await.get_register(new await.state(), r.id);
                        Console.WriteLine(r.name + " = " + (val.HasValue ? val.Value.ToString("x" + (arch.data_size * 2).ToString()) : "unknown"));
                    }
                    else
                    {
                        obj obj = var.get_var(cmd_line[1]);
                        if (obj == null)
                            Console.WriteLine("Syntax Error");
                        else
                            Console.WriteLine(obj.addr.ToString("x" + (arch.address_size * 2).ToString()) + " = " + obj.ToString());
                    }
                }
                else if (cmd == "set")
                {
                    if (orig_cmd_line == "set")
                    {
                        foreach (KeyValuePair<string, obj> kvp in var.vars)
                            Console.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
                        continue;
                    }

                    int eq_pos = orig_cmd_line.IndexOf('=');

                    if (eq_pos == -1)
                    {
                        Console.WriteLine("Syntax Error");
                        continue;
                    }

                    string var_n = orig_cmd_line.Substring(4, eq_pos - 4).Trim();
                    string arg = orig_cmd_line.Substring(eq_pos + 1).Trim();

                    if (var_n.Contains(" ") || arg.Contains(" "))
                    {
                        Console.WriteLine("Syntax Error");
                        continue;
                    }

                    obj o = var.get_var(arg);
                    if (o == null)
                        Console.WriteLine("Syntax Error");
                    else
                        var.vars[var_n] = o;
                }
                else
                {
                    Console.WriteLine("Unrecognized command: " + cmd);
                    prev_cmd = null;
                    continue;
                }

                prev_cmd = cmd_line;
            }
        }

        private static bool load_sym_files()
        {
            foreach (string s in symbol_filenames)
            {
                if (!load_sym_file(s))
                    return false;
            }
            return true;
        }

        private static bool parse_args(string[] args)
        {
            int i = 0;
            while (i < args.Length)
            {
                if (args[i] == "--serial-port")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    serial_port = args[i];
                }
                else if (args[i] == "--baud")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    try
                    {
                        serial_baud = Int32.Parse(args[i]);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else if (args[i] == "--stop-bits")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    string sb = args[i];
                    if ((sb == "0") || (sb.ToLower() == "none"))
                        serial_stopbits = System.IO.Ports.StopBits.None;
                    else if ((sb == "1") || (sb.ToLower() == "one"))
                        serial_stopbits = System.IO.Ports.StopBits.One;
                    else if ((sb == "15") || (sb == "1_5") || (sb == "1-5") || (sb == "1.5") || (sb.ToLower() == "onepointfive"))
                        serial_stopbits = System.IO.Ports.StopBits.OnePointFive;
                    else if ((sb == "2") || (sb.ToLower() == "two"))
                        serial_stopbits = System.IO.Ports.StopBits.Two;
                    else
                        return false;
                }
                else if (args[i] == "--parity")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    string p = args[i];
                    if ((p.ToLower() == "none") || (p == "0"))
                        serial_parity = System.IO.Ports.Parity.None;
                    else if (p.ToLower() == "odd")
                        serial_parity = System.IO.Ports.Parity.Odd;
                    else if (p.ToLower() == "even")
                        serial_parity = System.IO.Ports.Parity.Even;
                    else if (p.ToLower() == "mark")
                        serial_parity = System.IO.Ports.Parity.Mark;
                    else if (p.ToLower() == "space")
                        serial_parity = System.IO.Ports.Parity.Space;
                    else
                        return false;
                }
                else if (args[i] == "--data-bits")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    string db = args[i];
                    if ((db == "7") || (db.ToLower() == "seven"))
                        serial_databits = 7;
                    else if ((db == "8") || (db.ToLower() == "eight"))
                        serial_databits = 8;
                    else
                        return false;
                }
                else if (args[i] == "--symbols")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    symbol_filenames.Add(args[i]);
                }
                else if (args[i] == "--tydb-file")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    tydbfiles.Add(args[i]);
                }
                else if (args[i] == "--pe-file")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    if (pefile != null)
                        return false;
                    pefile = args[i];
                }
                else if (args[i] == "--text-offset")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    if (args[i].StartsWith("0x"))
                        text_offset = UInt64.Parse(args[i].Substring(2), System.Globalization.NumberStyles.HexNumber);
                    else
                        text_offset = UInt64.Parse(args[i]);
                }
                else if (args[i].StartsWith("-L"))
                {
                    string ldir = null;
                    if (args[i] == "-L")
                    {
                        i++;
                        ldir = args[i];
                    }
                    else
                        ldir = args[i].Substring(2);
                    lib_dirs.Add(ldir);
                }

                i++;
            }

            return true;
        }
    }
}
