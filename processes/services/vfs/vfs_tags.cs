/* Copyright (C) 2015 by John Cronin
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
    partial class vfs
    {
        /* A dictionary of strings (tag_names) pointing to a dictionary of
            strings (tag_items) pointing to a list of PathParts (implemented
            as a Dictionary<PathPart, int> to facilitate fast searching)
            
          For example, we maintain the tag_name 'class' to identify devices
          of a particular class.  The 'class' tag_name points to a Dictionary
          of classes, e.g. 'block', 'char' etc which in turn point to lists
          of devices, e.g. /dev/ata0 etc
        */

        internal Type[] CallbackSignature;

        Dictionary<string, TagName> tags =
            new Dictionary<string, TagName>(
                new tysos.Program.MyGenericEqualityComparer<string>());

        public delegate void ModifyDelegate(string path);

        void AddTag(string tag_name, string tag_value, PathPart val)
        {
            lock (tags)
            {
                GetTagName(tag_name).AddTag(tag_value, val);
            }
        }

        void DeleteTag(string tag_name, string tag_value, PathPart val)
        {
            lock (tags)
            {
                GetTagName(tag_name).DeleteTag(tag_value, val);
            }
        }

        public void RegisterAddHandler(string tag_name, string tag_value, string del)
        {
            RegisterAddHandler(tag_name, tag_value, del, false);
        }

        public void RegisterAddHandler(string tag_name, string tag_value, string del,
            bool run_for_current)
        {
            lock (tags)
            {
                var tv = GetTagName(tag_name).GetTagValue(tag_value);
                tv.add_dels.Add(new TagValue.CallbackFunc
                {
                    s = this.SourceThread.owning_process.MessageServer,
                    method = del
                });

                /* Execute the passed callback on all members of this tag */
                if (run_for_current && SourceThread.owning_process.MessageServer != null)
                {
                    foreach (var e in tv.entries.Keys)
                    {
                        SourceThread.owning_process.MessageServer.InvokeAsync(del,
                            new object[] { e.ToString() }, CallbackSignature);
                    }
                }
            }
        }

        public void RegisterDeleteHandler(string tag_name, string tag_value, string del)
        {
            lock (tags)
            {
                GetTagName(tag_name).GetTagValue(tag_value).rem_dels.Add(new TagValue.CallbackFunc
                {
                    s = this.SourceThread.owning_process.MessageServer,
                    method = del
                });
            }
        }

        TagName GetTagName(string tag_name)
        {
            TagName tv;
            if (tags.TryGetValue(tag_name, out tv) == false)
            {
                tv = new TagName(this);
                tags[tag_name] = tv;
            }
            return tv;
        }

        class TagName
        {
            internal Dictionary<string, TagValue> tag_values =
                new Dictionary<string, TagValue>(
                    new tysos.Program.MyGenericEqualityComparer<string>());

            internal vfs vfs;

            public TagValue GetTagValue(string tag_value)
            {
                TagValue tv;
                if (tag_values.TryGetValue(tag_value, out tv) == false)
                {
                    tv = new TagValue(vfs);
                    tag_values[tag_value] = tv;
                }
                return tv;
            }

            public void AddTag(string tag_value, PathPart val)
            {
                var tv = GetTagValue(tag_value);
                tv.AddEntry(val);
            }

            public void DeleteTag(string tag_value, PathPart val)
            {
                var tv = GetTagValue(tag_value);
                tv.DeleteEntry(val);
            }

            public TagName(vfs Vfs) { vfs = Vfs; }
        }

        class TagValue
        {
            internal Dictionary<PathPart, int> entries =
                new Dictionary<PathPart, int>(
                    new tysos.Program.MyGenericEqualityComparer<PathPart>());

            internal vfs vfs;

            internal List<CallbackFunc> add_dels = new List<CallbackFunc>();
            internal List<CallbackFunc> rem_dels = new List<CallbackFunc>();

            internal class CallbackFunc
            {
                internal tysos.ServerObject s;
                internal string method;
            }

            public void AddEntry(PathPart val)
            {
                entries[val] = 1;

                foreach (var add_del in add_dels)
                {
                    if (add_del.s != null)
                        add_del.s.InvokeAsync(add_del.method,
                            new object[] { val.ToString() },
                            vfs.CallbackSignature);
                }
            }

            public void DeleteEntry(PathPart val)
            {
                if (entries.ContainsKey(val))
                {
                    entries.Remove(val);

                    foreach (var rem_del in rem_dels)
                    {
                        if (rem_del.s != null)
                            rem_del.s.InvokeAsync(rem_del.method,
                                new object[] { val.ToString() },
                                vfs.CallbackSignature);
                    }
                }
            }

            public TagValue(vfs Vfs) { vfs = Vfs; }
        }

        public IEnumerable<string> GetPathsByTag(string tag_name, string tag_value)
        {
            var tv = GetTagName(tag_name).GetTagValue(tag_value);

            List<string> ret = new List<string>();
            foreach (var p in tv.entries.Keys)
                ret.Add(p.ToString());

            return ret;
        }

        public void RegisterTag(string tag_name, string path)
        {
            /* We do not allow the user to arbritratily specify a tag value for
            a particular path - we query the file system to get the 'tag'
            property for the particular path */

            if(path == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "RegisterTag called with path as null from " +
                    SourceThread.ProcessName);
                return;
            }
            if (tag_name == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "RegisterTag called with tag_name as null from " +
                    SourceThread.ProcessName);
                return;
            }

            var f = OpenFile(path, System.IO.FileMode.Open,
                System.IO.FileAccess.Read,
                System.IO.FileShare.ReadWrite,
                System.IO.FileOptions.None);
            if (f == null || f.Error != tysos.lib.MonoIOError.ERROR_SUCCESS)
            {
                System.Diagnostics.Debugger.Log(0, null, "RegisterTag(" + tag_name +
                    ", " + path + ") failed to open path");
                return;
            }

            var p_tag_value = f.GetPropertyByName(tag_name);
            CloseFile(f);
            if (p_tag_value == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "RegisterTag(" +
                    tag_name + ", " + path + ") failed to get property");
                return;
            }

            string tag_value = p_tag_value.Value as string;
            if (tag_value == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "RegisterTag(" +
                    tag_name + ", " + path + ") property is not of type string");
                return;
            }

            Path p = GetPath(path);

            AddTag(tag_name, tag_value, (PathPart)p);

            System.Diagnostics.Debugger.Log(0, null, "RegisterTag: added " + p.FullPath +
                " to " + tag_name + "." + tag_value);
        }
    }
}
