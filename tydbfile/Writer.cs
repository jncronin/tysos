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
        void write(byte[] buf, int offset, int val)
        {
            byte[] val_b = BitConverter.GetBytes(val);
            for (int i = 0; i < val_b.Length; i++)
                buf[offset + i] = val_b[i];
        }

        void write(byte[] buf, int offset, uint val)
        {
            byte[] val_b = BitConverter.GetBytes(val);
            for (int i = 0; i < val_b.Length; i++)
                buf[offset + i] = val_b[i];
        }

        void write(byte[] buf, int offset, ulong val)
        {
            byte[] val_b = BitConverter.GetBytes(val);
            for (int i = 0; i < val_b.Length; i++)
                buf[offset + i] = val_b[i];
        }

        void write(byte[] buf, int offset, string val)
        { write(buf, offset, GetStringTableOffset(val)); }

        Dictionary<string, int> string_table_offsets = new Dictionary<string, int>();
        List<byte> string_table = new List<byte> { 0 };
        int string_table_offset = 1;

        int GetStringTableOffset(string s)
        {
            if (s == null)
                return 0;

            if (string_table_offsets.ContainsKey(s))
                return string_table_offsets[s];

            string_table_offsets[s] = string_table_offset;

            byte[] enc = Encoding.UTF8.GetBytes(s);
            foreach (byte b in enc)
                string_table.Add(b);
            string_table.Add(0);

            string_table_offset += (enc.Length + 1);

            return string_table_offsets[s];
        }

        public override void Write(Stream s)
        {
            string_table_offsets = new Dictionary<string, int>();
            string_table = new List<byte> { 0 };
            string_table_offset = 1;

            // header
            byte[] header = new byte[36];
            
            // magic 'TYDB'
            header[0] = 0x54;
            header[1] = 0x59;
            header[2] = 0x44;
            header[3] = 0x42;

            // version 0x00000001
            header[4] = 0x01;
            header[5] = 0x00;
            header[6] = 0x00;
            header[7] = 0x00;

            // function table offset - fill in later
            // line table offset - fill in later
            // vararg table offset - fill in later
            // string table offset - fill in later
            // location table offset - fill in later
            // function count - fill in later

            // compiled file name
            write(header, 32, CompiledFileName);


            // functions
            List<byte> functions = new List<byte>();
            List<byte> lines = new List<byte>();
            List<byte> varargs = new List<byte>();
            List<byte> locations = new List<byte>();
            Dictionary<string, int> location_cache = new Dictionary<string, int>();

            foreach (libtysila.tydb.Function f in Functions)
            {
                int var_start = varargs.Count;
                foreach (libtysila.tydb.VarArg var in f.Vars)
                    write(varargs, locations, location_cache, var);
                int arg_start = varargs.Count;
                foreach (libtysila.tydb.VarArg arg in f.Args)
                    write(varargs, locations, location_cache, arg);
                int lines_start = lines.Count;
                foreach (libtysila.tydb.Line line in f.Lines)
                    write(lines, line);               

                byte[] f_buf = new byte[40];

                write(f_buf, 0, f.MetadataFileName);
                write(f_buf, 4, f.MetadataToken);
                write(f_buf, 8, f.MangledName);
                write(f_buf, 12, f.TextOffset);
                write(f_buf, 16, lines_start);
                write(f_buf, 20, f.Lines.Count);
                write(f_buf, 24, var_start);
                write(f_buf, 28, f.Vars.Count);
                write(f_buf, 32, arg_start);
                write(f_buf, 36, f.Args.Count);

                functions.AddRange(f_buf);
            }

            // now work out the offsets of the various sections
            int header_offset = 0;
            int func_offset = header_offset + header.Length;
            int lines_offset = func_offset + functions.Count;
            int varargs_offset = lines_offset + lines.Count;
            int locs_offset = varargs_offset + varargs.Count;
            int strings_offset = locs_offset + locations.Count;

            // fill them in the header
            write(header, 8, func_offset);
            write(header, 12, lines_offset);
            write(header, 16, varargs_offset);
            write(header, 20, strings_offset);
            write(header, 24, locs_offset);
            write(header, 28, Functions.Count);

            // now write the file
            s.Write(header, 0, header.Length);
            s.Write(functions.ToArray(), 0, functions.Count);
            s.Write(lines.ToArray(), 0, lines.Count);
            s.Write(varargs.ToArray(), 0, varargs.Count);
            s.Write(locations.ToArray(), 0, locations.Count);
            s.Write(string_table.ToArray(), 0, string_table.Count);
        }

        private void write(List<byte> lines, libtysila.tydb.Line line)
        {
            byte[] buf = new byte[8];
            write(buf, 0, line.ILOffset);
            write(buf, 4, line.CompiledOffset);
            lines.AddRange(buf);
        }

        private void write(List<byte> varargs, List<byte> locations, Dictionary<string, int> location_cache, libtysila.tydb.VarArg var)
        {
            byte[] va_buf = new byte[8];
            write(va_buf, 0, var.Name);
            write(va_buf, 4, write(locations, location_cache, var.Location));
            varargs.AddRange(va_buf);            
        }

        private int write(List<byte> locations, Dictionary<string, int> location_cache, libtysila.tydb.Location location)
        {
            if(location_cache.ContainsKey(location.ToString()))
                return location_cache[location.ToString()];

            int contents_of = -1;
            if (location.ContentsOf != null)
                contents_of = write(locations, location_cache, location.ContentsOf);

            int loc_id = locations.Count;
            byte[] buf = null;

            switch (location.Type)
            {
                case libtysila.tydb.Location.LocationType.Register:
                    buf = new byte[9];
                    write(buf, 0, 0);
                    write(buf, 4, location.RegisterName);
                    buf[8] = (byte)location.Length;
                    break;
                case libtysila.tydb.Location.LocationType.Memory:
                    buf = new byte[13];
                    write(buf, 0, 1);
                    write(buf, 4, location.MemoryLocation);
                    buf[12] = (byte)location.Length;
                    break;
                case libtysila.tydb.Location.LocationType.ContentsOfLocation:
                    buf = new byte[17];
                    write(buf, 0, 2);
                    write(buf, 4, contents_of);
                    write(buf, 8, (ulong)location.Offset);
                    buf[16] = (byte)location.Length;
                    break;
                default:
                    throw new NotSupportedException();
            }

            location_cache.Add(location.ToString(), loc_id);
            locations.AddRange(buf);
            return loc_id;
        }
    }
}
