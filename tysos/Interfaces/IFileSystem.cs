using System;
using System.Collections.Generic;
using System.Text;
using static tysos.ServerObject;

namespace tysos.Interfaces
{
    [libsupcs.AlwaysInvoke]
    public interface IFileSystem
    {
        RPCResult<tysos.lib.File> Open(IList<string> path, System.IO.FileMode mode,
            System.IO.FileAccess access, System.IO.FileShare share,
            System.IO.FileOptions options);
        RPCResult<bool> Close(lib.File handle);
        RPCResult<int> Read(tysos.lib.File f, long pos, byte[] dest, int dest_offset, int count);
        RPCResult<int> Write(tysos.lib.File f, long pos, byte[] dest, int dest_offset, int count);
        RPCResult<int> IntProperties(tysos.lib.File f);
        RPCResult<string> GetName(lib.File f);
        RPCResult<tysos.lib.File.Property> GetPropertyByName(lib.File f, string name);
        RPCResult<tysos.lib.File.Property[]> GetAllProperties(lib.File f);
        RPCResult<long> GetLength(lib.File f);
        RPCResult<bool> SetMountPath(string p);
    }
}
