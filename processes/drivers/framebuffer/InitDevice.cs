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
using tysos;
using tysos.lib;
using tysos.Resources;

namespace framebuffer
{
    partial class framebuffer : tysos.lib.VirtualFileServer
    {
        tysos.VirtualMemoryResource64 vmem;
        tysos.PhysicalMemoryResource64 pmem;
        int w, h, str, bpp, pformat;
        byte[] buf;

        internal framebuffer(tysos.lib.File.Property[] Properties)
        {
            root.AddRange(Properties);
        }

        public override bool InitServer()
        {
            System.Diagnostics.Debugger.Log(0, null, "Framebuffer driver started");

            /* Parse properties */
            foreach(var prop in root)
            {
                if (prop.Name == "pmem")
                    pmem = prop.Value as tysos.PhysicalMemoryResource64;
                else if (prop.Name == "vmem")
                    vmem = prop.Value as tysos.VirtualMemoryResource64;
                else if (prop.Name == "height")
                    h = (int)prop.Value;
                else if (prop.Name == "width")
                    w = (int)prop.Value;
                else if (prop.Name == "bpp")
                    bpp = (int)prop.Value;
                else if (prop.Name == "pformat")
                    pformat = (int)prop.Value;
                else if (prop.Name == "stride")
                    str = (int)prop.Value;
            }

            if (pmem == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "pmem not provided");
                return false;
            }
            if (vmem == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "vmem not provided");
                return false;
            }

            pmem.Map(vmem);
            buf = vmem.ToArray();

            System.Diagnostics.Debugger.Log(0, null, "Mode: " + w.ToString() +
                "x" + h.ToString() + "x" + bpp.ToString() + ", stride: " +
                str.ToString() + ", pformat: " + pformat.ToString() + ", paddr: " +
                pmem.Addr64.ToString("X") + ", vaddr: " + vmem.Addr64.ToString());

            if(bpp != 32)
            {
                System.Diagnostics.Debugger.Log(0, null, "BPP " + bpp.ToString() +
                    " not supported");
                return false;
            }

            for(int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                    SetPixel(x, y, 0x000000ff);
            }

            root.Add(new File.Property { Name = "class", Value = "framebuffer" });
            Tags.Add("class");

            return true;
        }

        /* In native pixel format */
        void SetPixel(int x, int y, uint color)
        {
            int idx = (x + y * str) * 4;
            buf[idx] = (byte)(color & 0xff);
            buf[idx + 1] = (byte)((color >> 8) & 0xff);
            buf[idx + 2] = (byte)((color >> 16) & 0xff);
            buf[idx + 3] = (byte)((color >> 24) & 0xff);
        }
    }
}
