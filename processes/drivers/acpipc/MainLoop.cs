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
using tysos.Messages;

namespace acpipc
{
    partial class Program
    {
        static void Main(string[] args)
        {
            /* Add any specific startup code here */
            tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: driver starting\n");

            /* The main message loop */
            while (true)
            {
                tysos.IPCMessage msg = null;
                do
                {
                    msg = tysos.Syscalls.IPCFunctions.ReadMessage();

                    if (msg != null)
                        HandleMessage(msg);
                } while (msg != null);

                tysos.Syscalls.SchedulerFunctions.Block();
            }
        }

        private static void HandleMessage(tysos.IPCMessage msg)
        {
            switch (msg.Type)
            {
                #region Device Messages
                case deviceMessageTypes.INIT_DEVICE:
                    {
                        deviceMessageTypes.InitDeviceMessage idm = msg.Message as deviceMessageTypes.InitDeviceMessage;
                        if (idm != null)
                        {
                            // Handle the message
                            _InitDevice(idm.Resources, idm.Node, ref idm.Device);
                            idm.completed.Set();
                        }
                    }
                    break;
                #endregion
            }
        }
    }
}
