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
using System.Text;

namespace binary_library.elf
{
    partial class ElfFile
    {
        /* JCA relocations */
        public const int R_JCA_NONE = 0;
        public const int R_JCA_LITR1 = 1;
        public const int R_JCA_LIT = 2;
        public const int R_JCA_SRCA = 3;
        public const int R_JCA_SRCB = 4;
        public const int R_JCA_SRCAB = 5;
        public const int R_JCA_SRCBCOND = 6;
        public const int R_JCA_SRCABCOND = 7;
        public const int R_JCA_SRCAREL = 8;
        public const int R_JCA_SRCBREL = 9;
        public const int R_JCA_SRCABREL = 10;
        public const int R_JCA_SRCBCONDREL = 11;
        public const int R_JCA_SRCABCONDREL = 12;

        class Rel_JCA_LitR1 : IRelocationType
        {
            public ulong KeepMask
            {
                get
                {
                    return 0x80000000;
                }
            }

            public int Length
            {
                get
                {
                    return 4;
                }
            }

            public virtual string Name
            {
                get
                {
                    return "R_JCA_LITR1";
                }
            }

            public ulong SetMask
            {
                get
                {
                    return 0x7fffffff;
                }
            }

            public virtual int Type
            {
                get
                {
                    return R_JCA_LITR1;
                }
            }

            public virtual long Evaluate(IRelocation reloc)
            {
                // S + A
                return (long)(reloc.References.DefinedIn.LoadAddress +
                    reloc.References.Offset) + reloc.Addend;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 31; } }
            public bool IsSigned
            { get { return false; } }
            public int BitOffset
            { get { return 0; } }
        }

        class Rel_JCA_Lit : IRelocationType
        {
            public ulong KeepMask
            {
                get
                {
                    return 0xfe000000;
                }
            }

            public int Length
            {
                get
                {
                    return 4;
                }
            }

            public virtual string Name
            {
                get
                {
                    return "R_JCA_LIT";
                }
            }

            public ulong SetMask
            {
                get
                {
                    return 0x1ffffff;
                }
            }

            public virtual int Type
            {
                get
                {
                    return R_JCA_LIT;
                }
            }

