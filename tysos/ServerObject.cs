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
    public abstract class ServerObject
    {
        Thread t;

        // cache of previously called methods
        Dictionary<string, System.Reflection.MethodInfo> members =
            new Dictionary<string, System.Reflection.MethodInfo>(new Program.MyGenericEqualityComparer<string>());

        public ServerObject()
        {
            t = Syscalls.SchedulerFunctions.GetCurrentThread();
        }

        public virtual object Invoke(string name, object[] p)
        {
            InvokeEvent e = InvokeAsync(name, p);
            while (e.IsSet == false)
                Syscalls.SchedulerFunctions.Block(e);
            return e.ReturnValue;
        }

        public class InvokeEvent : Event
        {
            public object ReturnValue;
            public string MethodName;
            public object[] Parameters;
        }

        public virtual InvokeEvent InvokeAsync(string name, object[] p)
        {
            if (Syscalls.SchedulerFunctions.GetCurrentThread() == t)
            {
                // If we are running on the current thread of the target object then just call it
                InvokeEvent e = new InvokeEvent();
                e.ReturnValue = InvokeInternal(name, p);
                e.Set();
                return e;
            }
            else
            {
                // Else, send a message to the target thread to run it
                InvokeEvent e = new InvokeEvent { Parameters = p, MethodName = name };
                Syscalls.IPCFunctions.SendMessage(t.owning_process,
                    new IPCMessage { Message = e, Type = tysos.Messages.Message.MESSAGE_GENERIC });
                return e;
            }
        }

        public virtual object InvokeInternal(string name, object[] p)
        {
            if(name == null)
            {
                // Return a list of all possible methods
                return this.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            }

            System.Reflection.MethodInfo mi = null;
            if (members.ContainsKey(name))
                mi = members[name];
            else
            {
                Type[] ts = new Type[p.Length];
                for (int i = 0; i < p.Length; i++)
                    ts[i] = p[i].GetType();

                mi = this.GetType().GetMethod(name, ts);

                // Can only invoke public instance methods
                if (mi == null || mi.IsPublic == false || mi.IsStatic == true)
                    return null;

                members[name] = mi;
            }

            return mi.Invoke(this, p);
        }

        protected virtual void MessageLoop()
        {
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
            if(msg.Type == Messages.Message.MESSAGE_GENERIC)
            {
                InvokeEvent e = msg.Message as InvokeEvent;
                e.ReturnValue = InvokeInternal(e.MethodName, e.Parameters);
                e.Set();
            }
            else
            {
                Syscalls.DebugFunctions.DebugWrite("ServerObject: unknown message type: " +
                    msg.Type.ToString("X8") + "\n");
            }
        }

        protected virtual void BackgroundProc()
        {

        }
    }
}
