using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila5.target
{
    partial class Target
    {
        public void AllocateLocalVarsArgs(Code c)
        {
            /* Generate list of locations of local vars */
            var m = c.ms.m;
            var ms = c.ms;
            int idx = c.lvar_sig_tok;
            int lv_count = m.GetLocalVarCount(ref idx);

            int[] lv_locs = new int[lv_count];
            c.lv_locs = new Reg[lv_count];
            c.lv_sizes = new int[lv_count];
            c.lv_types = new metadata.TypeSpec[lv_count];
            int cur_loc = 0;
            for (int i = 0; i < lv_count; i++)
            {
                var type = m.GetTypeSpec(ref idx, c.ms.gtparams,
                    c.ms.gmparams);
                int t_size = GetSize(type);
                lv_locs[i] = cur_loc;

                t_size = util.util.align(t_size, GetPointerSize());
                c.lv_sizes[i] = t_size;

                c.lv_types[i] = type;
                c.lv_locs[i] = GetLVLocation(cur_loc, t_size, c);

                cur_loc += t_size;

                // align to pointer size
                int diff = cur_loc % GetPointerSize();
                if (diff != 0)
                    cur_loc = cur_loc - diff + GetPointerSize();
            }
            c.lv_total_size = cur_loc;

            /* Do the same for local args */
            int la_count = m.GetMethodDefSigParamCountIncludeThis(
                c.ms.msig);
            int[] la_locs = new int[la_count];
            c.la_locs = new Reg[la_count];
            c.la_needs_assign = new bool[la_count];
            int la_count2 = m.GetMethodDefSigParamCount(
                c.ms.msig);
            int laidx = 0;
            cur_loc = 0;

            var cc = cc_map["sysv"];
            int stack_loc = 0;
            var la_phys_locs = GetRegLocs(new ir.Param
            {
                m = m,
                ms = c.ms,
            }, ref stack_loc, cc,
            out c.la_sizes, out c.la_types);
            c.incoming_args = la_phys_locs;

            if (la_count != la_count2)
            {
                var this_size = GetCTSize(ir.Opcode.ct_object);
                c.la_locs[laidx] = GetLALocation(cur_loc, this_size, c);
                c.la_sizes[laidx] = this_size;

                // value type methods have mptr to type as their this pointer
                if (ms.type.IsValueType)
                {
                    c.la_types[laidx] = ms.type.ManagedPointer;
                }
                else
                    c.la_types[laidx] = ms.type;

                la_locs[laidx] = cur_loc;
                cur_loc += this_size;

                laidx++;
            }
            idx = m.GetMethodDefSigRetTypeIndex(
                ms.msig);
            // pass by rettype
            m.GetTypeSpec(ref idx, c.ms.gtparams, c.ms.gmparams);

            for (int i = 0; i < la_count; i++)
            {
                var mreg = la_phys_locs[i];

                if (mreg.type == rt_stack)
                {
                    la_phys_locs[i] = GetLALocation(mreg.stack_loc, util.util.align(c.la_sizes[i], GetPointerSize()), c);
                    c.la_locs[i] = la_phys_locs[i];
                    c.la_needs_assign[i] = false;
                }
                else
                {
                    throw new NotImplementedException();
                    var type = m.GetTypeSpec(ref idx, c.ms.gtparams, c.ms.gmparams);
                    var la_size = GetSize(type);
                    la_locs[laidx++] = cur_loc;
                    cur_loc += la_size;
                }
            }

        }
    }
}
