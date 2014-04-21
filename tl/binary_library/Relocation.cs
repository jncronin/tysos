/* Copyright (C) 2013 by John Cronin
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
    class Relocation : IRelocation
    {
        protected IRelocationType type = null;
        protected ISection defined_in = null;
        protected ISymbol references = null;
        protected long addend = 0;
        protected ulong offset = 0;

        public IRelocationType Type
        {
            get
            {
                return type;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                type = value;
            }
        }

        public ISection DefinedIn
        {
            get
            {
                return defined_in;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                defined_in = value;
            }
        }

        public ISymbol References
        {
            get
            {
                return references;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                references = value;
            }
        }

        public long Addend
        {
            get
            {
                return addend;
            }
            set
            {
                addend = value;
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
    }
}
