using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace make_x86_opcodes
{
    class Program
    {
        static List<string> ret = new List<string>();
        static Dictionary<string, List<string>> ret2 = new Dictionary<string, List<string>>();
        static Dictionary<string, string> tysila_types = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            InitTysilaTypes();

            XDocument xd = XDocument.Load("x86reference.xml");
            var one_byte = xd.Element("x86reference").Element("one-byte");
            var one_byte_opcodes = one_byte.Descendants("pri_opcd");
            var two_byte_opcodes = xd.Element("x86reference").Element("two-byte").Descendants("pri_opcd");

            AddEntries(one_byte_opcodes, false);
            AddEntries(two_byte_opcodes, true);

            /* Write out */
            StreamWriter sw = new StreamWriter(new FileStream("../../../../libtysila/x86_64/x86_64_asm_instrs.cs", FileMode.Create, FileAccess.Write));
            sw.WriteLine("/* Produced by make_x86_opcodes based upon the x86reference.xml file");
            sw.WriteLine(" *  provided by Karel Lejska (http://ref.x86asm.net) - see the Licence");
            sw.WriteLine(" *  there */");
            sw.WriteLine();
            sw.WriteLine("using System.Collections.Generic;");
            sw.WriteLine();
            sw.WriteLine("namespace libtysila");
            sw.WriteLine("{");
            sw.WriteLine("\tpartial class x86_64_Assembler");
            sw.WriteLine("\t{");
            sw.WriteLine("\t\tpartial class x86_64_TybelNode");
            sw.WriteLine("\t\t{");
            sw.WriteLine("\t\t\tstatic void InitX86Opcodes()");
            sw.WriteLine("\t\t\t{");

            foreach (string instr_def in ret)
            {
                sw.Write("\t\t\t\t");
                sw.WriteLine(instr_def);
            }

            sw.WriteLine();
            foreach (KeyValuePair<string, List<string>> kvp in ret2)
            {
                sw.Write("\t\t\t\tinstr_choices[\"");
                sw.Write(kvp.Key);
                sw.Write("\"] = new List<inst_def> { ");

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    if (i != 0)
                        sw.Write(", ");
                    sw.Write("instrs[\"");
                    sw.Write(kvp.Value[i]);
                    sw.Write("\"]");
                }

                sw.WriteLine(" };");
            }
            sw.WriteLine("\t\t\t}");

            sw.WriteLine();
            sw.WriteLine("\t\t\tpublic enum opcode {");
            int c = 0;
            foreach (string op in ret2.Keys)
            {
                if (c == 0)
                    sw.Write("\t\t\t\t");
                else
                    sw.Write(", ");

                sw.Write(op.Replace(".", "").ToUpper());
                c++;
                if (c == 8)
                {
                    sw.WriteLine(",");
                    c = 0;
                }
            }
            if (c != 0)
                sw.WriteLine();
            sw.WriteLine("\t\t\t};");

            sw.WriteLine("\t\t}");
            sw.WriteLine("\t}");
            sw.WriteLine("}");
            sw.WriteLine();

            sw.Close();
        }

        private static void AddEntries(IEnumerable<XElement> opcodes, bool two_bytes)
        {
            foreach (var opcode in opcodes)
            {
                byte opcode_byte = Byte.Parse(opcode.Attribute("value").Value, System.Globalization.NumberStyles.HexNumber);
                List<byte> opcode_bytes = new List<byte> { opcode_byte };
                if(two_bytes)
                    opcode_bytes.Insert(0, 0x0f);

                var entries = opcode.Descendants("entry");
                foreach (var entry in entries)
                {
                    bool sign_ext = false;
                    var se = entry.Attribute("sign-ext");
                    if (se != null)
                        sign_ext = se.Value == "1";
                    bool op_size = false;
                    var os = entry.Attribute("op_size");
                    if (os != null)
                        op_size = os.Value == "1";

                    List<byte> new_opcode_bytes = new List<byte>(opcode_bytes);

                    var pref_opcode = entry.Element("pref");
                    if (pref_opcode != null)
                        new_opcode_bytes.Insert(0, Byte.Parse(pref_opcode.Value, System.Globalization.NumberStyles.HexNumber));

                    var sec_opcode = entry.Element("sec_opcd");
                    if (sec_opcode != null)
                        new_opcode_bytes.Add(Byte.Parse(sec_opcode.Value, System.Globalization.NumberStyles.HexNumber));

                    AddEntry(entry, new_opcode_bytes);
                }
            }
        }

        private static void AddEntry(XElement entry, List<byte> opcode_bytes)
        {
            var syntax = entry.Element("syntax");

            var v_fixed_r = entry.Element("opcd_ext");

            var v_name = syntax.Element("mnem");
            if (v_name != null)
            {
                string name = v_name.Value.ToLower();

                var v_src = syntax.Elements("src");
                var v_dst = syntax.Elements("dst");
                List<XElement> ops = new List<XElement>(v_dst);
                List<string> op_types = new List<string>();
                ops.AddRange(v_src);

                StringBuilder sb = new StringBuilder();
                sb.Append("instrs[\"");

                StringBuilder obj_name = new StringBuilder();
                obj_name.Append(name);

                List<int> dest_indices = new List<int>();
                List<int> src_indices = new List<int>();
                int idx = 0;

                foreach (XElement op in ops)
                {
                    if ((op.Attribute("displayed") == null) || (op.Attribute("displayed").Value == "yes"))
                    {
                        if (v_dst.Contains<XElement>(op))
                            dest_indices.Add(idx++);
                        else
                            src_indices.Add(idx++);

                        obj_name.Append("_");
                        string op_type = InterpretOperandCode(op);
                        op_types.Add(op_type);
                        obj_name.Append(op_type);
                    }
                }

                sb.Append(obj_name);
                sb.Append("\"] = new inst_def { name = \"");
                sb.Append(name);
                sb.Append("\", int_name = \"");
                sb.Append(obj_name);
                sb.Append("\", opcodes = new byte[] { ");
                for (int i = 0; i < opcode_bytes.Count; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append("0x");
                    sb.Append(opcode_bytes[i].ToString("X2"));
                }
                sb.Append(" }, ops = new inst_def.optype[] { ");
                for (int i = 0; i < op_types.Count; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(TysilaType(op_types[i]));
                }
                sb.Append(" }, dest_indices = new int[] { ");
                for (int i = 0; i < dest_indices.Count; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(dest_indices[i].ToString());
                }
                sb.Append(" }, src_indices = new int[] { ");
                for (int i = 0; i < src_indices.Count; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(src_indices[i].ToString());
                }

                sb.Append(" }");

                if (v_fixed_r != null)
                {
                    sb.Append(", fixed_r = ");
                    sb.Append(v_fixed_r.Value);
                }
                sb.Append(" };");

                ret.Add(sb.ToString());

                if (!ret2.ContainsKey(name))
                    ret2[name] = new List<string>();
                ret2[name].Add(obj_name.ToString());
            }
        }

        private static string TysilaType(string src_type)
        {
            if (tysila_types.ContainsKey(src_type))
                return tysila_types[src_type];

            else if (src_type.StartsWith("rm"))
                return "inst_def.optype.RM";


            else return "inst_def.optype.FixedReg";
        }

        enum AddressingMethod { ptr, m, CRn, DRn, rm, STim, STi, r, imm, rel, mm, moffs, mmm64, Sreg, TRn, xmm, xmmm };
        enum OperandType { O1632and1632, O8, O80dec, O32, O32int, O128, O3264, O64real, O1428, O80real, O161632, O64, O16163264,
            O64int, O32real, O94108, O512, O6416, O163264, O1632, O16, O16int, O16and3264 };

        static string InterpretOperandCode(XElement op)
        {
            var a = op.Element("a");
            var t = op.Element("t");
            if ((a != null) && (t != null))
                return InterpretOperandCode(a.Value, t.Value);
            else
                return op.Value.ToLower().Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(":", "");
        }

        static string InterpretOperandCode(string a, string t)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(InterpretA(a).ToString());
            sb.Append(InterpretT(t).ToString().Substring(1));

            return sb.ToString();
        }

        static AddressingMethod InterpretA(string a)
        {
            if (a == "A")
                return AddressingMethod.ptr;
            else if (a == "BA")
                return AddressingMethod.m;
            else if (a == "BB")
                return AddressingMethod.m;
            else if (a == "BD")
                return AddressingMethod.m;
            else if (a == "C")
                return AddressingMethod.CRn;
            else if (a == "D")
                return AddressingMethod.DRn;
            else if (a == "E")
                return AddressingMethod.rm;
            else if (a == "ES")
                return AddressingMethod.STim;
            else if (a == "EST")
                return AddressingMethod.STi;
            else if (a == "G")
                return AddressingMethod.r;
            else if (a == "H")
                return AddressingMethod.r;
            else if (a == "I")
                return AddressingMethod.imm;
            else if (a == "J")
                return AddressingMethod.rel;
            else if (a == "M")
                return AddressingMethod.m;
            else if (a == "N")
                return AddressingMethod.mm;
            else if (a == "O")
                return AddressingMethod.moffs;
            else if (a == "P")
                return AddressingMethod.mm;
            else if (a == "Q")
                return AddressingMethod.mmm64;
            else if (a == "R")
                return AddressingMethod.r;
            else if (a == "S")
                return AddressingMethod.Sreg;
            else if (a == "T")
                return AddressingMethod.TRn;
            else if (a == "U")
                return AddressingMethod.xmm;
            else if (a == "V")
                return AddressingMethod.xmm;
            else if (a == "W")
                return AddressingMethod.xmmm;
            else if (a == "X")
                return AddressingMethod.m;
            else if (a == "Y")
                return AddressingMethod.m;
            else if (a == "Z")
                return AddressingMethod.r;

            else
                throw new NotSupportedException();
        }

        static OperandType InterpretT(string t)
        {
            if (t == "a")
                return OperandType.O1632and1632;
            else if (t == "b")
                return OperandType.O8;
            else if (t == "bcd")
                return OperandType.O80dec;
            else if (t == "bs")
                return OperandType.O8;
            else if (t == "bss")
                return OperandType.O8;
            else if (t == "d")
                return OperandType.O32;
            else if (t == "di")
                return OperandType.O32int;
            else if (t == "dq")
                return OperandType.O128;
            else if (t == "dqp")
                return OperandType.O3264;
            else if (t == "dr")
                return OperandType.O64real;
            else if (t == "ds")
                return OperandType.O32;
            else if (t == "e")
                return OperandType.O1428;
            else if (t == "er")
                return OperandType.O80real;
            else if (t == "p")
                return OperandType.O161632;
            else if (t == "pd")
                return OperandType.O128;
            else if (t == "pi")
                return OperandType.O64;
            else if (t == "ps")
                return OperandType.O128;
            else if (t == "psq")
                return OperandType.O64;
            else if (t == "ptp")
                return OperandType.O16163264;
            else if (t == "q")
                return OperandType.O64;
            else if (t == "qi")
                return OperandType.O64int;
            else if (t == "qp")
                return OperandType.O64;
            else if (t == "s")
                return OperandType.O16and3264;
            else if (t == "sd")
                return OperandType.O64real;
            else if (t == "ss")
                return OperandType.O32real;
            else if (t == "sr")
                return OperandType.O32real;
            else if (t == "st")
                return OperandType.O94108;
            else if (t == "stx")
                return OperandType.O512;
            else if (t == "v")
                return OperandType.O1632;
            else if (t == "vds")
                return OperandType.O1632;
            else if (t == "vq")
                return OperandType.O6416;
            else if (t == "vqp")
                return OperandType.O163264;
            else if (t == "vs")
                return OperandType.O1632;
            else if (t == "w")
                return OperandType.O16;
            else if (t == "wi")
                return OperandType.O16int;

            else
                throw new NotSupportedException();
        }

        private static void InitTysilaTypes()
        {
            tysila_types["r8"] = "inst_def.optype.R8";
            tysila_types["r16"] = "inst_def.optype.R16";
            tysila_types["r32"] = "inst_def.optype.R32";
            tysila_types["r3264"] = "inst_def.optype.R32";
            tysila_types["r163264"] = "inst_def.optype.R32";
            tysila_types["r64"] = "inst_def.optype.R64";
            tysila_types["rm8"] = "inst_def.optype.RM8";
            tysila_types["rm16"] = "inst_def.optype.RM16";
            tysila_types["rm32"] = "inst_def.optype.RM32";
            tysila_types["rm64"] = "inst_def.optype.RM64";
            tysila_types["rm1632"] = "inst_def.optype.RM32";
            tysila_types["rm163264"] = "inst_def.optype.RM32";
            tysila_types["rm3264"] = "inst_def.optype.RM32";
            tysila_types["rm6416"] = "inst_def.optype.RM64";
            tysila_types["imm8"] = "inst_def.optype.Imm8";
            tysila_types["imm16"] = "inst_def.optype.Imm16";
            tysila_types["imm32"] = "inst_def.optype.Imm32";
            tysila_types["imm64"] = "inst_def.optype.Imm64";
            tysila_types["imm1632"] = "inst_def.optype.Imm32";
            tysila_types["imm163264"] = "inst_def.optype.Imm32";
            tysila_types["rel1632"] = "inst_def.optype.Rel32";
            tysila_types["rel32"] = "inst_def.optype.Rel32";
            tysila_types["rel8"] = "inst_def.optype.Rel8";
        }
    }
}
