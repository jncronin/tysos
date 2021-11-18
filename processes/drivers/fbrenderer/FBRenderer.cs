using System;

namespace fbrenderer
{
    public partial class FBRenderer
    {
        tysos.SharedMemory<tysos.Interfaces.IRendererMessage> sm;

        public class FBDesc
        {
            public int w, h;
            public int stride;
            public tysos.Interfaces.IRendererMessage.PixelType pt;
            public byte[] buf;
        }

        FBDesc fbd = null;

        public FBRenderer(FBDesc fbdesc, tysos.VirtualMemoryResource64 vmem)
        {
            fbd = fbdesc;
            sm = new tysos.SharedMemory<tysos.Interfaces.IRendererMessage>(vmem);
        }

        public FBRenderer(FBDesc fbdesc, int size = 1024*1024)
        {
            fbd = fbdesc;
            sm = new tysos.SharedMemory<tysos.Interfaces.IRendererMessage>(new byte[size]);
        }

        public void ForkRendererThread()
        {
            var t = new System.Threading.Thread(HandlerThread);
            t.Start();
        }

        public tysos.SharedMemory<tysos.Interfaces.IRendererMessage> MessageList { get { return sm; } }
    }
}
