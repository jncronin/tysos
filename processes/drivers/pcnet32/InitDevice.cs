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

namespace pcnet32
{
    partial class pcnet32 : tysos.lib.VirtualFileServer
    {
        tysos.RangeResource io = null;
        InterruptLine irq = null;
        tysos.VirtualMemoryResource64 buf = null;
        ServerObject net;
        int dev_no;
        net.HWAddr hwaddr;

        TransmitRBE[] txbufs;
        ReceiveRBE[] rxbufs;

        int cur_tx_buf = 0;
        int cur_rx_buf = 0;

        internal pcnet32(tysos.lib.File.Property[] Properties)
        {
            root.AddRange(Properties);
        }

        public override bool InitServer()
        {
            System.Diagnostics.Debugger.Log(0, "pcnet32", "PCNET32 driver started");

            while (Syscalls.ProcessFunctions.GetSpecialProcess(tysos.Syscalls.ProcessFunctions.SpecialProcessType.Net) == null) ;
            net = Syscalls.ProcessFunctions.GetSpecialProcess(tysos.Syscalls.ProcessFunctions.SpecialProcessType.Net);

            /* Get our ports and interrupt */
            foreach (var r in root)
            {
                if (r.Name == "interrupt" && (r.Value is tysos.Resources.InterruptLine))
                {
                    irq = r.Value as tysos.Resources.InterruptLine;
                }
                else if (r.Name == "vmem" && (r.Value is VirtualMemoryResource64))
                    buf = r.Value as VirtualMemoryResource64;
            }
            var pciconf = pci.PCIConfiguration.GetPCIConf(root);

            io = pciconf.GetBAR(0);

            if (io == null || irq == null || buf == null)
            {
                System.Diagnostics.Debugger.Log(0, "ata", "Insufficient resources provided");
                return false;
            }

            buf.Map();

            System.Diagnostics.Debugger.Log(0, null, "Using " + io.ToString() + ", " + irq.ToString() + " and " + buf.ToString());

            // Enable PCI IO space access
            uint c_04 = pciconf.ReadConfig(0x4);
            c_04 &= 0xffff0000U;
            c_04 |= 0x5;    // IO space + bus mastering
            pciconf.WriteConfig(0x4, c_04);

            // Reset the controller - try both dword and word reads as we don't
            //  know what mode the controller is currently in
            io.Read(io.Addr32 + 0x18, 4);
            io.Read(io.Addr32 + 0x14, 2);

            // wait 1 us - TODO: use timer
            for (int i = 0; i < 100000; i++) ;

            // Set DWORD mode
            io.Write(io.Addr32 + 0x10, 4, 0);

            // Read our MAC address
            uint mac03 = io.Read(io.Addr32 + 0x0, 4);
            uint mac47 = io.Read(io.Addr32 + 0x4, 4);
            hwaddr = new net.HWAddr();
            byte[] mac = hwaddr.MAC;
            mac[0] = (byte)(mac03 & 0xff);
            mac[1] = (byte)((mac03 >> 8) & 0xff);
            mac[2] = (byte)((mac03 >> 16) & 0xff);
            mac[3] = (byte)((mac03 >> 24) & 0xff);
            mac[4] = (byte)(mac47 & 0xff);
            mac[5] = (byte)((mac47 >> 8) & 0xff);

            StringBuilder sb = new StringBuilder("MAC address: ");
            for (int i = 0; i < 6; i++)
            {
                if (i != 0)
                    sb.Append(":");
                sb.Append(mac[i].ToString("X2"));
            }

            System.Diagnostics.Debugger.Log(0, null, sb.ToString());

            // Register interrupt handler
            irq.RegisterHandler(Interrupt);

            // Set SWSTYLE to 2 (PCnet-PCI II), 32 bit data accesses + addresses
            uint sstyle = ReadCSR(58);
            sstyle &= 0xfff0;
            sstyle |= 2;
            WriteCSR(58, sstyle);
            sstyle = ReadCSR(58);
            System.Diagnostics.Debugger.Log(0, null, "CSR58: " + sstyle.ToString("X4"));

            /* Set BCR2.ASEL to one (automatic link type detection - should
                already be done by H_RESET, but force it to set incase firmware
                has altered it). */
            uint bcr2 = ReadBCR(2);
            bcr2 |= 0x2;
            WriteBCR(2, bcr2);

            /* Calculate how many buffers we can make */
            ulong buf_size = 1548;    // 1544 is used in linux - we round up to a multiple of 16
            ulong rde_size = 16;
            ulong init_blk_size = 32;   // actually 28, aligned to 16 bytes
            ulong buf_count = (buf.Length64 - init_blk_size) / (buf_size + rde_size);

            // split along the lines of 1 transmit buffer to 2 receive buffers
            ulong tx_count = buf_count / 3;
            // reduce it to be the greatest power of 2 less than current count
            int tx_bit_no = -1;
            for (int i = 31; i >= 0; i--)
            {
                if (((1UL << i) & tx_count) != 0)
                {
                    tx_bit_no = i;
                    break;
                }
            }
            if (tx_bit_no <= 0)
            {
                System.Diagnostics.Debugger.Log(0, null, "Insufficient buffer space provided");
                return false;
            }
            if (tx_bit_no > 9)
                tx_bit_no = 9;
            tx_count = 1UL << tx_bit_no;
            int rx_bit_no = tx_bit_no + 1;
            if (rx_bit_no > 9)
                rx_bit_no = 9;
            ulong rx_count = 1UL << rx_bit_no;

            System.Diagnostics.Debugger.Log(0, null, "Creating " + rx_count.ToString() +
                " receive buffers and " + tx_count.ToString() + " transmit buffers");

            // set up ring buffers - see spec p. 62
            uint rdra_offset; // Offset to start of receive ring buffer (within our own buffer)
            uint tdra_offset; // Offset to start of transmit ring buffer (within our own buffer)
            rdra_offset = (uint)init_blk_size;
            tdra_offset = (uint)(rdra_offset + rde_size * rx_count);
            uint rd_offset;     // Offset to start of receive data
            uint td_offset;     // Offset to start of transmit data
            rd_offset = (uint)(tdra_offset + rde_size * tx_count);
            td_offset = (uint)(rd_offset + buf_size * rx_count);

            rxbufs = new ReceiveRBE[(int)rx_count];
            txbufs = new TransmitRBE[(int)tx_count];

            for (int i = 0; i < (int)rx_count; i++)
            {
                rxbufs[i] = new ReceiveRBE(buf, (int)(rdra_offset + i * (int)rde_size),
                    (int)(rd_offset + i * (int)buf_size), i, (int)buf_size);
            }
            for (int i = 0; i < (int)tx_count; i++)
            {
                txbufs[i] = new TransmitRBE(buf, (int)(tdra_offset + i * (int)rde_size),
                    (int)(td_offset + i * (int)buf_size), i, (int)buf_size);
            }

            // Begin setting up the initialization block (p. 156 in spec) - SSIZE32 = 1
            byte[] init_blk = buf.ToArray();

            byte rlen = (byte)rx_bit_no;  // RLEN = log2 of number of receive buffer entries (max = 9, i.e. 512 entries)
            byte tlen = (byte)tx_bit_no;  // TLEN = log2 of number of receive buffer entries (max = 9, i.e. 512 entries)

            init_blk[0] = 0;    // MODE = 0
            init_blk[1] = 0;    // MODE = 0
            init_blk[2] = (byte)(0 | (rlen << 4));      // reserved / rlen 
            init_blk[3] = (byte)(0 | (tlen << 4));      // reserved / tlen
            for (int i = 0; i < 6; i++)
                init_blk[4 + i] = mac[i];               // PADR
            init_blk[0xa] = 0;  // reserved
            init_blk[0xb] = 0;  // reserved
            for (int i = 0; i < 8; i++)
                init_blk[0xc + i] = 0;                  // LADRF - disable logical addressing

            byte[] rdra_arr = BitConverter.GetBytes(buf.MappedTo.Addr32 + rdra_offset);
            for (int i = 0; i < 4; i++)
                init_blk[0x14 + i] = rdra_arr[i];

            byte[] tdra_arr = BitConverter.GetBytes(buf.MappedTo.Addr32 + tdra_offset);
            for (int i = 0; i < 4; i++)
                init_blk[0x18 + i] = tdra_arr[i];

            // write init block address to CSR1 + 2
            uint ibaddr = buf.MappedTo.Addr32;
            WriteCSR(1, ibaddr & 0xffffU);
            WriteCSR(2, (ibaddr >> 16) & 0xffffU);

            // mask out IDON interrupt, ensure little endian mode
            uint csr3 = (1U << 8);
            WriteCSR(3, csr3);

            // set automatic frame padding
            uint csr4 = ReadCSR(4);
            csr4 |= (1U << 11);
            WriteCSR(4, csr4);

            // set init and interrupt enable in CSR 0
            uint csr0 = (1U << 0) | (1U << 6);
            WriteCSR(0, csr0);

            // poll until IDON set
            while ((csr0 & 1U << 8) == 0)
                csr0 = ReadCSR(0);

            // handle setting start and stop bits (CSR0) in separate Start/Stop functions
            
            root.Add(new File.Property { Name = "class", Value = "netdev" });
            Tags.Add("class");

            return true;
        }

