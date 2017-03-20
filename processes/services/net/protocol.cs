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
using System.Text;

namespace net
{
    /* Define the address given to a network interface */
    abstract public class p_netdev_addr : System.IEquatable<p_netdev_addr>
    {
        public abstract bool Equals(p_netdev_addr other);
    }

    /* Define a particular address */
    abstract public class p_addr : System.IEquatable<p_addr>
    {
        public abstract bool Equals(p_addr other);
        public abstract ushort EtherType { get; }
    }

    public class IPv4Address : p_addr
    {
        public uint addr;

        public override bool Equals(p_addr other)
        {
            IPv4Address o = other as IPv4Address;
            if (o == null)
                return false;
            return addr == o.addr;
        }

        public override int GetHashCode()
        {
            return addr.GetHashCode();
        }

        public override ushort EtherType
        { get { return 0x0800; } }

        public static implicit operator IPv4Address(uint a)
        { return new IPv4Address { addr = a }; }

        public static implicit operator uint(IPv4Address a)
        { return a.addr; }

        public byte[] Octets { get
            {
                return new byte[] { (byte)((addr >> 24) & 0xff),
                    (byte)((addr >> 16) & 0xff),
                    (byte)((addr >> 8) & 0xff),
                    (byte)(addr & 0xff)
                };
            } }

        public override string ToString()
        {
            byte[] o = Octets;
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < 4; i++)
            {
                if (i != 0)
                    sb.Append(".");
                sb.Append(o[i].ToString());
            }
            return sb.ToString();
        }
    }

    public class IPv4DevAddress : p_netdev_addr
    {
        public IPv4Address addr;
        public IPv4Address mask;

        public override bool Equals(p_netdev_addr other)
        {
            IPv4DevAddress o = other as IPv4DevAddress;
            if (o == null)
                return false;
            return (addr.addr & mask.addr) == (o.addr.addr & o.mask.addr);
        }

        public override int GetHashCode()
        {
            return (addr.addr & mask.addr).GetHashCode();
        }
    }

    public class HWAddr : p_addr
    {
        byte[] mac = new byte[6];

        public static HWAddr Multicast
        {
            get
            {
                HWAddr ret = new HWAddr();
                ret.Addr = 0xffffffffffffUL;
                return ret;
            }
        }

        public static HWAddr Zero
        {
            get
            {
                HWAddr ret = new HWAddr();
                ret.Addr = 0UL;
                return ret;
            }
        }

        public override ushort EtherType
        { get { return 0x0; } }

        public byte[] MAC { get { return mac; } }
        public ulong Addr
        {
            get
            {
                return mac[0] | ((ulong)mac[1] << 8) |
                    ((ulong)mac[2] << 16) | ((ulong)mac[3] << 24) |
                    ((ulong)mac[4] << 32) | ((ulong)mac[5] << 40);
            }
            set
            {
                mac[0] = (byte)(value & 0xff);
                mac[1] = (byte)((value >> 8) & 0xff);
                mac[2] = (byte)((value >> 16) & 0xff);
                mac[3] = (byte)((value >> 24) & 0xff);
                mac[4] = (byte)((value >> 32) & 0xff);
                mac[5] = (byte)((value >> 40) & 0xff);
            }
        }

        public override bool Equals(p_addr other)
        {
            HWAddr o = other as HWAddr;
            if (o == null)
                return false;
            for (int i = 0; i < 6; i++)
            {
                if (mac[i] != o.mac[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return Addr.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < 6; i++)
            {
                if (i != 0)
                    sb.Append(":");
                sb.Append(mac[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
