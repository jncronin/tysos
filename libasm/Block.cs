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
    public enum LabelType
    {
        Function,
        Object,
        AddressOf,
        DataSectionPointer,
        FunctionPointerFromData
    }

    public class OutputBlock
    {
    }

    public class PrefixBlock : OutputBlock
    {
        public virtual IList<byte> Code { get { return new byte[] { }; } }
    }

    public class NodeReference : OutputBlock
    {
        public int block_id;
        public int length;
        public int offset;
        public int shift_after_offset = 0;

        public int longest_distance;

        public bool IsPossible
        {
            get
            {
                switch (length)
                {
                    case 1:
                        if ((longest_distance >= SByte.MinValue) && (longest_distance <= SByte.MaxValue))
                            return true;
                        return false;
                    case 2:
                        if ((longest_distance >= Int16.MinValue) && (longest_distance <= Int16.MaxValue))
                            return true;
                        return false;
                    case 3:
                        if ((longest_distance >= -8388608) && (longest_distance <= 8388607))
                            return true;
                        return false;
                    case 4:
                        if ((longest_distance >= Int32.MinValue) && (longest_distance <= Int32.MaxValue))
                            return true;
                        return false;
                    case 8:
                        return true;
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }

    public class NodeHeader : OutputBlock
    {
        public int block_id;
    }

    public class BlockChoice : OutputBlock
    {
        public IList<IList<OutputBlock>> Choices;
    }

    public class RelativeReference : OutputBlock
    {
        public string Target;
        public int Size;
        public int Addend;
        public RelocationBlock.RelocationType RelType;
    }

    public class CodeBlock : OutputBlock
    {
        public IList<byte> Code;

        public class CompiledInstruction
        {
            protected CompiledInstruction() { }
            public CompiledInstruction(string inst) { _inst = inst; }
            string _inst = null;

            public override string ToString()
            {
                if (_inst == null)
                    return "";
                else
                    return _inst;
            }

            public virtual IList<byte> GetCompiledRepresentation() { return new byte[] { }; }
        }

        public IList<CompiledInstruction> Instructions;

        public CodeBlock() { }
        public CodeBlock(IList<byte> v1) { Code = v1; }
        public CodeBlock(IList<byte> v1, IList<byte> v2) { Code = new List<byte>(v1); ((List<byte>)Code).AddRange(v2); }
        public CodeBlock(IList<byte> v1, IList<CompiledInstruction> instrs) : this(v1) { Instructions = instrs; }
        public CodeBlock(IList<byte> v1, CompiledInstruction instr) : this(v1) { Instructions = new CompiledInstruction[] { instr }; }
        public CodeBlock(IList<byte> v1, IList<byte> v2, IList<CompiledInstruction> instrs) : this(v1, v2) { Instructions = instrs; }
    }

    public class DataBlock : OutputBlock
    {
        public byte[] Data;

        public bool ReadOnly = true;

        public DataBlock(byte[] data) { Data = data; }
        public DataBlock(byte[] data, bool read_only) { Data = data; ReadOnly = read_only; }
        public DataBlock() { }
    }

    public class ExportedSymbol : OutputBlock
    {
        public string Name;
        public int Offset;
        public bool LocalOnly;
        public bool IsFunc;

        public ExportedSymbol(string name, bool local_only, bool is_func) { Name = name; LocalOnly = local_only; IsFunc = is_func; }
        public ExportedSymbol(string name) { Name = name; LocalOnly = false; IsFunc = true; }
    }

    public class LocalSymbol : ExportedSymbol
    {
        public LocalSymbol(string name, bool is_func) : base(name, true, is_func) { }
        public LocalSymbol(string name) : base(name) { }
    }

    public enum SymbolDescribes { text, data, rodata };

    public class RelocationBlock : OutputBlock
    {
        public int Size = 4;

        public RelocationType RelType;
        public string Target;

        public int Offset;
        public int Value;

        public byte[] OutputBytes;

        public class RelocationType
        {
            public uint type;

            public static implicit operator uint(RelocationType rt)
            {
                return rt.type;
            }

            public static implicit operator RelocationType(uint rt)
            {
                return new RelocationType { type = rt };
            }

            public override string ToString()
            {
                return type.ToString();
            }
        }
    }

}
