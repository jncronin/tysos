/* Copyright (C) 2016 by John Cronin
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
using System.Text;
using libtysila5.ir;

namespace libtysila5.target
{
    public abstract partial class Target
    {
        public static Dictionary<string, Target> targets =
            new Dictionary<string, Target>(
                new libtysila5.GenericEqualityComparer<string>());

        public static Dictionary<int, string> pt_names
            = new Dictionary<int, string>(
                new GenericEqualityComparer<int>());

        public static Dictionary<int, string> rt_map
           = new Dictionary<int, string>(
               new GenericEqualityComparer<int>());

        protected static Dictionary<int, string> insts =
            new Dictionary<int, string>(
                new GenericEqualityComparer<int>());

        public Dictionary<string, ulong> cc_callee_preserves_map
            = new Dictionary<string, ulong>(
                new GenericEqualityComparer<string>());
        public Dictionary<string, ulong> cc_caller_preserves_map
            = new Dictionary<string, ulong>(
                new GenericEqualityComparer<string>());
        public Dictionary<string, Dictionary<int, int[]>> cc_map
            = new Dictionary<string, Dictionary<int, int[]>>(
                new GenericEqualityComparer<string>());
        public Dictionary<string, Dictionary<int, int[]>> retcc_map
            = new Dictionary<string, Dictionary<int, int[]>>(
                new GenericEqualityComparer<string>());

        protected internal abstract int GetCondCode(MCInst i);
        protected internal abstract Reg GetMoveSrc(MCInst i);
        protected internal abstract Reg GetMoveDest(MCInst i);
        protected internal abstract bool IsBranch(MCInst i);
        protected internal abstract bool IsCall(MCInst i);
        protected internal abstract int GetBranchDest(MCInst i);
        protected internal abstract void SetBranchDest(MCInst i, int d);
        protected internal abstract Reg GetLVLocation(int lv_loc, int lv_size);
        protected internal abstract MCInst[] SetupStack(int lv_size);
        protected internal abstract MCInst[] CreateMove(Reg src, Reg dest);
        protected internal abstract binary_library.IRelocationType GetDataToDataReloc();

        protected internal virtual bool HasSideEffects(MCInst i)
        { return IsCall(i); }

        public binary_library.IBinaryFile bf;
        public binary_library.ISection text_section;
        public StringTable st;
        public Requestor r;

        protected internal virtual bool NeedsMregLiveness(MCInst i)
        {
            if (i.p.Length == 0)
                return false;
            if (i.p[0].t == ir.Opcode.vl_str)
            {
                switch (i.p[0].v)
                {
                    case Generic.g_precall:
                    case Generic.g_postcall:
                        return true;
                }
            }
            return false;
        }

        protected internal abstract MCInst SaveRegister(Reg r);
        protected internal abstract MCInst RestoreRegister(Reg r);

        public string name;
        public int ptype;
        public HashTable instrs;
        public Reg[] regs;

        /** <summary>Return a hardware register that can be used for
                a particular stack location, or -1 if the actual
                stack should be used.</summary>*/
        public virtual int GetRegStackLoc(int stack_loc, int ct) { return -1; }

        static Target()
        {
            init_targets();
            init_pt();
            init_rtmap();
        }

        public class ContentsReg : Reg, IEquatable<Reg>
        {
            public Reg basereg;
            public long disp;

            public ContentsReg()
            {
                type = rt_contents;
            }

            public override string ToString()
            {
                if (disp == 0)
                    return "[" + basereg.ToString() + "]";
                else
                    return "[" + basereg.ToString() + " + " + disp.ToString() + "]";
            }

            public override bool Equals(Reg other)
            {
                var cr = other as ContentsReg;
                if (cr == null)
                    return false;
                if (basereg.Equals(cr.basereg) == false)
                    return false;
                return disp == cr.disp;
            }
        }

        public class AddrAndContentsReg : Reg, IEquatable<Reg>
        {
            public Reg Addr, Contents;

            public override bool Equals(Reg other)
            {
                var acr = other as AddrAndContentsReg;
                if (acr == null)
                    return false;
                if (Addr.Equals(acr.Addr) == false)
                    return false;
                return Contents.Equals(acr.Contents);
            }
        }

        public class Reg : IEquatable<Reg>
        {
            public string name;
            public int id;
            public int type;
            public int size;
            public ulong mask;
            public int stack_loc;

            public override string ToString()
            {
                if (name == "stack")
                    return "stack(" + stack_loc.ToString() + ")";
                else
                    return name;
            }

            public virtual bool Equals(Reg other)
            {
                if (other.type != type)
                    return false;
                if (other.size != size)
                    return false;
                if (other.id != id)
                    return false;
                if (other.stack_loc != stack_loc)
                    return false;
                return true;
            }

            public static implicit operator Param(Reg r)
            {
                return new Param { t = Opcode.vl_mreg, mreg = r };
            }
        }

        protected virtual Reg GetRegLoc(ir.Param csite,
            ref int stack_loc,
            int cc_next,
            int ct,
            metadata.TypeSpec ts)
        {
            throw new NotSupportedException("Architecture does not support ct: " + Opcode.ct_names[ct]);
        }

        internal int GetSize(metadata.TypeSpec ts)
        {
            switch(ts.stype)
            {
                case metadata.TypeSpec.SpecialType.None:
                    if(ts.m.is_corlib)
                    {
                        var simple = ts.m.simple_type_idx[ts.tdrow];
                        if (simple != -1)
                            return GetCTSize(ir.Opcode.GetCTFromType(simple));
                    }
                    if (ts.IsValueType())
                    {
                        return layout.Layout.GetTypeSize(ts, this, false);
                    }
                    return GetCTSize(Opcode.ct_object);

                default:
                    return GetCTSize(Opcode.ct_object);
            }
        }

        internal int GetSize(int ptype, uint token)
        {
            throw new NotSupportedException();
            if (ptype == 0x11)
                throw new NotImplementedException();
            else
            {
                return GetCTSize(ir.Opcode.GetCTFromType(ptype));
            }
        }

        protected internal virtual int GetPointerSize()
        {
            return GetCTSize(ptype);
        }

        internal int GetCTSize(int ct)
        {
            switch (ct)
            {
                case ir.Opcode.ct_int32:
                    return 4;
                case ir.Opcode.ct_int64:
                    return 8;
                case ir.Opcode.ct_intptr:
                case ir.Opcode.ct_object:
                case ir.Opcode.ct_ref:
                    return GetCTSize(ptype);
                case ir.Opcode.ct_float:
                    return 8;
                default:
                    throw new NotSupportedException();
            }
        }

        protected internal virtual Reg[] GetRegLocs(ir.Param csite,
            ref int stack_loc,
            Dictionary<int, int[]> cc)
        {
            var m = csite.m;
            var idx = (int)csite.v2;
            metadata.TypeSpec[] gtparams = null;
            metadata.TypeSpec[] gmparams = null;
            if (csite.ms != null)
            {
                idx = csite.ms.msig;
                m = csite.ms.m;
                gtparams = csite.ms.gtparams;
                gmparams = csite.ms.gmparams;
            }
            Dictionary<int, int> cc_next = new Dictionary<int, int>();

            var pcount = m.GetMethodDefSigParamCountIncludeThis(idx);
            idx = m.GetMethodDefSigRetTypeIndex(idx);

            // Skip rettype
            bool is_req;
            uint token;
            while (m.GetRetTypeCustomMod(ref idx, out is_req, out token)) ;
            m.GetTypeSpec(ref idx, gtparams, gmparams);

            // Read types of parameters
            Target.Reg[] ret = new Target.Reg[pcount];
            for (int i = 0; i < pcount; i++)
            {
                while (m.GetRetTypeCustomMod(ref idx, out is_req, out token)) ;
                var v = m.GetTypeSpec(ref idx, gtparams, gmparams);

                var ct = ir.Opcode.GetCTFromType(v);
                Reg r = null;

                int cur_cc_next;
                if(cc_next.TryGetValue(ct, out cur_cc_next) == false)
                    cur_cc_next = 0;

                int[] cc_map;
                if (cc.TryGetValue(ct, out cc_map))
                {
                    if (cur_cc_next >= cc_map.Length)
                        cur_cc_next = cc_map.Length - 1;

                    var reg_id = cc_map[cur_cc_next];
                    if (regs[reg_id].type == rt_stack)
                    {
                        var size = GetSize(v);
                        Reg rstack = new Reg()
                        {
                            type = rt_stack,
                            id = regs[reg_id].id,
                            size = size,
                            stack_loc = stack_loc,
                            name = regs[reg_id].name,
                            mask = regs[reg_id].mask
                        };
                        stack_loc += size;

                        var diff = stack_loc % size;
                        if (diff != 0)
                            stack_loc = stack_loc + size - diff;
                        r = rstack;
                    }
                    else
                        r = regs[reg_id];
                }
                else
                {
                    r = GetRegLoc(csite, ref stack_loc,
                        cur_cc_next, ct, v);

                }
                cc_next[ct] = cur_cc_next + 1;

                ret[i] = r;
            }

            return ret;
        }
    }
}
