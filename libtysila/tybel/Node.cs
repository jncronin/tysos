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

namespace libtysila.tybel
{
    public abstract class Node : timple.BaseNode
    {
        public IList<timple.BaseNode> next = new List<timple.BaseNode>();
        public IList<timple.BaseNode> prev = new List<timple.BaseNode>();
        public override IList<timple.BaseNode> Next { get { return next; } set { next = value; } }
        public override IList<timple.BaseNode> Prev { get { return prev; } set { prev = value; } }
        public timple.TreeNode TimpleInst;
        public virtual IList<vara> VarList { get { return new vara[] { }; } set { throw new NotSupportedException(); } }

        public abstract IEnumerable<libasm.OutputBlock> Assemble(Assembler ass, Assembler.MethodAttributes attrs);

        public abstract bool IsMove { get; }
        public abstract bool IsUnconditionalJmp { get; }

        public override timple.BaseNode InsertAfter(timple.BaseNode new_node)
        {
            new_node.Prev.Clear();
            new_node.Next.Clear();
            new_node.Prev.Add(this);
            foreach (timple.BaseNode next in this.Next)
            {
                new_node.Next.Add(next);
                int idx = next.Prev.IndexOf(this);
                next.Prev[idx] = new_node;
            }

            this.Next.Clear();
            this.Next.Add(new_node);

            return new_node;
        }

        public timple.BaseNode InsertBefore(timple.BaseNode new_node)
        {
            new_node.Prev.Clear();
            new_node.Next.Clear();
            new_node.Next.Add(this);
            foreach (timple.BaseNode prev in this.Prev)
            {
                new_node.Prev.Add(prev);
                int idx = prev.Next.IndexOf(this);
                prev.Next[idx] = new_node;
            }

            this.Prev.Clear();
            this.Prev.Add(new_node);

            return new_node;
        }

        public override void Remove()
        {
            if (Next.Count > 1 && Prev.Count > 1)
                throw new Exception("Cannot remove node with more than one predecessors and successors");

            foreach (timple.BaseNode next in Next)
            {
                next.Prev.Remove(this);
                foreach (timple.BaseNode prev in Prev)
                    next.Prev.Add(prev);
            }

            foreach (timple.BaseNode prev in Prev)
            {
                prev.Next.Remove(this);
                foreach (timple.BaseNode next in Next)
                    prev.Next.Add(next);
            }
        }
    }

    public class EmptyNode : Node
    {
        public override IEnumerable<libasm.OutputBlock> Assemble(Assembler ass, Assembler.MethodAttributes attrs)
        {
            return new List<libasm.OutputBlock>();
        }

        public override bool IsMove
        {
            get { return false; }
        }

        public override bool IsUnconditionalJmp
        {
            get { return false; }
        }

        public override ICollection<vara> uses
        {
            get { return new List<vara>(); }
        }

        public override ICollection<vara> defs
        {
            get { return new List<vara>(); }
        }
    }

    public class MultipleNode : Node
    {
        public List<Node> Nodes;
        internal ICollection<vara> _uses, _defs;
        bool _ismove;

        public MultipleNode(List<Node> nodes, ICollection<vara> Uses, ICollection<vara> Defs)
        {
            Nodes = nodes;
            _uses = Uses;
            _defs = Defs;
            validate(ref _uses);
            validate(ref _defs);
            _ismove = false;
            UsesLiveAfter = true;
        }

        private void validate(ref ICollection<vara> a)
        {
            List<vara> new_a = new List<vara>(a);
            int i = 0;
            while (i < new_a.Count)
            {
                if (new_a[i].VarType == vara.vara_type.Logical ||
                    new_a[i].VarType == vara.vara_type.MachineReg)
                {
                    i++;
                    continue;
                }
                new_a.RemoveAt(i);
            }
            a = new_a;
        }

        public MultipleNode(List<Node> nodes, ICollection<vara> Uses, ICollection<vara> Defs, bool ismove)
        {
            Nodes = nodes;
            _uses = Uses;
            _defs = Defs;
            validate(ref _uses);
            validate(ref _defs);
            _ismove = ismove;
            UsesLiveAfter = true;
        }

        public override ICollection<vara> uses
        {
            get { return _uses; }
        }

        public override ICollection<vara> defs
        {
            get { return _defs; }
        }

        public override bool IsMove
        {
            get { return _ismove; }
        }

        public override bool IsUnconditionalJmp
        {
            get { return false; }
        }