        void WriteRAP(uint val)
        {
            io.Write(io.Addr32 + 0x14, 4, val & 0xffff);
        }

        void WriteCSR(uint reg, uint val)
        {
            WriteRAP(reg);
            io.Write(io.Addr32 + 0x10, 4, val & 0xffff);
        }

        uint ReadCSR(uint reg)
        {
            WriteRAP(reg);
            return io.Read(io.Addr32 + 0x10, 4) & 0xffff;
        }

        void WriteBCR(uint reg, uint val)
        {
            WriteRAP(reg);
            io.Write(io.Addr32 + 0x1c, 4, val & 0xffff);
        }

        uint ReadBCR(uint reg)
        {
            WriteRAP(reg);
            return io.Read(io.Addr32 + 0x1c, 4) & 0xffff;
        }

        bool Interrupt()
        {
            uint csr0 = ReadCSR(0);
            uint csr4 = ReadCSR(4);
            uint ints = csr0;
            System.Diagnostics.Debugger.Log(0, "pcnet", "INTERRUPT: CSR0: " + csr0.ToString("X4") +
                ", CSR4: " + csr4.ToString("X4"));


            // Is RINT set?
            if((ints & 0x0400) != 0)
            {
                /* Loop through those buffers we own */
                int i = cur_rx_buf;

                do
                {
                    ReceiveRBE b = rxbufs[i];

                    if (b.DriverOwns)
                    {
                        uint dlen = b.MCNT;
                        ushort flags = b.Flags;

                        System.Diagnostics.Debugger.Log(0, "pcnet", "Receive buffer " +
                            i.ToString() + " has message of length " + dlen.ToString() +
                            ", flags: " + flags.ToString("X4"));

                        /*
                        byte[] d = b.Buffer;
                        StringBuilder sb = null;
                        for(uint j = 0; j < dlen; j++)
                        {
                            if((j % 8) == 0)
                            {
                                if (sb != null)
                                    System.Diagnostics.Debugger.Log(0, "pcnet", sb.ToString());

                                sb = new StringBuilder(j.ToString("X4"));
                                sb.Append(":");
                            }

                            sb.Append(" ");
                            sb.Append(d[j].ToString("X2"));
                        }
                        if (sb != null)
                            System.Diagnostics.Debugger.Log(0, "pcnet", sb.ToString()); */

                        // Send the message to the net process
                        if(b.Error == false)
                        {
                            byte[] msg = new byte[dlen];
                            for (uint j = 0; j < dlen; j++)
                                msg[j] = b.Buffer[j];
                            net.InvokeAsync("PacketReceived", new object[] { msg, dev_no, 0, msg.Length, null },
                                global::net.net.sig_packet);
                        }

                        // reset the descriptor entry
                        b.Reset();
                    }
                    else
                        break;

                    i = next_rx_buf(i);
                } while (i != cur_rx_buf);

                // advance rx pointer
                cur_rx_buf = i;
            }

            // Mark all interrupts as handled
            csr0 |= 0x7f00;
            WriteCSR(0, csr0);
            csr4 |= 0x26a;
            WriteCSR(4, csr4);

            return true;
        }

