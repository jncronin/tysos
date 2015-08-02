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
using tysos.Messages;

namespace vfs
{
    partial class vfs : tysos.ServerObject
    {
        static Dictionary<string, tysos.ServerObject> mounts =
            new Dictionary<string, tysos.ServerObject>(new tysos.Program.MyGenericEqualityComparer<string>());

        static void Main()
        {
            vfs v = new vfs();
            tysos.Syscalls.ProcessFunctions.RegisterSpecialProcess(v, tysos.Syscalls.ProcessFunctions.SpecialProcessType.Vfs);
            v.MessageLoop();
        }

        public System.IO.FileAttributes GetFileAttributes(string path)
        {
            tysos.lib.File f = OpenFile(path, System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite,
                System.IO.FileOptions.None);
            if (f.Error != tysos.lib.MonoIOError.ERROR_SUCCESS)
                return (System.IO.FileAttributes)(-1);

            int props = f.IntProperties;
            CloseFile(f);

            return (System.IO.FileAttributes)props;
        }

        public Path GetPath(string path)
        {
            //tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetPath(string) called with path: " + path + "\n");

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
            //tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetPath(string) resolved path to: " + mount + "\n");
            List<string> additional = new List<string>();

            do
            {
                if (mount != "/")
                    mount = mount.TrimEnd('/');
                //tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetPath: checking for mount point " + mount + "\n");
                if (mounts.ContainsKey(mount))
                {
                    //tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetPath: found mount point " + mount + " of type " + mounts[mount].GetType().FullName + "\n");
                    return new Path { device = mounts[mount], path = additional, mount_point = mount };
                }

                if (mount == "/")
                {
                    // handle being passed the root directory when nothing is mounted
                    if (new_path.Count == 0)
                        return new Path { device = null, mount_point = null, path = new List<string> { } };
                    return null;
                }

                int last_idx = mount.LastIndexOf('/');
                additional.Insert(0, mount.Substring(last_idx + 1));
                if (last_idx != 0)
                    mount = mount.Substring(0, last_idx);
                else
                    mount = "/";
            } while (true);

        }
    }

    public class Path
    {
        public tysos.ServerObject device;
        public string mount_point;
        public IList<string> path;

        public string FullPath
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (mount_point != null)
                    sb.Append(mount_point);

                foreach(string s in path)
                {
                    sb.Append("/");
                    sb.Append(s);
                }

                if (sb.Length == 0)
                    sb.Append("/");

                return sb.ToString();
            }
        }
    }

    public abstract class FileSystemObject
    {
        protected internal FileSystemObject parent = null;
        protected internal string name = "";

        public virtual string Name { get { return name; } }
        public virtual IList<FileSystemObject> Children { get { return null; } }
        public virtual FileSystemObject Parent { get { return parent; } }

        internal VersionedList<tysos.StructuredStartupParameters.Param> props =
            new VersionedList<tysos.StructuredStartupParameters.Param>();

        protected internal virtual ICollection<tysos.StructuredStartupParameters.Param> Properties { get { return props; } }
        protected internal virtual tysos.StructuredStartupParameters.Param GetPropertyByName(string name)
        {
            foreach(tysos.StructuredStartupParameters.Param p in props)
            {
                if (p.Name == name)
                    return p;
            }
            return null;
        }

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
}
