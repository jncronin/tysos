/* Copyright (C) 2008 - 2015 by John Cronin
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
    public partial class vfs : tysos.ServerObject, tysos.Interfaces.IVfs
    {
        static Dictionary<PathPart, tysos.Interfaces.IFileSystem> mounts =
            new Dictionary<PathPart, tysos.Interfaces.IFileSystem>(new tysos.Program.MyGenericEqualityComparer<PathPart>());

        static void Main()
        {
            vfs v = new vfs();
            v.CallbackSignature = new Type[] { typeof(string) };
            tysos.Syscalls.ProcessFunctions.RegisterSpecialProcess(v, tysos.Syscalls.ProcessFunctions.SpecialProcessType.Vfs);
            v.MessageLoop();
        }

        public RPCResult<System.IO.FileAttributes> GetFileAttributes(string path)
        {
            tysos.lib.File f = OpenFile(path, System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite,
                System.IO.FileOptions.None).Sync();
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
            if (path[0] != '/')
                throw new Exception("relative paths not supported");

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

            List<string> additional = new List<string>();

            PathPart pp_mount = new PathPart { path = new_path };
            PathPart pp_additional = new PathPart { path = additional };

            /* The entire path is in pp_mount.  Check if this is a mount point.
                If not, take the last element and insert it at the beginning of
                additional and repeat until either a mount point is found or
                mount is empty, at which point fail
            */
            do
            {
                //tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetPath: checking for mount point " + mount + "\n");
                if (mounts.ContainsKey(pp_mount))
                {
                    //tysos.Syscalls.DebugFunctions.DebugWrite("vfs: GetPath: found mount point " + mount + " of type " + mounts[mount].GetType().FullName + "\n");
                    return new Path { device = mounts[pp_mount], path = pp_additional, mount_point = pp_mount };
                }

                // handle being passed the root directory when nothing is mounted
                if (new_path.Count == 0)
                {
                    if (additional.Count == 0)
                        return new Path { device = null, mount_point = null, path = pp_additional };
                    else
                        return null;
                }

                string last_elem = new_path[new_path.Count - 1];
                new_path.RemoveAt(new_path.Count - 1);
                additional.Insert(0, last_elem);
            } while (true);
        }
    }

    public class PathPart : IEquatable<PathPart>
    {
        public IList<string> path;

        public void AppendTo(StringBuilder sb)
        {
            if (path == null)
                return;
            foreach(string s in path)
            {
                sb.Append("/");
                sb.Append(s);
            }
        }

        public bool Equals(PathPart other)
        {
            if (other == null)
                return false;

            if (path == null)
            {
                if (other.path == null)
                    return true;
                else
                    return false;
            }
            else if (other.path == null)
                return false;

            if (path.Count != other.path.Count)
                return false;

            for(int i = 0; i < path.Count; i++)
            {
                if (path[i].Equals(other.path[i]) == false)
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PathPart);
        }

        public override int GetHashCode()
        {
            if (path == null)
                return base.GetHashCode();

            int hc = 0;
            foreach (string s in path)
                hc ^= s.GetHashCode();

            return hc;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            AppendTo(sb);
            if (sb.Length == 0)
                return "/";
            return sb.ToString();
        }
    }

    public class Path
    {
        public tysos.Interfaces.IFileSystem device;
        public PathPart mount_point;
        public PathPart path;

        public string FullPath
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (mount_point != null)
                    mount_point.AppendTo(sb);
                if (path != null)
                    path.AppendTo(sb);

                if (sb.Length == 0)
                    sb.Append("/");

                return sb.ToString();
            }
        }

        public static explicit operator PathPart(Path p)
        {
            List<string> pp = new List<string>();
            if(p.mount_point != null)
            {
                foreach (string s in p.mount_point.path)
                    pp.Add(s);
            }
            if(p.path != null)
            {
                foreach (string s in p.path.path)
                    pp.Add(s);
            }
            return new PathPart { path = pp };
        }
    }
}
