﻿/* Copyright (C) 2013-2016 by John Cronin
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

namespace binary_library
{
    public enum Bitness
    {
        BitsUnknown = 0, Bits8 = 8, Bits16 = 16, Bits32 = 32, Bits64 = 64, Bits128 = 128, Bits256 = 256
    }


    public interface IBinaryFile
    {
        string Filename { get; set; }
        string Architecture { get; set; }
        string OS { get; set; }
        string BinaryType { get; set; }
        string NameTriple { get; }
        string EntryPoint { get; set; }
        Bitness Bitness { get; set; }
        bool IsExecutable { get; set; }

        void Init();
        void Write();
        void Read();
        void Read(System.IO.Stream s);

        ISection GetTextSection();
        ISection GetDataSection();
        ISection GetRDataSection();
        ISection GetBSSSection();

        int GetSectionCount();
        ISection GetSection(int idx);
        int AddSection(ISection section);
        void RemoveSection(int idx);

        ISection CopySectionType(ISection tmpl);
        ISection GetGlobalSection();
        ISection GetCommonSection();
        ISection CreateSection();
        ISection CreateContentsSection();
        ISection FindSection(string name);

#if HAVE_SYSTEM
        ISection FindSection(System.Text.RegularExpressions.Regex r);
#endif

        int GetSymbolCount();
        ISymbol GetSymbol(int idx);
        ISymbol FindSymbol(string name);
        ISymbol CreateSymbol();
        IEnumerable<ISymbol> GetSymbols();
        void RemoveSymbol(int idx);

        bool ContainsSymbol(ISymbol symbol);

        int GetRelocationCount();
        IRelocation GetRelocation(int idx);
        int AddRelocation(IRelocation reloc);
        void RemoveRelocation(int idx);
        IRelocation CreateRelocation();
    }

    /** <summary>Implement this interface to support reading back the types of file supported by
     * this class</summary>
     */
    public interface IBinaryFileTypeName
    {
        string[] GetSupportedFileTypes();
    }

    /** <summary>Implement this if this binary file is actually a collection of other binary files</summary>
     */
    public interface IBinaryFileCollection
    {
        int GetBinaryFileCount();
        IBinaryFile GetBinaryFile(int idx);
        IBinaryFile FindBinaryFile(string name);
    }
}
