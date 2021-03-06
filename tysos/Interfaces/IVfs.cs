using System;
using System.Collections.Generic;
using System.Text;
using static tysos.ServerObject;

namespace tysos.Interfaces
{
    [libsupcs.AlwaysInvoke]
    public interface IVfs
    {
        RPCResult<bool> Mount(string mount_path);
        RPCResult<bool> Mount(string mount_path, string src);
        RPCResult<bool> Mount(string mount_path, IFileSystem src);

        RPCResult<tysos.lib.File> OpenFile(string path, System.IO.FileMode mode, System.IO.FileAccess access,
            System.IO.FileShare share, System.IO.FileOptions options);
        RPCResult<System.IO.FileAttributes> GetFileAttributes(string path);
        RPCResult<string[]> GetFileSystemEntries(string path, string path_with_pattern,
            int attrs, int mask);
        RPCResult<bool> CloseFile(tysos.lib.File handle);

        RPCResult<bool> RegisterAddHandler(string tag_name, string tag_value, int msg_id,
            bool run_for_current);
        RPCResult<bool> RegisterDeleteHandler(string tag_name, string tag_value, int msg_id);

        RPCResult<bool> RegisterTag(string tag, string path);
    }
}
