/* Copyright (c) 2008, John Cronin
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the copyright holder nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. */

using System;
using System.IO;
using System.Collections.Generic;

namespace tybuild
{
    class tybuild
    {
        public static string dir_split = "\\";
        public static int platform;
        static bool dump_options = false;

        static string tools_ver_override = null;

        public static string csc(string tools_ver)
        {
            if (tools_ver_override != null)
                tools_ver = tools_ver_override;

            if (platform == 0)
            {
                // assume mono for unix
                if ((tools_ver == "2.0") || (tools_ver == "3.0") || (tools_ver == "3.5") || (tools_ver == "4.0"))
                    return "gmcs";
                else
                    return "mcs";
            }
            else
            {
                // assume csc for windows

                string windir = Environment.GetEnvironmentVariable("windir");
                string framework_dir = windir + "\\Microsoft.NET\\Framework";
                DirectoryInfo fdi = new DirectoryInfo(framework_dir);
                DirectoryInfo[] matches = fdi.GetDirectories("v" + tools_ver + "*");

                foreach (DirectoryInfo match in matches)
                {
                    FileInfo[] file_matches = match.GetFiles("csc.exe");
                    if (file_matches.Length == 1)
                        return file_matches[0].FullName;
                }

                // fallback
                return "csc.exe";
            }
        }
        public static string ref_dir(string tools_ver)
        {
            if (tools_ver_override != null)
                tools_ver = tools_ver_override;

            if (platform == 0)
                return "";
            else
            {
                string fwork_ver;
                if ((tools_ver == "2.0") || (tools_ver == "3.0") || (tools_ver == "3.5") || (tools_ver == "4.0"))
                    fwork_ver = "2.0";
                else
                    fwork_ver = tools_ver;

                string windir = Environment.GetEnvironmentVariable("windir");
                string framework_dir = windir + "\\Microsoft.NET\\Framework";
                DirectoryInfo fdi = new DirectoryInfo(framework_dir);
                DirectoryInfo[] matches = fdi.GetDirectories("v" + fwork_ver + "*");

                foreach (DirectoryInfo match in matches)
                {
                    FileInfo[] file_matches = match.GetFiles("System.dll");
                    if (file_matches.Length == 1)
                        return file_matches[0].DirectoryName;
                }

                // fallback
                return framework_dir + "\\v2.0.50727\\";
            }
        }

