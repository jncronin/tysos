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
using tysos.lib;
using tysos.Resources;

namespace ata
{
    partial class ata : tysos.lib.VirtualDirectoryServer, tysos.Interfaces.IFileSystem
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
        internal class DeviceInfo
        {
            public enum DeviceType { ATA, ATAPI, SATA, SATAPI };
            public DeviceType Type;

            public uint SectorSize;
            public ulong SectorCount;
            public bool Lba48;

            public string SerialNumber, FirmwareRevision, ModelNumber;

            public int MaxPIO, MaxUDMA;

            public int Id;
        }

        DeviceInfo[] Devices;

        /* Command object */
        internal class Cmd
        {
            public bool is_write;
            public ulong sector_idx;
            public ulong cur_sector;
            public ulong sector_count;
            public byte[] buf;
            public int buf_offset;

            public int cur_cmd_sector_count;
            public int cur_cmd_sector_idx;

            public DeviceInfo d;

            public vfs.BlockEvent ev;
        }

        Cmd cur_cmd;
        internal tysos.Collections.Queue<Cmd> cmds = new tysos.Collections.Queue<Cmd>();

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

            root.Add(new File.Property { Name = "class", Value = "bus" });
            Tags.Add("class");

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
            ReadCompleted,
            WriteCompleted,
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
                case State.ReadSent:
                case State.WriteSent:
                    if((Status & 0x1) != 0)
                    {
                        System.Diagnostics.Debugger.Log(0, "ata", "Error in state " + s.ToString() +
                            ".  Error register: " + Error.ToString("X2"));
                        if (s == State.ReadSent)
                            cur_cmd.ev.Error = MonoIOError.ERROR_READ_FAULT;
                        else
                            cur_cmd.ev.Error = MonoIOError.ERROR_WRITE_FAULT;

                        s = State.Idle;
                        return;
                    }

                    /* No error - do read/write */
                    cur_cmd.cur_cmd_sector_idx += 1;
                    cur_cmd.ev.SectorsTransferred++;
                    if (s == State.ReadSent)
                    {
                        for (int i = 0; i < 256; i++)
                        {
                            ushort val = (ushort)cmd.Read(cmd.Addr64 + DATA, 2);
                            cur_cmd.buf[cur_cmd.buf_offset] = (byte)(val & 0xffU);
                            cur_cmd.buf[cur_cmd.buf_offset + 1] = (byte)(val >> 8);
                            cur_cmd.buf_offset += 2;
                        }
                    }
                    else
                    {
                        for(int i = 0; i < 256; i++)
                        {
                            ushort val = cur_cmd.buf[cur_cmd.buf_offset];
                            val |= (ushort)((ushort)cur_cmd.buf[cur_cmd.buf_offset + 1] << 8);
                            cur_cmd.buf_offset += 2;
                            cmd.Write(cmd.Addr64 + DATA, 2, val);
                        }
                    }

                    if (cur_cmd.cur_cmd_sector_idx == cur_cmd.cur_cmd_sector_count)
                    {
                        /* The current command has finished, however we may need to
                        execute another if sector_count > 256 */

                        if (cur_cmd.sector_count != 0)
                        {
                            SendDeviceCommand(cur_cmd);
                            return;
                        }
                        else
                        {
                            /* The command has completed */
                            cur_cmd.ev.Error = MonoIOError.ERROR_SUCCESS;
                            cur_cmd.ev.Set();

                            cur_cmd = null;
                            s = State.Idle;
                            return;
                        }
                    }

                    return;
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
                //System.Diagnostics.Debugger.Log(0, "ata", "Device " + d.ToString() + " idx " + i.ToString() + ": " + id[i].ToString("X4"));
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

            Devices[d].Id = d;

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

            /* Generate child object */
            string child_name = "device_" + d.ToString();
            List<tysos.lib.File.Property> props = new List<File.Property>();
            props.Add(new File.Property { Name = "serial", Value = Devices[d].SerialNumber });
            props.Add(new File.Property { Name = "model", Value = Devices[d].SerialNumber });
            props.Add(new File.Property { Name = "driver", Value = "disk" });
            props.Add(new File.Property { Name = "blockdev", Value = new Drive(Devices[d], this) });
            children.Add(child_name, props);
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

