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

namespace tysos
{
    public class Event
    {
        ulong event_id;
        public string name;

        public ulong EventId { get { return event_id; } }

        static ulong next_event_id = 0;
        static object next_event_id_lock = new object();

        public enum EventType { BlockOnMessage, Standard }
        internal EventType Type;
        internal Thread BlockingThread;

        public Event()
        {
            //libsupcs.x86_64.Cpu.Break();
            lock (next_event_id_lock)
            {
                event_id = next_event_id++;
            }
            Type = EventType.Standard;
        }

        volatile int mutex = 0;

        public virtual bool IsSet
        {
            get
            {
                if (mutex == 1)
                    return true;

                if (Type == EventType.BlockOnMessage)
                {
                    if ((BlockingThread != null) && (BlockingThread.owning_process != null) && (BlockingThread.owning_process.ipc != null))
                    {
                        if (BlockingThread.owning_process.ipc.PeekMessage() != null)
                        {
                            mutex = 1;
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public virtual void Set()
        {
            mutex = 1;
        }

        public virtual void Reset()
        {
            mutex = 0;
        }
    }

    public class MultipleEvent : Event
    {
        public List<Event> Children = new List<Event>();

        public override void Set()
        { }
        public override void Reset()
        { }
    }

    public class WaitAnyEvent : MultipleEvent
    {
        public override bool IsSet
        {
            get
            {
                foreach (Event e in Children)
                {
                    if (e.IsSet)
                        return true;
                }
                return false;
            }
        }
    }

    public class WaitAllEvent : MultipleEvent
    {
        public override bool IsSet
        {
            get
            {
                foreach (Event e in Children)
                {
                    if (!e.IsSet)
                        return false;
                }
                return true;
            }
        }
    }

    public class ProcessEvent : Event
    {
        string _ProcessName;
        Process _Process;

        public Process Process
        {
            get
            {
                if (_Process == null)
                {
                    if (Program.running_processes.ContainsKey(_ProcessName))
                        _Process = Program.running_processes[_ProcessName];
                }
                return _Process;
            }
        }

        public string ProcessName
        {
            get
            {
                return _ProcessName;
            }

            set
            {
                _ProcessName = value;
            }
        }

        public enum ProcessEventTypeKind { ReadyForMessages }

        public ProcessEventTypeKind ProcessEventType;

        public override bool IsSet
        {
            get
            {
                if (Process == null)
                    return false;
                        
                if ((ProcessEventType == ProcessEventTypeKind.ReadyForMessages) && (Process.ipc != null))
                    return true;
                return false;
            }
        }
    }

    public class TimerEvent : Event
    {
        long limit_val;
        Timer timer;

        public TimerEvent(long tick_delay)
        {
            timer = Program.cur_cpu_data.CurrentTimer;
            unchecked
            {
                limit_val = timer.Ticks + tick_delay;
            }
        }

        public override bool IsSet
        {
            get
            {
#if DEBUG_TIMER_EVENT
                Formatter.Write("TimerEvent: current ticks: ", Program.arch.DebugOutput);
                Formatter.Write((ulong)timer.Ticks, Program.arch.DebugOutput);
                Formatter.Write("  limit: ", Program.arch.DebugOutput);
                Formatter.Write((ulong)limit_val, Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
#endif

                if (timer.Ticks > limit_val)
                    return true;
                return false;
            }
        }
    }
}