        public static int Main(string [] args)
        {
            identify_environment();
            string fname = null;
            string config = null;
			int verbosity = 1;
            bool use_temp = false;
            bool _unsafe = false;
            bool _debug = false;
            List<string> extra_add = new List<string>();
            List<string> extra_defines = new List<string>();

	        List<string> project_names = null;

#if !DEBUG
			try {
#endif
                foreach (string arg in args)
                {
                    if (arg.StartsWith("/p:Configuration="))
                        config = arg.Substring("/p:Configuration=".Length);
                    else if ((arg == "/v") || (arg == "/verbose"))
                        verbosity = 2;
                    else if (arg == "/debug")
                        _debug = true;
                    else if ((arg == "/q") || (arg == "/quiet"))
                        verbosity = 0;
                    else if (arg.StartsWith("/dump:"))
                    {
                        if (!dump(arg.Substring("/dump:".Length)))
                            return 0;
                    }
                    else if(arg.StartsWith("/define:"))
                        extra_defines.Add(arg.Substring("/define:".Length));
                    else if (arg.StartsWith("/project:"))
                    {
                        if (project_names == null)
                            project_names = new List<string>();
                        project_names.Add(arg.Substring("/project:".Length));
                    }
                    else if (arg.StartsWith("/d:"))
                    {
                        if (!dump(arg.Substring("/d:".Length)))
                            return 0;
                    }
                    else if ((arg == "/t") || (arg == "/temp"))
                        use_temp = true;
                    else if (arg == "/unsafe")
                        _unsafe = true;
                    else if (arg == "/tools:2")
                        tools_ver_override = "2.0";
                    else if (arg == "/tools:3")
                        tools_ver_override = "3.0";
                    else if (arg == "/tools:3_5")
                        tools_ver_override = "3.5";
                    else if (arg == "/tools:4")
                        tools_ver_override = "4.0";
                    else if (arg.StartsWith("/Wc,"))
                    {
                        string[] extra_args = arg.Split(',');
                        for (int i = 1; i < extra_args.Length; i++)
                            extra_add.Add(extra_args[i]);
                    }
                    else
                        fname = arg;
                }

				if (fname == null)
				{
					/* attempt to guess */
					DirectoryInfo cdir = new DirectoryInfo(Environment.CurrentDirectory);

					FileInfo[] slns = cdir.GetFiles("*.sln");
					if (slns.Length > 1)
						throw new Exception("No file specified and more than one solution in current directory");
					if (slns.Length == 0)
					{
						FileInfo[] projs = cdir.GetFiles("*.csproj");
						if (projs.Length > 1)
							throw new Exception("No file specified and more than one project in current directory");
                        if (projs.Length == 0)
                        {
                            FileInfo[] sources = cdir.GetFiles("*.sources");
                            if (sources.Length > 1)
                                throw new Exception("No file specified and more than one .sources file in " +
                                    "current directory");
                            if (sources.Length == 0)
                                throw new Exception("No files specified");
                            else
                                fname = sources[0].FullName;
                        }
                        else
						    fname = projs[0].FullName;
					}
					else
						fname = slns[0].FullName;
				}
				
				Solution s;
				if (fname.EndsWith(".csproj") || fname.EndsWith(".sources"))
				{
					s = new Solution(new Project(new FileStream(fname, FileMode.Open, FileAccess.Read), config,
						GetBaseDir(fname)), config);

				}
                else if (fname.EndsWith(".cs"))
                {
                    s = new Solution(new Project(new string[] { fname }, fname.Substring(0, fname.Length - 3) + ".exe"), config);
                }
                else
                {
                    s = new Solution(new FileStream(fname, FileMode.Open,
                        FileAccess.Read), config, GetBaseDir(fname));
                }

                foreach (KeyValuePair<Guid, ProjectId> kvp in s.Projects)
                {
                    if (kvp.Value.Project == null)
                        Console.WriteLine("Warning: unable to load project: " + kvp.Value.ProjectFile + " : " + kvp.Value.ErrorMessage);
                }

                foreach (KeyValuePair<Guid, ProjectId> kvp in s.Projects)
                {
                    Project p = kvp.Value.Project;
                    if (p == null)
                        continue;
                    if (_unsafe)
                        p.extra_add += " /unsafe";
                    if (_debug)
                        p.extra_add += " /debug";
                    foreach (string extra_define in extra_defines)
                        p.extra_add += " /define:" + extra_define;
                    foreach (string extra_add_item in extra_add)
                        p.extra_add += " /" + extra_add_item;
                }

                int ret;
                if(dump_options)
                    ret = s.Build(3, true, use_temp, project_names);
				else
                    ret = s.Build(verbosity, false, use_temp, project_names);
				return ret;
#if !DEBUG
			} catch (Exception e) {
				Console.WriteLine("Error: " + e.Message);
				return -1;
			}
#endif
        }

        private static void identify_environment()
        {
            /* Attempt to detect between windows and unix */
            if ((Environment.OSVersion.Platform == PlatformID.Unix) ||
                ((int)Environment.OSVersion.Platform == 128))
                platform = 0;
            else if (Environment.OSVersion.Platform == PlatformID.WinCE)
                platform = 2;
            else
                platform = 1;

            if (platform == 2)
                throw new Exception("Not supported on wince");

            if (platform == 0)
                dir_split = "/";
            if (platform == 1)
                dir_split = "\\";            
        }

		public static bool dump(string dump_opts)
		{
			string tools_ver = "2.0";
			string dump_cmd = null;
			if(dump_opts.Contains(":")) {
				dump_cmd = dump_opts.Substring(0, dump_opts.IndexOf(":"));
				tools_ver = dump_opts.Substring(dump_opts.IndexOf(":") + 1);
			} else
				dump_cmd = dump_opts;

			if(dump_cmd == "csc")
				Console.Write(csc(tools_ver));
			else if(dump_cmd == "ref_dir")
				Console.Write(ref_dir(tools_ver));
			else if(dump_cmd == "dir_split")
				Console.Write(dir_split);
            else if(dump_cmd == "opts") {
                dump_options = true;
                return true;
            }

            return false;
		}

        public static string GetBaseDir(string dir)
        {
            if (!dir.Contains(dir_split))
                return "." + dir_split;
            return dir.Substring(0, dir.LastIndexOf(dir_split) + 1);
        }

        public static string AppendDirSplit(string dir)
        {
            if (dir == null)
                return "";
            if (dir == "")
                return dir;
            if (dir.EndsWith(dir_split))
                return dir;
            return dir + dir_split;
        }

        public static string ReplaceDirSplit(string path)
        { return ReplaceDirSplit(path, false); }
		public static string ReplaceDirSplit(string path, bool path_contains_unix_splits)
		{
            if(path_contains_unix_splits)
                return path.Replace("/", dir_split);

			if(dir_split == "\\")
				return path;

			return path.Replace("\\", dir_split);
		}
    }
}
