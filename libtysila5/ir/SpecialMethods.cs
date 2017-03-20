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
        public int throw_;
        public int try_enter;
        public int catch_enter;
        public int leave;
        public int rethrow;
        public int strlen;
        public int wcslen;
        public int inst_Rv_s;

        public int string_ci;
        public int string_Zc;
        public int string_Pcii;
        public int string_Pa;
        public int string_Zcii;
        public int string_Pc;
        public int string_PaiiEncoding;
        public int string_Paii;

        MetadataStream corlib;

        List<byte> b = new List<byte>();

        public override uint GetIntEntry(int table_id, int row, int col)
        {
            return corlib.GetIntEntry(table_id, row, col);
        }

        public SpecialMethods(metadata.MetadataStream m)
        {

            corlib = m.al.GetAssembly("mscorlib");
            var i = corlib.GetTypeSpec("System", "Int32");
            var I = corlib.GetTypeSpec("System", "IntPtr");
            var o = corlib.GetSimpleTypeSpec(0x1c);
            var s = corlib.GetSimpleTypeSpec(0x0e);
            var a = corlib.GetSimpleTypeSpec(0x0a);
            var Pa = a.Pointer;
            var c = corlib.GetSimpleTypeSpec(0x03);
            var Pc = c.Pointer;
            var Zc = c.SzArray;

            gcmalloc = CreateMethodSignature(b, I,
                new metadata.TypeSpec[] { i });
            castclassex = CreateMethodSignature(b, o,
                new TypeSpec[] { I, I, i });
            throw_ = CreateMethodSignature(b, null,
                new TypeSpec[] { o });
            try_enter = CreateMethodSignature(b, null,
                new TypeSpec[] { I });
            catch_enter = CreateMethodSignature(b, null,
                new TypeSpec[] { I });
            leave = CreateMethodSignature(b, null,
                new TypeSpec[] { I });
            rethrow = CreateMethodSignature(b, null,
                new TypeSpec[] { });
            strlen = CreateMethodSignature(b, I,
                new TypeSpec[] { Pa });
            wcslen = CreateMethodSignature(b, I,
                new TypeSpec[] { Pc });

            inst_Rv_s = CreateMethodSignature(b, null,
                new TypeSpec[] { s }, true);

            string_ci = CreateMethodSignature(null,
                new TypeSpec[] { c, i }, true);
            string_Zc = CreateMethodSignature(null,
                new TypeSpec[] { Zc }, true);
            string_Pcii = CreateMethodSignature(null,
                new TypeSpec[] { Pc, i, i }, true);
            string_Pa = CreateMethodSignature(null,
                new TypeSpec[] { Pa }, true);
            string_Zcii = CreateMethodSignature(null,
                new TypeSpec[] { Zc, i, i }, true);
            string_Pc = CreateMethodSignature(null,
                new TypeSpec[] { Pc }, true);
            string_PaiiEncoding = CreateMethodSignature(null,
                new TypeSpec[] { Pa, i, i, corlib.GetTypeSpec("System.Text", "Encoding") }, true);
            string_Paii = CreateMethodSignature(null,
                new TypeSpec[] { Pa, i, i }, true);

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
            if(ts == null)
            {
                tmp.Add(0x01);
                return;
            }

            switch(ts.stype)
            {
                case TypeSpec.SpecialType.None:
                    if (ts.m.is_corlib == false)
                        throw new NotSupportedException();
                    var stype = ts.m.simple_type_idx[ts.tdrow];
                    if (stype == -1)
                    {
                        if (ts.IsValueType)
                            tmp.Add(0x11);
                        else
                            tmp.Add(0x12);

                        // create typedef pointer
                        var tok = corlib.MakeCodedIndexEntry(
                            tid_TypeDef,
                            ts.tdrow,
                            TypeDefOrRef);
                        CompressInt(tmp, tok);
                    }
                    tmp.Add((byte)stype);
                    break;
                case TypeSpec.SpecialType.Ptr:
                    tmp.Add(0x0f);
                    CreateTypeSignature(tmp, ts.other);
                    break;
                case TypeSpec.SpecialType.MPtr:
                    tmp.Add(0x10);
                    CreateTypeSignature(tmp, ts.other);
                    break;
                case TypeSpec.SpecialType.SzArray:
                    tmp.Add(0x1d);
                    CreateTypeSignature(tmp, ts.other);
                    break;
                default:
                    throw new NotImplementedException();
            }
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

        private void CompressInt(List<byte> b, uint v)
        {
            unchecked
            {
                if (v <= 127)
                {
                    b.Add((byte)v);
                }
                else if (v <= 0x3fff)
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
