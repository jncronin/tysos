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
using System.Linq;
using System.Text;

namespace tl
{
    class Program
    {
        internal static Options options = new Options();

        static void Main(string[] args)
        {
            // Set some options
            options["text-start-addr"] = Options.OptionValue.InterpretString("0x400000");
            options["data-start-addr"].Set = false;
            options["rodata-start-addr"].Set = false;
            options["bss-start-addr"].Set = false;
            options["page-size"] = Options.OptionValue.InterpretString("0x1000");

            // Initialize the default linker scripts
            DefaultScripts.InitScripts();

            // Load the input files
            List<binary_library.IBinaryFile> inputs = new List<binary_library.IBinaryFile>();
            string[] ifiles = new string[] { "C:\\Users\\jncronin\\Documents\\Visual Studio 2008\\Projects\\Tysos\\tysos\\tysos.obj",
                "C:\\Users\\jncronin\\Documents\\Visual Studio 2008\\Projects\\Tysos\\tysos\\halt.o",
                "C:\\Users\\jncronin\\Documents\\Visual Studio 2008\\Projects\\Tysos\\tysos\\cpu.o",
                "C:\\Users\\jncronin\\Documents\\Visual Studio 2008\\Projects\\Tysos\\tysos\\undefined.o",
                "C:\\Users\\jncronin\\Documents\\Visual Studio 2008\\Projects\\Tysos\\tysos\\x86_64\\switcher.o",
                "C:\\Users\\jncronin\\Documents\\Visual Studio 2008\\Projects\\Tysos\\tysos\\x86_64\\exceptions.o",
                "C:\\Users\\jncronin\\Documents\\Visual Studio 2008\\Projects\\Tysos\\libtysila\\libtysila.obj",
                "C:\\Users\\jncronin\\Documents\\Visual Studio 2008\\Projects\\Tysos\\mono\\corlib\\mscorlib.obj",
                "C:\\Users\\jncronin\\Documents\\Visual Studio 2008\\Projects\\Tysos\\libsupcs\\libsupcs.obj"
            };

            foreach (string ifile in ifiles)
            {
                binary_library.elf.ElfFile ef = new binary_library.elf.ElfFile();
                ef.Filename = ifile;
                ef.Read();
                inputs.Add(ef);
            }

            // Determine the architecture
            string cur_arch = null;
            if (options["arch"].Set)
                cur_arch = options["arch"].String;
            foreach (binary_library.IBinaryFile ifile in inputs)
            {
                if (cur_arch == null)
                    cur_arch = ifile.NameTriple;
                else if (!(cur_arch.Equals(ifile.NameTriple)))
                {
                    Console.WriteLine("Error: " + ifile.Filename + " is not of type " + cur_arch);
                    throw new Exception();
                }
            }
            string[] ntriple = cur_arch.Split('-');
            if(ntriple.Length != 3)
            {
                Console.WriteLine("Error: invalid architecture: " + cur_arch);
                throw new Exception();
            }

            // Create an output file
            binary_library.elf.ElfFile of = new binary_library.elf.ElfFile();
            of.Init();
            of.Architecture = ntriple[0];
            of.BinaryType = ntriple[1];
            of.OS = ntriple[2];

            // Get the linker script
            LinkerScript script = DefaultScripts.Scripts[of.BinaryType];
            
            // Run the script
            script.RunScript(of, inputs);
        }
    }
}