        int next_tx_buf(int cur_idx)
        {
            int ret = cur_idx + 1;

            if (ret >= txbufs.Length)
                ret = 0;

            return ret;
        }

        int next_rx_buf(int cur_idx)
        {
            int ret = cur_idx + 1;

            if (ret >= rxbufs.Length)
                ret = 0;

            return ret;
        }

        public void RegisterDevNo(int dn)
        {
            if (SourceThread.owning_process.MessageServer != 
                Syscalls.ProcessFunctions.GetSpecialProcess(Syscalls.ProcessFunctions.SpecialProcessType.Net))
            {
                System.Diagnostics.Debugger.Log(0, null, "Process " +
                    SourceThread.ProcessName + " tried to call RegisterDevNo");
                return;
            }

            System.Diagnostics.Debugger.Log(0, null, "Allocated network device number " +
                dn.ToString());

            dev_no = dn;
        }

        public void Start()
        {
            if (SourceThread.owning_process.MessageServer !=
                Syscalls.ProcessFunctions.GetSpecialProcess(Syscalls.ProcessFunctions.SpecialProcessType.Net))
            {
                System.Diagnostics.Debugger.Log(0, null, "Process " +
                    SourceThread.ProcessName + " tried to call Start");
                return;
            }

            System.Diagnostics.Debugger.Log(0, null, "Starting");
            uint csr0 = ReadCSR(0);

            // Clear both STOP and INIT bits, set STRT bit
            csr0 &= ~(1U << 2);
            csr0 &= ~(1U << 0);
            csr0 |= (1U << 1);

            WriteCSR(0, csr0);
        }

