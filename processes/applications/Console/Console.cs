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

namespace Console
{
    class Console : tysos.IInputStream, tysos.IOutputStream, tysos.lib.ConsoleDriver.IConsole
    {
        tysos.Process gui;
        Gui.Window window;
        Gui.Buffer backbuffer;
        tysos.Process current_process;
        tysos.Event Initialized = new tysos.Event();

        List<byte> read_buffer = new List<byte>();
        List<tysos.IPCMessage> pending_reads = new List<tysos.IPCMessage>();

        const int BUFFER_HEIGHT = 300;
        int cur_window_y = 0;

        int cur_x = 0;
        int cur_y = 0;

        static void Main(string[] args)
        {
            new Console().Init();
        }

        long tysos.IInputStream.Position { get { return 0; } }
        long tysos.IOutputStream.Position { get { return 0; } }
        public long Length { get { return 0; } }
        public void Seek(long position, tysos.SeekPosition whence) { throw new NotImplementedException(); }

        private void Init()
        {
            tysos.Syscalls.DebugFunctions.DebugWrite("Console: acquiring Gui process\n");
            gui = Gui.Gui.GetGUIProcess();

            tysos.Syscalls.DebugFunctions.DebugWrite("Console: creating a window\n");
            window = Gui.Window.CreateWindow(-1, -1, true);

            tysos.Syscalls.DebugFunctions.DebugWrite("Console: creating a backbuffer\n");
            backbuffer = new Gui.Buffer(window.Graphics.Width, BUFFER_HEIGHT, window.Graphics.PixelFormat);

            tysos.Syscalls.DebugFunctions.DebugWrite("Console: starting shell\n");
            tysos.Process shell_p = tysos.Syscalls.ProcessFunctions.ExecModule("shell", false);
            shell_p.stderr = this;
            shell_p.stdout = this;
            shell_p.stdin = this;
            shell_p.startup_thread.do_profile = true;
            tysos.Syscalls.ProcessFunctions.StartProcess(shell_p);

            tysos.Syscalls.DebugFunctions.DebugWrite("Console: entering message loop\n");
            tysos.Syscalls.IPCFunctions.InitIPC();
            current_process = tysos.Syscalls.ProcessFunctions.GetCurrentProcess();
            Initialized.Set();
            bool cont = true;
            while (cont)
            {
                tysos.IPCMessage msg = null;
                do
                {
                    msg = tysos.Syscalls.IPCFunctions.ReadMessage();

                    if (msg != null)
                        handle_message(msg);
                } while (msg != null);

                tysos.Syscalls.SchedulerFunctions.Block();
            }
        }

        private void handle_message(tysos.IPCMessage msg)
        {
            switch (msg.Type)
            {
                case vfsMessageTypes.READ:
                    {
                        vfsMessageTypes.ReadWriteMessage rwm = msg.Message as vfsMessageTypes.ReadWriteMessage;
                        if (rwm != null)
                        {
                            if (read_buffer.Count > 0)
                            {
                                int count_read = 0;
                                while ((read_buffer.Count > 0) && (count_read < rwm.count))
                                {
                                    rwm.buf[rwm.buf_offset + count_read] = read_buffer[0];
                                    read_buffer.RemoveAt(0);
                                    count_read++;
                                }
                                rwm.count_read = count_read;
                                rwm.completed.Set();
                            }
                            else
                                pending_reads.Add(msg);
                        }
                    }
                    break;

                case vfsMessageTypes.WRITE:
                    {
                        vfsMessageTypes.ReadWriteMessage rwm = msg.Message as vfsMessageTypes.ReadWriteMessage;
                        if (rwm != null)
                            _Write(rwm.buf, rwm.buf_offset, rwm.count);
                    }
                    break;

                case vfsMessageTypes.PEEK:
                    {
                        vfsMessageTypes.ReadWriteMessage rwm = msg.Message as vfsMessageTypes.ReadWriteMessage;
                        if (rwm != null)
                        {
                            //lock (rwm.completed)
                            {
                                if (read_buffer.Count >= rwm.count)
                                {
                                    rwm.count_read = rwm.count;
                                    rwm.completed.Set();
                                }
                                else
                                    pending_reads.Add(msg);
                            }
                        }
                    }
                    break;

                case GuiMessageTypes.KEYPRESS_MESSAGE:
                    {
                        GuiMessageTypes.KeyPressMessage kpm = msg.Message as GuiMessageTypes.KeyPressMessage;
                        if (kpm != null)
                        {
                            read_buffer.Add((byte)kpm.key);
                            process_pending_reads();
                        }
                    }
                    break;
            }
        }

