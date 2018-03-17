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
using System.IO;
using System.Text;

namespace tymake_lib
{
    public class MakeState
    {
        public TextReader stdin;
        public TextWriter stdout, stderr;

        public LocationBase fcall;

        public MakeState parent = null;
        public Expression.EvalResult returns = null;
        public List<string> search_paths = new List<string> { ".", "" };

        protected Dictionary<string, Expression.EvalResult> defs = new Dictionary<string, Expression.EvalResult>();
        protected Dictionary<string, Expression.EvalResult> local_defs = new Dictionary<string, Expression.EvalResult>();
        public Dictionary<string, FunctionStatement> funcs = new Dictionary<string, FunctionStatement>();

        public MakeState()
        {
            // Add a few default defines
            SetDefine("OSVER", new Expression.EvalResult(Environment.OSVersion.Platform.ToString()));
        }

        public bool IsDefined(string tag) {
            if (local_defs.ContainsKey(tag) == true)
                return true;
            if (defs.ContainsKey(tag) == true)
                return true;
            return VarGenFunction.all_defs.ContainsKey(tag);
        }

        public void PromoteLocalDefines()
        {
            foreach (var kvp in local_defs)
                defs[kvp.Key] = kvp.Value;
        }

        public void ClearLocalDefines()
        {
            local_defs = new Dictionary<string, Expression.EvalResult>();
        }

        public void SetDefine(string tag, Expression.EvalResult e) { defs[tag] = e; }
        public void SetLocalDefine(string tag, Expression.EvalResult e) { local_defs[tag] = e; }
        public void SetDefine(string tag, Expression.EvalResult e, bool export)
        {
            if (export == false)
                SetLocalDefine(tag, e);
            else
            {
                MakeState s = this;
                while(s != null)
                {
                    s.SetDefine(tag, e);
                    s = s.parent;
                }
            }
        }

        public Expression.EvalResult GetDefine(string tag) {
            if (local_defs.ContainsKey(tag))
                return local_defs[tag];
            if (defs.ContainsKey(tag))
                return defs[tag];
            else if (VarGenFunction.all_defs.ContainsKey(tag))
                return VarGenFunction.all_defs[tag];
            else
                return new Expression.EvalResult();
        }

        public void SetDefine(string tag, Expression.EvalResult e, Tokens assignop)
        {
            switch (assignop)
            {
                case Tokens.ASSIGN:
                    local_defs[tag] = e;
                    break;

                case Tokens.ASSIGNIF:
                    if (!defs.ContainsKey(tag) && !local_defs.ContainsKey(tag))
                        local_defs[tag] = e;
                    break;

                case Tokens.APPEND:
                    if (local_defs.ContainsKey(tag))
                    {
                        Expression.EvalResult src = local_defs[tag];
                        Expression append = new Expression { a = src, b = e, op = Tokens.PLUS };
                        local_defs[tag] = append.Evaluate(this);
                    }
                    else if(defs.ContainsKey(tag))
                    {
                        Expression.EvalResult src = defs[tag];
                        Expression append = new Expression { a = src, b = e, op = Tokens.PLUS };
                        local_defs[tag] = append.Evaluate(this);
                    }
                    else
                        defs[tag] = e;
                    break;
            }
        }

        protected void CloneTo(MakeState other)
        {
            foreach (KeyValuePair<string, Expression.EvalResult> kvp in defs)
                other.defs[kvp.Key] = kvp.Value;

            foreach (KeyValuePair<string, Expression.EvalResult> kvp in local_defs)
                other.local_defs[kvp.Key] = kvp.Value;

            foreach (KeyValuePair<string, FunctionStatement> kvp in funcs)
                other.funcs[kvp.Key] = kvp.Value;

            other.parent = this;
            other.search_paths = new List<string>(search_paths);

            other.stdin = stdin;
            other.stdout = stdout;
            other.stderr = stderr;
        }

        public virtual MakeState Clone()
        {
            MakeState other = new MakeState();

            CloneTo(other);

            return other;
        }

        public void Merge(MakeState other)
        {
            foreach (KeyValuePair<string, Expression.EvalResult> kvp in other.defs)
                this.defs[kvp.Key] = kvp.Value;
            foreach (KeyValuePair<string, Expression.EvalResult> kvp in other.local_defs)
                this.local_defs[kvp.Key] = kvp.Value;
            foreach (KeyValuePair<string, FunctionStatement> kvp in other.funcs)
                this.funcs[kvp.Key] = kvp.Value;
        }
    }
}
