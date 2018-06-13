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
using System.Linq;
using System.Text;
using QUT.Gppg;

namespace tymake_lib
{
    public class TymakeLib
    {
        public static void InitMakeState(MakeState s,
            TextReader stdin = null,
            TextWriter stdout = null,
            TextWriter stderr = null)
        {
            s.stdin = stdin;
            s.stdout = stdout;
            s.stderr = stderr;

            new PrintFunction { args = new List<FunctionStatement.FunctionArg> { new FunctionStatement.FunctionArg { name = "val", argtype = Expression.EvalResult.ResultType.Int } } }.Execute(s);
            new PrintFunction { args = new List<FunctionStatement.FunctionArg> { new FunctionStatement.FunctionArg { name = "val", argtype = Expression.EvalResult.ResultType.String } } }.Execute(s);
            new PrintFunction { args = new List<FunctionStatement.FunctionArg> { new FunctionStatement.FunctionArg { name = "val", argtype = Expression.EvalResult.ResultType.Object } } }.Execute(s);
            new PrintFunction { args = new List<FunctionStatement.FunctionArg> { new FunctionStatement.FunctionArg { name = "val", argtype = Expression.EvalResult.ResultType.Array } } }.Execute(s);
            new SetColourFunction().Execute(s);
            new VarGenFunction(Expression.EvalResult.ResultType.Int).Execute(s);
            new VarGenFunction(Expression.EvalResult.ResultType.Array).Execute(s);
            new VarGenFunction(Expression.EvalResult.ResultType.String).Execute(s);
            new VarGenFunction(Expression.EvalResult.ResultType.Object).Execute(s);
            new VarGetFunction().Execute(s);
            new DefinedFunction().Execute(s);
            new DefineBlobFunction().Execute(s);
            new ToIntFunction().Execute(s);
            new DumpBlobFunction().Execute(s);
            new ToByteArrayFunction(4).Execute(s);
            new ToByteArrayFunction(2).Execute(s);
            new ToByteArrayFunction(1).Execute(s);
            new ToByteArrayFunction(8).Execute(s);
            new ThrowFunction().Execute(s);
            new InputFunction().Execute(s);
            new ExistsFunction().Execute(s);
            new RmFunction().Execute(s);
            new CopyFunction().Execute(s);
            new FOpenFunction().Execute(s);
            new MkDirFunction().Execute(s);
            new DirectoryFunction { name = "dir" }.Execute(s);
            new DirectoryFunction { name = "basefname" }.Execute(s);
            new DirectoryFunction { name = "ext" }.Execute(s);
            new DirectoryFunction { name = "files" }.Execute(s);
            new DirectoryFunction(2) { name = "files" }.Execute(s);
            new DirectoryFunction { name = "dirs" }.Execute(s);
            new DownloadFunction(false).Execute(s);
            new DownloadFunction(true).Execute(s);
            new ExtractFunction(false).Execute(s);
            new ExtractFunction(true).Execute(s);
            new ExitFunction().Execute(s);
            new StrToArrFunction().Execute(s);
            new ArrToStrFunction().Execute(s);
        }

        public static Expression.EvalResult ExecuteFile(string name, MakeState s)
        {
            // find the file by using search paths
            FileInfo fi = null;
            foreach (var sp in s.search_paths)
            {
                var test = sp + "/" + name;
                var test2 = sp + name;

                try
                {
                    fi = new FileInfo(test);
                    if (fi.Exists)
                        break;
                }
                catch (Exception) { }
                try
                {
                    fi = new FileInfo(test2);
                    if (fi.Exists)
                        break;
                }
                catch (Exception) { }
            }
            if (fi == null || fi.Exists == false)
                throw new Exception("included file: " + name + " not found");

            // add included files location to search paths
            s.search_paths.Insert(0, fi.DirectoryName);

            // set full name as THIS entry
            s.SetDefine("THIS", new Expression.EvalResult(fi.FullName), true);

            FileStream f = fi.OpenRead();
            Parser p = new Parser(new Scanner(f, fi.FullName));
            bool res = p.Parse();
            if (res == false)
                throw new Exception("Parse error");
            var ret = p.output.Execute(s);
            f.Close();
            return ret;
        }

