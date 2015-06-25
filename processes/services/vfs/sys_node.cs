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

namespace vfs
{
    public class fixed_text_file : FileSystemObject
    {
        string _contents = "";
        protected internal virtual string contents { get { return _contents; } }

        internal fixed_text_file(string Contents, string _name, DirectoryFileSystemObject Parent) : base(_name, Parent) { _contents = Contents; }
        protected fixed_text_file(string _name, DirectoryFileSystemObject Parent) : base(_name, Parent) { }

        public override tysos.IFile Open(System.IO.FileAccess access, out tysos.lib.MonoIOError error)
        {
            if (access != System.IO.FileAccess.Read)
            {
                error = tysos.lib.MonoIOError.ERROR_ACCESS_DENIED;
                return null;
            }

            error = tysos.lib.MonoIOError.ERROR_SUCCESS;
            return new handle(this);
        }

        class handle : tysos.IFile, tysos.IInputStream
        {
            fixed_text_file parent;
            int offset = 0;

            internal handle(fixed_text_file p) { parent = p; }

            public tysos.IInputStream GetInputStream()
            {
                return this;
            }

            public tysos.IOutputStream GetOutputStream()
            {
                return null;
            }

            public int Read(byte[] dest, int dest_offset, int count)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("fixed_text_file.handle.Read: called (dest_offset = " + dest_offset.ToString() + ", count = " + count.ToString() + "\n");
                int max_len = parent.contents.Length;
                if (offset >= max_len)
                    return 0;

                int to_read = max_len - offset;
                if (to_read > count)
                    to_read = count;

                tysos.Syscalls.DebugFunctions.DebugWrite("fixed_text_file.handle.Read: returning:");
                for (int i = 0; i < to_read; i++)
                {
                    tysos.Syscalls.DebugFunctions.DebugWrite(" " + ((byte)parent.contents[offset + i]).ToString());
                    dest[dest_offset + i] = (byte)parent.contents[offset + i];
                }
                tysos.Syscalls.DebugFunctions.DebugWrite("\n");

                offset += to_read;
                return to_read;
            }

            public int DataAvailable(int timeout)
            {
                int max_len = parent.contents.Length;
                if (offset >= max_len)
                    return 0;
                else
                    return max_len - offset;
            }
        }
    }

    class sys_node : DirectoryFileSystemObject
    {
        public sys_node(DirectoryFileSystemObject Parent) : base("sys", Parent)
        {
            DateTime comp_time = libsupcs.TysosModule.ReinterpretAsTysosModule(typeof(tysos.Program).Module).CompileTime;
            children.Add(new fixed_text_file("Tysos v0.2.0\n" + "Compiled " + comp_time.ToLongDateString() + " " + comp_time.ToLongTimeString() + "\n", "info", this));
        }
    }
}
