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
    partial class Program
    {
        static void Main(string[] args)
        {
            var s = new TyMakeState();
            TymakeLib.InitMakeState(s, Console.In,
                Console.Out, Console.Out);
            new TyProject().Execute(s);
            new RuleForFunction().Execute(s);
            new BuildCommandStatement().Execute(s);
            new AutoDirStatement().Execute(s);
            new ShellCommandFunction().Execute(s);

            /* Add in current environment variables */
            System.Collections.IDictionary env_vars = Environment.GetEnvironmentVariables();
            foreach (System.Collections.DictionaryEntry env_var in env_vars)
                s.SetDefine(env_var.Key.ToString().ToUpper(), new Expression.EvalResult(env_var.Value.ToString()));

            if(System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                s.SetDefine("EXEC_EXTENSIONS", new Expression.EvalResult(new Expression.EvalResult[] { new Expression.EvalResult("") }));
                s.SetDefine("PATH_SPLIT", new Expression.EvalResult(":"));
                s.SetDefine("DIR_SPLIT", new Expression.EvalResult("/"));
            }
            else
            {
                s.SetDefine("EXEC_EXTENSIONS", new Expression.EvalResult(new Expression.EvalResult[] { new Expression.EvalResult(".exe") }));
                s.SetDefine("PATH_SPLIT", new Expression.EvalResult(";"));
                s.SetDefine("DIR_SPLIT", new Expression.EvalResult("\\"));
            }

            /* Include the standard library */
            System.IO.FileInfo exec_fi = new System.IO.FileInfo(typeof(Program).Module.FullyQualifiedName);
            System.IO.FileInfo[] stdlib_fis = exec_fi.Directory.GetFiles("*.tmh");
            foreach(System.IO.FileInfo stdlib_fi in stdlib_fis)
                TymakeLib.ExecuteFile(stdlib_fi.FullName, s);

            /* Determine what to run - either interpret the arguments as files or as commands */
            bool immediate = false;
            foreach(string arg in args)
            {
                if (arg == "-")
                    immediate = true;
                else if (Statement.FileDirExists(arg))
                    TymakeLib.ExecuteFile(arg, s);
                else
                    TymakeLib.ExecuteString(arg, s);
            }

            if(immediate)
            {
                while(true)
                {
                    Console.Write("> ");
                    try
                    {
                        TymakeLib.ExecuteString(Console.ReadLine(), s);
                    }
                    catch (ParseException e)
                    {
                        Console.WriteLine();
                        Console.WriteLine(e.Message);
                    }
                    catch(Statement.SyntaxException e)
                    {
                        Console.WriteLine();
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
    }

    class TyProject : FunctionStatement
    {
        public TyProject()
        {
            name = "_typroject";
            args = new List<FunctionArg> { new FunctionArg { name = "projfile", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "config", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "tools_ver", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "unsafe", argtype = Expression.EvalResult.ResultType.Int } };
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

            typroject.Project p = null;
            if (fname.ToLower().EndsWith(".csproj"))
                p = typroject.Project.xml_read(fi.OpenRead(), config, fi.DirectoryName, cur_dir);
            else if (fname.ToLower().EndsWith(".sources"))
                p = typroject.Project.sources_read(fi.OpenRead(), config, fi.DirectoryName, cur_dir);

            Dictionary<string, Expression.EvalResult> ret = new Dictionary<string, Expression.EvalResult>();
            ret["OutputFile"] = new Expression.EvalResult(p.OutputFile);
            ret["OutputType"] = new Expression.EvalResult(p.output_type.ToString());
            ret["AssemblyName"] = new Expression.EvalResult(p.assembly_name);
            ret["Configuration"] = new Expression.EvalResult(p.configuration);
            ret["Defines"] = new Expression.EvalResult(p.defines);
            ret["Sources"] = new Expression.EvalResult(p.Sources);
            ret["GACReferences"] = new Expression.EvalResult(p.References);
            ret["ProjectReferences"] = new Expression.EvalResult(new string[] { });
            foreach (typroject.Project pref in p.ProjectReferences)
                ret["ProjectReferences"].arrval.Add(new Expression.EvalResult(pref.ProjectFile));
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
                return new Expression.EvalResult(p.build(new List<string>(), new List<string>(), new List<string>(), do_unsafe));
            }
        }
    }

    class DirectoryFunction : FunctionStatement
    {
        public DirectoryFunction()
        {
            args = new List<FunctionArg> { new FunctionArg { name = "fname", argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            if (name == "autodir")
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(passed_args[0].strval);

                while (di != null)
                {
                    List<Expression> depend_list = new List<Expression>();
                    if (di.Parent != null)
                        depend_list.Add(new StringExpression { val = di.Parent.FullName });

                    throw new NotImplementedException();
                    /*MakeRuleStatement mr = new MakeRuleStatement
                    {
                        output_file = new StringExpression { val = di.FullName },
                        rules = new tymake.MkDirCommandStatement { dir = new StringExpression { val = di.FullName } },
                        export = true,
                        depend_list = depend_list,
                        inputs_list = new List<Expression>()
                    };
                    mr.Execute(s);*/
                    di = di.Parent;
                }

                return new Expression.EvalResult(0);
            }

            System.IO.FileInfo fi = new System.IO.FileInfo(passed_args[0].strval);
            if (name == "dir")
                return new Expression.EvalResult(fi.DirectoryName);
            else if (name == "basefname")
            {
                string fname = fi.Name;
                if (fname.Contains("."))
                    fname = fname.Substring(0, fname.LastIndexOf('.'));

                return new Expression.EvalResult(fname);
            }
            else if (name == "ext")
            {
                return new Expression.EvalResult(fi.Extension);
            }
            throw new Exception("Unsupported function");
        }
    }
}
