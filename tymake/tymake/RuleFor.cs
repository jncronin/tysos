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
        internal class RuleForExists : FunctionStatement
        {
            public RuleForExists()
            {
                name = "rulefor";
                args = new List<FunctionArg>
                {
                    new FunctionArg { name = "output", argtype = Expression.EvalResult.ResultType.String }
                };
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                TyMakeState tms = s as TyMakeState;
                string wc_pat;
                if (tms.GetRule(passed_args[0].strval, out wc_pat) == null)
                    return new Expression.EvalResult(0);
                else
                    return new Expression.EvalResult(1);
            }
        }
        internal class RuleForFunction : FunctionStatement
        {
            public RuleForFunction()
            {
                name = "rulefor";
                args = new List<FunctionArg> {
                new FunctionArg { name = "output", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "inputs", argtype = Expression.EvalResult.ResultType.Array },
                new FunctionArg { name = "depends", argtype = Expression.EvalResult.ResultType.Array },
                new FunctionArg { name = "rule", argtype = Expression.EvalResult.ResultType.Function },
            };
            }

            public Expression output_file;
            public List<Expression.EvalResult> depend_list;
            public List<Expression.EvalResult> inputs_list;
            public List<string> dfiles = null;
            public List<string> ifiles = null;
            public Statement rules;

            MakeState state_at_def;

            public int Build(MakeState s, string tfile, string wc_match)
            {
#if DEBUG
//                System.Console.WriteLine("Considering building " + tfile);
#endif
                List<string> depends = new List<string>();
                List<string> inputs = new List<string>();
                List<string> all_deps = new List<string>();

                /*MakeState cur_s = state_at_def.Clone();
                cur_s.Merge(s);
                s = cur_s;*/

                MakeState cur_s = s.Clone();
                cur_s.Merge(state_at_def);
                s = cur_s;

                if (depend_list != null)
                {
                    /* Build a list of dependencies */
                    //List<string> dfiles = FlattenToString(depend_list, s);
                    foreach (string cur_dfile in dfiles)
                    {
                        string dfile = cur_dfile;
                        int wc_index = -1;
                        for (int i = 0; i < dfile.Length; i++)
                        {
                            if (dfile[i] == '%')
                            {
                                if (i == 0 || dfile[i - 1] != '\\')
                                {
                                    wc_index = i;
                                    break;
                                }
                            }
                        }

                        if (wc_index != -1)
                        {
                            if (wc_match == null)
                                throw new Exception("wildcard specified in depends list but not in target name");

                            dfile = dfile.Substring(0, wc_index) + wc_match + dfile.Substring(wc_index + 1);
                        }

                        depends.Add(dfile);
                        all_deps.Add(dfile);
                    }
                }

                if (inputs_list != null)
                {
                    //List<string> dfiles = FlattenToString(inputs_list, s);
                    foreach (string cur_dfile in ifiles)
                    {
                        string dfile = cur_dfile;

                        int wc_index = -1;
                        for (int i = 0; i < dfile.Length; i++)
                        {
                            if (dfile[i] == '%')
                            {
                                if (i == 0 || dfile[i - 1] != '\\')
                                {
                                    wc_index = i;
                                    break;
                                }
                            }
                        }

                        if (wc_index != -1)
                        {
                            if (wc_match == null)
                                throw new Exception("wildcard specified in inputs list but not in target name");

                            dfile = dfile.Substring(0, wc_index) + wc_match + dfile.Substring(wc_index + 1);
                        }

                        inputs.Add(dfile);
                        all_deps.Add(dfile);
                    }
                }

#if DEBUG
                //System.Console.Write("Dependencies: ");
                //foreach(var dep in all_deps)
                //    System.Console.Write(dep + ", ");
                //System.Console.WriteLine();
#endif

                /* Now ensure all the dependencies are available */
                DateTime most_recent_dependency = new DateTime(0);
                System.IO.FileInfo mrd_fi = null;   // store the most recent dependency for debugging purposes
                foreach (string depend in all_deps)
                {
                    BuildCommandStatement bc = new BuildCommandStatement();
                    int dep_ret = (int)bc.Run(s, new List<Expression.EvalResult> { new Expression.EvalResult(depend) }).AsInt;
                    System.IO.FileInfo dep_fi = null;
                    if (dep_ret == BuildCommandStatement.RUN_NO_RULE)
                    {
                        /* No rule to build the file - its still okay as long as the file already exists */
                        dep_fi = new System.IO.FileInfo(depend);
                        if (!Statement.FileDirExists(depend))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(depend + " does not exist and no rule to build it");
                            Console.ResetColor();
                            return -1;
                        }
                    }
                    else if (dep_ret != 0)
                        return dep_ret;

                    dep_fi = new System.IO.FileInfo(depend);
                    if (!Statement.FileDirExists(depend))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error building " + depend);
                        Console.ResetColor();
                        return -1;
                    }

                    if ((dep_fi.Attributes & System.IO.FileAttributes.Directory) != System.IO.FileAttributes.Directory)
                    {
                        if (dep_fi.LastWriteTime.CompareTo(most_recent_dependency) > 0)
                        {
                            most_recent_dependency = dep_fi.LastWriteTime;
                            mrd_fi = dep_fi;
                        }
                    }
                }

                /* See if we need to build this file */
                bool to_build = false;
                System.IO.FileSystemInfo targ_fi = new System.IO.FileInfo(tfile);
                DateTime targ_lwt = DateTime.Now;
                if(s.GetDefine("REBUILD_ALL").AsInt != 0 && ((targ_fi.Attributes & System.IO.FileAttributes.Directory) != System.IO.FileAttributes.Directory))
                {
                    to_build = true;

                    Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine("Building " + tfile
#if DEBUG
                        + " because REBUILD_ALL is set"
#endif
                        );
                    Console.ResetColor();

                }
                else if (!Statement.FileDirExists(tfile))
                {
                    to_build = true;

                    Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine("Building " + tfile
#if DEBUG
                        + " because it does not exist"
#endif
                        );
                    Console.ResetColor();
                }
                else if (most_recent_dependency.CompareTo(targ_lwt = targ_fi.LastWriteTime) > 0)
                {
                    to_build = true;

                    Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine("Building " + tfile
#if DEBUG
                        + " because of a newer dependency (" + mrd_fi.FullName + ")"
#endif
                        );
                    Console.ResetColor();
                }
                else if (depend_list == null)
                {
                    to_build = true;

                    Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine("Building " + tfile
#if DEBUG
                        + " because dependency list is null"
#endif
                        );
                    Console.ResetColor();
                }

                if (to_build)
                {
                    MakeState new_s = s.Clone();

                    if (inputs.Count > 0)
                        new_s.SetDefine("_RULE_INPUT", new Expression.EvalResult(inputs[0]));
                    StringBuilder inputs_str = new StringBuilder();
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        if (i != 0)
                            inputs_str.Append(" ");
                        inputs_str.Append(inputs[i]);
                    }
                    StringBuilder deps_str = new StringBuilder();
                    for (int i = 0; i < depends.Count; i++)
                    {
                        if (i != 0)
                            deps_str.Append(" ");
                        deps_str.Append(depends[i]);
                    }
                    new_s.SetDefine("_RULE_INPUTS", new Expression.EvalResult(inputs_str.ToString()));
                    new_s.SetDefine("_RULE_DEPENDS", new Expression.EvalResult(deps_str.ToString()));
                    new_s.SetDefine("_RULE_OUTPUT", new Expression.EvalResult(tfile));
                    var ret = (int)rules.Execute(new_s).AsInt;

                    /* See if the target failed to build despite success from the rules */
                    if (ret == 0)
                    {
                        targ_fi = new System.IO.FileInfo(tfile);
                        if ((targ_fi.Attributes & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
                            targ_fi = new System.IO.DirectoryInfo(tfile);
                        if (targ_fi.Exists == false || targ_fi.LastWriteTime.CompareTo(targ_lwt) < 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Rule failed to build " + tfile);
                            Console.ResetColor();
                            return -1;
                        }
                    }

                    return ret;
                }
                else
                {
#if DEBUG
//                    System.Console.WriteLine("Not building " + tfile);
#endif
                }

                return 0;
            }

            private List<string> FlattenToString(List<Expression.EvalResult> depend_list, MakeState s)
            {
                List<string> ret = new List<string>();
                foreach (var e in depend_list)
                {
                    FlattenToString(e, ret, s);
                }

                return ret;
            }

            private void FlattenToString(Expression.EvalResult er, List<string> ret, MakeState s)
            {
                switch (er.Type)
                {
                    case Expression.EvalResult.ResultType.String:
                        ret.Add(er.strval);
                        break;
                    case Expression.EvalResult.ResultType.Array:
                        foreach (Expression.EvalResult ea in er.arrval)
                            FlattenToString(ea, ret, s);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                // On run, clone the rule as all its member variables are shared by all instances
                RuleForFunction new_mrs = new RuleForFunction { output_file = output_file, depend_list = depend_list, inputs_list = inputs_list, rules = rules, export = export };

                new_mrs.output_file = passed_args[0];
                new_mrs.inputs_list = passed_args[1].arrval;
                new_mrs.depend_list = passed_args[2].arrval;
                new_mrs.rules = passed_args[3].funcval.code;

                string ofile = new_mrs.output_file.Evaluate(s).strval;
                new_mrs.state_at_def = s.Clone();
                foreach(var dep in new_mrs.depend_list)
                {
                    if (dep.Type == Expression.EvalResult.ResultType.Undefined)
                        throw new SyntaxException("rule for " + new_mrs.output_file.ToString() + " depends on " + dep.strval + " which is undefined", dep.orig_expr);
                }
                foreach (var dep in new_mrs.inputs_list)
                {
                    if (dep.Type == Expression.EvalResult.ResultType.Undefined)
                        throw new SyntaxException("rule for " + new_mrs.output_file.ToString() + " requires " + dep.strval + " which is undefined", dep.orig_expr);
                }
                new_mrs.dfiles = FlattenToString(new_mrs.depend_list, s);
                new_mrs.ifiles = FlattenToString(new_mrs.inputs_list, s);

                ((TyMakeState)s).AddRule(ofile, new_mrs);
                return new Expression.EvalResult(0);
            }
        }
    }
}
