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
using tydisasm;
using System.IO;

namespace tyasm
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load a test file
            string test_file = "..\\..\\..\\libsupcs\\x86_64_cpu.asm";
            FileStream fs = new FileStream(test_file, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string file = sr.ReadToEnd();

            // Tokenize and parse
            List<Tokenizer.TokenDefinition> tokens = Tokenizer.Tokenize(file, Tokenizer.CAsmTokenGrammar, Tokenizer.NasmPreprocessorOptions);
            AsmParser.ParseOutput parsed = AsmParser.Parse(tokens);

            // Create an output file
            binary_library.IBinaryFile output_file = binary_library.BinaryFile.CreateBinaryFile("elf");

            // Assemble to the output file
            Assembler ass = new x86_Assembler();
            ass.Assemble(parsed, output_file);
        }
    }
}
