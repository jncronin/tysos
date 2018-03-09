using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isomake
{
    class AnnotatedFSO
    {
        public FileSystemInfo fsi;
        public string Identifier;

        public string FName, Ext;

        public List<AnnotatedFSO> Children = new List<AnnotatedFSO>();
        public AnnotatedFSO Parent;

        public int dir_idx;

        public int lba;

        public override string ToString()
        {
            if (Parent != null)
                return Parent.ToString() + "/" + Identifier;
            else
                return "";
        }

        internal static List<AnnotatedFSO>[] BuildAFSOTree(DirectoryInfo rootdir, out List<AnnotatedFSO> dirs, out List<AnnotatedFSO> files)
        {
            List<AnnotatedFSO>[] ret = new List<AnnotatedFSO>[8];

            // First, iterate all FSOs to build MSDOS names from them
            Dictionary<string, List<AnnotatedFSO>> doscache = new Dictionary<string, List<AnnotatedFSO>>();
            var root = doscache_add(rootdir, null, doscache, true);

            // Now determine how many need mangling, and extract to dir/file lists
            dirs = new List<AnnotatedFSO>();
            files = new List<AnnotatedFSO>();
            foreach(var key in doscache.Keys)
            {
                var contents = doscache[key];
                bool needs_mangling = false;
                if (contents.Count > 1)
                    needs_mangling = true;
                for(int i = 0; i < contents.Count; i++)
                {
                    var ci = contents[i];
                    if (needs_mangling)
                    {
                        string mn = i.ToString("D2");
                        if (contents.Count >= 10)
                        {
                            ci.FName = ci.FName.Substring(0, Math.Min(6, ci.FName.Length)).PadRight(6) + mn;
                        }
                        else
                        {
                            ci.FName = ci.FName.Substring(0, Math.Min(7, ci.FName.Length)).PadRight(7) + mn[1];
                        }
                        if (ci.fsi is FileInfo)
                            ci.Identifier = ci.FName + "." + ci.Ext;
                        else
                            ci.Identifier = ci.FName;
                    }
                    else
                        ci.Identifier = key;

                    // add version to files
                    if(ci.fsi is FileInfo)
                        ci.Identifier = ci.Identifier + ";1";

                    if (ci.fsi is DirectoryInfo)
                        dirs.Add(ci);
                    else
                        files.Add(ci);
                }
            }

            // Build the directory tree
            List<AnnotatedFSO> cur_level = new List<AnnotatedFSO> { root };
            int cur_didx = 1;
            for(int level = 0; level < 8; level++)
            {
                List<AnnotatedFSO> next_level = new List<AnnotatedFSO>();

                cur_level.Sort(sort_func);

                var cur_dirs = new List<AnnotatedFSO>();

                foreach(var c in cur_level)
                {
                    if(c.fsi is DirectoryInfo)
                    {
                        cur_dirs.Add(c);
                        c.dir_idx = cur_didx++;
                        next_level.AddRange(c.Children);
                    }
                }

                ret[level] = cur_dirs;
                cur_level = next_level;
            }


            return ret;
        }

        static int sort_func(AnnotatedFSO a, AnnotatedFSO b)
        {
            if (a.Parent == null && b.Parent == null)
                return 0;
            else if (a.Parent == null)
                return -1;
            else if (b.Parent == null)
                return 1;

            int pcompare = sort_func(a.Parent, b.Parent);
            if (pcompare != 0)
                return pcompare;

            return a.Identifier.CompareTo(b.Identifier);
        }

        private static AnnotatedFSO doscache_add(FileSystemInfo d, AnnotatedFSO parent, Dictionary<string, List<AnnotatedFSO>> doscache, bool is_root = false)
        {
            var dosname = build_dosname(d, is_root, out var dn2);
            if(!doscache.TryGetValue(dn2, out var dcl))
            {
                dcl = new List<AnnotatedFSO>();
                doscache[dn2] = dcl;
            }

            var new_afso = new AnnotatedFSO();
            new_afso.fsi = d;
            new_afso.Parent = parent;
            new_afso.FName = dosname[0];
            new_afso.Ext = dosname[1];
            dcl.Add(new_afso);

            var di = d as DirectoryInfo;
            if(di != null)
            {
                foreach(var c in di.GetFiles())
                {
                    new_afso.Children.Add(doscache_add(c, new_afso, doscache));
                }
                foreach(var c in di.GetDirectories())
                {
                    new_afso.Children.Add(doscache_add(c, new_afso, doscache));
                }
            }

            return new_afso;
        }

        private static string[] build_dosname(FileSystemInfo d, bool is_root, out string id)
        {
            if (is_root)
            {
                id = "\0";
                return new string[] { "\0", null };
            }

            // Split so that the first, rather than last, period defines the extension
            var wn = d.Name + "." + d.Extension + " ";
            var n = wn.Substring(0, wn.IndexOf('.'));
            var e = wn.Substring(wn.IndexOf('.') + 1);
            e = string.Join("", e.Split('.'));
            e = e.TrimEnd(' ');

            string[] ret;

            if(d is DirectoryInfo)
            {
                var di = d as DirectoryInfo;
                ret = new string[] { trim_str(n, 8), null };
                ret[0] = dosify(ret[0]);
                id = ret[0];
            }
            else
            {
                var fi = d as FileInfo;
                ret = new string[] { trim_str(n, 8), trim_str(e, 3) };
                ret[0] = dosify(ret[0]);
                ret[1] = dosify(ret[1]);
                id = ret[0] + "." + ret[1];
            }

            return ret;
        }

        private static string dosify(string v)
        {
            // replace all non-strD characters with _
            StringBuilder sb = new StringBuilder();
            foreach(var c in v)
            {
                var d = char.ToUpper(c);
                if (d == '_' || (d >= 'A' && d <= 'Z') || (d >= '0' || d <= '9'))
                    sb.Append(d);
                else
                    sb.Append(' ');
            }
            return sb.ToString();
        }

        private static string trim_str(string name, int v)
        {
            if (name.Length <= v)
                return name;
            else
                return name.Substring(0, v);
            //else
            //    return name.PadRight(v);
        }
    }

    class IdentifierCache
    {
        Dictionary<string, AnnotatedFSO> cache = new Dictionary<string, AnnotatedFSO>();
    }


}
