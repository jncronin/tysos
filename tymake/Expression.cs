using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using tymakeParse;

namespace tymake
{
    internal class Expression
    {
        public Expression a, b;
        public Tokens op;

        public virtual EvalResult Evaluate(MakeState s)
        {
            EvalResult ea, eb;

            switch (op)
            {
                case Tokens.NOT:
                    ea = a.Evaluate(s);
                    if (ea.AsInt == 0)
                        return new EvalResult(1);
                    else
                        return new EvalResult(0);

                case Tokens.LAND:
                    ea = a.Evaluate(s);
                    if (ea.AsInt == 0)
                        return new EvalResult(0);
                    eb = b.Evaluate(s);
                    if (eb.AsInt == 0)
                        return new EvalResult(0);
                    return new EvalResult(1);

                case Tokens.LOR:
                    ea = a.Evaluate(s);
                    if (ea.AsInt != 0)
                        return new EvalResult(1);
                    eb = b.Evaluate(s);
                    if (eb.AsInt != 0)
                        return new EvalResult(1);
                    return new EvalResult(0);

                case Tokens.PLUS:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    if (ea.Type == EvalResult.ResultType.Int && eb.Type == EvalResult.ResultType.Int)
                        return new EvalResult(ea.intval + eb.intval);
                    else if (ea.Type == EvalResult.ResultType.String && eb.Type == EvalResult.ResultType.String)
                        return new EvalResult(ea.strval + eb.strval);
                    else if (ea.Type == EvalResult.ResultType.Void && eb.Type == EvalResult.ResultType.Void)
                        return new EvalResult();
                    else
                        throw new Statement.SyntaxException("Mismatched arguments to PLUS: " + ea.Type.ToString() + " and " + eb.Type.ToString());

                case Tokens.MUL:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    return new EvalResult(ea.AsInt * eb.AsInt);

                case Tokens.MINUS:
                    ea = a.Evaluate(s);

                    if (b == null)
                    {
                        // unary minus
                        if (ea.Type == EvalResult.ResultType.Void)
                            return ea;
                        if (ea.Type == EvalResult.ResultType.String)
                            throw new Statement.SyntaxException("Cannot apply unary minus to type string");
                        return new EvalResult(0 - ea.intval);
                    }
                    else
                    {
                        eb = b.Evaluate(s);

                        if (ea.Type == EvalResult.ResultType.String && (eb.Type == EvalResult.ResultType.Int || eb.Type == EvalResult.ResultType.Void))
                        {
                            int rem_amount = eb.AsInt;
                            if (rem_amount > ea.strval.Length)
                                rem_amount = ea.strval.Length;
                            return new EvalResult(ea.strval.Substring(0, ea.strval.Length - rem_amount));
                        }
                        else if (ea.Type == EvalResult.ResultType.String && eb.Type == EvalResult.ResultType.String)
                        {
                            if (ea.strval.EndsWith(eb.strval))
                                return new EvalResult(ea.strval.Substring(0, ea.strval.Length - eb.strval.Length));
                            else
                                throw new Statement.SyntaxException(ea.strval + " does not end with " + eb.strval);
                        }
                        else if (ea.Type == EvalResult.ResultType.Void && eb.Type == EvalResult.ResultType.Void)
                        {
                            return new EvalResult();
                        }
                        else
                        {
                            return new EvalResult(ea.AsInt - eb.AsInt);
                        }
                    }

                case Tokens.DEFINED:
                    ea = a.Evaluate(s);

                    if (ea.Type != EvalResult.ResultType.String)
                        throw new Statement.SyntaxException("defined requires a string/label argument");

                    if (s.IsDefined(ea.strval))
                        return new EvalResult(1);
                    else
                        return new EvalResult(0);

                case Tokens.EQUALS:
                case Tokens.NOTEQUAL:
                    {
                        int _true = 1;
                        int _false = 0;

                        if (op == Tokens.NOTEQUAL)
                        {
                            _true = 0;
                            _false = 1;
                        }

                        ea = a.Evaluate(s);
                        eb = b.Evaluate(s);

                        if (ea.Type == EvalResult.ResultType.String && eb.Type == EvalResult.ResultType.String)
                        {
                            if (ea.strval == null)
                            {
                                if (eb.strval == null)
                                    return new EvalResult(_true);
                                else
                                    return new EvalResult(_false);
                            }
                            if (ea.strval.Equals(eb.strval))
                                return new EvalResult(_true);
                            else
                                return new EvalResult(_false);
                        }
                        else
                        {
                            if (ea.AsInt == eb.AsInt)
                                return new EvalResult(_true);
                            else
                                return new EvalResult(_false);
                        }
                    }

                case Tokens.LT:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    if (ea.AsInt < eb.AsInt)
                        return new EvalResult(1);
                    else
                        return new EvalResult(0);

            }
                    
            throw new NotImplementedException(op.ToString());
        }

