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
    class icmp : ServerObject, IPacketHandler
    {
        ipv4 ip;

        public icmp(ipv4 IP)
        {
            ip = IP;
        }

        public override bool InitServer()
        {
            lock (ip.packet_handlers)
            {
                ip.packet_handlers[1] = this;
            }

            return true;
        }

        public RPCResult<bool> PacketReceived(byte[] packet, int dev_no, int payload_offset,
            int payload_len, p_addr src)
        {
            /* Parse the provided packet */

            byte type = packet[payload_offset];
            byte code = packet[payload_offset + 1];
            uint csum = ipv4.calc_checksum(packet, payload_offset, payload_len);
            if(csum != 0)
            {
                System.Diagnostics.Debugger.Log(0, null, "Received ICMP message with " +
                    "invalid checksum " + csum.ToString("X4"));
                return false;
            }
            ushort ident = net.ReadWord(packet, payload_offset + 4);
            ushort seq = net.ReadWord(packet, payload_offset + 6);

            System.Diagnostics.Debugger.Log(0, null, "ICMP message received from " +
                src.ToString() + ": type: " + type.ToString() + ", code: " +
                code.ToString() + ", ident: " + ident.ToString() + ", seq: " +
                seq.ToString());

            return true;
        }
    }
}
