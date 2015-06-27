/* Copyright (C) 2015 by John Cronin
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

namespace vfs
{
    class vmem_file : FileSystemObject
    {
        tysos.RangeResource vm;
        internal vmem_file(string Name, tysos.RangeResource vmem,
            DirectoryFileSystemObject Parent) : base(Name, Parent)
        {
            vm = vmem;
        }

        class vmem_file_handle : tysos.IFile, tysos.IInputStream, tysos.IOutputStream
        {
            int offset;
            long wroffset;
            tysos.RangeResource vmem;

            internal vmem_file_handle(tysos.RangeResource vm) { vmem = vm; }

            long tysos.IInputStream.Position { get { return (long)offset; } }
            long tysos.IOutputStream.Position { get { return wroffset; } }
            public long Length { get { return (long)vmem.Length64; } }
            public void Seek(long position, tysos.SeekPosition whence) { throw new NotImplementedException(); }

            public tysos.IInputStream GetInputStream()
            {
                return this;
            }

            public tysos.IOutputStream GetOutputStream()
            {
                return this;
            }

            public int Read(byte[] dest, int dest_offset, int count)
            {
                if (offset < 0)
                    return 0;
                if (count < 0)
                    return 0;
                if ((ulong)offset > vmem.Length64)
                    return 0;
                ulong to_read = vmem.Length64 - (ulong)offset;
                if (to_read > (ulong)count)
                    to_read = (ulong)count;

                for (ulong i = 0; i < to_read; i++)
                    dest[dest_offset + (int)i] = (byte)vmem.Read(vmem.Addr64 + (ulong)offset + i, 1);

                offset += (int)to_read;
                return (int)to_read;
            }

            public int DataAvailable(int timeout)
            {
                ulong max_len = vmem.Length64;
                if (offset < 0)
                    return 0;
                if ((ulong)offset >= max_len)
                    return 0;
                return (int)(max_len - (ulong)offset);
            }

            public void Write(byte[] src, int src_offset, int count)
            {
                if (offset < 0)
                    return;
                if (count < 0)
                    return;
                if ((ulong)offset > vmem.Length64)
                    return;
                ulong to_write = vmem.Length64 - (ulong)offset;
                if (to_write > (ulong)count)
                    to_write = (ulong)count;

                for (ulong i = 0; i < to_write; i++)
                    vmem.Write(vmem.Addr64 + (ulong)offset + i, 1, src[src_offset + (int)i]);

                offset += (int)to_write;
            }
        }

        public override tysos.IFile Open(System.IO.FileAccess access, out tysos.lib.MonoIOError error)
        {
            if(access != System.IO.FileAccess.Read)
            {
                error = tysos.lib.MonoIOError.ERROR_ACCESS_DENIED;
                return null;
            }

            error = tysos.lib.MonoIOError.ERROR_SUCCESS;
            return new vmem_file_handle(vm);
        }
    }
}
