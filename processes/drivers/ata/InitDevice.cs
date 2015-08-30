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
        const uint ERROR = 1;
        const uint SECTOR_COUNT = 2;
        const uint INTERRUPT_REASON = 2;
        const uint LBALO = 3;
        const uint LBAMID = 4;
        const uint BYTECOUNTLO = 4;
        const uint LBAHI = 5;
        const uint BYTECOUNTHI = 5;
        const uint DRIVE = 6;
        const uint DEVICE = 6;
        const uint COMMAND = 7;
        const uint STATUS = 7;

        const uint MASTER_DRIVE = 0xa0;
        const uint SLAVE_DRIVE = 0xb0;

        /* Commands */
        const uint IDENTIFY_DRIVE = 0xec;

        /* Define device data */
        class DeviceInfo
        {
            public enum DeviceType { ATA, ATAPI, SATA, SATAPI };
            public DeviceType Type;

            public uint SectorSize;
            public ulong SectorCount;
            public bool Lba48;

            public string SerialNumber, FirmwareRevision, ModelNumber;

            public int MaxPIO, MaxUDMA;
        }

        DeviceInfo[] Devices;

        void Wait400ns()
        {
            //TODO: reimplement with a proper wait using a timer
            for (int i = 0; i < 4; i++)
                ctrl.Read(ctrl.Addr64, 1);
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

            Devices = new DeviceInfo[2];

            /* Set control register to 0 - enable interrupts */
            ctrl.Write(ctrl.Addr64, 1, 0);

            /* Enter Init state */
            s = State.Init;
            drive = 0;

            /* Register IRQ */
            irq.RegisterHandler(new InterruptLine.InterruptHandler(IrqHandler));

            /* Run handler for Init state (unless the IRQ has already done this
            for us) */
            if (s == State.Init)
                StateMachine((uint)ctrl.Read(ctrl.Addr64, 1), (uint)cmd.Read(cmd.Addr64 + ERROR, 1));

            return true;
        }

        bool IrqHandler()
        {
            uint status = (uint)cmd.Read(cmd.Addr64 + STATUS, 1);
            System.Diagnostics.Debugger.Log(0, "ata", "IRQ SIGNALLED: STATUS: " + status.ToString("X8") + ", state: " + s.ToString() + ", drive: " + drive.ToString());

            StateMachine(status, (uint)cmd.Read(cmd.Addr64 + ERROR, 1));

            return true;
        }

        enum State
        {
            Init,
            StartupIdentifySent,
            ReadSent,
            WriteSent,
            Idle
        }

        State s = State.Init;
        int drive = 0;

        public void StateMachine(uint Status, uint Error)
        {
            switch(s)
            {
                case State.Init:
                    /* Send IdentifyDevice to Master drive */
                    SendIdentifyDevice(0);

                    Wait400ns();
                    if(s == State.Init)
                    {
                        /* Assume IDENTIFY failed */
                        drive = 1;
                        s = State.StartupIdentifySent;
                        Devices[0] = null;
                        System.Diagnostics.Debugger.Log(0, "ata", "No master device");
                        SendIdentifyDevice(1);
                    }
                    return;
                case State.StartupIdentifySent:
                    switch(drive)
                    {
                        case 0:
                            HandleIdentifyDevice(0, Status, Error);
                            SendIdentifyDevice(1);

                            Wait400ns();
                            if(s == State.StartupIdentifySent && drive == 1)
                            {
                                /* Assume IDENTIFY failed */
                                s = State.Idle;
                                Devices[1] = null;
                                System.Diagnostics.Debugger.Log(0, "ata", "No slave device");
                            }
                            return;
                        case 1:
                            HandleIdentifyDevice(1, Status, Error);
                            s = State.Idle;
                            return;
                    }
                    break;

            }
            System.Diagnostics.Debugger.Log(0, "ata", "Unhandled state: " + s.ToString() +
                ", drive: " + drive.ToString() + ", status: " + Status.ToString("X2") +
                ", error: " + Error.ToString("X2"));
        }

        private void HandleIdentifyDevice(int d, uint Status, uint Error)
        {
            Devices[d] = new DeviceInfo();

            /* If device returned aborted, it is not a valid ATA device */
            if((Error & (0x1U << 2)) != 0)
            {
                /* Read the signature of the device */
                uint ireason = (uint)cmd.Read(cmd.Addr64 + INTERRUPT_REASON, 1);
                uint lbalo = (uint)cmd.Read(cmd.Addr64 + LBALO, 1);
                uint bcl = (uint)cmd.Read(cmd.Addr64 + BYTECOUNTLO, 1);
                uint bch = (uint)cmd.Read(cmd.Addr64 + BYTECOUNTHI, 1);

                if(ireason == 0x1 && lbalo == 0x1 && bcl == 0x14 && bch == 0xeb)
                {
                    /* Its an ATAPI device */
                    System.Diagnostics.Debugger.Log(0, "ata", "Device " + d.ToString() +
                        " is ATAPI");
                    Devices[d].Type = DeviceInfo.DeviceType.ATAPI;
                    return;
                }
                else
                {
                    System.Diagnostics.Debugger.Log(0, "ata", "Unknown device signature for device " + d.ToString() +
                        ": " + ireason.ToString("X2") + " " + lbalo.ToString("X2") +
                        " " + bcl.ToString("X2") + " " + bch.ToString("X2"));
                    Devices[d] = null;
                    return;
                }
            }

            /* Its a valid ATA device.  Read its info */
            ushort[] id = new ushort[256];

            for (int i = 0; i < 256; i++)
            {
                id[i] = (ushort)cmd.Read(cmd.Addr64 + DATA, 2);
                System.Diagnostics.Debugger.Log(0, "ata", "Device " + d.ToString() + " idx " + i.ToString() + ": " + id[i].ToString("X4"));
            }

            /* Get some info about it */
            uint lba28_sects = (((uint)id[61]) << 16) + (uint)id[60];
            bool is_lba48 = (id[83] & (1 << 10)) != 0;
            Devices[d].Lba48 = is_lba48;
            if (is_lba48)
            {
                ulong lba48_sects = (((ulong)id[103]) << 48) +
                    (((ulong)id[102]) << 32) +
                    (((ulong)id[101]) << 16) +
                    (ulong)id[100];

                if (lba48_sects != 0)
                    Devices[d].SectorCount = lba48_sects;
                else
                    Devices[d].SectorCount = lba28_sects;
            }
            else
                Devices[d].SectorCount = lba28_sects;

            Devices[d].SectorSize = 512;

            Devices[d].SerialNumber = ReadString(id, 10, 10);
            Devices[d].FirmwareRevision = ReadString(id, 23, 4);
            Devices[d].ModelNumber = ReadString(id, 27, 20);

            Devices[d].MaxPIO = 1;
            Devices[d].MaxUDMA = -1;
            if((id[53] & 0x2) != 0)
            {
                if ((id[64] & 0x2) != 0)
                    Devices[d].MaxPIO = 4;
                else if ((id[64] & 0x1) != 0)
                    Devices[d].MaxPIO = 3;
            }
            if((id[53] & 0x4) != 0)
            {
                for(int i = 0; i < 7; i++)
                {
                    if ((id[88] & (1 << i)) != 0)
                        Devices[d].MaxUDMA = i;
                }
            }

            /* Dump output */
            System.Diagnostics.Debugger.Log(0, "ata", ((d == 0) ? "MASTER: " : "SLAVE: ") +
                "SerialNumber: " + Devices[d].SerialNumber +
                ", ModelNumber: " + Devices[d].ModelNumber +
                ", FirmwareRevision: " + Devices[d].FirmwareRevision +
                ", SectorSize: " + Devices[d].SectorSize.ToString() +
                ", SectorCount: " + Devices[d].SectorCount.ToString() +
                ", MaxPIO: " + Devices[d].MaxPIO.ToString() +
                ((Devices[d].MaxUDMA != -1) ? (", MaxUDMA: " + Devices[d].MaxUDMA.ToString()) : "") +
                ((Devices[d].Lba48 == true) ? ", LBA48" : ""));
        }

        private string ReadString(ushort[] id, int idx, int wordlength)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < wordlength; i++)
            {
                ushort val = id[idx + i];
                char c_1 = (char)(val >> 8);
                char c_2 = (char)(val & 0xffU);
                sb.Append(c_1);
                sb.Append(c_2);
            }
            string ret = sb.ToString().TrimEnd(' ');
            return ret;
        }

        private void SendIdentifyDevice(int d)
        {
            drive = d;
            s = State.StartupIdentifySent;

            cmd.Write(cmd.Addr64 + DEVICE, 1, (uint)d << 4);
            Wait400ns();
            cmd.Write(cmd.Addr64 + COMMAND, 1, IDENTIFY_DRIVE);
        }
    }
}
