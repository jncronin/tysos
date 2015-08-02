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
        public string[] GetFileSystemEntries(string path, string path_with_pattern,
            int attrs, int mask)
        {
            /* We are handed a string, path_with_pattern, which may include wildcards
             * 
             * It should be an absolute path (handled by the tysos.lib.MonoIO layer - TODO)
             * 
             * We then take each element in turn, expanding it to all possible matches
             * and move on
             */

            List<string> ret = new List<string>();
            string[] split_path = path_with_pattern.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            /* Open the root directory and begin searching from there */
            tysos.lib.File root_dir_f = OpenFile("/", System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite,
                System.IO.FileOptions.None);
            getFSE(split_path, 0, ret, root_dir_f, new List<string>(), attrs, mask);
            CloseFile(root_dir_f);

            return ret.ToArray();
        }

        private void getFSE(string[] split_path, int cur_depth, List<string> ret,
            tysos.lib.File cur_dir_f, List<string> cur_path, int attrs, int mask)
        {
            // Get the children of the current node
            if(cur_dir_f == null || cur_dir_f.Error != tysos.lib.MonoIOError.ERROR_SUCCESS)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("getFSE: cur_dir_f is null");
                return;
            }
            tysos.StructuredStartupParameters.Param children = cur_dir_f.GetPropertyByName("Children");
            if(children == null)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("getFSE: GetPropertyByName(\"Children\") returned null");
                return;
            }

            // Iterate through each, seeing if they match the current part of the name
            IList<string> children_list = children.Value as IList<string>;
            if(children_list == null)
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("getFSE: children.Value is not of type " +
                    "IList<string> (instead is " + children.Value.GetType().FullName + ")");
                ulong v0 = libsupcs.CastOperations.ReinterpretAsUlong(children.Value);
                ulong v1 = libsupcs.CastOperations.ReinterpretAsUlong(children.Value.GetType());
                ulong v2 = libsupcs.CastOperations.ReinterpretAsUlong(typeof(IList<string>));
                tysos.Syscalls.DebugFunctions.DebugWrite("getFSE: " + v1.ToString("X16") + " (" + v0.ToString("X16") + ") vs " + v2.ToString("X16"));
                return;
            }
            foreach(string child in children_list)
            {
                //tysos.Syscalls.DebugFunctions.DebugWrite("getFSE: examining child: " + child + "\n");

                if (match(child, split_path[cur_depth], true))
                {
                    //tysos.Syscalls.DebugFunctions.DebugWrite("match successful\n");

                    /* Build the absolute path to the child */
                    StringBuilder cur_child_path = new StringBuilder();
                    foreach (string parent in cur_path)
                    {
                        cur_child_path.Append("/");
                        cur_child_path.Append(parent);
                    }
                    cur_child_path.Append("/");
                    cur_child_path.Append(child);

                    /* Open the file to get its properties */
                    tysos.lib.File child_dir = OpenFile(cur_child_path.ToString(),
                        System.IO.FileMode.Open, System.IO.FileAccess.Read,
                        System.IO.FileShare.ReadWrite,
                        System.IO.FileOptions.None);
                    if (child_dir == null || child_dir.Error != tysos.lib.MonoIOError.ERROR_SUCCESS)
                    {
                        tysos.Syscalls.DebugFunctions.DebugWrite("getFSE: OpenFile(" + cur_child_path.ToString() + ") failed\n");
                        return;
                    }
                    int int_attrs = child_dir.IntProperties;

                    /* If this is the deepest depth to check, then all matching
                    children are valid and should be added to ret */
                    if (cur_depth == split_path.Length - 1)
                    {
                        //tysos.Syscalls.DebugFunctions.DebugWrite("adding: " + cur_child_path.ToString() + "\n");

                        if((int_attrs & mask) == attrs)
                            ret.Add(cur_child_path.ToString());
                    }
                    else
                    {
                        /* We are not yet at the deepest search path yet -
                        recurse into the next directory if it is one */
                        System.IO.FileAttributes child_attrs = (System.IO.FileAttributes)int_attrs;
                        if ((child_attrs & System.IO.FileAttributes.Directory) != 0)
                        {
                            /* This is a directory - recurse into it */
                            cur_path.Add(child);
                            getFSE(split_path, cur_depth + 1, ret, child_dir, cur_path, attrs, mask);
                            cur_path.RemoveAt(cur_path.Count - 1);
                        }

                        CloseFile(child_dir);
                    }
                }
                else
                {
                    // tysos.Syscalls.DebugFunctions.DebugWrite("match unsuccessful\n");
                }
            }
        }

        private bool match(string ts, string ws, bool caseSensitive)
        {
            /* tysos.Syscalls.DebugFunctions.DebugWrite("match: entered\n");
            tysos.Syscalls.DebugFunctions.DebugWrite("match: matching " + ts + " against " +
                ws); */

            /* Match the tame string 'ts' against the wild string 'ws'

            Algorithm is based upon a Dr. Dobbs article (Matching Wildcards:
            An Algorithm - by Kirk J. Krauss, 26-Aug-2008, available from:
            http://www.drdobbs.com/architecture-and-design/matching-wildcards-an-algorithm/210200888)

            A * in the wild string is matched against 0 or more characters
            */

            int idx_after_last_star = -1;
            int idx_t = 0;
            int idx_w = 0;
            while(true)
            {
                /* Handle the case where we are at the end of the tame string */
                if(idx_t >= ts.Length)
                {
                    /* If we are also at the end of the wild string, we have a
                        match */
                    if (idx_w >= ws.Length)
                    {
//                        tysos.Syscalls.DebugFunctions.DebugWrite("1");
                        return true;
                    }
                    else if (ws[idx_w] == '*')
                    {
                        /* The end of the tame string is also matched by *,
                        as long as nothing further follows it */
//                        tysos.Syscalls.DebugFunctions.DebugWrite("2");
                        idx_w++;
                        continue;
                    }
                    else
                    {
                        /* There are extra characters in ws unaccounted for */
//                        tysos.Syscalls.DebugFunctions.DebugWrite("3");
                        return false;
                    }
                }

                /* We are not at the end of the tame string - get the next
                character in ts and ws (special-case if we are already at
                the end of ws - this is handled later) */
                char t = ts[idx_t];
                char w = '\u0000';
                if (idx_w < ws.Length)
                    w = ws[idx_w];

                /* Convert to lower case if case-insensitive comparison */
                if(caseSensitive == false)
                {
//                    tysos.Syscalls.DebugFunctions.DebugWrite("4");
                    t = Char.ToLower(t);
                    w = Char.ToLower(w);
                }

                /* If the characters match, then continue */
                if(t == w)
                {
//                    tysos.Syscalls.DebugFunctions.DebugWrite("5");
                    idx_t++;
                    idx_w++;
                    continue;
                }

                /* They don't match.  Is the w character a star?  If so,
                store its location and continue parsing the w string (don't
                advance idx_t yet - this handles multiple * characters in a
                row) */
                if(w == '*')
                {
//                    tysos.Syscalls.DebugFunctions.DebugWrite("6");
                    idx_w++;
                    idx_after_last_star = idx_w;
                    continue;
                }

                /* They don't match, and the w is not a star.  If we are
                currently parsing a * then continue parsing it */
                if(idx_after_last_star != -1)
                {
//                    tysos.Syscalls.DebugFunctions.DebugWrite("7");
                    idx_w = idx_after_last_star;

                    if(idx_w >= ws.Length)
                    {
                        /* Handle the case of a * at the end of the string
                        which will match anything that has already parsed
                        this far */
//                        tysos.Syscalls.DebugFunctions.DebugWrite("9");
                        return true;                        
                    }

                    /* Get the next item in the wild string and compare it
                    against t.  If it matches (and we already know it is not
                    a *) then continue parsing the wild and tame strings.
                    If not, treat it as matching the * in the wild string,
                    and only advance the tame string */
                    w = ws[idx_w];
                    if (caseSensitive == false)
                        w = Char.ToLower(w);
                    if (w == t)
                    {
//                        tysos.Syscalls.DebugFunctions.DebugWrite("9");
                        idx_w++;
                    }
//                    tysos.Syscalls.DebugFunctions.DebugWrite("A");
                    idx_t++;
                    continue;
                }

                /* They don't match, and we are not currently parsing a * in
                the wild string, therefore fail */
                return false;
            }
        }
    }
}
