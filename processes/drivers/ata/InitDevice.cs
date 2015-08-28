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
using tysos;
using tysos.Resources;

namespace ata
{
    partial class ata : tysos.lib.VirtualDirectoryServer
    {
        tysos.RangeResource cmd = null, ctrl = null;
        tysos.Resources.InterruptLine irq = null;

        /* Port offsets from command port */
        const uint DATA = 0;
        const uint FEATURES = 1;
        const uint SECTOR_COUNT = 2;
        const uint LBALO = 3;
        const uint LBAMID = 4;
        const uint LBAHI = 5;
        const uint DRIVE = 6;
        const uint COMMAND = 7;
        const uint STATUS = 7;

        const uint MASTER_DRIVE = 0xa0;
        const uint SLAVE_DRIVE = 0xb0;

        const uint IDENTIFY_DRIVE = 0xec;

        void Wait400ns()
        {
            //TODO: reimplement with a proper wait using a timer
            for (int i = 0; i < 4; i++)
                cmd.Read(cmd.Addr64 + STATUS, 1);
        }

        internal ata(tysos.lib.File.Property[] Properties)
        {
            root = new List<tysos.lib.File.Property>(Properties);
        }

        public override bool InitServer()
        {
            System.Diagnostics.Debugger.Log(0, "pciide", "PCI IDE driver started");

            /* Get our ports and interrupt */
            foreach(var r in root)
            {
                if(r.Name == "io" && (r.Value is tysos.x86_64.IOResource))
                {
                    var io = r.Value as tysos.x86_64.IOResource;
                    if (cmd == null && io.Length64 >= 8)
                        cmd = io;
                    else
                        ctrl = io;
                }
                if(r.Name == "interrupt" && (r.Value is tysos.Resources.InterruptLine))
                {
                    irq = r.Value as tysos.Resources.InterruptLine;
                }
            }

            if(cmd == null || ctrl == null || irq == null)
            {
                System.Diagnostics.Debugger.Log(0, "ata", "Insufficient resources provided");
                return false;
            }

            irq.RegisterHandler(new InterruptLine.InterruptHandler(IrqHandler));

            /* Detect drives on the different ports */
            cmd.Write(cmd.Addr64 + DRIVE, 1, MASTER_DRIVE);
            Wait400ns();

            // Detect if this is ATAPI or other
            uint lbamid = (uint)cmd.Read(cmd.Addr64 + LBAMID, 1);
            uint lbahi = (uint)cmd.Read(cmd.Addr64 + LBAHI, 1);
            if (lbamid == 0x14 && lbahi == 0xeb)
                System.Diagnostics.Debugger.Log(0, "ata", "Master is PATAPI");
            else if (lbamid == 0x69 && lbahi == 0x96)
                System.Diagnostics.Debugger.Log(0, "ata", "Master is SATAPI");
            else if (lbamid == 0 && lbahi == 0)
                System.Diagnostics.Debugger.Log(0, "ata", "Master is PATA");
            else if (lbamid == 0x3c && lbahi == 0xc3)
                System.Diagnostics.Debugger.Log(0, "ata", "Master is SATA");
            else
                System.Diagnostics.Debugger.Log(0, "ata", "Master is unknown type");

            cmd.Write(cmd.Addr64 + LBALO, 1, 0);
            cmd.Write(cmd.Addr64 + LBAMID, 1, 0);
            cmd.Write(cmd.Addr64 + LBAHI, 1, 0);
            cmd.Write(cmd.Addr64 + SECTOR_COUNT, 1, 0);


            cmd.Write(cmd.Addr64 + COMMAND, 1, IDENTIFY_DRIVE);

            uint status = (uint)cmd.Read(cmd.Addr64 + STATUS, 1);
            if (status == 0)
            {
                System.Diagnostics.Debugger.Log(0, "ata", "No master drive detected");
            }
            else
            {
                while ((status & 0x80) != 0)
                    status = (uint)cmd.Read(cmd.Addr64 + STATUS, 1);

                Wait400ns();

                for (int i = 0; i < 256; i++)
                {
                    uint word = (uint)cmd.Read(cmd.Addr64 + DATA, 2);
                    System.Diagnostics.Debugger.Log(0, "ata", i.ToString() + ": " + word.ToString("X4"));
                }
            }


            cmd.Write(cmd.Addr64 + DRIVE, 1, SLAVE_DRIVE);
            Wait400ns();

            // Detect if this is ATAPI or other
            lbamid = (uint)cmd.Read(cmd.Addr64 + LBAMID, 1);
            lbahi = (uint)cmd.Read(cmd.Addr64 + LBAHI, 1);
            if (lbamid == 0x14 && lbahi == 0xeb)
                System.Diagnostics.Debugger.Log(0, "ata", "Slave is PATAPI");
            else if (lbamid == 0x69 && lbahi == 0x96)
                System.Diagnostics.Debugger.Log(0, "ata", "Slave is SATAPI");
            else if (lbamid == 0 && lbahi == 0)
                System.Diagnostics.Debugger.Log(0, "ata", "Slave is PATA");
            else if (lbamid == 0x3c && lbahi == 0xc3)
                System.Diagnostics.Debugger.Log(0, "ata", "Slave is SATA");
            else
                System.Diagnostics.Debugger.Log(0, "ata", "Slave is unknown type");

            cmd.Write(cmd.Addr64 + LBALO, 1, 0);
            cmd.Write(cmd.Addr64 + LBAMID, 1, 0);
            cmd.Write(cmd.Addr64 + LBAHI, 1, 0);
            cmd.Write(cmd.Addr64 + SECTOR_COUNT, 1, 0);


            cmd.Write(cmd.Addr64 + COMMAND, 1, IDENTIFY_DRIVE);

            status = (uint)cmd.Read(cmd.Addr64 + STATUS, 1);
            if (status == 0)
            {
                System.Diagnostics.Debugger.Log(0, "ata", "No slave drive detected");
            }
            else
            {
                while ((status & 0x80) != 0)
                    status = (uint)cmd.Read(cmd.Addr64 + STATUS, 1);

                Wait400ns();

                for (int i = 0; i < 256; i++)
                {
                    uint word = (uint)cmd.Read(cmd.Addr64 + DATA, 2);
                    System.Diagnostics.Debugger.Log(0, "ata", i.ToString() + ": " + word.ToString("X4"));
                }
            }



            return true;
        }

        bool IrqHandler()
        {
            uint status = (uint)cmd.Read(cmd.Addr64 + STATUS, 1);
            System.Diagnostics.Debugger.Log(0, "ata", "IRQ SIGNALLED: STATUS: " + status.ToString("X8"));
            return true;
        }
    }
}
