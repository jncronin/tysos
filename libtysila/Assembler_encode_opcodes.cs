/* Copyright (C) 2008 - 2011 by John Cronin
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
using libasm;

namespace libtysila
{
    partial class Assembler
    {
        public const int throw_OverflowException = 1;
        public const int throw_InvalidCastException = 2;
        public const int throw_NullReferenceException = 3;
        public const int throw_MissingMethodException = 4;
        public const int throw_IndexOutOfRangeException = 5;

        bool Decompose_newobj(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
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

            if (!(instrs[idx].inline_tok is TTCToken) && !(instrs[idx].inline_tok is MTCToken))
            {
                MethodToCompile ctor_mtc = Metadata.GetMTC(new Metadata.TableIndex(instrs[idx].inline_tok), containing_type, containing_meth.msig, this);

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
                    Signature.Method c_2 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.Char) }, this) } };
                    Signature.Method c_3 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.Char) }, this), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) } };
                    Signature.Method c_4 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.I1) }, this) } };
                    Signature.Method c_5 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.Char) }, this), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) } };
                    Signature.Method c_6 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.Char) }, this) } };
                    Signature.Method c_7 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.I1) }, this), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4), new Signature.Param(Metadata.GetTypeDef("mscorlib", "System.Text", "Encoding", this), this) } };
                    Signature.Method c_8 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.I1) }, this), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) } };

                    Assembler.TypeToCompile str_ttc = Metadata.GetTTC(new Signature.Param(BaseType_Type.String), containing_type, null, this);
                    Signature.Method intallocstr_msig = new Signature.Method { HasThis = false, RetType = new Signature.Param(BaseType_Type.String), Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.I4) } };
                    Metadata.MethodDefRow intallocstr_mdr = Metadata.GetMethodDef(str_ttc.type.m, "InternalAllocateStr", str_ttc.type, intallocstr_msig, this);
                    MethodToCompile intallocstr_mtc = new MethodToCompile { _ass = this, meth = intallocstr_mdr, msig = intallocstr_msig, type = str_ttc.type, tsigp = str_ttc.tsig };

                    if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_1, this))
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


                        instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = new MTCToken { mtc = intallocstr_mtc } });
                        instrs.Insert(idx + 3, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 4, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = 3 });
                        instrs.Insert(idx + 5, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = 3 });
                        instrs.Insert(idx + 6, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = instrs[idx].inline_tok });

                        instrs.RemoveAt(idx);

                        return true;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_2, this))
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

                        instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldlen] });
                        instrs.Insert(idx + 3, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = new MTCToken { mtc = intallocstr_mtc } });
                        instrs.Insert(idx + 4, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 5, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = 2 });
                        instrs.Insert(idx + 6, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = 2 });
                        instrs.Insert(idx + 7, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = instrs[idx].inline_tok });

                        instrs.RemoveAt(idx);

                        return true;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_3, this))
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

                        instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = new MTCToken { mtc = intallocstr_mtc } });
                        instrs.Insert(idx + 3, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 4, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = 4 });
                        instrs.Insert(idx + 5, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = 4 });
                        instrs.Insert(idx + 6, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = instrs[idx].inline_tok });

                        instrs.RemoveAt(idx);

                        return true;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_4, this))
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

                        instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[0xfd2c] });
                        instrs.Insert(idx + 3, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = new MTCToken { mtc = intallocstr_mtc } });
                        instrs.Insert(idx + 4, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 5, new InstructionLine { opcode = Opcodes[0xfd21] });
                        instrs.Insert(idx + 6, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = instrs[idx].inline_tok });

                        instrs.RemoveAt(idx);

                        return true;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_5, this))
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

                        instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = new MTCToken { mtc = intallocstr_mtc } });
                        instrs.Insert(idx + 3, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
                        instrs.Insert(idx + 4, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = 4 });
                        instrs.Insert(idx + 5, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = 4 });
                        instrs.Insert(idx + 6, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = instrs[idx].inline_tok });

                        instrs.RemoveAt(idx);

                        return true;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_6, this))
                    {
                        return false;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_7, this))
                    {
                        return false;
                    }
                    else if (Signature.BaseMethodSigCompare(ctor_mtc.msig, c_8, this))
                    {
                        return false;
                    }
                    else
                        return false;


                }
            }
            return false;
        }

        bool Decompose_callvirt(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            MethodToCompile call_mtc = Metadata.GetMTC(new Metadata.TableIndex(instrs[idx].inline_tok), containing_type, containing_meth.msig, this);
            int arg_count = get_arg_count(call_mtc.msig);
            Signature.Param stack_type = instrs[idx].stack_before[instrs[idx].stack_before.Count - arg_count].type;
            Assembler.TypeToCompile stack_ttc = new TypeToCompile(stack_type, this);

            {
                /* Implement the constrained prefix, see CIL III:2.1
                 * 
                 * The type on the stack must be a managed pointer to constrained_tok
                 * If constrained_tok is a reference type then first dereference the ptr argument on the stack
                 * If constrained_tok is a value type and it implements the method then do nothing
                 * If constrained_tok is a value type and it does not implement the method then dereference it and box it
                 */

                if (instrs[idx].Prefixes.constrained)
                {
                    // remove the constrained instruction so that on the next pass we skip this part
                    instrs[idx].Prefixes.constrained = false;

                    Token constrained_t = instrs[idx].Prefixes.constrained_tok;
                    Assembler.TypeToCompile constrained_tok = Metadata.GetTTC(constrained_t, containing_type, containing_meth.msig, this);

                    if (!((stack_ttc.tsig.Type is Signature.ManagedPointer) &&
                        (Signature.TypeCompare(new TypeToCompile { _ass = this, tsig = new Signature.Param(((Signature.ManagedPointer)stack_ttc.tsig.Type).ElemType, this), type = stack_ttc.type }, constrained_tok, this))))
                    {
                        VerificationException ve = new VerificationException("constrained: The type on the stack must be a managed pointer to constrained_tok", instrs[idx], containing_meth);
                        if (state.security.RequireVerifiedCode)
                            throw ve;
                        else
                            state.warnings.Add(ve);
                    }

                    if ((constrained_tok.type == null) || !constrained_tok.type.IsValueType(this))
                    {
                        // case for reference types

                        // use a special case of the flip opcode to rotate the stack to bring the ptr argument to the top, then perform a ldind.ref
                        int offset = 0;
                        if (arg_count > 1)
                            instrs.Insert(idx + (offset++), new InstructionLine { opcode = Opcodes[0xfd20], stack_before_adjust = arg_count - 1 });
                        instrs.Insert(idx + (offset++), new InstructionLine { opcode = Opcodes[0x50] });
                        if (arg_count > 1)
                            instrs.Insert(idx + (offset++), new InstructionLine { opcode = Opcodes[0xfd20], stack_before_adjust = arg_count - 1 });

                        return true;
                    }
                    else
                    {
                        // case for value types

                        // see if the class implements the method
                        Layout l = Layout.GetLayout(constrained_tok, this);
                        if (l.GetVirtualMethod(call_mtc) != null)
                        {
                            // it does, so do nothing
                            return true;
                        }
                        else
                        {
                            // it doesn't, so dereference it (ldobj) then box it
                            int offset = 0;
                            if (arg_count > 1)
                                instrs.Insert(idx + (offset++), new InstructionLine { opcode = Opcodes[0xfd20], stack_before_adjust = arg_count - 1 });
                            instrs.Insert(idx + (offset++), new InstructionLine { opcode = Opcodes[0x71], inline_tok = constrained_t });
                            instrs.Insert(idx + (offset++), new InstructionLine { opcode = Opcodes[0x8c], inline_tok = constrained_t });
                            if (arg_count > 1)
                                instrs.Insert(idx + (offset++), new InstructionLine { opcode = Opcodes[0xfd20], stack_before_adjust = arg_count - 1 });

                            return true;
                        }
                    }
                }
            }
            {
                /* Deal with the case where the object on the stack is not knowingly of the type of the method being called
                 * 
                 * e.g. calling methods defined on System.Object where the object on the stack is an interface.
                 * 
                 * In this circumstance, execute a cast to System.Object first (will require a runtime dynamic cast) before doing the call */

                Layout l = Layout.GetLayout(stack_ttc, this, false);

                /* identify which interface should implement this method
                 * calls to methods of value types are actually calls to their boxed representations
                 */
                Assembler.TypeToCompile call_mtc_ttc = call_mtc.GetTTC(this);
                if (call_mtc_ttc.type.IsValueType(this) && !(call_mtc_ttc.tsig.Type is Signature.BoxedType))
                    call_mtc_ttc.tsig.Type = new Signature.BoxedType(call_mtc_ttc.tsig.Type);

                if (l.ClassesImplemented.Contains(call_mtc_ttc))
                    return false;

                /* Implement the cast */

                // if (arg_count == 4)
                // arg3, arg2, arg1, src, ...

                // bringforward(3) ->       src, arg3, arg2, arg1, ...
                // castclass       ->       dest, arg3, arg2, arg1, ...
                // pushback(3)     ->       arg3, arg2, arg1, ...

                instrs.Insert(idx, new InstructionLine { opcode = Opcodes[0xfd31], inline_int = arg_count - 1 });
                instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.castclass], inline_tok = new TTCToken { ttc = call_mtc_ttc } });
                instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = arg_count - 1 });
                return true;
            }
        }

        bool Decompose_box(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            // Don't box reference types
            Signature.Param unbox_type = instrs[idx].stack_before[instrs[idx].stack_before.Count - 1].type;
            Metadata.TypeDefRow tdr = Metadata.GetTypeDef(unbox_type.Type, this);
            if ((tdr != null) && tdr.IsValueType(this))
            {
                // newobj, dup, flip3, stfld
                Signature.Param box_type = new Signature.Param(new Signature.BoxedType { Type = unbox_type.Type }, this);
                Assembler.TypeToCompile boxed_ttc = new TypeToCompile { _ass = this, tsig = box_type, type = tdr };
                Assembler.FieldToCompile value_ftc = new FieldToCompile
                {
                    _ass = this,
                    definedin_tsig = boxed_ttc.tsig,
                    definedin_type = boxed_ttc.type,
                    field = new Metadata.FieldRow { Name = "m_value" },
                    fsig = unbox_type
                };
                instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[0x73], inline_tok = new TTCToken { ttc = boxed_ttc } });
                instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[0x25] });
                instrs.Insert(idx + 3, new InstructionLine { opcode = Opcodes[0xfd21] });
                instrs.Insert(idx + 4, new InstructionLine { opcode = Opcodes[0x7d], inline_tok = new FTCToken { ftc = value_ftc } });
                instrs.RemoveAt(idx);
            }
            else
            {
                // If this is a reference type simply change the instruction to nop
                instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[0x00] });
                instrs.RemoveAt(idx);
            }
            return true;
        }

        bool Decompose_ldtoken(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            /* First decide on the type of handle to create: field, type or method */
            Metadata.TypeDefRow th_type;
            Opcode init_opcode;

            switch (instrs[idx].inline_tok.Value.TableId())
            {
                case (int)Metadata.TableId.MethodDef:
                case (int)Metadata.TableId.MethodSpec:
                    th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeMethodHandle", this);
                    init_opcode = Opcodes[0xfd26];
                    break;

                case (int)Metadata.TableId.TypeDef:
                case (int)Metadata.TableId.TypeRef:
                case (int)Metadata.TableId.TypeSpec:
                    th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeTypeHandle", this);
                    init_opcode = Opcodes[0xfd22];
                    break;

                case (int)Metadata.TableId.Field:
                    th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeFieldHandle", this);
                    init_opcode = Opcodes[0xfd27];
                    break;

                case (int)Metadata.TableId.MemberRef:
                    {
                        Metadata.ITableRow ref_type = Metadata.ResolveRef(instrs[idx].inline_tok.Value, this);
                        if (ref_type is Metadata.MethodDefRow)
                        {
                            th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeMethodHandle", this);
                            init_opcode = Opcodes[0xfd26];
                        }
                        else if (ref_type is Metadata.FieldRow)
                        {
                            th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeFieldHandle", this);
                            init_opcode = Opcodes[0xfd27];
                        }
                        else
                            throw new NotSupportedException();
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }

            // newobj, init_rth/rfh/rmh
            Assembler.TypeToCompile rth_ttc = new TypeToCompile();
            rth_ttc.type = th_type;
            rth_ttc.tsig = new Signature.Param(rth_ttc.type, this);
            instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[0x73], inline_tok = new TTCToken { ttc = rth_ttc } });

            instrs.Insert(idx + 2, new InstructionLine
            {
                opcode = init_opcode,
                inline_tok = instrs[idx].inline_tok
            });
            instrs.RemoveAt(idx);
            return true;
        }

        bool Decompose_castclass(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            // null -> null
            // obj -> obj2 if obj can be cast to <T>
            // throw InvalidCastException else

            // src, ...

            // dup ->           src, src, ...
            // castclassex ->   dest, src, ...
            // dup ->           dest, dest, src, ...
            // pushback(2) ->   dest, src, dest, ...
            // xor ->           result (0 if src == dest), dest, ...
            // throwtrue ->     dest, ... (or exception thrown)

            instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
            instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[0xfd23], inline_tok = instrs[idx].inline_tok });
            instrs.Insert(idx + 3, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
            instrs.Insert(idx + 4, new InstructionLine { opcode = Opcodes[0xfd2f], inline_int = 2 });
            instrs.Insert(idx + 5, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.xor], allow_obj_numop = true });
            instrs.Insert(idx + 6, new InstructionLine { opcode = Opcodes[0xfd30], inline_int = 2 });

            /*instrs.Insert(idx + 1, new InstructionLine
            {
                opcode = Opcodes[0xfd23],
                inline_tok = instrs[idx].inline_tok,
                stack_before_adjust = instrs[idx].stack_before_adjust
            });
            instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
            instrs.Insert(idx + 3, new InstructionLine
            {
                opcode = Opcodes[0xfd24],
                inline_int = 2
            });*/
            instrs.RemoveAt(idx);

            return true;
        }

        bool Decompose_isinst(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            // null -> null
            // obj -> obj2 if obj can be cast to <T>
            // null else

            // castclassex
            instrs.Insert(idx + 1, new InstructionLine
            {
                opcode = Opcodes[0xfd23],
                inline_tok = instrs[idx].inline_tok
            });
            instrs.RemoveAt(idx);

            return true;
        }

        bool Decompose_unbox_any(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            // if applied to a boxed value type, do (optionally check assignment compatibility), unbox, ldobj
            // if applied to a reference type, do castclass

            Assembler.TypeToCompile unbox_type = Metadata.GetTTC(instrs[idx].inline_tok, containing_type, containing_meth.msig, this);

            if ((unbox_type.type != null) && unbox_type.type.IsValueType(this))
            {
                instrs.Insert(idx + 1, new InstructionLine
                {
                    opcode = Opcodes[(int)SingleOpcodes.unbox],
                    inline_tok = instrs[idx].inline_tok
                });
                instrs.Insert(idx + 2, new InstructionLine
                {
                    opcode = Opcodes[(int)SingleOpcodes.ldobj],
                    inline_tok = instrs[idx].inline_tok
                });
                instrs.RemoveAt(idx);
            }
            else
            {
                instrs.Insert(idx + 1, new InstructionLine
                {
                    opcode = Opcodes[(int)SingleOpcodes.castclass],
                    inline_tok = instrs[idx].inline_tok
                });
                instrs.RemoveAt(idx);
            }

            return true;
        }

        bool Decompose_unbox(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            /* extract a managed pointer from a boxed value type */
            /* simply ldflda m_value */
            Assembler.TypeToCompile unbox_type = Metadata.GetTTC(instrs[idx].inline_tok, containing_type, containing_meth.msig, this);
            Signature.Param box_type = new Signature.Param(new Signature.BoxedType(unbox_type.tsig.Type), this);
            Metadata.TypeDefRow tdr = unbox_type.type;
            //Assembler.TypeToCompile unboxed_ttc = new TypeToCompile { tsig = unbox_type, type = tdr };
            Assembler.FieldToCompile value_ftc = new FieldToCompile
            {
                definedin_tsig = box_type,
                definedin_type = tdr,
                field = new Metadata.FieldRow { Name = "m_value" },
                fsig = unbox_type.tsig
            };
            instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldflda], inline_tok = new FTCToken { ftc = value_ftc } });
            instrs.RemoveAt(idx);
            return true;
        }

        bool Decompose_stelem(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            // Decide on the stelem instruction depending on the type of 'token'
            SingleOpcodes op = SingleOpcodes.stelem_ref;

            Assembler.TypeToCompile ttc = Metadata.GetTTC(instrs[idx].inline_tok, containing_type, containing_meth.msig, this);

            if (ttc.tsig.Type is Signature.BaseType)
            {
                BaseType_Type bt = ((Signature.BaseType)ttc.tsig.Type).Type;

                switch (bt)
                {
                    case BaseType_Type.I:
                        op = SingleOpcodes.stelem_i;
                        break;
                    case BaseType_Type.Byte:
                    case BaseType_Type.Boolean:
                    case BaseType_Type.I1:
                    case BaseType_Type.U1:
                        op = SingleOpcodes.stelem_i1;
                        break;
                    case BaseType_Type.Char:
                    case BaseType_Type.I2:
                    case BaseType_Type.U2:
                        op = SingleOpcodes.stelem_i2;
                        break;
                    case BaseType_Type.I4:
                    case BaseType_Type.U4:
                        op = SingleOpcodes.stelem_i4;
                        break;
                    case BaseType_Type.I8:
                    case BaseType_Type.U8:
                        op = SingleOpcodes.stelem_i8;
                        break;
                    case BaseType_Type.R4:
                        op = SingleOpcodes.stelem_r4;
                        break;
                    case BaseType_Type.R8:
                        op = SingleOpcodes.stelem_r8;
                        break;
                    case BaseType_Type.U:
                        op = SingleOpcodes.stelem_i;
                        break;
                }
            }
            else if ((ttc.tsig.Type is Signature.ComplexType) || (ttc.tsig.Type is Signature.GenericType))
            {
                if (ttc.type.IsValueType(this) || ((ttc.tsig.Type is Signature.GenericType) && ((Signature.GenericType)ttc.tsig.Type).GenType.IsValueType(this)))
                {
                    /* Rewrite to ldelema, stobj
                     * 
                     * We need to do a bit of playing with the stack to get it in the right order, however
                     * 
                     * flip3, flip, ldelema, flip, stobj
                     */

                    Token elem_type_tok = instrs[idx].inline_tok;

                    instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[0xfd21] });
                    instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[0xfd20] });
                    instrs.Insert(idx + 3, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldelema], inline_tok = elem_type_tok });
                    instrs.Insert(idx + 4, new InstructionLine { opcode = Opcodes[0xfd20] });
                    instrs.Insert(idx + 5, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stobj], inline_tok = elem_type_tok });
                    instrs.RemoveAt(idx);
                    return true;
                }
            }

            instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)op], int_array = instrs[idx].int_array });
            instrs.RemoveAt(idx);
            return true;
        }

        bool Decompose_ldelem(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            // Decide on the ldelem instruction depending on the type of 'token'
            SingleOpcodes op = SingleOpcodes.ldelem_ref;

            Assembler.TypeToCompile ttc;
            if (instrs[idx].inline_tok is TTCToken)
                ttc = ((TTCToken)instrs[idx].inline_tok).ttc;
            else
                ttc = Metadata.GetTTC(new Metadata.TableIndex(instrs[idx].inline_tok.Metadata, instrs[idx].inline_tok), containing_type, containing_meth.msig, this);
            if (ttc.tsig.Type is Signature.BaseType)
            {
                BaseType_Type bt = ((Signature.BaseType)ttc.tsig.Type).Type;
                switch (bt)
                {
                    case BaseType_Type.Boolean:
                        op = SingleOpcodes.ldelem_u1;
                        break;
                    case BaseType_Type.Byte:
                        op = SingleOpcodes.ldelem_u1;
                        break;
                    case BaseType_Type.Char:
                        op = SingleOpcodes.ldelem_u2;
                        break;
                    case BaseType_Type.I:
                        op = SingleOpcodes.ldelem_i;
                        break;
                    case BaseType_Type.I1:
                        op = SingleOpcodes.ldelem_i1;
                        break;
                    case BaseType_Type.I2:
                        op = SingleOpcodes.ldelem_i2;
                        break;
                    case BaseType_Type.I4:
                        op = SingleOpcodes.ldelem_i4;
                        break;
                    case BaseType_Type.I8:
                        op = SingleOpcodes.ldelem_i8;
                        break;
                    case BaseType_Type.R4:
                        op = SingleOpcodes.ldelem_r4;
                        break;
                    case BaseType_Type.R8:
                        op = SingleOpcodes.ldelem_r8;
                        break;
                    case BaseType_Type.U:
                        op = SingleOpcodes.ldelem_i;
                        break;
                    case BaseType_Type.U1:
                        op = SingleOpcodes.ldelem_u1;
                        break;
                    case BaseType_Type.U2:
                        op = SingleOpcodes.ldelem_u2;
                        break;
                    case BaseType_Type.U4:
                        op = SingleOpcodes.ldelem_u4;
                        break;
                    case BaseType_Type.U8:
                        op = SingleOpcodes.ldelem_i8;
                        break;
                }
            }
            else if ((ttc.tsig.Type is Signature.ComplexType) || (ttc.tsig.Type is Signature.GenericType))
            {
                if (ttc.type.IsValueType(this))
                {
                    /* Rewrite to ldelema, ldobj */
                    instrs[idx].opcode = Opcodes[(int)SingleOpcodes.ldelema];
                    instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldobj], inline_tok = instrs[idx].inline_tok });
                    return true;
                }
            }

            instrs.Insert(idx + 1, new InstructionLine { opcode = Opcodes[(int)op], int_array = instrs[idx].int_array });
            instrs.RemoveAt(idx);
            return true;
        }

        bool Decompose_sizeof(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            Assembler.TypeToCompile ttc = Metadata.GetTTC(new Metadata.TableIndex(instrs[idx].inline_tok.Metadata, instrs[idx].inline_tok), containing_type, containing_meth.msig, this);

            instrs.Insert(idx + 1, new InstructionLine
            {
                opcode = Opcodes[(int)SingleOpcodes.ldc_i4],
                inline_int = GetSizeOf(ttc.tsig)
            });
            instrs.Insert(idx + 2, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.conv_u4] });
            instrs.RemoveAt(idx);

            return true;
        }

        bool DecomposeComplexOpts(List<InstructionLine> instrs, ref int idx, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            // Split complex operations into simpler ones

            switch (instrs[idx].opcode.opcode1)
            {
                case SingleOpcodes.newobj:
                    return Decompose_newobj(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.callvirt:
                    return Decompose_callvirt(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.box:
                    return Decompose_box(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.ldtoken:
                    return Decompose_ldtoken(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.castclass:
                    return Decompose_castclass(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.isinst:
                    return Decompose_isinst(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.unbox_any:
                    return Decompose_unbox_any(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.unbox:
                    return Decompose_unbox(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.stelem:
                    return Decompose_stelem(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.ldelem:
                    return Decompose_ldelem(instrs, ref idx, containing_type, containing_meth, state);
                case SingleOpcodes.double_:
                    switch (instrs[idx].opcode.opcode2)
                    {
                        case DoubleOpcodes._sizeof:
                            return Decompose_sizeof(instrs, ref idx, containing_type, containing_meth, state);
                    }
                    break;
            }
            return false;
        }

        void EncodeOpcode(InstructionLine i, Metadata.MethodDefRow meth, Assembler.MethodToCompile mtc, Metadata m, Assembler.MethodToCompile call_site,
            cfg_node cfg_node, AssemblerState state, List<cfg_node> nodes)
        {
            switch (i.opcode.opcode1)
            {
                case SingleOpcodes.pop:
                    break;
                case SingleOpcodes.ldarg_0:
                    enc_ldarg(i, meth, m, 0, state);
                    break;
                case SingleOpcodes.ldarg_1:
                    enc_ldarg(i, meth, m, 1, state);
                    break;
                case SingleOpcodes.ldarg_2:
                    enc_ldarg(i, meth, m, 2, state);
                    break;
                case SingleOpcodes.ldarg_3:
                    enc_ldarg(i, meth, m, 3, state);
                    break;
                case SingleOpcodes.ldarg_s:
                    enc_ldarg(i, meth, m, i.inline_int, state);
                    break;
                case SingleOpcodes.ldarga_s:
                    enc_ldarga(i, mtc, i.inline_int, state);
                    break;
                case SingleOpcodes.starg_s:
                    enc_starg(i, meth, m, i.inline_int, state);
                    break;
                case SingleOpcodes.call:
                    enc_call(i, mtc, state);
                    break;
                case SingleOpcodes.calli:
                    enc_call(i, mtc, state);
                    break;
                case SingleOpcodes.callvirt:
                    enc_call(i, mtc, state);
                    break;
                case SingleOpcodes.newobj:
                    enc_newobj(i, mtc, m, state);
                    break;
                case SingleOpcodes.ret:
                    //Signature.Method sig = Signature.ParseMethodDefSig(m, meth.Signature);

                    if (state.profile)
                        i.tacs.Add(new CallEx(var.Null, new var[] { state.mangled_name_var }, "__endprofile", callconv_profile));

                    Signature.Method sig = null;
                    if (mtc.msig is Signature.Method)
                        sig = mtc.msig as Signature.Method;
                    else
                        sig = ((Signature.GenericMethod)mtc.msig).GenMethod;
                    ThreeAddressCode.Op retop = ThreeAddressCode.Op.ret_void;
                    i.pop_count = 1;

                    switch (sig.RetType.CliType(this))
                    {
                        case CliType.void_:
                            i.pop_count = 0;
                            retop = ThreeAddressCode.Op.ret_void;
                            break;
                        case CliType.int32:
                            retop = ThreeAddressCode.Op.ret_i4;
                            break;
                        case CliType.int64:
                            retop = ThreeAddressCode.Op.ret_i8;
                            break;
                        case CliType.F64:
                        case CliType.F32:
                            retop = ThreeAddressCode.Op.ret_r8;
                            break;
                        case CliType.native_int:
                        case CliType.O:
                        case CliType.reference:
                            retop = ThreeAddressCode.Op.ret_i;
                            break;
                        case CliType.vt:
                            retop = ThreeAddressCode.Op.ret_vt;
                            break;
                    }

                    if (retop == ThreeAddressCode.Op.ret_vt)
                    {
                        // Returning value types returns a pointer to the value type which is immediately assigned to a location in the calling function on return
                        var vt_var = i.stack_before[i.stack_before.Count - 1].contains_variable;

                        if (vt_var.is_address_of_vt)
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.ret_i, var.Null, vt_var, var.Null));
                        else
                        {
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.ret_i, var.Null, var.AddrOf(vt_var), var.Null));
                            //i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, vt_var, var.Null));
                            //i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.ret_i, var.Null, state.next_variable - 1, var.Null));
                        }
                    }
                    else
                    {
                        /* Some built-in value types are being represented as a pointer on the stack, for these we need to dereference first */
                        if ((retop != ThreeAddressCode.Op.ret_void) && i.stack_before[i.stack_before.Count - 1].contains_variable.is_address_of_vt)
                        {
                            ThreeAddressCode.Op mov_op = GetAssignTac(sig.RetType.CliType(this));
                            i.tacs.Add(new ThreeAddressCode(mov_op, state.next_variable++, var.ContentsOf(i.stack_before[i.stack_before.Count - 1].contains_variable), var.Null));
                            i.tacs.Add(new ThreeAddressCode(retop, var.Null, state.next_variable - 1, var.Null));
                        }
                        else
                        {
                            i.tacs.Add(new ThreeAddressCode(retop, 0,
                                (i.pop_count == 1) ? i.stack_before[i.stack_before.Count - 1].contains_variable : var.Null, var.Null));
                        }
                    }
                    cfg_node.has_fall_through = false;
                    break;
                case SingleOpcodes.ldstr:
                    i.pushes = new Signature.Param(BaseType_Type.String);
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++,
                        m.StringTable.GetStringAddress(((Metadata.UserStringHeapItem)i.inline_tok.Value).Value, this), 0));
                    i.pushes_variable = state.next_variable - 1;
                    i.pushes_variable.known_value = ((Metadata.UserStringHeapItem)i.inline_tok.Value).Value;
                    break;
                case SingleOpcodes.ldobj:
                    {
                        TypeToCompile ldobj_type = Metadata.GetTTC(i.inline_tok, mtc.GetTTC(this), mtc.msig, this);
                        ThreeAddressCode.Op ldobj_tac = GetLdObjTac(GetCliType(ldobj_type));
                        i.pushes = ldobj_type.tsig;
                        i.tacs.Add(new ThreeAddressCode(ldobj_tac, state.next_variable++,
                            i.stack_before[i.stack_before.Count - 1].contains_variable, 0,
                            (ldobj_tac == ThreeAddressCode.Op.ldobj_vt) ? GetSizeOf(i.pushes) : 0));
                        i.pushes_variable = state.next_variable - 1;
                    }
                    break;
                case SingleOpcodes.ldind_i:
                case SingleOpcodes.ldind_i1:
                case SingleOpcodes.ldind_i2:
                case SingleOpcodes.ldind_i4:
                case SingleOpcodes.ldind_i8:
                case SingleOpcodes.ldind_r4:
                case SingleOpcodes.ldind_r8:
                case SingleOpcodes.ldind_ref:
                case SingleOpcodes.ldind_u1:
                case SingleOpcodes.ldind_u2:
                case SingleOpcodes.ldind_u4:
                    enc_ldind(i, mtc, state);
                    break;
                case SingleOpcodes.stind_i:
                case SingleOpcodes.stind_i1:
                case SingleOpcodes.stind_i2:
                case SingleOpcodes.stind_i4:
                case SingleOpcodes.stind_i8:
                case SingleOpcodes.stind_r4:
                case SingleOpcodes.stind_r8:
                case SingleOpcodes.stind_ref:
                    enc_stind(i, mtc);
                    break;
                case SingleOpcodes.ldsfld:
                    enc_ldfld(i, call_site.GetTTC(this), call_site.msig, true, state);
                    break;
                case SingleOpcodes.ldfld:
                    enc_ldfld(i, call_site.GetTTC(this), call_site.msig, false, state);
                    break;                
                case SingleOpcodes.ldsflda:
                    enc_ldflda(i, call_site.GetTTC(this), call_site.msig, true, state);
                    break;
                case SingleOpcodes.ldflda:
                    enc_ldflda(i, call_site.GetTTC(this), call_site.msig, false, state);
                    break;
                case SingleOpcodes.stsfld:
                    enc_stfld(i, call_site, m, true, state);
                    break;
                case SingleOpcodes.stfld:
                    enc_stfld(i, call_site, m, false, state);
                    break;

                case SingleOpcodes.ldloc_0:
                    enc_ldloc(i, meth, m, 0, state);
                    break;
                case SingleOpcodes.ldloc_1:
                    enc_ldloc(i, meth, m, 1, state);
                    break;
                case SingleOpcodes.ldloc_2:
                    enc_ldloc(i, meth, m, 2, state);
                    break;
                case SingleOpcodes.ldloc_3:
                    enc_ldloc(i, meth, m, 3, state);
                    break;
                case SingleOpcodes.ldloc_s:
                    enc_ldloc(i, meth, m, (int)i.inline_uint, state);
                    break;
                case SingleOpcodes.ldloca_s:
                    enc_ldloca(i, mtc, (int)i.inline_uint, state);
                    break;
                case SingleOpcodes.stloc_0:
                    enc_stloc(i, meth, m, 0, state);
                    break;
                case SingleOpcodes.stloc_1:
                    enc_stloc(i, meth, m, 1, state);
                    break;
                case SingleOpcodes.stloc_2:
                    enc_stloc(i, meth, m, 2, state);
                    break;
                case SingleOpcodes.stloc_3:
                    enc_stloc(i, meth, m, 3, state);
                    break;
                case SingleOpcodes.stloc_s:
                    enc_stloc(i, meth, m, (int)i.inline_uint, state);
                    break;

                case SingleOpcodes.ldelema:
                    enc_ldelema(i.stack_before[i.stack_before.Count - 2], i.stack_before[i.stack_before.Count - 1],
                        out i.pushes_variable, i, state);

                    i.pushes = new Signature.Param (this)
                    {
                        Type = new Signature.ManagedPointer
                        {
                            ElemType =
                                ((Signature.ZeroBasedArray)i.stack_before[i.stack_before.Count - 2].type.Type).ElemType
                        }
                    };
                    break;

                case SingleOpcodes.stelem_i:
                case SingleOpcodes.stelem_i1:
                case SingleOpcodes.stelem_i2:
                case SingleOpcodes.stelem_i4:
                case SingleOpcodes.stelem_i8:
                case SingleOpcodes.stelem_r4:
                case SingleOpcodes.stelem_r8:
                case SingleOpcodes.stelem_ref:
                    {
                        var elema;
                        enc_ldelema(i.stack_before[i.stack_before.Count - 3], i.stack_before[i.stack_before.Count - 2],
                            out elema, i, state);
                        i.tacs.Add(new ThreeAddressCode(GetPokeTac(new Signature.Param(GetElemType(i.opcode.opcode1), this), this),
                            var.Null, elema, i.stack_before[i.stack_before.Count - 1].contains_variable));
                    }
                    break;
                case SingleOpcodes.ldelem_i:
                case SingleOpcodes.ldelem_i1:
                case SingleOpcodes.ldelem_i2:
                case SingleOpcodes.ldelem_i4:
                case SingleOpcodes.ldelem_i8:
                case SingleOpcodes.ldelem_r4:
                case SingleOpcodes.ldelem_r8:
                case SingleOpcodes.ldelem_u1:
                case SingleOpcodes.ldelem_u2:
                case SingleOpcodes.ldelem_u4:
                    {
                        var elema;
                        Signature.BaseOrComplexType elem_type;

                        enc_ldelema(i.stack_before[i.stack_before.Count - 2], i.stack_before[i.stack_before.Count - 1],
                            out elema, i, state);

                        Signature.Param p = i.stack_before[i.stack_before.Count - 2].type;

                        /*if (p.Type is Signature.ZeroBasedArray)
                            elem_type = ((Signature.ZeroBasedArray)p.Type).ElemType;
                        else */
                            elem_type = GetElemType(i.opcode.opcode1);

                        i.tacs.Add(new ThreeAddressCode(GetPeekTac(new Signature.Param(elem_type, this), this),
                            state.next_variable++, elema, var.Null));

                        i.pushes_variable = state.next_variable - 1;
                        i.pushes = new Signature.Param(elem_type, this);
                    }
                    break;

                case SingleOpcodes.ldelem_ref:
                    {
                        var elema;
                        enc_ldelema(i.stack_before[i.stack_before.Count - 2], i.stack_before[i.stack_before.Count - 1],
                            out elema, i, state);

                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.peek_u, state.next_variable++, elema, var.Null));
                        i.pushes_variable = state.next_variable - 1;

                        Signature.Param p = i.stack_before[i.stack_before.Count - 2].type;
                        if (!(p.Type is Signature.ZeroBasedArray))
                            throw new NotSupportedException();
                        Signature.ZeroBasedArray arr = p.Type as Signature.ZeroBasedArray;
                        i.pushes = Signature.ResolveGenericParam(new Signature.Param(arr.ElemType, this), mtc.tsig, mtc.msig, this);
                    }

                    break;
                case SingleOpcodes.add:
                case SingleOpcodes.sub:
                case SingleOpcodes.mul:
                case SingleOpcodes.div:
                case SingleOpcodes.rem:
                case SingleOpcodes.add_ovf_un:
                case SingleOpcodes.sub_ovf_un:
                case SingleOpcodes.add_ovf:
                case SingleOpcodes.sub_ovf:
                case SingleOpcodes.mul_ovf:
                case SingleOpcodes.mul_ovf_un:
                    enc_binnumop(i, meth, m, i.opcode.opcode1, state);
                    break;
                case SingleOpcodes.neg:
                    enc_unnumop(i, meth, m, i.opcode.opcode1, state);
                    break;
                case SingleOpcodes.dup:
                    {
                        // Make a copy of the item on the stack

                        var v_obj = i.stack_before[i.stack_before.Count - 1].contains_variable;
                        int vt_len = GetSizeOf(i.stack_before[i.stack_before.Count - 1].type);

                        i.tacs.Add(new ThreeAddressCode(GetAssignTac(GetCliType(i.stack_before[i.stack_before.Count - 1].type.Type)), state.next_variable++, v_obj, var.Null, vt_len));

                        i.pushes = i.stack_before[i.stack_before.Count - 1].type;
                        i.pushes_variable = state.next_variable - 1;
                        break;
                    }
                case SingleOpcodes.ldnull:
                    i.pushes = new Signature.Param(this) { Type = new Signature.BaseType(BaseType_Type.Object) };
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++,
                        var.Const(0), 0));
                    i.pushes_variable = state.next_variable - 1;
                    break;
                case SingleOpcodes.newarr:
                    {
                        Assembler.TypeToCompile elem_type = Metadata.GetTTC(i.inline_tok, mtc.GetTTC(this), mtc.msig, this);
                        i.pushes = new Signature.Param(this) { Type = new Signature.ZeroBasedArray { ElemType = elem_type.tsig.Type } };

                        Assembler.TypeToCompile array_type = CreateArray(new Signature.Param(
                            new Signature.ComplexArray { _ass = this, ElemType = elem_type.tsig.Type, LoBounds = new int[] { 0 }, Sizes = new int[] { }, Rank = 1 }, this),
                            1, elem_type);
                        int array_type_size = Layout.GetClassInstanceSize(array_type, this);

                        var var_numelems = i.stack_before[i.stack_before.Count - 1].contains_variable;
                        var v_arr = state.next_variable++;
                        i.tacs.Add(new CallEx(v_arr, new var[] { var.Const(array_type_size) }, "gcmalloc", callconv_gcmalloc));
                        
                        // Set up __vtbl and __object_id
                        if (Options.EnableRTTI)
                        {
                            Assembler.TypeToCompile pushes_ttc = Metadata.GetTTC(i.pushes, mtc.GetTTC(this), mtc.msig, this);
                            Layout pushes_l = Layout.GetTypeInfoLayout(pushes_ttc, this, false);
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, var.ContentsOf(v_arr, GetStringFieldOffset(StringFields.vtbl)),
                                var.AddrOfObject(Mangler2.MangleTypeInfo(pushes_ttc, this), pushes_l.FixedLayout[Layout.ID_VTableStructure].Offset), var.Null));
                             Requestor.RequestTypeInfo(new TypeToCompile { _ass = this, tsig = i.pushes, type = array_type.type });
                       }
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, var.ContentsOf(v_arr, GetStringFieldOffset(StringFields.objid)), var.Const(next_object_id.Increment), var.Null));

                        // Call the constructor
                        bool found = false;
                        foreach (Metadata.MethodDefRow ctor_mdr in array_type.type.Methods)
                        {
                            if (ctor_mdr.IsSpecialName && (ctor_mdr.Name == ".ctor") && (ctor_mdr.msig.Method.Params.Count == 1))
                            {
                                MethodToCompile ctor_mtc = Metadata.GetMTC(new Metadata.TableIndex(ctor_mdr), array_type, null, this);
                                ctor_mtc.tsigp = array_type.tsig;
                                Mangler2.MangleMethod(ctor_mtc, this);
                                i.tacs.Add(new CallEx(var.Null, new var[] { v_arr, var_numelems }, Mangler2.MangleMethod(ctor_mtc, this), call_convs[Options.CallingConvention](ctor_mtc, CallConv.StackPOV.Caller, this, new ThreeAddressCode(ThreeAddressCode.Op.call_void))));
                                Requestor.RequestMethod(ctor_mtc);
                                found = true;
                            }
                        }
                        if(!found)
                            throw new Exception("Constructor not found");

                        i.pushes_variable = v_arr;
                    }

                    break;
                case SingleOpcodes.and:
                case SingleOpcodes.div_un:
                case SingleOpcodes.or:
                case SingleOpcodes.rem_un:
                case SingleOpcodes.xor:
                    enc_2intop(i, meth, m, i.opcode.opcode1, state);
                    break;
                case SingleOpcodes.not:
                    enc_1intop(i, meth, m, i.opcode.opcode1, state);
                    break;
                case SingleOpcodes.shl:
                case SingleOpcodes.shr:
                case SingleOpcodes.shr_un:
                    enc_shiftop(i, meth, m, i.opcode.opcode1, state);
                    break;
                case SingleOpcodes.beq:
                case SingleOpcodes.beq_s:
                case SingleOpcodes.bge:
                case SingleOpcodes.bge_s:
                case SingleOpcodes.bge_un:
                case SingleOpcodes.bge_un_s:
                case SingleOpcodes.bgt:
                case SingleOpcodes.bgt_s:
                case SingleOpcodes.bgt_un:
                case SingleOpcodes.bgt_un_s:
                case SingleOpcodes.ble:
                case SingleOpcodes.ble_s:
                case SingleOpcodes.ble_un:
                case SingleOpcodes.ble_un_s:
                case SingleOpcodes.blt:
                case SingleOpcodes.blt_s:
                case SingleOpcodes.blt_un:
                case SingleOpcodes.blt_un_s:
                case SingleOpcodes.bne_un:
                case SingleOpcodes.bne_un_s:
                case SingleOpcodes.brtrue:
                case SingleOpcodes.brtrue_s:
                case SingleOpcodes.brfalse:
                case SingleOpcodes.brfalse_s:
                    enc_brif(i, meth, m, i.opcode.opcode1, cfg_node, state);
                    break;

                case SingleOpcodes.leave:
                case SingleOpcodes.leave_s:
                    i.stack_after = new List<PseudoStack>();

                    // is this a protected instruction block? If so, call the handler
                    if (mtc.meth.Body.exceptions != null)
                    {
                        foreach (Metadata.MethodBody.EHClause ehclause in mtc.meth.Body.exceptions)
                        {
                            if (ehclause.IsFinally && (i.il_offset >= (int)ehclause.TryOffset) && (i.il_offset < (int)(ehclause.TryOffset + ehclause.TryLength)))
                            {
                                // we have found a handling finally block
                                // we should only call the handler if the target is outside of the try block
                                if (((i.il_offset_after + i.inline_int) < (int)ehclause.TryOffset) || ((i.il_offset_after + i.inline_int) >= (int)(ehclause.TryOffset + ehclause.TryLength)))
                                {
                                    // we have found a handling finally block, now find its node
                                    cfg_node handling_node = null;
                                    foreach (cfg_node check_node in nodes)
                                    {
                                        if (check_node.il_offset == ehclause.HandlerOffset)
                                        {
                                            handling_node = check_node;
                                            break;
                                        }
                                    }
                                    if (handling_node == null)
                                        throw new Exception("Cannot find a node that corresponds to the finally handler block");
                                    i.tacs.Add(new BrEx(ThreeAddressCode.Op.br_ehclause, handling_node.block_id));
                                }
                            }
                        }
                    }

                    /* if(is_within_protected_block(i, mtc.meth.Body.exceptions))
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throw_, var.Null, var.Const(-1), var.Null)); */
                    i.tacs.Add(new BrEx(ThreeAddressCode.Op.br, cfg_node.isuc[0].block_id));
                    cfg_node.has_fall_through = false;
                    break;

                case SingleOpcodes.br:
                case SingleOpcodes.br_s:
                    i.tacs.Add(new BrEx(ThreeAddressCode.Op.br, cfg_node.isuc[0].block_id));
                    cfg_node.has_fall_through = false;
                    break;

                case SingleOpcodes.conv_i:
                    enc_conv(i, meth, m, BaseType_Type.I, false, state);
                    break;
                case SingleOpcodes.conv_i1:
                    enc_conv(i, meth, m, BaseType_Type.I1, false, state);
                    break;
                case SingleOpcodes.conv_i2:
                    enc_conv(i, meth, m, BaseType_Type.I2, false, state);
                    break;
                case SingleOpcodes.conv_i4:
                    enc_conv(i, meth, m, BaseType_Type.I4, false, state);
                    break;
                case SingleOpcodes.conv_i8:
                    enc_conv(i, meth, m, BaseType_Type.I8, false, state);
                    break;
                case SingleOpcodes.conv_ovf_i:
                    enc_conv(i, meth, m, BaseType_Type.I, true, state);
                    break;
                case SingleOpcodes.conv_ovf_i_un:
                    enc_conv(i, meth, m, BaseType_Type.I, true, true, state);
                    break;
                case SingleOpcodes.conv_ovf_i1:
                    enc_conv(i, meth, m, BaseType_Type.I1, true, state);
                    break;
                case SingleOpcodes.conv_ovf_i1_un:
                    enc_conv(i, meth, m, BaseType_Type.I1, true, true, state);
                    break;
                case SingleOpcodes.conv_ovf_i2:
                    enc_conv(i, meth, m, BaseType_Type.I2, true, state);
                    break;
                case SingleOpcodes.conv_ovf_i2_un:
                    enc_conv(i, meth, m, BaseType_Type.I2, true, true, state);
                    break;
                case SingleOpcodes.conv_ovf_i4:
                    enc_conv(i, meth, m, BaseType_Type.I4, true, state);
                    break;
                case SingleOpcodes.conv_ovf_i4_un:
                    enc_conv(i, meth, m, BaseType_Type.I4, true, true, state);
                    break;
                case SingleOpcodes.conv_ovf_i8:
                    enc_conv(i, meth, m, BaseType_Type.I8, true, state);
                    break;
                case SingleOpcodes.conv_ovf_i8_un:
                    enc_conv(i, meth, m, BaseType_Type.I8, true, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u:
                    enc_conv(i, meth, m, BaseType_Type.U, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u_un:
                    enc_conv(i, meth, m, BaseType_Type.U, true, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u1:
                    enc_conv(i, meth, m, BaseType_Type.U1, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u1_un:
                    enc_conv(i, meth, m, BaseType_Type.U1, true, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u2:
                    enc_conv(i, meth, m, BaseType_Type.U2, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u2_un:
                    enc_conv(i, meth, m, BaseType_Type.U2, true, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u4:
                    enc_conv(i, meth, m, BaseType_Type.U4, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u4_un:
                    enc_conv(i, meth, m, BaseType_Type.U4, true, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u8:
                    enc_conv(i, meth, m, BaseType_Type.U8, true, state);
                    break;
                case SingleOpcodes.conv_ovf_u8_un:
                    enc_conv(i, meth, m, BaseType_Type.U8, true, true, state);
                    break;
                case SingleOpcodes.conv_r4:
                    enc_conv(i, meth, m, BaseType_Type.R4, false, state);
                    break;
                case SingleOpcodes.conv_r8:
                    enc_conv(i, meth, m, BaseType_Type.R8, false, state);
                    break;
                case SingleOpcodes.conv_u:
                    enc_conv(i, meth, m, BaseType_Type.U, false, state);
                    break;
                case SingleOpcodes.conv_u1:
                    enc_conv(i, meth, m, BaseType_Type.U1, false, state);
                    break;
                case SingleOpcodes.conv_u2:
                    enc_conv(i, meth, m, BaseType_Type.U2, false, state);
                    break;
                case SingleOpcodes.conv_u4:
                    enc_conv(i, meth, m, BaseType_Type.U4, false, state);
                    break;
                case SingleOpcodes.conv_u8:
                    enc_conv(i, meth, m, BaseType_Type.U8, false, state);
                    break;
                case SingleOpcodes.conv_r_un:
                    enc_conv(i, meth, m, BaseType_Type.R8, false, state);
                    break;

                case SingleOpcodes.double_:
                    switch (i.opcode.opcode2)
                    {
                        case DoubleOpcodes.ldarg:
                            enc_ldarg(i, meth, m, i.inline_int, state);
                            break;
                        case DoubleOpcodes.ldloc:
                            enc_ldloc(i, meth, m, i.inline_int, state);
                            break;
                        case DoubleOpcodes.ldloca:
                            enc_ldloca(i, mtc, i.inline_int, state);
                            break;
                        case DoubleOpcodes.starg:
                            enc_starg(i, meth, m, i.inline_int, state);
                            break;
                        case DoubleOpcodes.ceq:
                        case DoubleOpcodes.cgt:
                        case DoubleOpcodes.cgt_un:
                        case DoubleOpcodes.clt:
                        case DoubleOpcodes.clt_un:
                            enc_brif(i, meth, m, i.opcode.opcode1, i.opcode.opcode2, cfg_node, state);
                            break;
                        case DoubleOpcodes.ldarga:
                            enc_ldarga(i, mtc, i.inline_int, state);
                            break;
                        case DoubleOpcodes.localloc:
                            enc_localloc(i, state);
                            break;
                        case DoubleOpcodes.ldftn:
                        case DoubleOpcodes.ldvirtftn:
                            enc_call(i, mtc, state);
                            //enc_ldftn(i, mtc, state);
                            break;
                        case DoubleOpcodes.initobj:
                            enc_initobj(i, mtc, state);
                            break;
                        case DoubleOpcodes.rethrow:
                            {
                                var v_catchobj = state.next_variable++;
                                var v_methinfo = state.next_variable++;

                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.ldcatchobj, v_catchobj, var.Null, var.Null));
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.ldmethinfo, v_methinfo, var.Null, var.Null));
                                i.tacs.Add(new CallEx(var.Null, new var[] { v_catchobj, v_methinfo }, "throw", callconv_throw));
                                cfg_node.has_fall_through = false;
                                break;
                            }
                        case DoubleOpcodes.stloc:
                            enc_stloc(i, meth, m, i.inline_int, state);
                            break;
                        default:
                            throw new NotImplementedException(i.opcode.name);
                    }
                    break;

                case SingleOpcodes.tysila:
                    switch (i.opcode.opcode2)
                    {
                        case DoubleOpcodes.flip:
                            i.stack_after = new List<PseudoStack>(i.stack_before);
                            int flip_dist = i.stack_before_adjust;
                            if (flip_dist == 0)
                                flip_dist = 1;
                            i.stack_after[i.stack_before.Count - 1] = i.stack_before[i.stack_before.Count - 1 - flip_dist];
                            i.stack_after[i.stack_before.Count - 1 - flip_dist] = i.stack_before[i.stack_before.Count - 1];
                            break;
                        case DoubleOpcodes.pushback:
                            {
                                i.stack_after = new List<PseudoStack>(i.stack_before);
                                PseudoStack move_value = i.stack_after[i.stack_after.Count - 1];
                                i.stack_after.Insert(i.stack_after.Count - 1 - i.inline_int, move_value);
                                i.stack_after.RemoveAt(i.stack_after.Count - 1);
                                break;
                            }
                        case DoubleOpcodes.bringforward:
                            {
                                /* V0, V1, V2, V3, V4
                                 * 
                                 * Count = 5
                                 * To bring forward V2 (bringforward 2):
                                 * move_value = stack(Count - inline_int - 1)
                                 * RemoveAt(Count - inline_int - 1)
                                 * Add(move_value)
                                 */

                                i.stack_after = new List<PseudoStack>(i.stack_before);
                                PseudoStack move_value = i.stack_after[i.stack_after.Count - 1 - i.inline_int];
                                i.stack_after.RemoveAt(i.stack_after.Count - 1 - i.inline_int);
                                i.stack_after.Add(move_value);
                                break;
                            }
                        case DoubleOpcodes.flip3:
                            i.stack_after = new List<PseudoStack>(i.stack_before);
                            i.stack_after[i.stack_before.Count - 1] = i.stack_before[i.stack_before.Count - 3];
                            i.stack_after[i.stack_before.Count - 3] = i.stack_before[i.stack_before.Count - 1];
                            break;
                        case DoubleOpcodes.init_rth:
                        case DoubleOpcodes.init_rfh:
                        case DoubleOpcodes.init_rmh:
                            enc_initrth(i, mtc, m, i.inline_tok);
                            break;
                        case DoubleOpcodes.castclassex:
                            enc_castclassex(i, mtc, i.inline_tok, state);
                            break;
                        case DoubleOpcodes.throwfalse:
                            {
                                ThreeAddressCode.Op cmptac = ThreeAddressCode.Op.cmp_i;

                                if (i.stack_before[i.stack_before.Count - 1].type.CliType(this) == CliType.int32)
                                    cmptac = ThreeAddressCode.Op.cmp_i4;
                                i.tacs.Add(new ThreeAddressCode(cmptac, var.Null,
                                    i.stack_before[i.stack_before.Count - 1].contains_variable, var.Const(0)));
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throweq, var.Null,
                                    var.Const(throw_InvalidCastException), var.Null));
                            }
                            break;
                        case DoubleOpcodes.throwtrue:
                            {
                                ThreeAddressCode.Op cmptac = ThreeAddressCode.Op.cmp_i;

                                if (i.stack_before[i.stack_before.Count - 1].type.CliType(this) == CliType.int32)
                                    cmptac = ThreeAddressCode.Op.cmp_i4;
                                i.tacs.Add(new ThreeAddressCode(cmptac, var.Null,
                                    i.stack_before[i.stack_before.Count - 1].contains_variable, var.Const(0)));
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throwne, var.Null,
                                    var.Const(throw_InvalidCastException), var.Null));
                            }
                            break;
                        case DoubleOpcodes.profile:
                            i.tacs.Add(new CallEx(var.Null, new var[] { i.stack_before[i.stack_before.Count - 1].contains_variable }, "__profile", callconv_profile));
                            state.mangled_name_var = i.stack_before[i.stack_before.Count - 1].contains_variable;
                            break;
                        case DoubleOpcodes.gcmalloc:
                            {
                                var v_size = i.stack_before[i.stack_before.Count - 1].contains_variable;
                                var v_mem = state.next_variable++;
                                i.tacs.Add(new CallEx(v_mem, new var[] { v_size }, "gcmalloc", callconv_gcmalloc));
                                i.pushes_variable = v_mem;
                                i.pushes = Metadata.GetTTC(i.inline_tok, mtc.GetTTC(this), mtc.msig, this).tsig;
                                break;
                            }
                        case DoubleOpcodes.ldobj_addr:
                            {
                                var v_obj_addr = state.next_variable++;
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_obj_addr, var.AddrOfObject(((StringToken)i.inline_tok).str, i.inline_int), var.Null));
                                i.pushes_variable = v_obj_addr;
                                i.pushes = new Signature.Param(BaseType_Type.I);
                                break;
                            }
                        case DoubleOpcodes.mbstrlen:
                            {
                                i.tacs.Add(new CallEx(state.next_variable++, new var[] { i.stack_before[i.stack_before.Count - 1].contains_variable }, "__mbstrlen", callconv_mbstrlen));
                                i.pushes_variable = state.next_variable - 1;
                                i.pushes = new Signature.Param(BaseType_Type.I4);
                                break;
                            }
                        case DoubleOpcodes.loadcatchobj:
                            {
                                var v_catchobj = state.next_variable++;
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.ldcatchobj, v_catchobj, var.Null, var.Null));
                                i.pushes_variable = v_catchobj;
                                i.pushes = new Signature.Param(i.inline_tok, this);
                                break;
                            }
                        case DoubleOpcodes.instruction_label:
                            i.tacs.Add(new InstructionLabelEx(((InstructionLabel)i).instr));
                            break;
                        default:
                            throw new NotImplementedException(i.opcode.name);
                    }
                    break;

                case SingleOpcodes.ldc_i4:
                case SingleOpcodes.ldc_i4_s:
                    enc_ldci4(i, meth, m, i.inline_int, state);
                    break;
                case SingleOpcodes.ldc_i4_0:
                    enc_ldci4(i, meth, m, 0, state);
                    break;
                case SingleOpcodes.ldc_i4_1:
                    enc_ldci4(i, meth, m, 1, state);
                    break;
                case SingleOpcodes.ldc_i4_2:
                    enc_ldci4(i, meth, m, 2, state);
                    break;
                case SingleOpcodes.ldc_i4_3:
                    enc_ldci4(i, meth, m, 3, state);
                    break;
                case SingleOpcodes.ldc_i4_4:
                    enc_ldci4(i, meth, m, 4, state);
                    break;
                case SingleOpcodes.ldc_i4_5:
                    enc_ldci4(i, meth, m, 5, state);
                    break;
                case SingleOpcodes.ldc_i4_6:
                    enc_ldci4(i, meth, m, 6, state);
                    break;
                case SingleOpcodes.ldc_i4_7:
                    enc_ldci4(i, meth, m, 7, state);
                    break;
                case SingleOpcodes.ldc_i4_8:
                    enc_ldci4(i, meth, m, 8, state);
                    break;
                case SingleOpcodes.ldc_i4_m1:
                    enc_ldci4(i, meth, m, -1, state);
                    break;
                case SingleOpcodes.ldc_i8:
                    enc_ldci8(i, meth, m, i.inline_int64, state);
                    break;
                case SingleOpcodes.ldc_r4:
                    enc_ldcr4(i, meth, m, i.inline_sgl, state);
                    break;
                case SingleOpcodes.ldc_r8:
                    enc_ldcr8(i, meth, m, i.inline_dbl, state);
                    break;
                case SingleOpcodes.throw_:
                    {
                        var v_exception_obj = i.stack_before[i.stack_before.Count - 1].contains_variable;
                        var v_methinfo = state.next_variable++;
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.ldmethinfo, v_methinfo, var.Null, var.Null));
                        i.tacs.Add(new CallEx(0, new var[] { i.stack_before[i.stack_before.Count - 1].contains_variable, v_methinfo },
                            ThreeAddressCode.Op.call_void, "throw", callconv_throw));
                        cfg_node.has_fall_through = false;
                        break;
                    }
                case SingleOpcodes.stobj:
                    enc_stobj(i, meth, m, mtc.GetTTC(this), mtc, state);
                    break;
                case SingleOpcodes.ldlen:
                    if ((i.stack_before[i.stack_before.Count - 1].type.Type is Signature.ZeroBasedArray) && (
                        ((Signature.ZeroBasedArray)i.stack_before[i.stack_before.Count - 1].type.Type).numElems >= 0))
                    {
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++,
                            var.Const(((Signature.ZeroBasedArray)i.stack_before[i.stack_before.Count - 1].type.Type).numElems), var.Null));
                    }
                    else
                    {
                        var obj_var = i.stack_before[i.stack_before.Count - 1].contains_variable;
                        if (obj_var.type != var.var_type.LogicalVar)
                        {
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++,
                                obj_var, var.Null));
                            obj_var = state.next_variable - 1;
                        }

                        /* Get the size of rank 0 */
                        var v_sizes = state.next_variable++;
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_sizes, var.ContentsOf(obj_var, GetArrayFieldOffset(ArrayFields.sizes)), var.Null));
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.ContentsOf(v_sizes), var.Null));
                    }

                    i.pushes = new Signature.Param(BaseType_Type.U);
                    i.pushes_variable = state.next_variable - 1;
                    break;
                case SingleOpcodes.switch_:
                    var target = i.stack_before[i.stack_before.Count - 1].contains_variable;
                    cfg_node.has_fall_through = false;

                    if (Implements(ThreeAddressCode.Op.switch_))
                    {
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, target, var.Const(i.inline_array.Count)));
                        i.tacs.Add(new BrEx(ThreeAddressCode.Op.bge, cfg_node.isuc[i.inline_array.Count].block_id));
                        SwitchEx swe = new SwitchEx();
                        for (int sw_i = 0; sw_i < i.inline_array.Count; sw_i++)
                            swe.Block_Targets.Add(cfg_node.isuc[sw_i].block_id);
                        i.tacs.Add(swe);
                    }
                    else
                    {
                        /* If the current assembler does not implement switch_ to create a jump table then implement as
                         * a sequence of tests and jumps */
                        for (int sw_i = 0; sw_i < i.inline_array.Count; sw_i++)
                        {
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, target, var.Const(sw_i)));
                            i.tacs.Add(new BrEx(ThreeAddressCode.Op.beq, cfg_node.isuc[sw_i].block_id));
                        }
                    }
                    break;
                case SingleOpcodes.nop:
                    break;
                case SingleOpcodes.endfinally:
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.endfinally));
                    cfg_node.has_fall_through = false;
                    break;
                default:
                    throw new NotImplementedException(i.opcode.name);
            }
        }

        private CliType GetCliType(Assembler.TypeToCompile ttc)
        { return GetCliType(ttc.tsig.Type); }

        private CliType GetCliType(Signature.BaseOrComplexType sig)
        {
            if (sig is Signature.BaseType)
            {
                Signature.BaseType bt = sig as Signature.BaseType;
                switch (bt.Type)
                {
                    case BaseType_Type.Array:
                    case BaseType_Type.Boxed:
                    case BaseType_Type.Class:
                    case BaseType_Type.Object:
                    case BaseType_Type.String:
                    case BaseType_Type.SzArray:
                    case BaseType_Type.RefGenericParam:
                        return CliType.O;

                    case BaseType_Type.Boolean:
                    case BaseType_Type.Byte:
                    case BaseType_Type.Char:
                    case BaseType_Type.I1:
                    case BaseType_Type.I2:
                    case BaseType_Type.I4:
                    case BaseType_Type.U1:
                    case BaseType_Type.U2:
                    case BaseType_Type.U4:
                        return CliType.int32;

                    case BaseType_Type.I8:
                    case BaseType_Type.U8:
                        return CliType.int64;

                    case BaseType_Type.I:
                    case BaseType_Type.U:
                        return CliType.native_int;

                    case BaseType_Type.Byref:
                    case BaseType_Type.Ptr:
                        return CliType.reference;

                    case BaseType_Type.R4:
                        return CliType.F32;
                    case BaseType_Type.R8:
                        return CliType.F64;

                    case BaseType_Type.Void:
                        return CliType.void_;

                    case BaseType_Type.ValueType:
                    case BaseType_Type.TypedByRef:
                        return CliType.vt;

                    default:
                        throw new NotSupportedException();
                }
            }
            else if (sig is Signature.ManagedPointer)
                return CliType.reference;
            else if (sig is Signature.UnmanagedPointer)
                return CliType.native_int;
            else if (sig is Signature.BoxedType)
                return CliType.O;
            else if (sig is Signature.ZeroBasedArray)
                return CliType.O;
            else if (sig is Signature.ComplexArray)
                return CliType.O;
            else if (sig is Signature.GenericType)
                return GetCliType(((Signature.GenericType)sig).GenType);
            else if (sig is Signature.ComplexType)
            {
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(sig, this);

                if (tdr.IsValueType(this))
                {
                    if (tdr.IsEnum(this))
                        return new Signature.Param(sig, this).CliType(this);
                    return CliType.vt;
                }
                else
                    return CliType.O;
            }
            else
                throw new NotSupportedException();
        }

        private void enc_initobj(InstructionLine i, MethodToCompile mtc, AssemblerState state)
        {
            Assembler.TypeToCompile ttc = Metadata.GetTTC(i.inline_tok, mtc.GetTTC(this), mtc.msig, this);
            int size = GetSizeOf(ttc.tsig);
            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.zeromem, var.Null, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Const(size)));
        }

        private void enc_localloc(InstructionLine i, AssemblerState state)
        {
            ThreeAddressCode.Op alloca_inst = ThreeAddressCode.Op.alloca_i;

            Signature.BaseType bt = i.stack_before[i.stack_before.Count - 1].type.Type as Signature.BaseType;
            if (bt == null)
                throw new Exception("Invalid argument to localloc");
            if ((bt.Type == BaseType_Type.U) || (bt.Type == BaseType_Type.I))
                alloca_inst = ThreeAddressCode.Op.alloca_i;
            else if ((bt.Type == BaseType_Type.U4) || (bt.Type == BaseType_Type.I4))
                alloca_inst = ThreeAddressCode.Op.alloca_i4;
            else
                throw new Exception("Invalid argument to localloc");

            i.tacs.Add(new ThreeAddressCode(alloca_inst, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            i.pushes = new Signature.Param(BaseType_Type.I);
            i.pushes_variable = state.next_variable - 1;
        }

        private void enc_castclassex(InstructionLine i, Assembler.MethodToCompile mtc, Token token, AssemblerState state)
        {
            // if obj can be cast to obj2 easily, do it
            // else invoke a runtime helper

            // ..., src -> ..., dest/null

            Signature.Param dest;

            i.stack_after = new List<PseudoStack>(i.stack_before);
            Assembler.TypeToCompile dest_ttc;
            if(token is TTCToken)
                dest_ttc = ((TTCToken)token).ttc;
            else
                dest_ttc = Metadata.GetTTC(token, mtc.GetTTC(this), mtc.msig, this);
            dest = dest_ttc.tsig;
            if ((dest_ttc.type != null) && (dest_ttc.type.IsValueType(this)) && (!(dest.Type is Signature.BoxedType)))
                dest = new Signature.Param(new Signature.BoxedType(dest.Type), this);
            i.stack_after[i.stack_after.Count - 1 - i.stack_before_adjust] = new PseudoStack { contains_variable = i.stack_before[i.stack_before.Count - 1 - i.stack_before_adjust].contains_variable, type = dest };

            /* See if we can do a cast at compile time */
            if (can_cast(dest.Type, i.stack_before[i.stack_before.Count - 1 - i.stack_before_adjust].type.Type))
                return;

            /* Else do a runtime dynamic cast */
            Layout dest_l = Layout.GetTypeInfoLayout(dest_ttc, this, false, false);
            i.tacs.Add(new CallEx(state.next_variable++,
                new var[] { i.stack_before[i.stack_before.Count - 1 - i.stack_before_adjust].contains_variable, 
                    var.AddrOfObject(Mangler2.MangleTypeInfo(dest_ttc, this), dest_l.FixedLayout[Layout.ID_VTableStructure].Offset) },
                    ThreeAddressCode.Op.call_i, "castclassex", callconv_castclassex));
            i.stack_after[i.stack_after.Count - 1 - i.stack_before_adjust].contains_variable = state.next_variable - 1;
            Requestor.RequestTypeInfo(dest_ttc);
        }

        private void enc_initrth(InstructionLine i, Assembler.MethodToCompile mtc, Metadata m, Token token)
        {
            /* Can be used to initialise one of RuntimeTypeHandle, RuntimeMethodHandle or RuntimeFieldHandle */
            string ti_name;
            int offset = 0;
            string sym_name;

            switch (i.opcode.opcode2)
            {
                case DoubleOpcodes.init_rfh:
                    {
                        Assembler.FieldToCompile tok_ftc = Metadata.GetFTC(new Metadata.TableIndex(token), mtc.GetTTC(this), mtc.msig, this);
                        sym_name = Mangler2.MangleFieldInfoSymbol(tok_ftc, this);
                        ti_name = Mangler2.MangleTypeInfo(tok_ftc.DefinedIn, this);
                        Layout l2 = Layout.GetTypeInfoLayout(tok_ftc.DefinedIn, this, false);
                        offset = l2.Symbols[sym_name];
                    }
                    break;

                case DoubleOpcodes.init_rmh:
                    {
                        Assembler.MethodToCompile tok_mtc = Metadata.GetMTC(new Metadata.TableIndex(token), mtc.GetTTC(this), mtc.msig, this);
                        sym_name = Mangler2.MangleMethodInfoSymbol(tok_mtc, this);
                        ti_name = Mangler2.MangleTypeInfo(tok_mtc.GetTTC(this), this);
                        Layout l2 = Layout.GetTypeInfoLayout(tok_mtc.GetTTC(this), this, false);
                        offset = l2.Symbols[sym_name];
                    }
                    break;

                case DoubleOpcodes.init_rth:
                    Assembler.TypeToCompile tok_ttc = Metadata.GetTTC(new Metadata.TableIndex(token), mtc.GetTTC(this), mtc.msig, this);
                    ti_name = Mangler2.MangleTypeInfo(tok_ttc, this);
                    Requestor.RequestTypeInfo(tok_ttc);
                    break;

                default:
                    throw new NotSupportedException();
            }

            Assembler.TypeToCompile rh_ttc = new TypeToCompile(i.stack_before[i.stack_before.Count - 1].type, this);
            Layout l = Layout.GetLayout(rh_ttc, this);

            int fld_offset = l.GetField("IntPtr value", false).offset;

            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i,
                var.ContentsOf(i.stack_before[i.stack_before.Count - 1].contains_variable, fld_offset),
                var.AddrOfObject(ti_name, offset), var.Null));
        }

        public static ThreeAddressCode.Op GetPeekTac(Signature.Param val, Assembler assembler)
        {
            ThreeAddressCode.Op poketac = GetPokeTac(val, assembler);
            switch (poketac)
            {
                case ThreeAddressCode.Op.poke_u:
                    return ThreeAddressCode.Op.peek_u;
                case ThreeAddressCode.Op.poke_u1:
                    return ThreeAddressCode.Op.peek_u1;
                case ThreeAddressCode.Op.poke_u2:
                    return ThreeAddressCode.Op.peek_u2;
                case ThreeAddressCode.Op.poke_u4:
                    return ThreeAddressCode.Op.peek_u4;
                case ThreeAddressCode.Op.poke_u8:
                    return ThreeAddressCode.Op.peek_u8;
                case ThreeAddressCode.Op.poke_r4:
                    return ThreeAddressCode.Op.peek_r4;
                case ThreeAddressCode.Op.poke_r8:
                    return ThreeAddressCode.Op.peek_r8;
                default:
                    throw new NotSupportedException();
            }
        }

        public static ThreeAddressCode.Op GetPokeTac(Signature.Param val, Assembler assembler)
        {
            if (val.Type is Signature.BaseType)
            {
                Signature.BaseType bt = val.Type as Signature.BaseType;
                switch (bt.Type)
                {
                    case BaseType_Type.Byte:
                    case BaseType_Type.I1:
                    case BaseType_Type.U1:
                    case BaseType_Type.Boolean:
                        return ThreeAddressCode.Op.poke_u1;
                    case BaseType_Type.Char:
                    case BaseType_Type.U2:
                    case BaseType_Type.I2:
                        return ThreeAddressCode.Op.poke_u2;
                    case BaseType_Type.I4:
                    case BaseType_Type.U4:
                        return ThreeAddressCode.Op.poke_u4;
                    case BaseType_Type.I:
                    case BaseType_Type.U8:
                    case BaseType_Type.I8:
                    case BaseType_Type.Object:
                    case BaseType_Type.String:
                        return ThreeAddressCode.Op.poke_u;
                    case BaseType_Type.R4:
                        return ThreeAddressCode.Op.poke_r4;
                    case BaseType_Type.R8:
                        return ThreeAddressCode.Op.poke_r8;
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (val.Type is Signature.ComplexType)
            {
                Signature.ComplexType ct = val.Type as Signature.ComplexType;
                if (ct.isValueType)
                    throw new NotSupportedException();
                return ThreeAddressCode.Op.poke_u;
            }
            else if (val.Type is Signature.BoxedType)
                return ThreeAddressCode.Op.poke_u;
            else if (val.Type is Signature.ZeroBasedArray)
                return ThreeAddressCode.Op.poke_u;
            else
            {
                throw new Exception();
            }
        }

        private void enc_newobj(InstructionLine i, MethodToCompile mtc, Metadata m, AssemblerState state)
        {
            // ..., arg1, ... argN -> obj

            // Determine the type to create and optionally the constructor to call afterwards
            Assembler.MethodToCompile ?constructor;
            Assembler.TypeToCompile type;

            if (i.inline_tok is TTCToken)
            {
                type = ((TTCToken)i.inline_tok).ttc;
                type.tsig = Signature.ResolveGenericParam(type.tsig, mtc.tsig, mtc.msig, this);
                constructor = null;
            }
            else if (i.inline_tok is MTCToken)
            {
                type = ((MTCToken)i.inline_tok).mtc.GetTTC(this);
                type.tsig = Signature.ResolveGenericParam(type.tsig, mtc.tsig, mtc.msig, this);
                constructor = ((MTCToken)i.inline_tok).mtc;
            }
            else if (i.inline_tok.Value is Metadata.TypeDefRow)
                throw new NotSupportedException();
            else
            {
                constructor = Metadata.GetMTC(new Metadata.TableIndex(i.inline_tok), mtc.GetTTC(this), mtc.msig, this);
                type = constructor.Value.GetTTC(this);
            }

            i.pushes = type.tsig;
            Layout l = Layout.GetLayout(type, this, false);

            // Allocate space for the new object
            var var_obj = state.next_variable++;
            if (type.type.IsValueType(this) && !(type.tsig.Type is Signature.BoxedType))
            {
                // Value types created with newobj are created on the stack
                var_obj.v_size = l.ClassSize;
                var_obj.is_address_of_vt = true;                
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.alloca_i, var_obj, var.Const(l.ClassSize), var.Null));
            }
            else
            {
                i.tacs.Add(new CallEx(var_obj,
                    new var[] { var.Const(l.ClassSize) },
                    ThreeAddressCode.Op.call_i, "gcmalloc", callconv_gcmalloc));
            }

            // Fill in the various runtime initialized fields
            if (l.has_vtbl)
            {
                l = Layout.GetTypeInfoLayout(type, this, false);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, var.ContentsOf(var_obj, l.vtbl_offset), var.AddrOfObject(l.typeinfo_object_name, l.FixedLayout[Layout.ID_VTableStructure].Offset), var.Null));
            }
            if (l.has_obj_id)
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, var.ContentsOf(var_obj, l.obj_id_offset), var.Const(next_object_id.Increment), var.Null));

            // run constructors
            if (constructor == null)
            {
                Requestor.RequestTypeInfo(type);
                i.pop_count = 0;
            }
            else
            {
                bool _is_ctor = false;

                if (((constructor.Value.meth.Flags & 0x800) == 0x800) && (constructor.Value.meth.Name == ".ctor"))
                {
                    _is_ctor = true;
                    Requestor.RequestTypeInfo(type);
                }

                if (_is_ctor && type.type.IsDelegate(this))
                {
                    /* requests to .ctor on delegate types need to be rewritten to have their last argument be virtftnptr rather
                     * than IntPtr */

                    /* Because the method may already be listed in Requestor's lists, we need to update the reference in them */
                    if (Requestor is FileBasedMemberRequestor)
                    {
                        FileBasedMemberRequestor r = Requestor as FileBasedMemberRequestor;

                        lock (Requestor.gmi_lock)
                        {
                            lock (Requestor.meth_lock)
                            {
                                bool _in_cm = false;
                                bool _in_rm = false;

                                if (r._compiled_meths.ContainsKey(constructor.Value))
                                {
                                    _in_cm = true;
                                    r._compiled_meths.Remove(constructor.Value);
                                }

                                Signature.Method msig = constructor.Value.msig.Method;
                                msig.Params[1] = new Signature.Param(BaseType_Type.VirtFtnPtr);

                                if (_in_cm)
                                    r._compiled_meths.Add(constructor.Value, 0);
                                if (_in_rm)
                                    r._requested_meths.Add(constructor.Value, 0);
                            }
                        }
                    }
                    else
                    {
                        // JIT member requestor handles this for us automatically
                        //throw new Exception("JIT not yet supported");
                    }
                }

                if (constructor.Value.msig is Signature.Method)
                {
                    Signature.Method msigm = constructor.Value.msig as Signature.Method;
                    int arg_count = msigm.Params.Count;
                    if ((msigm.HasThis == true) && (msigm.ExplicitThis == false))
                        arg_count++;
                    i.pop_count = arg_count - 1;

                    var[] var_args = new var[arg_count];
                    var_args[0] = var_obj;
                    for (int j = 1; j < arg_count; j++)
                        var_args[j] = i.stack_before[i.stack_before.Count - (arg_count - j)].contains_variable;

                    string callconv = Options.CallingConvention;
                    if ((constructor.Value.meth != null) && (constructor.Value.meth.CallConvOverride != null))
                        callconv = constructor.Value.meth.CallConvOverride;

                    i.tacs.Add(new CallEx(0, var_args, GetCallTac(msigm.RetType.CliType(this)),
                        Mangler2.MangleMethod(constructor.Value, this), call_convs[callconv](constructor.Value,
                        CallConv.StackPOV.Caller, this, new ThreeAddressCode(GetCallTac(msigm.RetType.CliType(this))))));
                }
                else
                {
                    throw new NotImplementedException();
                }

                Requestor.RequestMethod(constructor.Value);
            }

            i.pushes_variable = var_obj;
        }

        private void enc_stobj(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, Assembler.TypeToCompile containing_type, Assembler.MethodToCompile containing_meth, AssemblerState state)
        {
            // ..., dest, src -> ...,
            // Store reference or value type src to the managed pointer dest

            Signature.Param srcType = i.stack_before[i.stack_before.Count - 1].type;

            if (Signature.ParamCompare(i.stack_before[i.stack_before.Count - 2].type, new Signature.Param(BaseType_Type.U), this) ||
                Signature.ParamCompare(i.stack_before[i.stack_before.Count - 2].type, new Signature.Param(new Signature.UnmanagedPointer { _ass = this, BaseType = new Signature.BaseType(BaseType_Type.Void) }, this), this))
            {
                if (state.security.AllowUnsafeMemoryAccess)
                {
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, var.ContentsOf(i.stack_before[i.stack_before.Count - 2].contains_variable),
                        i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, GetSizeOf(srcType)));
                    return;
                }
                else
                    throw new VerificationException("Assigning to unsafe pointer is not allowed", i, containing_meth);
            }

            Signature.Param destType;
            if (i.stack_before[i.stack_before.Count - 2].type.Type is Signature.UnmanagedPointer)
            {
                if (state.security.AllowUnsafeMemoryAccess)
                    destType = new Signature.Param(((Signature.UnmanagedPointer)i.stack_before[i.stack_before.Count - 2].type.Type).BaseType, this);
                else
                    throw new VerificationException("Assigning to unsafe pointer is not allowed", i, containing_meth);
            }
            else
                destType = new Signature.Param(((Signature.ManagedPointer)i.stack_before[i.stack_before.Count - 2].type.Type).ElemType, this);
            Assembler.TypeToCompile typeTok = Metadata.GetTTC(i.inline_tok, containing_type, containing_meth.msig, this);

            if (!is_assignment_compatible(typeTok.tsig, srcType))
                throw new NotSupportedException();
            if (!is_assignment_compatible(destType, typeTok.tsig))
                throw new NotSupportedException();

            if (GetStObjTac(destType.CliType(this)) == ThreeAddressCode.Op.stobj_vt)
            {
                i.tacs.Add(new ThreeAddressCode(GetStObjTac(destType.CliType(this)), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable,
                    GetSizeOf(srcType)));
            }
            else
            {
                i.tacs.Add(new ThreeAddressCode(GetAssignTac(destType.CliType(this)),
                    var.ContentsOf(i.stack_before[i.stack_before.Count - 2].contains_variable),
                    i.stack_before[i.stack_before.Count - 1].contains_variable,
                    var.Null, GetSizeOf(srcType)));
            }
        }

        private void enc_starg(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, int p, AssemblerState state)
        {
            /*
            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.ldarga, next_variable++, p, 0));
            i.tacs.Add(new ThreeAddressCode(GetStObjTac(i.stack_before[i.stack_before.Count - 1].type.CliType(this)),
                0, next_variable - 1, i.stack_before[i.stack_before.Count - 1].contains_variable));*/

            ThreeAddressCode.Op assign_tac = GetAssignTac(i.stack_before[i.stack_before.Count - 1].type.CliType(this));
            int vt_size = 0;
            if (assign_tac == ThreeAddressCode.Op.assign_vt)
                vt_size = GetSizeOf(i.stack_before[i.stack_before.Count - 1].type);

            //i.tacs.Add(new ThreeAddressCode(assign_tac, var.LocalArg(p), i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, vt_size));
            i.tacs.Add(new ThreeAddressCode(assign_tac, state.la_locs[p], i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, vt_size));
        }

        private void enc_ldelema(PseudoStack array_, PseudoStack index, out var output_var, InstructionLine i, AssemblerState state)
        {
            Signature.Param p = array_.type;
            Signature.BaseOrComplexType elem_type;
            if (p.Type is Signature.ZeroBasedArray)
                elem_type = ((Signature.ZeroBasedArray)p.Type).ElemType;
            else
                elem_type = GetElemType(i.opcode.opcode1);

            var var_elem_size = var.Const(GetPackedSizeOf(new Signature.Param(elem_type, this)));

            // convert an int32 index to an native_int one, if necessary
            output_var = state.next_variable++;
            var inner_arr;
            var v_obj = array_.contains_variable;
            var v_index = state.next_variable++;
            var v_byte_offset = state.next_variable++;

            if (index.type.CliType(this) == CliType.int32)
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_index,
                    index.contains_variable, var.Null));
            }
            else if (index.type.CliType(this) == CliType.native_int)
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_index,
                    index.contains_variable, var.Null));
            }
            else
                throw new NotSupportedException();

            if (i.int_array)
                inner_arr = v_obj;
            else
            {
                inner_arr = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, inner_arr, var.ContentsOf(v_obj, GetArrayFieldOffset(ArrayFields.inner_array)), var.Null));
            }


            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i, v_byte_offset, v_index, var_elem_size));
            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, output_var, inner_arr, v_byte_offset));

            //i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, output_var, inner_arr, var.Null));
        }

        private Signature.BaseOrComplexType GetElemType(SingleOpcodes singleOpcodes)
        {
            switch (singleOpcodes)
            {
                case SingleOpcodes.ldelem_i:
                case SingleOpcodes.stelem_i:
                    return new Signature.BaseType(BaseType_Type.I);
                case SingleOpcodes.stelem_i1:
                case SingleOpcodes.ldelem_i1:
                    return new Signature.BaseType(BaseType_Type.I1);
                case SingleOpcodes.ldelem_i2:
                case SingleOpcodes.stelem_i2:
                    return new Signature.BaseType(BaseType_Type.I2);
                case SingleOpcodes.stelem_i4:
                case SingleOpcodes.ldelem_i4:
                    return new Signature.BaseType(BaseType_Type.I4);
                case SingleOpcodes.ldelem_i8:
                case SingleOpcodes.stelem_i8:
                    return new Signature.BaseType(BaseType_Type.I8);
                case SingleOpcodes.stelem_r4:
                case SingleOpcodes.ldelem_r4:
                    return new Signature.BaseType(BaseType_Type.R4);
                case SingleOpcodes.ldelem_r8:
                case SingleOpcodes.stelem_r8:
                    return new Signature.BaseType(BaseType_Type.R8);
                case SingleOpcodes.stelem_ref:
                case SingleOpcodes.ldelem_ref:
                    return new Signature.BaseType(BaseType_Type.Object);
                case SingleOpcodes.ldelem_u1:
                    return new Signature.BaseType(BaseType_Type.U1);
                case SingleOpcodes.ldelem_u2:
                    return new Signature.BaseType(BaseType_Type.U2);
                case SingleOpcodes.ldelem_u4:
                    return new Signature.BaseType(BaseType_Type.U4);
                default:
                    throw new NotSupportedException();
            }
        }

        private void enc_conv(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, BaseType_Type baseType_Type, bool ovf, AssemblerState state)
        { enc_conv(i, meth, m, baseType_Type, ovf, false, state); }
        private void enc_conv(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, BaseType_Type baseType_Type, bool ovf, bool un, AssemblerState state)
        {
            Signature.Param srcp = i.stack_before[i.stack_before.Count - 1].type;
            var srcv = i.stack_before[i.stack_before.Count - 1].contains_variable;

            CliType src_ct = srcp.CliType(this);
            BaseType_Type dest_bt = baseType_Type;

            var dest_v = enc_conv(i, dest_bt, srcp, srcv, false, state);
            
            if (dest_v.type != var.var_type.Void)
            {
                // Perform overflow testing if requested
                if (ovf)
                {
                    // convert back to original type and then compare with src
                    Signature.Param dest_p = new Signature.Param(dest_bt);
                    var second_dest_v;

                    switch (srcp.CliType(this))
                    {
                        case CliType.int32:
                            if (un)
                                second_dest_v = enc_conv(i, BaseType_Type.U4, dest_p, dest_v, true, state);
                            else
                                second_dest_v = enc_conv(i, BaseType_Type.I4, dest_p, dest_v, true, state);
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, srcv, second_dest_v));
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throwne, 0, var.Const(throw_OverflowException), var.Null));
                            break;
                        case CliType.int64:
                            if (un)
                                second_dest_v = enc_conv(i, BaseType_Type.U8, dest_p, dest_v, true, state);
                            else
                                second_dest_v = enc_conv(i, BaseType_Type.I8, dest_p, dest_v, true, state);
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i8, var.Null, srcv, second_dest_v));
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throwne, 0, var.Const(throw_OverflowException), var.Null));
                            break;
                        case CliType.native_int:
                            if (un)
                                second_dest_v = enc_conv(i, BaseType_Type.U, dest_p, dest_v, true, state);
                            else
                                second_dest_v = enc_conv(i, BaseType_Type.I, dest_p, dest_v, true, state);
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i8, var.Null, srcv, second_dest_v));
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throwne, 0, var.Const(throw_OverflowException), var.Null));
                            break;
                        case CliType.F64:
                            second_dest_v = enc_conv(i, BaseType_Type.R8, dest_p, dest_v, true, state);
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_r8, var.Null, srcv, second_dest_v));
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throwne, 0, var.Const(throw_OverflowException), var.Null));
                            break;
                        case CliType.F32:
                            second_dest_v = enc_conv(i, BaseType_Type.R4, dest_p, dest_v, true, state);
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_r4, var.Null, srcv, second_dest_v));
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throwne, 0, var.Const(throw_OverflowException), var.Null));
                            break;
                    }
                }

                i.pushes_variable = dest_v;
                i.pushes = new Signature.Param(baseType_Type);
            }
            else
                i.pushes_variable = i.stack_before[i.stack_before.Count - 1].contains_variable;
        }

        private var enc_conv(InstructionLine i, BaseType_Type dest_bt, Signature.Param srcp, var srcv, bool force_conversion, AssemblerState state)
        {
            CliType src_ct = srcp.CliType(this);
            // Convert from srcp.CliType to baseType_Type
            switch (src_ct)
            {
                case CliType.int32:
                    switch (dest_bt)
                    {
                        case BaseType_Type.I:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I1:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_i1sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I2:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_i2sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I4:
                            if (force_conversion)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, srcv, 0));
                            else
                                return var.Null;
                            break;
                        case BaseType_Type.I8:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_i8sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_uzx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U1:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_u1zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U2:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_u2zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U4:
                            if (force_conversion)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, srcv, 0));
                            else
                                return var.Null;
                            break;
                        case BaseType_Type.U8:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_u8zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.R4:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_r4, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.R8:
                            if (i.opcode.opcode1 == SingleOpcodes.conv_r_un)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_u4_r8, state.next_variable++, srcv, 0));
                            else
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_r8, state.next_variable++, srcv, 0));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;

                case CliType.int64:
                    switch (dest_bt)
                    {
                        case BaseType_Type.I:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_isx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I1:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_i1sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I2:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_i2sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I4:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_i4sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I8:
                            if (force_conversion)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i8, state.next_variable++, srcv, 0));
                            else
                                return var.Null;
                            break;
                        case BaseType_Type.U:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_uzx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U1:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_u1zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U2:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_u2zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U4:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_u4zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U8:
                            if (force_conversion)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i8, state.next_variable++, srcv, 0));
                            else
                                return var.Null;
                            break;
                        case BaseType_Type.R4:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_r4, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.R8:
                            if (i.opcode.opcode1 == SingleOpcodes.conv_r_un)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_u8_r8, state.next_variable++, srcv, 0));
                            else
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_r8, state.next_variable++, srcv, 0));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;

                case CliType.native_int:
                    switch (dest_bt)
                    {
                        case BaseType_Type.I:
                            if (force_conversion)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, srcv, 0));
                            else
                                return var.Null;
                            break;
                        case BaseType_Type.I1:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i1sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I2:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i2sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I4:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i4sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.I8:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i8sx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U:
                            if (force_conversion)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, srcv, 0));
                            else
                                return var.Null;
                            break;
                        case BaseType_Type.U1:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_u1zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U2:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_u2zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U4:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_u4zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.U8:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_u8zx, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.R4:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_r4, state.next_variable++, srcv, 0));
                            break;
                        case BaseType_Type.R8:
                            if (i.opcode.opcode1 == SingleOpcodes.conv_r_un)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_u_r8, state.next_variable++, srcv, 0));
                            else
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_r8, state.next_variable++, srcv, 0));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;

                case CliType.O:
                case CliType.reference:
                    switch (dest_bt)
                    {
                        case BaseType_Type.I8:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i8sx, state.next_variable++, srcv, var.Null));
                            break;
                        case BaseType_Type.U8:
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_u8zx, state.next_variable++, srcv, var.Null));
                            break;
                        case BaseType_Type.I:
                        case BaseType_Type.U:
                            if (force_conversion)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, srcv, 0));
                            else
                                return var.Null;
                            break;
                        case BaseType_Type.I4:
                            if (state.security.AllowConvORefToUI4)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i4sx, state.next_variable++, srcv, var.Null));
                            else
                                throw new NotSupportedException();
                            break;
                        case BaseType_Type.U4:
                            if (state.security.AllowConvORefToUI4)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_u4zx, state.next_variable++, srcv, var.Null));
                            else
                                throw new NotSupportedException();
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;

                case CliType.F32:
                case CliType.F64:
                    BaseType_Type from_type = ((Signature.BaseType)srcp.Type).Type;
                    switch (from_type)
                    {
                        case BaseType_Type.R4:
                            switch (dest_bt)
                            {
                                case BaseType_Type.R8:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r4_r8, state.next_variable++, srcv, var.Null));
                                    break;
                                case BaseType_Type.R4:
                                    if (force_conversion)
                                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_r4, state.next_variable++, srcv, 0));
                                    else
                                        return var.Null;
                                    break;
                                case BaseType_Type.I4:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r4_i4, state.next_variable++, srcv, var.Null));
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                            break;
                        case BaseType_Type.R8:
                            switch (dest_bt)
                            {
                                case BaseType_Type.R4:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_r4, state.next_variable++, srcv, var.Null));
                                    break;
                                case BaseType_Type.R8:
                                    if (force_conversion)
                                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_r8, state.next_variable++, srcv, 0));
                                    else
                                        return var.Null;
                                    break;
                                case BaseType_Type.U1:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_i8, state.next_variable++, srcv, var.Null));
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_u1zx, state.next_variable, state.next_variable - 1, var.Null));
                                    state.next_variable++;
                                    break;
                                case BaseType_Type.U2:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_i8, state.next_variable++, srcv, var.Null));
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_u2zx, state.next_variable, state.next_variable - 1, var.Null));
                                    state.next_variable++;
                                    break;
                                case BaseType_Type.U4:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_i8, state.next_variable++, srcv, var.Null));
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_u4zx, state.next_variable, state.next_variable - 1, var.Null));
                                    state.next_variable++;
                                    break;
                                case BaseType_Type.I8:
                                case BaseType_Type.U8:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_i8, state.next_variable++, srcv, var.Null));
                                    break;
                                case BaseType_Type.I1:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_i8, state.next_variable++, srcv, var.Null));
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_i1sx, state.next_variable, state.next_variable - 1, var.Null));
                                    state.next_variable++;
                                    break;
                                case BaseType_Type.I2:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_i8, state.next_variable++, srcv, var.Null));
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_i2sx, state.next_variable, state.next_variable - 1, var.Null));
                                    state.next_variable++;
                                    break;
                                case BaseType_Type.I4:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_i8, state.next_variable++, srcv, var.Null));
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_i4sx, state.next_variable, state.next_variable - 1, var.Null));
                                    state.next_variable++;
                                    break;
                                case BaseType_Type.I:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_i8, state.next_variable++, srcv, var.Null));
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_isx, state.next_variable, state.next_variable - 1, var.Null));
                                    state.next_variable++;
                                    break;
                                case BaseType_Type.U:
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r8_i8, state.next_variable++, srcv, var.Null));
                                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_uzx, state.next_variable, state.next_variable - 1, var.Null));
                                    state.next_variable++;
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;
                default:
                    throw new NotSupportedException("Cannot use conv on type " + srcp.CliType(this));
            }

            return state.next_variable - 1;
        }

        private void enc_stloc(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, int p, AssemblerState state)
        {
            ThreeAddressCode.Op assign_tac = GetAssignTac(i.stack_before[i.stack_before.Count - 1].type.CliType(this));

            //var dest = var.LocalVar(p);
            var dest = state.lv_locs[p];
            if (assign_tac == ThreeAddressCode.Op.assign_vt)
            {
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(i.stack_before[i.stack_before.Count - 1].type.Type, this);
                Layout l = Layout.GetLayout(new TypeToCompile { _ass = this, tsig = i.stack_before[i.stack_before.Count - 1].type, type = tdr }, this);
                int vt_size = l.ClassSize;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_vt, dest, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, vt_size));
            }
            else
                i.tacs.Add(new ThreeAddressCode(assign_tac, dest, i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
        }

        private ThreeAddressCode.Op GetStObjTac(Token token, Assembler ass)
        { return GetStObjTac(GetLdObjTac(token, ass)); }
        private ThreeAddressCode.Op GetStObjTac(CliType ct)
        { return GetStObjTac(GetLdObjTac(ct)); }
        private ThreeAddressCode.Op GetCallTac(Token token, Assembler ass)
        { return GetCallTac(GetLdObjTac(token, ass)); }
        public static ThreeAddressCode.Op GetCallTac(CliType ct)
        { if (ct == CliType.void_) return ThreeAddressCode.Op.call_void; else return GetCallTac(GetLdObjTac(ct)); }

        public static ThreeAddressCode.Op GetStObjTac(ThreeAddressCode.Op ldobjtac)
        {
            switch (ldobjtac)
            {
                case ThreeAddressCode.Op.ldobj_i:
                    return ThreeAddressCode.Op.stobj_i;
                case ThreeAddressCode.Op.ldobj_i4:
                    return ThreeAddressCode.Op.stobj_i4;
                case ThreeAddressCode.Op.ldobj_i8:
                    return ThreeAddressCode.Op.stobj_i8;
                case ThreeAddressCode.Op.ldobj_r4:
                    return ThreeAddressCode.Op.stobj_r4;
                case ThreeAddressCode.Op.ldobj_r8:
                    return ThreeAddressCode.Op.stobj_r8;
                case ThreeAddressCode.Op.ldobj_vt:
                    return ThreeAddressCode.Op.stobj_vt;
                default:
                    throw new NotSupportedException();
            }
        }

        public static ThreeAddressCode.Op GetCallTac(ThreeAddressCode.Op ldobjtac)
        {
            switch (ldobjtac)
            {
                case ThreeAddressCode.Op.ldobj_i:
                    return ThreeAddressCode.Op.call_i;
                case ThreeAddressCode.Op.ldobj_i4:
                    return ThreeAddressCode.Op.call_i4;
                case ThreeAddressCode.Op.ldobj_i8:
                    return ThreeAddressCode.Op.call_i8;
                case ThreeAddressCode.Op.ldobj_r4:
                    return ThreeAddressCode.Op.call_r4;
                case ThreeAddressCode.Op.ldobj_r8:
                    return ThreeAddressCode.Op.call_r8;
                default:
                    throw new NotSupportedException();
            }
        }

        private void enc_brif(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, SingleOpcodes singleOpcodes,
            cfg_node node, AssemblerState state)
        { enc_brif(i, meth, m, singleOpcodes, DoubleOpcodes.arglist, node, state); }

        private void enc_brif(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, SingleOpcodes singleOpcodes,
            DoubleOpcodes doubleOpcodes, cfg_node cfg_node, AssemblerState state)
        {
            Signature.Param a = null;
            var src_a = var.Null;
            if(i.stack_before.Count > 1) {
                a = i.stack_before[i.stack_before.Count - 2].type;
                src_a = i.stack_before[i.stack_before.Count - 2].contains_variable;
            }
            Signature.Param b = i.stack_before[i.stack_before.Count - 1].type;
            var src_b = i.stack_before[i.stack_before.Count - 1].contains_variable;

            SingleOpcodes long_code = to_long(singleOpcodes);
            DoubleOpcodes long_code2 = doubleOpcodes;

            if (is_br(singleOpcodes, SingleOpcodes.brtrue) || is_br(singleOpcodes, SingleOpcodes.brfalse))
            {
                a = new Signature.Param(b.CliType(this));
                src_a = src_b;

                switch (a.CliType(this))
                {
                    case CliType.F32:
                    case CliType.F64:
                        if (Signature.BCTCompare(b.Type, new Signature.BaseType(BaseType_Type.R4), this))
                        {
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_r4, state.next_variable++, var.Const(0.0), var.Null));
                            a.Type = new Signature.BaseType(BaseType_Type.R4);
                        }
                        else
                            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_r8, state.next_variable++, var.Const(0d), 0));
                        break;
                    case CliType.int32:
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, 
                            var.Const(0), 0));
                        break;
                    case CliType.int64:
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i8, state.next_variable++,
                            var.Const(0L), 0));
                        break;
                    case CliType.native_int:
                    case CliType.O:
                    case CliType.reference:
                        i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++,
                            var.Const(this.ConvertToI(0)), 0));
                        break;
                    default:
                        throw new NotSupportedException();
                }
                src_b = state.next_variable - 1;
            }

            bool a_is_r4 = Signature.BCTCompare(a.Type, new Signature.BaseType(BaseType_Type.R4), this);
            bool a_is_r8 = Signature.BCTCompare(a.Type, new Signature.BaseType(BaseType_Type.R8), this);
            bool b_is_r4 = Signature.BCTCompare(b.Type, new Signature.BaseType(BaseType_Type.R4), this);
            bool b_is_r8 = Signature.BCTCompare(b.Type, new Signature.BaseType(BaseType_Type.R8), this);

            if (a.CliType(this) != b.CliType(this))
            {
                // convert int32 to native ints if necessary
                if ((a.CliType(this) == CliType.int32) && (b.CliType(this) == CliType.native_int))
                {
                    // convert a to native int
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, state.next_variable++, src_a, 0));
                    src_a = state.next_variable - 1;
                    a = new Signature.Param(CliType.native_int);
                }
                else if ((a.CliType(this) == CliType.native_int) && (b.CliType(this) == CliType.int32))
                {
                    // convert b to native int
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, state.next_variable++, src_b, 0));
                    src_b = state.next_variable - 1;
                    b = new Signature.Param(CliType.native_int);
                }
                else
                {
                    CliType a_cli = a.CliType(this);
                    CliType b_cli = b.CliType(this);

                    // According to CIL III Table 4, we allow comparisons between & and native int for beq[.s], bne.un[.s] and ceq
                    bool allowed = false;
                    if ((((a_cli == CliType.native_int) && (b_cli == CliType.reference)) || ((a_cli == CliType.reference) && (b_cli == CliType.native_int))) &&
                        ((i.opcode.opcode1 == SingleOpcodes.beq) || (i.opcode.opcode1 == SingleOpcodes.beq_s) ||
                        (i.opcode.opcode1 == SingleOpcodes.bne_un) || (i.opcode.opcode1 == SingleOpcodes.bne_un_s) ||
                        (i.opcode.opcode2 == DoubleOpcodes.ceq)))
                        allowed = true;

                    if(!allowed)
                        throw new NotSupportedException();
                }
            }

            if (a_is_r4 && b_is_r8)
            {
                /* Convert a to r8 */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r4_r8, state.next_variable++, src_a, var.Null));
                src_a = state.next_variable - 1;
                a = new Signature.Param(BaseType_Type.R8);
                a_is_r4 = false;
                a_is_r8 = true;
            }
            else if (a_is_r8 && b_is_r4)
            {
                /* Convert b to r8 */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_r4_r8, state.next_variable++, src_b, var.Null));
                src_b = state.next_variable - 1;
                b = new Signature.Param(BaseType_Type.R8);
                b_is_r4 = false;
                b_is_r8 = true;
            }

            bool is_r4 = a_is_r4 || b_is_r4;
            bool is_r8 = a_is_r8 || b_is_r8;

            bool is_un = false;
            if ((long_code == SingleOpcodes.bge_un) || (long_code == SingleOpcodes.ble_un) || (long_code == SingleOpcodes.blt_un) ||
                (long_code == SingleOpcodes.bne_un) || (long_code2 == DoubleOpcodes.cgt_un) || (long_code2 == DoubleOpcodes.clt_un))
                is_un = true;

            // Do the comparison
            switch (a.CliType(this))
            {
                case CliType.F32:
                case CliType.F64:
                    {
                        if (is_un)
                        {
                            if (is_r4)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_r4_un, var.Null, src_a, src_b));
                            else
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_r8_un, var.Null, src_a, src_b));
                        }
                        else
                        {
                            if (is_r4)
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_r4, var.Null, src_a, src_b));
                            else
                                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_r8, var.Null, src_a, src_b));
                        }
                    }
                    break;
                case CliType.int32:
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, 0, src_a, src_b));
                    break;
                case CliType.int64:
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i8, 0, src_a, src_b));
                    break;
                case CliType.native_int:
                case CliType.O:
                case CliType.reference:
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i, 0, src_a, src_b));
                    break;
            }

            if (long_code == SingleOpcodes.double_)
            {
                // assume its a cmp instruction
                i.tacs.Add(new ThreeAddressCode(GetSetInst(long_code2), state.next_variable++, 0, 0, 0, is_r4 || is_r8));
                i.pushes_variable = state.next_variable - 1;
                i.pushes = new Signature.Param(CliType.int32);
            }
            else
            {
                // assume its a br instruction
                i.tacs.Add(new BrEx(GetBrInst(long_code), cfg_node.isuc[1].block_id, is_r4 || is_r8));
            }
        }

        private ThreeAddressCode.Op GetBrInst(SingleOpcodes long_code)
        {
            switch (long_code)
            {
                case SingleOpcodes.beq:
                    return ThreeAddressCode.Op.beq;
                case SingleOpcodes.bge:
                    return ThreeAddressCode.Op.bge;
                case SingleOpcodes.bge_un:
                    return ThreeAddressCode.Op.bae;
                case SingleOpcodes.bgt:
                    return ThreeAddressCode.Op.bg;
                case SingleOpcodes.bgt_un:
                    return ThreeAddressCode.Op.ba;
                case SingleOpcodes.ble:
                    return ThreeAddressCode.Op.ble;
                case SingleOpcodes.ble_un:
                    return ThreeAddressCode.Op.bbe;
                case SingleOpcodes.blt:
                    return ThreeAddressCode.Op.bl;
                case SingleOpcodes.blt_un:
                    return ThreeAddressCode.Op.bb;
                case SingleOpcodes.bne_un:
                    return ThreeAddressCode.Op.bne;
                case SingleOpcodes.brfalse:
                    return ThreeAddressCode.Op.beq;
                case SingleOpcodes.brtrue:
                    return ThreeAddressCode.Op.bne;
                default:
                    throw new NotSupportedException();
            }
        }

        private ThreeAddressCode.Op GetSetInst(DoubleOpcodes long_code2)
        {
            switch (long_code2)
            {
                case DoubleOpcodes.ceq:
                    return ThreeAddressCode.Op.seteq;
                case DoubleOpcodes.cgt:
                    return ThreeAddressCode.Op.setg;
                case DoubleOpcodes.cgt_un:
                    return ThreeAddressCode.Op.seta;
                case DoubleOpcodes.clt:
                    return ThreeAddressCode.Op.setl;
                case DoubleOpcodes.clt_un:
                    return ThreeAddressCode.Op.setb;
                default:
                    throw new NotSupportedException();
            }
        }

        private bool is_br(SingleOpcodes singleOpcodes, SingleOpcodes singleOpcodes_2)
        {
            if (to_long(singleOpcodes) == to_long(singleOpcodes_2))
                return true;
            return false;
        }

        private SingleOpcodes to_long(SingleOpcodes singleOpcodes)
        {
            switch (singleOpcodes)
            {
                case SingleOpcodes.br:
                case SingleOpcodes.br_s:
                    return SingleOpcodes.br;
                case SingleOpcodes.beq:
                case SingleOpcodes.beq_s:
                    return SingleOpcodes.beq;
                case SingleOpcodes.bge:
                case SingleOpcodes.bge_s:
                    return SingleOpcodes.bge;
                case SingleOpcodes.bge_un:
                case SingleOpcodes.bge_un_s:
                    return SingleOpcodes.bge_un;
                case SingleOpcodes.bgt:
                case SingleOpcodes.bgt_s:
                    return SingleOpcodes.bgt;
                case SingleOpcodes.bgt_un:
                case SingleOpcodes.bgt_un_s:
                    return SingleOpcodes.bgt_un;
                case SingleOpcodes.ble:
                case SingleOpcodes.ble_s:
                    return SingleOpcodes.ble;
                case SingleOpcodes.ble_un:
                case SingleOpcodes.ble_un_s:
                    return SingleOpcodes.ble_un;
                case SingleOpcodes.blt:
                case SingleOpcodes.blt_s:
                    return SingleOpcodes.blt;
                case SingleOpcodes.blt_un:
                case SingleOpcodes.blt_un_s:
                    return SingleOpcodes.blt_un;
                case SingleOpcodes.bne_un:
                case SingleOpcodes.bne_un_s:
                    return SingleOpcodes.bne_un;
                case SingleOpcodes.brfalse:
                case SingleOpcodes.brfalse_s:
                    return SingleOpcodes.brfalse;
                case SingleOpcodes.brtrue:
                case SingleOpcodes.brtrue_s:
                    return SingleOpcodes.brtrue;
                case SingleOpcodes.double_:
                    return SingleOpcodes.double_;
                default:
                    throw new NotSupportedException();
            }
        }

        private void enc_ldcr8(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, double p, AssemblerState state)
        {
            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_r8, state.next_variable++, var.Const(p), 0));
            i.pushes_variable = state.next_variable - 1;
            i.pushes = new Signature.Param(BaseType_Type.R8);
        }

        private void enc_ldcr4(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, float p, AssemblerState state)
        {
            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_r4, state.next_variable++, var.Const(p), 0));
            i.pushes_variable = state.next_variable - 1;
            i.pushes = new Signature.Param(BaseType_Type.R4);
        }

        private void enc_ldci4(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, int p, AssemblerState state)
        {
            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, 0, 0));
            i.pushes_variable = state.next_variable - 1;
            i.pushes = new Signature.Param(CliType.int32);
            i.tacs[i.tacs.Count - 1].Operand1.constant_val = p;
        }

        private void enc_ldci8(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, long p, AssemblerState state)
        {
            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i8, state.next_variable++, 0, 0));
            i.pushes_variable = state.next_variable - 1;
            i.pushes = new Signature.Param(CliType.int64);
            i.tacs[i.tacs.Count - 1].Operand1.constant_val = p;
        }

        private void enc_1intop(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, SingleOpcodes singleOpcodes, AssemblerState state)
        {
            Signature.Param a = i.stack_before[i.stack_before.Count - 1].type;
            CliType p = CliType.void_;
            switch (a.CliType(this))
            {
                case CliType.int32:
                case CliType.int64:
                case CliType.native_int:
                    p = a.CliType(this);
                    break;
            }
            if (p == CliType.void_)
                throw new NotSupportedException();
            i.pushes = new Signature.Param(p);

            ThreeAddressCode.Op op = ThreeAddressCode.Op.invalid;

            switch (singleOpcodes)
            {
                case SingleOpcodes.not:
                    switch (a.CliType(this))
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.not_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.not_i8;
                            break;
                        case CliType.native_int:
                            op = ThreeAddressCode.Op.not_i;
                            break;
                    }
                    break;
            }

            if (op == ThreeAddressCode.Op.invalid)
                throw new NotSupportedException();

            i.tacs.Add(new ThreeAddressCode(op, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable,
                0));
            i.pushes_variable = state.next_variable - 1;
        }

        public static ThreeAddressCode.Op GetPhiTac(CliType ct)
        { return GetPhiTac(GetLdObjTac(ct)); }
        internal ThreeAddressCode.Op GetAssignTac(Token token, Assembler ass)
        { return GetAssignTac(GetLdObjTac(token, ass)); }
        public static ThreeAddressCode.Op GetAssignTac(CliType ct)
        { return GetAssignTac(GetLdObjTac(ct)); }
        internal ThreeAddressCode.Op GetAssignTac(var_semantic vs)
        {
            if (vs.needs_float64)
                return ThreeAddressCode.Op.assign_r8;
            else if (vs.needs_float32)
                return ThreeAddressCode.Op.assign_r4;
            else if (vs.needs_vtype)
                return ThreeAddressCode.Op.assign_vt;
            else if (vs.needs_intptr)
                return ThreeAddressCode.Op.assign_i;
            else if (vs.needs_int64)
                return ThreeAddressCode.Op.assign_i8;
            else if (vs.needs_int32)
                return ThreeAddressCode.Op.assign_i4;
            else
                throw new NotSupportedException();
        }
        private static ThreeAddressCode.Op GetAssignTac(ThreeAddressCode.Op ldconsttac)
        {
            switch (ldconsttac)
            {
                case ThreeAddressCode.Op.ldconst_i:
                case ThreeAddressCode.Op.ldobj_i:
                case ThreeAddressCode.Op.phi_i:
                    return ThreeAddressCode.Op.assign_i;
                case ThreeAddressCode.Op.ldconst_i4:
                case ThreeAddressCode.Op.ldobj_i4:
                case ThreeAddressCode.Op.phi_i4:
                    return ThreeAddressCode.Op.assign_i4;
                case ThreeAddressCode.Op.ldconst_i8:
                case ThreeAddressCode.Op.ldobj_i8:
                case ThreeAddressCode.Op.phi_i8:
                    return ThreeAddressCode.Op.assign_i8;
                case ThreeAddressCode.Op.ldconst_r4:
                case ThreeAddressCode.Op.ldobj_r4:
                case ThreeAddressCode.Op.phi_r4:
                    return ThreeAddressCode.Op.assign_r4;
                case ThreeAddressCode.Op.ldconst_r8:
                case ThreeAddressCode.Op.ldobj_r8:
                case ThreeAddressCode.Op.phi_r8:
                    return ThreeAddressCode.Op.assign_r8;
                case ThreeAddressCode.Op.phi_vt:
                case ThreeAddressCode.Op.ldobj_vt:
                    return ThreeAddressCode.Op.assign_vt;
                case ThreeAddressCode.Op.ldobj_virtftnptr:
                    return ThreeAddressCode.Op.assign_virtftnptr;
                default:
                    throw new NotSupportedException();
            }
        }

        public static ThreeAddressCode.Op GetPhiTac(ThreeAddressCode.Op ldconsttac)
        {
            switch (ldconsttac)
            {
                case ThreeAddressCode.Op.ldconst_i:
                case ThreeAddressCode.Op.ldobj_i:
                case ThreeAddressCode.Op.phi_i:
                    return ThreeAddressCode.Op.phi_i;
                case ThreeAddressCode.Op.ldconst_i4:
                case ThreeAddressCode.Op.ldobj_i4:
                case ThreeAddressCode.Op.phi_i4:
                    return ThreeAddressCode.Op.phi_i4;
                case ThreeAddressCode.Op.ldconst_i8:
                case ThreeAddressCode.Op.ldobj_i8:
                case ThreeAddressCode.Op.phi_i8:
                    return ThreeAddressCode.Op.phi_i8;
                case ThreeAddressCode.Op.ldconst_r4:
                case ThreeAddressCode.Op.ldobj_r4:
                case ThreeAddressCode.Op.phi_r4:
                    return ThreeAddressCode.Op.phi_r4;
                case ThreeAddressCode.Op.ldconst_r8:
                case ThreeAddressCode.Op.ldobj_r8:
                case ThreeAddressCode.Op.phi_r8:
                    return ThreeAddressCode.Op.phi_r8;
                default:
                    throw new NotSupportedException();
            }
        }

        public static ThreeAddressCode.Op GetLdObjTac(CliType clitype)
        {
            switch (clitype)
            {
                case CliType.F64:
                    return ThreeAddressCode.Op.ldobj_r8;
                case CliType.F32:
                    return ThreeAddressCode.Op.ldobj_r4;
                case CliType.int32:
                    return ThreeAddressCode.Op.ldobj_i4;
                case CliType.int64:
                    return ThreeAddressCode.Op.ldobj_i8;
                case CliType.native_int:
                    return ThreeAddressCode.Op.ldobj_i;
                case CliType.O:
                    return ThreeAddressCode.Op.ldobj_i;
                case CliType.reference:
                    return ThreeAddressCode.Op.ldobj_i;
                case CliType.vt:
                    return ThreeAddressCode.Op.ldobj_vt;
                case CliType.void_:
                    return ThreeAddressCode.Op.nop;
                case CliType.virtftnptr:
                    return ThreeAddressCode.Op.ldobj_virtftnptr;
                default:
                    throw new NotSupportedException();
            }
        }

        private ThreeAddressCode.Op GetLdObjTac(Token token, Assembler ass)
        {
            CliType ct = CliType.void_;

            if ((token.Value is Metadata.FieldRow) || (token.Value is Metadata.MemberRefRow))
            {
                Metadata.FieldRow frow = token.Value as Metadata.FieldRow;
                if (frow == null)
                {
                    // its a memberrefrow
                    Metadata.MemberRefRow mref = token.Value as Metadata.MemberRefRow;

                    if (mref.Signature[0] == 0x06)
                    {
                        // field reference
                        Signature.Field fsig = Signature.ParseFieldSig(token.Metadata, mref.Signature, ass);
                        ct = fsig.AsParam(ass).CliType(this);
                    }
                }
                else
                {
                    Signature.Field sig = Signature.ParseFieldSig(token.Metadata, frow.Signature, ass);
                    ct = sig.AsParam(ass).CliType(this);
                }
            }
            else if ((token.Value is Metadata.TypeDefRow) || (token.Value is Metadata.TypeRefRow))
            {
                Signature.Param p = new Signature.Param(token, this);
                ct = p.CliType(this);
            }

            return GetLdObjTac(ct);
        }

        private void enc_shiftop(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, SingleOpcodes singleOpcodes, AssemblerState state)
        {
            Signature.Param a, b;
            a = i.stack_before[i.stack_before.Count - 2].type;
            b = i.stack_before[i.stack_before.Count - 1].type;

            CliType p = CliType.void_;

            switch (a.CliType(this))
            {
                case CliType.int32:
                    switch (b.CliType(this))
                    {
                        case CliType.int32:
                        case CliType.native_int:
                            p = CliType.int32;
                            break;
                    }
                    break;
                case CliType.int64:
                    switch (b.CliType(this))
                    {
                        case CliType.int32:
                        case CliType.native_int:
                            p = CliType.int64;
                            break;
                    }
                    break;
                case CliType.native_int:
                    switch (b.CliType(this))
                    {
                        case CliType.int32:
                        case CliType.native_int:
                            p = CliType.native_int;
                            break;
                    }
                    break;
            }
            if (p == CliType.void_)
                throw new NotSupportedException();
            i.pushes = new Signature.Param (this) { Type = new Signature.BaseType(p) };

            ThreeAddressCode.Op op = ThreeAddressCode.Op.invalid;

            switch (singleOpcodes)
            {
                case SingleOpcodes.shl:
                    switch (p)
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.shl_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.shl_i8;
                            break;
                        case CliType.native_int:
                            op = ThreeAddressCode.Op.shl_i;
                            break;
                    }
                    break;
                case SingleOpcodes.shr:
                    switch (p)
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.shr_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.shr_i8;
                            break;
                        case CliType.native_int:
                            op = ThreeAddressCode.Op.shr_i;
                            break;
                    }
                    break;
                case SingleOpcodes.shr_un:
                    switch (p)
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.shr_un_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.shr_un_i8;
                            break;
                        case CliType.native_int:
                            op = ThreeAddressCode.Op.shr_un_i;
                            break;
                    }
                    break;
            }

            if (op == ThreeAddressCode.Op.invalid)
                throw new NotSupportedException();

            i.tacs.Add(new ThreeAddressCode(op, state.next_variable++, i.stack_before[i.stack_before.Count - 2].contains_variable,
                i.stack_before[i.stack_before.Count - 1].contains_variable));
            i.pushes_variable = state.next_variable - 1;
        }

        private void enc_2intop(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, SingleOpcodes singleOpcodes, AssemblerState state)
        {
            Signature.Param a, b;
            a = i.stack_before[i.stack_before.Count - 2].type;
            b = i.stack_before[i.stack_before.Count - 1].type;

            CliType p = CliType.void_;
            bool conv_a_i4_i = false;
            bool conv_b_i4_i = false;

            switch (a.CliType(this))
            {
                case CliType.int32:
                    switch (b.CliType(this))
                    {
                        case CliType.int32:
                            p = CliType.int32;
                            break;
                        case CliType.native_int:
                            p = CliType.native_int;
                            conv_a_i4_i = true;
                            break;
                    }
                    break;
                case CliType.int64:
                    switch (b.CliType(this))
                    {
                        case CliType.int64:
                            p = CliType.int64;
                            break;
                    }
                    break;
                case CliType.native_int:
                    switch (b.CliType(this))
                    {
                        case CliType.int32:
                            p = CliType.native_int;
                            conv_b_i4_i = true;
                            break;
                        case CliType.native_int:
                            p = CliType.native_int;
                            break;
                    }
                    break;
                case CliType.O:
                    switch (b.CliType(this))
                    {
                        case CliType.O:
                            if (i.allow_obj_numop)
                                p = CliType.native_int;
                            break;
                    }
                    break;
            }

            if (p == CliType.void_)
                throw new NotSupportedException();
            i.pushes = new Signature.Param(this) { Type = new Signature.BaseType(p) };

            // Convert int32s to native ints if necessary
            var src_a = i.stack_before[i.stack_before.Count - 2].contains_variable;
            var src_b = i.stack_before[i.stack_before.Count - 1].contains_variable;

            if (conv_a_i4_i)
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, state.next_variable, src_a, 0));
                src_a = state.next_variable++;
            }
            if (conv_b_i4_i)
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, state.next_variable, src_b, 0));
                src_b = state.next_variable++;
            }

            // Perform relevant operation
            ThreeAddressCode.Op op = ThreeAddressCode.Op.invalid;
            switch (singleOpcodes)
            {
                case SingleOpcodes.and:
                    switch (p)
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.and_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.and_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.and_i;
                            break;
                    }
                    break;
                case SingleOpcodes.div_un:
                    switch (p)
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.div_u4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.div_u8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.div_u;
                            break;
                    }
                    break;
                case SingleOpcodes.or:
                    switch (p)
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.or_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.or_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.or_i;
                            break;
                    }
                    break;
                case SingleOpcodes.rem_un:
                    switch (p)
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.rem_un_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.rem_un_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.rem_un_i;
                            break;
                    }
                    break;
                case SingleOpcodes.xor:
                    switch (p)
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.xor_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.xor_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.xor_i;
                            break;
                    }
                    break;
            }

            if (op == ThreeAddressCode.Op.invalid)
                throw new NotSupportedException();

            i.tacs.Add(new ThreeAddressCode(op, state.next_variable++, src_a, src_b));
            i.pushes_variable = state.next_variable - 1;
        }

        private void enc_unnumop(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, SingleOpcodes singleOpcodes, AssemblerState state)
        {
            Signature.Param a = i.stack_before[i.stack_before.Count - 1].type;
            CliType p = CliType.void_;
            switch (a.CliType(this))
            {
                case CliType.int32:
                case CliType.int64:
                case CliType.native_int:
                case CliType.F32:
                case CliType.F64:
                    p = a.CliType(this);
                    break;
            }
            if (p == CliType.void_)
                throw new NotSupportedException();
            i.pushes = new Signature.Param(p);

            ThreeAddressCode.Op op = ThreeAddressCode.Op.invalid;

            switch (singleOpcodes)
            {
                case SingleOpcodes.neg:
                    switch (a.CliType(this))
                    {
                        case CliType.int32:
                            op = ThreeAddressCode.Op.neg_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.neg_i8;
                            break;
                        case CliType.F32:
                        case CliType.F64:
                            if (Signature.BCTCompare(a.Type, new Signature.BaseType(BaseType_Type.R4), this))
                            {
                                op = ThreeAddressCode.Op.neg_r4;
                                i.pushes = new Signature.Param(BaseType_Type.R4);
                            }
                            else
                                op = ThreeAddressCode.Op.neg_r8;
                            break;
                        case CliType.native_int:
                            op = ThreeAddressCode.Op.neg_i;
                            break;
                    }
                    break;
            }

            if (op == ThreeAddressCode.Op.invalid)
                throw new NotSupportedException();

            i.tacs.Add(new ThreeAddressCode(op, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable,
                0));
            i.pushes_variable = state.next_variable - 1;
        }

        private void enc_binnumop(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, SingleOpcodes singleOpcodes, AssemblerState state)
        {
            Signature.Param a, b;
            a = i.stack_before[i.stack_before.Count - 2].type;
            b = i.stack_before[i.stack_before.Count - 1].type;

            CliType p = CliType.void_;
            bool conv_a_i4_i = false;
            bool conv_b_i4_i = false;

            switch (a.CliType(this))
            {
                case CliType.int32:
                    switch (b.CliType(this))
                    {
                        case CliType.int32:
                            p = CliType.int32;
                            break;
                        case CliType.native_int:
                            p = CliType.native_int;
                            conv_a_i4_i = true;
                            break;
                        case CliType.reference:
                            if (singleOpcodes == SingleOpcodes.add)
                            {
                                p = CliType.reference;
                                conv_a_i4_i = true;
                            }
                            break;
                    }
                    break;
                case CliType.int64:
                    switch (b.CliType(this))
                    {
                        case CliType.int64:
                            p = CliType.int64;
                            break;
                    }
                    break;
                case CliType.native_int:
                    switch (b.CliType(this))
                    {
                        case CliType.int32:
                            p = CliType.native_int;
                            conv_b_i4_i = true;
                            break;
                        case CliType.native_int:
                            p = CliType.native_int;
                            break;
                        case CliType.reference:
                            if (singleOpcodes == SingleOpcodes.add)
                                p = CliType.reference;
                            break;
                    }
                    break;
                case CliType.F32:
                case CliType.F64:
                    switch (b.CliType(this))
                    {
                        case CliType.F32:
                            p = CliType.F32;
                            break;
                        case CliType.F64:
                            p = CliType.F64;
                            break;
                    }
                    break;
                case CliType.reference:
                    switch (b.CliType(this))
                    {
                        case CliType.int32:
                            if ((singleOpcodes == SingleOpcodes.add) || (singleOpcodes == SingleOpcodes.sub))
                            {
                                p = CliType.reference;
                                conv_b_i4_i = true;
                            }
                            break;
                        case CliType.native_int:
                            if ((singleOpcodes == SingleOpcodes.add) || (singleOpcodes == SingleOpcodes.sub))
                                p = CliType.reference;
                            break;
                        case CliType.reference:
                            if (singleOpcodes == SingleOpcodes.sub)
                                p = CliType.native_int;
                            break;
                    }
                    break;
            }

            if (p == CliType.void_)
                throw new NotSupportedException();

            i.pushes = new Signature.Param(this) { Type = new Signature.BaseType(p) };

            // Convert int32s to native ints if necessary
            var src_a = i.stack_before[i.stack_before.Count - 2].contains_variable;
            var src_b = i.stack_before[i.stack_before.Count - 1].contains_variable;

            if (conv_a_i4_i)
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, state.next_variable, src_a, 0));
                src_a = state.next_variable++;
            }
            if (conv_b_i4_i)
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, state.next_variable, src_b, 0));
                src_b = state.next_variable++;
            }

            // Perform relevant operation
            ThreeAddressCode.Op op = ThreeAddressCode.Op.add_i;
            switch (singleOpcodes)
            {
                case SingleOpcodes.add:
                case SingleOpcodes.add_ovf:
                case SingleOpcodes.add_ovf_un:
                    switch (p)
                    {
                        case CliType.F32:
                        case CliType.F64:
                            if (Signature.BCTCompare(a.Type, new Signature.BaseType(BaseType_Type.R4), this))
                            {
                                op = ThreeAddressCode.Op.add_r4;
                                i.pushes = new Signature.Param(BaseType_Type.R4);
                            }
                            else
                                op = ThreeAddressCode.Op.add_r8;
                            break;
                        case CliType.int32:
                            op = ThreeAddressCode.Op.add_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.add_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.add_i;
                            break;
                    }
                    break;
                case SingleOpcodes.div:
                    switch (p)
                    {
                        case CliType.F32:
                        case CliType.F64:
                            if (Signature.BCTCompare(a.Type, new Signature.BaseType(BaseType_Type.R4), this))
                            {
                                op = ThreeAddressCode.Op.div_r4;
                                i.pushes = new Signature.Param(BaseType_Type.R4);
                            }
                            else
                                op = ThreeAddressCode.Op.div_r8;
                            break;
                        case CliType.int32:
                            op = ThreeAddressCode.Op.div_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.div_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.div_i;
                            break;
                    }
                    break;
                case SingleOpcodes.mul:
                case SingleOpcodes.mul_ovf:
                    switch (p)
                    {
                        case CliType.F32:
                        case CliType.F64:
                            if (Signature.BCTCompare(a.Type, new Signature.BaseType(BaseType_Type.R4), this))
                            {
                                op = ThreeAddressCode.Op.mul_r4;
                                i.pushes = new Signature.Param(BaseType_Type.R4);
                            }
                            else
                                op = ThreeAddressCode.Op.mul_r8;
                            break;
                        case CliType.int32:
                            op = ThreeAddressCode.Op.mul_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.mul_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.mul_i;
                            break;
                    }
                    break;
                case SingleOpcodes.mul_ovf_un:
                    switch (p)
                    {
                        case CliType.F32:
                        case CliType.F64:
                            throw new NotSupportedException();
                        case CliType.int32:
                            op = ThreeAddressCode.Op.mul_un_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.mul_un_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.mul_un_i;
                            break;
                    }
                    break;
                case SingleOpcodes.rem:
                    switch (p)
                    {
                        case CliType.F32:
                        case CliType.F64:
                            if (Signature.BCTCompare(a.Type, new Signature.BaseType(BaseType_Type.R4), this))
                            {
                                op = ThreeAddressCode.Op.sub_r4;
                                i.pushes = new Signature.Param(BaseType_Type.R4);
                            }
                            else
                                op = ThreeAddressCode.Op.rem_r8;
                            break;
                        case CliType.int32:
                            op = ThreeAddressCode.Op.rem_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.rem_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.rem_i;
                            break;
                    }
                    break;
                case SingleOpcodes.sub:
                case SingleOpcodes.sub_ovf:
                case SingleOpcodes.sub_ovf_un:
                    switch (p)
                    {
                        case CliType.F32:
                        case CliType.F64:
                            if (Signature.BCTCompare(a.Type, new Signature.BaseType(BaseType_Type.R4), this))
                            {
                                op = ThreeAddressCode.Op.sub_r4;
                                i.pushes = new Signature.Param(BaseType_Type.R4);
                            }
                            else
                                op = ThreeAddressCode.Op.sub_r8;
                            break;
                        case CliType.int32:
                            op = ThreeAddressCode.Op.sub_i4;
                            break;
                        case CliType.int64:
                            op = ThreeAddressCode.Op.sub_i8;
                            break;
                        case CliType.native_int:
                        case CliType.reference:
                            op = ThreeAddressCode.Op.sub_i;
                            break;
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }

            // now perform a op b
            ThreeAddressCode numop = new ThreeAddressCode(op, state.next_variable++, src_a, src_b);
            i.tacs.Add(numop);
            i.pushes_variable = state.next_variable - 1;

            // perform overflow checking if required
            switch (singleOpcodes)
            {
                case SingleOpcodes.add_ovf:
                case SingleOpcodes.sub_ovf:
                case SingleOpcodes.mul_ovf:
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throw_ovf, var.Null, var.Const(throw_OverflowException), var.Null));
                    numop.remove_if_optimized = new ThreeAddressCode[] { i.tacs[i.tacs.Count - 1] };
                    break;
                case SingleOpcodes.add_ovf_un:
                case SingleOpcodes.sub_ovf_un:
                case SingleOpcodes.mul_ovf_un:
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throw_ovf_un, var.Null, var.Const(throw_OverflowException), var.Null));
                    numop.remove_if_optimized = new ThreeAddressCode[] { i.tacs[i.tacs.Count - 1] };
                    break;
            }
        }

        private void enc_ldloc(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, int p, AssemblerState state)
        {
            i.pushes = i.lv_before[p].type;
            //i.pushes_variable = var.LocalVar(p);
            i.pushes_variable = state.lv_locs[p];
        }

        private void enc_ldfld(InstructionLine i, Assembler.TypeToCompile containing_type, Signature.BaseMethod containing_meth, bool static_, AssemblerState state)
        { enc_ldfld(i, containing_type, containing_meth, static_, false, state); }
        private void enc_ldflda(InstructionLine i, Assembler.TypeToCompile containing_type, Signature.BaseMethod containing_meth, bool static_, AssemblerState state)
        { enc_ldfld(i, containing_type, containing_meth, static_, true, state); }

        private void enc_ldfld(InstructionLine i, Assembler.TypeToCompile containing_type, Signature.BaseMethod containing_meth, bool static_, bool get_address, AssemblerState state)
        {
            FieldToCompile ftc;
            if (i.inline_tok is FTCToken)
                ftc = ((FTCToken)i.inline_tok).ftc;
            else
                ftc = Metadata.GetFTC(new Metadata.TableIndex(i.inline_tok), containing_type, containing_meth, this);

            if (static_ || (i.inline_tok is FTCToken))
            {
                ftc.memberof_type = ftc.definedin_type;
                ftc.memberof_tsig = ftc.definedin_tsig;
                if (static_)
                {
                    if (_options.EnableRTTI)
                        Requestor.RequestTypeInfo(new TypeToCompile { _ass = this, tsig = ftc.definedin_tsig, type = ftc.definedin_type });
                    else
                        Requestor.RequestStaticFields(new TypeToCompile { _ass = this, tsig = ftc.definedin_tsig, type = ftc.definedin_type });

                    if (!i.cfg_node.types_whose_static_fields_are_referenced.Contains(ftc.DefinedIn))
                        i.cfg_node.types_whose_static_fields_are_referenced.Add(ftc.DefinedIn);
                }
            }
            else
            {
                ftc.memberof_tsig = i.stack_before[i.stack_before.Count - 1].type;
                ftc.memberof_type = Metadata.GetTypeDef(ftc.memberof_tsig.Type, this);
            }

            Layout l = Layout.GetLayout(new TypeToCompile { _ass = this, tsig = ftc.memberof_tsig, type = ftc.memberof_type }, this, false);
            string fom = Mangler2.MangleFieldInfoSymbol(ftc, this);
            int field_offset;

            ThreeAddressCode.Op ldfld_tac = GetAssignTac(ftc.fsig.CliType(this));
            if (i.Prefixes.volatile_)
                ldfld_tac = get_volatile_tac(ldfld_tac);

            if (static_)
            {
                Layout.Field iif = l.GetField(fom, true);
                field_offset = iif.offset;
                var static_field_addr = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i,
                    static_field_addr,
                    var.AddrOf(var.Label(l.static_object_name)), 0));                    

                if (get_address)
                {
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i,
                        state.next_variable++,
                        static_field_addr,
                        var.Const(field_offset)));
                }
                else
                {
                    i.tacs.Add(new ThreeAddressCode(ldfld_tac,
                        state.next_variable++,
                        var.ContentsOf(static_field_addr, field_offset), var.Null, iif.size));
                }
            }
            else
            {
                Layout.Field iif = l.GetField(ftc.field.Name, false);
                field_offset = iif.offset;
                var obj_var = i.stack_before[i.stack_before.Count - 1].contains_variable;

                if (ftc.memberof_type.IsValueType(this) && (obj_var.is_address_of_vt == false) &&
                    !(ftc.memberof_tsig.Type is Signature.BoxedType) && !(ftc.memberof_tsig.Type is Signature.ManagedPointer) &&
                    !(ftc.memberof_tsig.Type is Signature.UnmanagedPointer))
                {
                    /* Handle the case where the value on the stack is an instance of a value type (rather than reference type,
                     * boxed type or (un)managed pointer) */
                    obj_var = var.AddrOf(obj_var);
                }

                if (obj_var.type != var.var_type.LogicalVar)
                {
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++,
                        obj_var, var.Null));
                    obj_var = state.next_variable - 1;
                }

                if (get_address)
                {
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i,
                        state.next_variable++,
                        obj_var,
                        var.Const(field_offset)));
                }
                else
                {
                    i.tacs.Add(new ThreeAddressCode(ldfld_tac, state.next_variable++,
                        var.ContentsOf(obj_var, field_offset), var.Null, iif.size));
                }
            }

            i.pushes_variable = state.next_variable - 1;

            if (get_address)
                i.pushes = new Signature.Param(this) { Type = new Signature.ManagedPointer { ElemType = ftc.fsig.Type } };
            else
                i.pushes = ftc.fsig;
        }

        private void enc_checknullref(InstructionLine i, var v)
        {
            // Throw NullReferenceException if v == null
            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i, var.Null, v, var.Const(new IntPtr(0))));
            i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throweq, var.Null, var.Const(throw_NullReferenceException), var.Null));
        }

        private void enc_stfld(InstructionLine i, MethodToCompile containing_meth, Metadata m, bool static_, AssemblerState state)
        {
            /*Signature.Param sig;
            Metadata.FieldRow fdr = null;
            Metadata.TypeDefRow tdr;
            Signature.BaseOrComplexType tsig;*/

            FieldToCompile ftc;
            TypeToCompile stack_type;
            if (i.inline_tok is FTCToken)
                ftc = ((FTCToken)i.inline_tok).ftc;
            else
                ftc = Metadata.GetFTC(new Metadata.TableIndex(i.inline_tok), containing_meth.GetTTC(this), containing_meth.msig, this);

            if (static_)
            {
                if (_options.EnableRTTI)
                    Requestor.RequestTypeInfo(ftc.DefinedIn);
                else
                    Requestor.RequestStaticFields(ftc.DefinedIn);

                stack_type = ftc.DefinedIn;

                if (!i.cfg_node.types_whose_static_fields_are_referenced.Contains(ftc.DefinedIn))
                    i.cfg_node.types_whose_static_fields_are_referenced.Add(ftc.DefinedIn);
            }
            else
            {
                stack_type = new TypeToCompile { _ass = this, tsig = i.stack_before[i.stack_before.Count - 2].type, type = Metadata.GetTypeDef(i.stack_before[i.stack_before.Count - 2].type.Type, this) };
            }

            Layout l = Layout.GetLayout(stack_type, this, false);
            int field_size = GetSizeOf(ftc.fsig);

            string fom = Mangler2.MangleFieldInfoSymbol(ftc, this);
            int field_offset;

            ThreeAddressCode.Op stfld_tac = GetAssignTac(ftc.fsig.CliType(this));
            var op2 = var.Null;
            /* Handle the case where we are storing to a virtftnptr from a native int
             * 
             * This is supported only in mscorlib, for security reasons
             */
            if ((stfld_tac == ThreeAddressCode.Op.assign_virtftnptr) && (Signature.ParamCompare(i.stack_before[i.stack_before.Count - 1].type, new Signature.Param(BaseType_Type.I), this)))
            {
                if (state.security.AllowAssignIntPtrToVirtftnptr)
                {
                    stfld_tac = ThreeAddressCode.Op.assign_to_virtftnptr;
                    op2 = var.Const(new IntPtr(0));
                }
                else
                    throw new NotSupportedException();
            }

            if (i.Prefixes.volatile_)
                stfld_tac = get_volatile_tac(stfld_tac);

            if (static_)
            {
                field_offset = l.GetField(ftc.field.Name, true).offset;
                var static_field_addr = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i,
                    static_field_addr,
                    var.AddrOf(var.Label(l.static_object_name)), var.Null));                    
                i.tacs.Add(new ThreeAddressCode(stfld_tac,
                    var.ContentsOf(static_field_addr, field_offset),
                    i.stack_before[i.stack_before.Count - 1].contains_variable,
                    op2, field_size));
            }
            else
            {
                field_offset = l.GetField(ftc.field.Name, false).offset;
                var obj_var = i.stack_before[i.stack_before.Count - 2].contains_variable;
                if (obj_var.type != var.var_type.LogicalVar)
                {
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, obj_var, var.Null));
                    obj_var = state.next_variable - 1;
                }
                i.tacs.Add(new ThreeAddressCode(stfld_tac,
                    var.ContentsOf(obj_var, field_offset),
                    i.stack_before[i.stack_before.Count - 1].contains_variable,
                    op2, field_size));
            }
        }

        private ThreeAddressCode.Op get_volatile_tac(ThreeAddressCode.Op input)
        {
            switch (input)
            {
                case ThreeAddressCode.Op.assign_i:
                    return ThreeAddressCode.Op.assign_v_i;
                case ThreeAddressCode.Op.assign_i4:
                    return ThreeAddressCode.Op.assign_v_i4;
                case ThreeAddressCode.Op.assign_i8:
                    return ThreeAddressCode.Op.assign_v_i8;
                default:
                    throw new NotImplementedException("Volatile version of: " + input.ToString() + " not yet implemented");
            }
        }

        private int get_arg_count(Signature.BaseMethod m)
        {
            Signature.Method meth;
            if (m is Signature.Method)
                meth = m as Signature.Method;
            else if (m is Signature.GenericMethod)
                meth = ((Signature.GenericMethod)m).GenMethod;
            else
                throw new NotSupportedException();

            int arg_count = meth.Params.Count;
            if ((meth.HasThis == true) && (meth.ExplicitThis == false))
                arg_count++;

            return arg_count;
        }

        private void enc_call(InstructionLine i, Assembler.MethodToCompile containing_mtc, AssemblerState state)
        {
            /* Encode the opcodes call, calli, callvirt, ldftn and ldvirtftn */

            // First, identify the method to call
            Assembler.MethodToCompile call_mtc;
            if (i.inline_tok is MTCToken)
                call_mtc = ((MTCToken)i.inline_tok).mtc;
            else
                call_mtc = Metadata.GetMTC(new Metadata.TableIndex(i.inline_tok), containing_mtc.GetTTC(this), containing_mtc.msig, this);

            // Determine the type of call
            bool is_callvirt = false;
            bool is_ldvirtftn = false;
            bool is_ldftn = false;
            bool is_calli = false;
            bool is_call = false;
            bool is_anycall = false;
            bool is_virt = false;
            if (i.opcode.opcode == 0x6f)
                is_callvirt = true;
            if (i.opcode.opcode == 0xfe07)
                is_ldvirtftn = true;
            if (i.opcode.opcode == 0x29)
                is_calli = true;
            if(i.opcode.opcode == 0x28)
                is_call = true;
            if (is_calli || is_callvirt || is_call)
                is_anycall = true;
            if (is_callvirt || is_ldvirtftn)
                is_virt = true;
            if (i.opcode.opcode == 0xfe06)
                is_ldftn = true;

            // If calling an internal call, then emit it as an inline instead if possible
            if (is_call)
            {
                if (enc_intcall(i, call_mtc.meth, call_mtc.msig, call_mtc.type, call_mtc.tsigp, state))
                    return;
            }

            Signature.Method msigm = null;
            if(call_mtc.msig is Signature.Method)
                msigm = call_mtc.msig as Signature.Method;
            else
                msigm = ((Signature.GenericMethod)call_mtc.msig).GenMethod;
            int arg_count = get_arg_count(call_mtc.msig);


            // Determine the address of the function to call along with whether to adjust the this pointer
            var v_fptr = state.next_variable++;
            var v_thisadjust = var.Const(new IntPtr(0));
            var v_this_pointer = var.Null;

            if (is_virt && call_mtc.meth.IsVirtual)
            {
                // Identify the type on the stack
                Assembler.TypeToCompile cur_ttc = new TypeToCompile(i.stack_before[i.stack_before.Count - arg_count].type, this);
                Layout l = Layout.GetLayout(cur_ttc, this);

                // Identify the type that defines the method to call
                Assembler.TypeToCompile call_mtc_ttc = call_mtc.GetTTC(this);
                if (call_mtc_ttc.type.IsValueType(this) && !(call_mtc_ttc.tsig.Type is Signature.BoxedType))
                    call_mtc_ttc.tsig.Type = new Signature.BoxedType(call_mtc_ttc.tsig.Type);
                Layout call_mtc_ttc_l = Layout.GetLayout(call_mtc_ttc, this, false);

                // make the this pointer be a simple logical var
                v_this_pointer = i.stack_before[i.stack_before.Count - arg_count].contains_variable;
                if (v_this_pointer.type != var.var_type.LogicalVar)
                {
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, v_this_pointer, var.Null));
                    v_this_pointer = state.next_variable - 1;
                }

                // get the vtable for the this_pointer object - v_vtable = [v_this_pointer]
                var v_vtable = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_vtable, var.ContentsOf(v_this_pointer), var.Null));

                if (call_mtc_ttc.type.IsInterface)
                {
                    // Perform an interface call

                    /* Find the interface typeinfo */
                    var iface_ti_obj = state.next_variable++;
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, iface_ti_obj, var.AddrOfObject(call_mtc_ttc_l.typeinfo_object_name), var.Null));
                    Requestor.RequestTypeInfo(call_mtc_ttc);

                    /* Find the itablepointer (2nd entry in vtable) */
                    var itableptr_obj = state.next_variable++;
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, itableptr_obj, var.ContentsOf(v_vtable, GetSizeOfIntPtr()), var.Null));

                    /* The procedure from here is:
                     * 
                     * start_search:
                     *     cmp [itablepointer], 0
                     *     throw_equal interface_not_found
                     *     cmp [itablepointer], iface_ti
                     *     je iface_found
                     * do_loop:
                     *     add itable_pointer, GetSizeOfIntPtr() * 2
                     *     jmp start_search
                     * iface_found:
                     *     ifacemembers = [itablepointer + GetSizeOfIntPtr()]
                     *     vfptr = [ifacemembers + offset_within_interface]
                     */

                    int start_search = state.next_block++;
                    int do_loop = state.next_block++;
                    int iface_found = state.next_block++;
                    var ifacemembers = state.next_variable++;
                    Layout.Interface iface = Layout.GetInterfaceLayout(call_mtc_ttc, this);
                    int offset_within_interface = iface.GetVirtualMethod(call_mtc, this).offset;

                    i.tacs.Add(LabelEx.LocalLabel(start_search));
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i, var.Null, var.ContentsOf(itableptr_obj), var.Const(new IntPtr(0))));
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throweq, var.Null, var.Const(throw_MissingMethodException), var.Null));
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i, var.Null, var.ContentsOf(itableptr_obj), iface_ti_obj, var.Null));
                    i.tacs.Add(new BrEx(ThreeAddressCode.Op.beq, iface_found));
                    i.tacs.Add(LabelEx.LocalLabel(do_loop));
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, itableptr_obj, itableptr_obj, var.Const(GetSizeOfIntPtr() * 2)));
                    i.tacs.Add(new BrEx(ThreeAddressCode.Op.br, start_search));
                    i.tacs.Add(LabelEx.LocalLabel(iface_found));
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, ifacemembers, var.ContentsOf(itableptr_obj, GetSizeOfIntPtr()), var.Null));
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_fptr, var.ContentsOf(ifacemembers, offset_within_interface), var.Null));
                }
                else
                {
                    // Perform a virtual call

                    // Get the position of the virtual method in the vtable
                    Layout.Method m = call_mtc_ttc_l.GetVirtualMethod(call_mtc);
                    int vmeth_offset = m.offset;

                    // At this point, fptr = [v_vtable + vmeth_offset]
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_fptr, var.ContentsOf(v_vtable, vmeth_offset), var.Null));
                }
            }
            else if (is_calli)
            {
                // If its a call_i, then the last stack position will be of type virtftnptr and we extract the pointer from that

                if (!((i.stack_before[i.stack_before.Count - 1].type.Type is Signature.BaseType) && (((Signature.BaseType)i.stack_before[i.stack_before.Count - 1].type.Type).Type == BaseType_Type.VirtFtnPtr)))
                    throw new Exception("calli used without valid virtftnptr on stack");

                var v_virtftnptr = i.stack_before[i.stack_before.Count - 1].contains_variable;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_from_virtftnptr_ptr, v_fptr, v_virtftnptr, var.Null));

                // The arguments are one place further back for a calli instruction
                v_this_pointer = i.stack_before[i.stack_before.Count - arg_count - 1].contains_variable;
            }
            else
            {
                // Its either a simple call or ldftn.  Load the function pointer
                string func_name;

                // Does the method to call have a methodalias?
                if (call_mtc.meth.ReferenceAlias != null)
                    func_name = call_mtc.meth.ReferenceAlias;
                else
                    func_name = Mangler2.MangleMethod(call_mtc, this);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_fptr, var.AddrOfFunction(func_name), var.Null));

                // Request the method to be compiled
                Requestor.RequestMethod(call_mtc);

                // If its a static method we need to initialize it
                if (call_mtc.meth.IsStatic && !call_mtc.type.IsBeforeFieldInit)
                {
                    if (!i.cfg_node.types_whose_static_fields_are_referenced.Contains(call_mtc.GetTTC(this)))
                        i.cfg_node.types_whose_static_fields_are_referenced.Add(call_mtc.GetTTC(this));
                }
            }

            // If this is a ldftn/ldvirtftn, then create a virtftnptr and return it
            if (is_ldftn || is_ldvirtftn)
            {
                var v_virtftnptr = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_to_virtftnptr, v_virtftnptr, v_fptr, v_thisadjust));

                i.pushes = new Signature.Param(BaseType_Type.VirtFtnPtr);
                i.pushes_variable = v_virtftnptr;
                return;
            }

            // Determine the arguments to push
            bool is_vt_call = false;
            CallConv cc;
            var[] var_args = null;
            var_args = new var[arg_count];
            for (int j = 0; j < arg_count; j++)
            {
                var v = i.stack_before[i.stack_before.Count - (arg_count - j) + (is_calli ? -1 : 0)].contains_variable;
                if ((v.is_address_of_vt) && !((j == 0) && (call_mtc.msig.Method.HasThis || call_mtc.msig.Method.ExplicitThis)))
                    var_args[j] = var.ContentsOf(v, 0, v.v_size);
                else
                    var_args[j] = v;
            }

            // Get the calling convention
            ThreeAddressCode.Op call_tac;
            if (GetLdObjTac(msigm.RetType.CliType(this)) == ThreeAddressCode.Op.ldobj_vt)
            {
                is_vt_call = true;
                call_tac = ThreeAddressCode.Op.call_vt;
            }
            else
            {
                is_vt_call = false;
                call_tac = GetCallTac(msigm.RetType.CliType(this));
            }
            int vt_size = 0;
            if(is_vt_call)
                vt_size = GetSizeOf(msigm.RetType);
            string call_conv = Options.CallingConvention;
            if ((call_mtc.meth != null) && (call_mtc.meth.CallConvOverride != null))
                call_conv = call_mtc.meth.CallConvOverride;
            cc = call_convs[call_conv](call_mtc, CallConv.StackPOV.Caller, this, new ThreeAddressCode(call_tac, vt_size));

            // Make the actual call
            var v_ret = (msigm.RetType.CliType(this) == CliType.void_) ? 0 : state.next_variable++;
            
            i.tacs.Add(new CallEx(v_ret, var_args, call_tac, v_fptr, cc, vt_size));

            if (msigm.RetType.CliType(this) != CliType.void_)
                i.pushes_variable = v_ret;

            i.pop_count = arg_count;
            if (is_calli)
                i.pop_count++;
            i.pushes = msigm.RetType;
        }

        private void enc_ldarg(InstructionLine i, Metadata.MethodDefRow meth, Metadata m, int p, AssemblerState state)
        {
            i.pushes = i.la_before[p].type;
            i.pushes_variable = state.la_locs[p];
            
            
            /* var v_dest = state.next_variable++;
            i.tacs.Add(new ThreeAddressCode(GetAssignTac(i.pushes.CliType(this)), v_dest, var.LocalArg(p), var.Null, GetSizeOf(i.pushes)));
            i.pushes_variable = v_dest; */
        }

        private void enc_ldarga(InstructionLine i, Assembler.MethodToCompile mtc, int p, AssemblerState state)
        {
            i.pushes = new Signature.Param(this) { Type = new Signature.ManagedPointer { ElemType = i.la_before[p].type.Type } };
            //i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, var.AddrLocalArg(p), var.Null));
            //i.pushes_variable = state.next_variable - 1;
            i.pushes_variable = var.AddrOf(state.la_locs[p]);
        }

        private void enc_ldind(InstructionLine i, Assembler.MethodToCompile mtc, AssemblerState state)
        {
            ThreeAddressCode.Op op = ThreeAddressCode.Op.nop;
            switch (i.opcode.opcode1)
            {
                case SingleOpcodes.ldind_i:
                    op = ThreeAddressCode.Op.peek_u;
                    i.pushes = new Signature.Param(BaseType_Type.I);
                    break;
                case SingleOpcodes.ldind_ref:
                    {
                        op = ThreeAddressCode.Op.peek_u;

                        // see if we can determine the type of the object to load
                        Assembler.TypeToCompile prev_ttc = new TypeToCompile(i.stack_before[i.stack_before.Count - 1].type, this);
                        if (prev_ttc.tsig.Type is Signature.ManagedPointer)
                            i.pushes = new Signature.Param(((Signature.ManagedPointer)prev_ttc.tsig.Type).ElemType, this);
                        else
                            i.pushes = new Signature.Param(BaseType_Type.Object);
                        break;
                    }
                case SingleOpcodes.ldind_i1:
                    op = ThreeAddressCode.Op.peek_i1;
                    i.pushes = new Signature.Param(BaseType_Type.I1);
                    break;
                case SingleOpcodes.ldind_i2:
                    op = ThreeAddressCode.Op.peek_i2;
                    i.pushes = new Signature.Param(BaseType_Type.I2);
                    break;
                case SingleOpcodes.ldind_i4:
                    op = ThreeAddressCode.Op.peek_u4;
                    i.pushes = new Signature.Param(BaseType_Type.I4);
                    break;
                case SingleOpcodes.ldind_u4:
                    op = ThreeAddressCode.Op.peek_u4;
                    i.pushes = new Signature.Param(BaseType_Type.U4);
                    break;
                case SingleOpcodes.ldind_r4:
                    op = ThreeAddressCode.Op.peek_r4;
                    i.pushes = new Signature.Param(BaseType_Type.R4);
                    break;
                case SingleOpcodes.ldind_i8:
                    op = ThreeAddressCode.Op.peek_u8;
                    i.pushes = new Signature.Param(BaseType_Type.I8);
                    break;
                case SingleOpcodes.ldind_r8:
                    op = ThreeAddressCode.Op.peek_r8;
                    i.pushes = new Signature.Param(BaseType_Type.R8);
                    break;
                case SingleOpcodes.ldind_u1:
                    op = ThreeAddressCode.Op.peek_u1;
                    i.pushes = new Signature.Param(BaseType_Type.U1);
                    break;
                case SingleOpcodes.ldind_u2:
                    op = ThreeAddressCode.Op.peek_u2;
                    i.pushes = new Signature.Param(BaseType_Type.U2);
                    break;
                default:
                    throw new NotSupportedException();
            }

            i.tacs.Add(new ThreeAddressCode(op, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            i.pushes_variable = state.next_variable - 1;
        }

        private void enc_stind(InstructionLine i, Assembler.MethodToCompile mtc)
        {
            ThreeAddressCode.Op op = ThreeAddressCode.Op.nop;
            switch (i.opcode.opcode1)
            {
                case SingleOpcodes.stind_i:
                case SingleOpcodes.stind_ref:
                    op = ThreeAddressCode.Op.poke_u;
                    break;
                case SingleOpcodes.stind_i1:
                    op = ThreeAddressCode.Op.poke_u1;
                    break;
                case SingleOpcodes.stind_i2:
                    op = ThreeAddressCode.Op.poke_u2;
                    break;
                case SingleOpcodes.stind_i4:
                    op = ThreeAddressCode.Op.poke_u4;
                    break;
                case SingleOpcodes.stind_i8:
                    op = ThreeAddressCode.Op.poke_u8;
                    break;
                case SingleOpcodes.stind_r4:
                    op = ThreeAddressCode.Op.poke_r4;
                    break;
                case SingleOpcodes.stind_r8:
                    op = ThreeAddressCode.Op.poke_r8;
                    break;
                default:
                    throw new NotSupportedException();
            }

            i.tacs.Add(new ThreeAddressCode(op, var.Null, i.stack_before[i.stack_before.Count - 2].contains_variable, i.stack_before[i.stack_before.Count - 1].contains_variable));
        }

        private void enc_ldloca(InstructionLine i, Assembler.MethodToCompile mtc, int p, AssemblerState state)
        {
            Signature.Param lv_sig = i.lv_before[p].type;
            Metadata.TypeDefRow type = Metadata.GetTypeDef(lv_sig.Type, this);

            //i.pushes_variable = var.AddrLocalVar(p);
            i.pushes_variable = var.AddrOf(state.lv_locs[p]);
            i.pushes = new Signature.Param(this) { Type = new Signature.ManagedPointer { ElemType = lv_sig.Type } };
        }

        Opcode GetOpcode(SingleOpcodes s)
        {
            return Opcodes[(int)s];
        }
        Opcode GetOpcode(DoubleOpcodes d)
        {
            return ((int)d >= 0x20) ? Opcodes[0xfd00 + (int)d] : Opcodes[0xfe00 + (int)d];
        }

        internal class VerificationException : AssemblerException
        {
            public VerificationException(string msg, InstructionLine inst, MethodToCompile mtc) : base(msg, inst, mtc) { }
        }

        internal class MissingOpcodeException : AssemblerException
        {
            public MissingOpcodeException(ThreeAddressCode inst, MethodToCompile mtc)
                : base("Opcode " + inst.Operator.ToString() + " is not implemented " +
                    "for this architecture", null, mtc) { }
        }
    }
}
