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

/* This class can be subclassed to create all server objects used in the microkernel.
 * 
 * Methods on it can be called via the 'Invoke' method which will either run them directly
 * if the calling thread == the target thread, or will execute them via a remote procedure
 * call (encapsulated in the InvokeEvent) object.
 * 
 * Invoke performs sychronous calls, InvokeAsync performs asynchronous calls
 * 
 * Typically, a newly created server will perform any needed initialization, then call the
 * MessageLoop() function to respond to messages.  BackgroundProc() can be overloaded to
 * run any additional logic instead of by default blocking when no further messages are
 * received.
 */

namespace tysos
{
    public class RPCFunctionAttribute : libsupcs.AlwaysCompileAttribute
    {

    }

    public abstract class ServerObject
    {
        protected Thread t = null;
        protected Thread SourceThread = null;
        public string MountPath = null;
        public List<string> Tags = new List<string>();

        public RPCMessage CurrentMessage = null;

        public ServerObject()
        {
        }

        public class RPCResult<T> : Event
        {
            public ServerObject Server;
            public static implicit operator RPCResult<T>(T v) => new RPCResult<T> { Result = v, mutex = 1 };

            public delegate object ObjectDelegate(object o, T retval);
            protected ObjectDelegate CallOnSet;
            protected object CallbackObject;

            /* This nested class enforces boxing of potential value types */
            public class Box<U> {
                public static implicit operator U(Box<U> val) => (val == null) ? default(U) : val.v;
                public static implicit operator Box<U>(U val) => new Box<U> { v = val };
                private U v;
            }

            public Box<T> Result;

            public T Sync()
            {
                System.Diagnostics.Debugger.Log(0, null, "RPC Sync begin waiting");
                while (!IsSet)
                {
                    Syscalls.SchedulerFunctions.Block(this);
                }
                System.Diagnostics.Debugger.Log(0, null, "RPC Sync waiting done");
                return Result;
            }

            public void SetCallback(ObjectDelegate meth, object cb_obj)
            {
                lock(this)
                {
                    CallOnSet = meth;
                    CallbackObject = cb_obj;
                }

                if(IsSet)
                {
                    meth(cb_obj, Result);
                }
            }

            public override void Set()
            {
                base.Set();

                lock (this)
                {
                    if (CallOnSet != null)
                        CallOnSet(CallbackObject, Result);
                }
            }
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("invoke")]
        static unsafe void* Invoke(void *mptr, object[] args, void *vtbl_ptr, uint flags)
        {
            if (args.Length < 1 ||
                !(args[0] is ServerObject))
            {
                // Not an invoke call to a ServerObject - just call the method
                return libsupcs.TysosMethod.InternalInvoke(mptr, args, vtbl_ptr, flags);
            }

            var curso = args[0] as ServerObject;

            if (curso.t == null)
            {
                // Wait for us to enter the message loop
                System.Diagnostics.Debugger.Log(0, "ServerObject", "InvokeAsync: awaiting message loop in " + libsupcs.CastOperations.ReinterpretAsUlong(curso).ToString("X"));
                while (curso.t == null)
                {
                    Syscalls.SchedulerFunctions.Block(new DelegateWithParameterEvent(delegate (object so) { return ((ServerObject)so).t != null; }, curso));
                }
                System.Diagnostics.Debugger.Log(0, "ServerObject", "InvokeAsync: message loop has started");
            }

            // If we are the current thread, just run the function
            if(Syscalls.SchedulerFunctions.GetCurrentThread() == curso.t)
            {
                return libsupcs.TysosMethod.InternalInvoke(mptr, args, vtbl_ptr, flags);
            }
            else
            {
                // Send a message to the target ServerObject
                var rpc = new RPCMessage { mptr = mptr, args = args, rtype = vtbl_ptr, flags = flags, Type = Messages.Message.MESSAGE_RPC };
                rpc.result = new RPCResult<object> { Server = curso };

                void* res_obj = libsupcs.CastOperations.ReinterpretAsPointer(rpc.result);
                *(void**)res_obj = vtbl_ptr;    // make rpc.result of type RPCResult<T>

                System.Diagnostics.Debugger.Log(0, null, "RPC request remote " + libsupcs.CastOperations.ReinterpretAs<ulong>(mptr).ToString("X16") +
                    " on thread " + curso.t.name);

                Syscalls.IPCFunctions.SendMessage(curso.t.owning_process, rpc);

                return libsupcs.CastOperations.ReinterpretAsPointer(rpc.result);
            }
        }

