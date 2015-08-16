/* Copyright (C) 2015 by John Cronin
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

namespace acpipc.Aml
{
    class Namespace
    {
        public class State
        {
            public ACPIName Scope;
            public Dictionary<int, ACPIObject> Args;
            public Dictionary<int, ACPIObject> Locals;
            public ACPIObject Return;
        }

        IMachineInterface mi;

        Dictionary<string, ACPIObject> Objects = new Dictionary<string, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<string>());
        public Dictionary<string, ACPIObject> Devices = new Dictionary<string, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<string>());
        public Dictionary<string, ACPIObject> Processors = new Dictionary<string, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<string>());

        const ulong Revision = 0x20;
        const ulong One = 1UL;
        const ulong Zero = 0UL;
        const ulong Ones = 0xffffffffffffffff;

        public Namespace(IMachineInterface MachineInterface)
        {
            mi = MachineInterface;
        }

        public bool ParseDefBlockHeader(byte[] aml, ref int idx, DefBlockHeader h)
        {
            h.TableSignature = ReadDWord(aml, ref idx);
            h.TableLength = (int)ReadDWord(aml, ref idx);
            h.SpecCompliance = ReadByte(aml, ref idx);
            h.CheckSum = ReadByte(aml, ref idx);
            h.OemID = ReadString(aml, ref idx, 6);
            h.OemTableID = ReadString(aml, ref idx, 8);
            h.OemRevision = ReadDWord(aml, ref idx);
            h.CreatorID = ReadDWord(aml, ref idx);
            h.CreatorRevision = ReadDWord(aml, ref idx);

            return true;
        }

        private string ReadString(byte[] aml, ref int idx, int max_count)
        {
            char[] str = new char[max_count];
            int i;
            for(i = 0; i < max_count; i++)
                str[i] = (char)ReadByte(aml, ref idx);
            for (i = 0; i < max_count; i++)
            {
                if (str[i] == (char)0x0)
                    break;
            }
            return new string(str, 0, i);
        }

        private byte ReadByte(byte[] aml, ref int idx)
        {
            return aml[idx++];
        }

        private ushort ReadWord(byte[] aml, ref int idx)
        {
            ushort ret = BitConverter.ToUInt16(aml, idx);
            idx += 2;
            return ret;
        }

        private uint ReadDWord(byte[] aml, ref int idx)
        {
            uint ret = BitConverter.ToUInt32(aml, idx);
            idx += 4;
            return ret;
        }

        private ulong ReadQWord(byte[] aml, ref int idx)
        {
            ulong ret = BitConverter.ToUInt64(aml, idx);
            idx += 8;
            return ret;
        }

        public bool ParseTermList(byte[] aml, ref int idx, int count, ACPIName scope)
        {
            ACPIObject retval;
            State s = new State { Args = new Dictionary<int,ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()), Locals = new Dictionary<int,ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()), Scope = scope };
            return ParseTermList(aml, ref idx, count, out retval, s);
        }

        public bool ParseTermList(byte[] aml, ref int idx, int count, State s)
        {
            ACPIObject retval;
            return ParseTermList(aml, ref idx, count, out retval, s);
        }

        public bool ParseTermList(byte[] aml, ref int idx, int count, out ACPIObject retval, State s)
        {
            int old_idx = idx;
            retval = null;

            //System.Diagnostics.Debugger.Log(0, "acpipc", "TermList");

            if (count < 0)
                count = aml.Length - idx;

            while (idx < (old_idx + count))
            {
                if (!ParseTermObj(aml, ref idx, out retval, s))
                {
                    idx = old_idx;
                    return false;
                }

                if (retval != null)
                    idx = old_idx + count;
            }

            if (s.Return != null && retval == null)
                retval = s.Return;

            return true;
        }

        bool ParseTermObj(byte[] aml, ref int idx, out ACPIObject retval, State s)
        {
            int old_idx = idx;
            ACPIObject res;
            retval = null;

            if (ParseNameSpaceModifierObj(aml, ref idx, s) ||
                ParseNamedObj(aml, ref idx, s) ||
                ParseDefReturn(aml, ref idx, out retval, s) ||
                ParseType1Opcode(aml, ref idx, s) ||
                ParseType2Opcode(aml, ref idx, out res, s))
                return true;

            idx = old_idx;
            return false;
        }

        private bool ParseType2Opcode(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseDefAcquire(aml, ref idx, out result, s) ||
                ParseDefAdd(aml, ref idx, out result, s) ||
                ParseDefAnd(aml, ref idx, out result, s) ||
                ParseDefBuffer(aml, ref idx, out result, s) ||
                ParseDefConcat(aml, ref idx, out result, s) ||
                ParseDefConcatRes(aml, ref idx, out result, s) ||
                ParseDefCondRefOf(aml, ref idx, out result, s) ||
                ParseDefCopyObject(aml, ref idx, out result, s) ||
                ParseDefDecrement(aml, ref idx, out result, s) ||
                ParseDefDerefOf(aml, ref idx, out result, s) ||
                ParseDefDivide(aml, ref idx, out result, s) ||
                ParseDefFindSetLeftBit(aml, ref idx, out result, s) ||
                ParseDefFindRightSetBit(aml, ref idx, out result, s) ||
                ParseDefFromBCD(aml, ref idx, out result, s) ||
                ParseDefIncrement(aml, ref idx, out result, s) ||
                ParseDefIndex(aml, ref idx, out result, s) ||
                ParseDefLAnd(aml, ref idx, out result, s) ||
                ParseDefLEqual(aml, ref idx, out result, s) ||
                ParseDefLGreater(aml, ref idx, out result, s) ||
                ParseDefLGreaterEqual(aml, ref idx, out result, s) ||
                ParseDefLLess(aml, ref idx, out result, s) ||
                ParseDefLLessEqual(aml, ref idx, out result, s) ||
                ParseDefMid(aml, ref idx, out result, s) ||
                ParseDefLNot(aml, ref idx, out result, s) ||
                ParseDefLNotEqual(aml, ref idx, out result, s) ||
                ParseDefLoadTable(aml, ref idx, out result, s) ||
                ParseDefLOr(aml, ref idx, out result, s) ||
                ParseDefMatch(aml, ref idx, out result, s) ||
                ParseDefMod(aml, ref idx, out result, s) ||
                ParseDefMultiply(aml, ref idx, out result, s) ||
                ParseDefNAnd(aml, ref idx, out result, s) ||
                ParseDefNOr(aml, ref idx, out result, s) ||
                ParseDefNot(aml, ref idx, out result, s) ||
                ParseDefObjectType(aml, ref idx, out result, s) ||
                ParseDefOr(aml, ref idx, out result, s) ||
                ParseDefPackage(aml, ref idx, out result, s) ||
                ParseDefVarPackage(aml, ref idx, out result, s) ||
                ParseDefRefOf(aml, ref idx, out result, s) ||
                ParseDefShiftLeft(aml, ref idx, out result, s) ||
                ParseDefShiftRight(aml, ref idx, out result, s) ||
                ParseDefSizeOf(aml, ref idx, out result, s) ||
                ParseDefStore(aml, ref idx, out result, s) ||
                ParseDefSubtract(aml, ref idx, out result, s) ||
                ParseDefTimer(aml, ref idx, out result, s) ||
                ParseDefToBCD(aml, ref idx, out result, s) ||
                ParseDefToBuffer(aml, ref idx, out result, s) ||
                ParseDefToDecimalString(aml, ref idx, out result, s) ||
                ParseDefToHexString(aml, ref idx, out result, s) ||
                ParseDefToInteger(aml, ref idx, out result, s) ||
                ParseDefToString(aml, ref idx, out result, s) ||
                ParseDefWait(aml, ref idx, out result, s) ||
                ParseDefXOr(aml, ref idx, out result, s) ||
                ParseUserTermObj(aml, ref idx, out result, s))
                return true;

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefAcquire(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x23))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefAdd(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject a, b;
            ACPIObject target;

            if (ParseByte(aml, ref idx, 0x72) &&
                ParseTermArg(aml, ref idx, out a, s) &&
                ParseTermArg(aml, ref idx, out b, s) &&
                ParseTarget(aml, ref idx, out target, s))
            {
                a = a.Evaluate(mi, s, this);
                b = b.Evaluate(mi, s, this);

                if (a.Type != ACPIObject.DataType.Integer || b.Type != ACPIObject.DataType.Integer)
                    throw new Exception("Add requires integer operands");

                ulong val = unchecked(a.IntegerData + b.IntegerData);                

                target.Write(val, mi, s, this);
                result = val;
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefAnd(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject a, b;
            ACPIObject target;

            if (ParseByte(aml, ref idx, 0x7b) &&
                ParseTermArg(aml, ref idx, out a, s) &&
                ParseTermArg(aml, ref idx, out b, s) &&
                ParseTarget(aml, ref idx, out target, s))
            {
                ACPIObject a2 = a.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);
                ACPIObject b2 = b.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);

                ulong val = a2.IntegerData & b2.IntegerData;

                target.Write(val, mi, s, this);
                result = val;
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseTarget(byte[] aml, ref int idx, out ACPIObject target, State s)
        {
            int old_idx = idx;
            ACPIName name = s.Scope.Clone();

            if (ParseSuperName(aml, ref idx, out target, s))
                return true;
            else if (ParseNullName(aml, ref idx, name, s))
            {
                target = new ACPIObject(ACPIObject.DataType.ObjectReference, new ACPIObject.ObjRefData { Object = null, n = this });
                return true;
            }

            idx = old_idx;
            target = null;
            return false;
        }

        private bool ParseSuperName(byte[] aml, ref int idx, out ACPIObject target, State s)
        {
            int old_idx = idx;

            if (ParseSimpleName(aml, ref idx, out target, s) ||
                ParseDebugObj(aml, ref idx, out target, s) ||
                ParseType6Opcode(aml, ref idx, out target, s))
                return true;

            idx = old_idx;
            target = null;
            return false;
        }

        private bool ParseType6Opcode(byte[] aml, ref int idx, out ACPIObject target, State s)
        {
            throw new NotImplementedException();
        }

        private bool ParseDebugObj(byte[] aml, ref int idx, out ACPIObject target, State s)
        {
            throw new NotImplementedException();
        }

        private bool ParseSimpleName(byte[] aml, ref int idx, out ACPIObject target, State s)
        {
            int old_idx = idx;
            ACPIName name;

            if (ParseNameString(aml, ref idx, out name, s))
            {
                target = new ACPIObject(ACPIObject.DataType.ObjectReference, new ACPIObject.ObjRefData { ObjectName = name, n = this });
                return true;
            }
            else if (ParseArgObj(aml, ref idx, out target, s) ||
                ParseLocalObj(aml, ref idx, out target, s))
                return true;

            idx = old_idx;
            target = null;
            return false;
        }

        private bool ParseDefConcat(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x73))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefConcatRes(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x84))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefCondRefOf(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x12))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefCopyObject(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x9d))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefDecrement(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject a;

            if (ParseByte(aml, ref idx, 0x76) &&
                ParseSuperName(aml, ref idx, out a, s))
            {
                //System.Diagnostics.Debugger.Log(0, "acpipc", "Decrement");

                ACPIObject a2 = a.Evaluate(mi, s, this);
                if (a2.Type != ACPIObject.DataType.Integer)
                    throw new Exception("Decrement requires Integer argument");

                ACPIObject res = unchecked((ulong)a2.Data - 1);
                result = res;
                a.Write(res, mi, s, this);
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefDerefOf(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject objref;

            if (ParseByte(aml, ref idx, 0x83) &&
                ParseTermArg(aml, ref idx, out objref, s))
            {
                objref = objref.Evaluate(mi, s, this);
                if (objref.Type != ACPIObject.DataType.ObjectReference)
                    throw new Exception("Source must be ObjectReference");

                ACPIObject.ObjRefData ord = objref.Data as ACPIObject.ObjRefData;

                switch (ord.Object.Type)
                {
                    case ACPIObject.DataType.Buffer:
                        result = (ulong)((byte[])ord.Object.Data)[ord.Index];
                        return true;
                    case ACPIObject.DataType.String:
                        result = (ulong)((string)ord.Object.Data)[ord.Index];
                        return true;
                    case ACPIObject.DataType.Package:
                        result = ((ACPIObject[])ord.Object.Data)[ord.Index];
                        return true;
                    default:
                        throw new NotSupportedException();
                }
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefDivide(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x78))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefFindSetLeftBit(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x81))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefFindRightSetBit(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x83))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefFromBCD(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x28))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefIncrement(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject a;

            if (ParseByte(aml, ref idx, 0x75) &&
                ParseSuperName(aml, ref idx, out a, s))
            {
                //System.Diagnostics.Debugger.Log(0, "acpipc", "Increment");
                ACPIObject a2 = a.Evaluate(mi, s, this);
                if (a2.Type != ACPIObject.DataType.Integer)
                    throw new Exception("Increment requires Integer argument");

                ACPIObject res = unchecked((ulong)a2.Data + 1);
                result = res;
                a.Write(res, mi, s, this);
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefIndex(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject Source;
            ACPIObject Index;
            ACPIObject Target;

            if (ParseByte(aml, ref idx, 0x88) &&
                ParseTermArg(aml, ref idx, out Source, s) &&
                ParseTermArg(aml, ref idx, out Index, s) &&
                ParseTermArg(aml, ref idx, out Target, s))
            {
                Source = Source.Evaluate(mi, s, this);
                Index = Index.Evaluate(mi, s, this);

                if (Source.Type != ACPIObject.DataType.Buffer &&
                    Source.Type != ACPIObject.DataType.Package &&
                    Source.Type != ACPIObject.DataType.String)
                    throw new Exception("Source must be Buffer, Package or String");

                if (Index.Type != ACPIObject.DataType.Integer)
                    throw new Exception("Index must be Integer");

                ACPIObject.ObjRefData res = new ACPIObject.ObjRefData { Object = Source, Index = (int)Index.IntegerData, n = this };
                result = new ACPIObject(ACPIObject.DataType.ObjectReference, res);

                Target.Write(result, mi, s, this);
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLAnd(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject a, b;

            if (ParseByte(aml, ref idx, 0x90) &&
                ParseTermArg(aml, ref idx, out a, s) &&
                ParseTermArg(aml, ref idx, out b, s))
            {
                /* Try to evaluate to integers */
                ACPIObject ai, bi;
                ai = a.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);
                bi = b.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);
                if ((ai != null) && (bi != null))
                {
                    if ((ai.IntegerData != 0) && (bi.IntegerData != 0))
                        result = Ones;
                    else
                        result = Zero;
                    return true;
                }
                throw new NotImplementedException();
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLEqual(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject a, b;

            if (ParseByte(aml, ref idx, 0x93) &&
                ParseTermArg(aml, ref idx, out a, s) &&
                ParseTermArg(aml, ref idx, out b, s))
            {
                /* Try to evaluate to integers */
                ACPIObject ai, bi;
                ai = a.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);
                bi = b.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);
                if ((ai != null) && (bi != null))
                {
                    if (ai.IntegerData == bi.IntegerData)
                        result = Ones;
                    else
                        result = Zero;
                    return true;
                }
                throw new NotImplementedException();
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLGreater(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x94))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLGreaterEqual(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x92) &&
                ParseByte(aml, ref idx, 0x95))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLLess(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x95))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLLessEqual(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x92) &&
                ParseByte(aml, ref idx, 0x94))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefMid(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x9e))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLNot(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x92))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLNotEqual(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x92) &&
                ParseByte(aml, ref idx, 0x93))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLoadTable(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x1f))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefLOr(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x91))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefMatch(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x89))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefMod(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x85))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefMultiply(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x77))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefNAnd(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x7c))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefNOr(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x7e))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefNot(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x80))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefObjectType(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x8e))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefOr(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x7d))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefRefOf(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x71))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefShiftLeft(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject a, b;
            ACPIObject target;

            if (ParseByte(aml, ref idx, 0x79) &&
                ParseTermArg(aml, ref idx, out a, s) &&
                ParseTermArg(aml, ref idx, out b, s) &&
                ParseTarget(aml, ref idx, out target, s))
            {
                a = a.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);
                b = b.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);

                ulong val = unchecked(a.IntegerData << (int)b.IntegerData);

                target.Write(val, mi, s, this);
                result = val;
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefShiftRight(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject a, b;
            ACPIObject target;

            if (ParseByte(aml, ref idx, 0x7a) &&
                ParseTermArg(aml, ref idx, out a, s) &&
                ParseTermArg(aml, ref idx, out b, s) &&
                ParseTarget(aml, ref idx, out target, s))
            {
                a = a.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);
                b = b.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);

                ulong val = unchecked(a.IntegerData >> (int)b.IntegerData);

                target.Write(val, mi, s, this);
                result = val;
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefSizeOf(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject ObjectName;

            if (ParseByte(aml, ref idx, 0x87) &&
                ParseSuperName(aml, ref idx, out ObjectName, s))
            {
                // ObjectName must be string, buffer or package data
                ObjectName = ObjectName.Evaluate(mi, s, this);
                ulong val = 0;

                switch (ObjectName.Type)
                {
                    case ACPIObject.DataType.String:
                        val = (ulong)((string)ObjectName.Data).Length;
                        break;
                    case ACPIObject.DataType.Buffer:
                        val = (ulong)((byte[])ObjectName.Data).Length;
                        break;
                    case ACPIObject.DataType.Package:
                        val = (ulong)((ACPIObject[])ObjectName.Data).Length;
                        break;
                    default:
                        throw new Exception("SizeOf required String, Buffer or Package as argument");
                }

                result = val;
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefStore(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject Source, Dest;

            if (ParseByte(aml, ref idx, 0x70) &&
                ParseTermArg(aml, ref idx, out Source, s) &&
                ParseSuperName(aml, ref idx, out Dest, s))
            {
                //System.Diagnostics.Debugger.Log(0, "acpipc", "Store");

                Source = Source.Evaluate(mi, s, this);
                Dest.Write(Source, mi, s, this);
                result = null;
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefSubtract(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIObject a, b;
            ACPIObject target;

            if (ParseByte(aml, ref idx, 0x74) &&
                ParseTermArg(aml, ref idx, out a, s) &&
                ParseTermArg(aml, ref idx, out b, s) &&
                ParseTarget(aml, ref idx, out target, s))
            {
                a = a.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);
                b = b.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);

                if (a.Type != ACPIObject.DataType.Integer || b.Type != ACPIObject.DataType.Integer)
                    throw new Exception("Subtract requires integer operands (" + a.Type.ToString() + " and " + b.Type.ToString() + ")");

                ulong val = unchecked(a.IntegerData - b.IntegerData);

                target.Write(val, mi, s, this);
                result = val;
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefTimer(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x33))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefToBCD(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x29))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefToBuffer(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x96))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefToDecimalString(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x97))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefToHexString(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x98))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefToInteger(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x99))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefToString(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x9c))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefWait(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x25))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefXOr(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x7f))
                throw new NotImplementedException();

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseUserTermObj(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;
            ACPIName name;

            if (ParseNameString(aml, ref idx, out name, s))
            {
                /* 0x00 can encode NullChar, ZeroOp or NullString - here we default to
                 * ZeroOp */
                if (name.IsNull)
                {
                    result = new ACPIObject(ACPIObject.DataType.Integer, 0UL);
                    return true;
                }

                /* If it is a valid name, we interpret it somehow */
                result = FindObject(name);

                if (result.Type == ACPIObject.DataType.Method)
                {
                    /* If its a method call, we emit a method call object, for evaluation later */
                    ACPIObject.MethodData MethodData = result.Data as ACPIObject.MethodData;

                    State new_state = new State();
                    new_state.Scope = result.Name;
                    new_state.Locals = new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>());
                    new_state.Args = new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>());
                    for (int i = 0; i < MethodData.ArgCount; i++)
                    {
                        ACPIObject p;
                        if (!ParseTermArg(aml, ref idx, out p, s))
                        {
                            idx = old_idx;
                            result = null;
                            return false;
                        }
                        new_state.Args[i] = p.Evaluate(mi, s, this);
                    }
                    int meth_idx = MethodData.Offset;
                    if (ParseTermList(aml, ref meth_idx, MethodData.Length, out result, new_state))
                        return true;
                }
                else
                    return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        public ACPIObject Evaluate(ACPIName name, IMachineInterface mi)
        {
            return Evaluate(name, mi, new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()));
        }

        public ACPIObject Evaluate(ACPIName name, IMachineInterface mi,
            Dictionary<int, ACPIObject> args)
        {
            /* Look for the specified object and evaluate it */
            ACPIObject obj = FindObject(name, false);
            if (obj == null)
                return null;

            State s = new State
            {
                Args = args,
                Locals = new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()),
                Scope = obj.Name
            };

            return obj.Evaluate(mi, s, this);
        }

        public ACPIObject FindObject(ACPIName name)
        { return FindObject(name, true); }

        public ACPIObject FindObject(ACPIName name, bool check_outer_scope)
        {
            /* Find an object in the current scope or all upper ones */
            ACPIName new_name = name.Clone();
            if (Objects.ContainsKey(new_name))
                return Objects[new_name];

            if (check_outer_scope)
            {
                while ((new_name = new_name.ScopeUp()) != null)
                {
                    if (Objects.ContainsKey(new_name))
                        return Objects[new_name];
                }
            }
            return null;
        }

        private bool ParseType1Opcode(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            ACPIObject retval;

            if (ParseDefBreak(aml, ref idx, s) ||
                ParseDefBreakPoint(aml, ref idx, s) ||
                ParseDefContinue(aml, ref idx, s) ||
                ParseDefFatal(aml, ref idx, s) ||
                ParseDefIfElse(aml, ref idx, s) ||
                ParseDefLoad(aml, ref idx, s) ||
                ParseDefNoop(aml, ref idx, s) ||
                ParseDefNotify(aml, ref idx, s) ||
                ParseDefRelease(aml, ref idx, s) ||
                ParseDefReset(aml, ref idx, s) ||
                ParseDefReturn(aml, ref idx, out retval, s) ||
                ParseDefSignal(aml, ref idx, s) ||
                ParseDefSleep(aml, ref idx, s) ||
                ParseDefStall(aml, ref idx, s) ||
                ParseDefUnload(aml, ref idx, s) ||
                ParseDefWhile(aml, ref idx, s))
                return true;

            idx = old_idx;
            return false;
        }

        private bool ParseDefBreak(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0xa5))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefBreakPoint(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0xcc))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefContinue(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x9f))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefFatal(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x32))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefIfElse(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            int pkg_length, pkg_length_offset;
            ACPIObject Predicate;

            if (ParseByte(aml, ref idx, 0xa0) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseTermArg(aml, ref idx, out Predicate, s))
            {
                int pkg_end = pkg_length_offset + pkg_length;

                if (Predicate.IntegerData != 0)
                {
                    /* Execute the if block */
                    ParseTermList(aml, ref idx, pkg_end - idx, s);
                }
                else
                    idx = pkg_end;

                /* See if there is an else block */
                if (ParseByte(aml, ref idx, 0xa1))
                {
                    int epkg_length, epkg_length_offset;
                    ParsePkgLength(aml, ref idx, out epkg_length, out epkg_length_offset);
                    int epkg_end = epkg_length_offset + epkg_length;

                    if (Predicate.IntegerData == 0)
                    {
                        /* Execute the else block */
                        ParseTermList(aml, ref idx, epkg_end - idx, s);
                    }
                    else
                        idx = epkg_end;
                }

                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDefLoad(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x20))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefNoop(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0xa3))
            {
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDefNotify(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x86))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefRelease(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x27))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefReset(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x26))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefReturn(byte[] aml, ref int idx, out ACPIObject retval, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0xa4) &&
                ParseTermArg(aml, ref idx, out retval, s))
            {
                s.Return = retval;
                return true;
            }

            idx = old_idx;
            retval = null;
            return false;
        }

        private bool ParseDefSignal(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x24))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefSleep(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x22))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefStall(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x21))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefUnload(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x2a))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefWhile(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            int pkg_length, pkg_length_offset;
            ACPIObject Predicate;

            if (ParseByte(aml, ref idx, 0xa2) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseTermArg(aml, ref idx, out Predicate, s))
            {
                ACPIObject PEval = Predicate.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);

                int pkg_end = pkg_length_offset + pkg_length;
                int pkg_start = idx;

                while (PEval.IntegerData != Zero)
                {
                    int widx = pkg_start;

                    ParseTermList(aml, ref widx, pkg_end - pkg_start, s);

                    PEval = Predicate.EvaluateTo(ACPIObject.DataType.Integer, mi, s, this);
                }

                idx = pkg_end;
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseNamedObj(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseDefBankField(aml, ref idx, s) ||
                ParseDefCreateBitField(aml, ref idx, s) ||
                ParseDefCreateByteField(aml, ref idx, s) ||
                ParseDefCreateDWordField(aml, ref idx, s) ||
                ParseDefCreateField(aml, ref idx, s) ||
                ParseDefCreateQWordField(aml, ref idx, s) ||
                ParseDefCreateWordField(aml, ref idx, s) ||
                ParseDefDataRegion(aml, ref idx, s) ||
                ParseDefDevice(aml, ref idx, s) ||
                ParseDefEvent(aml, ref idx, s) ||
                ParseDefField(aml, ref idx, s) ||
                ParseDefIndexField(aml, ref idx, s) ||
                ParseDefMethod(aml, ref idx, s) ||
                ParseDefMutex(aml, ref idx, s) ||
                ParseDefOpRegion(aml, ref idx, s) ||
                ParseDefPowerRes(aml, ref idx, s) ||
                ParseDefProcessor(aml, ref idx, s) ||
                ParseDefThermalZone(aml, ref idx, s))
                return true;

            idx = old_idx;
            return false;
        }

        private bool ParseDefEvent(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x02))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefThermalZone(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x85))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefPowerRes(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x84))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefOpRegion(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            ACPIName RegionName;
            byte RegionSpace;
            ACPIObject RegionOffset;
            ACPIObject RegionLen;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x80) &&
                ParseNameString(aml, ref idx, out RegionName, s) &&
                ParseByteData(aml, ref idx, out RegionSpace) &&
                ParseTermArg(aml, ref idx, out RegionOffset, s) &&
                ParseTermArg(aml, ref idx, out RegionLen, s))
            {
                if (RegionOffset.Type != ACPIObject.DataType.Integer)
                    throw new Exception("RegionOffset does not evaluate to Integer");
                if (RegionLen.Type != ACPIObject.DataType.Integer)
                    throw new Exception("RegionLen does not evaluate to Integer");

                ACPIObject r = new ACPIObject(ACPIObject.DataType.OpRegion, new ACPIObject.OpRegionData
                {
                    RegionSpace = RegionSpace,
                    Length = RegionLen.IntegerData,
                    Offset = RegionOffset.IntegerData
                });
                r.Name = RegionName;
                Objects[RegionName] = r;
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseTermArg(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            if (ParseType2Opcode(aml, ref idx, out result, s) ||
                ParseDataObject(aml, ref idx, out result, s) ||
                ParseArgObj(aml, ref idx, out result, s) ||
                ParseLocalObj(aml, ref idx, out result, s))
                return true;

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseLocalObj(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            byte b = ReadByte(aml, ref idx);
            if (b >= 0x60 && b <= 0x67)
            {
                result = new ACPIObject(ACPIObject.DataType.Local, (int)(b - 0x60));
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseArgObj(byte[] aml, ref int idx, out ACPIObject result, State s)
        {
            int old_idx = idx;

            byte b = ReadByte(aml, ref idx);
            if (b >= 0x68 && b <= 0x6e)
            {
                result = new ACPIObject(ACPIObject.DataType.Arg, (int)(b - 0x68));
                return true;
            }

            idx = old_idx;
            result = null;
            return false;
        }

        private bool ParseDefMethod(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            int pkg_length;
            int pkg_length_offset;
            ACPIName MethodName;
            byte MethodFlags;

            if (ParseByte(aml, ref idx, 0x14) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseNameString(aml, ref idx, out MethodName, s) &&
                ParseByteData(aml, ref idx, out MethodFlags))
            {
                byte arg_count = (byte)(MethodFlags & 0x7);
                byte serialize = (byte)(MethodFlags >> 3 & 0x1);
                byte synclevel = (byte)(MethodFlags >> 4 & 0xf);

                ACPIObject.MethodData md = new ACPIObject.MethodData();
                md.AML = aml;
                md.ArgCount = (int)arg_count;
                int method_end = pkg_length_offset + pkg_length;
                md.Offset = idx;
                md.Length = method_end - idx;
                md.Serialized = (serialize == 1) ? true : false;
                md.SyncLevel = (int)synclevel;

                ACPIObject m = new ACPIObject(ACPIObject.DataType.Method, md);
                m.Name = MethodName;

                Objects[MethodName] = m;
                idx = method_end;
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDefField(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            int pkg_length;
            int pkg_length_offset;
            ACPIName RegionName;
            byte FieldFlags;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x81) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseNameString(aml, ref idx, out RegionName, s) &&
                ParseByteData(aml, ref idx, out FieldFlags))
            {
                int end_fields = pkg_length_offset + pkg_length;

                ACPIObject op_region = Objects[RegionName];

                ACPIObject.FieldUnitData.AccessType cur_access;
                ACPIObject.FieldUnitData.AccessAttribType cur_attrib =
                    ACPIObject.FieldUnitData.AccessAttribType.Undefined;
                ACPIObject.FieldUnitData.LockRuleType cur_lr = ACPIObject.FieldUnitData.LockRuleType.NoLock;
                ACPIObject.FieldUnitData.UpdateRuleType cur_ur = ACPIObject.FieldUnitData.UpdateRuleType.Preserve;
                int cur_offset = 0;

                byte at_bits = (byte)(FieldFlags & 0xf);
                byte lr_bits = (byte)(FieldFlags >> 4 & 0x1);
                byte ur_bits = (byte)(FieldFlags >> 5 & 0x3);
                byte res_bits = (byte)(FieldFlags >> 7 & 0x1);

                if (res_bits != 0)
                    throw new Exception("Reserved bits not set to zero in DefField");
                cur_access = ParseAccessType(at_bits);
                switch (lr_bits)
                {
                    case 0:
                        cur_lr = ACPIObject.FieldUnitData.LockRuleType.NoLock;
                        break;
                    case 1:
                        cur_lr = ACPIObject.FieldUnitData.LockRuleType.Lock;
                        break;
                }
                switch (ur_bits)
                {
                    case 0:
                        cur_ur = ACPIObject.FieldUnitData.UpdateRuleType.Preserve;
                        break;
                    case 1:
                        cur_ur = ACPIObject.FieldUnitData.UpdateRuleType.WriteAsOnes;
                        break;
                    case 2:
                        cur_ur = ACPIObject.FieldUnitData.UpdateRuleType.WriteAsZeros;
                        break;
                }

                while (idx < end_fields)
                {
                    int reserved_bits;
                    int rb_length;
                    byte at, aa;
                    ACPIName n;

                    if (ParseByte(aml, ref idx, 0x00) &&
                        ParsePkgLength(aml, ref idx, out reserved_bits, out rb_length))
                        cur_offset += reserved_bits;
                    else if (ParseByte(aml, ref idx, 0x01) &&
                        ParseByteData(aml, ref idx, out at) &&
                        ParseByteData(aml, ref idx, out aa))
                    {
                        cur_access = ParseAccessType(at);
                        cur_attrib = ParseAccessAttribType(aa);
                    }
                    else if (ParseNameString(aml, ref idx, out n, s) &&
                        ParsePkgLength(aml, ref idx, out reserved_bits, out rb_length))
                    {
                        ACPIObject.FieldUnitData fud = new ACPIObject.FieldUnitData();
                        fud.Access = cur_access;
                        fud.AccessAttrib = cur_attrib;
                        fud.BitOffset = cur_offset;
                        fud.BitLength = reserved_bits;
                        fud.OpRegion = op_region;
                        fud.UpdateRule = cur_ur;
                        fud.LockRule = cur_lr;

                        ACPIObject f = new ACPIObject(ACPIObject.DataType.FieldUnit, fud);
                        f.Name = n;

                        Objects[n] = f;

                        cur_offset += reserved_bits;
                    }
                    else
                        throw new Exception("Invalid FieldUnitData");
                }
                return true;
            }

            idx = old_idx;
            return false;
        }

        private ACPIObject.FieldUnitData.AccessAttribType ParseAccessAttribType(byte aa_bits)
        {
            switch (aa_bits)
            {
                case 0x00:
                    return ACPIObject.FieldUnitData.AccessAttribType.Undefined;
                case 0x02:
                    return ACPIObject.FieldUnitData.AccessAttribType.SMBQuick;
                case 0x04:
                    return ACPIObject.FieldUnitData.AccessAttribType.SMBSendReceive;
                case 0x06:
                    return ACPIObject.FieldUnitData.AccessAttribType.SMBByte;
                case 0x08:
                    return ACPIObject.FieldUnitData.AccessAttribType.SMBWord;
                case 0x0a:
                    return ACPIObject.FieldUnitData.AccessAttribType.SMBBlock;
                case 0x0c:
                    return ACPIObject.FieldUnitData.AccessAttribType.SMBProcessCall;
                case 0x0d:
                    return ACPIObject.FieldUnitData.AccessAttribType.SMBBlockProcessCall;
                default:
                    throw new Exception("Unsupported AccessAttrib: " + aa_bits.ToString());
            }
        }

        private ACPIObject.FieldUnitData.AccessType ParseAccessType(byte at_bits)
        {
            switch (at_bits)
            {
                case 0:
                    return ACPIObject.FieldUnitData.AccessType.AnyAcc;
                case 1:
                    return ACPIObject.FieldUnitData.AccessType.ByteAcc;
                case 2:
                    return ACPIObject.FieldUnitData.AccessType.WordAcc;
                case 3:
                    return ACPIObject.FieldUnitData.AccessType.DWordAcc;
                case 4:
                    return ACPIObject.FieldUnitData.AccessType.QWordAcc;
                case 5:
                    return ACPIObject.FieldUnitData.AccessType.BufferAcc;
                default:
                    throw new Exception("Unsupported access type: " + at_bits.ToString());
            }
        }

        private bool ParseDefProcessor(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            int pkg_length;
            int pkg_length_offset;
            ACPIName ProcessorName;
            byte ProcID;
            uint PblkAddr;
            byte PblkLen;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x83) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseNameString(aml, ref idx, out ProcessorName, s) &&
                ParseByteData(aml, ref idx, out ProcID) &&
                ParseDwordData(aml, ref idx, out PblkAddr) &&
                ParseByteData(aml, ref idx, out PblkLen))
            {
                ACPIObject.ProcessorData pd = new ACPIObject.ProcessorData();
                pd.ID = (ulong)ProcID;
                pd.BlkAddr = (ulong)PblkAddr;
                pd.BlkLen = (ulong)PblkLen;

                ACPIObject p = new ACPIObject(ACPIObject.DataType.Processor, pd);
                p.Name = ProcessorName;

                Objects[ProcessorName] = p;

                ACPIName old_scope = s.Scope.Clone();
                int scope_end = pkg_length_offset + pkg_length;
                bool ret = ParseTermList(aml, ref idx, scope_end - idx, ProcessorName.Clone());
                s.Scope.CloneFrom(old_scope);

                Processors[ProcessorName] = p;

                if (ret)
                    return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDefMutex(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            byte SyncData;
            ACPIName MutexName;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x01) &&
                ParseNameString(aml, ref idx, out MutexName, s))
            {
                SyncData = ReadByte(aml, ref idx);
                ACPIObject m = new ACPIObject(ACPIObject.DataType.Mutex, SyncData);
                m.Name = MutexName;
                Objects[m.Name] = m;
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDefIndexField(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            int pkg_length;
            int pkg_length_offset;
            ACPIName IndexName, DataName;
            byte FieldFlags;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x86) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseNameString(aml, ref idx, out IndexName, s) &&
                ParseNameString(aml, ref idx, out DataName, s) &&
                ParseByteData(aml, ref idx, out FieldFlags))
            {
                int end_fields = pkg_length_offset + pkg_length;

                ACPIObject index = FindObject(IndexName);
                ACPIObject data = FindObject(DataName);

                ACPIObject.FieldUnitData.AccessType cur_access;
                ACPIObject.FieldUnitData.AccessAttribType cur_attrib =
                    ACPIObject.FieldUnitData.AccessAttribType.Undefined;
                ACPIObject.FieldUnitData.LockRuleType cur_lr = ACPIObject.FieldUnitData.LockRuleType.NoLock;
                ACPIObject.FieldUnitData.UpdateRuleType cur_ur = ACPIObject.FieldUnitData.UpdateRuleType.Preserve;
                int cur_offset = 0;

                byte at_bits = (byte)(FieldFlags & 0xf);
                byte lr_bits = (byte)(FieldFlags >> 4 & 0x1);
                byte ur_bits = (byte)(FieldFlags >> 5 & 0x3);
                byte res_bits = (byte)(FieldFlags >> 7 & 0x1);

                if (res_bits != 0)
                    throw new Exception("Reserved bits not set to zero in DefField");
                cur_access = ParseAccessType(at_bits);
                switch (lr_bits)
                {
                    case 0:
                        cur_lr = ACPIObject.FieldUnitData.LockRuleType.NoLock;
                        break;
                    case 1:
                        cur_lr = ACPIObject.FieldUnitData.LockRuleType.Lock;
                        break;
                }
                switch (ur_bits)
                {
                    case 0:
                        cur_ur = ACPIObject.FieldUnitData.UpdateRuleType.Preserve;
                        break;
                    case 1:
                        cur_ur = ACPIObject.FieldUnitData.UpdateRuleType.WriteAsOnes;
                        break;
                    case 2:
                        cur_ur = ACPIObject.FieldUnitData.UpdateRuleType.WriteAsZeros;
                        break;
                }

                while (idx < end_fields)
                {
                    int reserved_bits;
                    int rb_length;
                    byte at, aa;
                    ACPIName n;

                    if (ParseByte(aml, ref idx, 0x00) &&
                        ParsePkgLength(aml, ref idx, out reserved_bits, out rb_length))
                        cur_offset += reserved_bits;
                    else if (ParseByte(aml, ref idx, 0x01) &&
                        ParseByteData(aml, ref idx, out at) &&
                        ParseByteData(aml, ref idx, out aa))
                    {
                        cur_access = ParseAccessType(at);
                        cur_attrib = ParseAccessAttribType(aa);
                    }
                    else if (ParseNameString(aml, ref idx, out n, s) &&
                        ParsePkgLength(aml, ref idx, out reserved_bits, out rb_length))
                    {
                        ACPIObject.IndexFieldUnitData fud = new ACPIObject.IndexFieldUnitData();
                        fud.Access = cur_access;
                        fud.AccessAttrib = cur_attrib;
                        fud.BitOffset = cur_offset;
                        fud.BitLength = reserved_bits;
                        fud.Index = index;
                        fud.Data = data;
                        fud.UpdateRule = cur_ur;
                        fud.LockRule = cur_lr;

                        ACPIObject f = new ACPIObject(ACPIObject.DataType.FieldUnit, fud);
                        f.Name = n;

                        Objects[n] = f;

                        cur_offset += reserved_bits;
                    }
                    else
                        throw new Exception("Invalid IndexFieldUnitData");
                }
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDefDevice(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            int pkg_length;
            int pkg_length_offset;
            ACPIName DeviceName;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x82) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseNameString(aml, ref idx, out DeviceName, s))
            {
                ACPIObject d = new ACPIObject(ACPIObject.DataType.Device, null);
                d.Name = DeviceName;
                Objects[DeviceName] = d;

                ACPIName old_scope = s.Scope.Clone();
                int device_end = pkg_length_offset + pkg_length;
                bool ret = ParseTermList(aml, ref idx, device_end - idx, DeviceName.Clone());
                s.Scope.CloneFrom(old_scope);

                System.Diagnostics.Debugger.Log(0, "acpipc", "Found device: " + DeviceName.ToString());
                Devices[DeviceName] = d;

                if (ret)
                    return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDefDataRegion(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x88))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefCreateWordField(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            ACPIObject SourceBuff;
            ACPIObject ByteIndex;
            ACPIName Name;

            if (ParseByte(aml, ref idx, 0x8b) &&
                ParseTermArg(aml, ref idx, out SourceBuff, s) &&
                ParseTermArg(aml, ref idx, out ByteIndex, s) &&
                ParseNameString(aml, ref idx, out Name, s))
            {
                ACPIObject.BufferFieldData bfd = new ACPIObject.BufferFieldData();
                bfd.Buffer = SourceBuff;
                bfd.BitLength = 16;
                bfd.BitOffset = (int)(ByteIndex.IntegerData * 8);

                ACPIObject n = new ACPIObject(ACPIObject.DataType.BufferField, bfd);
                n.Name = Name;
                Objects[Name] = n;
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDefCreateQWordField(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x8f))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefCreateField(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x13))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefCreateDWordField(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            ACPIObject SourceBuff;
            ACPIObject ByteIndex;
            ACPIName Name;

            if (ParseByte(aml, ref idx, 0x8a) &&
                ParseTermArg(aml, ref idx, out SourceBuff, s) &&
                ParseTermArg(aml, ref idx, out ByteIndex, s) &&
                ParseNameString(aml, ref idx, out Name, s))
            {
                ACPIObject.BufferFieldData bfd = new ACPIObject.BufferFieldData();
                bfd.Buffer = SourceBuff;
                bfd.BitLength = 32;
                bfd.BitOffset = (int)(ByteIndex.IntegerData * 8);

                ACPIObject n = new ACPIObject(ACPIObject.DataType.BufferField, bfd);
                n.Name = Name;
                Objects[Name] = n;
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDefCreateByteField(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x8c))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefCreateBitField(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x8d))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        private bool ParseDefBankField(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x87))
                throw new NotImplementedException();

            idx = old_idx;
            return false;
        }

        bool ParseNameSpaceModifierObj(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;

            if (ParseDefAlias(aml, ref idx, s) ||
                ParseDefName(aml, ref idx, s) ||
                ParseDefScope(aml, ref idx, s))
                return true;

            idx = old_idx;
            return false;
        }

        private bool ParseDefScope(byte[] aml, ref int idx, State s)
        {
            int old_idx = idx;
            int pkg_length;
            int pkg_length_offset;
            ACPIName Name;
            ACPIName old_scope = s.Scope.Clone();

            if (ParseByte(aml, ref idx, 0x10) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseNameString(aml, ref idx, out Name, s))
            {
                int scope_end = pkg_length_offset + pkg_length;
                if (ParseTermList(aml, ref idx, scope_end - idx, Name))
                {
                    s.Scope.CloneFrom(old_scope);
                    return true;
                }
            }

            idx = old_idx;
            s.Scope.CloneFrom(old_scope);
            return false;
        }

        bool ParseDefAlias(byte[] aml, ref int idx, State s)
        {
            ACPIName SourceObject, AliasObject;
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x06) &&
                ParseNameString(aml, ref idx, out SourceObject, s) &&
                ParseNameString(aml, ref idx, out AliasObject, s))
            {
                Objects[AliasObject] = Objects[SourceObject];
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseNameString(byte[] aml, ref int idx, out ACPIName namestring, State s)
        {
            int old_idx = idx;

            ACPIName n = ACPIName.Clone(s.Scope);

            if (ParseRootCharNamePath(aml, ref idx, n, s) ||
                ParsePrefixPathNamePath(aml, ref idx, n, s))
            {
                namestring = n;
                return true;
            }

            idx = old_idx;
            namestring = null;
            return false;
        }

        private bool ParsePrefixPathNamePath(byte[] aml, ref int idx, ACPIName n, State s)
        {
            int old_idx = idx;
            ACPIName old_name = n.Clone();

            while (ParseByte(aml, ref idx, 0x5e))
                n.Prefix();

            if (ParseNamePath(aml, ref idx, n, s))
                return true;

            n.CloneFrom(old_name);
            idx = old_idx;
            return false;
        }

        private bool ParseRootCharNamePath(byte[] aml, ref int idx, ACPIName n, State s)
        {
            int old_idx = idx;
            ACPIName old_name = n.Clone();

            if (ParseRootChar(aml, ref idx, n) &&
                ParseNamePath(aml, ref idx, n, s))
                return true;

            n.CloneFrom(old_name);
            idx = old_idx;
            return false;
        }

        private bool ParseNamePath(byte[] aml, ref int idx, ACPIName n, State s)
        {
            int old_idx = idx;
            ACPIName old_name = n.Clone();

            if (ParseNameSeg(aml, ref idx, n, s) ||
                ParseDualNamePath(aml, ref idx, n, s) ||
                ParseMultiNamePath(aml, ref idx, n, s) ||
                ParseNullName(aml, ref idx, n, s))
            {
                return true;
            }

            n.CloneFrom(old_name);
            idx = old_idx;
            return false;
        }

        private bool ParseNullName(byte[] aml, ref int idx, ACPIName n, State s)
        {
            if (ParseByte(aml, ref idx, 0x00))
            {
                n.Null();
                return true;
            }

            return false;
        }

        private bool ParseMultiNamePath(byte[] aml, ref int idx, ACPIName n, State s)
        {
            int old_idx = idx;
            ACPIName old_name = n.Clone();
            byte SegCount;

            bool success = true;

            if (ParseByte(aml, ref idx, 0x2f))
            {
                SegCount = aml[idx++];

                for (byte i = 0; i < SegCount; i++)
                {
                    if (!ParseNameSeg(aml, ref idx, n, s))
                    {
                        success = false;
                        break;
                    }
                }
            }
            else
                success = false;

            if (success)
                return true;

            n.CloneFrom(old_name);
            idx = old_idx;
            return false;
        }

        private bool ParseDualNamePath(byte[] aml, ref int idx, ACPIName n, State s)
        {
            int old_idx = idx;
            ACPIName old_name = n.Clone();

            if (ParseByte(aml, ref idx, 0x2e) &&
                ParseNameSeg(aml, ref idx, n, s) &&
                ParseNameSeg(aml, ref idx, n, s))
            {
                return true;
            }

            n.CloneFrom(old_name);
            idx = old_idx;
            return false;
        }

        private bool ParseNameSeg(byte[] aml, ref int idx, ACPIName n, State s)
        {
            int old_idx = idx;
            ACPIName old_name = n.Clone();
            char a, b, c, d;

            if (ParseLeadNameChar(aml, ref idx, out a) &&
                ParseNameChar(aml, ref idx, out b) &&
                ParseNameChar(aml, ref idx, out c) &&
                ParseNameChar(aml, ref idx, out d))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(a); sb.Append(b); sb.Append(c); sb.Append(d);
                n.NameSeg(sb.ToString());
                return true;
            }

            n.CloneFrom(old_name);
            idx = old_idx;
            return false;
        }

        private bool ParseNameChar(byte[] aml, ref int idx, out char a)
        {
            byte b = aml[idx];

            if ((b >= 0x41 && b <= 0x5a) || b == 0x5f || (b >= 0x30 && b <= 0x39))
            {
                idx++;
                a = (char)b;
                return true;
            }

            a = (char)0;
            return false;
        }

        private bool ParseLeadNameChar(byte[] aml, ref int idx, out char a)
        {
            byte b = aml[idx];

            if ((b >= 0x41 && b <= 0x5a) || b == 0x5f)
            {
                idx++;
                a = (char)b;
                return true;
            }

            a = (char)0;
            return false;
        }

        private bool ParseRootChar(byte[] aml, ref int idx, ACPIName n)
        {
            if (ParseByte(aml, ref idx, 0x5c))
            {
                n.Root();
                return true;
            }
            return false;
        }

        private bool ParseByte(byte[] aml, ref int idx, byte b)
        {
            if (aml[idx] == b)
            {
                idx++;
                return true;
            }
            return false;
        }

        private bool ParseByteData(byte[] aml, ref int idx, out byte b)
        {
            b = aml[idx++];
            return true;
        }

        private bool ParseDwordData(byte[] aml, ref int idx, out uint b)
        {
            b = BitConverter.ToUInt32(aml, idx);
            idx += 4;
            return true;
        }

        bool ParseDefName(byte[] aml, ref int idx, State s)
        {
            ACPIName ObjectName;
            ACPIObject Object;
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x08) &&
                ParseNameString(aml, ref idx, out ObjectName, s) &&
                ParseDataRefObject(aml, ref idx, out Object, s))
            {
                Object.Name = ObjectName;
                Objects[ObjectName] = Object;
                return true;
            }

            idx = old_idx;
            return false;
        }

        private bool ParseDataRefObject(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            int old_idx = idx;

            if (ParseDataObject(aml, ref idx, out Object, s) ||
                ParseObjectReference(aml, ref idx, out Object, s) ||
                ParseDDBHandle(aml, ref idx, out Object, s))
                return true;

            idx = old_idx;
            return false;
        }

        private bool ParseObjectReference(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            ACPIName ns;
            int old_idx = idx;

            if (ParseNameString(aml, ref idx, out ns, s) &&
                ns.IsNull == false)
            {
                Object = new ACPIObject(ACPIObject.DataType.ObjectReference, new ACPIObject.ObjRefData { ObjectName = ns, n = this });
                return true;
            }

            idx = old_idx;
            Object = null;
            return false;
        }

        private bool ParseDDBHandle(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            throw new NotImplementedException();
        }

        private bool ParseDataObject(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            int old_idx = idx;

            if (ParseComputationalData(aml, ref idx, out Object, s) ||
                ParseDefPackage(aml, ref idx, out Object, s) ||
                ParseDefVarPackage(aml, ref idx, out Object, s))
                return true;

            idx = old_idx;
            return false;
        }

        private bool ParseDefVarPackage(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x13))
                throw new NotImplementedException();

            idx = old_idx;
            Object = null;
            return false;
        }

        private bool ParseDefPackage(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            int old_idx = idx;
            int pkg_length;
            int pkg_length_offset;
            byte NumElements;

            if (ParseByte(aml, ref idx, 0x12) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseByteData(aml, ref idx, out NumElements))
            {
                ACPIObject[] pkg = new ACPIObject[NumElements];
                for (byte i = 0; i < NumElements; i++)
                    pkg[i] = new ACPIObject(ACPIObject.DataType.Uninitialized, null);

                Object = new ACPIObject(ACPIObject.DataType.Package, pkg);

                int pkg_end = pkg_length_offset + pkg_length;
                int cur_pkg_idx = 0;
                while (idx < pkg_end)
                {
                    if (ParseDataRefObject(aml, ref idx, out pkg[cur_pkg_idx], s))
                    {
                        cur_pkg_idx++;
                        continue;
                    }

                    ACPIName ns;
                    if (ParseNameString(aml, ref idx, out ns, s))
                    {
                        ACPIObject oref = new ACPIObject(ACPIObject.DataType.ObjectReference, new ACPIObject.ObjRefData { ObjectName = ns, n = this });
                        pkg[cur_pkg_idx++] = oref;
                        continue;
                    }

                    throw new Exception("Invalid package contents");
                }

                return true;
            }

            idx = old_idx;
            Object = null;
            return false;
        }

        private bool ParsePkgLength(byte[] aml, ref int idx, out int pkg_length, out int pkg_length_offset)
        {
            int old_idx = idx;
            pkg_length_offset = old_idx;

            byte b1 = ReadByte(aml, ref idx);
            if ((b1 & 0xc0) == 0)
            {
                pkg_length = (int)b1;
                return true;
            }
            else if ((b1 & 0x30) != 0)
            {
                idx = old_idx;
                pkg_length = 0;
                return false;
            }
            else
            {
                int extra_bytes = (int)(b1 >> 6);
                uint ret = (uint)(b1 & 0xf);
                int cur_bit_offset = 4;

                for (int i = 0; i < extra_bytes; i++)
                {
                    uint b = (uint)ReadByte(aml, ref idx);
                    b <<= cur_bit_offset;
                    ret |= b;
                    cur_bit_offset += 8;
                }

                pkg_length = (int)ret;
                return true;
            }
        }

        private bool ParseComputationalData(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            int old_idx = idx;

            if (ParseByteConst(aml, ref idx, out Object) ||
                ParseWordConst(aml, ref idx, out Object) ||
                ParseDWordConst(aml, ref idx, out Object) ||
                ParseQWordConst(aml, ref idx, out Object) ||
                ParseString(aml, ref idx, out Object, s) ||
                ParseConstObj(aml, ref idx, out Object, s) ||
                ParseRevisionOp(aml, ref idx, out Object, s) ||
                ParseDefBuffer(aml, ref idx, out Object, s))
                return true;

            idx = old_idx;
            return false;
        }

        private bool ParseDefBuffer(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            int old_idx = idx;
            int pkg_length;
            int pkg_length_offset;
            ACPIObject BufferSize;

            if (ParseByte(aml, ref idx, 0x11) &&
                ParsePkgLength(aml, ref idx, out pkg_length, out pkg_length_offset) &&
                ParseTermArg(aml, ref idx, out BufferSize, s))
            {
                BufferSize = BufferSize.Evaluate(mi, s, this);
                if ((BufferSize.Type != ACPIObject.DataType.Integer) &&
                    (BufferSize.Type != ACPIObject.DataType.Uninitialized))
                    throw new Exception("BufferSize does not evaluate to an Integer");

                int buffer_end = pkg_length_offset + pkg_length;
                int initializer_len = buffer_end - idx;
                int buffer_len = 0;
                if (BufferSize.Type == ACPIObject.DataType.Integer)
                    buffer_len = (int)BufferSize.IntegerData;
                if (initializer_len > buffer_len)
                    buffer_len = initializer_len;

                byte[] buf = new byte[buffer_len];
                Array.Copy(aml, idx, buf, 0, initializer_len);

                idx += initializer_len;

                Object = new ACPIObject(ACPIObject.DataType.Buffer, buf);
                return true;
            }

            idx = old_idx;
            Object = null;
            return false;
        }

        private bool ParseRevisionOp(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            int old_idx = idx;

            if (ParseByte(aml, ref idx, 0x5b) &&
                ParseByte(aml, ref idx, 0x30))
            {
                Object = new ACPIObject(ACPIObject.DataType.Integer, Revision);
                return true;
            }

            Object = null;
            return false;
        }

        private bool ParseConstObj(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            int old_idx = idx;

            if (ParseZeroOp(aml, ref idx, out Object, s) ||
                ParseOneOp(aml, ref idx, out Object, s) ||
                ParseOnesOp(aml, ref idx, out Object, s))
                return true;

            idx = old_idx;
            return false;
        }

        private bool ParseZeroOp(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            if (ParseByte(aml, ref idx, 0x00))
            {
                Object = new ACPIObject(ACPIObject.DataType.Integer, Zero);
                return true;
            }
            Object = null;
            return false;
        }

        private bool ParseOneOp(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            if (ParseByte(aml, ref idx, 0x01))
            {
                Object = new ACPIObject(ACPIObject.DataType.Integer, One);
                return true;
            }
            Object = null;
            return false;
        }

        private bool ParseOnesOp(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            if (ParseByte(aml, ref idx, 0xff))
            {
                Object = new ACPIObject(ACPIObject.DataType.Integer, Ones);
                return true;
            }
            Object = null;
            return false;
        }

        private bool ParseString(byte[] aml, ref int idx, out ACPIObject Object, State s)
        {
            int old_idx = idx;
            string str;

            if (ParseByte(aml, ref idx, 0x0d) &&
                ParseAsciiCharListNullChar(aml, ref idx, out str))
            {
                Object = new ACPIObject(ACPIObject.DataType.String, str);
                return true;
            }

            idx = old_idx;
            Object = null;
            return false;
        }

        private bool ParseAsciiCharListNullChar(byte[] aml, ref int idx, out string str)
        {
            int old_idx = idx;
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                byte b = ReadByte(aml, ref idx);
                if (b == 0x0)
                    break;
                else if (b >= 0x1 && b <= 0x7f)
                    sb.Append((char)b);
                else
                {
                    idx = old_idx;
                    str = null;
                    return false;
                }
            }

            str = sb.ToString();
            return true;
        }

        private bool ParseByteConst(byte[] aml, ref int idx, out ACPIObject Object)
        {
            if (ParseByte(aml, ref idx, 0x0a))
            {
                Object = new ACPIObject(ACPIObject.DataType.Integer, (ulong)ReadByte(aml, ref idx));
                return true;
            }
            Object = null;
            return false;
        }

        private bool ParseWordConst(byte[] aml, ref int idx, out ACPIObject Object)
        {
            if (ParseByte(aml, ref idx, 0x0b))
            {
                Object = new ACPIObject(ACPIObject.DataType.Integer, (ulong)ReadWord(aml, ref idx));
                return true;
            }
            Object = null;
            return false;
        }

        private bool ParseDWordConst(byte[] aml, ref int idx, out ACPIObject Object)
        {
            if (ParseByte(aml, ref idx, 0x0c))
            {
                Object = new ACPIObject(ACPIObject.DataType.Integer, (ulong)ReadDWord(aml, ref idx));
                return true;
            }
            Object = null;
            return false;
        }

        private bool ParseQWordConst(byte[] aml, ref int idx, out ACPIObject Object)
        {
            if (ParseByte(aml, ref idx, 0x0e))
            {
                Object = new ACPIObject(ACPIObject.DataType.Integer, (ulong)ReadQWord(aml, ref idx));
                return true;
            }
            Object = null;
            return false;
        }
    }
}
