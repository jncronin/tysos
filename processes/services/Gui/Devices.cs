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
using tysos;

namespace gui
{
    partial class gui
    {
        List<tysos.ServerObject> displays = new List<tysos.ServerObject>();
        List<tysos.ServerObject> inputs = new List<tysos.ServerObject>();

        public void RegisterDisplay(string path)
        {
            displays.Add(GetServer(path));
        }

        public void RegisterInput(string path)
        {
            inputs.Add(GetServer(path));
        }

        private ServerObject GetServer(string path)
        {
            // Open the file and get its 'server' property
            System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                ") called");

            var ret = vfs.OpenFile(path, System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite,
                System.IO.FileOptions.None);
            Syscalls.SchedulerFunctions.Block(ret);
            tysos.lib.File f = ret.Result;

            if(f == null || f.Error != tysos.lib.MonoIOError.ERROR_SUCCESS)
            {
                System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                    ") failed - could not open file");
                return null;
            }

            var p = f.GetPropertyByName("server");
            vfs.CloseFile(f);
            if(p == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                    ") failed - no 'server' property found");
                return null;
            }

            var s = p.Value as tysos.ServerObject;
            if(s == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                    ") failed - 'server' property is not of type ServerObject");
                return null;
            }

            System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                ") succeeded");
            return s;
        }
    }
}