        public static Expression.EvalResult ExecuteString(string str, MakeState s)
        {
            System.IO.MemoryStream f = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(str));
            Parser p = new Parser(new Scanner(f));
            bool res = p.Parse();
            if (res == false)
                throw new Exception("Parse error");
            return p.output.Execute(s);
        }

    }

    class VarGenFunction : FunctionStatement
    {
        internal static Dictionary<string, Expression.EvalResult> all_defs =
            new Dictionary<string, Expression.EvalResult>();

        public VarGenFunction(Expression.EvalResult.ResultType arg_type)
        {
            name = "vargen";
            args = new List<FunctionArg>() {
                new FunctionArg { argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { argtype = arg_type }
            };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            var n = passed_args[0].strval;

            s.SetDefine(n, passed_args[1], true);
            all_defs[n] = passed_args[1];

            return passed_args[1];
        }
    }

    class ToIntFunction : FunctionStatement
    {
        public ToIntFunction()
        {
            name = "toint";
            args = new List<FunctionArg>() { new FunctionArg { argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            return new Expression.EvalResult(int.Parse(passed_args[0].strval));
        }
    }

    class ThrowFunction : FunctionStatement
    {
        public ThrowFunction()
        {
            name = "throw";
            args = new List<FunctionArg> { new FunctionArg { argtype = Expression.EvalResult.ResultType.String }, new FunctionArg { argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            var obj_type = passed_args[0].strval;
            var msg = passed_args[1].strval;

            var obj_ts = Type.GetType(obj_type);
            var obj_ctor = obj_ts.GetConstructor(new Type[] { typeof(string) });
            var obj = obj_ctor.Invoke(new object[] { msg });
            throw obj as Exception;
        }
    }

    class ToByteArrayFunction : FunctionStatement
    {
        int bc = 0;

        public ToByteArrayFunction(int byte_count)
        {
            name = "tobytearray" + byte_count.ToString();
            args = new List<FunctionArg> { new FunctionArg { argtype = Expression.EvalResult.ResultType.Any } };
            bc = byte_count;
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            IList<byte> ret = GetBytes(passed_args[0]);
            if (ret == null)
                throw new Exception("Cannot call " + name + " with " + passed_args[0].ToString());

            List<Expression.EvalResult> r = new List<Expression.EvalResult>();
            for(int i = 0; i < bc; i++)
            {
                if (i < ret.Count)
                    r.Add(new Expression.EvalResult(ret[i]));
                else
                    r.Add(new Expression.EvalResult(0));
            }
            return new Expression.EvalResult(r);
        }

        private IList<byte> GetBytes(Expression.EvalResult e)
        {
            switch (e.Type)
            {
                case Expression.EvalResult.ResultType.Int:
                    return BitConverter.GetBytes(e.intval);
                case Expression.EvalResult.ResultType.String:
                    return Encoding.UTF8.GetBytes(e.strval);
                case Expression.EvalResult.ResultType.Array:
                    var ret = new List<byte>();
                    foreach (var aentry in e.arrval)
                        ret.AddRange(GetBytes(aentry));
                    return ret;
                default:
                    return null;
            }
        }
    }

    class VarGetFunction : FunctionStatement
    {
        public VarGetFunction()
        {
            name = "varget";
            args = new List<FunctionArg>()
            {
                new FunctionArg { argtype = Expression.EvalResult.ResultType.String }
            };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            return VarGenFunction.all_defs[passed_args[0].strval];
        }
    }

    class DefineBlobFunction : FunctionStatement
    {
        /* Used to generate a hash table
            
            Arguments are:
                name
                key
                value

            Hash table contains three parts:
                bucket list
                chain list
                data list
                    - concatenations of:
                        1 byte: length of key
                        key
                        value

        */

        public static Dictionary<string, HTable> tables = new Dictionary<string, HTable>();

        public class HTable
        {
            public List<byte> data = new List<byte>();
            public List<KeyIndex> keys = new List<KeyIndex>();
            
            public class KeyIndex
            {
                public List<byte> key = new List<byte>();
                public int idx;
                public uint hc;
            }
        }
        
        public DefineBlobFunction()
        {
            name = "defblob";
            args = new List<FunctionArg>()
            {
                new FunctionArg { argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { argtype = Expression.EvalResult.ResultType.Array },
                new FunctionArg { argtype = Expression.EvalResult.ResultType.Array }
            };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            var name = passed_args[0].strval;
            var key = passed_args[1].arrval;
            var value = passed_args[2].arrval;

            /* Coerce key and value to byte arrays */
            var key_b = ToByteArray(key);
            var value_b = ToByteArray(value);

            /* Get hash code */
            var hc = Hash(key_b);

            /* Get hash table */
            HTable ht;
            if(tables.TryGetValue(name, out ht) == false)
            {
                ht = new HTable();
                tables[name] = ht;
            }

            /* Build table entry */
            HTable.KeyIndex k = new HTable.KeyIndex();
            k.hc = hc;
            k.idx = ht.data.Count;
            k.key = key_b;
            ht.keys.Add(k);

            /* Add data entry */
            if (key_b.Count > 255)
                throw new Exception("key too large");
            ht.data.Add((byte)key_b.Count);
            ht.data.AddRange(key_b);
            ht.data.AddRange(value_b);

            return new Expression.EvalResult();
        }

        public static uint Hash(IEnumerable<byte> v)
        {
            uint h = 0;
            uint g = 0;

            foreach(var b in v)
            {
                h = (h << 4) + b;
                g = h & 0xf0000000U;
                if (g != 0)
                    h ^= g >> 24;
                h &= ~g;
            }
            return h;
        }

        private List<byte> ToByteArray(List<Expression.EvalResult> v)
        {
            List<byte> ret = new List<byte>();
            foreach(var b in v)
            {
                ToByteArray(b, ret);
            }
            return ret;
        }

        public List<byte> ToByteArray(Expression.EvalResult v)
        {
            List<byte> ret = new List<byte>();
            ToByteArray(v, ret);
            return ret;
        }

        public void CompressInt(int val, List<byte> ret)
        {
            var u = BitConverter.ToUInt32(BitConverter.GetBytes(val), 0);

            CompressUInt(u, ret);
        }

        public void CompressUInt(uint u, List<byte> ret)
        { 
            var b1 = u & 0xff;
            var b2 = (u >> 8) & 0xff;
            var b3 = (u >> 16) & 0xff;
            var b4 = (u >> 24) & 0xff;

            if (u <= 0x7fU)
            {
                ret.Add((byte)b1);
                return;
            }
            else if (u <= 0x3fffU)
            {
                ret.Add((byte)(b2 | 0x80U));
                ret.Add((byte)b1);
            }
            else if (u <= 0x1FFFFFFFU)
            {
                ret.Add((byte)(b4 | 0xc0U));
                ret.Add((byte)b3);
                ret.Add((byte)b2);
                ret.Add((byte)b1);
            }
            else
                throw new Exception("integer too large to compress");
        }

        public void ToByteArray(Expression.EvalResult v, List<byte> ret)
        {
            switch(v.Type)
            {
                case Expression.EvalResult.ResultType.Array:
                    foreach (var a in v.arrval)
                        ToByteArray(a, ret);
                    break;
                case Expression.EvalResult.ResultType.Int:
                    CompressInt((int)v.intval, ret);
                    /*ret.Add((byte)(v.intval & 0xff));
                    ret.Add((byte)((v.intval >> 8) & 0xff));
                    ret.Add((byte)((v.intval >> 16) & 0xff));
                    ret.Add((byte)((v.intval >> 24) & 0xff));*/
                    break;
                case Expression.EvalResult.ResultType.Object:
                    var vlist = new List<string>();
                    foreach (var kvp in v.objval)
                        vlist.Add(kvp.Key);
                    vlist.Sort();
                    foreach (var k in vlist)
                        ToByteArray(v.objval[k]);
                    break;
                case Expression.EvalResult.ResultType.String:
                    ret.AddRange(Encoding.UTF8.GetBytes(v.strval));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    class DumpBlobFunction : FunctionStatement
    {
        /* Dump the hash table defined with a DefBlob function */

        public DumpBlobFunction()
        {
            name = "dumpblob";
            args = new List<FunctionArg>()
            {
                new FunctionArg { argtype = Expression.EvalResult.ResultType.String },
                new FunctionArg { argtype = Expression.EvalResult.ResultType.String }
            };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            if(s.stdout == null)
            {
                if (s.stderr != null)
                    s.stderr.WriteLine("ERROR: dumpblob called without a valid stdout stream");
                return new Expression.EvalResult();
            }

            var blob_name = passed_args[0].strval;
            var hc_name = passed_args[1].strval;

            var hc = DefineBlobFunction.tables[blob_name];

            // Decide on a sensible value for nbuckets - sqroot(n)
            //  seems appropriate
            var nbuckets = (int)Math.Sqrt(hc.keys.Count);

            /* We need a map between key indices and data blob offsets
                - this is stored in idx_map.

               To find entries, we perform hc % nbuckets to get a
                bucket number.  Then index buckets[bucket_no] to get
                index of first item.  If it is not what we want, go to
                next item chain[cur_index] and so on.
            
               To create the hash table, therefore, we iterate through
                each key.  First, store its index to the idx_map.  Next,
                calculate bucket_no.  chain[cur_idx] is set to whatever
                is currently in buckets[bucket_no], and buckets[bucket_no]
                is updated to the current index.  This means the last item
                added will actually be the first out, and the first item
                will have its chain[] value set to -1 (the initial value
                of buckets[])
            */

            int[] buckets = new int[nbuckets];
            for (int i = 0; i < nbuckets; i++)
                buckets[i] = -1;
            int[] chain = new int[hc.keys.Count];
            int[] idx_map = new int[hc.keys.Count];

            for (int i = 0; i < hc.keys.Count; i++)
            {
                var hte = hc.keys[i];

                idx_map[i] = hte.idx;

                var bucket_no = hte.hc % (uint)nbuckets;

                var cur_bucket = buckets[bucket_no];
                chain[i] = cur_bucket;
                buckets[bucket_no] = i;
            }

            /* Now dump the hash table:
                var hc_name = new HashTable {
                    nbucket = nbuckets,
                    nchain = hc.keys.Count,
                    bucket = new byte[] {
                        bucket_dump
                    },
                    chain = new byte[] {
                        chain_dump
                    },
                    idx_map = new byte[] {
                        idx_map_dump
                    },
                    data = new byte[] {
                        data_dump
                    }
                };
            */

            s.stdout.Write("\t\t\tvar " + hc_name + " = new HashTable {\n");
            s.stdout.Write("\t\t\t\tnbucket = " + nbuckets.ToString() + ",\n");
            s.stdout.Write("\t\t\t\tnchain = " + hc.keys.Count.ToString() + ",\n");
            s.stdout.Write("\t\t\t\tbucket = new int[] {\n");
            DumpArray<int>(buckets, s);
            s.stdout.Write("\t\t\t\t},\n");
            s.stdout.Write("\t\t\t\tchain = new int[] {\n");
            DumpArray<int>(chain, s);
            s.stdout.Write("\t\t\t\t},\n");
            s.stdout.Write("\t\t\t\tidx_map = new int[] {\n");
            DumpArray<int>(idx_map, s);
            s.stdout.Write("\t\t\t\t},\n");
            s.stdout.Write("\t\t\t\tdata = new byte[] {\n");
            DumpArray<byte>(hc.data, "\t\t\t\t\t", 16, s);
            s.stdout.Write("\t\t\t\t},\n");
            s.stdout.Write("\t\t\t};\n");
            s.stdout.Write("\n");

            return new Expression.EvalResult();
        }

        void DumpArray<T>(IList<T> arr, MakeState s)
        {
            DumpArray(arr, "\t\t\t\t\t", 8, s);
        }

        void DumpArray<T>(IList<T> arr, string line_prefix, int per_line, MakeState s)
        {
            int cur_line = 0;

            for(int i = 0; i < arr.Count; i++)
            {
                var b = arr[i];

                if (cur_line == 0)
                    s.stdout.Write(line_prefix);

                s.stdout.Write(b.ToString());
                s.stdout.Write(", ");

                cur_line++;
                if(cur_line == per_line)
                {
                    s.stdout.Write("\n");
                    cur_line = 0;
                }
            }

            if (cur_line != 0)
                s.stdout.Write("\n");
        }
    }

    class SetColourFunction : FunctionStatement
    {
        public SetColourFunction()
        {
            name = "setoutputcolor";
            args = new List<FunctionArg> { new FunctionArg { name = "color", argtype = Expression.EvalResult.ResultType.Array } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            if(s.stdout == null || !s.stdout.Equals(Console.Out))
            {
                if (s.stderr != null)
                    s.stderr.WriteLine("ERROR: setoutputcolor called without a console for output");
                return new Expression.EvalResult();
            }

            string fground = "reset";
            string bground = "reset";
            if (passed_args[0].arrval.Count == 0)
            {
            }
            else if (passed_args[0].arrval.Count == 1)
            {
                if(passed_args[0].arrval[0].strval != null)
                    fground = passed_args[0].arrval[0].strval.ToLower();
            }
            else if (passed_args[0].arrval.Count == 2)
            {
                if (passed_args[0].arrval[0].strval != null)
                    fground = passed_args[0].arrval[0].strval.ToLower();
                if (passed_args[0].arrval[1].strval != null)
                    bground = passed_args[0].arrval[1].strval.ToLower();
            }
            else
            {
                if (s.stderr != null)
                    s.stderr.WriteLine("ERROR: setoutputcolor called without a two member array as input");
                return new Expression.EvalResult();
            }

            Console.ResetColor();

            if (!IsDefault(fground))
            {
                var fgc = GetColor(fground, s);
                if(fgc != (ConsoleColor)(-1))
                    Console.ForegroundColor = fgc;
            }
            if (!IsDefault(bground))
            {
                var bgc = GetColor(bground, s);
                if(bgc != (ConsoleColor)(-1))
                    Console.BackgroundColor = bgc;
            }

            return new Expression.EvalResult();
        }

        private bool IsDefault(string fground)
        {
            if (fground == "default" || fground == "reset" || fground == "")
                return true;
            return false;
        }

        private ConsoleColor GetColor(string col, MakeState s)
        {
            if (col.StartsWith("light"))
                col = col.Substring(5);
            var cc_flds = typeof(ConsoleColor).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach(var cc_fld in cc_flds)
            {
                if(cc_fld.Name.ToLower() == col)
                {
                    var v = (int)cc_fld.GetValue(null);
                    return (ConsoleColor)v;
                }
            }
            if (s.stderr != null)
                s.stderr.WriteLine("ERROR: setoutputcolor unknown color " + col);
            return (ConsoleColor)(-1);
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
            if (s.stdout == null)
            {
                if (s.stderr != null)
                    s.stderr.WriteLine("ERROR: print called without a valid stdout stream");
                return new Expression.EvalResult();
            }

            Print(args[0], true, s);

            return new Expression.EvalResult();
        }

        void Print(Expression.EvalResult e, bool toplevel, MakeState s)
        {
            switch (e.Type)
            {
                case Expression.EvalResult.ResultType.Int:
                    s.stdout.Write(e.intval);
                    break;
                case Expression.EvalResult.ResultType.String:
                    if (!toplevel)
                        s.stdout.Write("\"");
                    s.stdout.Write(e.strval);
                    if (!toplevel)
                        s.stdout.Write("\"");
                    break;
                case Expression.EvalResult.ResultType.Array:
                    s.stdout.Write("[ ");
                    for (int i = 0; i < e.arrval.Count; i++)
                    {
                        if (i != 0)
                            s.stdout.Write(", ");
                        Print(e.arrval[i], false, s);
                    }
                    s.stdout.Write(" ]");
                    break;
                case Expression.EvalResult.ResultType.Object:
                    s.stdout.Write("[ ");
                    int j = 0;
                    foreach (KeyValuePair<string, Expression.EvalResult> kvp in e.objval)
                    {
                        if (j != 0)
                            s.stdout.Write(", ");
                        s.stdout.Write(kvp.Key);
                        s.stdout.Write(": ");
                        Print(kvp.Value, false, s);
                        j++;
                    }
                    s.stdout.Write(" ]");
                    break;
            }
        }
    }

    class ExitFunction : FunctionStatement
    {
        public ExitFunction()
        {
            name = "exit";
            args = new List<FunctionArg> { new FunctionArg { name = "ec", argtype = Expression.EvalResult.ResultType.Int } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            System.Environment.Exit((int)passed_args[0].AsInt);
            return new Expression.EvalResult();
        }
    }

    class InputFunction : FunctionStatement
    {
        public InputFunction()
        {
            name = "input";
            args = new List<FunctionArg>();
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            if (s.stdin == null)
            {
                if (s.stderr != null)
                    s.stderr.WriteLine("ERROR: input called without a valid stdin stream");
                return new Expression.EvalResult("");
            }

            string ret = s.stdin.ReadLine();
            return new Expression.EvalResult(ret);
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

    class DefinedFunction : FunctionStatement
    {
        public DefinedFunction()
        {
            name = "defined";
            args = new List<FunctionArg> { new FunctionArg { name = "varname", argtype = Expression.EvalResult.ResultType.Any } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            if (passed_args[0].Type == Expression.EvalResult.ResultType.Void ||
                passed_args[0].Type == Expression.EvalResult.ResultType.Undefined)
                return new Expression.EvalResult(0);
            else
                return new Expression.EvalResult(1);
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
            //return new Expression.EvalResult(-1);
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
        public DirectoryFunction(int pcount = 1)
        {
            args = new List<FunctionArg>();
            for (int i = 0; i < pcount; i++)
                args.Add(new FunctionArg { name = "fname", argtype = Expression.EvalResult.ResultType.String });
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(passed_args[0].strval);
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
            else if(name == "files")
            {
                DirectoryInfo di = new DirectoryInfo(passed_args[0].strval);

                List<Expression.EvalResult> ret = new List<Expression.EvalResult>();
                if(di.Exists)
                {
                    if (passed_args.Count == 2)
                    {
                        foreach (var f in di.GetFiles(passed_args[1].strval))
                        {
                            ret.Add(new Expression.EvalResult(f.FullName));
                        }
                    }
                    else
                    {
                        foreach (var f in di.GetFiles())
                        {
                            ret.Add(new Expression.EvalResult(f.FullName));
                        }
                    }
                }

                return new Expression.EvalResult(ret);
            }
            else if (name == "dirs")
            {
                DirectoryInfo di = new DirectoryInfo(passed_args[0].strval);

                List<Expression.EvalResult> ret = new List<Expression.EvalResult>();
                if (di.Exists)
                {
                    foreach (var f in di.GetDirectories())
                    {
                        ret.Add(new Expression.EvalResult(f.FullName));
                    }
                }

                return new Expression.EvalResult(ret);
            }

            throw new Exception("Unsupported function");
        }
    }

    class MkDirFunction : FunctionStatement
    {
        public MkDirFunction()
        {
            name = "mkdir";
            args = new List<FunctionArg> { new FunctionArg { name = "dirname", argtype = Expression.EvalResult.ResultType.String } };
        }

        void create(DirectoryInfo di)
        {
            if (di.Parent.Exists == false)
                create(di.Parent);
            di.Create();
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(passed_args[0].strval);
                create(di);
                di.Refresh();
                if (di.Exists)
                    return new Expression.EvalResult(0);
                else
                    return new Expression.EvalResult(-1);
            }
            catch(Exception)
            {
                return new Expression.EvalResult(-1);
            }
            return new Expression.EvalResult();
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

            CloseFunction cf = new CloseFunction(fs);
            ret[cf.Mangle()] = new Expression.EvalResult(cf);

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
                var offset = (int)passed_args[2].intval;
                var len = (int)passed_args[3].intval;

                fs.Position = obj["Pos"].intval;
                byte[] csbuf = new byte[len];
                int read = fs.Read(csbuf, 0, len);

                for (int i = 0; i < read; i++)
                {
                    byte v = csbuf[i];
                    var didx = i + offset;

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
                int offset = (int)passed_args[2].intval;
                int len = (int)passed_args[3].intval;

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

        internal class CloseFunction : FunctionStatement
        {
            System.IO.FileStream fs;

            public CloseFunction(System.IO.FileStream fstream)
            {
                fs = fstream;
                name = "Close";
                args = new List<FunctionArg> { new FunctionArg { name = "this", argtype = Expression.EvalResult.ResultType.Object } };
            }

            public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
            {
                fs.Close();
                return new Expression.EvalResult(0);
            }
        }
    }

    class DownloadFunction : FunctionStatement
    {
        public DownloadFunction(bool allow_cache_arg)
        {
            name = "download";
            if(allow_cache_arg)
                args = new List<FunctionArg> { new FunctionArg { name = "url", argtype = Expression.EvalResult.ResultType.String }, new FunctionArg { name = "allow_cache", argtype = Expression.EvalResult.ResultType.Int } };
            else
                args = new List<FunctionArg> { new FunctionArg { name = "url", argtype = Expression.EvalResult.ResultType.String } };
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            try
            {
                var uri = new Uri(passed_args[0].strval);
                var cache_rel_path = uri.Scheme + "/" + uri.Host + uri.AbsolutePath;
                var temp = Path.GetTempPath() + "/tymake/download";
                var cache_path = temp + "/" + cache_rel_path;
                DirectoryInfo di = new DirectoryInfo(cache_path);
                if (!di.Exists)
                    di.Create();
                var fname = uri.Segments[uri.Segments.Length - 1];
                FileInfo fi = new FileInfo(di.FullName + "/" + fname);

                if (passed_args.Count >= 2 && passed_args[1].intval != 0)
                {
                    // Try and fetch from download cache
                    if (fi.Exists)
                        return new Expression.EvalResult(fi.FullName);
                }

                // Else download
                var wc = new System.Net.WebClient();
                wc.DownloadFile(uri, fi.FullName);

                return new Expression.EvalResult(fi.FullName);
            }
            catch(Exception e)
            {
                if (s.stderr != null)
                    s.stderr.WriteLine("ERROR: download: " + e.ToString());
                return new Expression.EvalResult();
            }
        }
    }

    class ExtractFunction : FunctionStatement
    {
        public ExtractFunction(bool allow_use_cache)
        {
            name = "extract";

            args = new List<FunctionArg> { new FunctionArg { name = "archive", argtype = Expression.EvalResult.ResultType.String } };
            if (allow_use_cache)
                args.Add(new FunctionArg { argtype = Expression.EvalResult.ResultType.Int, name = "use_cache" });

            /* Clean out old extracted files */
            var temp = Path.GetTempPath() + "/tymake/extract/" + Path.GetRandomFileName();
            DirectoryInfo di = new DirectoryInfo(temp);
            if(di.Exists)
            {
                foreach (var x in di.GetDirectories())
                {
                    if((DateTime.Now - x.CreationTime).TotalDays > 7)
                        x.Delete();
                }
            }
        }

        public override Expression.EvalResult Run(MakeState s, List<Expression.EvalResult> passed_args)
        {
            DirectoryInfo di = null;

            if(passed_args.Count == 2 && passed_args[1].intval != 0)
            {
                // using cache
                di = new DirectoryInfo(passed_args[0].strval + ".extcache");
                if (di.Exists)
                {
                    // check it is newer than the source archive
                    var src_fi = new FileInfo(passed_args[0].strval);

                    if (src_fi.LastWriteTime <= di.LastWriteTime)
                        return new Expression.EvalResult(di.FullName);
                    else
                        di.Delete(true);
                }
                try
                {
                    di.Create();
                }
                catch (Exception)
                {
                    di = null;
                }
            }

            if (di == null)
            {
                var temp = Path.GetTempPath() + "/tymake/extract/" + Path.GetRandomFileName();
                while ((di = new DirectoryInfo(temp)).Exists)
                    temp = Path.GetTempPath() + "/tymake/extract/" + Path.GetRandomFileName();
                di.Create();
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(passed_args[0].strval, di.FullName);

            return new Expression.EvalResult(di.FullName);
        }
    }

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

        protected override void PostDoAction()
        {
            LocationBase e = null;
            if(CurrentSemanticValue.exprval != null)
                e = CurrentSemanticValue.exprval;
            else if(CurrentSemanticValue.stmtval != null)
                e = CurrentSemanticValue.stmtval;
            if(e != null)
            {
                e.scol = CurrentLocationSpan.StartColumn;
                e.sline = CurrentLocationSpan.StartLine;
                e.ecol = CurrentLocationSpan.EndColumn;
                e.eline = CurrentLocationSpan.EndLine;
                e.fname = ((Scanner)Scanner).filename;
            }
        }
    }

    partial class Scanner
    {
        internal string filename;
        internal Scanner(Stream file, string fname) : this(file) { filename = fname; }

        public override void yyerror(string format, params object[] args)
        {
            var stext = String.Format(format, args);
            stext = stext.Replace("LABEL", tokTxt);
            throw new ParseException(stext + " at line " + yyline + ", col " + yycol + " in " + filename, yyline, yycol);
        }

        internal int sline { get { return yyline; } }
        internal int scol { get { return yycol; } }

        public override LexLocation yylloc
        {
            get
            {
                return new LexLocation(tokLin, tokCol, tokELin, tokECol);
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }

    public class ParseException : Exception
    {
        int l, c;
        public ParseException(string msg, int line, int col) : base(msg) { l = line; c = col; }
    }
}
