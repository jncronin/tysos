using System;
using System.Collections.Generic;
using System.Text;

namespace fbrenderer
{
    partial class FBRenderer
    {
        void HandlerThread()
        {
            while(true)
            {
                if(sm.Read(out var msg))
                {
                    if(msg.ready)
                    {
                        switch(msg.mt)
                        {
                            case tysos.Interfaces.IRendererMessage.MessageType.DrawRect:
                                DrawRect(msg);
                                break;
                        }
                    }
                }
                else
                {
                    tysos.Syscalls.SchedulerFunctions.Block(new tysos.DelegateEvent(
                        delegate ()
                        {
                            return sm.MessgeReady;
                        }));
                }
            }
        }
    }
}
