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
    class TyMakeState : MakeState
    {
        Dictionary<string, Program.RuleForFunction> rules = new Dictionary<string, Program.RuleForFunction>();
        List<string> wildcards = new List<string>();

        class Wildcard
        {
            public Program.RuleForFunction mr;
            public string target;
        }

        List<Wildcard> Wildcards = new List<Wildcard>();

        public void AddRule(string target, Program.RuleForFunction rule)
        {
            /* Identify wildcards as those containing unescaped % characters */
            bool wc = false;
            for (int i = 0; i < target.Length; i++)
            {
                if (target[i] == '%')
                {
                    if (i == 0 || target[i - 1] != '\\')
                    {
                        wc = true;
                        break;
                    }
                }
            }


            if (wc)
            {
                wildcards.Add(target);
                Wildcards.Add(new Wildcard { mr = rule, target = target });
            }
            else
            {
                target = new Uri(new Uri(Environment.CurrentDirectory + "/"), target).AbsolutePath;
                rules[target] = rule;
            }

            rule.export = true;

            if (rule.export)
            {
                TyMakeState cur_s = parent as TyMakeState;
                while (cur_s != null)
                {
                    if (wc)
                    {
                        cur_s.wildcards.Add(target);
                        cur_s.Wildcards.Add(new Wildcard { mr = rule, target = target });
                    }
                    else
                        cur_s.rules[target] = rule;
                    cur_s = cur_s.parent as TyMakeState;
                }
            }
        }

        public class MakeRuleMatch
        {
            public Program.RuleForFunction mr;
            public string wc_pattern;
            public int wc_len;
        }

        public List<MakeRuleMatch> GetRules(string fname)
        {
            List<MakeRuleMatch> ret = new List<MakeRuleMatch>();

            if (rules.ContainsKey(fname))
            {
                ret.Add(new MakeRuleMatch { mr = rules[fname], wc_len = 0, wc_pattern = null });
            }
            else
            {
                /* Now match all possible wildcard characters */
                foreach (Wildcard wc_test in Wildcards)
                {
                    /* Find the wildcard character */
                    int wc_char_idx = -1;
                    for (int i = 0; i < wc_test.target.Length; i++)
                    {
                        if (wc_test.target[i] == '%')
                        {
                            if (i == 0 || wc_test.target[i - 1] != '\\')
                            {
                                wc_char_idx = i;
                                break;
                            }
                        }
                    }

                    if (wc_char_idx == -1)
                        throw new Exception("no wildcard characted found in " + wc_test);

                    string start_match = null;
                    string end_match = null;

                    if (wc_char_idx != 0)
                        start_match = wc_test.target.Substring(0, wc_char_idx);
                    if (wc_char_idx != wc_test.target.Length - 1)
                        end_match = wc_test.target.Substring(wc_char_idx + 1);

                    /* Check for a match */
                    if (start_match != null && !fname.StartsWith(start_match))
                        continue;
                    if (end_match != null && !fname.EndsWith(end_match))
                        continue;

                    Wildcard wc_match = wc_test;
                    //max_wc_chars = wc_test.Length;

                    int start_match_length = (start_match == null) ? 0 : start_match.Length;
                    int end_match_length = (end_match == null) ? 0 : end_match.Length;
                    string wc_pattern = fname.Substring(start_match_length, fname.Length - start_match_length - end_match_length);

                    /* Ensure the inputs have rules */
                    bool input_rules = true;
                    foreach (string s in wc_test.mr.ifiles)
                    {
                        string dfile = s;
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

                            dfile = dfile.Substring(0, wc_index) + wc_pattern + dfile.Substring(wc_index + 1);
                        }

                        if (GetRules(dfile).Count == 0)
                        {
                            input_rules = false;
                            break;
                        }
                    }

                    if (input_rules)
                    {
                        ret.Add(new MakeRuleMatch { mr = wc_test.mr, wc_len = wc_test.target.Length, wc_pattern = wc_pattern });
                    }
                }
            }
            if (parent != null)
                ret.AddRange(((TyMakeState)parent).GetRules(fname));

            if (ret.Count == 0 && Statement.FileDirExists(fname))
                ret.Add(new MakeRuleMatch { mr = null, wc_len = 0, wc_pattern = null });

            return ret;
        }

        public Program.RuleForFunction GetRule(string fname, out string wc_pattern)
        {
            wc_pattern = null;

            if (rules.ContainsKey(fname))
                return rules[fname];

            /* Now try and match the greatest amount of wildcard characters */
            int max_wc_chars = -1;
            string wc_match = null;
            foreach (string wc_test in wildcards)
            {
                if (wc_test.Length < max_wc_chars)
                    continue;

                /* Find the wildcard character */
                int wc_char_idx = -1;
                for (int i = 0; i < wc_test.Length; i++)
                {
                    if (wc_test[i] == '%')
                    {
                        if (i == 0 || wc_test[i - 1] != '\\')
                        {
                            wc_char_idx = i;
                            break;
                        }
                    }
                }

                if (wc_char_idx == -1)
                    throw new Exception("no wildcard characted found in " + wc_test);

                string start_match = null;
                string end_match = null;

                if (wc_char_idx != 0)
                    start_match = wc_test.Substring(0, wc_char_idx);
                if (wc_char_idx != wc_test.Length - 1)
                    end_match = wc_test.Substring(wc_char_idx + 1);

                /* Check for a match */
                if (start_match != null && !fname.StartsWith(start_match))
                    continue;
                if (end_match != null && !fname.EndsWith(end_match))
                    continue;

                wc_match = wc_test;
                max_wc_chars = wc_test.Length;

                int start_match_length = (start_match == null) ? 0 : start_match.Length;
                int end_match_length = (end_match == null) ? 0 : end_match.Length;
                wc_pattern = fname.Substring(start_match_length, fname.Length - start_match_length - end_match_length);
            }

            if (wc_match != null)
                return rules[wc_match];
            else
                return null;
        }

        public Program.RuleForFunction GetRule(Expression fname, out string wc_pattern)
        {
            Expression.EvalResult e = fname.Evaluate(this);
            if (e.Type != Expression.EvalResult.ResultType.String)
                throw new Exception("build expects a string argument");

            string f = e.strval;

            return GetRule(f, out wc_pattern);
        }

        public override MakeState Clone()
        {
            TyMakeState other = new TyMakeState();
            other.wildcards = new List<string>(wildcards);
            other.Wildcards = new List<Wildcard>(Wildcards);
            other.rules = new Dictionary<string, Program.RuleForFunction>();
            foreach (var kvp in rules)
                other.rules[kvp.Key] = kvp.Value;

            CloneTo(other);

            return other;
        }

    }
}
