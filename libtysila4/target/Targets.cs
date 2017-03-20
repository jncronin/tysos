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
using libtysila4.ir;

namespace libtysila4.target
{
    public abstract partial class Target
    {
        public static Dictionary<string, Target> targets =
            new Dictionary<string, Target>(
                new libtysila4.GenericEqualityComparer<string>());

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
        protected internal abstract bool IsMoveVreg(MCInst i);
        protected internal abstract bool IsMoveMreg(MCInst i);
        protected internal abstract ir.Param GetMoveSrc(MCInst i);
        protected internal abstract ir.Param GetMoveDest(MCInst i);
        protected internal abstract bool IsBranch(MCInst i);
        protected internal abstract bool IsCall(MCInst i);
        protected internal abstract ir.Param GetBranchDest(MCInst i);
        protected internal abstract void SetBranchDest(MCInst i, ir.Param d);
        protected internal abstract Reg GetLVLocation(int lv_loc, int lv_size);
        protected internal abstract MCInst[] SetupStack(int lv_size);
        protected internal abstract MCInst[] CreateMove(Reg src, Reg dest);
        protected internal abstract MCInst[] CreateMove(Param src, Param dest);
        protected internal abstract binary_library.IRelocationType GetDataToDataReloc();

        protected internal virtual bool HasSideEffects(MCInst i)
        { return IsCall(i); }

        public binary_library.IBinaryFile bf;
        public binary_library.ISection text_section;
        public StringTable st;
        public Requestor r;

        public virtual IEnumerable<graph.Graph.PassDelegate> GetOutputMCPasses()
        {
            return new graph.Graph.PassDelegate[0];
        }

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

        public static graph.Graph MCLowerPass(graph.Graph input, Target t)
        {
            // Lower graph nodes to linear machine code
            graph.Graph ret = new graph.Graph();
            var ig = input as ir.IrGraph;

            ret.cg = ig.cg;
            ret.ms = ig.ms;
            ret.uses = ig.uses;
            ret.defs = ig.defs;
            ret.lvars_for_simplifying = ig.lvars_for_simplifying;

            foreach (var ir_node in ig.LinearStream)
            {
                var o = ir_node.c as ir.Opcode;
                foreach(var oc in o.all_insts)
                    t.MCLower(oc as ir.Opcode, ref input.next_vreg_id);
            }

            /* Return a graph with basic blocks as nodes */
            List<MCNode> bbs = new List<MCNode>();
            for(int i = 0; i < ig.blocks.Count; i++)
            {
                MCNode cur_node = new MCNode();
                cur_node.locals = new List<ir.Param>();
                cur_node.defs = new List<ir.Param>();
                cur_node.uses = new List<ir.Param>();
                cur_node.insts = new List<MCInst>();
                    
                cur_node.bb_idx = i;

                foreach(var n in ig.blocks[i])
                {
                    var o = n.c as ir.Opcode;

                    foreach (var oc in o.all_insts)
                    {
                        cur_node.insts.AddRange(oc.mcinsts);
                        if (oc.uses != null)
                            cur_node.uses.AddRange(oc.uses);
                        if (oc.defs != null)
                            cur_node.defs.AddRange(oc.defs);
                    }
                }

                /*foreach(var use in cur_node.uses)
                {
                    if (cur_node.defs.Contains(use))
                        cur_node.locals.Add(use);
                }*/

                bbs.Add(cur_node);
            }

            ret.bbs_after = ig.bbs_after;
            ret.bbs_before = ig.bbs_before;

            ret.LinearStream = new List<graph.BaseNode>();
            for(int i = 0; i < bbs.Count; i++)
            {
                var n = new graph.MultiNode();
                n.c = bbs[i];
                n.bb = i;
                ret.LinearStream.Add(n);
                ret.bb_starts.Add(n);
                ret.bb_ends.Add(n);
            }

            for(int i = 0; i < bbs.Count; i++)
            {
                foreach (var next in ig.bbs_after[i])
                    ret.LinearStream[i].AddNext(ret.LinearStream[next]);
                foreach (var prev in ig.bbs_before[i])
                    ret.LinearStream[i].AddPrev(ret.LinearStream[prev]);
            }
            for(int i = 0; i < ig.Starts.Count; i++)
            {
                var start = ig.Starts[i];
                ret.Starts.Add(ret.LinearStream[start.bb]);
            }
            for (int i = 0; i < ig.Ends.Count; i++)
            {
                var end = ig.Ends[i];
                ret.Ends.Add(ret.LinearStream[end.bb]);
            }

            ret.next_vreg_id = input.next_vreg_id;

            return ret;
        }