        private void process_pending_reads()
        {
            while (pending_reads.Count > 0)
            {
                //lock (pending_awaits[0].completed)
                {
                    vfsMessageTypes.ReadWriteMessage rwm = pending_reads[0].Message as vfsMessageTypes.ReadWriteMessage;

                    if (rwm == null)
                    {
                        pending_reads.RemoveAt(0);
                        continue;
                    }

                    if (rwm.completed.IsSet)
                    {
                        pending_reads.RemoveAt(0);
                        continue;
                    }

                    if(read_buffer.Count > 0)
                    {
                        int count_read = 0;
                        if (pending_reads[0].Type == vfsMessageTypes.READ)
                        {
                            while ((read_buffer.Count > 0) && (count_read < rwm.count))
                            {
                                rwm.buf[rwm.buf_offset + count_read] = read_buffer[0];
                                read_buffer.RemoveAt(0);
                                count_read++;
                            }
                        }
                        else
                        {
                            count_read = read_buffer.Count;
                            if (rwm.count < count_read)
                                count_read = rwm.count;
                        }

                        rwm.count_read = count_read;
                        rwm.completed.Set();
                        pending_reads.RemoveAt(0);
                        continue;
                    }
                    else
                        break;
                }
            }
        }

        public void Write(byte[] src, int src_offset, int count)
        {
            vfsMessageTypes.ReadWriteMessage rwm = new vfsMessageTypes.ReadWriteMessage();
            rwm.buf = new byte[count];
            for (int i = 0; i < count; i++)
                rwm.buf[i] = src[i + src_offset];
            rwm.buf_offset = 0;
            rwm.count = count;

            tysos.Syscalls.IPCFunctions.SendMessage(GetConsoleProcess(), new tysos.IPCMessage { Type = vfsMessageTypes.WRITE, Message = rwm });
        }

        public int Read(byte[] dest, int dest_offset, int count)
        {
            vfsMessageTypes.ReadWriteMessage rwm = new vfsMessageTypes.ReadWriteMessage();
            rwm.buf = dest;
            rwm.buf_offset = dest_offset;
            rwm.count = count;
            
            tysos.Syscalls.IPCFunctions.SendMessage(GetConsoleProcess(), new tysos.IPCMessage { Type = vfsMessageTypes.READ, Message = rwm });
            tysos.Syscalls.SchedulerFunctions.Block(rwm.completed);
            tysos.Syscalls.DebugFunctions.DebugWrite("Console: Read: returning " + rwm.count_read.ToString() + "\n");
            return rwm.count_read;
        }

