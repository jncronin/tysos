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

namespace tytrie
{
    class Program
    {
        static void Main(string[] args)
        {
            Trie t = new Trie();

            LoadELFFile("../../../libsupcs/libsupcs.obj", t);

            System.IO.FileStream fs = new System.IO.FileStream("out.bin", System.IO.FileMode.Create);
            System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
            t.Write(bw, 0x1, 0, 1);
        }

        static void LoadELFFile(string name, Trie t)
        {
            binary_library.IBinaryFile file = new binary_library.elf.ElfFile();
            file.Filename = name;
            file.Read();

            int sym_count = file.GetSymbolCount();
            for (int i = 0; i < sym_count; i++)
            {
                binary_library.ISymbol sym = file.GetSymbol(i);
                if ((sym.Name != null) && (sym.Name != ""))
                    t.AddSymbol(sym.Name, sym.Offset);
            }
        }
    }
}
