/* Copyright (C) 2008 - 2013 by John Cronin
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

namespace libtysila.arm
{
    partial class arm_Assembler : LSB_Assembler
    {
        internal enum FT { softfloat };
        internal enum ABI { eabi };

        FT ft;
        ABI abi;

        public arm_Assembler(Architecture arch, FileLoader fileLoader, MemberRequestor memberRequestor, AssemblerOptions options) : base(arch, fileLoader, memberRequestor, options)
        {
            if ((arch.OutputFormat != "jit") && (arch.OutputFormat != "elf"))
                throw new Exception("Invalid output format: " + arch.OutputFormat);

            if ((arch.InstructionSet == "arm") || (arch.InstructionSet == "arm-softfloat") || (arch.InstructionSet == "arm-eabi") ||
                (arch.InstructionSet == "arm-softfloat-eabi"))
            {
                ft = FT.softfloat;
                abi = ABI.eabi;
            }
            else
                throw new Exception("Invalid instruction set: " + arch.InstructionSet);

            arch._instruction_set = "arm";            

            if (arch.OutputFormat == "jit")
                is_jit = true;

            if (arch.OperatingSystem != "tysos")
                throw new Exception("Invalid operating system: " + arch.OperatingSystem);

            // Always use the fast register allocator for ARM as it has so many registers and there are few constraints on which
            //  register to use in each instruction
            options.RegAlloc = AssemblerOptions.RegisterAllocatorType.fastreg;
        }

        public static Assembler.Architecture[] ListAssemblerArchitectures()
        {
            return new Assembler.Architecture[] {
                new Assembler.Architecture { _instruction_set = "arm", _oformat = "elf", _os = "tysos", _extra_ops = new List<string> { "args_in_registers" } },
                new Assembler.Architecture { _instruction_set = "arm", _oformat = "jit", _os = "tysos", _extra_ops = new List<string> { "args_in_registers" } },
                new Assembler.Architecture { _instruction_set = "arm-eabi", _oformat = "elf", _os = "tysos", _extra_ops = new List<string> { "args_in_registers" } },
                new Assembler.Architecture { _instruction_set = "arm-eabi", _oformat = "jit", _os = "tysos", _extra_ops = new List<string> { "args_in_registers" } },
                new Assembler.Architecture { _instruction_set = "arm-softfloat", _oformat = "elf", _os = "tysos", _extra_ops = new List<string> { "args_in_registers" } },
                new Assembler.Architecture { _instruction_set = "arm-softfloat", _oformat = "jit", _os = "tysos", _extra_ops = new List<string> { "args_in_registers" } },
                new Assembler.Architecture { _instruction_set = "arm-softfloat-eabi", _oformat = "elf", _os = "tysos", _extra_ops = new List<string> { "args_in_registers" } },
                new Assembler.Architecture { _instruction_set = "arm-softfloat-eabi", _oformat = "jit", _os = "tysos", _extra_ops = new List<string> { "args_in_registers" } },
            };
        }

        internal override Bitness GetBitness()
        {
            return Bitness.Bits32;
        }

        internal override void ArchSpecific(List<ThreeAddressCode> ir, List<Assembler.cfg_node> nodes, Assembler.AssemblerState state, Assembler.MethodToCompile mtc)
        {
        }

        internal override List<OutputBlock> ArchSpecificProlog(AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();

            /* Arm instructions start with the special symbol $a */
            ret.Add(new LocalSymbol("$a", false));

            /* Do:
             * 
             * push lr
             * push fp
             * mov fp, sp
             * (sub sp, stack_space_used) (if stack_space_used != 0)
             */

            ret.Add(EncSingleRegListOpcode(cond.Always, 0x12, SP, LR));
            ret.Add(EncSingleRegListOpcode(cond.Always, 0x12, SP, FP));
            ret.Add(EncDPROpcode(cond.Always, 0x1a, R0, FP, 0, 0, SP));
            if (state.stack_space_used > 0)
                ret.Add(EncImmOpcode(cond.Always, 0x4, SP, SP, (uint)state.stack_space_used));

            return ret;
        }

        internal override hloc_constraint GetConstraintFromSemantic(var_semantic vs)
        {
            if (vs.needs_memloc || vs.needs_vtype || vs.needs_virtftnptr)
            {
                hloc_constraint ret = CMem;
                ((hardware_stackloc)ret.specific).size = vs.vtype_size;
                return ret;
            }
            if (vs.needs_float)
                throw new NotImplementedException("ARM floating point not yet implemented");
            if (vs.needs_int32 || vs.needs_intptr)
                return CGpr;
            if (vs.needs_int64)
                return C2Gpr;
            throw new NotSupportedException();
        }
        
        internal override List<byte> SaveLocation(hardware_location loc)
        {
            if (loc is arm_gpr)
            {
                return new List<byte>(EncSingleRegListOpcode(cond.Always, 0x12, SP, loc).Code);
            }
            else
                throw new NotImplementedException();
        }

        internal override List<byte> RestoreLocation(hardware_location loc)
        {
            if (loc is arm_gpr)
            {
                return new List<byte>(EncSingleRegListOpcode(cond.Always, 0x9, SP, loc).Code);
            }
            else
                throw new NotImplementedException();
        }

        internal override List<byte> SwapLocation(hardware_location a, hardware_location b)
        {
            throw new NotImplementedException();
        }

        public override uint DataToDataRelocType()
        {
            return 2;       // R_ARM_ABS32
        }

        internal override byte[] IntPtrByteArray(object v)
        {
            return ToByteArray(Convert.ToInt32(v));
        }

        internal override object ConvertToI(object v)
        {
            return Convert.ToInt32(v);
        }

        internal override object ConvertToU(object v)
        {
            return Convert.ToInt32(v);
        }

        internal override int GetSizeOfUncondBr()
        {
            return 4;
        }

        public override MiniAssembler GetMiniAssembler()
        {
            throw new NotImplementedException();
        }

        internal override tydb.Location GetDebugLocation(hardware_location loc)
        {
            return null;
        }

        public override int GetPackedSizeOf(Signature.Param p)
        {
            if (p.Type is Signature.BaseType)
            {
                Signature.BaseType bt = p.Type as Signature.BaseType;
                switch (bt.Type)
                {
                    case BaseType_Type.I1:
                    case BaseType_Type.Byte:
                    case BaseType_Type.Boolean:
                    case BaseType_Type.U1:
                        return 1;
                    case BaseType_Type.Char:
                    case BaseType_Type.I2:
                    case BaseType_Type.U2:
                        return 2;
                }
            }
            return GetSizeOf(p);
        }

        public override IntPtr FromByteArrayI(IList<byte> v)
        {
            return new IntPtr(FromByteArrayI4(v));
        }

        public override IntPtr FromByteArrayI(IList<byte> v, int offset)
        {
            return new IntPtr(FromByteArrayI4(v, offset));
        }

        public override UIntPtr FromByteArrayU(IList<byte> v)
        {
            return new UIntPtr(FromByteArrayU4(v));
        }

        public override UIntPtr FromByteArrayU(IList<byte> v, int offset)
        {
            return new UIntPtr(FromByteArrayU4(v, offset));
        }

        public override string GetCType(BaseType_Type baseType_Type)
        {
            throw new NotImplementedException();
        }

        internal override int GetSizeOf(Signature.Param p)
        {
            switch (p.CliType(this))
            {
                case CliType.F64:
                    return 8;
                case CliType.F32:
                    return 4;
                case CliType.int32:
                    return 4;
                case CliType.int64:
                    return 8;
                case CliType.native_int:
                case CliType.O:
                case CliType.reference:
                case CliType.void_:
                    return 4;
                case CliType.vt:
                    return GetSizeOfType(p);
                case CliType.virtftnptr:
                    return 8;
                default:
                    throw new NotSupportedException();
            }
        }

        internal override IEnumerable<hardware_location> GetAllHardwareLocationsOfType(Type type, hardware_location example)
        {
            if (type == typeof(arm_gpr))
            {
                yield return R4;
                yield return R5;
                yield return R6;
                yield return R7;
                yield return R8;
                yield return R9;
                yield return R0;
                yield return R1;
                yield return R2;
                yield return R3;

                if (Options.EnableRTTI)
                    yield return R10;

                yield break;
            }
            else if (type == typeof(multiple_hardware_location))
            {
                foreach (hardware_location hl in GetMultipleHardwareLocations(example as multiple_hardware_location))
                    yield return hl;
                yield break;
            }
            throw new NotSupportedException();
        }

        internal override hardware_location GetMethinfoPointerLocation()
        {
            return R10;
        }

        hardware_location InterpretStackLocation(hardware_stackloc sl, AssemblerState state)
        {
            /* The enter code for ARM, if the stack is used for arguments, is:
             * push { lr }
             * push { fp }
             * mov fp, sp
             * sub sp, #stack_space_used
             * 
             * The local vars are therefore located at:
             *  [ fp - (stack_loc + 4) ]
            */

            int stack_offset = -(sl.loc + 4) + sl.offset_within_loc;
            return new hardware_contentsof { base_loc = FP, const_offset = stack_offset, size = sl.size };
        }

        public override util.Set<hardware_location> MachineRegisters
        {
            get { throw new NotImplementedException(); }
        }

        public override util.Set<hardware_location> MachineRegistersForDataType(CliType dt)
        {
            throw new NotImplementedException();
        }
    }
}
