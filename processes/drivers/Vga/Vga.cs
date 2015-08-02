/* Copyright (C) 2011 by John Cronin
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

namespace Vga
{
    class Vga
    {
        static tysos.Process gui;
        static Gui.Buffer back_buffer;
        static ulong va_vidmem;

        static void Main(string[] args)
        {
            /* Wait for the gui to start up */
            tysos.Syscalls.DebugFunctions.DebugWrite("Vga: awaiting gui startup\n");
            tysos.ProcessEvent e = new tysos.ProcessEvent();
            e.ProcessEventType = tysos.ProcessEvent.ProcessEventTypeKind.ReadyForMessages;
            e.ProcessName = "gui";
            tysos.Syscalls.SchedulerFunctions.Block(e);

            /* Create our back buffer */
            va_vidmem = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(0xb8000, 0x1000, tysos.Syscalls.MemoryFunctions.CacheType.Uncacheable, true);
            back_buffer = new Gui.Buffer(80, 25, Gui.Buffer.PixelFormatType.PF_16_8CHAR_8IDX);

            /* Disable the kernel Vga driver */
            tysos.Syscalls.DebugFunctions.DebugWrite("Vga: disabling kernel vga driver\n");
            tysos.x86_64.Vga.Enabled = false;

            /* Move the hardware cursor beyond the end of the screen */
            ushort position = 25 * 80;
            libsupcs.IoOperations.PortOut(0x3d4, (byte)0x0f);
            libsupcs.IoOperations.PortOut(0x3d5, (byte)(position & 0xff));
            libsupcs.IoOperations.PortOut(0x3d4, (byte)0x0e);
            libsupcs.IoOperations.PortOut(0x3d5, (byte)((position >> 8) & 0xff));

            /* Register ourselves with the gui */
            tysos.Syscalls.DebugFunctions.DebugWrite("Vga: registering with gui\n");
            gui = e.Process;
            if (gui == null)
                throw new Exception("Unable to communicate with gui process");
            //tysos.Syscalls.IPCFunctions.SendMessage(gui, new tysos.IPCMessage { Type = Gui.GuiMessageTypes.REGISTER_OUTPUT, Message = new Gui.GuiMessageTypes.RegisterOutputMessage { buffer = back_buffer } });

            /* Listen for shutdown messages */
            tysos.Syscalls.DebugFunctions.DebugWrite("Vga: entering message loop\n");
            tysos.Syscalls.IPCFunctions.InitIPC();
            bool cont = true;

            while (cont)
            {
                tysos.IPCMessage msg = null;
                do
                {
                    msg = tysos.Syscalls.IPCFunctions.ReadMessage();

                    if (msg != null)
                    {
                        switch (msg.Type)
                        {
                            case tysos.IPCMessage.TYPE_CLOSE:
                                cont = false;
                                break;

                            /*case Gui.GuiMessageTypes.UPDATE_OUTPUT:
                                update_output();
                                break;*/
                        }
                    }

                    if (cont == false)
                        break;

                    tysos.Syscalls.SchedulerFunctions.Block();
                } while (msg != null);
            }

        }

        private static void update_output()
        {
            unsafe
            {
                byte *buf_dest = (byte *)va_vidmem;

                for (int i = 0; i < (80 * 25 * 2); i++)
                    buf_dest[i] = back_buffer.Data[i];
            }
        }
    }
}
