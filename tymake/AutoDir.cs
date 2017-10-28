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
        internal class AutoDirStatement : FunctionStatement
        {
            public AutoDirStatement()
            {
                name = "autodir";
                args = new List<FunctionArg> { new FunctionArg { name = "dirname", argtype = Expression.EvalResult.ResultType.String } };
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(passed_args[0].strval);

                while (di != null)
                {
                    List<Expression.EvalResult> depend_list = new List<Expression.EvalResult>();
                    if (di.Parent != null)
                        depend_list.Add(new Expression.EvalResult(di.Parent.FullName));

                    RuleForFunction mr = new RuleForFunction();
                    mr.Run(s, new List<Expression.EvalResult>
                    {
                        new Expression.EvalResult(di.FullName),
                        new Expression.EvalResult(new List<Expression.EvalResult>()),
                        new Expression.EvalResult(depend_list),
                        new Expression.EvalResult(
                            new FunctionStatement
                            {
                                code = new ExpressionStatement
                                {
                                    expr = new FuncCall
                                    {
                                        target = "mkdir",
                                        args = new List<Expression> { new StringExpression { val = di.FullName } }
                                    }
                                },
                            })
                    });
                    di = di.Parent;
                }

                return new Expression.EvalResult(0);
            }
        }
    }
}
