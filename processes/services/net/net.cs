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
using tysos;

namespace net
{
    public partial class net : tysos.ServerObject, INetInternal, tysos.Interfaces.INet
    {
        tysos.Interfaces.IVfs vfs;

        public static Type[] sig_void;
        public static object[] arg_void;
        public static Type[] sig_packet;
        public static Type[] sig_arp_announcedevice;
        public static Type[] sig_arp_resolveaddress;

        internal IArp arp;

        static net()
        {
            sig_void = new Type[] { };
            arg_void = new object[] { };
            sig_packet = new Type[] { typeof(byte[]), typeof(int),
                typeof(int), typeof(int), typeof(p_addr) };
            sig_arp_announcedevice = new Type[] { typeof(int), typeof(ushort) };
            sig_arp_resolveaddress = new Type[] { typeof(int), typeof(p_addr) };
        }

        static void Main()
        {
            net g = new net();
            g.MessageLoop();
        }

        public override bool InitServer()
        {
            tysos.Syscalls.ProcessFunctions.RegisterSpecialProcess(this,
                tysos.Syscalls.ProcessFunctions.SpecialProcessType.Net);

            // Register handlers for devices
            while (tysos.Syscalls.ProcessFunctions.GetVfs() == null) ;
            vfs = tysos.Syscalls.ProcessFunctions.GetVfs();

            vfs.RegisterAddHandler("class", "netdev", tysos.Messages.Message.MESSAGE_NET_REGISTER_DEVICE, true);

            /* Start protocol handlers */
            var _arp = new arp();
            tysos.Process p_arp = tysos.Process.CreateProcess("arp",
                new System.Threading.ThreadStart(_arp.MessageLoop),
                new object[] { _arp });
            p_arp.Start();
            arp = _arp;

            ipv4 _ipv4 = new ipv4();
            tysos.Process p_ipv4 = tysos.Process.CreateProcess("ipv4",
                new System.Threading.ThreadStart(_ipv4.MessageLoop),
                new object[] { _ipv4 });
            p_ipv4.Start();

            return true;
        }

        protected override bool HandleGenericMessage(IPCMessage msg)
        {
            switch(msg.Type)
            {
                case tysos.Messages.Message.MESSAGE_NET_REGISTER_DEVICE:
                    RegisterDevice(msg.Message as string);
                    return true;
                default:
                    return false;
            }
        }
    }
}