            public virtual long Evaluate(IRelocation reloc)
            {
                // S + A
                return (long)(reloc.References.DefinedIn.LoadAddress +
                    reloc.References.Offset) + reloc.Addend;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 25; } }
            public bool IsSigned
            { get { return false; } }
            public int BitOffset
            { get { return 0; } }
        }

        class Rel_JCA_Srca : IRelocationType
        {
            public ulong KeepMask
            {
                get
                {
                    return 0xffffffe0;
                }
            }

            public int Length
            {
                get
                {
                    return 4;
                }
            }

            public virtual string Name
            {
                get
                {
                    return "R_JCA_SRCA";
                }
            }

            public ulong SetMask
            {
                get
                {
                    return 0x1f;
                }
            }

            public virtual int Type
            {
                get
                {
                    return R_JCA_SRCA;
                }
            }

            public virtual long Evaluate(IRelocation reloc)
            {
                // S + A
                return (long)(reloc.References.DefinedIn.LoadAddress +
                    reloc.References.Offset) + reloc.Addend;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 5; } }
            public bool IsSigned
            { get { return true; } }
            public int BitOffset
            { get { return 0; } }
        }

        class Rel_JCA_Srcb : IRelocationType
        {
            public ulong KeepMask
            {
                get
                {
                    return 0xfffff83f;
                }
            }

            public int Length
            {
                get
                {
                    return 4;
                }
            }

            public virtual string Name
            {
                get
                {
                    return "R_JCA_SRCB";
                }
            }

            public ulong SetMask
            {
                get
                {
                    return 0x7c0;
                }
            }

            public virtual int Type
            {
                get
                {
                    return R_JCA_SRCB;
                }
            }

            public virtual long Evaluate(IRelocation reloc)
            {
                // S + A
                return ((long)(reloc.References.DefinedIn.LoadAddress +
                    reloc.References.Offset) + reloc.Addend) << 6;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 5; } }
            public bool IsSigned
            { get { return true; } }
            public int BitOffset
            { get { return 6; } }
        }

        class Rel_JCA_Srcab : IRelocationType
        {
            public ulong KeepMask
            {
                get
                {
                    return 0xfffff800;
                }
            }

            public int Length
            {
                get
                {
                    return 4;
                }
            }

            public virtual string Name
            {
                get
                {
                    return "R_JCA_SRCAB";
                }
            }

            public ulong SetMask
            {
                get
                {
                    return 0x7ff;
                }
            }

            public virtual int Type
            {
                get
                {
                    return R_JCA_SRCAB;
                }
            }

            public virtual long Evaluate(IRelocation reloc)
            {
                // S + A
                return (long)(reloc.References.DefinedIn.LoadAddress +
                    reloc.References.Offset) + reloc.Addend;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 11; } }
            public bool IsSigned
            { get { return true; } }
            public int BitOffset
            { get { return 0; } }
        }

        class Rel_JCA_Srcbcond : IRelocationType
        {
            public ulong KeepMask
            {
                get
                {
                    return 0xfffffc00;
                }
            }

            public int Length
            {
                get
                {
                    return 4;
                }
            }

            public virtual string Name
            {
                get
                {
                    return "R_JCA_SRCBCOND";
                }
            }

            public ulong SetMask
            {
                get
                {
                    return 0x3ff;
                }
            }

            public virtual int Type
            {
                get
                {
                    return R_JCA_SRCBCOND;
                }
            }

            public virtual long Evaluate(IRelocation reloc)
            {
                // S + A
                return ((long)(reloc.References.DefinedIn.LoadAddress +
                    reloc.References.Offset) + reloc.Addend) << 6;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 10; } }
            public bool IsSigned
            { get { return true; } }
            public int BitOffset
            { get { return 6; } }
        }

        class Rel_JCA_Srcabcond : IRelocationType
        {
            public ulong KeepMask
            {
                get
                {
                    return 0xffff0000;
                }
            }

            public int Length
            {
                get
                {
                    return 4;
                }
            }

            public virtual string Name
            {
                get
                {
                    return "R_JCA_SRCABCOND";
                }
            }

            public ulong SetMask
            {
                get
                {
                    return 0xffff;
                }
            }

            public virtual int Type
            {
                get
                {
                    return R_JCA_SRCABCOND;
                }
            }

            public virtual long Evaluate(IRelocation reloc)
            {
                // S + A
                return (long)(reloc.References.DefinedIn.LoadAddress +
                    reloc.References.Offset) + reloc.Addend;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 16; } }
            public bool IsSigned
            { get { return true; } }
            public int BitOffset
            { get { return 0; } }
        }

        class Rel_JCA_SrcaRel : Rel_JCA_Srca, IRelocationType
        {
            public override string Name
            { get { return base.Name + "REL"; } }
            public override int Type
            { get { return base.Type - R_JCA_SRCA + R_JCA_SRCAREL; } }
            public override long Evaluate(IRelocation reloc)
            { return base.Evaluate(reloc) - (long)(reloc.DefinedIn.LoadAddress + reloc.Offset); }
        }

        class Rel_JCA_SrcbRel : Rel_JCA_Srcb, IRelocationType
        {
            public override string Name
            { get { return base.Name + "REL"; } }
            public override int Type
            { get { return base.Type - R_JCA_SRCA + R_JCA_SRCAREL; } }
            public override long Evaluate(IRelocation reloc)
            { return base.Evaluate(reloc) - (long)(reloc.DefinedIn.LoadAddress + reloc.Offset); }
        }

        class Rel_JCA_SrcabRel : Rel_JCA_Srcab, IRelocationType
        {
            public override string Name
            { get { return base.Name + "REL"; } }
            public override int Type
            { get { return base.Type - R_JCA_SRCA + R_JCA_SRCAREL; } }
            public override long Evaluate(IRelocation reloc)
            { return base.Evaluate(reloc) - (long)(reloc.DefinedIn.LoadAddress + reloc.Offset); }
        }

        class Rel_JCA_SrcbcondRel : Rel_JCA_Srcbcond, IRelocationType
        {
            public override string Name
            { get { return base.Name + "REL"; } }
            public override int Type
            { get { return base.Type - R_JCA_SRCA + R_JCA_SRCAREL; } }
            public override long Evaluate(IRelocation reloc)
            { return base.Evaluate(reloc) - (long)(reloc.DefinedIn.LoadAddress + reloc.Offset); }
        }

        class Rel_JCA_SrcabcondRel : Rel_JCA_Srcabcond, IRelocationType
        {
            public override string Name
            { get { return base.Name + "REL"; } }
            public override int Type
            { get { return base.Type - R_JCA_SRCA + R_JCA_SRCAREL; } }
            public override long Evaluate(IRelocation reloc)
            { return base.Evaluate(reloc) - (long)(reloc.DefinedIn.LoadAddress + reloc.Offset); }
        }
    }
}