        public RPCResult<bool> SetMountPath(string p)
        {
            MountPath = p;
            return true;
        }

        public virtual bool InitServer()
        {
            return true;
        }

        public virtual void MessageLoop()
        {
            t = Syscalls.SchedulerFunctions.GetCurrentThread();
            System.Diagnostics.Debugger.Log(0, "ServerObject", "MessageLoop: in " + libsupcs.CastOperations.ReinterpretAsUlong(this).ToString("X") + 
                " t set to " + libsupcs.CastOperations.ReinterpretAsUlong(t).ToString("X"));
            Syscalls.SchedulerFunctions.GetCurrentThread().owning_process.MessageServer = this;

            if(InitServer() == false)
            {
                System.Diagnostics.Debugger.Log(0, null, "InitServer failed");
                return;
            }

            foreach (string tag in Tags)
            {
                while (Syscalls.ProcessFunctions.GetVfs() == null) ;
                var vfs = Syscalls.ProcessFunctions.GetVfs();

                vfs.RegisterTag(tag, MountPath);
            }

            System.Diagnostics.Debugger.Log(0, null, "entering message loop");

            while(true)
            {
                IPCMessage msg = null;
                do
                {
                    msg = Syscalls.IPCFunctions.ReadMessage();

                    if (msg != null)
                        HandleMessage(msg);
                } while (msg != null);

                BackgroundProc();

                Syscalls.SchedulerFunctions.Block();
            }
        }

        protected unsafe virtual void HandleMessage(IPCMessage msg)
        {
            SourceThread = msg.Source;
            if(msg.Type == Messages.Message.MESSAGE_RPC)
            {
                var rpc = msg as RPCMessage;

                var maddr = libsupcs.CastOperations.ReinterpretAs<ulong>(rpc.mptr);
                System.Diagnostics.Debugger.Log(0, null, "RPC call to " + maddr.ToString("X16") + " from " + msg.Source.name);

                CurrentMessage = rpc;
                void *ret = libsupcs.TysosMethod.InternalInvoke(rpc.mptr, rpc.args, rpc.rtype, rpc.flags);

                // ret is a RPCResult<T> object, we need to copy its result value (Box<T>)
                //  to that in the RPCMessage (which is the one the client is waiting
                //  on), then signal the Event
                var ret2 = libsupcs.CastOperations.ReinterpretAs<RPCResult<object>>(ret);
                (libsupcs.CastOperations.ReinterpretAs<RPCResult<object>>(rpc.result)).Result = ret2.Result;

                if(rpc.EventSetsOnReturn)
                    rpc.result.Set();

                System.Diagnostics.Debugger.Log(0, null, "RPC call to " + maddr.ToString("X16") + " finished");

                CurrentMessage = null;
            }
            else if(HandleGenericMessage(msg) == false)
            {
                System.Diagnostics.Debugger.Log(0, null, "unknown message type: " +
                    msg.Type.ToString("X8") + "\n");
            }
            SourceThread = null;
        }

        /** <summary>Override in subclasses to handle additional message types</summary> */
        protected virtual bool HandleGenericMessage(IPCMessage msg)
        {
            return false;
        }

        protected virtual void BackgroundProc()
        {

        }

        public static object GetFirstProperty(IEnumerable<lib.File.Property> props, string name)
        {
            foreach (var prop in props)
            {
                if (prop.Name == name)
                    return prop.Value;
            }
            return null;
        }
    }
}