        void _Write(byte[] src, int src_offset, int count)
        {
            /* Do the actual write */

            tysos.Syscalls.DebugFunctions.DebugWrite("Console:_Write: message:");
            for (int i = src_offset; i < (src_offset + count); i++)
                tysos.Syscalls.DebugFunctions.DebugWrite(" " + src[i].ToString());
            tysos.Syscalls.DebugFunctions.DebugWrite("\n");                

            for (int i = src_offset; i < (src_offset + count); i++)
            {
                char ch = (char)src[i];

                if((src[i] == 0xef) && ((i + 2) < (src_offset + count)) && ((src[i + 1] == 0xbb) || (src[i + 1] == 0xbf)) && (src[i + 2] == 0xbf))
                {
                    // ignore 0xffff and 0xfeff (byte order mark)
                    i += 2;
                    continue;
                }
                else if (ch == (char)10)
                {
                    nl();
                }
                else if ((src[i] == 0x1b) && ((i + 3) < (src_offset + count)) && (src[i + 1] == 0x5b) && (src[i + 2] == 0x36) && (src[i + 3] == 0x6e))
                {
                    // <ESC>[6n - query cursor position

                    // return <ESC>[{ROW};{COL}R

                    read_buffer.Add((byte)0x1b);
                    read_buffer.Add((byte)'[');
                    read_buffer.Add((byte)(((cur_y + 1 - cur_window_y) / 10) + '0'));
                    read_buffer.Add((byte)(((cur_y + 1 - cur_window_y) % 10) + '0'));
                    read_buffer.Add((byte)';');
                    read_buffer.Add((byte)(((cur_x + 1) / 10) + '0'));
                    read_buffer.Add((byte)(((cur_x + 1) % 10) + '0'));
                    read_buffer.Add((byte)'R');

                    i += 3;
                    continue;
                }
                else if (src[i] == 0x1b)
                {
                    // handle generic escape sequence
                    int j = i + 1;
                    if (j < (src_offset + count))
                    {
                        if (src[j] == (byte)'[')
                        {
                            // Read until we reach a character
                            List<char> msg = new List<char>();

                            j++;
                            while ((j < (src_offset + count)) && !is_char((char)src[j]))
                            {
                                msg.Add((char)src[j]);
                                j++;
                            }

                            if (j < (src_offset + count))
                            {
                                char id = (char)src[j];

                                switch (id)
                                {
                                    case 'H':
                                        {
                                            // Set character to a certain location
                                            int x = 0;
                                            int y = 0;

                                            if (msg.Count > 0)
                                            {
                                                x = 0;
                                                y = 0;

                                                bool in_y = true;
                                                foreach (char ch2 in msg)
                                                {
                                                    if (ch2 == ';')
                                                        in_y = false;
                                                    else
                                                    {
                                                        if (in_y)
                                                            y = y * 10 + (int)(ch2 - '0');
                                                        else
                                                            x = x * 10 + (int)(ch2 - '0');
                                                    }
                                                }

                                                tysos.Syscalls.DebugFunctions.DebugWrite("Console: received CursorHome message row " + y.ToString() + " col " + x.ToString() + "\n");

                                                x--;
                                                y--;
                                            }

                                            cur_x = x;
                                            cur_y = y + cur_window_y;
                                        }
                                        break;

                                    default:
                                        tysos.Syscalls.DebugFunctions.DebugWrite("Console: unknown escape sequence ending " + id.ToString() + "\n");
                                        break;

                                }

                                i = j;
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    write(ch);
                }
            }

            /* Copy the back buffer to our window */
            Gui.Buffer.Blit(window.Graphics, backbuffer, 0, cur_window_y, 0, 0, window.Graphics.Width, window.Graphics.Height);

            /* Send an UPDATE_OUTPUT message to the gui */
            tysos.Syscalls.IPCFunctions.SendMessage(gui, new tysos.IPCMessage { Type = GuiMessageTypes.UPDATE_OUTPUT });
        }

        private bool is_char(char p)
        {
            return char.IsLetter(p);
        }

       private void write(char ch)
        {
            Gui.Drawing.Text.DrawText(backbuffer, null, null, cur_x, cur_y, ch);
            Gui.Drawing.Rectangle extents = Gui.Drawing.Text.GetTextExtents(backbuffer, null, ch);
            cur_x += extents.Width;
            if (cur_x >= backbuffer.Width)
                nl();
        }

        private void nl()
        {
            cur_x = 0;
            cur_y += Gui.Drawing.Text.GetTextExtents(backbuffer, null, 'A').Height;

            if (cur_y >= (cur_window_y + window.Graphics.Height))
                cur_window_y = cur_y - window.Graphics.Height + 1;
        }

        public tysos.Process GetConsoleProcess()
        {
            if (current_process != null)
                return current_process;

            /* Wait for the console to start up */
            tysos.Syscalls.SchedulerFunctions.Block(Initialized);
            return current_process;
        }

        public int GetWidth()
        {
            return 80;
        }

        public int GetHeight()
        {
            return 25;
        }

        public int DataAvailable(int timeout)
        {
            vfsMessageTypes.ReadWriteMessage rwm = new vfsMessageTypes.ReadWriteMessage();
            rwm.buf = new byte[1];
            rwm.buf_offset = 0;
            rwm.count = 1;

            tysos.Syscalls.DebugFunctions.DebugWrite("Console: KeyAvailable: called (timeout = " + timeout.ToString() + ")\n");
            
            tysos.Syscalls.IPCFunctions.SendMessage(GetConsoleProcess(), new tysos.IPCMessage { Type = vfsMessageTypes.PEEK, Message = rwm });

            /* Block on either receiving a character or the timeout, whichever comes first */
            tysos.WaitAnyEvent wae = new tysos.WaitAnyEvent();
            wae.Children.Add(rwm.completed);
            wae.Children.Add(new tysos.TimerEvent(timeout));
            tysos.Syscalls.SchedulerFunctions.Block(wae);

            //lock (rwm.completed)
            {
                if (rwm.completed.IsSet)
                {
                    tysos.Syscalls.DebugFunctions.DebugWrite("Console: KeyAvailable: returning true\n");
                    return (int)rwm.count;
                }
                rwm.completed.Set();
                tysos.Syscalls.DebugFunctions.DebugWrite("Console: KeyAvailable: returning false\n");
                return 0;
            }
        }
    }
}
