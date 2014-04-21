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
    partial class x86_64_Assembler
    {
        IEnumerable<OutputBlock> x86_64_call(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return x86_64_call(op, result, op1, op2, tac, state, false); }

        IEnumerable<OutputBlock> x86_64_brlabel(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();
            if (op1.type == var.var_type.AddressOf && op1.base_var.v.type == var.var_type.Label)
            {
                switch (OType)
                {
                    case OutputType.x86_64_large_elf64:
                    case OutputType.x86_64_small_elf64:
                    case OutputType.i586_elf64:
                        ret.Add(new CodeBlock(new byte[] { 0xe9 }, new x86_64_Instruction { opcode = "jmp", Operand1 = op1.hardware_loc }));
                        ret.Add(new RelocationBlock
                        {
                            RelType = x86_64.x86_64_elf64.R_X86_64_PLT32,
                            Size = 4,
                            Target = op1.base_var.v.label,
                            Value = -4
                        });
                        break;
                    case OutputType.i586_elf:
                        ret.Add(new CodeBlock(new byte[] { 0xe9 }, new x86_64_Instruction { opcode = "jmp", Operand1 = op1.hardware_loc }));
                        ret.Add(new RelocationBlock
                        {
                            RelType = x86_64.x86_64_elf32.R_386_PLT32,
                            Size = 4,
                            Target = op1.base_var.v.label,
                            Value = -4
                        });
                        break;
                    default:
                        throw new Exception("Unknown output type");
                }
            }
            else
                throw new Exception("Invalid arguments to brlabel");

            return ret;
        }

        IEnumerable<OutputBlock> x86_64_call(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state, bool is_complex)
        {
            List<OutputBlock> ret = new List<OutputBlock>();

            /* Pass the arguments
             * 
             * We first allocate enough stack space, then allocate the arguments to the appropriate location, as determined by the calling convention
             */
            CallEx ce = tac as CallEx;
            CallConv cc = ce.call_conv;

            int stack_space_used = cc.StackSpaceUsed;
            hardware_location rcx_store = null;
            hardware_location rsi_store = null;
            hardware_location rdi_store = null;

            /* To push value types to the stack we need to use the string move instructions and therefore clobber Rcx, Rsi and Rdi
             * 
             * This means any variables that are either stored in any of these registers, or who are pointed to by these registers, need to be pushed first
             * Unfortunately sometimes there may be two value types to push, one that is pointed to by e.g. Rcx and one by Rsi
             * Then it becomes non-trivial to find the optimum way to push these two without destroying the address of the other
             * 
             * One way is to loop through first and push all integer values:
             *   - if they are stored in registers then push them as requested
             *   - if they are pointed to by registers then load them into the register that points to them, and then push them
             * Next, if there are any value types or floats to push, save Rcx, Rsi and Rdi to a location above the space stored at the beginning of the function
             * Then loop through and push floats and value types:
             *   - if they are pointed to by Rcx, Rsi or Rdi then use the saved values instead of the actual value
             *   - if they are pointed to by anything we need to do a quadword string move
             */

            bool is_simple = true;
            if (is_complex)
                is_simple = false;

            if ((cc.StackSpaceUsed > 0) || (!is_simple))
            {
                // First loop through and decide if we can do the call 'simply' i.e. if we don't need to save rcx, rsi and rdi
                // This is possible only if all the values are currently store in registers (gpr or xmm) or are consts <= 32 bits that can be pushed immediately
                if (is_simple)
                {
                    foreach (var v in ce.Var_Args)
                    {
                        if (!((v.hardware_loc is x86_64_gpr) || (v.hardware_loc is x86_64_xmm)))
                        {
                            if (v.hardware_loc is const_location)
                            {
                                object o = v.constant_val;
                                if (o == null) o = ((const_location)v.hardware_loc).c;
                                if (!FitsInt32(o))
                                {
                                    is_simple = false;
                                    break;
                                }
                            }
                            else
                            {
                                is_simple = false;
                                break;
                            }
                        }
                    }
                }

                if (is_simple)
                {
                    foreach (CallConv.ArgumentLocation arg_loc in cc.Arguments)
                    {
                        if (arg_loc.ValueSize > 8)
                        {
                            is_simple = false;
                            break;
                        }
                    }
                }

                if (!is_simple)
                {
                    /* If not simple, we need to save Rcx, Rsi and Rdi if they are used */
                    int offset = 0;

                    // rcx store is at [rsp + cc.StackSpaceUsed + 0]
                    // rsi store is at [rsp + cc.StackSpaceUsed + 8]
                    // rdi store is at [rsp + cc.StackSpaceUsed + 16]

                    if (tac.used_locations.Contains(Rcx))
                    {
                        rcx_store = new hardware_contentsof { base_loc = Rsp, const_offset = cc.StackSpaceUsed + offset, size = 8 };
                        offset += 8;
                    }
                    if (tac.used_locations.Contains(Rsi))
                    {
                        rsi_store = new hardware_contentsof { base_loc = Rsp, const_offset = cc.StackSpaceUsed + offset, size = 8 };
                        offset += 8;
                    }
                    if (tac.used_locations.Contains(Rdi))
                    {
                        rdi_store = new hardware_contentsof { base_loc = Rsp, const_offset = cc.StackSpaceUsed + offset, size = 8 };
                        offset += 8;
                    }
                    stack_space_used += offset;
                }

                ret.Add(new CodeBlock(EncOpcode(5, Rsp, 3, (ia == IA.x86_64), 0, (byte)((stack_space_used > SByte.MaxValue) ? 0x81 : 0x83)),
                    (stack_space_used > SByte.MaxValue) ? ToByteArray(Convert.ToInt32(stack_space_used)) : ToByteArray(Convert.ToSByte(stack_space_used)),
                    new x86_64_Instruction[] { new x86_64_Instruction { opcode = "sub", Operand1 = Rsp, Operand2 = new const_location { c = stack_space_used } } }));

                // Do the actual stores
                if (rcx_store != null)
                    ret.Add(new CodeBlock(EncOpcode(Rcx, rcx_store, 3, (ia == IA.x86_64), 0, 0x89), new x86_64_Instruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = rcx_store, Operand2 = Rcx } }));
                if (rsi_store != null)
                    ret.Add(new CodeBlock(EncOpcode(Rsi, rsi_store, 3, (ia == IA.x86_64), 0, 0x89), new x86_64_Instruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = rsi_store, Operand2 = Rsi } }));
                if (rdi_store != null)
                    ret.Add(new CodeBlock(EncOpcode(Rdi, rdi_store, 3, (ia == IA.x86_64), 0, 0x89), new x86_64_Instruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = rdi_store, Operand2 = Rdi } }));
            }


            // Now loop through the actual arguments and push them as required

            for (int arg_no = 0; arg_no < ce.Var_Args.Length; arg_no++)
            {
                var v = ce.Var_Args[arg_no];
                hardware_location src = v.hardware_loc;
                CallConv.ArgumentLocation dest = cc.Arguments[arg_no];

                // if src is a protected register (i.e rcx, rsi or rdi) or the contents of one, then re-write it
                if (!is_simple)
                {
                    if (src.Equals(Rcx) && rcx_store != null)
                        src = rcx_store;
                    else if (src.Equals(Rsi) && rsi_store != null)
                        src = rsi_store;
                    else if (src.Equals(Rdi) && rdi_store != null)
                        src = rdi_store;

                    else if (src is hardware_contentsof)
                    {
                        hardware_contentsof hco = src as hardware_contentsof;

                        if (hco.base_loc.Equals(Rcx) && rcx_store != null)
                            hco.base_loc = rcx_store;
                        if (hco.base_loc.Equals(Rsi) && rsi_store != null)
                            hco.base_loc = rsi_store;
                        if (hco.base_loc.Equals(Rdi) && rdi_store != null)
                            hco.base_loc = rdi_store;
                    }

                    else if (src is hardware_addressof)
                    {
                        hardware_addressof hao = src as hardware_addressof;

                        if (hao.base_loc.Equals(Rcx) && rcx_store != null)
                            hao.base_loc = rcx_store;
                        if (hao.base_loc.Equals(Rsi) && rsi_store != null)
                            hao.base_loc = rsi_store;
                        if (hao.base_loc.Equals(Rdi) && rdi_store != null)
                            hao.base_loc = rdi_store;
                    }
                }

                if (dest.ValueLocation != null)
                {
                    if (v.is_address_of_vt && !dest.ExpectsVTRef)
                        x86_64_assign(dest.ValueLocation, new hardware_contentsof { base_loc = src, size = v.v_size }, ret, ref is_complex);
                    else
                        x86_64_assign(dest.ValueLocation, src, ret, ref is_complex);
                }
                if (dest.ReferenceLocation != null)
                {
                    // Load a reference to the argument's actual value if required
                    x86_64_assign(dest.ReferenceLocation, src, ret, ref is_complex);

                    /*if (v.is_address_of_vt)
                        x86_64_assign(dest.ReferenceLocation, src, ret, ref is_complex);
                    else
                    {
                        // lea rcx, [dest.ValueLocation]
                        // mov [dest.ReferenceLocation], rcx
                        ret.Add(new CodeBlock(EncOpcode(Rcx, dest.ValueLocation, 0, true, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rcx, Operand2 = dest.ValueLocation }));
                        x86_64_assign(dest.ReferenceLocation, Rcx, ret, ref is_complex);
                    }*/
                }
            }

            /* Now do the actual call
             * 
             * This method can be used to call directly to a label (which would actually call through a jump table
             *  for PIC) or indirectly via a register +/- an offset
             */

            if(op1.type == var.var_type.AddressOf && op1.base_var.v.type == var.var_type.Label)
            {
                if (_options.PIC)
                {
                    switch (OType)
                    {
                        case OutputType.x86_64_large_elf64:
                        case OutputType.x86_64_small_elf64:
                        case OutputType.i586_elf64:
                            ret.Add(new CodeBlock(new byte[] { 0xe8 }, new x86_64_Instruction { opcode = "call", Operand1 = op1.hardware_loc }));
                            ret.Add(new RelocationBlock
                            {
                                RelType = x86_64.x86_64_elf64.R_X86_64_PLT32,
                                Size = 4,
                                Target = op1.base_var.v.label,
                                Value = -4
                            });
                            break;
                        case OutputType.i586_elf:
                            ret.Add(new CodeBlock(new byte[] { 0xe8 }, new x86_64_Instruction { opcode = "call", Operand1 = op1.hardware_loc }));
                            ret.Add(new RelocationBlock
                            {
                                RelType = x86_64.x86_64_elf32.R_386_PLT32,
                                Size = 4,
                                Target = op1.base_var.v.label,
                                Value = -4
                            });
                            break;
                        default:
                            throw new Exception("Unknown output type");
                    }
                }
                else
                {
                    switch (OType)
                    {
                        case OutputType.x86_64_large_elf64:
                        case OutputType.x86_64_small_elf64:
                        case OutputType.i586_elf64:
                            ret.Add(new CodeBlock(new byte[] { 0xe8 }, new x86_64_Instruction { opcode = "call", Operand1 = op1.hardware_loc }));
                            ret.Add(new RelocationBlock
                            {
                                RelType = x86_64.x86_64_elf64.R_X86_64_PC32,
                                Size = 4,
                                Target = op1.base_var.v.label,
                                Value = -4
                            });
                            break;
                        case OutputType.i586_elf:
                            ret.Add(new CodeBlock(new byte[] { 0xe8 }, new x86_64_Instruction { opcode = "call", Operand1 = op1.hardware_loc }));
                            ret.Add(new RelocationBlock
                            {
                                RelType = x86_64.x86_64_elf32.R_386_PC32,
                                Size = 4,
                                Target = op1.base_var.v.label,
                                Value = -4
                            });
                            break;
                        case OutputType.x86_64_jit:
                            // move absoulte address to rax, then call rax
                            ret.Add(new CodeBlock(EncAddOpcode(Rax, true, 0xb8)));
                            ret.Add(new RelocationBlock { RelType = x86_64.x86_64_elf64.R_X86_64_64, Size = 8, Value = 0, Target = op1.base_var.v.label });
                            ret.Add(new CodeBlock(EncOpcode(2, Rax, 3, true, 0, 0xff)));
                            break;
                        default:
                            throw new Exception("Unknown output type");
                    }
                }
            }
            else if((op1.hardware_loc is x86_64_gpr) || (op1.hardware_loc is hardware_contentsof))
                ret.Add(new CodeBlock(EncOpcode(2, op1.hardware_loc, 3, false, 0, 0xff), new x86_64_Instruction { opcode = "call", Operand1 = op1.hardware_loc }));

            /* if the call returns a value type, we need to save it to where it should go */
            if (op == ThreeAddressCode.Op.call_vt)
                x86_64_assign(result.hardware_loc, new hardware_contentsof { base_loc = Rax, size = tac.VTSize.Value }, ret, ref is_complex);

            if ((stack_space_used != 0) && (cc.CallerCleansStack))
            {
                // clean up the stack if we are required to do so
                ret.Add(new CodeBlock(EncOpcode(0, Rsp, 3, (ia == IA.x86_64), 0, (byte)((stack_space_used > SByte.MaxValue) ? 0x81 : 0x83)),
                    (stack_space_used > SByte.MaxValue) ? ToByteArray(Convert.ToInt32(stack_space_used)) : ToByteArray(Convert.ToSByte(stack_space_used)),
                    new x86_64_Instruction[] { new x86_64_Instruction { opcode = "add", Operand1 = Rsp, Operand2 = new const_location { c = stack_space_used } } }));
            }

            /* Pick up the occasions where we haven't predicted a complex push but in fact it was necessary */
            if (is_complex && is_simple)
                return x86_64_call(op, result, op1, op2, tac, state, true);

            return ret;
        }

        internal override void Assign(hardware_location dest, hardware_location from, List<OutputBlock> ret, AssemblerState state)
        {
            x86_64_assign(dest, from, ret);
        }

        private void x86_64_assign(hardware_location dest, hardware_location src, List<OutputBlock> ret)
        { bool isc = false; x86_64_assign(dest, src, ret, ref isc); }

        private void x86_64_assign(hardware_location dest, hardware_location src, List<OutputBlock> ret, ref bool is_complex)
        {
            if (dest == null)
                throw new Exception("dest cannot be null");
            if (src == null)
                throw new Exception("src cannot be null");

            if ((dest is hardware_contentsof) || (dest is hardware_stackloc))
            {
                if (src is x86_64_gpr)
                    ret.Add(new CodeBlock(EncOpcode(src, dest, 3, (ia == IA.x86_64), 0, 0x89), new x86_64_Instruction { opcode = "mov", Operand1 = dest, Operand2 = src }));
                else if ((src is hardware_contentsof) || (src is hardware_stackloc))
                {
                    int dest_size = (dest is hardware_contentsof) ? ((hardware_contentsof)dest).size : ((hardware_stackloc)dest).size;
                    int src_size = (src is hardware_contentsof) ? ((hardware_contentsof)src).size : ((hardware_stackloc)src).size;

                    if ((src is hardware_contentsof) && (((hardware_contentsof)src).base_loc is hardware_contentsof))
                    {
                        /* deal with the case where src is [[rsp + x] + y]
                            * this is created by call trying to push [rcx], where we have to use rcx_store instead (which is usually [rsp + x])
                            */

                        hardware_location intermediate = ((hardware_contentsof)src).base_loc;
                        ret.Add(new CodeBlock(EncOpcode(Rcx, intermediate, 3, (ia == IA.x86_64), 0, 0x8b), new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = intermediate }));
                        ((hardware_contentsof)src).base_loc = Rcx;
                        is_complex = true;
                    }

                    if (ia == IA.x86_64)
                    {
                        if ((src_size <= 8) && (dest_size <= 8))
                        {
                            ret.Add(new CodeBlock(EncOpcode(Rcx, src, 3, (src_size == 8) ? true : false, 0, 0x8b),
                                EncOpcode(Rcx, dest, 3, (dest_size == 8) ? true : false, 0, 0x89),
                                new x86_64_Instruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = src },
                            new x86_64_Instruction { opcode = "mov", Operand1 = dest, Operand2 = Rcx }
                        }));
                            is_complex = true;
                        }
                        else
                        {
                            int chunk_size = 8;
                            if ((dest_size % 8) == 0)
                                chunk_size = 8;
                            else if ((dest_size % 4) == 0)
                                chunk_size = 4;
                            else if ((dest_size % 2) == 0)
                                chunk_size = 2;
                            else
                                chunk_size = 1;

                            ret.Add(new CodeBlock(EncOpcode(Rdi, dest, 0, true, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rdi, Operand2 = dest }));
                            ret.Add(new CodeBlock(EncOpcode(Rsi, src, 0, true, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rsi, Operand2 = src }));
                            ret.Add(new CodeBlock(EncOpcode(0, Rcx, 3, true, 0, 0xc7), ToByteArray(dest_size / chunk_size),
                                new x86_64_Instruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = new const_location { c = dest_size / chunk_size } } }));

                            switch (chunk_size)
                            {
                                case 8:
                                    ret.Add(new CodeBlock(new byte[] { 0xf3, 0x48, 0xa5 }, new x86_64_Instruction { opcode = "rep movsq" }));
                                    break;
                                case 4:
                                    ret.Add(new CodeBlock(new byte[] { 0xf3, 0xa5 }, new x86_64_Instruction { opcode = "rep movsd" }));
                                    break;
                                case 2:
                                    ret.Add(new CodeBlock(new byte[] { 0x66, 0xf3, 0xa5 }, new x86_64_Instruction { opcode = "rep movsw" }));
                                    break;
                                case 1:
                                    ret.Add(new CodeBlock(new byte[] { 0xf3, 0xa4 }, new x86_64_Instruction { opcode = "rep movsb" }));
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }

                            is_complex = true;
                        }
                    }
                    else
                    {
                        // ia = i586

                        if ((src_size <= 4) && (dest_size <= 4))
                        {
                            ret.Add(new CodeBlock(EncOpcode(Rcx, src, 3, false, 0, 0x8b),
                                EncOpcode(Rcx, dest, 3, false, 0, 0x89),
                                new x86_64_Instruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = src },
                            new x86_64_Instruction { opcode = "mov", Operand1 = dest, Operand2 = Rcx }
                        }));
                            is_complex = true;
                        }
                        else
                        {
                            int chunk_size = 4;
                            if ((dest_size % 4) == 0)
                                chunk_size = 4;
                            else if ((dest_size % 2) == 0)
                                chunk_size = 2;
                            else
                                chunk_size = 1;

                            ret.Add(new CodeBlock(EncOpcode(Rdi, dest, 0, false, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rdi, Operand2 = dest }));
                            ret.Add(new CodeBlock(EncOpcode(Rsi, src, 0, false, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rsi, Operand2 = src }));
                            ret.Add(new CodeBlock(EncOpcode(0, Rcx, 3, false, 0, 0xc7), ToByteArray(dest_size / chunk_size),
                                new x86_64_Instruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = new const_location { c = dest_size / chunk_size } } }));

                            switch (chunk_size)
                            {
                                case 4:
                                    ret.Add(new CodeBlock(new byte[] { 0xf3, 0xa5 }, new x86_64_Instruction { opcode = "rep movsd" }));
                                    break;
                                case 2:
                                    ret.Add(new CodeBlock(new byte[] { 0x66, 0xf3, 0xa5 }, new x86_64_Instruction { opcode = "rep movsw" }));
                                    break;
                                case 1:
                                    ret.Add(new CodeBlock(new byte[] { 0xf3, 0xa4 }, new x86_64_Instruction { opcode = "rep movsb" }));
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }

                            is_complex = true;
                        }
                    }
                }
                else if (src is const_location)
                {
                    object c = ((const_location)src).c;

                    if (FitsInt32(c))
                    {
                        if (IsSigned(c))
                            ret.Add(new CodeBlock(EncOpcode(0, dest, 3, (ia == IA.x86_64), 0, 0xc7), ToByteArraySignExtend(c, 4), new x86_64_Instruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = dest, Operand2 = src } }));
                        else
                            ret.Add(new CodeBlock(EncOpcode(0, dest, 3, false, 0, 0xc7), ToByteArraySignExtend(c, 4), new x86_64_Instruction[] { new x86_64_Instruction { opcode = "mov", Operand1 = dest, Operand2 = src } }));
                    }
                    else
                    {
                        if (ia == IA.i586)
                            throw new Exception("Constant value does not fit in 32 bit register");
                        ret.Add(new CodeBlock(EncAddOpcode(Rcx, true, 0xb8, ToByteArraySignExtend(c, 8)), new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = src }));
                        ret.Add(new CodeBlock(EncOpcode(Rcx, dest, 3, true, 0, 0x89), new x86_64_Instruction { opcode = "mov", Operand1 = dest, Operand2 = Rcx }));
                        is_complex = true;
                    }
                }
                else if (src is hardware_addressoflabel)
                {
                    if (_options.PIC)
                    {
                        switch (OType)
                        {
                            case OutputType.x86_64_large_elf64:
                            case OutputType.x86_64_small_elf64:
                                ret.Add(new CodeBlock(EncOpcode(Rcx, 5, 0, true, 0, 0x8b), new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = src }));
                                ret.Add(new RelocationBlock
                                {
                                    Size = 4,
                                    RelType = x86_64.x86_64_elf64.R_X86_64_GOTPCREL,
                                    Target = ((hardware_addressoflabel)src).label,
                                    Value = -4
                                });
                                break;
                            case OutputType.i586_elf64:
                                ret.Add(new CodeBlock { Code = new byte[] { 0x8b, ModRM(2, (byte)((int)Rcx.reg % 8), (byte)((int)Rbx.reg % 8)) } });
                                ret.Add(new RelocationBlock { Target = ((hardware_addressoflabel)src).label, RelType = x86_64.x86_64_elf64.R_X86_64_GOT32, Size = 4, Value = 0 });
                                break;
                            case OutputType.i586_elf:
                                ret.Add(new CodeBlock { Code = new byte[] { 0x8b, ModRM(2, (byte)((int)Rcx.reg % 8), (byte)((int)Rbx.reg % 8)) } });
                                ret.Add(new RelocationBlock { Target = ((hardware_addressoflabel)src).label, RelType = x86_64.x86_64_elf32.R_386_GOT32, Size = 4, Value = 0 });
                                break;
                            default:
                                throw new Exception("Unknown output type");
                        }
                    }
                    else
                    {
                        switch (OType)
                        {
                            case OutputType.x86_64_large_elf64:
                            case OutputType.x86_64_jit:
                                ret.Add(new CodeBlock(EncAddOpcode(Rcx, true, 0xb8)));
                                ret.Add(new RelocationBlock { Size = 8, RelType = x86_64.x86_64_elf64.R_X86_64_64, Target = ((hardware_addressoflabel)src).label, Value = 0 });
                                break;
                            case OutputType.x86_64_small_elf64:
                                ret.Add(new CodeBlock(EncOpcode(0, Rcx, 3, false, 0, 0xc7)));
                                ret.Add(new RelocationBlock { Size = 4, RelType = x86_64.x86_64_elf64.R_X86_64_32, Target = ((hardware_addressoflabel)src).label, Value = 0 });
                                break;
                            case OutputType.i586_elf64:
                                ret.Add(new CodeBlock(EncAddOpcode(Rcx, false, 0xb8)));
                                ret.Add(new RelocationBlock { Size = 4, RelType = x86_64.x86_64_elf64.R_X86_64_32, Target = ((hardware_addressoflabel)src).label, Value = 0 });
                                break;
                            case OutputType.i586_elf:
                                ret.Add(new CodeBlock(EncAddOpcode(Rcx, false, 0xb8)));
                                ret.Add(new RelocationBlock { Size = 4, RelType = x86_64.x86_64_elf32.R_386_32, Target = ((hardware_addressoflabel)src).label, Value = 0 });
                                break;
                            default:
                                throw new Exception("Unknown output type");
                        }
                    }

                    if (((hardware_addressoflabel)src).const_offset != 0)
                    {
                        ret.Add(new CodeBlock(EncOpcode(0, Rcx, 3, (ia == IA.x86_64), 0, 0x81), ToByteArray(((hardware_addressoflabel)src).const_offset),
                            new x86_64_Instruction[] { new x86_64_Instruction { opcode = "add", Operand1 = Rcx, Operand2 = new const_location { c = ((hardware_addressoflabel)src).const_offset } } }));
                    }

                    ret.Add(new CodeBlock(EncOpcode(Rcx, dest, 0, (ia == IA.x86_64), 0, 0x89), new x86_64_Instruction { opcode = "mov", Operand1 = dest, Operand2 = Rcx }));
                    is_complex = true;
                }
                else if (src is hardware_addressof)
                {
                    hardware_location base_loc = ((hardware_addressof)src).base_loc;

                    if ((base_loc is hardware_contentsof) || (base_loc is hardware_stackloc))
                    {
                        ret.Add(new CodeBlock(EncOpcode(Rcx, base_loc, 0, (ia == IA.x86_64), 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rcx, Operand2 = base_loc }));
                        ret.Add(new CodeBlock(EncOpcode(Rcx, dest, 0, (ia == IA.x86_64), 0, 0x89), new x86_64_Instruction { opcode = "mov", Operand1 = dest, Operand2 = Rcx }));
                        is_complex = true;
                    }
                    else
                        throw new NotSupportedException();
                }
                else if (src is x86_64_xmm)
                    ret.Add(new CodeBlock(EncOpcode(src, dest, 0, false, 0, 0xf2, 0x0f, 0x11), new x86_64_Instruction { opcode = "movsd", Operand1 = dest, Operand2 = src }));
                else
                    throw new NotSupportedException();
            }
            else if (dest is x86_64_gpr)
            {
                x86_64_gpr d = dest as x86_64_gpr;
                if ((src is x86_64_gpr) || (src is hardware_contentsof) || (src is hardware_stackloc))
                    ret.Add(new CodeBlock(EncOpcode(dest, src, 3, (ia == IA.x86_64), 0, 0x8b), new x86_64_Instruction { opcode = "mov", Operand1 = dest, Operand2 = src }));
                else if (src is const_location)
                {
                    const_location c = src as const_location;
                    if (FitsInt32(c.c))
                    {
                        if (IsSigned(c.c))
                            ret.Add(new CodeBlock(EncOpcode(0, dest, 3, (ia == IA.x86_64), 0, 0xc7), ToByteArraySignExtend(c.c, 4)));
                        else
                            ret.Add(new CodeBlock(EncAddOpcode(dest as x86_64_gpr, false, 0xb8, ToByteArrayZeroExtend(c.c, 4))));
                    }
                    else
                    {
                        if (ia == IA.i586)
                            throw new Exception("Trying to assign 64 bit value to 32 bit register");
                        ret.Add(new CodeBlock(EncAddOpcode(dest as x86_64_gpr, true, 0xb8, ToByteArrayZeroExtend(c.c, 8))));
                    }
                }
                else if (src is hardware_addressoflabel)
                {
                    if (_options.PIC)
                    {
                        switch (OType)
                        {
                            case OutputType.x86_64_large_elf64:
                            case OutputType.x86_64_small_elf64:
                                ret.Add(new CodeBlock(EncOpcode(dest, 5, 0, true, 0, 0x8b), new x86_64_Instruction { opcode = "mov", Operand1 = d, Operand2 = src }));
                                ret.Add(new RelocationBlock
                                {
                                    Size = 4,
                                    RelType = x86_64.x86_64_elf64.R_X86_64_GOTPCREL,
                                    Target = ((hardware_addressoflabel)src).label,
                                    Value = -4
                                });
                                break;
                            case OutputType.i586_elf64:
                                ret.Add(new CodeBlock { Code = new byte[] { 0x8b, ModRM(2, (byte)((int)d.reg % 8), (byte)((int)Rbx.reg % 8)) } });
                                ret.Add(new RelocationBlock { Target = ((hardware_addressoflabel)src).label, RelType = x86_64.x86_64_elf64.R_X86_64_GOT32, Size = 4, Value = 0 });
                                break;
                            case OutputType.i586_elf:
                                ret.Add(new CodeBlock { Code = new byte[] { 0x8b, ModRM(2, (byte)((int)d.reg % 8), (byte)((int)Rbx.reg % 8)) } });
                                ret.Add(new RelocationBlock { Target = ((hardware_addressoflabel)src).label, RelType = x86_64.x86_64_elf32.R_386_GOT32, Size = 4, Value = 0 });
                                break;
                            default:
                                throw new Exception("Unknown output type");
                        }
                    }
                    else
                    {
                        switch (OType)
                        {
                            case OutputType.x86_64_large_elf64:
                            case OutputType.x86_64_jit:
                                ret.Add(new CodeBlock(EncAddOpcode(d, true, 0xb8)));
                                ret.Add(new RelocationBlock { Size = 8, RelType = x86_64.x86_64_elf64.R_X86_64_64, Target = ((hardware_addressoflabel)src).label, Value = 0 });
                                break;
                            case OutputType.x86_64_small_elf64:
                                ret.Add(new CodeBlock(EncOpcode(0, d, 3, false, 0, 0xc7)));
                                ret.Add(new RelocationBlock { Size = 4, RelType = x86_64.x86_64_elf64.R_X86_64_32, Target = ((hardware_addressoflabel)src).label, Value = 0 });
                                break;
                            case OutputType.i586_elf64:
                                ret.Add(new CodeBlock(EncAddOpcode(d, false, 0xb8)));
                                ret.Add(new RelocationBlock { Size = 4, RelType = x86_64.x86_64_elf64.R_X86_64_32, Target = ((hardware_addressoflabel)src).label, Value = 0 });
                                break;
                            case OutputType.i586_elf:
                                ret.Add(new CodeBlock(EncAddOpcode(d, false, 0xb8)));
                                ret.Add(new RelocationBlock { Size = 4, RelType = x86_64.x86_64_elf32.R_386_32, Target = ((hardware_addressoflabel)src).label, Value = 0 });
                                break;
                            default:
                                throw new Exception("Unknown output type");
                        }
                    }

                    if (((hardware_addressoflabel)src).const_offset != 0)
                    {
                        ret.Add(new CodeBlock(EncOpcode(0, d, 3, (ia == IA.x86_64), 0, 0x81), ToByteArray(((hardware_addressoflabel)src).const_offset),
                            new x86_64_Instruction[] { new x86_64_Instruction { opcode = "add", Operand1 = d, Operand2 = new const_location { c = ((hardware_addressoflabel)src).const_offset } } }));
                    }
                }
                else
                    throw new NotSupportedException();
            }
            else
                throw new NotSupportedException();
        }

    }
}
