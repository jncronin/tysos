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
        internal class ShellCommandFunction : FunctionStatement
        {
            public ShellCommandFunction()
            {
                name = "shellcmd";
                args = new List<FunctionArg> { new FunctionArg { name = "cmd", argtype = Expression.EvalResult.ResultType.String } };
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                var cmd = passed_args[0].strval;
                int first_space = cmd.IndexOf(' ');
                string fname, args;
                if (first_space == -1)
                {
                    fname = cmd;
                    args = "";
                }
                else
                {
                    fname = cmd.Substring(0, first_space);
                    args = cmd.Substring(first_space + 1);
                }

                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = fname;
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = false;
                //p.StartInfo.RedirectStandardError = true;
                //p.StartInfo.RedirectStandardOutput = true;

                /* Find the name of the path environment variable */
                /*string path = "PATH";
                foreach (string env_key in p.StartInfo.EnvironmentVariables.Keys)
                {
                    if (env_key.ToLower() == "path")
                        path = env_key;
                }
                string cur_path = "";
                if (p.StartInfo.EnvironmentVariables.ContainsKey(path))
                    cur_path = p.StartInfo.EnvironmentVariables[path];
                if (cur_path != "")
                    cur_path += ";";
                cur_path += "f:/cygwin64/bin";
                p.StartInfo.EnvironmentVariables[path] = cur_path;*/

                Environment.SetEnvironmentVariable("PATH", s.GetDefine("PATH").strval);

                Console.WriteLine("shellcmd: " + p.StartInfo.FileName + " " + p.StartInfo.Arguments);

                try
                {
                    if (p.Start() == false)
                        throw new Exception("unable to execute " + fname);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("error: " + e.ToString());
                    return new Expression.EvalResult(-1);
                }

                p.WaitForExit();

                if(p.ExitCode != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Shell command returned error " + p.ExitCode.ToString() + " (" + p.ExitCode.ToString("X8") + ")");
                    Console.ResetColor();
                    s.returns = new Expression.EvalResult(-1);
                    return new Expression.EvalResult(-1);
                }
                else
                    return new Expression.EvalResult(0);
            }
        }
    }
}
