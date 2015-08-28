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

        public override string ToString()
        {
            return "GlobalSystemInterrupt " + gsi_num.ToString();
        }
    }

    public class ACPIInterrupt : tysos.Resources.SharedInterruptLine
    {
        GlobalSystemInterrupt gsi;
        public int irq = -1;

        /* PCI interrupts are level sensitive, asserted low (see PCI spec para 2.2.6) */
        internal bool is_level_trigger = true;
        internal bool is_low_active = true;

        internal ACPIInterrupt(GlobalSystemInterrupt global_interrupt) : base(global_interrupt)
        {
            gsi = global_interrupt;
        }

        internal ACPIInterrupt(GlobalSystemInterrupt global_interrupt, bool level_trigger, bool low_active) : base(global_interrupt)
        {
            gsi = global_interrupt;
            is_level_trigger = level_trigger;
            is_low_active = low_active;
        }

        public override bool RegisterHandler(InterruptHandler handler)
        {
            System.Diagnostics.Debugger.Log(0, "ACPIInterrupt", ShortName + " RegisterHandler");
            bool ret = base.RegisterHandler(handler);
            if (ret == false)
                return false;
            gsi.SetMode(is_level_trigger, is_low_active);
            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ACPIInterrupt");
            if(irq != -1)
            {
                sb.Append(" (IRQ ");
                sb.Append(irq.ToString());
                sb.Append(")");
            }
            if(gsi != null)
            {
                sb.Append(" -> ");
                sb.Append(gsi.ToString());
            }
            return sb.ToString();
        }

        public override string ShortName
        {
            get
            {
                if (irq == -1)
                    return "PCI";
                else
                    return "IRQ" + irq.ToString();
            }
        }
    }

    /* A PCI interrupt */
    public class PCIInterrupt : ACPIInterrupt
    {
        internal PCIInterrupt(GlobalSystemInterrupt global_interrupt)
            : base(global_interrupt, true, true)
        { }
    }

    /* An ISA interrupt */
    public class ISAInterrupt : ACPIInterrupt
    {
        internal ISAInterrupt(GlobalSystemInterrupt global_interrupt)
            : base(global_interrupt, false, false)
        { }
    }
}
