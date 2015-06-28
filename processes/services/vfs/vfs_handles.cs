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
using tysos.Messages;

namespace vfs
{
    partial class vfs
    {
        class open_handle
        {
            internal tysos.IFile handle;
            internal tysos.Process process;
        }

        List<open_handle> open_handles = new List<open_handle>();

        public tysos.IFile OpenFile(string path, System.IO.FileMode mode, System.IO.FileAccess access,
            System.IO.FileShare share, System.IO.FileOptions options, out tysos.lib.MonoIOError error)
        {
            vfsMessageTypes.OpenFileMessage ofm = new vfsMessageTypes.OpenFileMessage();
            ofm.path = path;
            ofm.mode = mode;
            ofm.access = access;
            ofm.share = share;
            ofm.options = options;

            tysos.Syscalls.IPCFunctions.SendMessage(p_vfs, new tysos.IPCMessage { Type = vfsMessageTypes.OPEN, Message = ofm });
            tysos.Syscalls.SchedulerFunctions.Block(ofm.completed);

            error = ofm.error;
            return ofm.handle;
        }

        public bool CloseFile(tysos.IFile handle, out tysos.lib.MonoIOError error)
        {
            vfsMessageTypes.OpenFileMessage ofm = new vfsMessageTypes.OpenFileMessage();
            ofm.handle = handle;

            tysos.Syscalls.IPCFunctions.SendMessage(p_vfs, new tysos.IPCMessage { Type = vfsMessageTypes.CLOSE, Message = ofm });
            tysos.Syscalls.SchedulerFunctions.Block(ofm.completed);

            error = ofm.error;
            if (error != tysos.lib.MonoIOError.ERROR_SUCCESS)
                return false;
            return true;
        }

        tysos.IFile _OpenFile(string path, System.IO.FileMode mode, System.IO.FileAccess access,
            System.IO.FileShare share, System.IO.FileOptions options, out tysos.lib.MonoIOError error, tysos.Process from_proc)
        {
            List<FileSystemObject> fsos = GetFSO(path);
            if (fsos.Count != 1)
            {
                error = tysos.lib.MonoIOError.ERROR_FILE_NOT_FOUND;
                return null;
            }

            FileSystemObject fso = fsos[0];
            tysos.lib.MonoIOError err;
            tysos.IFile handle = fso.Open(access, out err);

            if (err == tysos.lib.MonoIOError.ERROR_SUCCESS)
            {
                open_handle h = new open_handle();
                h.handle = handle;
                h.process = from_proc;
                open_handles.Add(h);
            }

            error = err;
            return handle;
        }

        void _CloseFile(tysos.IFile handle, out tysos.lib.MonoIOError error, tysos.Process from_proc)
        {
            for (int i = 0; i < open_handles.Count; i++)
            {
                if ((open_handles[i].handle == handle) && (open_handles[i].process == from_proc))
                {
                    open_handles.RemoveAt(i);
                    error = tysos.lib.MonoIOError.ERROR_SUCCESS;
                    return;
                }
            }

            error = tysos.lib.MonoIOError.ERROR_INVALID_HANDLE;
        }
    }
}
