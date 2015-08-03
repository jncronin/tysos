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

            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: device: " +
                protocol + ", mount_point: " +
                path.FullPath + ", src: " + src + "\n");

            if (mounts.ContainsKey((PathPart)path))
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mount point " + path.FullPath + " is already mounted\n");
                return false;
            }

            /* Open source file */
            tysos.lib.File f_src = OpenFile(src, System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite,
                System.IO.FileOptions.None);
            if(f_src == null || f_src.Error != tysos.lib.MonoIOError.ERROR_SUCCESS)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: couldn't open source: " + src + "\n");
                return false;
            }

            /* See if the requested file system process is already running */
            tysos.Process p = tysos.Syscalls.ProcessFunctions.GetProcessByName(protocol);
            if(p == null)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: driver " + protocol + " not running.  Starting it...\n");
                tysos.lib.File f = OpenFile("/modules/" + protocol, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.Read,
                    System.IO.FileOptions.None);
                if(f.Error != tysos.lib.MonoIOError.ERROR_SUCCESS)
                    throw new Exception("failed to open /modules/" + protocol + ": " +
                        f.Error.ToString());

                p = tysos.Process.CreateProcess(f, protocol, new object[] { });
                p.Start();
            }
            else
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: driver " + protocol + " already running\n");
            }

            // TODO: create a special event class that waits for p.MessageServer to be non-null
            //  or for a timeout then handle failure
            while (p.MessageServer == null) ;

            // Invoke the process to create a new handler for the new mount
            tysos.ServerObject fs =
                p.MessageServer.Invoke("CreateFSHandler", new object[] { f_src },
                    new Type[] { typeof(tysos.lib.File) }) as tysos.ServerObject;

            if (fs == null)
                throw new Exception("CreateFSHandler failed");

            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mounting " + fs.GetType().FullName + " to " + path.FullPath + "\n");

            mounts[(PathPart)path] = fs;

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

            if (mounts.ContainsKey((PathPart)p))
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mount point " + p.FullPath + " is already mounted\n");
                return false;
            }

            mounts[(PathPart)p] = device;
            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: Mount: mounting " + device.GetType().FullName + " to " + p.FullPath + "\n");

            return true;
        }
    }
}
