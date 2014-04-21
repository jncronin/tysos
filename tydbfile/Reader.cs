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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace tydbfile
{
    partial class TyDbFile
    {
        static int ReadInt(Stream s) { return ReadInt(s, -1); }
        static int ReadInt(Stream s, int offset)
        {
            if (offset != -1) s.Seek((long)offset, SeekOrigin.Begin);
            byte[] buf = new byte[4];
            s.Read(buf, 0, buf.Length);
            return BitConverter.ToInt32(buf, 0);
        }

        static uint ReadUInt(Stream s) { return ReadUInt(s, -1); }
        static uint ReadUInt(Stream s, int offset)
        {
            if (offset != -1) s.Seek((long)offset, SeekOrigin.Begin);
            byte[] buf = new byte[4];
            s.Read(buf, 0, buf.Length);
            return BitConverter.ToUInt32(buf, 0);
        }

        static ulong ReadULong(Stream s) { return ReadULong(s, -1); }
        static ulong ReadULong(Stream s, int offset)
        {
            if (offset != -1) s.Seek((long)offset, SeekOrigin.Begin);
            byte[] buf = new byte[8];
            s.Read(buf, 0, buf.Length);
            return BitConverter.ToUInt64(buf, 0);
        }

        static int reader_strings_offset = 0;
        static Stream reader_s = null;
        static string reader_get_string_from_id(int id)
        {
            long pos = reader_s.Position;
            reader_s.Seek((long)(reader_strings_offset + id), SeekOrigin.Begin);

            byte b = (byte)reader_s.ReadByte();
            List<byte> buf = new List<byte>();

            while (b != 0)
            {
                buf.Add(b);
                b = (byte)reader_s.ReadByte();
            }

            string s = Encoding.UTF8.GetString(buf.ToArray());
            reader_s.Seek(pos, SeekOrigin.Begin);
            return s;
        }

        static string ReadString(Stream s) { return ReadString(s, -1); }
        static string ReadString(Stream s, int offset)
        {
            return reader_get_string_from_id(ReadInt(s, offset));
        }
        
        public static TyDbFile Read(Stream s)
        {
            TyDbFile ret = new TyDbFile();
            reader_s = s;

            uint magic = ReadUInt(s);
            if (magic != 0x42445954)
                throw new Exception("Invalid magic number");

            uint version = ReadUInt(s);
            if (version != 0x00000001)
                throw new Exception("Invalid version");

            int funcs_offset = ReadInt(s);
            int lines_offset = ReadInt(s);
            int varargs_offset = ReadInt(s);
            reader_strings_offset = ReadInt(s);
            int locs_offset = ReadInt(s);
            int func_count = ReadInt(s);
            ret.CompiledFileName = ReadString(s);

            s.Seek((long)funcs_offset, SeekOrigin.Begin);
            for (int i = 0; i < func_count; i++)
            {
                long f_offset = s.Position;

                libtysila.tydb.Function f = new libtysila.tydb.Function();
                f.MetadataFileName = ReadString(s);
                f.MetadataToken = ReadUInt(s);
                f.MangledName = ReadString(s);
                f.TextOffset = ReadUInt(s);

                int line_start = ReadInt(s);
                int line_count = ReadInt(s);
                int var_start = ReadInt(s);
                int var_count = ReadInt(s);
                int arg_start = ReadInt(s);
                int arg_count = ReadInt(s);

                for (int j = 0; j < line_count; j++)
                {
                    s.Seek((long)(lines_offset + line_start + j * 8), SeekOrigin.Begin);

                    libtysila.tydb.Line l = new libtysila.tydb.Line();
                    l.ILOffset = ReadInt(s);
                    l.CompiledOffset = ReadInt(s);
                    f.Lines.Add(l);

                    if(!f.compiled_to_il.ContainsKey(l.CompiledOffset))
                        f.compiled_to_il[l.CompiledOffset] = l.ILOffset;
                }

                for (int j = 0; j < var_count; j++)
                {
                    s.Seek((long)(varargs_offset + var_start + j * 8), SeekOrigin.Begin);

                    libtysila.tydb.VarArg v = new libtysila.tydb.VarArg();
                    v.Name = ReadString(s);
                    v.Location = ReadLocation(s, locs_offset, ReadInt(s));
                    f.Vars.Add(v);
                }

                for (int j = 0; j < arg_count; j++)
                {
                    s.Seek((long)(varargs_offset + arg_start + j * 8), SeekOrigin.Begin);

                    libtysila.tydb.VarArg a = new libtysila.tydb.VarArg();
                    a.Name = ReadString(s);
                    a.Location = ReadLocation(s, locs_offset, ReadInt(s));
                    f.Args.Add(a);
                }

                ret.Functions.Add(f);

                s.Seek(f_offset + 40, SeekOrigin.Begin);
            }

            return ret;
        }

        private static libtysila.tydb.Location ReadLocation(Stream s, int locs_offset, int loc_id)
        {
            libtysila.tydb.Location l = new libtysila.tydb.Location();
            long pos = s.Position;

            s.Seek((long)(locs_offset + loc_id), SeekOrigin.Begin);
            int loc_type = ReadInt(s);

            switch (loc_type)
            {
                case 0:
                    l.Type = libtysila.tydb.Location.LocationType.Register;
                    l.RegisterName = ReadString(s);
                    l.Length = s.ReadByte();
                    break;
                case 1:
                    l.Type = libtysila.tydb.Location.LocationType.Memory;
                    l.MemoryLocation = ReadULong(s);
                    l.Length = s.ReadByte();
                    break;
                case 2:
                    {
                        l.Type = libtysila.tydb.Location.LocationType.ContentsOfLocation;
                        int loc_id_2 = ReadInt(s);
                        l.Offset = (int)ReadULong(s);
                        l.Length = s.ReadByte();
                        l.ContentsOf = ReadLocation(s, locs_offset, loc_id_2);
                        break;
                    }
                default:
                    throw new NotSupportedException();
            }

            s.Seek(pos, SeekOrigin.Begin);
            return l;
        }
    }
}
