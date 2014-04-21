/* Copyright (C) 2012 by John Cronin
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

namespace libtysila.tydb
{
    public abstract class TyDbFile
    {
        public string CompiledFileName;
        public List<Function> Functions = new List<Function>();
        public abstract void Write(System.IO.Stream s);
    }

    public class Function
    {
        public string MetadataFileName;
        public uint MetadataToken;
        public string MangledName;
        public uint TextOffset;
        public List<Line> Lines = new List<Line>();
        public List<VarArg> Vars = new List<VarArg>();
        public List<VarArg> Args = new List<VarArg>();

        public override string ToString()
        {
            return MangledName;
        }

        public Dictionary<int, int> compiled_to_il = new Dictionary<int, int>();
        public int GetILOffsetFromCompiledOffset(int compiled_offset)
        {
            if (compiled_to_il.ContainsKey(compiled_offset))
                return compiled_to_il[compiled_offset];
            return -1;
        }

        public VarArg GetVarArg(string name)
        {
            foreach (VarArg arg in Args)
            {
                if (arg.Name == name)
                    return arg;
            }
            foreach (VarArg var in Vars)
            {
                if (var.Name == name)
                    return var;
            }
            return null;
        }
    }

    public class Location
    {
        public enum LocationType
        { Register, Memory, ContentsOfLocation };
        public LocationType Type;
        public string RegisterName;
        public ulong MemoryLocation;
        public Location ContentsOf;
        public int Length;
        public int Offset;

        public override string ToString()
        {
            switch (Type)
            {
                case LocationType.Register:
                    return RegisterName + "(" + Length.ToString() + ")";
                case LocationType.Memory:
                    return "*" + MemoryLocation.ToString() + "(" + Length.ToString() + ")";
                case LocationType.ContentsOfLocation:
                    return "[" + ContentsOf.ToString() + " + " + Offset.ToString() + "](" + Length.ToString() + ")";
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Line
    {
        public int ILOffset;
        public int CompiledOffset;
    }

    public class VarArg
    {
        public string Name;
        public Location Location;

        public override string ToString()
        {
            return Name;
        }
    }
}
