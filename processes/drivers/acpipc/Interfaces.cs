using System;
using System.Collections.Generic;
using System.Text;
using static tysos.ServerObject;

namespace acpipc
{
    [libsupcs.AlwaysInvoke]
    interface IGSIProvider
    {
        RPCResult<GlobalSystemInterrupt> GetInterruptLine(int gsi_num);
    }
}