        protected virtual void MCLower(ir.Opcode irnode, ref int next_temp_reg)
        {
            /* Base method is to use the instrs hash

                If more complicated instruction lowering is required,
                override this function in derived classes, but still
                call the base method to fall back to the default
                behaviour */

            List<int> next_vreg_ids = null;

            // Build a test key
            List<byte> test = new List<byte>();
            HashTable.CompressInt(irnode.oc, test);
            if (irnode.uses == null)
                HashTable.CompressInt(0, test);
            else
            {
                HashTable.CompressInt(irnode.uses.Length, test);
                foreach (var use in irnode.uses)
                    HashTable.CompressInt(use.DecoratedType(this), test);
            }
            if (irnode.defs == null)
                HashTable.CompressInt(0, test);
            else
            {
                HashTable.CompressInt(irnode.defs.Length, test);
                foreach (var def in irnode.defs)
                    HashTable.CompressInt(def.DecoratedType(this), test);
            }

            var bi = instrs.GetBlobIndex(test);
            if(bi != -1)
            {
                var vi = instrs.GetValueIndex(bi);
                var impl_lines = instrs.ReadCompressedUInt(ref vi);

                if(irnode.mcinsts == null)
                    irnode.mcinsts = new List<MCInst>();
                for(int line = 0; line < impl_lines; line++)
                {
                    MCInst mci = new MCInst();

                    var pcount = instrs.ReadCompressedUInt(ref vi);

                    mci.p = new ir.Param[pcount];
                    for(int p = 0; p < pcount; p++)
                    {
                        var pt = instrs.ReadCompressedUInt(ref vi);
                        var pv = instrs.ReadCompressedUInt(ref vi);

                        switch(pt)
                        {
                            case pt_mc:
                                mci.p[p] = new ir.Param { t = ir.Opcode.vl_str, v = pv, str = insts[(int)pv] };
                                break;
                            case pt_use:
                                mci.p[p] = irnode.uses[pv];
                                break;
                            case pt_def:
                                mci.p[p] = irnode.defs[pv];
                                break;
                            case pt_cc:
                                mci.p[p] = new ir.Param { t = ir.Opcode.vl_cc, v = irnode.cc };
                                break;
                            case pt_icc:
                                mci.p[p] = new ir.Param { t = ir.Opcode.vl_cc, v = ir.Opcode.cc_invert_map[irnode.cc] };
                                break;
                            case pt_br:
                                var dest_block = irnode.n.Next.GetEnumerator();
                                dest_block.MoveNext();
                                var c = pv;
                                while (c > 0)
                                {
                                    dest_block.MoveNext();
                                    c--;
                                }
                                mci.p[p] = new ir.Param { t = ir.Opcode.vl_br_target, v = dest_block.Current.bb };
                                break;
                            case pt_tu:
                            case pt_td:
                                if (next_vreg_ids == null)
                                    next_vreg_ids = new List<int>();
                                while (next_vreg_ids.Count <= (int)pv)
                                    next_vreg_ids.Add(next_temp_reg++);
                                var vreg_id = next_vreg_ids[(int)pv];
                                if(pt == pt_tu)
                                {
                                    var tmpreg = new ir.Param { t = ir.Opcode.vl_stack, ssa_idx = vreg_id, ud = ir.Param.UseDefType.Use };
                                    var new_uses = new List<ir.Param>(irnode.uses);
                                    new_uses.Add(tmpreg);
                                    irnode.uses = new_uses.ToArray();
                                    mci.p[p] = tmpreg;
                                }
                                else
                                {
                                    var tmpreg = new ir.Param { t = ir.Opcode.vl_stack, ssa_idx = vreg_id, ud = ir.Param.UseDefType.Def };
                                    var new_defs = new List<ir.Param>(irnode.defs);
                                    new_defs.Add(tmpreg);
                                    irnode.defs = new_defs.ToArray();
                                    mci.p[p] = tmpreg;
                                }
                                break;
                            default:
                                throw new NotSupportedException("Invalid param type " + pt.ToString());
                        }
                    }

                    irnode.mcinsts.Add(mci);
                }

                irnode.is_mc = true;
                return;
            }
            /* Special case some object instructions we can rewrite a bit */
            switch (irnode.oc)
            {
                case ir.Opcode.oc_ldstr:
                    if (LowerLdStr(irnode, ref next_temp_reg))
                        return;
                    break;

                case ir.Opcode.oc_ldlabaddr:
                    if (LowerLdLabAddr(irnode, ref next_temp_reg))
                        return;
                    break;

                case ir.Opcode.oc_enter:
                    if (irnode.mcinsts == null)
                        irnode.mcinsts = new List<MCInst>();
                    irnode.mcinsts.Add(new MCInst { p = new ir.Param[] { new ir.Param { t = ir.Opcode.vl_str, v = Generic.g_setupstack } } });
                    irnode.mcinsts.Add(new MCInst { p = new ir.Param[] { new ir.Param { t = ir.Opcode.vl_str, v = Generic.g_savecalleepreserves } } });
                    irnode.is_mc = true;
                    return;

                case Opcode.oc_pop:
                    irnode.is_mc = true;
                    if (irnode.mcinsts == null)
                        irnode.mcinsts = new List<MCInst>();
                    return;
            }
            throw new Exception("Unable to lower " + irnode.ToString());
        }

        private bool LowerLdLabAddr(Opcode irnode, ref int next_temp_reg)
        {
            var offset = irnode.uses[0].v;
            var lab = irnode.uses[0].str;

            irnode.mcinsts = new List<MCInst>();
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "loadaddress", v = Generic.g_loadaddress },
                    irnode.defs[0],
                    new Param { t = Opcode.vl_c, str = lab, v = offset },
                }
            });

            return true;
        }


        private bool LowerLdStr(Opcode irnode, ref int next_temp_reg)
        {
            var str_offset = irnode.uses[0].v;
            var m = irnode.uses[0].m;
            var strtab_lab = st.GetStringTableName();

            // Ensure top byte is 0x70
            if ((str_offset & 0xff000000U) != 0x70000000U)
                throw new Exception("Invalid string token: " + str_offset.ToString());
            str_offset &= 0x00ffffffU;

            // Get offset within string table
            var st_offset = st.GetStringAddress(m.GetUserString((int)str_offset),
                this);

            irnode.mcinsts = new List<MCInst>();
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "loadaddress", v = Generic.g_loadaddress },
                    irnode.defs[0],
                    new Param { t = Opcode.vl_c, str = strtab_lab, v = st_offset },
                }
            });

            return true;
        }

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
                    if (ts.IsValueType)
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