        public net.p_addr GetHardwareAddress()
        {
            return hwaddr;
        }

        public void Stop()
        {
            if (SourceThread.owning_process.MessageServer !=
                Syscalls.ProcessFunctions.GetSpecialProcess(Syscalls.ProcessFunctions.SpecialProcessType.Net))
            {
                System.Diagnostics.Debugger.Log(0, null, "Process " +
                    SourceThread.ProcessName + " tried to call Stop");
                return;
            }

            System.Diagnostics.Debugger.Log(0, null, "Stopping"); uint csr0 = ReadCSR(0);

            // Clear both STRT and INIT bits, set STOP bit
            csr0 &= ~(1U << 1);
            csr0 &= ~(1U << 0);
            csr0 |= (1U << 2);

            WriteCSR(0, csr0);
        }

        public bool TransmitPacket(byte[] packet, int dev_no, int packet_offset,
            int packet_len, net.p_addr dest)
        {
            if(txbufs[cur_tx_buf].DriverOwns == false)
            {
                /* No free transmit buffers.  Signal the driver to try and
                send now, but fail the current packet */
                uint csr0 = ReadCSR(0);
                csr0 |= (1U << 3);
                WriteCSR(0, csr0);
                return false;
            }

            TransmitRBE t = txbufs[cur_tx_buf];

            t.Reset();
            uint len = (uint)packet_len;
            byte[] b = t.Buffer;
            for (uint i = 0; i < len; i++)
                b[i] = packet[packet_offset + i];
            t.STP = true;
            t.ENP = true;
            t.BCNT = len;
            t.DriverOwns = false;

            cur_tx_buf = next_tx_buf(cur_tx_buf);

            return true;                      
        }

        class ReceiveRBE : RingBufferEntry
        {
            public ReceiveRBE(tysos.VirtualMemoryResource64 vmem,
                int offset_to_rde, int offset_to_data, int index,
                int len) : base(vmem, offset_to_rde, offset_to_data, index,
                    false, len)
            { }

            public uint MCNT { get { return b[o + 8] | ((uint)b[o + 9] << 8); } }
            public ushort Flags { get { return (ushort)(b[o + 6] | ((uint)b[o + 7] << 8)); } }

            public void Reset()
            {
                // Leave first 6 bytes intact (buffer address and length), clear all
                //  others, and toggle ownership bit (last of byte 7) back to 1 to
                //  hand the buffer back to the controller
                b[o + 6] = 0;
                b[o + 8] = 0;
                b[o + 9] = 0;
                b[o + 10] = 0;
                b[o + 11] = 0;

                // 12 through 15 are reserved bits - don't touch them

                // toggle ownership last (in case controller is polling the buffer entries)
                b[o + 7] = 0x80;
            }
        }

        class TransmitRBE : RingBufferEntry
        {
            public TransmitRBE(tysos.VirtualMemoryResource64 vmem,
                int offset_to_rde, int offset_to_data, int index,
                int len) : base(vmem, offset_to_rde, offset_to_data, index,
                    true, len)
            { }

