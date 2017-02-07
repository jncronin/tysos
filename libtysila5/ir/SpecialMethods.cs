/* Copyright (C) 2016 by John Cronin
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
using libtysila5.cil;
using metadata;

namespace libtysila5.ir
{
    internal class SpecialMethods : metadata.MetadataStream
    {
        public int gcmalloc;
        public int castclassex;

        List<byte> b = new List<byte>();

        public SpecialMethods(metadata.MetadataStream m)
        {

            var corlib = m.al.GetAssembly("mscorlib");
            var i4 = corlib.GetTypeSpec("System", "Int32");
            var i = corlib.GetTypeSpec("System", "IntPtr");

            gcmalloc = CreateMethodSignature(b, i,
                new metadata.TypeSpec[] { i4 });
            castclassex = CreateMethodSignature(b, i,
                new TypeSpec[] { i, i, i4 });

            sh_blob = new BlobStream(b);

            al = m.al;
        }

        internal int CreateMethodSignature(TypeSpec rettype, TypeSpec[] ps, bool has_this = false)
        {
            return CreateMethodSignature(b, rettype, ps, has_this);
        }

        int CreateMethodSignature(List<byte> b, TypeSpec rettype, TypeSpec[] ps, bool has_this = false)
        {
            List<byte> tmp = new List<byte>();

            if (has_this)
                tmp.Add(0x20);
            else
                tmp.Add(0x0);

            CompressInt(tmp, ps.Length);
            CreateTypeSignature(tmp, rettype);
            foreach (var p in ps)
                CreateTypeSignature(tmp, p);

            int ret = b.Count;

            CompressInt(b, tmp.Count);
            b.AddRange(tmp);

            return ret;
        }

        private void CreateTypeSignature(List<byte> tmp, TypeSpec ts)
        {
            if (ts.m.is_corlib == false)
                throw new NotSupportedException();
            var stype = ts.m.simple_type_idx[ts.tdrow];
            if (stype == -1)
                throw new NotSupportedException();
            tmp.Add((byte)stype);
        }

        private void CompressInt(List<byte> b, int v)
        {
            unchecked
            {
                if (v >= -64 && v <= 63)
                {
                    b.Add((byte)v);
                }
                else if (v >= -8192 && v <= 8191)
                {
                    b.Add((byte)(((v >> 8) & 0xff) | 0x80));
                    b.Add((byte)(v & 0xff));
                }
                else
                {
                    b.Add((byte)(((v >> 24) & 0xff) | 0xc0));
                    b.Add((byte)((v >> 16) & 0xff));
                    b.Add((byte)((v >> 8) & 0xff));
                    b.Add((byte)(v & 0xff));
                }
            }
        }

        class BlobStream : metadata.PEFile.StreamHeader
        {
            public BlobStream(IList<byte> arr)
            {
                di = new metadata.ArrayInterface(arr);
            }
        }
    }
}
