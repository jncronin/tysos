using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tymake
{
    internal abstract class Statement
    {
        public class SyntaxException : Exception
        {
            public SyntaxException(string msg) : base(msg) { }
        }

        public abstract Expression.EvalResult Execute(MakeState s);
        public bool export = false;

        public static bool FileDirExists(string name)
        {
            try
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(name);
                if (fi.Exists)
                    return true;
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(name);
                return di.Exists;
            } 
            catch (Exception)
            {
                return false;
            }
        }

        internal static void ExportDef(string tag, MakeState s)
        {
            Expression.EvalResult e = s.GetDefine(tag);
            MakeState cur_s = s.parent;
            while (cur_s != null)
            {
                cur_s.SetDefine(tag, e);
                cur_s = cur_s.parent;
            }
        }
    }

    internal abstract class DefineStatement : Statement
    {
        public tymakeParse.Tokens assignop;
    }

    internal class DefineStringStatement : DefineStatement 
    {
        public string tok_name;
        public string val;

        public override Expression.EvalResult Execute(MakeState s)
        {
            Expression.EvalResult e = new Expression.EvalResult(val);

            s.SetDefine(tok_name, e, assignop);
            if (export)
                ExportDef(tok_name, s);
            return new Expression.EvalResult(0);
        }
    }

    internal class DefineLabelStatement : DefineStatement
    {
        public string tok_name;
        public string val;

        public override Expression.EvalResult Execute(MakeState s)
        {
            LabelExpression le = new LabelExpression { val = val };
            Expression.EvalResult e = le.Evaluate(s);
            s.SetDefine(tok_name, e, assignop);
            if (export)
                ExportDef(tok_name, s);
            return new Expression.EvalResult(0);
        }
    }

    internal class DefineExprStatement : DefineStatement
    {
        public Expression val;
        public string tok_name;

        public override Expression.EvalResult Execute(MakeState s)
        {
            Expression.EvalResult e = val.Evaluate(s);
            s.SetDefine(tok_name, e);
            if (export)
                ExportDef(tok_name, s);
            return new Expression.EvalResult(0);
        }
    }

    internal class DefineIntStatement : DefineStatement
    { 
        public string tok_name; 
        public int val;

        public override Expression.EvalResult Execute(MakeState s)
        {
            Expression.EvalResult e = new Expression.EvalResult(val);
            s.SetDefine(tok_name, e, assignop);
            if (export)
                ExportDef(tok_name, s);
            return new Expression.EvalResult(0);
        }
    }

    internal abstract class ControlStatement : Statement
    {
        public Statement code;
        public Expression test;
    }

    internal class IfBlockStatement : ControlStatement 
    { 
        public Statement if_block; 
        public Statement else_block;

        public override Expression.EvalResult Execute(MakeState s)
        {
            if (test.Evaluate(s).AsInt == 0)
            {
                if (else_block != null)
                    return else_block.Execute(s);
            }
            else
            {
                if (if_block != null)
                    return if_block.Execute(s);
            }
            return new Expression.EvalResult(0);
        }
    }

    internal class DoBlock : ControlStatement
    {
        public override Expression.EvalResult Execute(MakeState s)
        {
            throw new NotImplementedException();
        }
    }

    internal class ForBlockStatement : ControlStatement
    {
        public Statement incr, init;

        public override Expression.EvalResult Execute(MakeState s)
        {
            // run initializer
            Expression.EvalResult ret = init.Execute(s);
            if (ret.AsInt != 0)
                return ret;

            while(true)
            {
                // check condition
                if (test.Evaluate(s).AsInt == 0)
                    break;

                // exec code
                ret = code.Execute(s);
                if (ret.AsInt != 0)
                    return ret;
                if(s.returns != null)
                {
                    return new Expression.EvalResult(0);
                }

                // run incrementer
                incr.Execute(s);
            }
            return new Expression.EvalResult(0);
        }
    }

    internal class ForEachBlock : ControlStatement
    {
        public Expression enumeration;
        public string val;

        public override Expression.EvalResult Execute(MakeState s)
        {
            Expression.EvalResult e = enumeration.Evaluate(s);
            if (e.Type != Expression.EvalResult.ResultType.Array)
                throw new Exception("does not evaluate to array");

            foreach (Expression.EvalResult i in e.arrval)
            {
                MakeState cur_s = s.Clone();
                cur_s.SetDefine(val, i);
                Expression.EvalResult ret = code.Execute(cur_s);
                if (ret.AsInt != 0)
                    return ret;
                if (cur_s.returns != null)
                {
                    s.returns = cur_s.returns;
                    return new Expression.EvalResult(0);
                }
            }
            return new Expression.EvalResult(0);
        }
    }

    internal class WhileBlock : ControlStatement
    {
        public override Expression.EvalResult Execute(MakeState s)
        {
            throw new NotImplementedException();
        }
    }

    internal class FunctionStatement : Statement
    {
        public string name;
        public List<FunctionArg> args;
        public Statement code;

        public override Expression.EvalResult Execute(MakeState s)
        {
            string mangledname = Mangle();
            s.funcs[mangledname] = this;

            if (export)
            {
                MakeState cur_s = s.parent;
                while (cur_s != null)
                {
                    cur_s.funcs[mangledname] = this;
                    cur_s = cur_s.parent;
                }
            }

            return new Expression.EvalResult(0);
        }

        public virtual Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            MakeState new_s = s.Clone();

            for (int i = 0; i < args.Count; i++)
                new_s.SetDefine(args[i].name, passed_args[i]);

            code.Execute(new_s);
            if (new_s.returns != null)
                return new_s.returns;

            return new Expression.EvalResult();
        }

        public class FunctionArg
        {
            public string name;
            public Expression.EvalResult.ResultType argtype;
        }

        public string Mangle()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name.Length.ToString());
            sb.Append(name);
            foreach (FunctionArg arg in args)
            {
                switch (arg.argtype)
                {
                    case Expression.EvalResult.ResultType.Int:
                        sb.Append("i");
                        break;
                    case Expression.EvalResult.ResultType.String:
                        sb.Append("s");
                        break;
                    case Expression.EvalResult.ResultType.Array:
                        sb.Append("a");
                        break;
                    case Expression.EvalResult.ResultType.Object:
                        sb.Append("o");
                        break;
                    case Expression.EvalResult.ResultType.Void:
                        sb.Append("v");
                        break;
                }
            }
            return sb.ToString();
        }
    }

    internal class ExportStatement : Statement
    {
        public string v;

        public override Expression.EvalResult Execute(MakeState s)
        {
            if (s.IsDefined(v) == false)
            {
                throw new Exception("export: variable " + v + " is not defined in this scope");
            }

            Expression.EvalResult e = s.GetDefine(v);
            MakeState cur_s = s.parent;
            while (cur_s != null)
            {
                cur_s.SetDefine(v, e);
                cur_s = cur_s.parent;
            }

            return new Expression.EvalResult(0);
        }
    }

    internal class ReturnStatement : Statement
    {
        public Expression v;

        public override Expression.EvalResult Execute(MakeState s)
        {
            s.returns = v.Evaluate(s);
            return new Expression.EvalResult(0);
        }
    }

    internal class MakeRuleStatement : Statement 
    {
        public Expression output_file;
        public List<Expression> depend_list;
        public List<Expression> inputs_list;
        public List<string> dfiles = null;
        public List<string> ifiles = null;
        public Statement rules;

        MakeState state_at_def;

        public override Expression.EvalResult Execute(MakeState s)
        {
            if (dfiles != null || ifiles != null)
            {
                /* We are re-executing a make rule - this can occur in a for/foreach/while block etc
                 * We need to clone the rule here as a separate instance is being created */

                MakeRuleStatement new_mrs = new MakeRuleStatement { output_file = output_file, depend_list = depend_list, inputs_list = inputs_list, rules = rules, export = export };
                return new_mrs.Execute(s);
            }
            string ofile = output_file.Evaluate(s).strval;
            state_at_def = s.Clone();
            dfiles = FlattenToString(depend_list, s);
            ifiles = FlattenToString(inputs_list, s);
            s.AddRule(ofile, this);
            return new Expression.EvalResult(0);
        }

        public int Build(MakeState s, Expression target, string wc_match)
        {
            string tfile = target.Evaluate(s).strval;
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

            /* Now ensure all the dependencies are available */
            DateTime most_recent_dependency = new DateTime(0);
            System.IO.FileInfo mrd_fi = null;   // store the most recent dependency for debugging purposes
            foreach (string depend in all_deps)
            {
                BuildCommandStatement bc = new BuildCommandStatement { fname = new StringExpression { val = depend } };
                int dep_ret = bc.Execute(s).AsInt;
                System.IO.FileInfo dep_fi = null;
                if (dep_ret == BuildCommandStatement.RUN_NO_RULE)
                {
                    /* No rule to build the file - its still okay as long as the file already exists */
                    dep_fi = new System.IO.FileInfo(depend);
                    if (!Statement.FileDirExists(depend))
                        throw new Exception(depend + " does not exist and no rule to build it");
                }
                else if (dep_ret != 0)
                    return dep_ret;

                dep_fi = new System.IO.FileInfo(depend);
                if (!Statement.FileDirExists(depend))
                    throw new Exception("error building " + depend);

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
            System.IO.FileInfo targ_fi = new System.IO.FileInfo(tfile);
            if (!Statement.FileDirExists(tfile))
                to_build = true;
            else if (most_recent_dependency.CompareTo(targ_fi.LastWriteTime) > 0)
                to_build = true;
            else if (depend_list == null)
                to_build = true;

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
                return rules.Execute(new_s).AsInt;
            }

            return 0;
        }

        private List<string> FlattenToString(List<Expression> depend_list, MakeState s)
        {
            List<string> ret = new List<string>();
            foreach (Expression e in depend_list)
            {
                Expression.EvalResult er = e.Evaluate(s);

                FlattenToString(er, ret, s);
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
    }

    internal class ShellCommandStatement : Statement 
    { 
        public Expression shell_cmd;

        public override Expression.EvalResult Execute(MakeState s)
        {
            string cmd = shell_cmd.Evaluate(s).strval;

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
            return new Expression.EvalResult(p.ExitCode);
        }
    }

    internal class TyProjectCommandStatement : Statement 
    {
        public Expression typroject;

        public override Expression.EvalResult Execute(MakeState s)
        {
            throw new NotImplementedException();
        }
    }

    internal class BuildCommandStatement : Statement
    {
        public Expression fname;

        public const int RUN_NO_RULE = 0x8000000;
        private int make_compare(MakeState.MakeRuleMatch a, MakeState.MakeRuleMatch b)
        {
            return a.wc_len - b.wc_len;
        }

        public override Expression.EvalResult Execute(MakeState s)
        {
            string target = fname.Evaluate(s).strval;
            Uri furi = new Uri(new Uri(Environment.CurrentDirectory + "/"), target);
            target = furi.AbsolutePath;
            List<MakeState.MakeRuleMatch> matches = s.GetRules(target);
            matches.Sort(make_compare);

            foreach (MakeState.MakeRuleMatch match in matches)
            {
                if (match.mr == null)
                    return new Expression.EvalResult(0);
                int ret = match.mr.Build(s, fname, match.wc_pattern);
                if (ret == 0)
                    return new Expression.EvalResult(0);
            }

            s.GetRules(target);
            return new Expression.EvalResult(RUN_NO_RULE);
        }
    }

    internal class StatementList : Statement 
    { 
        public List<Statement> list;

        public override Expression.EvalResult Execute(MakeState s)
        {
            if (list != null)
            {
                foreach (Statement st in list)
                {
                    Expression.EvalResult er = st.Execute(s);
                    if (!(st is ExpressionStatement) && er.AsInt != 0)
                        return er;
                    if (s.returns != null)
                        return new Expression.EvalResult(0);
                }
            }
            return new Expression.EvalResult(0);
        }
    }

    internal class MkDirCommandStatement : Statement 
    {
        public Expression dir;

        public override Expression.EvalResult Execute(MakeState s)
        {
            string dir_name = dir.Evaluate(s).strval;
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(dir_name);
            if (!di.Exists)
            {
                try
                {
                    di.Create();
                }
                catch (Exception e)
                {
                    Console.WriteLine("failed to create directory " + dir_name + ": " + e.ToString());
                    return new Expression.EvalResult(-1);
                }
            }
            return new Expression.EvalResult(0);
        }
    }

    internal class StringStatement : Statement 
    { 
        public string val;

        public override Expression.EvalResult Execute(MakeState s)
        {
            throw new NotImplementedException();
        }
    }

    internal class ExpressionStatement : Statement
    {
        public Expression expr;

        public override Expression.EvalResult Execute(MakeState s)
        {
            return expr.Evaluate(s);
        }
    }

    internal class LabelStatement : Statement 
    { 
        public string val;

        public override Expression.EvalResult Execute(MakeState s)
        {
            throw new NotImplementedException();
        }
    }

    internal class IncludeStatement : Statement 
    { 
        public Statement include_file;

        public override Expression.EvalResult Execute(MakeState s)
        {
            throw new NotImplementedException();
        }
    }
}
