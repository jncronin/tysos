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
using tysos.Messages;

namespace Gui
{
    public class Window
    {
        internal Buffer buf;
        internal tysos.Process owning_process;

        internal int x;
        internal int y;

        public bool Visible;
        public Buffer Graphics { get { return buf; } }

        public static Window CreateWindow(int width, int height, bool show)
        {
            /* Wait for the gui to be initialized */
            tysos.Syscalls.SchedulerFunctions.Block(Gui.Initialized);

            /* Create a window */
            Window w = new Window();

            if (height == -1)
                height = Gui.desktop_height;
            if (width == -1)
                width = Gui.desktop_width;

            w.buf = new Buffer(width, height, Gui.desktop_pformat);
            w.Visible = show;

            /* Create the window in the centre of the screen */
            w.x = (Gui.desktop_width - width) / 2;
            w.y = (Gui.desktop_height - height) / 2;

            /* Register the window */
            tysos.Syscalls.IPCFunctions.SendMessage(Gui.GetGUIProcess(), new tysos.IPCMessage { Type = GuiMessageTypes.CREATE_WINDOW, Message = w });

            return w;
        }
    }
}
