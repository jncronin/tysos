/* Copyright (C) 2008 - 2011 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace vfs
{
    partial class vfs : tysos.Ivfs
    {
        static Dictionary<string, FileSystemObject> mounts = new Dictionary<string, FileSystemObject>(new tysos.Program.MyGenericEqualityComparer<string>());
        tysos.Process p_vfs;

        static void Main(string[] args)
        {
            vfs v = new vfs();
            tysos.Syscalls.ProcessFunctions.RegisterSpecialProcess(v, tysos.Syscalls.ProcessFunctions.SpecialProcessType.Vfs);

            object[] a = args as object[];
            tysos.StructuredStartupParameters system_node = a[0] as tysos.StructuredStartupParameters;
            tysos.StructuredStartupParameters mods = a[1] as tysos.StructuredStartupParameters;

            v.Init(system_node, mods);
        }

        void Init(tysos.StructuredStartupParameters system_node,
            tysos.StructuredStartupParameters mods)
        {
            root_fs root = new root_fs(system_node, mods);
            mounts.Add("/", root);

            p_vfs = tysos.Syscalls.ProcessFunctions.GetCurrentProcess();
            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: entering message loop\n");
            tysos.Syscalls.IPCFunctions.InitIPC();

            // TEST: try and execute and load a program
            // first, dump the /devices/system node
            tysos.lib.MonoIOError err;
            tysos.IFile f = _OpenFile("/devices/system", System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.None, System.IO.FileOptions.None,
                out err, tysos.Syscalls.ProcessFunctions.GetCurrentProcess());
            if (f == null)
                throw new Exception("f is null: " + err.ToString());
            tysos.Syscalls.DebugFunctions.DebugWrite("vfs_test: found file with length " + f.Length.ToString() + "\n");
            //byte[] buf = new byte[300];
            System.Diagnostics.Debugger.Break();
            byte[] buf = new byte[f.Length];
            tysos.Syscalls.DebugFunctions.DebugWrite("vfs_test: buffer allocated, calling Read()\n");
            f.GetInputStream().Read(buf, 0, (int)f.Length);
            foreach(byte b in buf)
                tysos.Syscalls.DebugFunctions.DebugWrite((char)b);

            bool cont = true;
            while (cont)
            {
                tysos.IPCMessage msg = null;
                do
                {
                    msg = tysos.Syscalls.IPCFunctions.ReadMessage();

                    if (msg != null)
                        handle_message(msg);
                } while (msg != null);

                tysos.Syscalls.SchedulerFunctions.Block();
            }
        }

        private void handle_message(tysos.IPCMessage msg)
        {
            switch (msg.Type)
            {
                case vfsMessageTypes.GET_ATTRIBUTES:
                    {
                        vfsMessageTypes.FileAttributesMessage fam = msg.Message as vfsMessageTypes.FileAttributesMessage;
                        if (fam != null)
                        {
                            fam.attributes = _GetFileAttributes(ref fam.path);
                            fam.completed.Set();
                        }
                    }
                    break;

                case vfsMessageTypes.GET_FILE_SYSTEM_ENTRIES:
                    {
                        vfsMessageTypes.FileSystemEntriesMessage fsem = msg.Message as vfsMessageTypes.FileSystemEntriesMessage;
                        if (fsem != null)
                        {
                            fsem.files = _GetFileSystemEntries(fsem.path, fsem.path_with_pattern, fsem.attrs, fsem.mask);
                            fsem.completed.Set();
                        }
                    }
                    break;

                case vfsMessageTypes.OPEN:
                    {
                        vfsMessageTypes.OpenFileMessage ofm = msg.Message as vfsMessageTypes.OpenFileMessage;
                        if (ofm != null)
                        {
                            ofm.handle = _OpenFile(ofm.path, ofm.mode, ofm.access, ofm.share, ofm.options, out ofm.error, msg.Source.owning_process);
                            ofm.completed.Set();
                        }
                    }
                    break;

                case vfsMessageTypes.CLOSE:
                    {
                        vfsMessageTypes.OpenFileMessage ofm = msg.Message as vfsMessageTypes.OpenFileMessage;
                        if (ofm != null)
                        {
                            _CloseFile(ofm.handle, out ofm.error, msg.Source.owning_process);
                            ofm.completed.Set();
                        }
                    }
                    break;

                case vfsMessageTypes.MOUNT:
                    {
                        vfsMessageTypes.MountMessage mm = msg.Message as vfsMessageTypes.MountMessage;
                        if (mm != null)
                        {
                            _Mount(mm.mount_point, mm.device);
                            mm.completed.Set();
                        }
                    }
                    break;
            }
        }

        public object OpenFile(string fname)
        {
            throw new NotImplementedException();
        }

        public System.IO.FileAttributes GetFileAttributes(ref string fname)
        {
            vfsMessageTypes.FileAttributesMessage fam = new vfsMessageTypes.FileAttributesMessage();
            fam.path = fname;
            tysos.Syscalls.IPCFunctions.SendMessage(p_vfs, new tysos.IPCMessage { Type = vfsMessageTypes.GET_ATTRIBUTES, Message = fam });

            tysos.Syscalls.SchedulerFunctions.Block(fam.completed);
            fname = fam.path;
            return fam.attributes;
        }

        public string[] GetFileSystemEntries(string path, string path_with_pattern, int attrs, int mask)
        {
            vfsMessageTypes.FileSystemEntriesMessage fsem = new vfsMessageTypes.FileSystemEntriesMessage();
            fsem.path = path;
            fsem.path_with_pattern = path_with_pattern;
            fsem.attrs = attrs;
            fsem.mask = mask;
            tysos.Syscalls.IPCFunctions.SendMessage(p_vfs, new tysos.IPCMessage { Type = vfsMessageTypes.GET_FILE_SYSTEM_ENTRIES, Message = fsem });

            tysos.Syscalls.SchedulerFunctions.Block(fsem.completed);
            return fsem.files;
        }

        System.IO.FileAttributes _GetFileAttributes(ref string path)
        {
            List<FileSystemObject> fsos = GetFSO(path);
            if ((fsos == null) || (fsos.Count == 0))
                return (System.IO.FileAttributes)(-1);

            FileSystemObject fso = fsos[0];
            path = fso.FullPath;
            return (System.IO.FileAttributes)fso.IntAttributes;
        }

        string[] _GetFileSystemEntries(string path, string path_with_pattern, int attrs, int mask)
        {
            List<FileSystemObject> fsos = GetFSO(path_with_pattern);
            if (fsos == null)
                return null;

            List<string> ret = new List<string>();
            path = path.TrimEnd('/');
            path = path + "/";

            foreach (FileSystemObject fso in fsos)
            {
                if ((fso.IntAttributes & mask) == attrs)
                    ret.Add(path + fso.Name);
            }

            return ret.ToArray();
        }

        private List<FileSystemObject> GetFSO(string path)
        {
            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetFSO(string) called with path: " + path + "\n");

            path = path.TrimEnd('/');
            if (path == String.Empty)
                path = "/";
            if (path == "*")
                path = "/*";

            string[] split_string = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> new_path = new List<string>();
            foreach (string s in split_string)
            {
                if (s == ".")
                    continue;
                else if (s == "..")
                {
                    if (new_path.Count == 0)
                        return null;
                    new_path.RemoveAt(new_path.Count - 1);
                }
                else
                    new_path.Add(s);
            }

            string mount = "/" + string.Join("/", new_path.ToArray());
            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetFSO(string) resolved path to: " + mount + "\n");
            List<string> additional = new List<string>();

            do
            {
                if(mount != "/")
                    mount = mount.TrimEnd('/');
                tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetFSO: checking for mount point " + mount + "\n");
                if (mounts.ContainsKey(mount))
                    return GetFSO(mounts[mount], additional);

                if (mount == "/")
                    return null;

                int last_idx = mount.LastIndexOf('/');
                additional.Insert(0, mount.Substring(last_idx + 1));
                if (last_idx != 0)
                    mount = mount.Substring(0, last_idx);
                else
                    mount = "/";
            } while (true);
        }

        private List<FileSystemObject> GetFSO(FileSystemObject mount_point, List<string> path_below)
        {
            tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetFSO(FileSystemObject, List<string>) called\n");
            List<FileSystemObject> ret = new List<FileSystemObject>();

            if (path_below.Count == 0)
            {
                ret.Add(mount_point);
                return ret;
            }

            if (path_below[0] == ".")
            {
                path_below.RemoveAt(0);
                return GetFSO(mount_point, path_below);
            }

            if (path_below[0] == "..")
            {
                path_below.RemoveAt(0);
                if (mount_point.parent == null)
                    return ret;
                return GetFSO(mount_point.parent, path_below);
            }

            if (path_below[0] == "*")
            {
                path_below.RemoveAt(0);
                if (mount_point.Children == null)
                    return null;
                foreach (FileSystemObject fso in mount_point.Children)
                    ret.AddRange(GetFSO(fso, path_below));
                return ret;
            }

            if (mount_point.Children != null)
            {
                foreach (FileSystemObject fso in mount_point.Children)
                {
                    if (fso.name == path_below[0])
                    {
                        path_below.RemoveAt(0);
                        return GetFSO(fso, path_below);
                    }
                }
            }

            return null;
        }
    }

    public abstract class FileSystemObject
    {
        protected internal FileSystemObject parent = null;
        protected internal string name = "";

        public virtual string Name { get { return name; } }
        public virtual IList<FileSystemObject> Children { get { return null; } }
        public virtual FileSystemObject Parent { get { return parent; } }
        public virtual IList<tysos.StructuredStartupParameters.Param> Attributes { get { return null; } }

        public abstract tysos.IFile Open(System.IO.FileAccess access, out tysos.lib.MonoIOError error);

        protected internal FileSystemObject(string _name, DirectoryFileSystemObject Parent) { parent = Parent; name = _name; }

        public virtual string FullPath
        {
            get
            {
                List<string> members = new List<string>();

                FileSystemObject cur = this;
                while (cur.parent != null)
                {
                    members.Insert(0, cur.name);
                    cur = cur.parent;
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("/");
                for (int i = 0; i < members.Count; i++)
                {
                    if (i != 0)
                        sb.Append("/");
                    sb.Append(members[i]);
                }

                return sb.ToString();
            }
        }

        public virtual int IntAttributes
        {
            get
            {
                int attrs = 0;

                if (Children != null)
                    attrs |= (int)System.IO.FileAttributes.Directory;

                if (attrs == 0x0)
                    attrs = (int)System.IO.FileAttributes.Normal;

                return attrs;
            }
        }
    }

    public class DirectoryFileSystemObject : FileSystemObject
    {
        protected internal List<FileSystemObject> children = new List<FileSystemObject>();
        public override IList<FileSystemObject> Children { get { return children; } }

        public override tysos.IFile Open(System.IO.FileAccess access, out tysos.lib.MonoIOError error)
        {
            error = tysos.lib.MonoIOError.ERROR_ACCESS_DENIED;
            return null;
        }

        public DirectoryFileSystemObject(string _name, DirectoryFileSystemObject Parent) : base(_name, Parent) { }
    }

    public class vfsMessageTypes
    {
        public const int WRITE = 0x10010000;
        public const int READ = 0x10010001;
        public const int PEEK = 0x10010002;
        public const int GET_ATTRIBUTES = 0x10010003;
        public const int GET_FILE_SYSTEM_ENTRIES = 0x10010004;
        public const int OPEN = 0x10010005;
        public const int CLOSE = 0x10010006;
        public const int MOUNT = 0x10010007;
        public const int UNMOUNT = 0x10010008;

        public class ReadWriteMessage
        {
            public ulong Handle;
            public byte[] buf;
            public int buf_offset;
            public int count;
            public int count_read;
            public tysos.Event completed = new tysos.Event();
        }

        public class FileAttributesMessage
        {
            public string path;
            public System.IO.FileAttributes attributes;
            public tysos.Event completed = new tysos.Event();
        }

        public class FileSystemEntriesMessage
        {
            public string path;
            public string path_with_pattern;
            public int attrs;
            public int mask;
            public tysos.Event completed = new tysos.Event();
            public string[] files;
        }

        public class OpenFileMessage
        {
            public string path;
            public System.IO.FileMode mode;
            public System.IO.FileAccess access;
            public System.IO.FileShare share;
            public System.IO.FileOptions options;
            public tysos.lib.MonoIOError error;
            public tysos.IFile handle;
            public tysos.Event completed = new tysos.Event();
        }

        public class MountMessage
        {
            public string mount_point;
            public DirectoryFileSystemObject device;
            public tysos.Event completed = new tysos.Event();
        }
    }
}
