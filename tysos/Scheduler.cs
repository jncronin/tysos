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
    class Scheduler
    {
        internal protected tysos.Collections.Queue<Thread>[] running_tasks;
        internal protected tysos.Collections.DeltaQueue<Thread> sleeping_tasks;
        internal protected tysos.Collections.LinkedList<Thread> blocking_tasks;

        const int DEF_PRIORITIES = 11;

        protected int priorities;

        public Scheduler() : this(DEF_PRIORITIES) { }
        public Scheduler(int _priorities)
        {
            priorities = _priorities;
            running_tasks = new Collections.Queue<Thread>[priorities];
            for (int i = 0; i < priorities; i++)
                running_tasks[i] = new Collections.Queue<Thread>();

            sleeping_tasks = new Collections.DeltaQueue<Thread>();
            blocking_tasks = new Collections.LinkedList<Thread>();
        }

        /** <summary>Reschedule a thread to the end the priority queue associated with it</summary> */
        protected void _Reschedule(Thread thread)
        {
            _Release(thread);

            thread.location = thread.priority;
            thread.time_to_run = thread.default_slice;
            lock (thread.BlockingOn)
            {
                thread.BlockingOn.Clear();
            }
            running_tasks[thread.priority].Add(thread);
        }

        /** <summary>Remove a thread from the scheduler</summary> */
        protected void _Release(Thread thread)
        {
            if (thread.location >= 0)
                running_tasks[thread.location].Remove(thread);
            else if (thread.location == Thread.LOC_SLEEPING)
                sleeping_tasks.Remove(thread);
            else if (thread.location == Thread.LOC_BLOCKING)
                blocking_tasks.Remove(thread);

            thread.location = Thread.LOC_RELEASED;
        }

        /** <summary>Return the next thread to run after a certain delay has elapsed</summary> */
        protected Thread _GetNextThread(long ns)
        {
            Thread ret = GetNextThread();

            if (ret != null)
            {
                if (ret.time_to_run <= ns)
                    _Reschedule(ret);
                else
                    ret.time_to_run -= ns;
            }

            return ret;
        }

        /** <summary>Return the next thread to run</summary> */
        internal Thread GetNextThread()
        {
            _WakeUpBlockingTasks();

            int i;
            for (i = priorities - 1; i >= 0; i--)
            {
                Thread ret;

                ret = running_tasks[i].GetFirst(false);

                if (ret != null)
                    return ret;
            }

            return null;
        }

        private void _WakeUpBlockingTasks()
        {
            int i = 0;

            while (i < blocking_tasks.Count)
            {
                bool can_continue = true;

                lock (blocking_tasks[i].BlockingOn)
                {
                    foreach (Event e in blocking_tasks[i].BlockingOn)
                    {
                        if (!e.IsSet)
                        {
                            can_continue = false;
                            break;
                        }
                    }
                }

                if (can_continue)
                {
                    //blocking_tasks[i].BlockingOn.Clear();
                    _Reschedule(blocking_tasks[i]);
                }
                else
                    i++;
            }
        }

        // public members
        public void Deschedule(Thread thread)
        {
            lock (this)
            {
                _Release(thread);
            }
        }

        public void Reschedule(Thread thread)
        {
            lock (this)
            {
                _Reschedule(thread);
            }
        }

        public void Block(Thread thread)
        {
            lock (this)
            {
                _Release(thread);

                thread.location = Thread.LOC_BLOCKING;
                lock (thread.BlockingOn)
                {
                    thread.BlockingOn.Clear();
                    Event e = new Event();
                    e.name = "BlockOnMessage";
                    e.Type = Event.EventType.BlockOnMessage;
                    e.BlockingThread = thread;
                    thread.BlockingOn.Add(e);
                }
                blocking_tasks.Add(thread);
            }
        }

        public void Block(Thread thread, Event ev)
        {
            lock (this)
            {
                _Release(thread);

                thread.location = Thread.LOC_BLOCKING;
                lock (thread.BlockingOn)
                {
                    thread.BlockingOn.Clear();
                    thread.BlockingOn.Add(ev);
                }
                blocking_tasks.Add(thread);
            }
        }

        public void Sleep(Thread thread, long ns)
        {
            lock (this)
            {
                _Release(thread);

                thread.location = Thread.LOC_SLEEPING;
                sleeping_tasks.InsertAtDelta(thread, ns);
            }
        }

        public Thread ScheduleNext(long ns, Thread cur, TaskSwitcher switcher)
        {
            Thread next;
            lock (this)
            {
                next = _GetNextThread(ns);
            }

            if ((next != cur) && (next != null))
            {
                switcher.Switch(next);
                return next;
            }
            return null;
        }

        public Thread TimerTick(long ns, Thread cur, TaskSwitcher switcher)
        {
            /* first wake up any sleeping tasks */
            lock (this)
            {
                sleeping_tasks.DecreaseDelta(ns);
                Thread sleeping_thread = null;

                do
                {
                    sleeping_thread = sleeping_tasks.GetZero();
                    if (sleeping_thread != null)
                        _Reschedule(sleeping_thread);
                } while (sleeping_thread != null);
            }

            return ScheduleNext(ns, cur, switcher);
        }

        public static void TimerProc(long ns)
        {
            Program.cur_cpu_data.CurrentScheduler.TimerTick(ns, Program.cur_cpu_data.CurrentThread, Program.arch.Switcher);
        }
    }
}
