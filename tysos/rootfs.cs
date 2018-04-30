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

namespace tysos
{
    /* The root filesystem
     * 
     * This is read only and exposes access to the kernel through the /system node */

    class rootfs : ServerObject
    {
        internal class rootfs_item
        {
            public string Name;
            public List<lib.File.Property> Props;
        }

        internal List<rootfs_item> items;

        internal rootfs(List<rootfs_item> Items)
        {
            items = Items;
        }

        [libsupcs.AlwaysCompile]
        public tysos.lib.File Open(IList<string> path, System.IO.FileMode mode,
            System.IO.FileAccess access, System.IO.FileShare share,
            System.IO.FileOptions options)
        {
            Formatter.WriteLine("rootfs.Open: path is of type " + path.GetType().ToString(), Program.arch.DebugOutput);
            if (path.Count == 0)
            {
                List<string> children = new List<string>();
                foreach (rootfs_item item in items)
                    children.Add(item.Name);

                lib.File ret = new lib.VirtualDirectory(this, "", children);
                ret.Error = lib.MonoIOError.ERROR_SUCCESS;
                return ret;
            }
            else if (path.Count == 1)
            {
                foreach(rootfs_item item in items)
                {
                    if(item.Name == path[0])
                    {
                        system_node ret = new system_node(this, item.Name, item.Props);
                        ret.Error = lib.MonoIOError.ERROR_SUCCESS;
                        return ret;
                    }
                }
            }

            return new lib.ErrorFile(lib.MonoIOError.ERROR_FILE_NOT_FOUND);
        }

        [libsupcs.AlwaysCompile]
        public bool Close(lib.File handle)
        {
            return true;
        }

        [libsupcs.AlwaysCompile]
        public int Read(tysos.lib.File f, long pos, byte[] dest, int dest_offset, int count)
        {
            f.Error = lib.MonoIOError.ERROR_READ_FAULT;
            return 0;
        }

        [libsupcs.AlwaysCompile]
        public int Write(tysos.lib.File f, long pos, byte[] dest, int dest_offset, int count)
        {
            f.Error = lib.MonoIOError.ERROR_WRITE_FAULT;
            return 0;
        }

        [libsupcs.AlwaysCompile]
        public int IntProperties(tysos.lib.File f)
        {
            return ((lib.VirtualPropertyFile)f).intProperties;
        }

        [libsupcs.AlwaysCompile]
        public tysos.lib.File.Property GetPropertyByName(lib.File f, string name)
        {
            return ((lib.VirtualPropertyFile)f).GetPropertyByName(name);
        }

        class system_node : lib.VirtualPropertyFile
        {
            internal system_node(rootfs device, string name, List<lib.File.Property> props) : base(device, name)
            {
                Props = props;
            }

            internal List<tysos.lib.File.Property> GetProperties()
            { return Props; }
        }
    }
}
