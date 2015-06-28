/* Copyright (C) 2008 - 2011 by John Cronin
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
    /* We implement the IPC buffer as a ring-buffer of object references
     * 
     * Messages are passed as type object, which are actually pointers
     * to objects in the user heap.
     * 
     * We expect the receiving process to know how to interpret the message
     * 
     * Eventually we will implement a security mechanism where some processes
     * will only allow messages from trusted processes.  E.g. disk block device
     * drivers will only accept messages from file system drivers.
     */

    public class IPCMessage
    {
        public Thread Source;
        public int Type;
        public object Message;

        public const int TYPE_UNKNOWN = 0;
        public const int TYPE_STRING = 1;
        public const int TYPE_CLOSE = 2;
    }

    class IPC
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [libsupcs.ReinterpretAsMethod]
        public static extern IPCMessage ReinterpretAsIPCMessage(ulong addr);

        internal static bool InitIPC(Process p)
        {
            if (p.ipc != null)
                return false;

            Virtual_Regions.Region ipc_region = Program.arch.VirtualRegions.AllocRegion(Program.arch.PageSize,
                Program.arch.PageSize, p.name.ToString() + " IPC", 0, Virtual_Regions.Region.RegionType.IPC);

            if (ipc_region == null)
                return false;

            p.ipc_region = ipc_region;
            p.ipc = new IPC();
            p.ipc.start = ipc_region.start;
            p.ipc.end = ipc_region.start + ipc_region.length;
            p.ipc.readpointer = p.ipc.start;
            p.ipc.writepointer = p.ipc.start;
            p.ipc.ready = true;

            return true;
        }

        private object lock_obj = new object();

        internal IPCMessage PeekMessage()
        {
            if (!ready)
                return null;

            lock (lock_obj)
            {
                if (readpointer == writepointer)
                    return null;

                IPCMessage ret;
                unsafe
                {
                    ulong msg = *(ulong*)readpointer;
                    ret = ReinterpretAsIPCMessage(msg);
                }

                return ret;
            }
        }

        internal IPCMessage ReadMessage()
        {
            if (!ready)
                return null;

            lock (lock_obj)
            {
                if (readpointer == writepointer)
                    return null;

                IPCMessage ret;

                unsafe
                {
                    ulong msg = *(ulong*)readpointer;
                    ret = ReinterpretAsIPCMessage(msg);
                }

                readpointer += (ulong)Program.arch.PointerSize;
                if (readpointer >= end)
                    readpointer = start;

                return ret;
            }
        }

        internal bool SendMessage(IPCMessage message)
        {
            if (!ready)
                return false;

            lock (lock_obj)
            {
                /* Detect a buffer overflow condition */
                if (writepointer == (readpointer - (ulong)Program.arch.PointerSize))
                    return false;

                if ((writepointer == (end - (ulong)Program.arch.PointerSize)) &&
                    (readpointer == start))
                    return false;

                ulong msg = libsupcs.CastOperations.ReinterpretAsUlong(message);

                unsafe
                {
                    *(ulong*)writepointer = msg;
                }

                writepointer += (ulong)Program.arch.PointerSize;
                if (writepointer >= end)
                    writepointer = start;

                return true;
            }
        }

        ulong start;
        ulong end;
        ulong readpointer;
        ulong writepointer;
        bool ready;
    }
}
