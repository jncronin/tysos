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

namespace libasm
{
    public struct var_semantic
    {
        public bool needs_memloc;
        public bool needs_float32;
        public bool needs_float64;
        public bool needs_float { get { return needs_float32 | needs_float64; } }
        public bool needs_int32;
        public bool needs_int64;
        public bool needs_intptr;
        public bool needs_integer { get { return needs_int32 | needs_int64 | needs_intptr; } }
        public bool needs_vtype;
        public bool needs_virtftnptr;
        public int vtype_size;

        public void Merge(var_semantic semantic)
        {
            needs_memloc |= semantic.needs_memloc;
            needs_float32 |= semantic.needs_float32;
            needs_float64 |= semantic.needs_float64;
            needs_int32 |= semantic.needs_int32;
            needs_int64 |= semantic.needs_int64;
            needs_intptr |= semantic.needs_intptr;
            needs_memloc |= semantic.needs_memloc;
            needs_vtype |= semantic.needs_vtype;
            if (semantic.vtype_size > vtype_size)
                vtype_size = semantic.vtype_size;
        }
    }

    public class register : hardware_location { }

    public class hardware_location : IEquatable<hardware_location>
    {
        #region IEquatable<hardware_location> Members

        public virtual bool Equals(hardware_location other)
        {
            throw new NotImplementedException();
        }

        public virtual bool CanTakeAddressOf { get { return false; } }
        public virtual var_semantic GetSemantic() { return new var_semantic(); }

        #endregion

        public static implicit operator hardware_location(int i)
        { return new const_location { c = i }; }
    }

    public class multiple_hardware_location : hardware_location
    {
        public hardware_location[] hlocs;

        public multiple_hardware_location() { }
        public multiple_hardware_location(params hardware_location[] locs) { hlocs = locs; }

        public override string ToString()
        {
            if (hlocs == null)
                return ("{ Uninitialized }");

            StringBuilder sb = new StringBuilder();
            sb.Append("{ ");
            for (int i = 0; i < hlocs.Length; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(hlocs[i].ToString());
            }
            sb.Append(" }");
            return sb.ToString();
        }

        public override bool Equals(hardware_location other)
        {
            if (!(other is multiple_hardware_location))
                return false;
            multiple_hardware_location mhl = other as multiple_hardware_location;
            if (mhl.hlocs == null)
            {
                if (hlocs == null)
                    return true;
                return false;
            }
            if (hlocs == null)
                return false;

            if (hlocs.Length != mhl.hlocs.Length)
                return false;

            if (hlocs.Length == 0)
                return true;

            if (!hlocs[0].GetType().Equals(mhl.hlocs[0].GetType()))
                return false;

            for (int i = 0; i < hlocs.Length; i++)
            {
                if (!hlocs[i].Equals(mhl.hlocs[i]))
                    return false;
            }
            return true;
        }

        public override bool CanTakeAddressOf
        {
            get
            {
                foreach (hardware_location hloc in hlocs)
                {
                    if (!hloc.CanTakeAddressOf)
                        return false;
                }
                return true;
            }
        }

        public libasm.hardware_location this[int i]
        {
            get { return hlocs[i]; }
            set { hlocs[i] = value; }
        }
    }

    public class const_location : hardware_location
    {
        public object c;
        public override bool Equals(hardware_location other)
        {
            if (!(other is const_location))
                return false;
            return ((const_location)other).c.Equals(c);
        }

        public override bool CanTakeAddressOf
        {
            get
            {
                return false;
            }
        }

        public static implicit operator const_location(int i)
        { return new const_location { c = i }; }
    }

    public struct hloc_constraint
    {
        public enum c_ { None, Any, AnyOfType, Specific, Immediate, Operand1, Operand2, List, AnyOrNone };
        public c_ constraint;
        public hardware_location specific;
        public List<hloc_constraint> specific_list;

        public int const_bitsize;

        public bool IsSpecific { get { if (constraint == c_.Specific) return true; return false; } }
        public bool IsSpecificOrConst { get { if ((constraint == c_.Specific) || (constraint == c_.Immediate)) return true; return false; } }

        public static hloc_constraint Specific(hardware_location hloc)
        {
            if (hloc == null)
                return new hloc_constraint { constraint = c_.None };

            return new hloc_constraint
            {
                constraint = c_.Specific,
                specific = hloc
            };
        }

        public static bool SpecificCompare(hloc_constraint a, hloc_constraint b)
        {
            if ((a.constraint == c_.Specific) && (b.constraint == c_.Specific))
                return a.specific.Equals(b.specific);

            return false;
        }

