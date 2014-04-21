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
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace tybuild
{
    class Solution
    {
        public Dictionary<Guid, ProjectId> Projects = new Dictionary<Guid, ProjectId>();
        string base_dir;
        string _config;

        public Solution(Stream s, string config, string basedir)
        {
            base_dir = basedir;
            _config = config;
            StreamReader r = new StreamReader(s);

            while (r.EndOfStream == false)
            {
                string line = r.ReadLine();

                // Lines appear to be in the format: 'Project("{some GUID}") = "name", "project_file", "proj guid"

                if (line.StartsWith("Project("))
                {
                    ProjectId p = new ProjectId();

                    string[] split_line = line.Split('\"');
                    p.ProjectName = split_line[3];
                    p.ProjectFile = tybuild.AppendDirSplit(base_dir) + 
						tybuild.ReplaceDirSplit(split_line[5]);
                    p.Guid = new Guid(split_line[7]);
                    Projects.Add(p.Guid, p);
                }
            }
            r.Close();

            /* Now load up the projects themselves */
	    load_projects(config);
        }

        void load_projects(string config)
        {
            foreach (ProjectId pid in Projects.Values)
            {
                if (pid.Project == null)
                {
                    try
                    {
                        pid.Project = new Project(new FileStream(pid.ProjectFile, FileMode.Open, FileAccess.Read),
                                config, pid.ProjectBaseDir);
                    }
                    catch (Exception e)
                    {
                        pid.ErrorMessage = e.Message;
                    }
                }
            }
        }

        public Solution(Project proj, string config)
        {
            base_dir = proj.BaseDir;

            ProjectId prid = new ProjectId("", proj.AssemblyName, proj.Guid);
            prid.Project = proj;
            Projects.Add(proj.Guid, prid);

            foreach (ProjectId pref in proj.ProjectReferences)
                Projects.Add(pref.Guid, pref);

            /* Now load up the projects themselves */
			load_projects(config);
        }

        public int Build(int disp_cmd, bool dry_run, bool use_temp, List<string> project_name)
        {
            /* We loop through the list of projects, resolving dependencies on the way */
            foreach (ProjectId pid in Projects.Values)
            {
				if((project_name != null) && (!project_name.Contains(pid.ProjectName)))
					continue;

                if (pid.Project == null)
                {
                    Console.WriteLine("Skipping project " + pid.ProjectName);
                    continue;
                }
		    
                int ret = _DoBuild(pid, disp_cmd, dry_run, use_temp);
                if (ret != 0)
                    return ret;
            }
            return 0;
        }

        private int _DoBuild(ProjectId pid, int disp_cmd, bool dry_run, bool use_temp)
        {
            /* This is where the actual work goes on */

			/* Throw an error if we couldn't load the project */
			if (pid.ErrorMessage != null)
				throw new Exception(pid.ErrorMessage);

			Project p = pid.Project;

            if (disp_cmd >= 4)
                System.Console.Write(p.AssemblyName + ": ");

            /* Don't build if already built! */
            if (p.IsBuilt)
            {
                if (disp_cmd >= 4)
                    System.Console.WriteLine(" already built");
                return 0;
            }
            else if (disp_cmd >= 4)
                System.Console.WriteLine(" building");

            /* Create the output directory if necessary */
            DirectoryInfo odir = new DirectoryInfo(p.OutputDir);
            if (!odir.Exists)
                odir.Create();

            /* Resolve any project references */
            foreach (ProjectId pref in p.ProjectReferences)
            {
                ProjectId proj_ref = null;
                bool build = false;

                if (!Projects.ContainsKey(pref.Guid))
                {
                    // Attempt to load the project
                    Project new_proj = new Project(new FileStream(Path.Combine(p.BaseDir, pref.ProjectFile), FileMode.Open, FileAccess.Read), _config, Path.Combine(p.BaseDir, pref.ProjectBaseDir));

                    if (!pref.Guid.Equals(new_proj.Guid))
                    {
                        throw new Exception("Error loading referenced project: " + pref.ProjectName + " - GUIDs of project reference in solution and of " +
                            "that in referenced project file do not match");
                    }

                    proj_ref = pref;
                    proj_ref.Project = new_proj;
                    build = true;
                }
                else
                {
                    proj_ref = Projects[pref.Guid];

                    if ((Projects[pref.Guid].Project != null) && (Projects[pref.Guid].Project.IsBuilt == false))
                        build = true;
                }

                if (build)
                {
                    int ret = _DoBuild(proj_ref, disp_cmd, dry_run, use_temp);
                    if (ret != 0)
                        return ret;

                    if (proj_ref.Project.IsBuilt == false)
                        return -2;
                }

                if (proj_ref.Project != null)
                {
                    pref.OutputFile = proj_ref.Project.OutputFile;

                    /* Copy the referenced assemblies to the output directory */
                    FileInfo pref_file = new FileInfo(pref.OutputFile);
                    pref_file.CopyTo(tybuild.AppendDirSplit(p.OutputDir) + proj_ref.Project.AssemblyName,
                        true);
                }
            }

            /* Now actually run the compilation */
            if ((disp_cmd == 2) || (disp_cmd >= 4))
            {
                System.Console.WriteLine(tybuild.csc(p.ToolsVer) + " " + p.CommandLineArgs);
                System.Console.WriteLine();
            }
            else if (disp_cmd == 1)
            {
                System.Console.WriteLine(p.AssemblyName);
                System.Console.WriteLine();
            }
            else if (disp_cmd == 3)
            {
                System.Console.WriteLine(p.CommandLineArgs);
            }

            if (!dry_run)
            {
                System.Diagnostics.Process c_proc = new System.Diagnostics.Process();
                c_proc.EnableRaisingEvents = false;
                c_proc.StartInfo.FileName = tybuild.csc(p.ToolsVer);

                if (use_temp)
                {
                    // Write command line args to temporary file than use that as input to csc
                    FileStream tmp = new FileStream("tybuild.tmp", FileMode.Create, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(tmp);
                    sw.WriteLine(p.SourceList);
                    sw.Close();
                    c_proc.StartInfo.Arguments = p.CommandLineArgsWithoutSources + " @tybuild.tmp";
                } else 
                    c_proc.StartInfo.Arguments = p.CommandLineArgs;

                c_proc.StartInfo.CreateNoWindow = true;
                c_proc.StartInfo.UseShellExecute = false;
                c_proc.StartInfo.RedirectStandardOutput = true;
                c_proc.Start();
                Console.WriteLine(c_proc.StandardOutput.ReadToEnd());
                c_proc.WaitForExit();

                if (use_temp)
                {
                    FileInfo fi = new FileInfo("tybuild.tmp");
                    if (fi.Exists)
                        fi.Delete();
                }

                if (c_proc.ExitCode != 0)
                {
                    if (disp_cmd >= 4)
                        System.Console.WriteLine("Building returned " + c_proc.ExitCode.ToString());
                    return c_proc.ExitCode;
                }
            }

            p.IsBuilt = true;
            return 0;
        }
    }
}
