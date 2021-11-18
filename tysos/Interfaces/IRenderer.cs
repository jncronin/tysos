using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.Interfaces
{
    public struct IRendererMessage
    {
        public enum MessageType
        {
            DrawRect,
            BitBlt,
            DrawLine
        }

        public enum PixelType
        {
            ARGB8888,
            RGB888,
            BGRA8888,
            BGR888,
            RGB565,
            BGR565
        }

        public MessageType mt;
        public PixelType pt;
        public int x0, y0, x1, y1, x2, y2;
        public uint c0, c1;
        public ulong a0, a1;

        public bool ready;
    }

    [libsupcs.AlwaysInvoke]
    public interface IRenderer
    {
        SharedMemory<IRendererMessage> GetMessageList();
        void FlushQueue();
    }
}
