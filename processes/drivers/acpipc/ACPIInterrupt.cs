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
using tysos.Resources;

namespace acpipc
{
    abstract class GlobalSystemInterrupt : tysos.Resources.InterruptLine
    {
        internal int gsi_num;

        internal abstract void SetMode(bool is_level_trigger, bool is_low_active);
    }

    /* A non-shared ISA interrupt */
    class ISAInterrupt : tysos.Resources.InterruptLine
    {
        GlobalSystemInterrupt gsi;
        internal bool is_level_trigger = false;
        internal bool is_low_active = false;
        internal int irq;

        public ISAInterrupt(GlobalSystemInterrupt global_interrupt)
        {
            gsi = global_interrupt;
        }

        public override bool RegisterHandler(InterruptHandler handler)
        {
            bool ret = gsi.RegisterHandler(handler);
            if (ret == false)
                return false;
            gsi.SetMode(is_level_trigger, is_low_active);
            return true;
        }
    }
    
    /* A shared PCI interrupt */
    class PCIInterrupt : tysos.Resources.SharedInterruptLine
    {
        GlobalSystemInterrupt gsi;

        /* PCI interrupts are level sensitive, asserted low (see PCI spec para 2.2.6) */
        internal bool is_level_trigger = true;
        internal bool is_low_active = true;

        public PCIInterrupt(GlobalSystemInterrupt global_interrupt) : base(global_interrupt)
        {
            gsi = global_interrupt;
        }

        public override bool RegisterHandler(InterruptHandler handler)
        {
            bool ret = base.RegisterHandler(handler);
            if (ret == false)
                return false;
            gsi.SetMode(is_level_trigger, is_low_active);
            return true;
        }
    }
}
