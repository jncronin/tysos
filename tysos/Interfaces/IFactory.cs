using System;
using System.Collections.Generic;
using System.Text;
using static tysos.ServerObject;

namespace tysos.Interfaces
{
    [libsupcs.AlwaysInvoke]
    public interface IFactory
    {
        RPCResult<IFileSystem> CreateFSHandler(tysos.lib.File src);
    }
}