        public override void MessageLoop()
        {
            t = Syscalls.SchedulerFunctions.GetCurrentThread();
            Syscalls.SchedulerFunctions.GetCurrentThread().owning_process.MessageServer = this;

            if (InitServer() == false)
            {
                System.Diagnostics.Debugger.Log(0, null, "InitServer failed");
                return;
            }

            while (s != State.Idle) ;
            System.Diagnostics.Debugger.Log(0, null, "entering message loop");

            while (true)
            {
                IPCMessage msg = null;
                do
                {
                    msg = Syscalls.IPCFunctions.ReadMessage();

                    if (msg != null)
                        HandleMessage(msg);
                } while (msg != null);

                BackgroundProc();

                /* Block on either new messages or having a non-empty command queue */
                Syscalls.SchedulerFunctions.Block(new DelegateEvent(
                    delegate ()
                    {
                        if (t.owning_process.MessagePending == true)
                            return true;
                        if (cmds.Count != 0)
                            return true;
                        return false;
                    }));
            }
        }

        protected override void BackgroundProc()
        {
            if (cur_cmd != null)
                return;
            if (s != State.Idle)
                return;

            Cmd next_cmd;
            lock(cmds)
            {
                next_cmd = cmds.GetFirst();
            }
            if (next_cmd == null)
                return;

            System.Diagnostics.Debugger.Log(0, "ata", "Processing " + (next_cmd.is_write ? "write" : "read") + " command");

            /* Send the command and update current state */
            if (next_cmd.is_write)
                s = State.WriteSent;
            else
                s = State.ReadSent;

            cur_cmd = next_cmd;
            drive = next_cmd.d.Id;

            SendDeviceCommand(next_cmd);
        }
        
        void SendDeviceCommand(Cmd next_cmd)
        {
            System.Diagnostics.Debugger.Log(0, "ata", "SendDeviceCommand: is_write: " + next_cmd.is_write.ToString() +
                ", cur_sector: " + next_cmd.cur_sector.ToString() +
                ", sector_count: " + next_cmd.sector_count.ToString() +
                ", drive: " + drive.ToString());
            /* Build device command */
            byte lba_1 = (byte)(next_cmd.cur_sector & 0xffU);
            byte lba_2 = (byte)((next_cmd.cur_sector >> 8) & 0xffU);
            byte lba_3 = (byte)((next_cmd.cur_sector >> 16) & 0xffU);
            byte lba_4 = (byte)((next_cmd.cur_sector >> 24) & 0x0fU);

            byte sector_count = (byte)(next_cmd.sector_count & 0xffU);
            if(sector_count == 0)
            {
                next_cmd.cur_sector += 0x100;
                next_cmd.sector_count -= 0x100;
                next_cmd.cur_cmd_sector_count = 0x100;
            }
            else
            {
                next_cmd.cur_sector += sector_count;
                next_cmd.sector_count -= sector_count;
                next_cmd.cur_cmd_sector_count = sector_count;
            }
            next_cmd.cur_cmd_sector_idx = 0;

            cmd.Write(cmd.Addr64 + DEVICE, 1, ((uint)drive << 4) | 0x40U | lba_4);
            Wait400ns();
            cmd.Write(cmd.Addr64 + LBALO, 1, lba_1);
            cmd.Write(cmd.Addr64 + LBAMID, 1, lba_2);
            cmd.Write(cmd.Addr64 + LBAHI, 1, lba_3);
            cmd.Write(cmd.Addr64 + SECTOR_COUNT, 1, sector_count);

            if (next_cmd.is_write)
                cmd.Write(cmd.Addr64 + COMMAND, 1, 0x30U);
            else
                cmd.Write(cmd.Addr64 + COMMAND, 1, 0x20U);
        }
    }
}
