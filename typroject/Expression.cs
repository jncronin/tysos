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
            var stext = String.Format(format, args);
            stext = stext.Replace("LABEL", tokTxt);
            throw new Exception(stext + " at line " + yyline + ", col " + yycol + " in " + text);
        }

        internal int sline { get { return yyline; } }
        internal int scol { get { return yycol; } }
    }
}