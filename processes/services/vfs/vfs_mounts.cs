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
    partial class vfs
    {
        void _Mount(string mount_path, tysos.IDirectory device)
        {
            if (device == null)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: _Mount: device is null\n");
                return;
            }

            List<FileSystemObject> fsos = GetFSO(mount_path);
            if (fsos.Count == 0)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: _Mount: mount_point " + mount_path + " not found\n");
                return;
            }
            if (fsos.Count > 1)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: _Mount: multiple matches for mount point " + mount_path + "\n");
                return;
            }

            FileSystemObject fso = fsos[0];
            mount_path = fso.FullPath;
            if (mounts.ContainsKey(mount_path))
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: _Mount: mount point " + mount_path + " is already mounted\n");
                return;
            }

            if (fso.parent == null)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: _Mount: trying to remount root - not allowed\n");
                return;
            }

            throw new NotImplementedException();
            /*device.parent = fso.parent;
            device.name = fso.name;

            mounts.Add(mount_path, device);
            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: _Mount: successful mount to " + mount_path + "\n");*/
        }
    }
}
