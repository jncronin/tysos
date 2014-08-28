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

/* A new implementation of the var class, which encapsulates the type of the
 * arguments too.
 * 
 * It is renamed to vara to prevent clashes with the new var directive in C#
 */

namespace libtysila
{
    public struct vara
    {
        public enum vara_type { Void, Logical, AddrOf, ContentsOf, Label, Const, MachineReg };

        vara_type type;
        int log;
        long offset;
        string label;
        object c;
        Assembler.CliType ct;
        int ssa;
        libasm.hardware_location mreg;
        public Signature.Param vt_type;
        public bool vt_addr_of;
        public bool needs_memloc;
        bool is_object;

        public bool HasLogicalVar
        {
            get
            {
                switch (VarType)
                {
                    case vara_type.AddrOf:
                    case vara_type.ContentsOf:
                    case vara_type.Logical:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public vara GetLogicalVar()
        {
            if (!HasLogicalVar)
                throw new Exception("Cannot get logical var of var type " + VarType.ToString());
            return vara.Logical(LogicalVar, SSA, DataType);
        }

        public bool IsObject { get { return is_object; } }

        public static vara Void()
        {
            return new vara { type = vara_type.Void, ct = Assembler.CliType.void_ };
        }

        public static vara Logical(int Logical, Assembler.CliType DataType)
        {
            return new vara { log = Logical, ct = DataType, type = vara_type.Logical };
        }

        public static vara Logical(int Logical, int Ssa, Assembler.CliType DataType)
        {
            return new vara { log = Logical, ssa = Ssa, ct = DataType, type = vara_type.Logical };
        }

        public static vara AddrOf(int Logical, long Offset)
        {
            return new vara { log = Logical, ct = Assembler.CliType.native_int, type = vara_type.AddrOf, offset = Offset };
        }

        public static vara AddrOf(int Logical)
        {
            return AddrOf(Logical, 0);
        }

        public static vara AddrOf(vara v, long Offset)
        {
            return new vara { log = v.log, ssa = v.ssa, ct = Assembler.CliType.native_int, type = vara_type.AddrOf, offset = Offset };
        }

        public static vara AddrOf(vara v)
        {
            return AddrOf(v, 0);
        }

        public static vara ContentsOf(int Logical, int SSA, long Offset, Assembler.CliType DataType)
        {
            return new vara { log = Logical, ssa = SSA, ct = DataType, type = vara_type.ContentsOf, offset = Offset };
        }

        public static vara ContentsOf(int Logical, long Offset, Assembler.CliType DataType)
        {
            return ContentsOf(Logical, 0, Offset, DataType);
        }

        public static vara ContentsOf(int Logical, Assembler.CliType DataType)
        {
            return ContentsOf(Logical, 0, 0, DataType);
        }

        public static vara ContentsOf(vara v, long Offset, Assembler.CliType DataType)
        {
            return ContentsOf(v.log, v.ssa, Offset, DataType);
        }

        public static vara ContentsOf(vara v, Assembler.CliType DataType)
        {
            return ContentsOf(v.log, v.ssa, 0, DataType);
        }

        public static vara Label(string Label, long Offset, bool is_object)
        {
            return new vara { label = Label, ct = Assembler.CliType.native_int, type = vara_type.Label, offset = Offset, is_object = is_object };
        }

        public static vara Label(string Label, bool is_object)
        {
            return vara.Label(Label, 0, is_object);
        }

        public static vara Const(object Const, Assembler.CliType DataType)
        {
            return new vara { c = Const, ct = DataType, type = vara_type.Const };
        }

        public static vara Const(object Const)
        {
            /* Attempt to guess data type */
            Assembler.CliType dt = Assembler.CliType.void_;

            if (Const is int)
            {
                if (Assembler.FitsInt32((int)Const))
                    dt = Assembler.CliType.int32;
                else
                    dt = Assembler.CliType.int64;
            }

            return new vara { c = Const, ct = dt, type = vara_type.Const };
        }

        public static vara MachineReg(libasm.hardware_location reg)
        {
            return new vara { mreg = reg, type = vara_type.MachineReg, DataType = Assembler.CliType.void_ };
        }

        public static vara MachineReg(libasm.hardware_location reg, Assembler.CliType dt)
        {
            return new vara { mreg = reg, type = vara_type.MachineReg, DataType = dt };
        }

        public int LogicalVar { get { return log; } }
        public libasm.hardware_location MachineRegVal { get { return mreg; } }
        public long Offset { get { return offset; } }
        public object ConstVal { get { return c; } }
        public vara_type VarType { get { return type; } }
        public Assembler.CliType DataType { get { return ct; } set { ct = value; } }
        public string LabelVal { get { return label; } }
        public int SSA { get { return ssa; } set { ssa = value; } }

        public override string ToString()
        {
            switch (type)
            {
                case vara_type.Void:
                    return "<void>";

                case vara_type.Logical:
                    return log.ToString() + SSAString;

                case vara_type.AddrOf:
                    if (offset == 0)
                        return "&" + log.ToString() + SSAString;
                    else if (offset > 0)
                        return "&" + log.ToString() + SSAString + " + " + offset.ToString();
                    else
                        return "&" + log.ToString() + SSAString + " - " + (-offset).ToString();

                case vara_type.ContentsOf:
                    if (offset == 0)
                        return "*" + log.ToString() + SSAString;
                    else if (offset > 0)
                        return "*(" + log.ToString() + SSAString + " + " + offset.ToString() + ")";
                    else
                        return "*(" + log.ToString() + SSAString + " - " + (-offset).ToString() + ")";

                case vara_type.Label:
                    if (offset == 0)
                        return "&" + label;
                    else if (offset > 0)
                        return "&" + label + " + " + offset.ToString();
                    else
                        return "&" + label + " - " + (-offset).ToString();

                case vara_type.Const:
                    if (c == null)
                        return "$null";
                    return "$" + c.ToString();

                case vara_type.MachineReg:
                    return mreg.ToString().ToUpper();

                default:
                    return "<invalid>";
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is vara))
                return false;
            vara other = (vara)obj;
            if (log != other.log)
                return false;
            if (type != other.type)
                return false;
            //if (ct != other.ct)
            //    return false;
            if (c != other.c)
                return false;
            if (offset != other.offset)
                return false;
            if (label != other.label)
                return false;
            if (ssa != other.ssa)
                return false;
            if(mreg != null && !mreg.Equals(other.mreg))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hc = log.GetHashCode();
                hc <<= 5;
                hc ^= type.GetHashCode();
                hc <<= 5;
                hc ^= offset.GetHashCode();
                //hc <<= 5;
                //hc ^= ct.GetHashCode();
                hc <<= 5;
                hc ^= ssa.GetHashCode();
                if (label != null)
                {
                    hc <<= 5;
                    hc ^= label.GetHashCode();
                }
                if (c != null)
                {
                    hc <<= 5;
                    hc ^= c.GetHashCode();
                }
                if (mreg != null)
                {
                    hc <<= 5;
                    hc ^= mreg.GetHashCode();
                }
                return hc;
            }
        }

        private string SSAString
        {
            get
            {
                if (ssa == 0)
                    return "";
                else
                    return "." + ssa.ToString();
            }
        }
    }
}
