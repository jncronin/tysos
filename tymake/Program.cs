using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tymake
{
    class Program
    {
        static void Main(string[] args)
        {

            MakeState s = new MakeState();

            /* Define some common functions */
            new PrintFunction { args = new List<FunctionStatement.FunctionArg> { new FunctionStatement.FunctionArg { name = "val", argtype = Expression.EvalResult.ResultType.Int } } }.Execute(s);
            new PrintFunction { args = new List<FunctionStatement.FunctionArg> { new FunctionStatement.FunctionArg { name = "val", argtype = Expression.EvalResult.ResultType.String } } }.Execute(s);
            new PrintFunction { args = new List<FunctionStatement.FunctionArg> { new FunctionStatement.FunctionArg { name = "val", argtype = Expression.EvalResult.ResultType.Object } } }.Execute(s);
            new PrintFunction { args = new List<FunctionStatement.FunctionArg> { new FunctionStatement.FunctionArg { name = "val", argtype = Expression.EvalResult.ResultType.Array } } }.Execute(s);
            new DirectoryFunction { name = "dir" }.Execute(s);
            new DirectoryFunction { name = "basefname" }.Execute(s);
            new DirectoryFunction { name = "ext" }.Execute(s);
            new TyProject().Execute(s);
            new CopyFunction().Execute(s);
            new FOpenFunction().Execute(s);
            new StrToArrFunction().Execute(s);
            new ArrToStrFunction().Execute(s);
            new ExistsFunction().Execute(s);
            new RmFunction().Execute(s);

            /* Add in current environment variables */
            System.Collections.IDictionary env_vars = Environment.GetEnvironmentVariables();
            foreach (System.Collections.DictionaryEntry env_var in env_vars)
                s.SetDefine(env_var.Key.ToString(), new Expression.EvalResult(env_var.Value.ToString()));

            /* Include the standard library */
            System.IO.FileInfo exec_fi = new System.IO.FileInfo(typeof(Program).Module.FullyQualifiedName);
            System.IO.FileInfo[] stdlib_fis = exec_fi.Directory.GetFiles("*.tmh");
            foreach(System.IO.FileInfo stdlib_fi in stdlib_fis)
                ExecuteFile(stdlib_fi.FullName, s);

            /* Execute top-level statements */
            ExecuteFile("test.tmk", s);
        }

        internal static Expression.EvalResult ExecuteFile(string name, MakeState s)
        {
            System.IO.FileStream f = new System.IO.FileStream(name, System.IO.FileMode.Open);
            tymakeParse.Parser p = new tymakeParse.Parser(new tymakeParse.Scanner(f));
            bool res = p.Parse();
            if (res == false)
                throw new Exception("Parse error");
            return p.output.Execute(s);
        }
    }

    class StrToArrFunction : FunctionStatement
    {
        public StrToArrFunction()
        {
            name = "strtoarr";
            args = new List<FunctionArg> { new FunctionArg { name = "str", argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            string str = passed_args[0].strval;
            List<Expression.EvalResult> ret = new List<Expression.EvalResult>();
            foreach (char c in str)
                ret.Add(new Expression.EvalResult((int)c));
            return new Expression.EvalResult(ret);
        }
    }

    class ArrToStrFunction : FunctionStatement
    {
        public ArrToStrFunction()
        {
            name = "arrtostr";
            args = new List<FunctionArg> { new FunctionArg { name = "arr", argtype = Expression.EvalResult.ResultType.Array } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            List<Expression.EvalResult> arr = passed_args[0].arrval;
            StringBuilder sb = new StringBuilder();
            foreach (Expression.EvalResult e in arr)
            {
                if (e.Type != Expression.EvalResult.ResultType.Int)
                    throw new Exception("arrtostr: array of integers required");
                sb.Append((char)e.intval);
            }
            return new Expression.EvalResult(sb.ToString());
        }
    }

    class ExistsFunction : FunctionStatement
    {
        public ExistsFunction()
        {
            name = "exists";
            args = new List<FunctionArg> { new FunctionArg { name = "fname", argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            string fname = passed_args[0].strval;
            if (Statement.FileDirExists(fname))
                return new Expression.EvalResult(1);
            else
                return new Expression.EvalResult(0);
        }
    }

    class RmFunction : FunctionStatement
    {
        public RmFunction()
        {
            name = "rm";
            args = new List<FunctionArg> { new FunctionArg { name = "fname", argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            string fname = passed_args[0].strval;
            System.IO.FileInfo fi = new System.IO.FileInfo(fname);
            if (fi.Exists)
            {
                fi.Delete();
                return new Expression.EvalResult(0);
            }
            else
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(fname);
                if (di.Exists)
                {
                    di.Delete(true);
                    return new Expression.EvalResult(0);
                }
            }
            throw new Exception("rm: " + fname + " does not exist");
            return new Expression.EvalResult(-1);
        }
    }

    class CopyFunction : FunctionStatement
    {
        public CopyFunction()
        {
            name = "cp";
            args = new List<FunctionArg> { new FunctionArg { name = "src", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "dest", argtype = Expression.EvalResult.ResultType.String }
            };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            string src = passed_args[0].strval;
            string dest = passed_args[1].strval;

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(dest);
            if (di.Exists)
            {
                // di is a directory - append the source file name
                System.IO.FileInfo fi = new System.IO.FileInfo(src);
                dest += "/";
                dest += fi.Name;
            }

            System.IO.FileStream r = new System.IO.FileStream(src, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.IO.FileStream w = new System.IO.FileStream(dest, System.IO.FileMode.Create, System.IO.FileAccess.Write);

            int len = (int)r.Length;
            int buf_len = 0x1000;
            byte[] buf = new byte[buf_len];

            while (true)
            {
                int read = r.Read(buf, 0, buf_len);
                if (read == 0)
                    break;
                w.Write(buf, 0, read);
            }

            r.Close();
            w.Close();

            return new Expression.EvalResult(0);
        }
    }

    class DirectoryFunction : FunctionStatement
    {
        public DirectoryFunction()
        {
            args = new List<FunctionArg> { new FunctionArg { name = "fname", argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(passed_args[0].strval);
            if (!fi.Exists)
                throw new Exception(name + ": " + passed_args[0].strval + " does not exist");
            if (name == "dir")
                return new Expression.EvalResult(fi.DirectoryName);
            else if (name == "basefname")
            {
                string fname = fi.Name;
                if (fname.Contains('.'))
                    fname = fname.Substring(0, fname.LastIndexOf('.'));

                return new Expression.EvalResult(fname);
            }
            else if (name == "ext")
            {
                return new Expression.EvalResult(fi.Extension);
            }
            throw new Exception("Unsupported function");
        }
    }

    class PrintFunction : FunctionStatement
    {
        public PrintFunction()
        {
            name = "print";
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> args)
        {
            Print(args[0], true);

            return new Expression.EvalResult();
        }

        void Print(Expression.EvalResult e, bool toplevel)
        {
            switch (e.Type)
            {
                case Expression.EvalResult.ResultType.Int:
                    Console.Write(e.intval);
                    break;
                case Expression.EvalResult.ResultType.String:
                    if (!toplevel)
                        Console.Write("\"");
                    Console.Write(e.strval);
                    if (!toplevel)
                        Console.Write("\"");
                    break;
                case Expression.EvalResult.ResultType.Array:
                    Console.Write("[ ");
                    for (int i = 0; i < e.arrval.Count; i++)
                    {
                        if (i != 0)
                            Console.Write(", ");
                        Print(e.arrval[i], false);
                    }
                    Console.Write(" ]");
                    break;
                case Expression.EvalResult.ResultType.Object:
                    Console.Write("[ ");
                    int j = 0;
                    foreach (KeyValuePair<string, Expression.EvalResult> kvp in e.objval)
                    {
                        if (j != 0)
                            Console.Write(", ");
                        Console.Write(kvp.Key);
                        Console.Write(": ");
                        Print(kvp.Value, false);
                        j++;
                    }
                    Console.Write(" ]");
                    break;                        
            }
        }
    }

    class FOpenFunction : FunctionStatement
    {
        public FOpenFunction()
        {
            name = "fopen";
            args = new List<FunctionArg> { new FunctionArg { name = "fname", argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            string fname = passed_args[0].strval;

            System.IO.FileStream fs = new System.IO.FileStream(fname, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite);

            Dictionary<string, Expression.EvalResult> ret = new Dictionary<string, Expression.EvalResult>();
            ret["FileName"] = new Expression.EvalResult(new System.IO.FileInfo(fname).FullName);
            ret["Length"] = new Expression.EvalResult((int)fs.Length);
            ret["Pos"] = new Expression.EvalResult((int)fs.Position);

            ReadFunction rf = new ReadFunction(fs);
            ret[rf.Mangle()] = new Expression.EvalResult(rf);

            WriteFunction wf = new WriteFunction(fs);
            ret[wf.Mangle()] = new Expression.EvalResult(wf);

            return new Expression.EvalResult(ret);
        }

        internal class ReadFunction : FunctionStatement
        {
            System.IO.FileStream fs;

            public ReadFunction(System.IO.FileStream fstream)
            {
                fs = fstream;
                name = "Read";
                args = new List<FunctionArg> { new FunctionArg { name = "this", argtype = Expression.EvalResult.ResultType.Object },
                    new FunctionArg { name = "buf", argtype = Expression.EvalResult.ResultType.Array },
                    new FunctionArg { name = "offset", argtype = Expression.EvalResult.ResultType.Int },
                    new FunctionArg { name = "len", argtype = Expression.EvalResult.ResultType.Int } };
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                Dictionary<string, Expression.EvalResult> obj = passed_args[0].objval;
                List<Expression.EvalResult> buf = passed_args[1].arrval;
                int offset = passed_args[2].intval;
                int len = passed_args[3].intval;

                fs.Position = obj["Pos"].intval;
                byte[] csbuf = new byte[len];
                int read = fs.Read(csbuf, 0, len);

                for (int i = 0; i < read; i++)
                {
                    byte v = csbuf[i];
                    int didx = i + offset;

                    while (didx >= buf.Count)
                        buf.Add(new Expression.EvalResult(0));
                    buf[didx] = new Expression.EvalResult((int)v);
                }

                obj["Pos"].intval = (int)fs.Position;

                return new Expression.EvalResult(read);
            }
        }

        internal class WriteFunction : FunctionStatement
        {
            System.IO.FileStream fs;

            public WriteFunction(System.IO.FileStream fstream)
            {
                fs = fstream;
                name = "Write";
                args = new List<FunctionArg> { new FunctionArg { name = "this", argtype = Expression.EvalResult.ResultType.Object },
                    new FunctionArg { name = "buf", argtype = Expression.EvalResult.ResultType.Array },
                    new FunctionArg { name = "offset", argtype = Expression.EvalResult.ResultType.Int },
                    new FunctionArg { name = "len", argtype = Expression.EvalResult.ResultType.Int } };
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                Dictionary<string, Expression.EvalResult> obj = passed_args[0].objval;
                List<Expression.EvalResult> buf = passed_args[1].arrval;
                int offset = passed_args[2].intval;
                int len = passed_args[3].intval;

                fs.Position = obj["Pos"].intval;
                byte[] csbuf = new byte[len];

                for (int i = 0; i < len; i++)
                {
                    int sidx = i + offset;
                    byte v = (byte)buf[sidx].intval;

                    csbuf[i] = v;
                }

                fs.Write(csbuf, 0, len);
                obj["Pos"].intval = (int)fs.Position;
                obj["Length"].intval = (int)fs.Length;

                return new Expression.EvalResult(0);
            }
        }
    }

    class TyProject : FunctionStatement
    {
        public TyProject()
        {
            name = "_typroject";
            args = new List<FunctionArg> { new FunctionArg { name = "projfile", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "config", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "tools_ver", argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { name = "unsafe", argtype = Expression.EvalResult.ResultType.Int } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> args)
        {
            string fname = args[0].strval;
            string config = args[1].strval;
            string tools_ver = args[2].strval;
            bool do_unsafe = (args[3].intval == 0) ? false : true;

            string cur_dir = Environment.CurrentDirectory;
            System.IO.FileInfo fi = new System.IO.FileInfo(fname);
            if (!fi.Exists)
            {
                throw new Exception("_typroject: " + fname + " does not exist");
            }

            typroject.Project p = typroject.Project.xml_read(fi.OpenRead(), config, fi.DirectoryName, cur_dir);

            Dictionary<string, Expression.EvalResult> ret = new Dictionary<string, Expression.EvalResult>();
            ret["OutputFile"] = new Expression.EvalResult(p.OutputFile);
            ret["OutputType"] = new Expression.EvalResult(p.output_type.ToString());
            ret["AssemblyName"] = new Expression.EvalResult(p.assembly_name);
            ret["Configuration"] = new Expression.EvalResult(p.configuration);
            ret["Defines"] = new Expression.EvalResult(p.defines);
            ret["Sources"] = new Expression.EvalResult(p.Sources);
            ret["GACReferences"] = new Expression.EvalResult(p.References);
            ret["ProjectReferences"] = new Expression.EvalResult(new string[] { });
            foreach (typroject.Project pref in p.ProjectReferences)
                ret["ProjectReferences"].arrval.Add(new Expression.EvalResult(pref.ProjectFile));
            ret["ProjectFile"] = new Expression.EvalResult(fi.FullName);
            ret["ProjectName"] = new Expression.EvalResult(p.ProjectName);

            BuildFunction build = new BuildFunction(p);
            ret[build.Mangle()] = new Expression.EvalResult(build);

            if (do_unsafe)
                build.do_unsafe = true;
            if (tools_ver != "")
                p.tools_ver = tools_ver;

            return new Expression.EvalResult(ret);
        }

        class BuildFunction : FunctionStatement
        {
            typroject.Project p;
            internal bool do_unsafe = false;

            public BuildFunction(typroject.Project proj)
            {
                name = "Build";
                args = new List<FunctionArg> { new FunctionArg { argtype = Expression.EvalResult.ResultType.Object, name = "this" } };
                p = proj;
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                return new Expression.EvalResult(p.build(new List<string>(), new List<string>(), new List<string>(), do_unsafe));
            }
        }
    }

}

namespace tymakeParse
{
    partial class Parser
    {
        internal Parser(Scanner s) : base(s) { }

        internal void AddDefine(string t, string val)
        {
            throw new NotImplementedException();
        }

        internal void AddDefine(string t, int val)
        {
            throw new NotImplementedException();
        }

        internal int ResolveAsInt(string t)
        {
            throw new NotImplementedException();
        }
    }

    partial class Scanner
    {
        public override void yyerror(string format, params object[] args)
        {
            throw new ParseException(String.Format(format, args) + " at line " + yyline + ", col " + yycol, yyline, yycol);
        }

        internal int sline { get { return yyline; } }
        internal int scol { get { return yycol; } }
    }

    public class ParseException : Exception
    {
        int l, c;
        public ParseException(string msg, int line, int col) : base(msg) { l = line; c = col; }
    }
}

