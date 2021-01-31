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
        protected InvokeEvent CurrentInvokeEvent = null;
        public string MountPath = null;
        public List<string> Tags = new List<string>();

        public ServerObject()
        {
        }

        public virtual object Invoke(string name, object[] p)
        {
            Type[] ts = new Type[p.Length];
            for (int i = 0; i < p.Length; i++)
                ts[i] = p[i].GetType();
            return Invoke(name, p, ts);
        }

        public virtual object Invoke(string name, object[] p, Type[] ts)
        {
            InvokeEvent e = InvokeAsync(name, p, ts);
            while (e.IsSet == false)
                Syscalls.SchedulerFunctions.Block(e);
            return e.ReturnValue;
        }

        public class InvokeEvent : Event
        {
            public object ReturnValue;
            public string MethodName;
            public object[] Parameters;
            public Type[] ParamTypes;

            public bool EventSetsOnReturn = true; /** <summary>If unset, the event will not be signalled automatically when the Invoked method returns</summary> */

            public delegate object ObjectDelegate(object o, object retval);
            protected ObjectDelegate CallOnSet;
            protected object CallbackObject;

            public InvokeEvent(ObjectDelegate callOnSet, object callbackObject)
            {
                CallOnSet = callOnSet;
                CallbackObject = callbackObject;
            }

            public override void Set()
            {
                base.Set();
                if (CallOnSet != null)
                    CallOnSet(CallbackObject, ReturnValue);
            }
        }

        public virtual InvokeEvent InvokeAsync(string name, object[] p)
        {
            Type[] ts = new Type[p.Length];
            for (int i = 0; i < p.Length; i++)
                ts[i] = p[i].GetType();
            return InvokeAsync(name, p, ts, null, null);
        }

        public virtual InvokeEvent InvokeAsync(string name, object[] p, Type[] ts)
        { return InvokeAsync(name, p, ts, null, null); }

        public virtual InvokeEvent InvokeAsync(string name, object[] p, Type[] ts,
            InvokeEvent.ObjectDelegate callback, object callback_obj)
        {
            // Wait for us to enter the message loop
            System.Diagnostics.Debugger.Log(0, "ServerObject", "InvokeAsync: awaiting message loop in " + libsupcs.CastOperations.ReinterpretAsUlong(this).ToString("X"));
            while(t == null)
            {
                Syscalls.SchedulerFunctions.Block(new DelegateWithParameterEvent(delegate (object so) { return ((ServerObject)so).t != null; }, this));
            }
            System.Diagnostics.Debugger.Log(0, "ServerObject", "InvokeAsync: message loop has started");

            if (Syscalls.SchedulerFunctions.GetCurrentThread() == t)
            {
                // If we are running on the current thread of the target object then just call it
                InvokeEvent e = new InvokeEvent(callback, callback_obj);
                e.ReturnValue = InvokeInternal(name, p, ts);
                e.Set();
                return e;
            }
            else
            {
                // Else, send a message to the target thread to run it
                return InvokeRemoteAsync(t.owning_process, name, p, ts, callback, callback_obj);
            }
        }

        public static InvokeEvent InvokeRemoteAsync(Process proc, string name, object[] p, Type[] ts)
        { return InvokeRemoteAsync(proc, name, p, ts, null, null); }

        public static InvokeEvent InvokeRemoteAsync(Process proc, string name, object[] p, Type[] ts,
            InvokeEvent.ObjectDelegate callback, object callback_obj)
        {
            InvokeEvent e = new InvokeEvent(callback, callback_obj)
                { Parameters = p, MethodName = name, ParamTypes = ts };
            Syscalls.IPCFunctions.SendMessage(proc, new IPCMessage { Message = e, Type = tysos.Messages.Message.MESSAGE_GENERIC });
            return e;
        }

        public virtual object InvokeInternal(string name, object[] p, Type[] ts)
        {
            if(name == null)
            {
                // Return a list of all possible methods
                return this.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            }

            System.Reflection.MethodInfo mi = null;

            /* Look for methods on all base classes too
                TODO: libsupcs should do this for us */
            Type cur_type = this.GetType();
            while(mi == null && cur_type != null)
            {
                mi = cur_type.GetMethod(name, ts);
                cur_type = cur_type.BaseType;
            }

            // Can only invoke public instance methods
            if (mi == null || mi.IsPublic == false || mi.IsStatic == true)
            {
                Formatter.Write("InvokeInternal: method ", Program.arch.DebugOutput);
                Formatter.Write(name, Program.arch.DebugOutput);
                Formatter.WriteLine(" not found", Program.arch.DebugOutput);
                return null;
            }

            return mi.Invoke(this, p);
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
                while (Syscalls.ProcessFunctions.GetSpecialProcess(Syscalls.ProcessFunctions.SpecialProcessType.Vfs) == null) ;
                Syscalls.ProcessFunctions.GetSpecialProcess(Syscalls.ProcessFunctions.SpecialProcessType.Vfs).InvokeAsync(
                    "RegisterTag", new object[] { tag, MountPath },
                    new Type[] { typeof(string), typeof(string) });
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

        protected virtual void HandleMessage(IPCMessage msg)
        {
            SourceThread = msg.Source;
            if(msg.Type == Messages.Message.MESSAGE_GENERIC)
            {
                InvokeEvent e = msg.Message as InvokeEvent;
                CurrentInvokeEvent = e;
                e.ReturnValue = InvokeInternal(e.MethodName, e.Parameters, e.ParamTypes);
                CurrentInvokeEvent = null;

                if(e.EventSetsOnReturn)
                    e.Set();
            }
            else
            {
                System.Diagnostics.Debugger.Log(0, null, "unknown message type: " +
                    msg.Type.ToString("X8") + "\n");
            }
            SourceThread = null;
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
