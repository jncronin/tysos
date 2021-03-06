﻿/* Copyright (C) 2015 by John Cronin
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
using tysos;

namespace net
{
    partial class net
    {
        internal Dictionary<int, netdev> devs = new Dictionary<int, netdev>(
            new tysos.Program.MyGenericEqualityComparer<int>());
        internal Dictionary<p_addr, int> addrs = new Dictionary<p_addr, int>(
            new tysos.Program.MyGenericEqualityComparer<p_addr>());

        int next_dev_no = 0;

        internal class netdev
        {
            public INetworkDevice s;
            public int dev_no;
            public Dictionary<ushort, p_addr> addresses =
                new Dictionary<ushort, p_addr>(
                    new tysos.Program.MyGenericEqualityComparer<ushort>());

            public bool dev_appends_crc_on_tx;
            public bool dev_pads_on_tx;

            public p_addr a;

            HWAddr hwaddr = null;
            public HWAddr HWAddr
            {
                get
                {
                    if (hwaddr == null)
                        hwaddr = s.GetHardwareAddress().Sync();
                    return hwaddr;
                }
            }

            public void Start()
            {
                s.Start().Sync();
            }

            public void Stop()
            {
                s.Stop().Sync();
            }
        }

        public void RegisterDevice(string path)
        {
            var nd = new netdev();
            lock (devs)
            {
                nd.dev_no = next_dev_no++;
            }
            System.Diagnostics.Debugger.Log(0, null, "Registering device " +
                nd.dev_no.ToString());
            nd.s = GetServer(path);

            devs[nd.dev_no] = nd;

            // Automatically start the device
            System.Diagnostics.Debugger.Log(0, null, "Sending RegisterDevNo message");
            nd.s.RegisterDevNo(nd.dev_no);
            System.Diagnostics.Debugger.Log(0, null, "Sending Start message");
            //nd.s.InvokeAsync("Start", new object[] { }, new Type[] { });
            nd.Start();
            System.Diagnostics.Debugger.Log(0, null, "Device " + nd.dev_no.ToString() +
                " registered");

            // Configure a static IP for now
            nd.a = new IPv4Address { addr = 0xc0a83866 };
            nd.addresses[0x0800] = nd.a;
            addrs[nd.a] = nd.dev_no;

            // In future, query device for capabilities (FCS etc).  For now,
            //  assume it has these abilitied
            nd.dev_appends_crc_on_tx = true;
            nd.dev_pads_on_tx = true;

            // Send ARP announce
            arp.AnnounceDevice(nd.dev_no, 0);

            // Send a request for 192.168.56.100
            var test_spa = new IPv4Address { addr = 0xc0a83864 };
            arp.ResolveAddress(nd.dev_no, test_spa).SetCallback(
                    delegate (object spa, HWAddr hw_test)
                    {
                        if (hw_test == null)
                            System.Diagnostics.Debugger.Log(0, null, "Test ARP request for " +
                                spa.ToString() + " returned null");
                        else
                            System.Diagnostics.Debugger.Log(0, null, "Test ARP request for " +
                                spa.ToString() + " returned " + hw_test.ToString());
                        return null;
                    }
                    , test_spa);
        }

        private INetworkDevice GetServer(string path)
        {
            // Open the file and get its 'server' property
            System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                ") called");

            tysos.lib.File f = vfs.OpenFile("path", System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite,
                System.IO.FileOptions.None).Sync();

            if(f == null || f.Error != tysos.lib.MonoIOError.ERROR_SUCCESS)
            {
                System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                    ") failed - could not open file");
                return null;
            }

            var p = f.GetPropertyByName("server");

            vfs.CloseFile(f);

            if(p == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                    ") failed - no 'server' property found");
                return null;
            }

            var s = p.Value as INetworkDevice;
            if(s == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                    ") failed - 'server' property is not of type INetworkDevice");
                return null;
            }

            System.Diagnostics.Debugger.Log(0, null, "GetServer(" + path +
                ") succeeded");
            return s;
        }
    }
}
