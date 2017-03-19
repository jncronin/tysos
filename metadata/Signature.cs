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

namespace metadata
{
    partial class MetadataStream
    {
        /** <summary>Get local var count and increment idx to point
            to first type</summary> */
        public int GetLocalVarCount(ref int idx)
        {
            if (idx == 0)
                return 0;

            int old_idx = idx;

            // Parse blob length
            SigReadUSCompressed(ref idx);

            byte locals = sh_blob.di.ReadByte(idx++);
            if(locals != 0x7)
            {
                // not a locals directive
                throw new Exception(".locals not found");
            }

            return (int)SigReadUSCompressed(ref idx);
        }

        /** <summary>Get local var by index</summary> */
        public TypeSpec GetLocalVar(int lvar_sig_idx, int lvar_idx,
            TypeSpec[] gtparams, TypeSpec[] gmparams)
        {
            int lvar_count = GetLocalVarCount(ref lvar_sig_idx);
            if (lvar_idx >= lvar_count)
                throw new Exception("Invalid local var index: " + lvar_idx.ToString());
            TypeSpec ts = null;
            for (int i = 0; i <= lvar_idx; i++)
                ts = GetTypeSpec(ref lvar_sig_idx, gtparams, gmparams);
            return ts;
        }

        /** <summary>Get number of parameters in a MethodDefSig,
            excluding the this pointer</summary> */
        public int GetMethodDefSigParamCount(int idx)
        {
            // Parse blob length
            SigReadUSCompressed(ref idx);

            // Parse calling convention
            byte cc = sh_blob.di.ReadByte(idx++);
            if ((cc & 0x10) == 0x10)
                SigReadUSCompressed(ref idx); // GenericParamCount

            return (int)SigReadUSCompressed(ref idx);
        }

        /** <summary>Get number of parameters in a MethodDefSig,
            including the this pointer</summary> */
        public int GetMethodDefSigParamCountIncludeThis(int idx)
        {
            // Parse blob length
            SigReadUSCompressed(ref idx);

            // Parse calling convention
            byte cc = sh_blob.di.ReadByte(idx++);
            if ((cc & 0x10) == 0x10)
                SigReadUSCompressed(ref idx); // GenericParamCount

            int param_count = 0;
            if ((cc & 0x60) == 0x20)
                param_count++;      // HasThis and not ExplicitThis

            return param_count + (int)SigReadUSCompressed(ref idx);
        }

        /** <summary>Does the method have a non-explicit this pointer?</summary> */
        public bool GetMethodDefSigHasNonExplicitThis(int idx)
        {
            // Parse blob length
            SigReadUSCompressed(ref idx);

            // Parse calling convention
            byte cc = sh_blob.di.ReadByte(idx++);
            return (cc & 0x60) == 0x20;
        }

        /** <summary>Get index of type in a field sig</summary> */
        public int GetFieldSigTypeIndex(int idx)
        {
            // Parse blob length
            SigReadUSCompressed(ref idx);

            var fid = SigReadUSCompressed(ref idx);
            if (fid != 0x6)
                throw new Exception("Not a field signature");

            // Skip custom mod
            bool is_req;
            uint token;
            while (GetRetTypeCustomMod(ref idx, out is_req, out token)) ;

            return idx;
        }

        /** <summary>Get return type blob index</summary> */
        public int GetMethodDefSigRetTypeIndex(int idx)
        {
            // Parse blob length
            SigReadUSCompressed(ref idx);

            // Parse calling convention
            byte cc = sh_blob.di.ReadByte(idx++);
            if ((cc & 0x10) == 0x10)
                SigReadUSCompressed(ref idx); // GenericParamCount

            // Parse param count
            SigReadUSCompressed(ref idx);

            return idx;
        }

        /** <summary>Read a custom mod</summary> */
        public bool GetRetTypeCustomMod(ref int idx, out bool is_req, out uint token)
        {
            byte b = sh_blob.di.ReadByte(idx);
            if (b == 0x1f)
                is_req = true;
            else if (b == 0x20)
                is_req = false;
            else
            {
                is_req = false;
                token = 0;
                return false;
            }

            idx++;
            token = SigReadUSCompressed(ref idx);
            return true;
        }

        /** <summary>Read field type</summary> */
        public TypeSpec GetFieldType(MethodSpec fs, TypeSpec[] gtparams = null,
            TypeSpec[] gmparams = null)
        {
            int idx = fs.msig;
            return fs.m.GetFieldType(ref idx, gtparams, gmparams);
        }

        /** <summary>Read field type</summary> */
        public TypeSpec GetFieldType(ref int idx, TypeSpec[] gtparams = null,
            TypeSpec[] gmparams = null)
        {
            SigReadUSCompressed(ref idx);

            byte fld = sh_blob.di.ReadByte(idx++);
            if (fld != 0x6)
                throw new Exception("Not a field signature");

            bool is_req;
            uint token;
            while (GetRetTypeCustomMod(ref idx, out is_req, out token)) ;

            return GetTypeSpec(ref idx, gtparams, gmparams);
        }

        /** <summary>Read type</summary> */
        public int GetType(ref int idx, out uint token)
        {
            byte b = sh_blob.di.ReadByte(idx++);
            token = 0;
            switch(b)
            {
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                case 0x09:
                case 0x0a:
                case 0x0b:
                case 0x0c:
                case 0x0d:
                case 0x0e:
                    return b;

                case 0x0f:
                case 0x10:
                case 0x11:
                case 0x12:
                    token = SigReadUSCompressed(ref idx);
                    return b;

                case 0x16:
                case 0x18:
                case 0x19:
                    return b;

                case 0x1c:
                case 0x1d:
                    return b;

                default:
                    throw new NotImplementedException();
            }
        }

        // Internal functions that parse a bit of the signature
        internal uint SigReadUSCompressed(ref int idx, bool us = false)
        {
            PEFile.StreamHeader sh = sh_blob;
            if (us)
                sh = sh_us;

            byte b1 = sh.di.ReadByte(idx++);
            if ((b1 & 0x80) == 0)
                return b1;

            byte b2 = sh.di.ReadByte(idx++);
            if ((b1 & 0xc0) == 0x80)
                return (b1 & 0x3fU) << 8 | b2;

            byte b3 = sh.di.ReadByte(idx++);
            byte b4 = sh.di.ReadByte(idx++);
            return (b1 & 0x1fU) << 24 | ((uint)b2 << 16) |
                ((uint)b3 << 8) | b4;
        }

        public static List<byte> SigWriteUSCompressed(uint val)
        {
            List<byte> ret = new List<byte>();
            if (val <= 127)
            {
                ret.Add((byte)val);
                return ret;
            }
            else if(val <= 0x3fff)
            {
                ret.Add((byte)((val >> 8) | 0x80U));
                ret.Add((byte)(val & 0xffu));
                return ret;
            }
            else if(val <= 0x1fffffff)
            {
                ret.Add((byte)((val >> 24) | 0xc0u));
                ret.Add((byte)((val >> 16) & 0xffu));
                ret.Add((byte)((val >> 8) & 0xffu));
                ret.Add((byte)(val & 0xffu));
                return ret;
            }
            throw new ArgumentOutOfRangeException("val", "too large, must be <= 0x1fffffff");
        }
    }
}
