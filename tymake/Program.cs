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
            new TyProjectLibDirFunction().Execute(s);
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
                {
                    var ret = TymakeLib.ExecuteFile(arg, s);
                    if (ret.AsInt != 0)
                        System.Diagnostics.Debugger.Break();
                }
                else
                {
                    var ret = TymakeLib.ExecuteString(arg, s);
                    if (ret.AsInt != 0)
                        System.Diagnostics.Debugger.Break();
                }
            }


            if (immediate)
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
}