        public class EvalResult
        {
            public enum ResultType { Int, String, Void, Function, MakeRule, Object, Array };

            public ResultType Type;

            public string strval;
            public int intval;
            public Dictionary<string, EvalResult> objval;
            public FunctionStatement funcval;
            public MakeRuleStatement mrval;
            public List<EvalResult> arrval;

            public EvalResult()
            {
                Type = ResultType.Void;
            }
            public EvalResult(int i)
            {
                Type = ResultType.Int;
                intval = i;
            }
            public EvalResult(string s)
            {
                Type = ResultType.String;
                strval = s;
            }
            public EvalResult(FunctionStatement f)
            {
                Type = ResultType.Function;
                funcval = f;
            }
            public EvalResult(MakeRuleStatement mr)
            {
                Type = ResultType.MakeRule;
                mrval = mr;
            }
            public EvalResult(Dictionary<string, EvalResult> o)
            {
                Type = ResultType.Object;
                objval = o;
            }
            public EvalResult(IEnumerable<string> sarr)
            {
                Type = ResultType.Array;
                arrval = new List<EvalResult>();
                foreach (string src in sarr)
                    arrval.Add(new EvalResult(src));
            }
            public EvalResult(IEnumerable<int> iarr)
            {
                Type = ResultType.Array;
                arrval = new List<EvalResult>();
                foreach (int src in iarr)
                    arrval.Add(new EvalResult(src));
            }
            public EvalResult(IEnumerable<EvalResult> earr)
            {
                Type = ResultType.Array;
                arrval = new List<EvalResult>(earr);
            }

