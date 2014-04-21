/* Copyright (C) 2013 by John Cronin
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

/* Definitions:
 * 
 * E: line
 * D: directive
 * P: parameters
 * N: newline
 * X: prefix
 * O: operator
 * L: label
 * M: command
 * R: register
 * G: number
 * U: numop
 * 
 * Block: E*
 * 
 * E: N | M | LN | L':'N | L':'M
 * L: terminal that starts with [a-zA-Z_.] and is not in directives, prefixes, operations or registers
 * D: ['['] terminal that starts with [a-zA-Z_.] and is in directives [']']
 * X: terminal that starts with [a-zA-Z_.] and is in prefixes
 * O: terminal that starts with [a-zA-Z_.] and is in operators
 * R: terminal that starts with [a-zA-Z_.] and is in registers
 * M: D P* N | X* O P* N
 * P: R | L | G | PUP | '['P']'
 * the above is converted to:
 * P: RP' | LP' | GP' | '['P']' (ContentsOf)
 * P': UP | empty/anything else
 * to remove left recursion
 * G: terminal starting with [0-9]
 * U: '+' | '-' | '*' | '/' | '>>=' | '<<=' | '>>' | '<<' | '!' | '~' | '&' | '|' | '^'
 */

using System;
using System.Collections.Generic;
using System.Text;
using tydisasm;

