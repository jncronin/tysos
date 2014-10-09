/* Copyright (C) 2014 by John Cronin
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

namespace libtysila
{
    /** <summary>Tracks the hardware locations of each stack location on a per-CIL instruction level</summary>
     */
    abstract class Stack
    {
        /** <summary>Push an item to the stack and return the hardware location for it</summary> */
        public abstract libasm.hardware_location GetAddressFor(Signature.Param p, Assembler ass);

        /** <summary>Return the hardware location for a particular stack location</summary> */
        public abstract libasm.hardware_location GetAddressOf(int idx, Assembler ass);

        /** <summary>Return the hardware location for the last item on the stack and remove it from the stack</summary> */
        public abstract libasm.hardware_location Pop(Assembler ass);

        /** <summary>Clone the internal representation of the stack in this object</summary> */
        public abstract Stack Clone();

        /** <summary>Get the size in bytes of the hardware stack used by this particular stack</summary> */
        public abstract int ByteSize { get; }

        /** <summary>Return the hardware locations used by this stack</summary> */
        public abstract ICollection<libasm.hardware_location> UsedLocations { get; }

        /** <summary>Mark a hardware location as used - does not affect the stack but rather the return of UsedLocations</summary> */
        public abstract void MarkUsed(libasm.hardware_location loc);

        /** <summary>Empty the stack</summary> */
        public abstract void Clear();
    }

    /** <summary>A default implementation of the Stack class, using libasm.hardware_stackloc instances
     * as the locations</summary>
     */
    class DefaultStack : Stack
    {
        util.Stack<libasm.hardware_location> cur_locs;
        int next_loc = 0;
        libasm.hardware_stackloc.StackType stype = libasm.hardware_stackloc.StackType.Var;

        public DefaultStack() { }
        public DefaultStack(libasm.hardware_stackloc.StackType StackType) { stype = StackType; }

        public override libasm.hardware_location GetAddressFor(Signature.Param p, Assembler ass)
        {
            if (cur_locs == null)
                cur_locs = new util.Stack<libasm.hardware_location>();

            int obj_size = ass.GetSizeOf(p);
            libasm.hardware_stackloc l = new libasm.hardware_stackloc { loc = next_loc, size = obj_size, stack_type = stype };
            next_loc += util.align(obj_size, ass.GetStackAlign());
            cur_locs.Push(l);

            return l;
        }

        public override libasm.hardware_location GetAddressOf(int idx, Assembler ass)
        {
            if (cur_locs == null || idx < 0 || idx >= cur_locs.Count)
                throw new IndexOutOfRangeException();
            return cur_locs[idx];
        }

        public override libasm.hardware_location Pop(Assembler ass)
        {
            if (cur_locs == null || cur_locs.Count == 0)
                throw new IndexOutOfRangeException();

            libasm.hardware_location l = cur_locs.Pop();
            next_loc = ((libasm.hardware_stackloc)l).loc;
            return l;
        }

        public override Stack Clone()
        {
            return new DefaultStack { cur_locs = cur_locs == null ? null : new util.Stack<libasm.hardware_location>(cur_locs), next_loc = next_loc, stype = stype };
        }

        public override int ByteSize
        {
            get { return next_loc; }
        }

        public override ICollection<libasm.hardware_location> UsedLocations
        {
            get { return cur_locs; }
        }

        public override void MarkUsed(libasm.hardware_location loc)
        {
            if (cur_locs == null)
                cur_locs = new util.Stack<libasm.hardware_location>();
            if (cur_locs.Contains(loc))
                cur_locs.Add(loc);
        }

        public override void Clear()
        {
            cur_locs = new util.Stack<libasm.hardware_location>();
            next_loc = 0;
        }
    }
}