        public override IEnumerable<libasm.OutputBlock> Assemble(Assembler ass, Assembler.MethodAttributes attrs)
        {
            List<libasm.OutputBlock> ret = new List<libasm.OutputBlock>();
            foreach (Node n in Nodes)
                ret.AddRange(n.Assemble(ass, attrs));
            return ret;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (i > 0)
                    sb.Append("\n  ");
                sb.Append(Nodes[i].ToString());
            }
            return sb.ToString();
        }
    }

    public class SpecialNode : Node
    {
        public enum SpecialNodeType { SaveLive, Restore, SaveLiveExcept, SaveLiveIntersect, SaveCalleeSaved, RestoreCalleeSaved };
        public SpecialNodeType Type;
        IList<vara> varList;
        public Node SaveNode;
        public override IList<vara> VarList
        {
            get { return varList; }
            set { varList = value; }
        }

        public int IntVal;

        public object Val;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Type.ToString());

            switch (Type)
            {
                case SpecialNodeType.SaveLiveExcept:
                case SpecialNodeType.SaveLiveIntersect:
                    sb.Append(" ");
                    for (int i = 0; i < VarList.Count; i++)
                    {
                        if (i != 0)
                            sb.Append(", ");
                        sb.Append(VarList[i].ToString());
                    }
                    break;
            }

            return sb.ToString();
        }

        public override ICollection<vara> defs
        {
            get { return new vara[] { }; }
        }

        public override ICollection<vara> uses
        {
            get { return new vara[] { }; }
        }

        public override bool IsMove
        {
            get { return false; }
        }

        public override bool IsUnconditionalJmp
        {
            get { return false; }
        }

        public override IEnumerable<libasm.OutputBlock> Assemble(Assembler ass, Assembler.MethodAttributes attrs)
        {
            switch (Type)
            {
                case SpecialNodeType.SaveCalleeSaved:
                    {
                        util.Set<libasm.hardware_location> used_locs = Val as util.Set<libasm.hardware_location>;
                        used_locs = used_locs.Union(attrs.cc.CalleeAlwaysSavesLocations);
                        List<libasm.hardware_location> save_order = new List<libasm.hardware_location>(util.Intersect<libasm.hardware_location>(used_locs, attrs.cc.CalleePreservesLocations));
                        List<tybel.Node> save_instrs = new List<Node>();
                        foreach(libasm.hardware_location save_loc in save_order)
                            ass.Save(null, null, save_loc, save_instrs);
                        List<libasm.OutputBlock> ret = new List<libasm.OutputBlock>();
                        foreach (tybel.Node save_node in save_instrs)
                            ret.AddRange(save_node.Assemble(ass, attrs));
                        return ret;
                    }

                case SpecialNodeType.RestoreCalleeSaved:
                    {
                        util.Set<libasm.hardware_location> used_locs = Val as util.Set<libasm.hardware_location>;
                        used_locs = used_locs.Union(attrs.cc.CalleeAlwaysSavesLocations);
                        List<libasm.hardware_location> restore_order = new List<libasm.hardware_location>(util.Intersect<libasm.hardware_location>(used_locs, attrs.cc.CalleePreservesLocations));
                        restore_order.Reverse();
                        List<tybel.Node> restore_instrs = new List<Node>();
                        foreach (libasm.hardware_location restore_loc in restore_order)
                            ass.Restore(null, null, restore_loc, restore_instrs);
                        List<libasm.OutputBlock> ret = new List<libasm.OutputBlock>();
                        foreach (tybel.Node restore_node in restore_instrs)
                            ret.AddRange(restore_node.Assemble(ass, attrs));
                        return ret;
                    }
            }
            throw new NotImplementedException();
        }
    }

    public class CilNode : Node
    {
        public frontend.cil.CilNode Node;

        public override ICollection<vara> defs
        {
            get { return new vara[] { }; }
        }

        public override ICollection<vara> uses
        {
            get { return new vara[] { }; }
        }

        public override bool IsMove
        {
            get { return false; }
        }

        public override bool IsUnconditionalJmp
        {
            get { return false; }
        }

        public override IEnumerable<libasm.OutputBlock> Assemble(Assembler ass, Assembler.MethodAttributes attrs)
        {
            return new libasm.OutputBlock[] { new tybel.Tybel.DebugNode { Code = Node } };
        }

        public override string ToString()
        {
            return Node.ToString();
        }
    }

    public class LabelNode : Node
    {
        public string Label;
        public bool Local;
        public override string ToString()
        {
            return Label + ":";
        }

        public LabelNode(string label, bool local) { Label = label; Local = local; }

        public override IEnumerable<libasm.OutputBlock> Assemble(Assembler ass, Assembler.MethodAttributes attrs)
        {
            if (Local)
                return new libasm.OutputBlock[] { new libasm.LocalSymbol(Label, false) };
            else
                return new libasm.OutputBlock[] { new libasm.ExportedSymbol(Label, false, true) };
        }

        public override ICollection<vara> defs
        {
            get { return new vara[] { }; }
        }

        public override ICollection<vara> uses
        {
            get { return new vara[] { }; }
        }

        public override bool IsMove
        {
            get { return false; }
        }

        public override bool IsUnconditionalJmp
        {
            get { return false; }
        }
    }
}

namespace libtysila
{
    partial class Assembler
    {
        public abstract IList<tybel.Node> SelectInstruction(timple.TreeNode inst, ref int next_var, ref int next_block,
            IList<libasm.hardware_location> las, IList<libasm.hardware_location> lvs);
    }
}
