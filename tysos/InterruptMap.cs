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
    abstract public class InterruptMap
    {
        protected Dictionary<string, PICEntry> interrupts = new Dictionary<string, PICEntry>(new tysos.Program.MyGenericEqualityComparer<string>());
        protected List<int> free_cpu_vectors = new List<int>();
        protected Dictionary<string, Delegate> deferred_irqs = new Dictionary<string, Delegate>(new tysos.Program.MyGenericEqualityComparer<string>());

        public InterruptMap()
        { }

        public void RegisterInterrupt(string name, PICEntry interrupt)
        {
            tysos.Syscalls.DebugFunctions.DebugWrite("InterruptMap: registering " + name + " to " + interrupt.ToString() + "\n");
            interrupts[name] = interrupt;

            if (deferred_irqs.ContainsKey(name))
                RegisterIRQHandler(name, deferred_irqs[name]);
        }

        public void RegisterIRQHandler(string interrupt_name, Delegate handler)
        {
            if (!interrupts.ContainsKey(interrupt_name))
            {
                deferred_irqs[interrupt_name] = handler;
                tysos.Syscalls.DebugFunctions.DebugWrite("InterruptMap: handler for IRQ " + interrupt_name + " registered.  Deferring until interrupt routing is registered.\n");
            }
            else
            {
                int cpu_vector = get_free_cpu_vector();
                PICEntry pe = interrupts[interrupt_name];
                pe.cpu_id = 0;
                pe.cpu_vector = cpu_vector;
                tysos.Syscalls.InterruptFunctions.InstallHandler(cpu_vector, handler);
                pe.PIC.EnableIRQ(pe.pri, pe.cpu_vector, pe.cpu_id);
                tysos.Syscalls.DebugFunctions.DebugWrite("InterruptMap: routing " + interrupt_name + " to cpu id: " + pe.cpu_id.ToString() + " vector: " + pe.cpu_vector.ToString("X") + "\n");
            }
        }

        abstract public void SendEOI();

        private int get_free_cpu_vector()
        {
            lock (free_cpu_vectors)
            {
                if (free_cpu_vectors.Count == 0)
                    throw new Exception("No more free cpu vectors");
                int ret = free_cpu_vectors[0];
                free_cpu_vectors.RemoveAt(0);
                return ret;
            }
        }
    }

    public class PICEntry
    {
        public IPIC PIC;
        public int pri;        // PIC-relative interrupt
        public int cpu_id;
        public int cpu_vector;

        public override string ToString()
        {
            return PIC.ToString() + " IRQ: " + pri.ToString();
        }
    }

    public interface IPIC
    {
        void EnableIRQ(int pri, int cpu_vector, int cpu_id);
        void DisableIRQ(int pri);
    }
}
