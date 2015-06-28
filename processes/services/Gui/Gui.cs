/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace Gui
{
    public class Gui
    {
        static List<Output> outputs = new List<Output>();
        static List<Output> possible_outputs = new List<Output>();
        static List<Window> windows = new List<Window>();

        static Window focus_window = null;

        static internal tysos.Event Initialized = new tysos.Event();
        static internal Buffer.PixelFormatType desktop_pformat;
        static internal int desktop_width;
        static internal int desktop_height;

        static void Main(string[] args)
        {
            tysos.Syscalls.DebugFunctions.DebugWrite("gui: entering message loop\n");
            tysos.Syscalls.IPCFunctions.InitIPC();
            bool cont = true;
            while (cont)
            {
                tysos.IPCMessage msg = null;
                do
                {
                    msg = tysos.Syscalls.IPCFunctions.ReadMessage();

                    if(msg != null)
                        handle_message(msg);
                } while (msg != null);

                tysos.Syscalls.SchedulerFunctions.Block();
            }
        }

        private static void update_output()
        {
            foreach (Window w in windows)
            {
                if (w.Visible)
                {
                    foreach (Output o in outputs)
                    {
                        Buffer.Blit(o.back_buffer, w.buf, 0, 0, w.x, w.y, w.buf.Width, w.buf.Height);
                    }
                }
            }

            foreach (Output o in outputs)
                tysos.Syscalls.IPCFunctions.SendMessage(o.process, new tysos.IPCMessage { Type = GuiMessageTypes.UPDATE_OUTPUT });
        }

        private static void handle_message(tysos.IPCMessage msg)
        {
            switch (msg.Type)
            {
                case GuiMessageTypes.REGISTER_INPUT:
                    tysos.Syscalls.DebugFunctions.DebugWrite("Received RegisterInput message\n");
                    break;

                case GuiMessageTypes.REGISTER_OUTPUT:
                    tysos.Syscalls.DebugFunctions.DebugWrite("Received RegisterOutput message\n");
                    GuiMessageTypes.RegisterOutputMessage rom = msg.Message as GuiMessageTypes.RegisterOutputMessage;
                    if (rom != null)
                    {
                        Output o = new Output();
                        o.back_buffer = rom.buffer as Buffer;
                        o.process = msg.Source.owning_process;
                        o.x_origin = 0;
                        o.y_origin = 0;
                        possible_outputs.Add(o);

                        if (Initialized.IsSet)
                        {
                            /* Check the new output is compatible with the current desktop format, else don't automatically
                             * extend the desktop to it */

                            if (o.back_buffer.PixelFormat == desktop_pformat)
                            {
                                /* Extend horizontally for now */
                                o.x_origin = desktop_width;
                                o.y_origin = 0;
                                outputs.Add(o);

                                if (o.back_buffer.Height > desktop_height)
                                    desktop_height = o.back_buffer.Height;
                                desktop_width += o.back_buffer.Width;
                            }
                        }
                        else
                        {
                            /* Initialize the desktop with the current output */
                            desktop_height = o.back_buffer.Height;
                            desktop_width = o.back_buffer.Width;
                            desktop_pformat = o.back_buffer.PixelFormat;
                            outputs.Add(o);

                            Initialized.Set();
                        }
                    }
                    break;

                case GuiMessageTypes.KEYPRESS_MESSAGE:
                    GuiMessageTypes.KeyPressMessage kpm = msg.Message as GuiMessageTypes.KeyPressMessage;
                    if (kpm != null)
                    {
                        kpm.key = KeyMap.DecodeKey(kpm.tysos_scancode);
                        if ((kpm.key != (char)0) && (focus_window != null))
                            tysos.Syscalls.IPCFunctions.SendMessage(focus_window.owning_process, new tysos.IPCMessage { Type = GuiMessageTypes.KEYPRESS_MESSAGE, Message = kpm });
                    }
                    break;

                case GuiMessageTypes.CREATE_WINDOW:
                    if (msg.Message is Window)
                    {
                        Window w = msg.Message as Window;
                        w.owning_process = msg.Source.owning_process;
                        windows.Add(w);

                        if (focus_window == null)
                            focus_window = w;
                    }
                    break;

                case GuiMessageTypes.UPDATE_OUTPUT:
                    update_output();
                    break;
            }
        }

        public static tysos.Process GetGUIProcess()
        {
            /* Wait for the gui to start up */
            tysos.ProcessEvent e = new tysos.ProcessEvent();
            e.ProcessEventType = tysos.ProcessEvent.ProcessEventTypeKind.ReadyForMessages;
            e.ProcessName = "gui";
            tysos.Syscalls.SchedulerFunctions.Block(e);
            return e.Process;
        }
    }

    class Output
    {
        public tysos.Process process;
        public Buffer back_buffer;

        public int x_origin;
        public int y_origin;
    }
}
