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

/* A dummy output file that can be passed to the AssembleType/AssembleTypeInfo
 * functions to allow them to work, but not actually write anything to disk
 */

using System;
using System.Collections.Generic;
using System.Text;
using libtysila;

namespace tysila
{
    class DummyWriter : tydisasm.ByteProvider, IOutputFile
    {
        List<byte> text = new List<byte>();
        List<byte> data = new List<byte>();
        List<byte> rodata = new List<byte>();

        public IList<byte> GetText()
        {
            return text;
        }

        public IList<byte> GetData()
        {
            return data;
        }

        public IList<byte> GetRodata()
        {
            return rodata;
        }

        public void AlignText(int a)
        { }

        public void AlignData(int a)
        { }

        public void AlignRodata(int a)
        { }

        public void AddTextSymbol(int offset, string name, bool local_only, bool is_func, bool is_weak)
        { }

        public void AddDataSymbol(int offset, string name, bool is_weak)
        { }

        public void AddRodataSymbol(int offset, string name, bool is_weak)
        { }

        public void AddTextRelocation(int offset, string name, uint rel_type, long value)
        { }

        public void AddDataRelocation(int offset, string name, uint rel_type, long value)
        { }

        public void AddRodataRelocation(int offset, string name, uint rel_type, long value)
        { }

        public void SetEntryPoint(string name)
        { }

        public void Write(System.IO.Stream output)
        { }

        public void AddStaticClassPointer(string static_name, string typeinfo_name)
        { }

        public void DumpText(System.IO.TextWriter output)
        {
            foreach (byte b in text)
            {
                output.Write(b.ToString("X2"));
                output.Write(" ");
            }
            text.Clear();
        }

        int offset = 0;
        int line_offset = 0;

        public override byte GetNextByte()
        {
            byte ret = text[offset];
            offset++;
            return ret;
        }

        public bool MoreToRead
        {
            get
            {
                if (offset < text.Count)
                    return true;
                return false;
            }
        }

        public void LineStart()
        {
            line_offset = offset;
        }

        public override bool ProvidesCurPC()
        {
            return true;
        }

        public override ulong GetCurPC()
        {
            return (ulong)line_offset;
        }

        public override ulong Offset { get { return (ulong)offset; } }
    }
}
