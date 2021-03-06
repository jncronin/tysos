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
    class arp : ServerObject, IPacketHandler, IArp
    {
        INetInternal _net;
        net net;

        /* cache is sorted first by network device, then protocol number, then
            finally by address */
        Dictionary<int, Dictionary<ushort, Dictionary<p_addr, HWAddr>>> cache =
            new Dictionary<int, Dictionary<ushort, Dictionary<p_addr, HWAddr>>>(
                new tysos.Program.MyGenericEqualityComparer<int>());

        /* This is a store of pending ARP requests */
        Dictionary<int, Dictionary<ushort, Dictionary<p_addr, List<RPCMessage>>>> pending_reqs =
            new Dictionary<int, Dictionary<ushort, Dictionary<p_addr, List<RPCMessage>>>>(
                new tysos.Program.MyGenericEqualityComparer<int>());

        public override bool InitServer()
        {
            _net = Syscalls.ProcessFunctions.GetNet() as INetInternal;
            net = _net as net;
            return _net.RegisterPacketHandler(0x0806, this).Sync();
        }

        public RPCResult<bool> PacketReceived(byte[] packet, int dev_no, int payload_offset,
            int payload_len, p_addr src)
        {
            ushort htype = net.ReadWord(packet, payload_offset);
            ushort ptype = net.ReadWord(packet, payload_offset + 2);
            byte hlen = packet[payload_offset + 4];
            byte plen = packet[payload_offset + 5];
            ushort oper = net.ReadWord(packet, payload_offset + 6);

            if(htype != 1)
            {
                System.Diagnostics.Debugger.Log(0, null, "Packet with unknown HTYPE: " +
                    htype.ToString("X4"));
                return false;
            }
            if(hlen != 6)
            {
                System.Diagnostics.Debugger.Log(0, null, "Invalid HLEN: " +
                    hlen.ToString());
                return false;
            }

            HWAddr sha = net.ReadHWAddr(packet, payload_offset + 8);
            HWAddr tha = net.ReadHWAddr(packet, payload_offset + 8 + hlen + plen);

            var nd = net.devs[dev_no];

            // Reject those packets sent by ourselves (don't reply to our own
            //  announce packets)
            if (sha.Equals(nd.HWAddr))
                return false;

            switch(ptype)
            {
                case 0x0800:
                    // IPv4
                    {
                        if (plen != 4)
                        {
                            System.Diagnostics.Debugger.Log(0, null, "Invalid HLEN: " +
                                hlen.ToString());
                            return false;
                        }

                        IPv4Address spa = net.ReadIPv4Addr(packet, payload_offset + 14);
                        IPv4Address tpa = net.ReadIPv4Addr(packet, payload_offset + 24);

                        System.Diagnostics.Debugger.Log(0, null, "Received IPv4 packet: oper: " +
                            oper.ToString() + ", SHA: " + sha.ToString() +
                            ", SPA: " + spa.ToString() +
                            ", THA: " + tha.ToString() +
                            ", TPA: " + tpa.ToString());

                        if(oper == 1)
                        {
                            /* Is it a request for our own IP address? */
                            //var addr = net.devs[dev_no].a as IPv4Address
                            if(net.devs[dev_no].a.Equals(tpa))
                            {
                                System.Diagnostics.Debugger.Log(0, null, "Received ARP request for our IP");

                                // build a response packet.  We are usually directly above
                                //  the link layer in the stack, so only need to reserve space
                                //  for ethernet headers (if we are not big enough, the
                                //  ethernet layer will do this for us anyway, but it involves
                                //  a memcpy and so is less efficient)

                                byte[] ret = new byte[100];
                                int packet_offset = 32;
                                int packet_len = 0;

                                ret[packet_offset + packet_len++] = 0x00;       // HTYPE
                                ret[packet_offset + packet_len++] = 0x01;
                                ret[packet_offset + packet_len++] = 0x08;       // PTYPE
                                ret[packet_offset + packet_len++] = 0x00;
                                ret[packet_offset + packet_len++] = 0x06;       // HLEN
                                ret[packet_offset + packet_len++] = 0x04;       // PLEN
                                ret[packet_offset + packet_len++] = 0x00;       // OPER
                                ret[packet_offset + packet_len++] = 0x02;       
                                
                                // SHA
                                for (int i = 0; i < 6; i++)
                                    ret[packet_offset + packet_len++] = net.devs[dev_no].HWAddr.MAC[i];
                                // SPA
                                for (int i = 0; i < 4; i++)
                                    ret[packet_offset + packet_len++] = tpa.Octets[i];
                                // THA
                                for (int i = 0; i < 6; i++)
                                    ret[packet_offset + packet_len++] = sha.MAC[i];
                                // TPA
                                for (int i = 0; i < 4; i++)
                                    ret[packet_offset + packet_len++] = spa.Octets[i];


                                /*net.InvokeAsync("TransmitPacket",
                                    new object[] { ret, dev_no, packet_offset, packet_len, sha },
                                    net.sig_packet);*/
                                net.TransmitEthernetPacket(ret, dev_no, packet_offset,
                                    packet_len, sha, 0x0806);
                            }
                            else if(tpa.Equals(spa) && tha.Equals(HWAddr.Zero))
                            {
                                // This is a gratuitous request (announce) packet
                                // Interpret it as a response
                                oper = 2;
                            }
                        }
                        if(oper == 2)
                        {
                            /* This is a reply to a previous request, or an announcement */

                            // Cache the current response
                            System.Diagnostics.Debugger.Log(0, null, "Cacheing " +
                                spa.ToString() + " to " + sha.ToString());
                            GetProtocolDictionary(dev_no, 0x0800)[spa] = sha;

                            // Fulfil pending requests
                            var pending_list = GetPendingRequestList(dev_no, 0x0800,
                                spa);
                            while(pending_list.Count > 0)
                            {
                                var e = pending_list[pending_list.Count - 1];
                                System.Diagnostics.Debugger.Log(0, null, "Handling pending request");
                                ((RPCResult<HWAddr>)e.result).Result = sha;
                                e.result.Set();
                                pending_list.RemoveAt(pending_list.Count - 1);
                            }

                            // Remove the pointer to the pending list so it is collected
                            pending_reqs[dev_no][0x0800].Remove(spa);
                        }
                    }
                    break;

                default:
                    System.Diagnostics.Debugger.Log(0, null, "Packet with unknown PTYPE: " +
                        htype.ToString("X4"));
                    return false;
            }

            return true;
        }

        Dictionary<p_addr, HWAddr> GetProtocolDictionary(int dev_no, ushort etype)
        {
            /* Get the address->hwaddr dictionary, creating the various layers
                as we go */
            Dictionary<ushort, Dictionary<p_addr, HWAddr>> nd_dict;
            if(cache.TryGetValue(dev_no, out nd_dict) == false)
            {
                nd_dict = new Dictionary<ushort, Dictionary<p_addr, HWAddr>>(
                    new tysos.Program.MyGenericEqualityComparer<ushort>());
                cache[dev_no] = nd_dict;
            }

            Dictionary<p_addr, HWAddr> prot_dict;
            if(nd_dict.TryGetValue(etype, out prot_dict) == false)
            {
                prot_dict = new Dictionary<p_addr, HWAddr>(
                    new tysos.Program.MyGenericEqualityComparer<p_addr>());
                nd_dict[etype] = prot_dict;
            }

            return prot_dict;
        }

        List<RPCMessage> GetPendingRequestList(int dev_no, ushort etype,
            p_addr addr)
        {
            /* Get the address->hwaddr dictionary, creating the various layers
                as we go */
            Dictionary<ushort, Dictionary<p_addr, List<RPCMessage>>> nd_dict;
            if (pending_reqs.TryGetValue(dev_no, out nd_dict) == false)
            {
                nd_dict = new Dictionary<ushort, Dictionary<p_addr, List<RPCMessage>>>(
                    new tysos.Program.MyGenericEqualityComparer<ushort>());
                pending_reqs[dev_no] = nd_dict;
            }

            Dictionary<p_addr, List<RPCMessage>> prot_dict;
            if (nd_dict.TryGetValue(etype, out prot_dict) == false)
            {
                prot_dict = new Dictionary<p_addr, List<RPCMessage>>(
                    new tysos.Program.MyGenericEqualityComparer<p_addr>());
                nd_dict[etype] = prot_dict;
            }

            List<RPCMessage> ret;
            if(prot_dict.TryGetValue(addr, out ret) == false)
            {
                ret = new List<RPCMessage>();
                prot_dict[addr] = ret;
            }

            return ret;
        }

        public RPCResult<HWAddr> ResolveAddress(int dev_no, p_addr addr)
        {
            /* Get the appropriate dictionary */
            var prot_dict = GetProtocolDictionary(dev_no, addr.EtherType);

            /* If the cache contains the value we need, then we can use it
                directly, otherwise we need to send a request for it */
            HWAddr ret;
            if(prot_dict.TryGetValue(addr, out ret) == true)
            {
                System.Diagnostics.Debugger.Log(0, null, "Resolving " +
                    addr.ToString() + " from cache to " + ret.ToString());
                return ret;
            }

            /* We need to send a ARP request out to the network, then respond to
                the reply.  Unfortunately, messages sent to the arp subsystem are
                processed sequentially, therefore the reply will never be processed
                until the current function exits.  To get around this, we tell the
                Invoke mechanism not to return from the synchronous call until
                we have that response */
            var e = CurrentMessage;
            e.EventSetsOnReturn = false;

            /* Send the request */
            switch(addr.EtherType)
            {
                case 0x0800:
                    /* IPv4 */
                    HWAddr sha = net.devs[dev_no].HWAddr;
                    IPv4Address spa = net.devs[dev_no].addresses[0x0800] as IPv4Address;
                    IPv4Address tpa = addr as IPv4Address;
                    byte[] pkt = new byte[100];
                    int packet_offset = 32;
                    int packet_len = 0;

                    pkt[packet_offset + packet_len++] = 0x00;       // HTYPE
                    pkt[packet_offset + packet_len++] = 0x01;
                    pkt[packet_offset + packet_len++] = 0x08;       // PTYPE
                    pkt[packet_offset + packet_len++] = 0x00;
                    pkt[packet_offset + packet_len++] = 0x06;       // HLEN
                    pkt[packet_offset + packet_len++] = 0x04;       // PLEN
                    pkt[packet_offset + packet_len++] = 0x00;       // OPER
                    pkt[packet_offset + packet_len++] = 0x01;

                    // SHA
                    for (int i = 0; i < 6; i++)
                        pkt[packet_offset + packet_len++] = sha.MAC[i];
                    // SPA
                    for (int i = 0; i < 4; i++)
                        pkt[packet_offset + packet_len++] = spa.Octets[i];
                    // THA
                    for (int i = 0; i < 6; i++)
                        pkt[packet_offset + packet_len++] = 0;
                    // TPA
                    for (int i = 0; i < 4; i++)
                        pkt[packet_offset + packet_len++] = tpa.Octets[i];

                    net.TransmitEthernetPacket(pkt, dev_no, packet_offset,
                        packet_len, HWAddr.Multicast, 0x0806);

                    break;
            }

            // Store this request
            GetPendingRequestList(dev_no, addr.EtherType, addr).Add(e);

            // Return null for now until we actually get a result
            return null;
        }

        public RPCResult<bool> AnnounceDevice(int dev_no, ushort etype)
        {
            if(etype == 0)
            {
                // Announce all addresses
                var addrs = new List<ushort>(net.devs[dev_no].addresses.Keys);
                foreach (var addr in addrs)
                    AnnounceDevice(dev_no, addr);
            }
            else
            {
                switch(etype)
                {
                    case 0x0800:
                        {
                            /* IPv4 */
                            HWAddr sha = net.devs[dev_no].HWAddr;
                            IPv4Address spa = net.devs[dev_no].addresses[etype] as IPv4Address;
                            IPv4Address tpa = spa;
                            HWAddr tha = HWAddr.Zero;
                            byte[] pkt = new byte[100];
                            int packet_offset = 32;
                            int packet_len = 0;

                            pkt[packet_offset + packet_len++] = 0x00;       // HTYPE
                            pkt[packet_offset + packet_len++] = 0x01;
                            pkt[packet_offset + packet_len++] = 0x08;       // PTYPE
                            pkt[packet_offset + packet_len++] = 0x00;
                            pkt[packet_offset + packet_len++] = 0x06;       // HLEN
                            pkt[packet_offset + packet_len++] = 0x04;       // PLEN
                            pkt[packet_offset + packet_len++] = 0x00;       // OPER
                            pkt[packet_offset + packet_len++] = 0x01;

                            // SHA
                            for (int i = 0; i < 6; i++)
                                pkt[packet_offset + packet_len++] = sha.MAC[i];
                            // SPA
                            for (int i = 0; i < 4; i++)
                                pkt[packet_offset + packet_len++] = spa.Octets[i];
                            // THA
                            for (int i = 0; i < 6; i++)
                                pkt[packet_offset + packet_len++] = 0;
                            // TPA
                            for (int i = 0; i < 4; i++)
                                pkt[packet_offset + packet_len++] = tpa.Octets[i];

                            net.TransmitEthernetPacket(pkt, dev_no, packet_offset,
                                packet_len, HWAddr.Multicast, 0x0806);
                        }
                        break;
                }
            }

            return true;
        }
    }
}

