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

namespace tysos.x86_64
{
    public class Vga : IDebugOutput
    {
        int x = 0, y = 0;
        ushort base_port;
        byte background;
        tysos.Collections.StaticUShortArray mem;
        static bool enabled;

        public static bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        const int width = 80;
        const int height = 25;
        const byte Default_Background = 0x1f;
        const ulong VGA_BDA_PORT = 0x463;
        const ulong VGA_BDA_TYPE = 0x410;
        const ulong COLOUR_FRAMEBUFFER = 0xb8000;
        const ulong MONO_FRAMEBUFFER = 0xb0000;

        internal Vga(ulong bda_base, ulong fb_vaddr, VirtMem vmem)
        {
            ulong fb_paddr;
            byte c = libsupcs.MemoryOperations.PeekU1((System.UIntPtr)(bda_base + VGA_BDA_TYPE));
            if ((c & 0x30) == 0x30)
                fb_paddr = MONO_FRAMEBUFFER;
            else
                fb_paddr = COLOUR_FRAMEBUFFER;

            base_port = libsupcs.MemoryOperations.PeekU2((System.UIntPtr)(bda_base + VGA_BDA_PORT));
            enabled = true;

            vmem.map_page(fb_vaddr, fb_paddr);

            mem = new tysos.Collections.StaticUShortArray(fb_vaddr, 80 * 25);
            background = Default_Background;
            Clear();
            UpdateCursor();
        }

        public int GetX()
        {
            return x;
        }

        public int GetY()
        {
            return y;
        }

        public void SetX(int newx)
        {
            if (newx <= 0)
                throw new System.ArgumentOutOfRangeException();
            if (newx > width)
                throw new System.ArgumentOutOfRangeException();
            x = newx;
        }

        public void SetY(int newy)
        {
            if (newy <= 0)
                throw new System.ArgumentOutOfRangeException();
            if (newy > height)
                throw new System.ArgumentOutOfRangeException();
            y = newy;
        }

        public int GetWidth()
        {
            return width;
        }

        public int GetHeight()
        {
            return height;
        }

        public void Clear()
        {
            if (enabled)
            {
                mem.Clear((char)(((char)background << 8) | ' '));
                x = 0;
                y = 0;
            }
        }

        public void UpdateCursor()
        {
            if (enabled)
            {
                ushort position = (ushort)((y * width) + x);
                libsupcs.IoOperations.PortOut(base_port, (byte)0x0f);
                libsupcs.IoOperations.PortOut((ushort)(base_port + 1), (byte)(position & 0xff));
                libsupcs.IoOperations.PortOut(base_port, (byte)0x0e);
                libsupcs.IoOperations.PortOut((ushort)(base_port + 1), (byte)((position >> 8) & 0xff));
            }
        }

        public virtual void Write(string s)
        {
            if (enabled)
            {
                for (int i = 0; i < s.Length; i++)
                    Write(s[i]);
                UpdateCursor();
            }
        }

        public void Scroll(int lines)
        {
            if (enabled)
            {
                // Scroll the screen up a certain number of lines
                int dist_to_move = lines * width;
                int total_chars = height * width;
                int chars_to_move = total_chars - dist_to_move;

                for (int i = 0; i < chars_to_move; i++)
                    mem[i] = mem[i + dist_to_move];
                for (int i = chars_to_move; i < total_chars; i++)
                    mem[i] = (char)(((char)background << 8) | ' ');
            }
        }

        public void SetTextAttribute(byte attr)
        {
            background = attr;
        }

        #region IVideo Members


        public void Write(char ch)
        {
            if (enabled)
            {
                if (ch == '\n')
                    _putnl();
                else
                {
                    mem[x + y * width] = (char)((ch & 0xff) | (((char)background) << 8));
                    x++;
                    if (x >= width)
                        _putnl();
                }
            }
        }

        public void Flush()
        {
            if (enabled)
            {
                UpdateCursor();
            }
        }

        #endregion

        void _putnl()
        {
            if (enabled)
            {
                x = 0;
                y++;

                if (y == height)
                {
                    Scroll(1);
                    y = height - 1;
                }
            }
        }
    }
}
