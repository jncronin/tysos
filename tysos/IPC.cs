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

    /** <summary>A message with sufficient information to execute a remote procedure call</summary> */
    public unsafe class RPCMessage : IPCMessage
    {
        public void* mptr;
        public object[] args;
        public void* rtype;
        public uint flags;

        public Event result;
        public bool EventSetsOnReturn = true;
    }

    unsafe class IPC
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [libsupcs.ReinterpretAsMethod]
        public static extern IPCMessage ReinterpretAsIPCMessage(IntPtr addr);

        // The actual ring buffer
        Collections.RingBuffer<IntPtr> rb;

        internal static bool InitIPC(Process p)
        {
            if (p.ipc != null)
                return false;

            Virtual_Regions.Region ipc_region = Program.arch.VirtualRegions.AllocRegion(Program.arch.PageSize,
                Program.arch.PageSize, p.name.ToString() + " IPC", 0, Virtual_Regions.Region.RegionType.IPC, true);

            if (ipc_region == null)
                return false;

            p.ipc_region = ipc_region;
            p.ipc = new IPC();
            p.ipc.rb = new Collections.RingBuffer<IntPtr>((void*)ipc_region.start, (int)ipc_region.length);
            p.ipc.owning_process = p;
            p.ipc.ready = true;

            return true;
        }

        private object lock_obj = new object();
        internal Process owning_process;

        internal IPCMessage PeekMessage()
        {
            if (!ready)
                return null;

            IntPtr ptr;
            if(rb.Peek(out ptr) == false)
            {
                return null;
            }
            else
            {
                return ReinterpretAsIPCMessage(ptr);
            }
        }

        internal IPCMessage ReadMessage()
        {
            if (!ready)
                return null;

            IntPtr ptr;
            if(rb.Dequeue(out ptr) == false)
            {
                return null;
            }
            else
            {
                return ReinterpretAsIPCMessage(ptr);
            }
        }

        internal bool SendMessage(IPCMessage message)
        {
            if (!ready)
                return false;

            return rb.Enqueue((IntPtr)libsupcs.CastOperations.ReinterpretAsPointer(message));
        }

        bool ready;
    }
}
