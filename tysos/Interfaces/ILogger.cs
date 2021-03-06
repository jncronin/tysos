using System;
using System.Collections.Generic;
using System.Text;
using static tysos.ServerObject;

namespace tysos.Interfaces
{
    [libsupcs.AlwaysInvoke]
    public interface ILogger
    {
        RPCResult<bool> LogMessage(string source, int level, string message);
    }
}
