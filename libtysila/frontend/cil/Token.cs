/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace libtysila
{
    class TTCToken : Token
    {
        public Assembler.TypeToCompile ttc;
    }

    class FTCToken : Token
    {
        public Assembler.FieldToCompile ftc;
    }

    class MTCToken : Token
    {
        public Assembler.MethodToCompile mtc;
    }

    class StringToken : Token
    {
        public string str;

        public override Metadata.ITableRow Value
        {
            get
            {
                return new Metadata.UserStringHeapItem(str);
            }
        }
    }

    public class Token
    {
        int table_index, table_id;

        Metadata.ITableRow _val = null;

        protected Token() { }

        public static implicit operator Metadata.TableIndex(Token t) { return new Metadata.TableIndex(t); }

        public Token(UInt32 Token, Metadata m)
        {
            table_id = (int)((Token >> 24) & 0xff);
            table_index = (int)(Token & 0x00ffffff);
            _m = m;
        }
        public Token(int TableIndex, int TableId, Metadata m)
        {
            table_id = TableId;
            table_index = TableIndex;
            _m = m;
        }
        public Token(Metadata.ITableRow trow)
        {
            table_id = trow.TableId();
            table_index = trow.GetRowNumber();
            _m = trow.GetMetadata();
            _val = trow;
        }
        public uint ToUInt32()
        {
            return ((uint)table_id << 24) + (uint)table_index;
        }
        public IEnumerable<byte> CompressTypeDefOrRef()
        {
            uint tid;
            if (table_id == (int)Metadata.TableId.TypeDef)
                tid = 0;
            else if (table_id == (int)Metadata.TableId.TypeRef)
                tid = 1;
            else if (table_id == (int)Metadata.TableId.TypeSpec)
                tid = 2;
            else
                throw new NotSupportedException();

            tid |= ((uint)table_index << 2);
            return CompressValue(tid);
        }
        public static IEnumerable<byte> CompressValue(UInt32 val)
        {
            if (val <= 0x7f)
                return new byte[] { (byte)val };
            else if (val <= 0x3fff)
                return new byte[] { (byte)(((val >> 8) & 0x3f) | 0x80), (byte)(val & 0xff) };
            else
                return new byte[] { (byte)(((val >> 24) & 0x1f) | 0xc0), (byte)((val >> 16) & 0xff),
                    (byte)((val >> 8) & 0xff), (byte)(val & 0xff) };
        }
        public Metadata Metadata { get { return _m; } }

        private int ReadBlob(IList<byte> b, ref int offset)
        {
            if ((b[offset] & 0x80) == 0)
            {
                offset++;
                return (int)b[offset - 1];
            }
            else if ((b[offset] & 0x40) == 0)
            {
                byte b1 = (byte)(b[offset] & 0x3f);
                byte b2 = b[offset + 1];
                offset += 2;

                return ((int)b1 << 8) + (int)b2;
            }
            else
            {
                byte b1 = (byte)(b[offset] & 0x1f);
                byte b2 = b[offset + 1];
                byte b3 = b[offset + 2];
                byte b4 = b[offset + 3];
                offset += 4;
                return ((int)b1 << 24) + ((int)b2 << 16) + ((int)b3 << 8) + (int)b4;
            }
        }

        public virtual Metadata.ITableRow Value
        {
            get
            {
                if (_val != null)
                    return _val;
                else if (table_id == 0x70)
                {
                    Metadata.UserStringHeapItem sh = new Metadata.UserStringHeapItem();
                    sh.RowNumber = table_index;

                    int length;
                    int offset = table_index;

                    length = ReadBlob(_m.USHeap, ref offset);

                    List<byte> str = new List<byte>();

                    for (int i = 0; i < (length - 1); i++)
                    {
                        str.Add(_m.USHeap[offset + i]);
                    }

                    sh.ByteString = str.ToArray();

                    return sh;
                }
                else
                {
                    if (table_index == 0x0)
                        return null;
                    return _m.Tables[table_id][table_index - 1];
                }
            }
        }

        private Metadata _m;
    }
}
