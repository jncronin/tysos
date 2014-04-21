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
            Hash h = new Hash();
            System.IO.FileStream fs = new System.IO.FileStream(output, System.IO.FileMode.Create);
            System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
            h.Write(bw, f, ver, 0, bitness);
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
                else if (arg.Equals("-o"))
                {
                    if (i == args.Length - 1)
                    {
                        Usage();
                        return false;
                    }
                    output = args[++i];
                }
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
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
