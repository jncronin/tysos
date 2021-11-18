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

namespace bga
{
    partial class bga : tysos.lib.VirtualFileServer, tysos.Interfaces.IFileSystem, tysos.Interfaces.IRenderer
    {
        pci.PCIConfiguration pciconf;
        internal tysos.x86_64.IORangeManager ios = new tysos.x86_64.IORangeManager();
        tysos.VirtualMemoryResource64 vmem;
        tysos.x86_64.IOResource index, data;

        const ushort VBE_DISPI_IOPORT_INDEX = 0x1ce;
        const ushort VBE_DISPI_IOPORT_DATA = 0x1cf;

        const ushort VBE_DISPI_INDEX_ID = 0;
        const ushort VBE_DISPI_INDEX_XRES = 1;
        const ushort VBE_DISPI_INDEX_YRES = 2;
        const ushort VBE_DISPI_INDEX_BPP = 3;
        const ushort VBE_DISPI_INDEX_ENABLE = 4;
        const ushort VBE_DISPI_INDEX_VIRT_HEIGHT = 6;

        const ushort VBE_DISPI_DISABLED = 0;
        const ushort VBE_DISPI_ENABLED = 1;
        const ushort VBE_DISPI_LFB_ENABLED = 0x40;

        fbrenderer.FBRenderer.FBDesc fbd = null;

        public bga(tysos.lib.File.Property[] Properties)
        {
            root = new List<tysos.lib.File.Property>(Properties);
        }

        public override bool InitServer()
        {
            System.Diagnostics.Debugger.Log(0, "bga", "BGA driver started");

            // Interpret properties
            pciconf = pci.PCIConfiguration.GetPCIConf(root);
            ios.Init(root);
            index = ios.Contains(VBE_DISPI_IOPORT_INDEX, 2);
            data = ios.Contains(VBE_DISPI_IOPORT_DATA, 2);
            foreach (var prop in root)
            {
                if (prop.Name == "vmem" && (prop.Value is tysos.VirtualMemoryResource64))
                {
                    vmem = prop.Value as tysos.VirtualMemoryResource64;
                    break;
                }
            }

            if(pciconf == null)
            {
                System.Diagnostics.Debugger.Log(0, "bga", "no pci configuration provided");
                return false;
            }
            if(vmem == null)
            {
                System.Diagnostics.Debugger.Log(0, "bga", "no virtual memory space provided");
                return false;
            }
            if(index == null)
            {
                System.Diagnostics.Debugger.Log(0, "bga", "index port not provided");
                return false;
            }
            if (data == null)
            {
                System.Diagnostics.Debugger.Log(0, "bga", "data port not provided");
                return false;
            }

            /* Determine the version of the BGA */
            ushort ver = ReadRegister(VBE_DISPI_INDEX_ID);
            System.Diagnostics.Debugger.Log(0, "bga", "BGA version " + ver.ToString("X4") + " detected");
            if (ver < 0xb0c2)
            {
                System.Diagnostics.Debugger.Log(0, null, "Unsupported BGA version");
                return false;
            }

            /* Set a reasonable resolution */
            if(SetMode(1024, 768, 32) == false)
            {
                System.Diagnostics.Debugger.Log(0, "bga", "failed to set 1024x768x32");
                return false;
            }

            /* Identify ourselves as a framebuffer device */
            root.Add(new tysos.lib.File.Property { Name = "class", Value = "framebuffer" });
            Tags.Add("class");

            read_fbd();

            return true;
        }

        void WriteRegister(ushort RegId, ushort val)
        {
            index.Write(VBE_DISPI_IOPORT_INDEX, 2, RegId);
            index.Write(VBE_DISPI_IOPORT_DATA, 2, val);
        }

        ushort ReadRegister(ushort RegId)
        {
            index.Write(VBE_DISPI_IOPORT_INDEX, 2, RegId);
            return (ushort)index.Read(VBE_DISPI_IOPORT_DATA, 2);
        }

        public bool SetMode(int Width, int Height, int BitDepth)
        {
            /* As per OSdev wiki, sequence is:
                1) disable VBE extensions
                2) set resolution
                3) re-enable VBE extensions and LFB capability

               We then recheck BAR0 to get the physical address of the LFB
               in case it has changed following mode setting
               */

            WriteRegister(VBE_DISPI_INDEX_ENABLE, VBE_DISPI_DISABLED);
            WriteRegister(VBE_DISPI_INDEX_XRES, (ushort)Width);
            WriteRegister(VBE_DISPI_INDEX_YRES, (ushort)Height);
            WriteRegister(VBE_DISPI_INDEX_BPP, (ushort)BitDepth);
            WriteRegister(VBE_DISPI_INDEX_ENABLE, VBE_DISPI_ENABLED | VBE_DISPI_LFB_ENABLED);

            /* Read back the current display mode */
            int act_Width = ReadRegister(VBE_DISPI_INDEX_XRES);
            int act_Height = ReadRegister(VBE_DISPI_INDEX_YRES);
            int act_Bpp = ReadRegister(VBE_DISPI_INDEX_BPP);

            if(Width != act_Width || Height != act_Height || BitDepth != act_Bpp)
            {
                System.Diagnostics.Debugger.Log(0, null, "failed to set mode " +
                    Width.ToString() + "x" + Height.ToString() + "x" + BitDepth.ToString() +
                    " - current mode: " + act_Width.ToString() + "x" +
                    act_Height.ToString() + "x" + act_Bpp.ToString());
                return false;
            }

            /* Read LFB address */
            var lfb = pciconf.GetBAR(0) as tysos.PhysicalMemoryResource64;
            if(lfb == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "failed to read LFB address from BAR0");
                return false;
            }

            lfb.Map(vmem);

            /* Report success */
            System.Diagnostics.Debugger.Log(0, null, "SetMode: successfully set mode: " +
                Height.ToString() + "x" + Width.ToString() + "x" + BitDepth.ToString() +
                ", LFB at physical " + lfb.Addr64.ToString("X") + ", virtual " +
                vmem.Addr64.ToString("X"));

            read_fbd();

            return true;
        }

        private void read_fbd()
        {
            /* Read back the current display mode */
            int act_Width = ReadRegister(VBE_DISPI_INDEX_XRES);
            int act_Height = ReadRegister(VBE_DISPI_INDEX_YRES);
            int act_Bpp = ReadRegister(VBE_DISPI_INDEX_BPP);
            int act_Stride = ReadRegister(VBE_DISPI_INDEX_VIRT_HEIGHT);

            fbd.w = act_Width;
            fbd.h = act_Height;
            fbd.stride = (act_Stride > act_Width) ? act_Stride : act_Width;
            switch(act_Bpp)
            {
                case 0x10:  // 16BPP
                    fbd.pt = tysos.Interfaces.IRendererMessage.PixelType.BGR565;
                    break;
                case 0x18:  // 24BPP
                    fbd.pt = tysos.Interfaces.IRendererMessage.PixelType.BGR888;
                    break;
                case 0x20:  // 32BPP
                    fbd.pt = tysos.Interfaces.IRendererMessage.PixelType.BGRA8888;
                    break;
            }

            fbd.buf = vmem.ToArray();
        }

        fbrenderer.FBRenderer fbr;

        public tysos.SharedMemory<tysos.Interfaces.IRendererMessage> GetMessageList()
        {
            if(fbr == null)
            {
                fbr = new fbrenderer.FBRenderer(fbd);
                fbr.ForkRendererThread();
            }
            return fbr.MessageList;
        }

        public void FlushQueue()
        {
            // This happens automatically wih the FBRenderer
        }
    }
}
