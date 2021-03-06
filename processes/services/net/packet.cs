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

using System.Collections.Generic;
using tysos;

namespace net
{
    partial class net
    {
        internal Dictionary<ushort, IPacketHandler> packet_handlers =
            new Dictionary<ushort, IPacketHandler>(
                new tysos.Program.MyGenericEqualityComparer<ushort>());

        public RPCResult<bool> RegisterPacketHandler(ushort id, IPacketHandler handler)
        {
            lock(packet_handlers)
            {
                packet_handlers[id] = handler;
            }
            return true;
        }

        public RPCResult<bool> PacketReceived(byte[] packet, int dev_no, int payload_offset,
            int payload_len, p_addr devsrc)
        {
            HWAddr dest = ReadHWAddr(packet, payload_offset);
            HWAddr src = ReadHWAddr(packet, payload_offset + 6);

            /* Whole packet is 6 bytes dest MAC, 6 bytes source MAC,
                optional 4 bytes VLAN packet, 2 bytes ethertype,
                payload, 4 bytes frame check sequence (FCS) */
            payload_len -= 18;
            payload_offset += 14;

            if(packet[14] == 0x81 && packet[15] == 0x00)
            {
                /* This is a VLAN tagged packet
                    We don't do anything special (yet) */
                payload_len -= 4;
                payload_offset += 4;
            }

            ushort ethertype = (ushort)(((uint)packet[payload_offset - 2] << 8) |
                packet[payload_offset - 1]);

            System.Diagnostics.Debugger.Log(0, null, "Received packet of size " +
                packet.Length.ToString() + ", ethertype: " + ethertype.ToString("X4") +
                " from device " + dev_no.ToString());

            if (packet_handlers.ContainsKey(ethertype))
            {
                packet_handlers[ethertype].PacketReceived(packet, dev_no, payload_offset, payload_len, src);
            }

            return true;
        }

        public void TransmitEthernetPacket(byte[] packet, int dev_no, int payload_offset,
            int payload_len, p_addr dest, ushort etype)
        {
            /* Build an ethernet packet from the higher level packet passed to us.

                First, decide if we have enough space in the original buffer to
                build the packet around.  We need 14 bytes before the packet and
                optionally 4 bytes after if the device doesn't add FCS.

                TODO: add support also for padding here */

            var nd = devs[dev_no];

            bool need_new_buf = false;
            int header_len = 14;
            if (payload_offset < header_len)
                need_new_buf = true;
            int crc_len = 0;
            if (nd.dev_appends_crc_on_tx == false)
                crc_len = 4;
            if (packet.Length < payload_offset + payload_len + crc_len)
                need_new_buf = true;

            if(need_new_buf)
            {
                byte[] new_buf = new byte[header_len + crc_len + payload_len];
                for (int i = 0; i < payload_len; i++)
                    new_buf[header_len + i] = packet[payload_offset + i];
                payload_offset = header_len;
                packet = new_buf;
            }

            // Add in the header
            payload_offset -= header_len;
            payload_len += header_len;

            // dest
            var d = dest as HWAddr;
            if(d == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "TransmitEthernetPacket: "
                    + "dest is not a hardware address: " + dest.ToString());
                return;
            }
            for (int i = 0; i < 6; i++)
                packet[payload_offset + i] = d.MAC[i];

            // src
            for (int i = 0; i < 6; i++)
                packet[payload_offset + 6 + i] = nd.HWAddr.MAC[i];

            // ethertype
            packet[payload_offset + 12] = (byte)((etype >> 8) & 0xff);
            packet[payload_offset + 13] = (byte)(etype & 0xff);

            // TODO: pad if required
            if(nd.dev_pads_on_tx == false && payload_len + crc_len < 64)
            {
                System.Diagnostics.Debugger.Log(0, null, "TransmitEthernetPacket: "
                    + "packet too short and device does not automatically pad");
                return;
            }

            // TODO: add FCS if required
            if(nd.dev_appends_crc_on_tx == false)
            {
                System.Diagnostics.Debugger.Log(0, null, "TransmitEthernetPacket: "
                    + "device does not automatically add FCS");
                return;
            }

            // Send packet
            nd.s.TransmitPacket(packet, dev_no, payload_offset, payload_len, dest);
        }
    }
}
