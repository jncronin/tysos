using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace typroject
{
    class MakeState
    {
        public Dictionary<string, string> props;
    }

    class Expression
    {
        internal Expression a, b;
        internal Tokens op;

        public virtual EvalResult Evaluate(MakeState s)
        {
            EvalResult ea, eb;

            switch (op)
            {
                case Tokens.NOT:
                    ea = a.Evaluate(s);
                    check_null(ea);
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
                        else if (ea.Type == EvalResult.ResultType.Int && eb.Type == EvalResult.ResultType.Int)
                        {
                            if (ea.intval == eb.intval)
                                return new EvalResult(_true);
                            else
                                return new EvalResult(_false);
                        }
                        else
                            throw new NotSupportedException();
                    }

                case Tokens.LT:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    if (ea.AsInt < eb.AsInt)
                        return new EvalResult(1);
                    else
                        return new EvalResult(0);

                case Tokens.GT:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    if (ea.AsInt > eb.AsInt)
                        return new EvalResult(1);
                    else
                        return new EvalResult(0);

                case Tokens.GEQUAL:
                    ea = a.Evaluate(s);
                    eb = b.Evaluate(s);

                    check_null(ea);
                    check_null(eb);

                    if (ea.AsInt >= eb.AsInt)
                        return new EvalResult(1);
                    else
                        return new EvalResult(0);

            }

            throw new NotImplementedException(op.ToString());
        }

        private void check_null(EvalResult ea)
        {
        }

        public class EvalResult
        {
            public enum ResultType { Int, String };

            public ResultType Type;

            public string strval;
            public long intval;

            public EvalResult(long i)
            {
                Type = ResultType.Int;
                intval = i;
            }
            public EvalResult(string s)
            {
                Type = ResultType.String;
                strval = s;
            }

            public long AsInt
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
                            if (strval.ToLower() == "false")
                                return 0;
                            return 1;
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
                    default:
                        throw new NotSupportedException();
                }
            }
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
            return new EvalResult(Project.process_string(val, s.props));
        }
    }

    internal class PropertyExpression : Expression
    {
        public Expression val;

        public override EvalResult Evaluate(MakeState s)
        {
            return val.Evaluate(s);
        }
    }

    internal class ListExpression : Expression
    {
        public Expression val;

        public override EvalResult Evaluate(MakeState s)
        {
            throw new NotImplementedException();
        }
    }
    
    internal class LabelDotExpression : Expression
    {
        public Expression val;
        public Expression srcval;

        public override EvalResult Evaluate(MakeState s)
        {
            var lhs = srcval.Evaluate(s);

            // search for a method containing the appropriate arguments
            LabelExpression rhs = val as LabelExpression;
            if (val is LabelDotExpression)
                rhs = ((LabelDotExpression)val).srcval as LabelExpression;

            string[] args = new string[rhs.arglist.Count];
            Type[] str_types = new Type[rhs.arglist.Count];
            for (int i = 0; i < rhs.arglist.Count; i++)
            {
                args[i] = rhs.arglist[i].Evaluate(s).strval;
                str_types[i] = typeof(string);
            }

            var str_type = typeof(string);



            var meth = str_type.GetMethod(rhs.val, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, null, str_types, null);
            var res = meth.Invoke(lhs.strval, args);

            var ret = res.ToString();
            if (ret.ToLower() == "true")
                return new EvalResult(1);
            else if (ret.ToLower() == "false")
                return new EvalResult(0);
            else
                return new EvalResult(ret);
        }
    }

    internal class LabelExpression : Expression
    {
        public string val;
        public List<Expression> arglist;

        public override EvalResult Evaluate(MakeState s)
        {
            if(arglist != null)
                throw new NotImplementedException();

            if (!s.props.ContainsKey(val))
                return new EvalResult("");
            else
                return new EvalResult(s.props[val]);
        }
    }

    internal class MetadataExpression : Expression
    {
        public Expression val;

        public override EvalResult Evaluate(MakeState s)
        {
            throw new NotImplementedException();
        }
    }

    internal class ExistsExpression : Expression
    {
        public Expression val;

        public override EvalResult Evaluate(MakeState s)
        {
            var v = val.Evaluate(s);

            try
            {
                System.IO.FileSystemInfo fi = new System.IO.FileInfo(v.strval);
                if ((fi.Attributes & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
                    fi = new System.IO.DirectoryInfo(v.strval);
                if (fi.Exists)
                    return new EvalResult(1);
                else
                    return new EvalResult(0);
            }
            catch (Exception)
            {
                return new EvalResult(0);
            }
        }
    }

    internal class HasTrailingSlashExpression : Expression
    {
        public Expression val;

        public override EvalResult Evaluate(MakeState s)
        {
            var v = val.Evaluate(s).strval;

            v = Program.replace_dir_split(v);

            if (v.EndsWith("/") || v.EndsWith("\\"))
                return new EvalResult(1);
            else
                return new EvalResult(0);
        }
    }

    internal class StaticExpression : Expression
    {
        public string type;
        public Expression val;

        public override EvalResult Evaluate(MakeState s)
        {
            Type t = null;
            foreach(var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = ass.GetType(type);
                if (t != null)
                    break;
            }
            if (t == null)
                throw new NotSupportedException();

            var rhs = val as LabelExpression;

            string[] args = new string[rhs.arglist.Count];
            Type[] str_types = new Type[rhs.arglist.Count];
            for(int i = 0; i < rhs.arglist.Count; i++)
            {
                args[i] = rhs.arglist[i].Evaluate(s).strval;
                str_types[i] = typeof(string);
            }

            var meth = t.GetMethod(rhs.val, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, str_types, null);
            var r = meth.Invoke(null, args).ToString();

            if (r.ToLower() == "true")
                return new EvalResult(1);
            else if (r.ToLower() == "false")
                return new EvalResult(0);
            else
                return new EvalResult(r);
        }
    }

    partial class Parser
    {
        internal Parser(Scanner s) : base(s) { }

        internal Expression val;
    }

    partial class Scanner
    {
        string text;
        internal Scanner(string val) : this() { SetSource(val, 0); text = val; }

        public override void yyerror(string format, params object[] args)
        {
            StringBuilder sb = new StringBuilder();

            Scanner s = new Scanner(text);
            int tok;
            do
            {
                tok = s.yylex();
                sb.Append((Tokens)tok);
                if(tok == (int)Tokens.LABEL || tok == (int)Tokens.STRING)
                {
                    sb.Append("(");
                    sb.Append(s.yylval.strval);
                    sb.Append(")");
                }
                sb.Append(" ");
            } while (tok != (int)Tokens.EOF);


            var stext = String.Format(format, args);
            stext = stext.Replace("LABEL", tokTxt);
            throw new Exception(stext + " at line " + yyline + ", col " + yycol + " in " + text + " (" + sb.ToString() + ")");
        }

        internal int sline { get { return yyline; } }
        internal int scol { get { return yycol; } }
    }
}