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

namespace net
{
    class ipv4 : ServerObject
    {
        net net;
        icmp icmp;
        internal Dictionary<byte, ServerObject> packet_handlers =
            new Dictionary<byte, ServerObject>(
                new tysos.Program.MyGenericEqualityComparer<byte>());

        public override bool InitServer()
        {
            net = Syscalls.ProcessFunctions.GetSpecialProcess(Syscalls.ProcessFunctions.SpecialProcessType.Net)
                as net;

            lock (net.packet_handlers)
            {
                net.packet_handlers[0x0800] = this;
            }

            /* Start protocol handlers */
            icmp = new icmp(this);
            tysos.Process p_icmp = tysos.Process.CreateProcess("icmp",
                new System.Threading.ThreadStart(icmp.MessageLoop),
                new object[] { icmp });
            p_icmp.Start();

            return true;
        }

        public void PacketReceived(byte[] packet, int dev_no, int payload_offset,
            int payload_len, p_addr src)
        {
            /* Parse the provided packet */

            // check version
            uint ver = (uint)(packet[payload_offset] >> 4) & 0xfU;
            if(ver != 4)
            {
                System.Diagnostics.Debugger.Log(0, null, "Packet dropped as version incorrect: " +
                    ver.ToString());
                return;
            }

            // get header length (in bytes)
            int hlen = (int)((packet[payload_offset] & 0xfU) * 4);

            // checksum
            uint csum = calc_checksum(packet, payload_offset, hlen);
            if(csum != 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Packet dropped as checksum incorrect: ");
                sb.Append(csum.ToString());
                sb.Append(".  Packet header length: ");
                sb.Append(hlen.ToString());
                sb.Append(", words: ");
                for(int i = 0; i < hlen; i+= 2)
                {
                    if (i != 0)
                        sb.Append(" ");
                    sb.Append(net.ReadWord(packet, payload_offset + i).ToString("X4"));
                }
                System.Diagnostics.Debugger.Log(0, null, sb.ToString());
                return;
            }

            // get packet length (excluding header)
            int plen = net.ReadWord(packet, payload_offset + 2) - hlen;

            // get fragment offset and flags field
            ushort frag_off_flags = net.ReadWord(packet, payload_offset + 6);
            uint flags = (uint)(frag_off_flags >> 13) & 0x3U;

            // don't handle fragmented packets yet
            if(flags != 0)
            {
                System.Diagnostics.Debugger.Log(0, null, "Packet dropped as fragmented");
                return;
            }

            // get protocol
            byte prot = packet[payload_offset + 9];

            // get source and dest IPs
            IPv4Address spa = net.ReadIPv4Addr(packet, payload_offset + 12);
            IPv4Address dpa = net.ReadIPv4Addr(packet, payload_offset + 16);

            System.Diagnostics.Debugger.Log(0, null, "Received packet from " +
                spa.ToString() + " to " + dpa.ToString() + ", protocol 0x" +
                prot.ToString("X2"));

            // Do we accept the destination address?
            bool found = false;
            foreach(var addr in net.addrs)
            {
                // check octet by octet so we also catch broadcast addresses
                IPv4Address tpa = addr.Key as IPv4Address;
                if(tpa != null)
                {
                    bool pass = true;
                    for(int i = 0; i < 4; i++)
                    {
                        uint mask = 0xffU << ((3 - i) * 8);
                        uint dpa_masked = dpa.addr & mask;
                        if (dpa_masked != mask && dpa_masked != (tpa.addr & mask))
                        {
                            pass = false;
                            break;                            
                        }
                    }
                    if(pass)
                    {
                        found = true;
                        break;
                    }
                }         
            }

            if(found)
            {
                ServerObject prot_handler;
                if(packet_handlers.TryGetValue(prot, out prot_handler))
                {
                    prot_handler.InvokeAsync("PacketReceived",
                        new object[] { packet, dev_no, payload_offset + hlen, plen, spa },
                        net.sig_packet);
                }
            }
        }

        internal static uint calc_checksum(byte[] packet, int payload_offset, int len)
        {
            /* "16-bit one's complement of the one's complement sum of all 16-bit
                words in the header" - see https://en.wikipedia.org/wiki/IPv4#Header_Checksum */

            uint sum = 0;
            for(int i = 0; i < len; i+= 2)
                sum += net.ReadWord(packet, payload_offset + i);
            uint sum2 = (sum & 0xffff) + (sum >> 16);   // add in carry
            uint sum3 = (sum2 & 0xffff) + (sum2 >> 16); // in case above sum carried
            uint sum4 = ~sum2 & 0xffff;

            return sum4;
        }
    }
}
