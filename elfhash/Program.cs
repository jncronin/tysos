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
using System.Text;
using System.IO;

namespace elfhash
{
    class Program
    {
        static string input, output;
        static string hash_sym = "_hash_start";
        static int bitness = -1;
        static int ver = 0;

        static void Main(string[] args)
        {
            if (!ParseArgs(args))
                return;

            // Ensure the input file is readable
            System.IO.FileInfo input_fi = new System.IO.FileInfo(input);
            if (!input_fi.Exists)
            {
                System.Console.WriteLine("Unable to open: " + input);
                return;
            }

            // Determine the output filename if not specified
            if (output == null)
                output = input_fi.FullName + ".hash";

            // Load up the input file
            binary_library.IBinaryFile f = new binary_library.elf.ElfFile();
            f.Filename = input;
            f.Read();

            // Determine the bitness if not specified
            if (bitness == -1)
            {
                switch (f.Bitness)
                {
                    case binary_library.Bitness.Bits32:
                        bitness = 0;
                        break;
                    case binary_library.Bitness.Bits64:
                        bitness = 1;
                        break;
                    case binary_library.Bitness.BitsUnknown:
                        System.Console.WriteLine("Warning: unable to determine bitness of " + input + " - defaulting to 32 bits");
                        bitness = 0;
                        break;
                    default:
                        System.Console.WriteLine("Unsupported bitness of " + input + " - " + f.Bitness.ToString());
                        return;
                }
            }

            // Write the output file
            if (ver == 2)
            {
                // output copy of the original ELF file with a .hash section
                var hs = f.CreateSection();
                hs.Name = ".hash";
                hs.AddrAlign = 0x1000;
                hs.IsAlloc = true;
                hs.IsWriteable = false;
                hs.IsExecutable = false;
                hs.HasData = true;

                // locate it somewhere after the last offset and virtual address in the file
                if (f.IsExecutable)
                {
                    long last_offset = 0;
                    ulong last_vaddr = 0;
                    for (int i = 0; i < f.GetSectionCount(); i++)
                    {
                        var s = f.GetSection(i);
                        if(s != null && s.IsAlloc)
                        {
                            if (s.LoadAddress + (ulong)s.Length > last_vaddr)
                                last_vaddr = s.LoadAddress + (ulong)s.Length;
                            if (s.FileOffset + s.Length > last_offset)
                                last_offset = s.FileOffset + s.Length;
                        }
                    }

                    if ((last_vaddr & 0xfff) != 0)
                        last_vaddr = (last_vaddr + 0x1000UL) & ~0xfffUL;
                    if ((last_offset & 0xfff) != 0)
                        last_offset = (last_offset + 0x1000L) & ~0xfffL;

                    hs.LoadAddress = last_vaddr;
                    hs.FileOffset = last_offset;

                    // Create start symbol
                    var hss = f.CreateSymbol();
                    hss.Name = hash_sym;
                    hss.Type = binary_library.SymbolType.Global;
                    hss.ObjectType = binary_library.SymbolObjectType.Object;
                    hs.AddSymbol(hss);
                }

                f.AddSection(hs);

                f.Filename = output;
                ((binary_library.elf.ElfFile)f).CreateHashSection = true;
                ((binary_library.elf.ElfFile)f).SortSymbols = true;
                f.Write();
            }
            else
            {
                // generate a separate hash file
                Hash h = new Hash();
                System.IO.FileStream fs = new System.IO.FileStream(output, System.IO.FileMode.Create);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                h.Write(bw, f, ver, 0, bitness);
            }
        }

        private static bool ParseArgs(string[] args)
        {
            if (args.Length == 0)
            {
                Usage();
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg.Equals("-32"))
                    bitness = 0;
                else if (arg.Equals("-64"))
                    bitness = 1;
                else if (arg.Equals("-v0"))
                    ver = 0;
                else if (arg.Equals("-v1"))
                    ver = 1;
                else if (arg.Equals("-e"))
                    ver = 2;
                else if (arg.Equals("-o"))
                {
                    if (i == args.Length - 1)
                    {
                        Usage();
                        return false;
                    }
                    output = args[++i];
                }
                else if (arg.Equals("--hash-sym"))
                    hash_sym = args[++i];
                else if (i == args.Length - 1)
                    input = arg;
                else
                {
                    Usage();
                    return false;
                }
            }

            return true;
        }

        private static void Usage()
        {
            string cmd_line = Environment.GetCommandLineArgs()[0];
            FileInfo fi = new FileInfo(cmd_line);
            Console.WriteLine("Usage: " + fi.Name + " [options] <input_file>");
            Console.WriteLine(" Generates an ELF-style hash table from an input file");
            Console.WriteLine(" The options are:");
            Console.WriteLine("  -o <output_file>       specify output file name");
            Console.WriteLine("  -32                    enforce 32-bit hash file");
            Console.WriteLine("  -64                    enforce 64-bit hash file");
            Console.WriteLine("  -v0                    enforce version 0 (ELF-style) hash file without header");
            Console.WriteLine("  -v1                    enforce version 1 (tysos) hash file with header");
            Console.WriteLine("  -e                     embed in output ELF file");
            Console.WriteLine("  --hash-sym <name>      name of symbol that starts embedded hash (defaults to _hash_start)")
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
