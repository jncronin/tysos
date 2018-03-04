/* Copyright (C) 2013-2016 by John Cronin
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

namespace binary_library
{
    class Symbol : ISymbol
    {
        string name = "";
        internal ISection definedin = null;
        internal int index = -1;
        SymbolType type = SymbolType.Undefined;
        SymbolObjectType obj_type = SymbolObjectType.Unknown;
        ulong offset = 0;
        long size = 0;

        public virtual int Index { get { return index; } }

        public override string ToString()
        {
            return offset.ToString("X") + ": " + name;
        }

        public ISection DefinedIn { get { return definedin; } }

        public SymbolType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        public SymbolObjectType ObjectType
        {
            get
            {
                return obj_type;
            }
            set
            {
                obj_type = value;
            }
        }

        public ulong Offset
        {
            get
            {
                return offset;
            }
            set
            {
                offset = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name == null)
                    throw new ArgumentNullException();
                name = value;
            }
        }

        public long Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
            }
        }
    }
}
