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
            internal tysos.lib.File handle;
            internal tysos.Process process;
        }

        List<open_handle> open_handles = new List<open_handle>();

        public RPCResult<tysos.lib.File> OpenFile(string path, System.IO.FileMode mode, System.IO.FileAccess access,
            System.IO.FileShare share, System.IO.FileOptions options)
        {
            Path p = GetPath(path);
            if (p == null || p.device == null)
                return new tysos.lib.ErrorFile(tysos.lib.MonoIOError.ERROR_FILE_NOT_FOUND);

            tysos.lib.File ret = p.device.Open(p.path.path, mode, access, share, options).Sync();

            if(ret == null)
            {
                ret = new tysos.lib.ErrorFile(tysos.lib.MonoIOError.ERROR_GEN_FAILURE);
            }   

            if (ret.Error == tysos.lib.MonoIOError.ERROR_SUCCESS)
            {
                open_handle h = new open_handle();
                h.handle = ret;

                if(SourceThread != null)
                    h.process = SourceThread.owning_process;
                open_handles.Add(h);
            }

            return ret;
        }

        public RPCResult<bool> CloseFile(tysos.lib.File handle)
        {
            handle.Error = tysos.lib.MonoIOError.ERROR_INVALID_HANDLE;

            for (int i = 0; i < open_handles.Count; i++)
            {
                if ((open_handles[i].handle == handle) && 
                    ((open_handles[i].process == null && SourceThread == null) || 
                    (open_handles[i].process == SourceThread.owning_process)))
                {
                    open_handles.RemoveAt(i);
                    handle.Error = tysos.lib.MonoIOError.ERROR_SUCCESS;
                    break;
                }
            }

            if (handle.Error == tysos.lib.MonoIOError.ERROR_SUCCESS)
                return handle.Device.Close(handle).Sync();
            else
                return false;
        }
    }
}
