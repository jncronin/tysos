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

namespace PS2_keyboard
{
    class PS2_keyboard
    {
        static tysos.InterruptMap _imap;
        static tysos.Process gui;
        
        /* We define a generic tysos keypress message of type ushort
         * The least significant byte is the key (see below)
         * The most significant byte is the modifiers for the key
         * 
         * Modifiers:
         * 
         * bit 0 - left ctrl pressed
         * bit 1 - alt pressed
         * bit 2 - alt gr pressed
         * bit 3 - right ctrl pressed
         * 
         * keys: (k followed by another character refers to keys on the numeric keypad)
         * 
         *       _0   _1   _2   _3   _4   _5   _6   _7   _8   _9   _A   _B   _C   _D   _E   _F
         *  
         *  0_   err  esc  1    2    3    4    5    6    7    8    9    0    -    =    bksp tab
         *  1_   q    w    e    r    t    y    u    i    o    p    [    ]    ent       a    s
         *  2_   d    f    g    h    j    k    l    ;    '    `         \    z    x    c    v
         *  3_   b    n    m    ,    .    /         k*        spc       F1   F2   F3   F4   F5
         *  4_   F6   F7   F8   F9   F10            k7   k8   k9   k-   k4   k5   k6   k+   k1
         *  5_   k2   k3   k0   k.                  F11  F12
         *  6_
         *  7_
         *  8_   Q    W    E    R    T    Y    U    I    O    P    {    }
         *  9_   A    S    D    F    G    H    J    K    L    :    "    ¬
         *  A_   Z    X    C    V    B    N    M    <    >    ?
         *  B_   !    @    #    $    %    ^    &    *    (    )    _    +
         *  C_   |
         *  D_
         *  E_
         *  F_
         *  
         * To generate a character from a keyboard key press, first the key press is translated from
         * the keyboard hardware scancode to a tysos scancode, then the tysos scancode is used to look
         * up the information on a keymap table
         */

        public static void Main(string[] args)
        {
            /* Wait for the gui to start up */
            tysos.Syscalls.DebugFunctions.DebugWrite("PS2K: awaiting gui startup\n");
            tysos.ProcessEvent e = new tysos.ProcessEvent();
            e.ProcessEventType = tysos.ProcessEvent.ProcessEventTypeKind.ReadyForMessages;
            e.ProcessName = "gui";
            tysos.Syscalls.SchedulerFunctions.Block(e);

            /* Register ourselves with the gui */
            tysos.Syscalls.DebugFunctions.DebugWrite("PS2K: registering with gui\n");
            gui = e.Process;
            if (gui == null)
                throw new Exception("Unable to communicate with gui process");
            tysos.Syscalls.IPCFunctions.SendMessage(gui, new tysos.IPCMessage { Type = Gui.GuiMessageTypes.REGISTER_INPUT });

            /* Register our callback function */
            tysos.Syscalls.DebugFunctions.DebugWrite("PS2K: registering irq handler\n");
            _imap = tysos.Syscalls.InterruptFunctions.GetInterruptMap();
            //_imap.RegisterIRQHandler("Keyboard", new tysos.Interrupts.ISR(KeyboardHandler));

            /* Listen for shutdown messages */
            tysos.Syscalls.DebugFunctions.DebugWrite("PS2K: entering message loop\n");
            tysos.Syscalls.IPCFunctions.InitIPC();
            bool cont = true;

            while(cont)
            {
                tysos.IPCMessage msg = null;
                do
                {
                    msg = tysos.Syscalls.IPCFunctions.ReadMessage();

                    if(msg != null)
                    {
                        switch(msg.Type)
                        {
                            case tysos.IPCMessage.TYPE_CLOSE:
                                cont = false;
                                break;
                        }
                    }

                    if(cont == false)
                        break;

                    tysos.Syscalls.SchedulerFunctions.Block();
                } while (msg != null);
            }
        }

        static bool is_shifted = false;
        static bool lctrl_down = false;
        static bool rctrl_down = false;
        static bool lalt_down = false;
        static bool ralt_down = false;

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        static void KeyboardHandler()
        {
            byte scan_code = libsupcs.IoOperations.PortInb(0x60);
            //tysos.Syscalls.DebugFunctions.DebugWrite("PS2_keyboard: scan code: 0x" + scan_code.ToString("X") + "\n");

            ushort ty_code = 0;
            switch (scan_code)
            {
                case 0x2a:
                case 0x36:
                    is_shifted = true;
                    break;
                case 0xaa:
                case 0xb6:
                    is_shifted = false;
                    break;
            }
            if ((scan_code >= 0x02) && (scan_code <= 0x0d))
            {
                ty_code = (ushort)scan_code;
                if (is_shifted)
                    ty_code += (ushort)0xae;
            }
            else if ((scan_code >= 0x0e) && (scan_code <= 0x0f))
                ty_code = (ushort)scan_code;
            else if ((scan_code >= 0x10) && (scan_code <= 0x1b))
            {
                ty_code = (ushort)scan_code;
                if(is_shifted)
                    ty_code += (ushort)0x70;
            }
            else if ((scan_code >= 0x1e) && (scan_code <= 0x29))
            {
                ty_code = (ushort)scan_code;
                if(is_shifted)
                    ty_code += (ushort)0x72;
            }
            else if (scan_code == 0x2b)
            {
                ty_code = (ushort)scan_code;
                if (is_shifted)
                    ty_code += (ushort)0x95;
            }
            else if ((scan_code >= 0x2c) && (scan_code <= 0x35))
            {
                ty_code = (ushort)scan_code;
                if (is_shifted)
                    ty_code += (ushort)0x74;
            }
            else if ((scan_code == 0x1c) ||
                (scan_code == 0x37) ||
                (scan_code == 0x39) ||
                ((scan_code >= 0x3b) && (scan_code <= 0x44)) ||
                ((scan_code >= 0x47) && (scan_code <= 0x53)) ||
                (scan_code == 0x57) ||
                (scan_code == 0x58))
            {
                ty_code = (ushort)scan_code;
            }

            if (ty_code != 0x0)
            {
                if (lctrl_down)
                    ty_code |= 0x100;
                if (lalt_down)
                    ty_code |= 0x200;
                if (ralt_down)
                    ty_code |= 0x400;
                if (rctrl_down)
                    ty_code |= 0x800;

                handle_tysos_scancode(ty_code);
            }

            _imap.SendEOI();
        }

        static void handle_tysos_scancode(ushort code)
        {
            tysos.Syscalls.IPCFunctions.SendMessage(gui, new tysos.IPCMessage { Type = Gui.GuiMessageTypes.KEYPRESS_MESSAGE, Message = new Gui.GuiMessageTypes.KeyPressMessage { tysos_scancode = code } });
        }
    }
}
