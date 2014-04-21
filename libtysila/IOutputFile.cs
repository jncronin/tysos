/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace libtysila
{
    public interface IOutputFile
    {
        IList<byte> GetText();
        IList<byte> GetData();
        IList<byte> GetRodata();

        void AlignText(int a);
        void AlignData(int a);
        void AlignRodata(int a);

        void AddTextSymbol(int offset, string name, bool local_only, bool is_func, bool is_weak);
        void AddDataSymbol(int offset, string name);
        void AddRodataSymbol(int offset, string name);

        void AddTextRelocation(int offset, string name, uint rel_type, int value);
        void AddDataRelocation(int offset, string name, uint rel_type, int value);
        void AddRodataRelocation(int offset, string name, uint rel_type, int value);

        void AddStaticClassPointer(string static_object_name, string typeinfo_name);

        void SetEntryPoint(string name);

        void Write(System.IO.Stream output);
        void DumpText(System.IO.TextWriter output);
    }
}
