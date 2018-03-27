/* Copyright (C) 2016-2017 by John Cronin
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
using System.Threading.Tasks;
using tymake_lib;

namespace tymake
{
    class TyProject : FunctionStatement
    {
        public TyProject()
        {
            name = "_typroject";
            args = new List<FunctionArg> { new FunctionArg { name = "projfile", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "config", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "tools_ver", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "unsafe", argtype = Expression.EvalResult.ResultType.Int },
                new FunctionArg { name = "imports", argtype = Expression.EvalResult.ResultType.Array },
                new FunctionArg { name = "ref_overrides", argtype = Expression.EvalResult.ResultType.Array },
                new FunctionArg { name = "lib_dir", argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> args)
        {
            string fname = args[0].strval;
            string config = args[1].strval;
            string tools_ver = args[2].strval;
            bool do_unsafe = (args[3].intval == 0) ? false : true;

            string cur_dir = Environment.CurrentDirectory;
            System.IO.FileInfo fi = new System.IO.FileInfo(fname);
            if (!fi.Exists)
            {
                throw new Exception("_typroject: " + fname + " does not exist");
            }

            /* Extract out imports */
            List<string> imports = new List<string>();
            foreach (var import in args[4].arrval)
                imports.Add(import.strval);

            /* Extract out ref overrides */
            List<string> ref_overrides = new List<string>();
            foreach (var ro in args[5].arrval)
                ref_overrides.Add(ro.strval);


            typroject.Project p = null;
            if (fname.ToLower().EndsWith(".csproj"))
                p = typroject.Project.xml_read(fi.OpenRead(), config, fi.DirectoryName, cur_dir, imports, ref_overrides);
            else if (fname.ToLower().EndsWith(".sources"))
                p = typroject.Project.sources_read(fi.OpenRead(), config, fi.DirectoryName, cur_dir);

            if (args[6].strval != "")
                p.lib_dir = args[6].strval;

            Dictionary<string, Expression.EvalResult> ret = new Dictionary<string, Expression.EvalResult>();
            ret["OutputFile"] = new Expression.EvalResult(p.OutputFile);
            ret["OutputType"] = new Expression.EvalResult(p.output_type.ToString());
            ret["AssemblyName"] = new Expression.EvalResult(p.assembly_name);
            ret["Configuration"] = new Expression.EvalResult(p.configuration);
            ret["Defines"] = new Expression.EvalResult(p.defines);
            ret["Sources"] = new Expression.EvalResult(p.Sources);
            ret["Resources"] = new Expression.EvalResult(p.Resources);
            ret["GACReferences"] = new Expression.EvalResult(p.References);
            ret["ProjectReferences"] = new Expression.EvalResult(new string[] { });
            foreach (typroject.Project pref in p.ProjectReferences)
                ret["ProjectReferences"].arrval.Add(new Expression.EvalResult(pref.ProjectFile));
            ret["References"] = new Expression.EvalResult(new string[] { });
            foreach (var rref in p.References)
                ret["References"].arrval.Add(new Expression.EvalResult(rref));
            ret["ProjectFile"] = new Expression.EvalResult(fi.FullName);
            ret["ProjectName"] = new Expression.EvalResult(p.ProjectName);

            BuildFunction build = new BuildFunction(p);
            ret[build.Mangle()] = new Expression.EvalResult(build);

            if (do_unsafe)
                build.do_unsafe = true;
            if (tools_ver != "")
                p.tools_ver = tools_ver;

            return new Expression.EvalResult(ret);
        }

        class BuildFunction : FunctionStatement
        {
            typroject.Project p;
            internal bool do_unsafe = false;

            public BuildFunction(typroject.Project proj)
            {
                name = "Build";
                args = new List<FunctionArg> { new FunctionArg { argtype = Expression.EvalResult.ResultType.Object, name = "this" } };
                p = proj;
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                var o = passed_args[0].objval;
                p.assembly_name = o["AssemblyName"].strval;

                return new Expression.EvalResult(p.build(new List<string>(), new List<string>(), new List<string>(), do_unsafe));
            }
        }
    }

    class TyProjectLibDirFunction : FunctionStatement
    {
        public TyProjectLibDirFunction()
        {
            name = "typroject_refdir";
            args = new List<FunctionArg>
            {
                new FunctionArg {name = "tools_ver", argtype = Expression.EvalResult.ResultType.String }
            };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            return new Expression.EvalResult(typroject.Program.ref_dir(passed_args[0].strval));
        }
    }
}
