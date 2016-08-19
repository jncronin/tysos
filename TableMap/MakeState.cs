using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableMap
{
    class MakeState
    {
        public MakeState parent = null;
        public Expression.EvalResult returns = null;
        public List<string> search_paths = new List<string> { ".", "" };

        Dictionary<string, Expression.EvalResult> defs = new Dictionary<string, Expression.EvalResult>();
        public Dictionary<string, FunctionStatement> funcs = new Dictionary<string, FunctionStatement>();

        public bool IsDefined(string tag) {
            if (defs.ContainsKey(tag) == true)
                return true;
            return VarGenFunction.all_defs.ContainsKey(tag);
        }

        public void SetDefine(string tag, Expression.EvalResult e) { defs[tag] = e; }
        public void SetDefine(string tag, Expression.EvalResult e, bool export)
        {
            if (export == false)
                SetDefine(tag, e);
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
            if (defs.ContainsKey(tag))
                return defs[tag];
            else return VarGenFunction.all_defs[tag];
        }

        public void SetDefine(string tag, Expression.EvalResult e, Tokens assignop)
        {
            switch (assignop)
            {
                case Tokens.ASSIGN:
                    defs[tag] = e;
                    break;

                case Tokens.ASSIGNIF:
                    if (!defs.ContainsKey(tag))
                        defs[tag] = e;
                    break;

                case Tokens.APPEND:
                    if (defs.ContainsKey(tag))
                    {
                        Expression.EvalResult src = defs[tag];
                        Expression append = new Expression { a = src, b = e, op = Tokens.PLUS };
                        defs[tag] = append.Evaluate(this);
                    }
                    else
                        defs[tag] = e;
                    break;
            }
        }

        public MakeState Clone()
        {
            MakeState other = new MakeState();

            foreach (KeyValuePair<string, Expression.EvalResult> kvp in defs)
                other.defs[kvp.Key] = kvp.Value;

            foreach (KeyValuePair<string, FunctionStatement> kvp in funcs)
                other.funcs[kvp.Key] = kvp.Value;

            other.parent = this;
            other.search_paths = new List<string>(search_paths);

            return other;
        }
    }
}
