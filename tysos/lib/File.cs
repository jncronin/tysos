﻿/* Copyright (C) 2015 by John Cronin
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

namespace tysos.lib
{
    public class File
    {
        public class Times
        {
            public System.DateTime Creation, LastAccessed, LastModified;
        }

        public class Property
        {
            public string Name;
            public object Value;
        }

        public enum SeekPosition { Set, Cur, End }

        protected internal MonoIOError err;
        public virtual MonoIOError Error { get { return err; } set { err = value; } }

        protected internal long pos;
        protected internal bool CanRead, CanWrite, CanSeek, CanGrow;
        protected internal MonoFileType fileType;
        protected internal bool isatty;

        public virtual int IntProperties
        {
            get
            {
                return d.IntProperties(this).Sync();
            }
        }
        public virtual MonoFileType FileType { get { return fileType; } }
        public virtual bool Isatty { get { return isatty; } }
        public virtual Interfaces.IFileSystem Device { get { return d; } }

        protected internal Interfaces.IFileSystem d;

        public virtual long Position
        {
            get { return pos; }
            set
            {
                if (CanSeek == false)
                    throw new NotSupportedException();
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Cannot seek to negative position");
                if (value > Length && CanGrow == false)
                    throw new System.IO.EndOfStreamException();

                pos = value;
            }
        }

        public void Seek(long offset, System.IO.SeekOrigin whence)
        {
            switch (whence)
            {
                case System.IO.SeekOrigin.Current:
                    Position = Position + offset;
                    break;
                case System.IO.SeekOrigin.End:
                    Position = Length + offset;
                    break;
                case System.IO.SeekOrigin.Begin:
                    Position = offset;
                    break;
            }
        }

        public virtual long Length
        {
            get
            {
                return d.GetLength(this).Sync();
            }
        }

        public virtual int Read(byte[] dest, int dest_offset, int count)
        {
            if (CanRead == false)
            {
                err = MonoIOError.ERROR_ACCESS_DENIED;
                return 0;
            }

            var ret = d.Read(this, pos, dest, dest_offset, count).Sync();

            pos += ret;
            return ret;
        }

        public virtual int Write(byte[] dest, int dest_offset, int count)
        {
            if (CanWrite == false)
            {
                err = MonoIOError.ERROR_ACCESS_DENIED;
                return 0;
            }

            var ret = d.Write(this, pos, dest, dest_offset, count).Sync();

            pos += ret;
            return ret;
        }

        public virtual bool DataAvailable(int timeout)
        {
            if (Position >= Length)
                return false;
            return true;
        }

        public virtual tysos.lib.File.Property GetPropertyByName(string name)
        {
            return d.GetPropertyByName(this, name).Sync();
        }

        public virtual tysos.lib.File.Property[] GetAllProperties()
        {
            return d.GetAllProperties(this).Sync();
        }

        public virtual string Name
        {
            get
            {
                return d.GetName(this).Sync();
            }
        }

        public static System.Type[] sig_Open;
        public static System.Type[] sig_Close;
        public static System.Type[] sig_Read;
        public static System.Type[] sig_Write;
        public static System.Type[] sig_IntProperties;
        public static System.Type[] sig_GetLength;
        public static System.Type[] sig_GetName;
        public static System.Type[] sig_GetPropertyByName;
        public static System.Type[] sig_GetAllProperties;

        public static System.Type[] sig_vfs_OpenFile;
        public static System.Type[] sig_vfs_CloseFile;
        public static System.Type[] sig_vfs_GetFileAttributes;
        public static System.Type[] sig_vfs_GetFileSystemEntries;

        internal static void InitSigs()
        {
            sig_Open = new System.Type[] { typeof(IList<string>),
                typeof(System.IO.FileMode),
                typeof(System.IO.FileAccess),
                typeof(System.IO.FileShare),
                typeof(System.IO.FileOptions)
            };
            sig_Close = new System.Type[] { typeof(File) };
            sig_Read = new System.Type[] { typeof(File),
                typeof(long),
                typeof(byte[]),
                typeof(int),
                typeof(int)
            };
            sig_Write = new System.Type[] { typeof(File),
                typeof(long),
                typeof(byte[]),
                typeof(int),
                typeof(int)
            };
            sig_IntProperties = new System.Type[] { typeof(File) };
            sig_GetLength = new System.Type[] { typeof(File) };
            sig_GetName = new System.Type[] { typeof(File) };
            sig_GetPropertyByName = new System.Type[] { typeof(File), typeof(string) };
            sig_GetAllProperties = new System.Type[] { typeof(File) };

            sig_vfs_OpenFile = new System.Type[] {
                typeof(string),
                typeof(System.IO.FileMode),
                typeof(System.IO.FileAccess),
                typeof(System.IO.FileShare),
                typeof(System.IO.FileOptions)
            };
            sig_vfs_CloseFile = new System.Type[] {
                typeof(File)
            };
            sig_vfs_GetFileAttributes = new System.Type[] {
                typeof(string)
            };
            sig_vfs_GetFileSystemEntries = new System.Type[] {
                typeof(string),
                typeof(string),
                typeof(int),
                typeof(int)
            };
        }
    }

    public class VirtualDirectory : VirtualPropertyFile
    {
        public VirtualDirectory(ServerObject device, string name,
            List<string> children) : this(device, name, children, new Property[] { })
        { }

        public VirtualDirectory(ServerObject device, string name,
            List<string> children, IEnumerable<Property> props) : base(device, name)
        {
            intProperties = (int)System.IO.FileAttributes.Directory;
            base.Props.Add(new Property { Name = "Children", Value = children });
            Props.AddRange(props);
        }
    }

    public class VirtualPropertyFile : File
    {
        protected internal int intProperties;
        protected internal List<tysos.lib.File.Property> Props;
        protected internal string _name;

        public VirtualPropertyFile(ServerObject device, string name)
            : this(device, name, new List<Property>())
        { }

        public VirtualPropertyFile(ServerObject device, string name,
            List<tysos.lib.File.Property> props)
        {
            d = (Interfaces.IFileSystem)device;

            CanRead = false;
            CanWrite = false;
            CanSeek = false;
            CanGrow = false;

            isatty = false;

            Props = props;

            intProperties = (int)System.IO.FileAttributes.ReadOnly;
            _name = name;
        }

        public override int IntProperties
        {
            get
            {
                return intProperties;
            }
        }

        public override tysos.lib.File.Property GetPropertyByName(string name)
        {
            if (Props == null)
                return null;

            foreach (tysos.lib.File.Property p in Props)
            {
                if (p.Name == name)
                    return p;
            }
            return null;
        }

        public override Property[] GetAllProperties()
        {
            return Props.ToArray();
        }

        public override long Length
        {
            get
            {
                return 0;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }
    }

    public class VirtualDirectoryServer : ServerObject
    {
        protected Dictionary<string, List<File.Property>> children;
        protected List<File.Property> root;
        protected string name;
        protected bool is_dir = true;

        public VirtualDirectoryServer(string Name,
            List<File.Property> Root,
            Dictionary<string, List<File.Property>> Children)
        {
            children = Children;
            root = Root;
            name = Name;
            root.Add(new File.Property { Name = "server", Value = this });
        }

        public VirtualDirectoryServer()
        {
            children = new Dictionary<string, List<File.Property>>(new Program.MyGenericEqualityComparer<string>());
            root = new List<File.Property>();
            name = "";
            root.Add(new File.Property { Name = "server", Value = this });
        }

        public RPCResult<tysos.lib.File> Open(IList<string> path, System.IO.FileMode mode,
            System.IO.FileAccess access, System.IO.FileShare share,
            System.IO.FileOptions options)
        {
            if (path.Count == 0)
            {
                if (is_dir)
                {
                    List<string> c = new List<string>(children.Keys);
                    return new tysos.lib.VirtualDirectory(this, "", c, root);
                }
                else
                    return new VirtualPropertyFile(this, "", root);
            }

            // only one level
            if (path.Count != 1 || is_dir == false)
                return new tysos.lib.ErrorFile(tysos.lib.MonoIOError.ERROR_FILE_NOT_FOUND);

            if (children.ContainsKey(path[0]))
                return new VirtualPropertyFile(this, path[0], children[path[0]]);

            return new tysos.lib.ErrorFile(tysos.lib.MonoIOError.ERROR_FILE_NOT_FOUND);
        }

        public RPCResult<bool> Close(tysos.lib.File handle)
        {
            return true;
        }

        public RPCResult<int> Read(tysos.lib.File f, long pos, byte[] dest, int dest_offset, int count)
        {
            f.Error = MonoIOError.ERROR_READ_FAULT;
            return 0;
        }

        public RPCResult<int> Write(tysos.lib.File f, long pos, byte[] dest, int dest_offset, int count)
        {
            f.Error = MonoIOError.ERROR_WRITE_FAULT;
            return 0;
        }

        public RPCResult<string> GetName(File f)
        {
            throw new NotImplementedException();
        }

        public RPCResult<int> IntProperties(File f)
        {
            throw new NotImplementedException();
        }

        public RPCResult<File.Property> GetPropertyByName(File f, string name)
        {
            throw new NotImplementedException();
        }

        public RPCResult<File.Property[]> GetAllProperties(File f)
        {
            throw new NotImplementedException();
        }

        public RPCResult<long> GetLength(File f)
        {
            throw new NotImplementedException();
        }
    }

    public class VirtualFileServer : VirtualDirectoryServer
    {
        public VirtualFileServer(string Name,
            List<File.Property> Root) : base(Name, Root, null)
        {
            is_dir = false;
        }

        public VirtualFileServer() : base()
        {
            is_dir = false;
        }
    }
}

