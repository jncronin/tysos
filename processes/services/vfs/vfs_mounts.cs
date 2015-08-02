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
        public bool Mount(string mount_path, string src, string protocol)
        {
            if (mount_path == null)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mount_path is null\n");
                return false;
            }

            Path path = GetPath(mount_path);

            mount_path = path.FullPath;
            if (mounts.ContainsKey(mount_path))
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mount point " + mount_path + " is already mounted\n");
                return false;
            }

            /* See if the requested file system process is already running */
            tysos.Process p = tysos.Syscalls.ProcessFunctions.GetProcessByName(protocol);
            if(p == null)
            {
                tysos.lib.File f = OpenFile("/modules/" + protocol, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.Read,
                    System.IO.FileOptions.None);
                if(f.Error != tysos.lib.MonoIOError.ERROR_SUCCESS)
                    throw new Exception("failed to open /modules/" + protocol + ": " +
                        f.Error.ToString());

                p = tysos.Process.CreateProcess(f, protocol, new object[] { });
                p.Start();
            }

            // TODO: create a special event class that waits for p.MessageServer to be non-null
            //  or for a timeout then handle failure
            while (p.MessageServer == null) ;

            // Invoke the process to create a new handler for the new mount
            tysos.ServerObject fs =
                p.MessageServer.Invoke("CreateFSHandler", new object[] { src }) as tysos.ServerObject;

            if (fs == null)
                throw new Exception("CreateFSHandler failed");

            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mounting " + fs.GetType().FullName + " to " + mount_path + "\n");

            mounts[mount_path] = fs;

            return true;
        }

        public bool Mount(string mount_path, tysos.ServerObject device)
        {
            if (mount_path == null)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mount_path is null\n");
                return false;
            }

            Path p = GetPath(mount_path);

            mount_path = p.FullPath;
            if (mounts.ContainsKey(mount_path))
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mount point " + mount_path + " is already mounted\n");
                return false;
            }

            mounts[mount_path] = device;
            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mounting " + device.GetType().FullName + " to " + mount_path + "\n");

            return true;
        }
    }
}
