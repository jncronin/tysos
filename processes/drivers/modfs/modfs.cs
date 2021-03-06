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

namespace modfs
{
    public class modfs_factory : tysos.ServerObject, tysos.Interfaces.IFactory
    {
        static void Main(string[] args)
        {
            modfs_factory m = new modfs_factory();
            m.MessageLoop();
        }

        public RPCResult<tysos.Interfaces.IFileSystem> CreateFSHandler(tysos.lib.File src)
        {
            // Get the modules associated with the handler
            tysos.lib.File.Property m_param =
                src.GetPropertyByName("mods");
            if (m_param == null)
                throw new Exception("src does not contain 'mods' property");

            List<tysos.lib.File.Property> mods = m_param.Value as
                List<tysos.lib.File.Property>;
            if (mods == null)
                throw new Exception("mods is of inappropriate type");

            modfs fs = new modfs(mods);

            // Fork off a separate process to handle this instance of the driver
            tysos.Process p = tysos.Process.CreateProcess("modfs: " + src.Name, 
                new System.Threading.ThreadStart(fs.MessageLoop), new object[] { fs });
            p.Start();

            System.Diagnostics.Debugger.Log(0, "modfs", "Created FS handler\n");
            return fs;
        }
    }

    public class modfs : tysos.ServerObject, tysos.Interfaces.IFileSystem
    {
        public modfs(List<tysos.lib.File.Property> Mods)
        {
            mods = Mods;

            /* Build a cache of the root directory entries */
            children = new List<string>();
            foreach (tysos.lib.File.Property mod in mods)
                children.Add(mod.Name);
        }
        internal List<tysos.lib.File.Property> mods;

        public RPCResult<int> IntProperties(tysos.lib.File f)
        {
            return ((modfs_File)f).intProperties;
        }

        public RPCResult<long> GetLength(tysos.lib.File f)
        {
            return (long)((modfs_File)f).mem.Length64;
        }

        List<string> children;

        public RPCResult<tysos.lib.File> Open(IList<string> path, System.IO.FileMode mode,
            System.IO.FileAccess access, System.IO.FileShare share,
            System.IO.FileOptions options)
        {
            if(path.Count == 0)
            {
                var ret = new tysos.lib.VirtualDirectory(this, "", children,
                    new tysos.lib.File.Property[] {
                        new tysos.lib.File.Property { Name = "class", Value = "fs" }
                    });
            }

            // modfs has only one level
            if (path.Count != 1)
                return new tysos.lib.ErrorFile(tysos.lib.MonoIOError.ERROR_FILE_NOT_FOUND);

            foreach(tysos.lib.File.Property p in mods)
            {
                if(p.Name == path[0])
                {
                    modfs_File f = new modfs_File(this);
                    f.name = p.Name;
                    f.mem = p.Value as tysos.VirtualMemoryResource64;
                    f.Error = tysos.lib.MonoIOError.ERROR_SUCCESS;

                    System.Diagnostics.Debugger.Log(0, "modfs", "Request for " +
                        path[0] + " returning " +
                        f.mem.ToString());
                    return f;
                }
            }

            return new tysos.lib.ErrorFile(tysos.lib.MonoIOError.ERROR_FILE_NOT_FOUND);
        }

        public RPCResult<bool> Close(tysos.lib.File handle)
        {
            return true;
        }

        public RPCResult<int> Read(tysos.lib.File f, long pos, byte[] dest, int dest_offset, int count)
        {
            int bytes_read = 0;
            modfs_File mf = f as modfs_File;

            if (pos < 0)
            {
                f.Error = tysos.lib.MonoIOError.ERROR_READ_FAULT;
                System.Diagnostics.Debugger.Log(0, "modfs", "Read Fault (pos = " + pos.ToString() + ")");
                return -1;
            }

            System.Diagnostics.Debugger.Log(0, "modfs", "Reading from " + mf.name +
                ", pos: " + pos.ToString() +
                ", addr: " + (mf.mem.Addr64 + (ulong)pos).ToString("X") +
                ", length: " + ((long)mf.mem.Length64).ToString());

            while (pos < (long)mf.mem.Length64 && bytes_read < count)
            {
                dest[dest_offset++] = (byte)mf.mem.Read(mf.mem.Addr64 + (ulong)pos, 1);
                pos++;
                bytes_read++;
            }

            return bytes_read;
        }

        public RPCResult<int> Write(tysos.lib.File f, long pos, byte[] dest, int dest_offset, int count)
        {
            throw new NotImplementedException();
        }

        public RPCResult<string> GetName(tysos.lib.File f)
        {
            throw new NotImplementedException();
        }

        public RPCResult<tysos.lib.File.Property> GetPropertyByName(tysos.lib.File f, string name)
        {
            throw new NotImplementedException();
        }

        public RPCResult<tysos.lib.File.Property[]> GetAllProperties(tysos.lib.File f)
        {
            throw new NotImplementedException();
        }
    }

    class modfs_File : tysos.lib.File
    {
        internal modfs_File(modfs device)
        {
            d = device;

            CanRead = true;
            CanWrite = false;
            CanSeek = true;
            CanGrow = false;

            isatty = false;
            pos = 0;

            intProperties = (int)System.IO.FileAttributes.ReadOnly;
        }

        internal string name;
        internal tysos.VirtualMemoryResource64 mem;
        internal int intProperties;
    }
}
