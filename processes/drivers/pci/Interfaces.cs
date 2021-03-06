using System;
using System.Collections.Generic;
using System.Text;
using static tysos.ServerObject;

namespace pci
{
    [libsupcs.AlwaysInvoke]
    public interface IHostBridge
    {
        RPCResult<tysos.RangeResource> GetBAR(PCIConfiguration conf, int bar_no);
    }
}