            public int AsInt
            {
                get
                {
                    switch (Type)
                    {
                        case ResultType.Int:
                            return intval;
                        case ResultType.String:
                            if (strval == null || strval == "")
                                return 0;
                            return 1;
                        case ResultType.Void:
                            return 0;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            public static implicit operator Expression(EvalResult er)
            {
                return new ResultExpression { e = er };
            }

            public override string ToString()
            {
                switch (Type)
                {
                    case ResultType.Int:
                        return intval.ToString();
                    case ResultType.String:
                        return "\"" + strval + "\"";
                    case ResultType.Void:
                        return "{void}";
                    case ResultType.Array:
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("[ ");
                            for(int i = 0; i < arrval.Count; i++)
                            {
                                if (i != 0)
                                    sb.Append(", ");
                                sb.Append(arrval[i].ToString());
                            }
                            sb.Append(" ]");
                            return sb.ToString();
                        }
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }

    internal class ResultExpression : Expression
    {
        public EvalResult e;

        public override EvalResult Evaluate(MakeState s)
        {
            return e;
        }
    }

    internal class StringExpression : Expression
    {
        public string val;

        public override EvalResult Evaluate(MakeState s)
        {
            /* First, parse variables in the shell command */
            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < val.Length)
            {
                if (val[i] == '\\' && i < (val.Length - 1))
                {
                    sb.Append(val[i++]);
                    sb.Append(val[i++]);
                }
                else if (val[i] == '$')
                {
                    /* Now try and match the largest possible variable we can */
                    int j = i + 1;

                    string match = null;

                    while (j < (val.Length) && val[j] != '$')
                    {
                        string test = val.Substring(i + 1, j - i);

                        if (s.IsDefined(test))
                            match = test;
                        j++;
                    }

                    if (match != null)
                    {
                        sb.Append(s.GetDefine(match).strval);
                        i++;
                        i += match.Length;
                    }
                    else
                        sb.Append(val[i++]);
                }
                else
                    sb.Append(val[i++]);
            }
            return new EvalResult(sb.ToString());
        }
    }

    internal class LabelExpression : Expression
    {
        public string val;

        public override EvalResult Evaluate(MakeState s)
        {
            if (s.IsDefined(val) == false)
                return new EvalResult();
            return s.GetDefine(val);
        }
    }

    internal class LabelIndexedExpression : Expression
    {
        public Expression label;
        public Expression index;

        public override EvalResult Evaluate(MakeState s)
        {
            EvalResult elabel = label.Evaluate(s);
            EvalResult eindex = index.Evaluate(s);

            switch(elabel.Type)
            {
                case EvalResult.ResultType.String:
                    return new EvalResult(new string(elabel.strval[eindex.AsInt], 1));
                case EvalResult.ResultType.Array:
                    return elabel.arrval[eindex.AsInt];
                case EvalResult.ResultType.Object:
                    return elabel.objval[eindex.strval];
                default:
                    throw new Statement.SyntaxException("indexing cannot be applied to object of type: " + elabel.Type.ToString());
            }
        }
    }

    internal class LabelMemberExpression : Expression
    {
        public Expression label;
        public Expression member;

        public override EvalResult Evaluate(MakeState s)
        {
            EvalResult elabel = label.Evaluate(s);

            if (member is LabelExpression)
            {
                string m = ((LabelExpression)member).val;

                switch (elabel.Type)
                {
                    case EvalResult.ResultType.String:
                        if (m == "length")
                            return new EvalResult(elabel.strval.Length);
                        break;
                    case EvalResult.ResultType.Object:
                        if (elabel.objval.ContainsKey(m))
                            return elabel.objval[m];
                        break;
                    case EvalResult.ResultType.Array:
                        if (m == "length")
                            return new EvalResult(elabel.arrval.Count);
                        break;
                }

                if (m == "type")
                    return new EvalResult(elabel.Type.ToString());
                throw new Statement.SyntaxException("object: " + label.ToString() + " does not contain member " + m.ToString());
            }
            else if (member is FuncCall)
            {
                FuncCall f = member as FuncCall;
                FuncCall fsite = new FuncCall { target = f.target };
                fsite.args = new List<Expression>(f.args);
                fsite.args.Insert(0, label);
                string m = fsite.Mangle(s);

                switch (elabel.Type)
                {
                    case EvalResult.ResultType.String:
                        if(m == "5splitss")
                        {
                            string[] split = elabel.strval.Split(new string[] { f.args[0].Evaluate(s).strval }, StringSplitOptions.None);
                            EvalResult[] ret = new EvalResult[split.Length];
                            for (int i = 0; i < split.Length; i++)
                                ret[i] = new EvalResult(split[i]);
                            return new EvalResult(ret);
                        }
                        break;

                    case EvalResult.ResultType.Array:
                        if (m == "3addai")
                        {
                            elabel.arrval.Add(new EvalResult(f.args[0].Evaluate(s).intval));
                            return new EvalResult();
                        }
                        else if (m == "3addas")
                        {
                            elabel.arrval.Add(new EvalResult(f.args[0].Evaluate(s).strval));
                            return new EvalResult();
                        }
                        else if (m == "3addao")
                        {
                            elabel.arrval.Add(new EvalResult(f.args[0].Evaluate(s).objval));
                            return new EvalResult();
                        }
                        else if (m == "3addaa")
                        {
                            elabel.arrval.Add(new EvalResult(f.args[0].Evaluate(s).arrval));
                            return new EvalResult();
                        }
                        else if (m == "8addrangeaa")
                        {
                            elabel.arrval.AddRange(f.args[0].Evaluate(s).arrval);
                            return new EvalResult();
                        }
                        else if (m == "6insertaii")
                        {
                            elabel.arrval.Insert(f.args[1].Evaluate(s).intval, new EvalResult(f.args[0].Evaluate(s).intval));
                            return new EvalResult();
                        }
                        else if (m == "6insertais")
                        {
                            elabel.arrval.Insert(f.args[1].Evaluate(s).intval, new EvalResult(f.args[0].Evaluate(s).strval));
                            return new EvalResult();
                        }
                        else if (m == "6insertaio")
                        {
                            elabel.arrval.Insert(f.args[1].Evaluate(s).intval, new EvalResult(f.args[0].Evaluate(s).objval));
                            return new EvalResult();
                        }
                        else if (m == "6insertaia")
                        {
                            elabel.arrval.Insert(f.args[1].Evaluate(s).intval, new EvalResult(f.args[0].Evaluate(s).arrval));
                            return new EvalResult();
                        }
                        else if (m == "6removeai")
                        {
                            elabel.arrval.RemoveAt(f.args[0].Evaluate(s).intval);
                            return new EvalResult();
                        }
                        break;

                    case EvalResult.ResultType.Object:
                        if (elabel.objval.ContainsKey(m))
                        {
                            EvalResult feval = elabel.objval[m];
                            if (feval.Type == EvalResult.ResultType.Function)
                            {
                                List<EvalResult> fargs = new List<EvalResult>();
                                foreach (Expression e in fsite.args)
                                    fargs.Add(e.Evaluate(s));
                                return feval.funcval.Run(s, fargs);                                
                            }
                        }

                        break;
                }

                throw new Statement.SyntaxException("object: " + label.ToString() + " does not contain member " + m.ToString());
            }
            else
                throw new NotSupportedException();
        }
    }

    internal class IntExpression : Expression
    {
        public int val;

        public override EvalResult Evaluate(MakeState s)
        {
            return new EvalResult(val);
        }
    }

    internal class ObjectExpression : Expression
    {
        public Dictionary<string, Expression.EvalResult> val;

        public override EvalResult Evaluate(MakeState s)
        {
            return new EvalResult(val);
        }
    }

    internal class ProjectDepends : Expression
    {
        public Expression project;
    }

    internal class ShellCmdDepends : Expression
    {
        public Expression shellcmd;
    }

    internal class FuncCall : Expression
    {
        public string target;
        public List<Expression> args;

        public string Mangle(MakeState s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(target.Length.ToString());
            sb.Append(target);
            foreach (Expression arg in args)
            {
                Expression.EvalResult e = arg.Evaluate(s);
                switch (e.Type)
                {
                    case EvalResult.ResultType.Int:
                        sb.Append("i");
                        break;
                    case EvalResult.ResultType.String:
                        sb.Append("s");
                        break;
                    case EvalResult.ResultType.Array:
                        sb.Append("a");
                        break;
                    case EvalResult.ResultType.Object:
                        sb.Append("o");
                        break;
                    case EvalResult.ResultType.Void:
                        sb.Append("v");
                        break;
                }
            }
            return sb.ToString();
        }

        public override EvalResult Evaluate(MakeState s)
        {
            string mangled_name = Mangle(s);
            if (s.funcs.ContainsKey(mangled_name))
            {
                List<EvalResult> args_to_pass = new List<EvalResult>();
                foreach (Expression arg in args)
                    args_to_pass.Add(arg.Evaluate(s));
                return s.funcs[mangled_name].Run(s, args_to_pass);
            }
            else
                throw new Statement.SyntaxException("unable to find function " + mangled_name);
        }
    }

    class ArrayExpression : Expression
    {
        public List<Expression> val;

        public override EvalResult Evaluate(MakeState s)
        {
            List<EvalResult> ret = new List<EvalResult>();
            foreach (Expression e in val)
                ret.Add(e.Evaluate(s));
            return new EvalResult(ret);
        }
    }

    class ObjExpression : Expression
    {
        public List<ObjDef> val;

        public override EvalResult Evaluate(MakeState s)
        {
            Dictionary<string, EvalResult> ret = new Dictionary<string, EvalResult>();
            foreach (ObjDef o in val)
                ret[o.name] = o.val.Evaluate(s);
            return new EvalResult(ret);
        }
    }

    class ObjDef
    {
        public string name;
        public Expression val;
    }
}