            public uint BCNT {
                get
                {
                    unchecked
                    {
                        uint ret = b[o + 4] | ((uint)b[o + 5] << 8);
                        ret = ~ret + 1;
                        return ret;
                    }
                }
                set
                {
                    unchecked
                    {
                        uint val = ~value + 1;
                        b[o + 4] = (byte)(val & 0xffU);
                        b[o + 5] = (byte)((val >> 8) & 0xffU);
                    }
                }
            }

            public bool STP
            {
                get { return (b[o + 7] & 0x2) != 0; }
                set
                {
                    if (value)
                        b[o + 7] |= 0x2;
                    else
                        b[o + 7] &= 0xfd;
                }
            }

            public bool ENP
            {
                get { return (b[o + 7] & 0x1) != 0; }
                set
                {
                    if (value)
                        b[o + 7] |= 0x1;
                    else
                        b[o + 7] &= 0xfe;
                }
            }

            public void Reset()
            {
                // Reset descriptor so we're ready to write to it again
                if (!DriverOwns)
                    return;
                b[o + 4] = 0;
                b[o + 5] = 0xf0;
                b[o + 6] = 0;
                b[o + 7] = 0;
                b[o + 8] = 0;
                b[o + 9] = 0;
                b[o + 0xa] = 0;
                b[o + 0xb] = 0;
            }
        }

        abstract class RingBufferEntry
        {
            public int idx;
            public bool is_tx;
            public int length = 1544;
            public ulong buf_addr;

            protected byte[] b;
            protected int o;

            protected byte[] d;

            public bool DriverOwns
            {
                get { return (b[o + 7] & 0x80) == 0; }
                set
                {
                    if (value == false)
                        b[o + 7] |= 0x80;
                    else
                        b[o + 7] &= 0x7f;
                }
            }

            public bool Error { get { return (b[0 + 7] & 0x40) != 0; } }
            public byte[] Buffer { get { return d; } }

            public RingBufferEntry(tysos.VirtualMemoryResource64 vmem,
                int offset_to_rde, int offset_to_data, int index, bool is_transmit,
                int len)
            {
                length = len;
                idx = index;
                is_tx = is_transmit;

                VirtualMemoryResource64 new_vmem = vmem.Split(vmem.Addr64 + (ulong)offset_to_data,
                    (ulong)length) as VirtualMemoryResource64;
                d = new_vmem.ToArray();

                buf_addr = vmem.MappedTo.Addr64 + (ulong)offset_to_data;

                InitRDE(vmem.ToArray(), offset_to_rde);
            }

            void InitRDE(byte[] buf, int offset)
            {
                b = buf;
                o = offset;
                
                /* Set up a new ring buffer entry for this particular buffer.
                See spec p. 159, table 39 */

                /* First 4 bytes are physical address of buffer */
                byte[] addr_arr = BitConverter.GetBytes(buf_addr);
                for (int i = 0; i < 4; i++)
                    buf[offset + i] = addr_arr[i];

                /* Next two bytes are 0xf000 OR'd with the first 12 bits of the
                2s complement of the length */
                byte[] len_arr = BitConverter.GetBytes(-length);
                buf[offset + 0x4] = len_arr[0];
                buf[offset + 0x5] = (byte)(len_arr[1] | 0xf0);

                /* Next two bytes are error bits - we clear them all.  The last bit
                in this sequence however is the ownership bit - For receive buffers we
                let the device own the buffer, and for transmit ones the driver owns
                it initially */
                buf[offset + 0x6] = 0;
                if (is_tx)
                    buf[offset + 0x7] = 0;
                else
                    buf[offset + 0x7] = 0x80;

                /* Following bits are cleared to zero - descriptions are for
                receive buffers, but for uninitialized transmit buffers they are
                also zero */
                /* Next comes the message byte count.  As we are setting up the
                buffer for the first time, this is zero */
                buf[offset + 0x8] = 0;
                buf[offset + 0x9] = 0;

                /* Now are the runt packet count and receive collision count.
                Again, initialize to zero (they are set by the device) */
                buf[offset + 0xa] = 0;
                buf[offset + 0xb] = 0;

                /* Finally, 4 reserved bytes */
                buf[offset + 0xc] = 0;
                buf[offset + 0xd] = 0;
                buf[offset + 0xe] = 0;
                buf[offset + 0xf] = 0;
            }
        }
    }
}
