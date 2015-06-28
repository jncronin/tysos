/* Copyright (C) $year$ by $username$
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

namespace $safeprojectname$
{
    class Program
    {
        static void Main(string[] args)
        {
            /* Add any specific startup code here */


            /* The main message loop */
            while (true)
            {
                tysos.IPCMessage msg = null;
                do
                {
                    msg = tysos.Syscalls.IPCFunctions.ReadMessage();

                    if (msg != null)
                        HandleMessage(msg);
                } while (msg != null);

                tysos.Syscalls.SchedulerFunctions.Block();
            }
        }

        private void HandleMessage(tysos.IPCMessage msg)
        {
            switch (msg.Type)
            {
#region VFS messages
                case vfsMessageTypes.GET_ATTRIBUTES:
                    {
                        vfsMessageTypes.FileAttributesMessage fam = msg.Message as vfsMessageTypes.FileAttributesMessage;
                        if (fam != null)
                        {
                            // Handle the message
                            //fam.attributes = _GetFileAttributes(ref fam.path);
                            fam.completed.Set();
                        }
                    }
                    break;

                case vfsMessageTypes.GET_FILE_SYSTEM_ENTRIES:
                    {
                        vfsMessageTypes.FileSystemEntriesMessage fsem = msg.Message as vfsMessageTypes.FileSystemEntriesMessage;
                        if (fsem != null)
                        {
                            // Handle the message
                            //fsem.files = _GetFileSystemEntries(fsem.path, fsem.path_with_pattern, fsem.attrs, fsem.mask);
                            fsem.completed.Set();
                        }
                    }
                    break;

                case vfsMessageTypes.OPEN:
                    {
                        vfsMessageTypes.OpenFileMessage ofm = msg.Message as vfsMessageTypes.OpenFileMessage;
                        if (ofm != null)
                        {
                            // Handle the message
                            //ofm.handle = _OpenFile(ofm.path, ofm.mode, ofm.access, ofm.share, ofm.options, out ofm.error, msg.Source.owning_process);
                            ofm.completed.Set();
                        }
                    }
                    break;

                case vfsMessageTypes.CLOSE:
                    {
                        vfsMessageTypes.OpenFileMessage ofm = msg.Message as vfsMessageTypes.OpenFileMessage;
                        if (ofm != null)
                        {
                            // Handle the message
                            //_CloseFile(ofm.handle, out ofm.error, msg.Source.owning_process);
                            ofm.completed.Set();
                        }
                    }
                    break;

                case vfsMessageTypes.MOUNT:
                    {
                        vfsMessageTypes.MountMessage mm = msg.Message as vfsMessageTypes.MountMessage;
                        if (mm != null)
                        {
                            // Handle the message
                            //_Mount(mm.mount_point, mm.device);
                            mm.completed.Set();
                        }
                    }
                    break;
#endregion

#region Device Messages
                case deviceMessageTypes.INIT_DEVICE:
                    {
                        deviceMessageTypes.InitDeviceMessage idm = msg.Message as deviceMessageTypes.InitDeviceMessage;
                        if (idm != null)
                        {
                            // Handle the message
                            //_InitDevice(idm.Resources, idm.Node, ref idm.Device);
                            idm.completed.Set();
                        }
                    }
                    break;
#endregion
            }
        }
    }
}
