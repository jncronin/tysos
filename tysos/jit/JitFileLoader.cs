/* Copyright (C) 2012 by John Cronin
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

namespace tysos.jit
{
    class JitFileLoader : libtysila.Assembler.FileLoader
    {
        string[] file_extensions = new string[] { "", ".exe", ".dll" };
        class LoadedModule { public ulong base_addr; public ulong length; };

        Dictionary<string, LoadedModule> loaded_mods = new Dictionary<string, LoadedModule>(new Program.MyGenericEqualityComparer<string>());

        public override libtysila.Assembler.FileLoader.FileLoadResults LoadFile(string filename)
        {
            Formatter.Write("stab at ", Program.arch.DebugOutput);
            ulong stab = libsupcs.CastOperations.ReinterpretAsUlong(Program.stab);
            Formatter.Write(stab, "X", Program.arch.DebugOutput);
            Formatter.Write(", vtable: ", Program.arch.DebugOutput);
            unsafe
            {
                Formatter.Write(*(ulong*)stab, "X", Program.arch.DebugOutput);
                Formatter.Write(", ti: ", Program.arch.DebugOutput);
                Formatter.Write(**(ulong**)stab, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
            }

            LoadedModule mod = null;

            if (loaded_mods.ContainsKey(filename))
                mod = loaded_mods[filename];
            else
            {
                foreach (string ext in file_extensions)
                {
                    Multiboot.Module m = Program.find_module(Program.mboot_header.modules, filename + ext);
                    if (m != null)
                    {
                        mod = new LoadedModule { base_addr = Program.map_in(m), length = m.length };
                        Program.stab.Add("metadata_" + filename, mod.base_addr);
                        loaded_mods.Add(filename, mod);
                        break;
                    }
                }
            }

            if (mod == null)
                throw new Exception("Unable to find module: " + filename);

            UnsafeMemoryStream ms = new UnsafeMemoryStream(mod.base_addr, mod.length);
            
            return new FileLoadResults { FullFilename = filename, ModuleName = filename, Stream = ms };
        }
    }

    class UnsafeMemoryStream : System.IO.Stream
    {
        ulong base_addr;
        ulong length;
        ulong pos;

        public UnsafeMemoryStream(ulong BaseAddress, ulong Length)
        {
            base_addr = BaseAddress;
            length = Length;
            if (length > (ulong)long.MaxValue)
                throw new ArgumentException("Length is greater than the maximum value of " + long.MaxValue.ToString());
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            
        }

        public override long Length
        {
            get { return (long)length; }
        }

        public override long Position
        {
            get
            {
                return (long)pos;
            }
            set
            {
                Seek(value, System.IO.SeekOrigin.Begin);          
            }
        }

        public unsafe override int ReadByte()
        {
            return (int)*(byte*)(base_addr + pos++);
        }

        public unsafe override int Read(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < (offset + count); i++)
                buffer[i] = *(byte*)(base_addr + pos++);
            return count;
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            long orig_pos = (long)pos;
            long new_pos = 0;
            switch (origin)
            {
                case System.IO.SeekOrigin.Begin:
                    new_pos = offset;
                    break;
                case System.IO.SeekOrigin.End:
                    new_pos = (long)length + offset;
                    break;
                case System.IO.SeekOrigin.Current:
                    new_pos = orig_pos + offset;
                    break;
                default:
                    throw new NotSupportedException("Invalid origin value");
            }
            if (new_pos < 0)
                throw new Exception("Attempt to seek beyond start of stream");
            if (new_pos >= (long)length)
                throw new Exception("Attempt to seek beyond end of stream");
            pos = (ulong)new_pos;
            return new_pos;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public unsafe override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < (offset + count); i++)
                *(byte*)(base_addr + pos++) = buffer[i];
        }
    }
}