namespace tyasm
{
    class AsmParser
    {
        public class ParseOutput
        {
            public string Name { get { if (name == null) return Type.ToString(); else return name; } set { name = value; } }
            string name;
            public enum OutputType { Block, Line, Directive, Parameter, Label, Prefix, Operation, DirectiveCommand, OpCommand, Register, Number, NumOp, ContentsOf, Expression, ParameterPrime };
            public OutputType Type;
            public List<ParseOutput> Children = new List<ParseOutput>();

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Type.ToString());
                if (!Type.ToString().Equals(name))
                {
                    sb.Append(": ");
                    sb.Append(name);
                }
                if (Children.Count != 0)
                {
                    sb.Append("(");
                    for (int i = 0; i < Children.Count; i++)
                    {
                        if (i != 0)
                            sb.Append(", ");
                        sb.Append(Children[i].ToString());
                    }
                    sb.Append(")");
                }
                return sb.ToString();
            }
        }
        
        static AsmParser()
        {
            Initx86();
        }

        internal static ParseOutput Parse(IList<Tokenizer.TokenDefinition> input)
        {
            int i = 0;
            return ParseBlock(input, ref i);
        }

        internal static ParseOutput ParseBlock(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            // Match Line*
            ParseOutput ret = new ParseOutput { Type = ParseOutput.OutputType.Block };
            ParseOutput cur_line;
            while ((cur_line = ParseLine(input, ref i)) != null)
                ret.Children.Add(cur_line);
            return ret;
        }

        internal static ParseOutput ParseLine(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            // Line : EndOfInput | NewLine | Command | Label NewLine | Label ':' NewLine | Label ':' Command
            ParseOutput ret = new ParseOutput { Type = ParseOutput.OutputType.Line };

            // Match EndOfInput
            if (i >= input.Count)
                return null;

            int start_i = i;
            // Try and match NewLine
            if (MatchNewLine(input, ref i))
                return ret;

            // Try and match Command
            ParseOutput cmd = ParseCommand(input, ref i);
            if(cmd != null)
            {
                ret.Children.Add(cmd);
                return ret;
            }

            // Else rewind and try and match label
            i = start_i;
            ParseOutput label = ParseLabel(input, ref i);
            if (label != null)
            {
                // Try and match ':'
                if (MatchString(input, ":", ref i))
                {
                    // Try and match newline
                    if (MatchNewLine(input, ref i))
                    {
                        ret.Children.Add(label);
                        return ret;
                    }
                    else if ((cmd = ParseCommand(input, ref i)) != null)
                    {
                        ret.Children.Add(label);
                        ret.Children.Add(cmd);
                        return ret;
                    }
                    else
                    {
                        // Rewind and fail
                        i = start_i;
                        return null;
                    }
                }
                else if (MatchNewLine(input, ref i))
                {
                    ret.Children.Add(label);
                    return ret;
                }
                else
                {
                    // Rewind and fail
                    i = start_i;
                    return null;
                }
            }

            // Else rewind and fail
            i = start_i;
            return null;
        }

        internal static ParseOutput ParseCommand(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            // Command: Directive Parameters* NewLine | Prefix* Operation Parameters* NewLine
            int start_i = i;
            ParseOutput ret = new ParseOutput { Type = ParseOutput.OutputType.OpCommand };
            
            ParseOutput dir;
            ParseOutput param;
           
            if((dir = ParseDirective(input, ref i)) != null)
            {
                ret.Children.Add(dir);

                // Match Parameters

                while ((param = ParseParameter(input, ref i, true)) != null)
                    ret.Children.Add(param);
                
                // Match NewLine
                if (MatchNewLine(input, ref i))
                {
                    ret.Type = ParseOutput.OutputType.DirectiveCommand;
                    return ret;
                }
                else
                {
                    // Rewind and fail
                    i = start_i;
                    return null;
                }
            }

            // Else try and match prefixes
            ParseOutput prefix;
            while ((prefix = ParsePrefix(input, ref i)) != null)
                ret.Children.Add(prefix);

            // Match Operation
            ParseOutput op = ParseOperation(input, ref i);
            if (op == null)
            {
                // Rewind and fail
                i = start_i;
                return null;
            }
            ret.Children.Add(op);

            // Match parameters
            while ((param = ParseParameter(input, ref i, false)) != null)
                ret.Children.Add(param);

            // Match NewLine
            if (MatchNewLine(input, ref i))
                return ret;
            else
            {
                // Rewind and fail
                i = start_i;
                return null;
            }
        }

        internal static ParseOutput ParseLabel(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            if (input[i].Type != Tokenizer.TokenDefinition.def_type.String)
                return null;
            string s = input[i].String.ToLower();
            if (s.Length == 0)
                return null;
            if ((s[0] != '_') && (s[0] != '.') && (!char.IsLetter(s[0])))
                return null;
            if (directives.Contains(s))
                return null;
            if (prefixes.Contains(s))
                return null;
            if (registers.Contains(s))
                return null;
            if (operations.Contains(s))
                return null;
            i++;
            return new ParseOutput { Type = ParseOutput.OutputType.Label, Name = input[i - 1].String };
        }

        internal static ParseOutput ParseDirective(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            // Directive: directive | '[' directive ']' | '%' directive
            int start_i = i;
            string str;
            if (((str = MatchString(input, directives, ref i)) != null) || ((MatchString(input, "[", ref i)) &&
                ((str = MatchString(input, sq_bracket_directives, ref i)) != null) && (MatchString(input, "]", ref i))) ||
                ((MatchString(input, "%", ref i)) && ((str = MatchString(input, percent_directives, ref i)) != null)))
            {
                ParseOutput ret = new ParseOutput { Type = ParseOutput.OutputType.Directive, Name = str };
                return ret;
            }
            
            // Rewind and fail
            i = start_i;
            return null;
        }

        internal static ParseOutput ParseParameter(IList<Tokenizer.TokenDefinition> input, ref int i, bool is_directive)
        {
            // Parameter: register P' | label P' | number P' | '[' P ']' P'
            int start_i = i;
            ParseOutput ret = new ParseOutput { Type = ParseOutput.OutputType.Parameter };

            // Try and match a register
            ParseOutput reg = ParseRegister(input, ref i);
            ParseOutput pprime;
            if (reg != null)
            {
                // Match PPrime
                pprime = ParseParameterPrime(input, ref i, is_directive);
                if (pprime != null)
                {
                    InterpretParameterPrime(ret, reg, pprime);
                    return ret;
                }
                else
                {
                    // Rewind and fail
                    i = start_i;
                    return null;
                }
            }

            // Rewind and try and match a label
            i = start_i;
            ParseOutput label = ParseLabel(input, ref i);
            if(label != null)
            {
                // Match PPrime
                pprime = ParseParameterPrime(input, ref i, is_directive);
                if (pprime != null)
                {
                    InterpretParameterPrime(ret, label, pprime);
                    return ret;
                }
                else
                {
                    // Rewind and fail
                    i = start_i;
                    return null;
                }
            }

            // Rewind and try and match a number
            i = start_i;
            ParseOutput number = ParseNumber(input, ref i);
            if (number != null)
            {
                // Match PPrime
                pprime = ParseParameterPrime(input, ref i, is_directive);
                if (pprime != null)
                {
                    InterpretParameterPrime(ret, number, pprime);
                    return ret;
                }
                else
                {
                    // Rewind and fail
                    i = start_i;
                    return null;
                }
            }

            // Rewind and try and match a 'ContentsOf'
            i = start_i;
            ParseOutput cof = ParseContentsOf(input, ref i, is_directive);
            if (cof != null)
            {
                // Match PPrime
                pprime = ParseParameterPrime(input, ref i, is_directive);
                if (pprime != null)
                {
                    InterpretParameterPrime(ret, cof, pprime);
                    return ret;
                }
                else
                {
                    // Rewind and fail
                    i = start_i;
                    return null;
                }
            }

            // Else rewind and fail
            i = start_i;
            return null;
        }

        private static void InterpretParameterPrime(ParseOutput ret, ParseOutput lhs, ParseOutput pprime)
        {
            /* Convert LHS PPRIME to either:
             * 
             * if PPRIME.Children == 0:     LHS
             * else                   :     Expression(LHS, PPRIME.Children)
             */

            if (pprime.Children.Count == 0)
                ret.Children.Add(lhs);
            else
            {
                ParseOutput expression = new ParseOutput { Type = ParseOutput.OutputType.Expression };
                expression.Children.Add(lhs);

                foreach (ParseOutput pprime_child in pprime.Children)
                {
                    // If its a Parameter with a solitary child, use that instead
                    if ((pprime_child.Type == ParseOutput.OutputType.Parameter) && (pprime_child.Children.Count == 1))
                        expression.Children.Add(pprime_child.Children[0]);
                    else
                        expression.Children.Add(pprime_child);
                }
                
                ret.Children.Add(expression);
            }
        }

        internal static ParseOutput ParseParameterPrime(IList<Tokenizer.TokenDefinition> input, ref int i, bool is_directive)
        {
            // ParameterPrime: num_op parameter | empty (empty also consumes, but does not output, ',', and if(is_directive), ':')
            ParseOutput ret = new ParseOutput { Type = ParseOutput.OutputType.ParameterPrime };
            int start_i = i;

            // Try and match numop
            ParseOutput numop = ParseNumOp(input, ref i);
            if (numop != null)
            {
                ParseOutput param = ParseParameter(input, ref i, is_directive);
                if (param != null)
                {
                    ret.Children.Add(numop);
                    ret.Children.Add(param);
                    return ret;
                }
                else
                {
                    // Rewind and fail
                    i = start_i;
                    return null;
                }
            }

            // Else, consume commas and possibly colons
            while (MatchString(input, ",", ref i) || (is_directive && MatchString(input, ":", ref i))) ;
            return ret;
        }

        internal static ParseOutput ParseContentsOf(IList<Tokenizer.TokenDefinition> input, ref int i, bool is_directive)
        {
            // ContentsOf: '[' Parameter ']'
            ParseOutput ret = new ParseOutput { Type = ParseOutput.OutputType.ContentsOf };
            int start_i = i;

            // Try and match
            ParseOutput param;
            if (MatchString(input, "[", ref i) && ((param = ParseParameter(input, ref i, is_directive)) != null) && MatchString(input, "]", ref i))
            {
                ret.Children.Add(param);
                return ret;
            }

            // Else rewind and fail
            i = start_i;
            return null;
        }

        internal static ParseOutput ParseNumOp(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            string str = MatchString(input, numops, ref i);
            if (str != null)
                return new ParseOutput { Type = ParseOutput.OutputType.NumOp, Name = str };
            else
                return null;
        }

        internal static ParseOutput ParseOperation(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            string str = MatchString(input, operations, ref i);
            if (str != null)
                return new ParseOutput { Type = ParseOutput.OutputType.Operation, Name = str };
            else
                return null;
        }

        internal static ParseOutput ParsePrefix(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            string str = MatchString(input, prefixes, ref i);
            if (str != null)
                return new ParseOutput { Type = ParseOutput.OutputType.Prefix, Name = str };
            else
                return null;
        }

        internal static ParseOutput ParseNumber(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            if ((input[i].Type == Tokenizer.TokenDefinition.def_type.String) && (input[i].String.Length > 0) &&
                (char.IsNumber(input[i].String[0])))
            {
                i++;
                return new ParseOutput { Type = ParseOutput.OutputType.Number, Name = input[i - 1].String };
            }
            else
                return null;
        }

        internal static ParseOutput ParseRegister(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            string str = MatchString(input, registers, ref i);
            if (str != null)
                return new ParseOutput { Type = ParseOutput.OutputType.Register, Name = str };
            else
                return null;
        }

        internal static bool MatchString(IList<Tokenizer.TokenDefinition> input, string str, ref int i)
        {
            if ((input[i].Type == Tokenizer.TokenDefinition.def_type.String) && (input[i].String == str))
            {
                i++;
                return true;
            }
            else
                return false;
        }

        internal static string MatchString(IList<Tokenizer.TokenDefinition> input, ICollection<string> str, ref int i)
        {
            if ((input[i].Type == Tokenizer.TokenDefinition.def_type.String) && (str.Contains(input[i].String.ToLower())))
            {
                i++;
                return input[i - 1].String;
            }
            else
                return null;
        }

        internal static bool MatchNewLine(IList<Tokenizer.TokenDefinition> input, ref int i)
        {
            if (input[i].Type == Tokenizer.TokenDefinition.def_type.Newline)
            {
                i++;
                return true;
            }
            else
                return false;
        }

        static List<string> directives = new List<string>
        {
            "global", "extern",
            "weak", "section", "db", "dw", "dd", "dq", "dt", "ddq", "do", "resb", "resw",
            "resd", "resq", "rest", "resdq", "reso"
        };

        static List<string> sq_bracket_directives = new List<string>
        {
            "bits16", "bits32", "bits64", "use16", "use32", "use64"
        };

        static List<string> percent_directives = new List<string>
        {
            "assign", "rep", "endrep"
        };

        static List<string> numops = new List<string>
        {
            "+", "-", "*", "/", ">>", "<<", "^", "~"
        };

        static List<string> prefixes = new List<string>();
        static List<string> operations = new List<string>();
        static List<string> registers = new List<string>();

        private static void Initx86()
        {
            string[] pfs = new string[]
            {
                "lock", "rep", "repe", "repz", "repne", "repnz", "o16", "o32", "o64"
            };
            foreach (string pf in pfs)
                prefixes.Add(pf);

            string[] ops = new string[] 
            {
                "aaa", "aad", "aam", "aas", "adc", "add", "addpd", "addps",
                "addsd", "addss", "addsubpd", "addsubps", "aesdec", "aesdeclast",
                "aesenc", "aesenclast", "aesimc", "aeskeygenassist", "and",
                "andpd", "andps", "andnpd", "andnps", "arpl", "blendpd",
                "blendps", "blendvpd", "blendvps", "bound", "bsf", "bsr",
                "bswap", "bt", "btc", "btr", "bts", "call", "cbw", "cwde",
                "cdqe", "clc", "cld", "cflush", "cli", "clts", "cmc",
                "cmova", "cmovae", "cmovb", "cmovbe", "cmovc", "cmove", "cmovg",
                "cmovge", "cmovl", "cmovle", "cmovna", "cmovnae", "cmovnb",
                "cmovnbe", "cmovnc", "cmovne", "cmovng", "cmovnge", "cmovnl",
                "cmovnle", "cmovno", "cmovnp", "cmovns", "cmovnz", "cmovo",
                "cmovp", "cmovpe", "cmovpo", "cmovs", "cmovz", "cmp", "cmppd",
                "cmpps", "cmps", "cmpsb", "cmpsw", "cmpsd", "cmpsq", "cmpsd",
                "cmpss", "cmpxchg", "cmpxchg8b", "cmpxchg16b", "comisd",
                "comiss", "cpuid", "crc32", "cvtdq2pd", "cvtdq2ps", "cvtpd2dq",
                "cvtpd2pi", "cvtpd2ps", "cvtpi2pd", "cvtpi2ps", "cvtps2dq",
                "cvtps2pd", "cvtps2pi", "cvtsd2si", "cvtsd2ss", "cvtsi2sd",
                "cvtsi2ss", "cvtss2sd", "cvtss2si", "cvttpd2dq", "cvttpd2pi",
                "cvttps2dq", "cvttps2pi", "cvttsd2si", "cvttss2si", "cwd",
                "cdq", "cqo", "daa", "das", "dec", "div", "divpd", "divps",
                "divss", "dppd", "dpps", "emms", "enter", "extractps", "f2xm1",
                "fabs", "fadd", "faddp", "fiadd", "fbld", "fbstp", "fchs",
                "fclex", "fnclex", "fcmovcc-TODO", "fcomi", "fcomip", "fucomi",
                "fucomip", "fcos", "fdecstp", "fdiv", "fdivp", "fidiv", "fdivr",
                "fdivrp", "fidivr", "ffree", "ficom", "ficomp", "fild",
                "fincstp", "finit", "fninit", "fist", "fistp", "fisttp", "fld",
                "fld1", "fld2t", "fld2e", "fldpi", "fldlg2", "fldln2", "fldz",
                "fldcw", "fldenv", "fmul", "fmulp", "fimul", "fnop", "fpatan",
                "fprem", "fprem1", "fptan", "frndint", "frstor", "fsave",
                "fnsave", "fscale", "fsin", "fsincos", "fsqrt", "fst", "fstp",
                "fstcw", "fnstcw", "fstenv", "fnstenv", "fstsw", "fnstsw",
                "fsub", "fsubp", "fisub", "fsubr", "fsubrp", "fisubr", "ftst",
                "fucom", "fucomp", "fucompp", "fxam", "fxch", "fxrstor",
                "fxsave", "fxtract", "fyl2x", "fyl2xpi", "haddpd", "haddps",
                "hlt", "hsubpd", "hsubps", "idiv", "imul", "in", "inc", "ins",
                "insb", "insw", "insd", "insertps", "int", "into", "int3",
                "invd", "invlpg", "iret", "iretd", "ja", "jae", "jb", "jbe",
                "jc", "jcxz", "jecxz", "jrcxz", "je", "jg", "jge", "jl", "jle",
                "jna", "jnae", "jnb", "jnbe", "jnc", "jne", "jng", "jnge",
                "jnl", "jnle", "jno", "jnp", "jns", "jnz", "jo", "jp", "jpe",
                "jpo", "js", "jz", "jmp", "lahf", "lar", "lddqu", "ldmxcsr",
                "lds", "les", "lfs", "lgs", "lss", "lea", "leave", "lfence",
                "lgdt", "lidt", "lldt", "lmsw", "lods", "lodsb", "lodsw",
                "lodsd", "lodsq", "loop", "loope", "loopne", "lsl", "ltr",
                "maskmovdqu", "maskmovq", "maxpd", "maxps", "maxss", "mfence",
                "minpd", "minps", "minsd", "minss", "monitor", "mov", "movapd",
                "movaps", "movbe", "movd", "movq", "movddup", "movdqa",
                "movdqu", "movdq2q", "movhlps", "movhpd", "movhps", "movlhps",
                "movlpd", "movlps", "movmskpd", "movmskps", "movntdqa",
                "movntdq", "movnti", "movntpd", "movntps", "movntq", "movq",
                "movq2dq", "movs", "movsb", "movsw", "movsd", "movsq",
                "movsd", "movshdup", "movsldup", "movss", "movsx", "movsxd",
                "movupd", "movups", "movzx", "mpsadbw", "mul", "mulpd",
                "mulps", "mulsd", "mulss", "mwait", "neg", "nop", "not",
                "or", "orpd", "orps", "out", "outs", "outsb", "outsw", "outsd",
                "pabsb", "pabsw", "pabsd", "packsswb", "packssdw", "packusdw",
                "packuswb", "paddb", "paddw", "paddd", "paddq", "paddsb",
                "paddsw", "paddusb", "paddusw", "palignr", "pand", "pandn",
                "pause", "pavgb", "pavgw", "pblendvb", "pblendw", "pclmulqdq",
                "pcmpeqb", "pcmpeqw", "pcmpeqd", "pcmpeqq", "pcmpestri",
                "pcmpestrm", "pcmpgtb", "pcmpgtw", "pcmpgtd", "pcmpgtq",
                "pcmpistri", "pcmpistrm", "pextrb", "pextrd", "pextrq",
                "pextrw", "phaddw", "phaddd", "phaddsw", "phminposuw",
                "phsubw", "phsubd", "phsubsw", "pinsrb", "pinsrd", "pinsrq",
                "pinsrw", "pmaddubsw", "pmaddwd", "pmaxsb", "pmaxsd", "pmaxsw",
                "pmaxub", "pmaxud", "pmaxuw", "pminsb", "pminsd", "pminsw",
                "pminub", "pminud", "pminuw", "pmovmskb", "pmovsx", "pmovzx",
                "pmuldq", "pmulhrsw", "pmulhuw", "pmulhw", "pmulld", "pmullw",
                "pmuludq", "pop", "popa", "popad", "popcnt", "popf", "popfd",
                "popfq", "por", "prefetcht0", "prefetcht1", "prefetcht2",
                "prefetchnta", "psadbw", "pshufb", "pshufd", "pshufhw",
                "pshuflw", "pshufw", "psignb", "psignw", "psignd", "pslldq",
                "psllw", "pslld", "psllq", "psraw", "psrad", "psrldq",
                "psrlw", "psrld", "psrlq", "psubb", "psubw", "psubd", "psubq",
                "psubsb", "psubsw", "psubusb", "psubusw", "ptest", "punpckhbw",
                "punpckhwd", "punpckhdq", "punpckhqdq", "punpcklbw",
                "punpcklwd", "punpckldq", "punpcklqdq", "push", "pusha",
                "pushad", "pushf", "pushfd", "pxor", "rcl", "rcr", "rol", "ror",
                "rcpps", "rdmsr", "rdpmc", "rdtsc", "rstscp", "ret", "retf", "roundpd",
                "roundps", "roundsd", "roundss", "rsm", "rsqrtps", "rsqrtss",
                "sahf", "sal", "sar", "shl", "shr", "sbb", "scas", "scasb",
                "scasw", "scasd", "seta", "setae", "setb", "setbe", "setc",
                "sete", "setg", "setge", "setl", "setle", "setna", "setnae",
                "setnb", "setnbe", "setnc", "setne", "setng", "setnge", "setnl",
                "setnle", "setno", "setnp", "setns", "seto", "setp", "setpe",
                "setpo", "sets", "setz", "sfence", "sgdt", "shld", "shrd",
                "shufpd", "shufps", "sidt", "sldt", "smsw", "sqrtps", "sqrtsd",
                "sqrtss", "stc", "std", "sti", "stmxcsr", "stos", "stosb",
                "stosw", "stosd", "stosq", "str", "sub", "subpd", "subps",
                "subsd", "subss", "swapgs", "syscall", "sysenter", "sysexit",
                "sysret", "test", "ucomisd", "ucomiss", "ud2", "unpckhpd",
                "unpckhps", "unpcklpd", "unpcklps", "verr", "verw", "wait",
                "fwait", "wbinvd", "wrmsr", "xadd", "xchg", "xgetbv", "xlat",
                "xlatb", "xor", "xorpd", "xorps", "xrstor", "xsave", "xsetbv",
            };
            foreach(string op in ops)
                operations.Add(op);

            string[] regs = new string[] 
            {
                "al", "ah", "ax", "eax", "rax",
                "bl", "bh", "bx", "ebx", "rbx",
                "cl", "ch", "cx", "ecx", "rcx",
                "dl", "dh", "dx", "edx", "rdx",
                "di", "dil", "edi", "rdi",
                "si", "sil", "esi", "rsi",
                "bpl", "bp", "ebp", "rbp",
                "spl", "sp", "esp", "rsp",
                "r8l", "r8w", "r8d", "r8",
                "r9l", "r9w", "r9d", "r9",
                "r10l", "r10w", "r10d", "r10",
                "r11l", "r11w", "r11d", "r11",
                "r12l", "r12w", "r12d", "r12",
                "r13l", "r13w", "r13d", "r13",
                "r14l", "r14w", "r14d", "r14",
                "r15l", "r15w", "r15d", "r15",
                "cr0", "cr1", "cr2", "cr3",
                "cr4", "cr5", "cr6", "cr7",
                "dr0", "dr1", "dr2", "dr3",
                "dr4", "dr5", "dr6", "dr7",
                "cs", "ds", "es", "ss", "fs", "gs",
            };
            foreach (string reg in regs)
                registers.Add(reg);
        }
    }
}
