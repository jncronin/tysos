/* Copyright (C) 2014 by John Cronin
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

namespace libtysila.frontend.cil.DecomposeOpcodes
{
    internal class newobj
    {
        internal static CilNode Decompose_newobj(CilNode n, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            /* The normal operation of newobj (for reference types) is to determine the size of the type
             * by the Layout mechanism, then call gcmalloc to get some memory for it and finally call
             * the constructor specified.
             * 
             * This does not work for the constructors of System.String, as the amount of memory to allocate
             * depends upon the size of the string to allocate.  We thus need to re-write them to:
             * 
             * determine string length (exact instruction depends upon the constructor being used)
             * call static string System.String.InternalAllocateStr(int32 length)
             * call the constructor
             * with suitable re-arrangement of the arguments on the stack with flip instructions
             */

            if (!(n.il.inline_tok is TTCToken) && !(n.il.inline_tok is MTCToken))
            {
                Assembler.MethodToCompile ctor_mtc = Metadata.GetMTC(new Metadata.TableIndex(n.il.inline_tok), mtc.GetTTC(ass), mtc.msig, ass);

                if ((ctor_mtc.tsig is Signature.BaseType) && (((Signature.BaseType)ctor_mtc.tsig).Type == BaseType_Type.String))
                {
                    /* This is a String constructor
                     * 
                     * There are 8 of these:
                     * .ctor(char c, int32 count)
                     * .ctor(char [] val)
                     * .ctor(char * value, int32 startIndex, int32 length)
                     * .ctor(sbyte * value)
                     * .ctor(char [] val, int32 startIndex, int32 length)
                     * .ctor(char * value)
                     * .ctor(sbyte * value, int32 startIndex, int32 length, System.Text.Encoding enc)
                     * .ctor(sbyte * value, int32 startIndex, int32 length)
                     * 
                     * In most of these we can determine the length from the arguments, in the two that take
                     * a sole argument of sbyte* or char* we need to use an external function.  In the case
                     * of the char[] constructor we can use System.Array.GetLength()
                     */

                    Signature.Method c_1 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.Char), new Signature.Param(BaseType_Type.I4) } };
                    Signature.Method c_2 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.Char) }, ass) } };
                    Signature.Method c_3 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.Char) }, ass), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) } };
                    Signature.Method c_4 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.I1) }, ass) } };
                    Signature.Method c_5 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.Char) }, ass), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) } };
                    Signature.Method c_6 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.Char) }, ass) } };
                    Signature.Method c_7 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.I1) }, ass), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4), new Signature.Param(Metadata.GetTypeDef("mscorlib", "System.Text", "Encoding", ass), ass) } };
                    Signature.Method c_8 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.I1) }, ass), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) } };

                    Assembler.TypeToCompile str_ttc = Metadata.GetTTC(new Signature.Param(BaseType_Type.String), mtc.GetTTC(ass), null, ass);
                    Signature.Method intallocstr_msig = new Signature.Method { HasThis = false, RetType = new Signature.Param(BaseType_Type.String), Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.I4) } };
                    Metadata.MethodDefRow intallocstr_mdr = Metadata.GetMethodDef(str_ttc.type.m, "InternalAllocateStr", str_ttc.type, intallocstr_msig, ass);
                    Assembler.MethodToCompile intallocstr_mtc = new Assembler.MethodToCompile { _ass = ass, meth = intallocstr_mdr, msig = intallocstr_msig, type = str_ttc.type, tsigp = str_ttc.tsig };

                    if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_1, ass))
                    {
                        /* .ctor(char c, int count)
                         * 
                         * stack currently contains: count, c, ...
                         * 
                         * method is:
                         * dup                              -> length, length, c, ...
                         * call InternalAllocateStr         -> string, length, c, ...
                         * dup                              -> string, string, length, c, ...
                         * pushback(3)                      -> string, length, c, string, ...
                         * pushback(3)                      -> length, c, string, string, ...
                         * call .ctor                       -> string, ...
                         */

                        timple.BaseNode first = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        timple.BaseNode next = first;
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = new MTCToken { mtc = intallocstr_mtc } } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd2f), inline_int = 3 } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd2f), inline_int = 3 } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = n.il.inline_tok } });

                        n.Remove();

                        return (CilNode)first;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_2, ass))
                    {
                        /* .ctor(char[] c)
                         * 
                         * stack currently contains: c, ...
                         * 
                         * method is:
                         * dup                              -> c, c, ...
                         * ldlen                            -> length, c, ...
                         * call InternalAllocateStr         -> string, c, ...
                         * dup                              -> string, string, c, ...
                         * pushback(2)                      -> string, c, string, ...
                         * pushback(2)                      -> c, string, string, ...
                         * call .ctor
                         */

                        timple.BaseNode first = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        timple.BaseNode next = first;
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.ldlen) } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = new MTCToken { mtc = intallocstr_mtc } } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd2f), inline_int = 2 } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd2f), inline_int = 2 } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = n.il.inline_tok } });

                        n.Remove();

                        return (CilNode)first;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_3, ass))
                    {
                        /* .ctor(char *value, int startIndex, int length)
                         * 
                         * stack currently contains: length, startIndex, value
                         * 
                         * method is:
                         * 
                         * dup                              -> length, length, startIndex, value, ...
                         * call InternalAllocateStr         -> string, length, startIndex, value, ...
                         * dup                              -> string, string, length, startIndex, value, ...
                         * pushback(4)                      -> string, length, startIndex, value, string, ...
                         * pushback(4)                      -> length, startIndex, value, string, string, ...
                         * call .ctor                       -> string, ...
                         */

                        timple.BaseNode first = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        timple.BaseNode next = first;
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = new MTCToken { mtc = intallocstr_mtc } } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd2f), inline_int = 4 } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd2f), inline_int = 4 } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = n.il.inline_tok } });

                        n.Remove();

                        return (CilNode)first;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_4, ass))
                    {
                        /* .ctor(sbyte * value)
                         * 
                         * stack currently contains 'value'
                         * method is:
                         * 
                         * dup                              -> value, value, ...
                         * call mbstrlen                    -> length, value, ...
                         * call InternalAllocateStr         -> string, value, ...
                         * dup                              -> string, string, value, ...
                         * flip3                            -> value, string, string, ...
                         * call .ctor                       -> string, ...
                         */

                        timple.BaseNode first = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        timple.BaseNode next = first;
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd2c) } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = new MTCToken { mtc = intallocstr_mtc } } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd21) } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = n.il.inline_tok } });

                        n.Remove();

                        return (CilNode)first;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_5, ass))
                    {
                        /* .ctor(char[] chars, int startIndex, int length)
                         * 
                         * stack currently contains: length, startIndex, chars, ...
                         * method is:
                         * 
                         * dup                              -> length, length, startIndex, chars, ...
                         * call InternalAllocateStr         -> string, length, startIndex, chars, ...
                         * dup                              -> string, string, length, startIndex, chars, ...
                         * pushback(4)                      -> string, length, startIndex, chars, string, ...
                         * pushback(4)                      -> length, startIndex, chars, string, string, ...
                         * call .ctor                       -> string, ...
                         */

                        timple.BaseNode first = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        timple.BaseNode next = first;
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = new MTCToken { mtc = intallocstr_mtc } } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.dup) } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd2f), inline_int = 4 } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(0xfd2f), inline_int = 4 } });
                        next = next.InsertAfter(new CilNode { il = new InstructionLine { opcode = new Opcode(Opcode.SingleOpcodes.call), inline_tok = n.il.inline_tok } });

                        n.Remove();

                        return (CilNode)first;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_6, ass))
                    {
                        return n;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_7, ass))
                    {
                        return n;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_8, ass))
                    {
                        return n;
                    }
                    else
                        return n;
                }
            }
            return n;
        }
    }
}
