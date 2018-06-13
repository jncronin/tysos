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
        internal class BuildCommandStatement : FunctionStatement
        {
            public BuildCommandStatement()
            {
                name = "build";
                args = new List<FunctionArg> { new FunctionArg { name = "target", argtype = Expression.EvalResult.ResultType.String } };
            }

            public const int RUN_NO_RULE = 0x8000000;
            private int make_compare(TyMakeState.MakeRuleMatch a, TyMakeState.MakeRuleMatch b)
            {
                return a.wc_len - b.wc_len;
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                string target = passed_args[0].strval;
                Uri furi = new Uri(new Uri(Environment.CurrentDirectory + "/"), target);
                target = furi.AbsolutePath;
                List<TyMakeState.MakeRuleMatch> matches = ((TyMakeState)s).GetRules(target);
                matches.Sort(make_compare);

                foreach (TyMakeState.MakeRuleMatch match in matches)
                {
                    if (match.mr == null)
                        return new Expression.EvalResult(0);
                    int ret = match.mr.Build(s, target, match.wc_pattern);
                    if (ret != 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error building " + target + " (" + ret.ToString() + ")");
                        Console.ResetColor();
                        return new Expression.EvalResult(ret);
                    }
                    if (ret == 0)
                        return new Expression.EvalResult(0);
                }

                ((TyMakeState)s).GetRules(target);
                return new Expression.EvalResult(RUN_NO_RULE);
            }
        }
    }
}
