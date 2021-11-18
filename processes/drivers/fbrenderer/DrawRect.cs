using System;
using System.Collections.Generic;
using System.Text;

namespace fbrenderer
{
    partial class FBRenderer
    {
        void DrawRect(tysos.Interfaces.IRendererMessage msg)
        {
            // very simple for now, assumes stride = w, pt = 32
            byte cb = (byte)(msg.c0 & 0xffU);
            byte cg = (byte)((msg.c0 >> 8) & 0xffU);
            byte cr = (byte)((msg.c0 >> 16) & 0xffU);
            for (int y = msg.y0; y < msg.y1; y++)
            {
                for (int x = msg.x0; x < msg.x1; x++)
                {
                    fbd.buf[(y * fbd.stride + x) * 4] = cb;
                    fbd.buf[(y * fbd.stride + x) * 4 + 1] = cg;
                    fbd.buf[(y * fbd.stride + x) * 4 + 2] = cr;
                    fbd.buf[(y * fbd.stride + x) * 4 + 3] = 0;
                }
            }
        }
    }
}