        public static bool IsAssignableTo(hloc_constraint dest, hloc_constraint src)
        {
            if (dest.constraint == c_.Any)
                return true;
            if ((dest.constraint == c_.None) && (src.constraint == c_.None))
                return true;
            if ((dest.constraint == c_.Immediate) && (src.constraint == c_.Immediate))
                return true;
            if ((dest.constraint == c_.Specific) && (src.constraint == c_.Specific))
            {
                if (SpecificCompare(src, dest))
                    return true;
                return false;
            }

            if (dest.constraint == c_.List)
            {
                foreach (hloc_constraint d_item in dest.specific_list)
                {
                    if (IsAssignableTo(d_item, src))
                        return true;
                }
                return false;
            }

            if (dest.constraint == c_.AnyOfType)
            {
                if (src.specific.GetType() == dest.specific.GetType())
                    return true;
                return false;
            }

            if (dest.constraint != src.constraint)
                return false;

            throw new NotImplementedException();
        }

        public override string ToString()
        {
            if (constraint == c_.Specific)
                return specific.ToString();
            else if (constraint == c_.AnyOfType)
                return constraint.ToString() + "(" + specific.ToString() + ")";
            else if (constraint == c_.List)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < specific_list.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(specific_list[i].ToString());
                }
                sb.Append("]");
                return sb.ToString();
            }
            else
                return constraint.ToString();
        }
    }

    public class hardware_stackloc : hardware_location
    {
        public int loc;

        public int size;

        public int offset_within_loc;

        public object container;

        public enum StackType { Var, Arg, LocalVar };
        public StackType stack_type;

        public override bool Equals(hardware_location other)
        {
            if (!(other is hardware_stackloc))
                return false;
            if (loc != ((hardware_stackloc)other).loc)
                return false;
            if (container != ((hardware_stackloc)other).container)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return loc.GetHashCode() ^ (size.GetHashCode() << 3) ^ (offset_within_loc.GetHashCode() << 6) ^ (((container == null) ? 0 : container.GetHashCode()) << 9) ^ (stack_type.GetHashCode() << 12);
        }

        public override string ToString()
        {
            return "stack_" + stack_type.ToString() + "(" + loc.ToString() + ")" + ((offset_within_loc == 0) ? "" : (" + " + offset_within_loc.ToString()));
        }

        public override bool CanTakeAddressOf
        {
            get
            {
                return true;
            }
        }
    }

    public class hardware_contentsof : hardware_location
    {
        public hardware_location base_loc;
        public int const_offset;
        public int size;

        public override bool Equals(hardware_location other)
        {
            if (!(other is hardware_contentsof))
                return false;
            if (const_offset != ((hardware_contentsof)other).const_offset)
                return false;
            if (!base_loc.Equals(((hardware_contentsof)other).base_loc))
                return false;
            return true;
        }

        public override string ToString()
        {
            return "[" + base_loc.ToString() + ((const_offset != 0) ? (" + $" + const_offset.ToString()) : "") + "]";
        }

        public override int GetHashCode()
        {
            return 0x08080808 ^ base_loc.GetHashCode() ^ const_offset.GetHashCode();
        }
    }

    public class hardware_addressof : hardware_location
    {
        public hardware_location base_loc;

        public override bool Equals(hardware_location other)
        {
            if (!(other is hardware_addressof))
                return false;
            if (!base_loc.Equals(((hardware_addressof)other).base_loc))
                return false;
            return true;
        }

        public override string ToString()
        {
            return "&" + base_loc.ToString();
        }

        public override int GetHashCode()
        {
            return 0x0f0f0f0f ^ base_loc.GetHashCode();
        }
    }

    public class hardware_addressoflabel : hardware_location
    {
        public string label;
        public long const_offset;
        public bool is_object;

        public hardware_addressoflabel(string Label, bool IsObject) { label = Label; const_offset = 0; is_object = IsObject; }
        public hardware_addressoflabel(string Label, int Offset, bool IsObject) { label = Label; const_offset = Offset; is_object = IsObject; }
        public hardware_addressoflabel(string Label, long Offset, bool IsObject) { label = Label; const_offset = Offset; is_object = IsObject; }

        public override bool Equals(hardware_location other)
        {
            if (!(other is hardware_addressoflabel))
                return false;

            hardware_addressoflabel o = other as hardware_addressoflabel;
            if (!label.Equals(o.label))
                return false;
            if (const_offset != o.const_offset)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return label.GetHashCode() ^ const_offset.GetHashCode();
        }

        public override string ToString()
        {
            if (const_offset == 0)
                return "&" + label;

            return "&(" + label + " + " + const_offset.ToString() + ")";
        }
    }
}
