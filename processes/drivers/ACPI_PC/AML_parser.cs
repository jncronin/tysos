/* Copyright (C) 2011 by John Cronin
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

namespace aml_interpreter
{
    partial class AML
    {
        bool int_is_64_bits = false;
        bool adding_to_namespace = true;
        bool set_atn(bool val) { bool ret = adding_to_namespace; adding_to_namespace = val; return ret; }

        List<string> Scope = new List<string>();

        internal Dictionary<string, OpRegion> OpRegions = new Dictionary<string, OpRegion>(new tysos.Program.MyGenericEqualityComparer<string>());
        internal Dictionary<string, Field.FieldEntry> Fields = new Dictionary<string, Field.FieldEntry>(new tysos.Program.MyGenericEqualityComparer<string>());
        internal Dictionary<string, Object> Objects = new Dictionary<string, Object>(new tysos.Program.MyGenericEqualityComparer<string>());
        internal Dictionary<string, Device> Devices = new Dictionary<string, Device>(new tysos.Program.MyGenericEqualityComparer<string>());
        internal DefBlockHeader root;

        byte[] ReadByteArray(int length)
        { byte[] ret = new byte[length]; for (int i = 0; i < length; i++) ret[i] = ReadByte(); return ret; }
        ulong ReadInteger()
        { if (int_is_64_bits) return ReadQWord(); else return (ulong)ReadDWord(); }

        internal class Item { }

        class Arg : Item { public int ArgNo; }
        class LocalObj : Item { public int ObjNo; }
        class DataObj : Item { }
        class ByteConst : DataObj { public byte Val; }
        class ByteArrayConst : DataObj { public byte[] Val; }
        class WordConst : DataObj { public ushort Val; }
        class DWordConst : DataObj { public uint Val; }
        class QWordConst : DataObj { public ulong Val; }
        class StringConst : DataObj { public string String; }
        class ConstObj : DataObj
        {
            public enum DataType { Zero, One, Ones }
            public DataType Type;
        }
        class GenericObject : DataObj { public Item Val; }
        class Package : DataObj { public List<Item> Items = new List<Item>(); }

        class Buffer : OpCode {
            public byte[] Init_Val;
            public Item BufSize;
            public Buffer() { Opcode = Opcodes.BufferOp; }
        }
        
        class BitField : Field.FieldEntry
        {
            public Item SourceBuffer;
            public Item BitIndex;
        }
        class ByteField : Field.FieldEntry
        {
            public Item SourceBuffer;
            public Item ByteIndex;
        }
        class FieldWithVal : Field.FieldEntry
        {
            public Item Val;
        }

        internal class Device : Object
        {
            public override string ToString()
            {
                return Name.ToString();
            }

            public List<Item> Members = new List<Item>();
            public Device() { Opcode = Opcodes.NoopOp; }
        }
        internal class Processor : Device
        {
            public int ProcessorID;
            public uint SystemIO;
        }

        class NamedObject : Object { public Item Val; public NamedObject() { Opcode = Opcodes.NameOp; } }

        class UserTermObj : OpCode
        {
            public NameString Name;
        }

        internal class Object : OpCode
        {
            public NameString Name;
            public bool AddedToNamespace = false;
        }

        internal class DefBlockHeader : Item
        {
            public uint TableSignature;
            public uint TableLength;
            public byte SpecCompliance;
            public byte CheckSum;
            public byte[] OemID;
            public byte[] OemTableID;
            public uint OemRevision;
            public uint CreatorID;
            public uint CreatorRevision;

            public List<Item> Items = new List<Item>();
        }
        internal class OpRegion : Object
        {
            public enum RegionSpaceType { SystemMemory, SystemIO, PCI_Config, EmbeddedControl, SMBus, CMOS, PciBarTarget, IPMI, Indexed }
            public RegionSpaceType RegionSpace;
            public Item RegionOffset;
            public Item RegionLen;
        }
        internal class Field : Item
        {
            public enum AccessTypeType { AnyAcc, ByteAcc, WordAcc, DWordAcc, QWordAcc, BufferAcc }
            public enum LockRuleType { NoLock, Lock }
            public enum UpdateRuleType { Preserve, WriteAsOnes, WriteAsZeros }

            public class FieldEntry : Object {
                public byte Flags;

                public AccessTypeType AccessType { get { return (AccessTypeType)(int)(Flags & 0xf); } }
                public LockRuleType LockRule { get { return (LockRuleType)(int)((Flags >> 4) & 0x1); } }
                public UpdateRuleType UpdateRule { get { return (UpdateRuleType)(int)((Flags >> 5) & 0x3); } }
            }
            public class NamedFieldEntry : FieldEntry
            {
                public NamedFieldEntry() { Opcode = Opcodes.NoopOp; }
                public NameString RegionName;
                public int BitIndex;
                public int BitSize;
            }
            public class IndexFieldEntry : FieldEntry
            {
                public IndexFieldEntry() { Opcode = Opcodes.NoopOp; }
                public NameString IndexName;
                public NameString DataName;
                public int BitIndex;
                public int BitSize;
            }
        }
        internal class Method : Object
        {
            public byte Flags;

            public List<Item> TermList = new List<Item>();

            public int ArgCount { get { return (int)(Flags & 0x7); } }
            public bool Serialized { get { return (Flags & 0x8) == 0x8; } }
            public int SyncLevel { get { return (int)(Flags >> 4); } }

            public Method() { Opcode = Opcodes.MethodOp; }
        }
        internal class NameString : Item
        {
            public List<char> Prefixes = new List<char>();
            public List<string> NameSegs = new List<string>();
            public List<string> Scope = new List<string>();

            public List<string> FullName
            {
                get
                {
                    List<string> cur_scope = new List<string>(Scope);
                    if (Prefixes.Count > 0)
                    {
                        if (Prefixes[0] == '\\')
                            cur_scope.Clear();
                        else
                        {
                            for (int i = 0; i < Prefixes.Count; i++)
                            {
                                if (Prefixes[i] == '^')
                                {
                                    if (cur_scope.Count > 0)
                                        cur_scope.RemoveAt(cur_scope.Count - 1);
                                    else
                                    {
                                        StringBuilder sb = new StringBuilder();

                                        foreach (char ch in Prefixes)
                                            sb.Append(ch);

                                        foreach (string s in Scope)
                                        {
                                            sb.Append("\\");
                                            sb.Append(s);
                                        }

                                        throw new Exception("Invalid NameString: " + sb.ToString());                                       
                                    }
                                }
                                else
                                {
                                    StringBuilder sb = new StringBuilder();

                                    foreach (char ch in Prefixes)
                                        sb.Append(ch);

                                    foreach (string s in Scope)
                                    {
                                        sb.Append("\\");
                                        sb.Append(s);
                                    }

                                    throw new Exception("Invalid NameString: " + sb.ToString());
                                }
                            }
                        }
                    }
                    foreach (string str in NameSegs)
                        cur_scope.Add(str);

                    return cur_scope;
                }
            }

            public string FullNameString
            {
                get
                {
                    StringBuilder sb = new StringBuilder();
                    List<string> fullname = FullName;
                    foreach (string s in fullname)
                    {
                        sb.Append("\\");
                        sb.Append(s);
                    }
                    return sb.ToString();
                }
            }

            public List<string> AllNameStrings
            {
                get
                {
                    List<string> ret = new List<string>();

                    ret.Add(FullNameString);

                    if ((Prefixes.Count == 0) && (NameSegs.Count == 1))
                    {
                        if (Scope.Count > 0)
                        {
                            List<string> new_scope = new List<string>(Scope);
                            
                             while (new_scope.Count > 0) {
                                new_scope.RemoveAt(new_scope.Count - 1);
                                NameString ns = new NameString { NameSegs = NameSegs, Scope = new_scope };
                                ret.Add(ns.FullNameString);
                            }
                        }
                    }

                    return ret;
                }
            }

            public override string ToString()
            {
                return FullNameString;
            }
        }

        internal class OpCode : Item
        {
            public enum Opcodes
            {
                ZeroOp, OneOp, AliasOp, NameOp, BytePrefix, WordPrefix, DWordPrefix, StringPrefix, QWordPrefix, ScopeOp, BufferOp, PackageOp,
                VarPackageOp, MethodOp, DualNamePrefix, MultiNamePrefix, NameChar, ExtOpPrefix, MutexOp, EventOp, CondRefOfOp, CreateFieldOp, LoadTableOp,
                LoadOp, StallOp, SleepOp, AcquireOp, SignalOp, WaitOp, ResetOp, ReleaseOp, FromBCDOp, ToBCDOp, UnloadOp, RevisionOp, DebugOp, FatalOp, TimerOp,
                OpRegionOp, FieldOp, DeviceOp, ProcessorOp, PowerResOp, ThermalZoneOp, IndexFieldOp, BankFieldOp, DataRegionOp, RootChar, ParentPrefixChar,
                Local0Op, Local1Op, Local2Op, Local3Op, Local4Op, Local5Op, Local6Op, Local7Op, Arg0Op, Arg1Op, Arg2Op, Arg3Op, Arg4Op, Arg5Op,
                Arg6Op, StoreOp, RefOfOp, AddOp, ConcatOp, SubtractOp, IncrementOp, DecrementOp, MultiplyOp, DivideOp, ShiftLeftOp, ShiftRightOp, AndOp, NandOp,
                OrOp, NorOp, XorOp, NotOp, FindSetLeftBitOp, FindSetRightBitOp, DerefOfOp, ConcatResOp, ModOp, NotifyOp, SizeOfOp, IndexOp, MatchOp,
                CreateDWordFieldOp, CreateWordFieldOp, CreateByteFieldOp, CreateBitFieldOp, ObjectTypeOp, CreateQWordFieldOp, LandOp, LorOp, LnotOp, LNotEqualOp,
                LLessEqualOp, LGreaterEqualOp, LEqualOp, LGreaterOp, LLessOp, ToBufferOp, ToDecimalStringOp, ToHexStringOp, ToIntegerOp, ToStringOp, CopyObjectOp,
                MidOp, ContinueOp, IfOp, ElseOp, WhileOp, NoopOp, ReturnOp, BreakOp, BreakPointOp, OnesOp
            }

            public Opcodes Opcode;

            public List<Item> Arguments = new List<Item>();
        }

        class IfElseOp : OpCode
        {
            public Item Predicate;
            public List<Item> IfList = new List<Item>();
            public List<Item> ElseList = new List<Item>();
        }

        public void LoadTable()
        {
            tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: LoadTable: Parsing table\n");
            Parse();


            // First run all code not contained within a method
            tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: LoadTable: Running root methods\n");
            RunMethod(root.Items, null);

            // We are required to unconditionally execute \_SB._INI
            if (Objects.ContainsKey("\\_SB_\\_INI"))
            {
                tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: LoadTable: Running \\_SB_\\_INI\n");
                RunMethod(Objects["\\_SB_\\_INI"] as AML.Method);
            }

            /* We then have to run the _STA method of all root devices if it exists
             * 
             * Depending on the value of the present and functional bit, we conditionally execute
             * the _INI method of all root objects and those of child objects.
             * If _STA does not exist, we assume the device is both present and functional
             * 
             * Execution as per the following table:
             * 
             * Present          Functional          Action
             * 0                0                   Do not run _INI, do not examine children
             * 0                1                   Do not run _INI, examine children
             * 1                0                   Run _INI, examine children
             * 1                1                   Run _INI, examine children
             * 
             * The above taken from ACPI 4:6.5.1
             */

            tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: LoadTable: Initializing devices\n");
            InitializeDevices(GetDevices("\\_SB_"));
        }

        private void InitializeDevices(List<AML.Device> devices)
        {
            foreach (AML.Device dev in devices)
                InitializeDevice(dev);
        }

        private void InitializeDevice(AML.Device dev)
        {
            string sta_name = dev.Name.FullNameString + "\\_STA";
            string ini_name = dev.Name.FullNameString + "\\_INI";

            bool present = true;
            bool functional = true;

            if (Objects.ContainsKey(sta_name))
            {
                AML.AMLData sta_ret = GetObject(Objects[sta_name], null);
                long status = sta_ret.Integer;

                if ((sta_ret & 0x1) == 0x1)
                    present = true;
                else
                    present = false;

                if ((sta_ret & 0x8) == 0x8)
                    functional = true;
                else
                    functional = false;
            }

            if(present)
            {
                functional = true;

                if(Objects.ContainsKey(ini_name))
                    GetObject(Objects[ini_name], null);
            }

            if (functional)
                InitializeDevices(GetDevices(dev.Name.FullNameString));
        }

        private void not_impl(byte code, int offset)
        {
            tysos.Syscalls.DebugFunctions.Write("ACPI_PC: The AML interpreter does not currently support opcode: " + code.ToString() + " at offset: " + offset.ToString() + "\n");
            tysos.Syscalls.DebugFunctions.DebugWrite("ACPI_PC: The AML interpreter does not currently support opcode: " + code.ToString() + " at offset: " + offset.ToString() + "\n");

            while (true) ;
            throw new NotImplementedException(code.ToString());
        }

        private void not_impl2(byte code, int offset)
        {
            tysos.Syscalls.DebugFunctions.Write("ACPI_PC: The AML interpreter does not currently support opcode: 5B " + code.ToString() + " at offset: " + offset.ToString() + "\n");
            tysos.Syscalls.DebugFunctions.DebugWrite("ACPI_PC: The AML interpreter does not currently support opcode: 5B " + code.ToString() + " at offset: " + offset.ToString() + "\n");

            while (true) ;
            throw new NotImplementedException(code.ToString());
        }

        private void Parse()
        {
            root = parse_defblockheader();
            parse_termlist(root.Items, (int)root.TableLength);
        }

        private void parse_termlist(List<Item> termlist, int length)
        {
            while (CurOffset() < length)
                termlist.Add(parse_item());
        }

        private Item parse_item() { return parse_item(false); }
        private Item parse_item(bool is_target)
        {
            byte b1 = ReadByte();

#if _DEBUG_PARSE
            tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: parse_item, opcode: ");
            tysos.Syscalls.DebugFunctions.DebugWrite(b1.ToString());
            tysos.Syscalls.DebugFunctions.DebugWrite(" offset: ");
            tysos.Syscalls.DebugFunctions.DebugWrite((CurOffset() - 1).ToString());
            tysos.Syscalls.DebugFunctions.DebugWrite("\n");
#endif

            if ((b1 == 0x5c) || (b1 == 0x5e) || ((b1 >= 0x41) && (b1 <= 0x5a)) || (b1 == 0x5f))
            {
                // These are all the possible start encodings of a NameString
                // UserTermObj TermArgList

                // We can only find the length of TermArgList by identifying the name as an already
                //  declared method and finding it that way

                /* Names with more than one term (or with prefixes) are relative to the local scope
                 * Names with a single term are either within the local scope or in any of the parent scopes
                 *  up to and including the root */                  

                AdjustOffset(-1);
                bool old_atn = set_atn(false);

#if _DEBUG_PARSE
                tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: UTO: begin\n");
#endif
                UserTermObj uto = new UserTermObj();

                uto.Name = ReadNameString(true);

#if _DEBUG_PARSE
                tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: UTO: name: " + uto.Name + "\n");
#endif

                Item i = null;

                foreach (string s in uto.Name.AllNameStrings)
                {
                    if (Objects.ContainsKey(s))
                        i = Objects[s];
                    if (Fields.ContainsKey(s))
                        i = Fields[s];
                }

#if _DEBUG_PARSE
                if (i == null)
                    tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: UTO: name not found\n");
                else
                    tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: UTO: name is a " + i.GetType().ToString() + "\n");
#endif

                if(i != null)
                {
                    if (i is Method)
                    {
                        Method m = i as Method;
#if _DEBUG_PARSE
                        tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: UTO: name is a method with " + m.ArgCount.ToString() + " arguments\n");
#endif
                        for (int x = 0; x < m.ArgCount; x++)
                            uto.Arguments.Add(parse_item());
                    }
                }

#if _DEBUG_PARSE
                tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: UTO: end\n");
#endif
     
                adding_to_namespace = old_atn;
                return uto;
            }
            else if ((b1 >= 0x60) && (b1 <= 0x67))
                return new LocalObj { ObjNo = (int)b1 - 0x60 };     // LocalObj
            else if ((b1 >= 0x68) && (b1 <= 0x6e))
                return new Arg { ArgNo = (int)b1 - 0x68 };          // ArgObj

            switch (b1)
            {
                case 0x00:
                    if (is_target)
                        return new StringConst { String = null };
                    else
                        return new ConstObj { Type = ConstObj.DataType.Zero };

                case 0xa3:
                    // Noop
                    return null;

                case 0x70:
                    {
                        // StoreOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.StoreOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0xa4:
                    {
                        // ReturnOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.ReturnOp };
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x87:
                    {
                        // SizeOfOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.SizeOfOp };
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x72:
                    {
                        // AddOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.AddOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x76:
                    {
                        // DecrementOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.DecrementOp };
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0xa2:
                    {
                        // WhileOp
                        bool old_atn = set_atn(false);
                        IfElseOp o = new IfElseOp { Opcode = OpCode.Opcodes.WhileOp };
                        int cur_offset = CurOffset();
                        int pkg_length = (int)ReadPkgLength();
                        o.Predicate = parse_item();
                        while (CurOffset() < (cur_offset + pkg_length))
                            o.IfList.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x83:
                    {
                        // DerefOfOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.DerefOfOp };
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x88:
                    {
                        // IndexOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.IndexOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x75:
                    {
                        // IncrementOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.IncrementOp };
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x12:
                    {
                        // PackageOp
                        Package p = new Package();
                        int cur_offset = CurOffset();
                        int pkg_length = (int)ReadPkgLength();
                        int num_elems = (int)ReadByte();
                        for (int i = 0; (i < num_elems) && (cur_offset < (CurOffset() + pkg_length)); i++)
                            p.Items.Add(parse_item());

                        return p;
                    }

                case 0x78:
                    {
                        // DivideOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.DivideOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0xa0:
                    {
                        // IfOp
#if _DEBUG_PARSE
                        tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: IfOp");
#endif

                        bool old_atn = set_atn(false);
                        IfElseOp o = new IfElseOp { Opcode = OpCode.Opcodes.IfOp };
                        int cur_offset = CurOffset();
                        int pkg_length = (int)ReadPkgLength();

#if _DEBUG_PARSE
                        tysos.Syscalls.DebugFunctions.DebugWrite(", pkg_length: " + pkg_length.ToString() + "\n");
                        tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: IfOp: begin_read_predicate\n");
#endif
                        o.Predicate = parse_item();
                        
#if _DEBUG_PARSE
                        tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: IfOp: end_read_predicate\n");
                        tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: IfOp: begin_read_if_block, cur_offset + pkg_length = " + (cur_offset + pkg_length).ToString() + "\n");
#endif

                        while (CurOffset() < (cur_offset + pkg_length))
                        {
#if _DEBUG_PARSE
                            tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: IfOp: begin_read_if_block_clause, CurOffset() = " + CurOffset().ToString() + "\n");
#endif
                            o.IfList.Add(parse_item());
#if _DEBUG_PARSE
                            tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: IfOp: end_read_if_block_clause, CurOffset() = " + CurOffset().ToString() + "\n");
#endif
                        }

                        byte b2 = ReadByte();
                        if (b2 == 0xa1)
                        {
                            // ElseOp
                            cur_offset = CurOffset();
                            pkg_length = (int)ReadPkgLength();
                            while (CurOffset() < (cur_offset + pkg_length))
                                o.ElseList.Add(parse_item());
                        }
                        else
                            AdjustOffset(-1);

                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x90:
                    {
                        // LandOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.LandOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x91:
                    {
                        // LOrOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.LorOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x93:
                    {
                        // LEqualOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.LEqualOp };

#if _DEBUG_PARSE
                        tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: LEqualOp: begin_read_arg1\n");
#endif
                        o.Arguments.Add(parse_item());
#if _DEBUG_PARSE
                        tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: LEqualOp: begin_read_arg2\n");
#endif
                        o.Arguments.Add(parse_item());
#if _DEBUG_PARSE
                        tysos.Syscalls.DebugFunctions.DebugWrite("AML_parser: LEqualOp: end\n");
#endif
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x94:
                    {
                        // LGreaterOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.LGreaterOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x95:
                    {
                        // LLessOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.LLessOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x79:
                    {
                        // ShiftLeftOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.ShiftLeftOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x7a:
                    {
                        // ShiftRightOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.ShiftRightOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x86:
                    {
                        // NotifyOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.NotifyOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x74:
                    {
                        // SubtractOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.SubtractOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x7b:
                    {
                        // AndOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.AndOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x7c:
                    {
                        // NandOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.NandOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x7d:
                    {
                        // OrOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.OrOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x7e:
                    {
                        // NorOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.NorOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x7f:
                    {
                        // XorOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.XorOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x80:
                    {
                        // NotOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.NotOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x81:
                    {
                        // FindSetLeftBitOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.FindSetLeftBitOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x82:
                    {
                        // FindSetRightBitOp
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.FindSetRightBitOp };
                        o.Arguments.Add(parse_item());
                        o.Arguments.Add(parse_item(true));
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x71:
                    {
                        // DefRefOf
                        bool old_atn = set_atn(false);
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.RefOfOp };
                        o.Arguments.Add(parse_item());
                        adding_to_namespace = old_atn;
                        return o;
                    }

                case 0x8d:
                    {
                        // CreateBitFieldOp
                        not_impl(b1, CurOffset() - 1);
                        break;
                    }

                case 0x8c:
                    {
                        // CreateByteFieldOp
                        ByteField bf = new ByteField { Opcode = OpCode.Opcodes.CreateByteFieldOp };
                        bf.SourceBuffer = parse_item();
                        bf.ByteIndex = parse_item();
                        bf.Name = ReadNameString();

                        /*if (adding_to_namespace)
                        {
                            Fields.Add(bf.Name.FullNameString, bf);
                            bf.AddedToNamespace = true;
                        }*/
                        return bf;
                    }

                case 0x8a:
                    {
                        // CreateDWordFieldOp
                        ByteField bf = new ByteField { Opcode = OpCode.Opcodes.CreateDWordFieldOp };
                        bf.SourceBuffer = parse_item();
                        bf.ByteIndex = parse_item();
                        bf.Name = ReadNameString();

                        /*if (adding_to_namespace)
                        {
                            Fields.Add(bf.Name.FullNameString, bf);
                            bf.AddedToNamespace = true;
                        }*/
                        return bf;
                    }

                case 0x8f:
                    {
                        // CreateQWordFieldOp
                        ByteField bf = new ByteField { Opcode = OpCode.Opcodes.CreateQWordFieldOp };
                        bf.SourceBuffer = parse_item();
                        bf.ByteIndex = parse_item();
                        bf.Name = ReadNameString();

                        /*if (adding_to_namespace)
                        {
                            Fields.Add(bf.Name.FullNameString, bf);
                            bf.AddedToNamespace = true;
                        }*/
                        return bf;
                    }

                case 0x8b:
                    {
                        // CreateWordFieldOp
                        ByteField bf = new ByteField { Opcode = OpCode.Opcodes.CreateWordFieldOp };
                        bf.SourceBuffer = parse_item();
                        bf.ByteIndex = parse_item();
                        bf.Name = ReadNameString();

                        /*if (adding_to_namespace)
                        {
                            Fields.Add(bf.Name.FullNameString, bf);
                            bf.AddedToNamespace = true;
                        }*/
                        return bf;
                    }

                case 0x14:
                    {
                        // MethodOp
                        bool old_atn = set_atn(false);
                        Method m = new Method();
                        int cur_offset = CurOffset();
                        int pkg_length = (int)ReadPkgLength();
                        m.Name = ReadNameString();
                        m.Flags = ReadByte();

                        List<string> old_scope = Scope;
                        Scope = new List<string>(m.Name.FullName);

                        while ((uint)CurOffset() < (cur_offset + pkg_length))
                            m.TermList.Add(parse_item());

                        adding_to_namespace = old_atn;
                        Scope = old_scope;

                        if (adding_to_namespace)
                        {
                            Objects.Add(m.Name.ToString(), m);
                            m.AddedToNamespace = true;
                        }

                        return m;
                    }

                case 0x0a:
                    return new ByteConst { Val = ReadByte() };
                case 0x0b:
                    return new WordConst { Val = ReadWord() };
                case 0x0c:
                    return new DWordConst { Val = ReadDWord() };
                case 0x0e:
                    return new QWordConst { Val = ReadQWord() };
                case 0x0d:
                    AdjustOffset(-1);
                    return parse_stringconst();
                case 0x01:
                    return new ConstObj { Type = ConstObj.DataType.One };
                case 0xff:
                    return new ConstObj { Type = ConstObj.DataType.Ones };

                case 0x11:
                    {

                        int cur_pos = CurOffset();
                        int pkg_length = (int)ReadPkgLength();

                        Buffer b = new Buffer();
                        b.BufSize = parse_item();
                        b.Init_Val = ReadByteArray(pkg_length - (CurOffset() - cur_pos));

                        return b;
                    }

                case 0x06:
                    {
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.AliasOp };
                        NameString src = ReadNameString();
                        NameString dest = ReadNameString();
                        o.Arguments.Add(src);
                        o.Arguments.Add(dest);

                        if (adding_to_namespace)
                        {
                            Objects.Add(dest.ToString(), Objects[src.ToString()]);
                        }

                        return o;
                    }

                case 0x08:
                    {
                        NameString name = ReadNameString();
                        Item data = parse_item();

                        NamedObject no = new NamedObject { Name = name, Val = data };
                        if(adding_to_namespace)
                            Objects.Add(name.ToString(), no);

                        return no;
                    }

                case 0x10:
                    {
                        OpCode o = new OpCode { Opcode = OpCode.Opcodes.ScopeOp };
                        int cur_offset = CurOffset();
                        uint pkg_length = ReadPkgLength();
                        o.Arguments.Add(new DWordConst { Val = pkg_length });
                        NameString scope_name = ReadNameString();
                        o.Arguments.Add(scope_name);

                        List<string> old_scope = Scope;
                        Scope = new List<string>(scope_name.FullName);
                        while (CurOffset() < (cur_offset + (int)pkg_length))
                            o.Arguments.Add(parse_item());
                        Scope = old_scope;

                        return o;
                    }

                case 0x5b:
                    {
                        byte b2 = ReadByte();

                        switch (b2)
                        {
                            case 0x31:
                                {
                                    // DebugOp
                                    OpCode o = new OpCode { Opcode = OpCode.Opcodes.DebugOp };
                                    return o;
                                }

                            case 0x87:
                                {
                                    // BankFieldOp
                                    not_impl2(b2, CurOffset() - 2);
                                    break;
                                }

                            case 0x13:
                                {
                                    // CreateFieldOp
                                    not_impl2(b2, CurOffset() - 2);
                                    break;
                                }

                            case 0x88:
                                {
                                    // DataRegionOp
                                    not_impl2(b2, CurOffset() - 2);
                                    break;
                                }

                            case 0x82:
                                {
                                    // DeviceOp
                                    Device d = new Device();
                                    int cur_offset = CurOffset();
                                    int pkg_length = (int)ReadPkgLength();
                                    d.Name = ReadNameString();

                                    List<string> old_scope = Scope;
                                    Scope = new List<string>(d.Name.FullName);

                                    while (CurOffset() < (cur_offset + pkg_length))
                                        d.Members.Add(parse_item());

                                    Scope = old_scope;

                                    if (adding_to_namespace)
                                    {
                                        Objects.Add(d.Name.FullNameString, d);
                                        Devices.Add(d.Name.FullNameString, d);
                                        d.AddedToNamespace = true;
                                    }
                                    return d;
                                }

                            case 0x02:
                                {
                                    // EventOp
                                    not_impl2(b2, CurOffset() - 2);
                                    break;
                                }

                            case 0x81:
                                {
                                    // FieldOp
                                    int cur_offset = CurOffset();
                                    int pkg_length = (int)ReadPkgLength();
                                    NameString RegionName = ReadNameString();
                                    byte FieldFlags = ReadByte();

                                    int offset = 0;
                                    while (CurOffset() < (cur_offset + pkg_length))
                                    {
                                        byte f_b = ReadByte();

                                        if (f_b == 0x00)
                                        {
                                            int bit_length = (int)ReadPkgLength();
                                            offset += bit_length;
                                        }
                                        else if (f_b == 0x01)
                                            not_impl2(b2, cur_offset - 2);
                                        else
                                        {
                                            AdjustOffset(-1);
                                            string name = ReadNameSeg();
                                            NameString Name = new NameString { NameSegs = new List<string> { name }, Scope = new List<string>(Scope) };
                                            int bit_length = (int)ReadPkgLength();

                                            Field.NamedFieldEntry nfe = new Field.NamedFieldEntry { Name = Name, RegionName = RegionName, Flags = FieldFlags, BitIndex = offset, BitSize = bit_length };
                                            if (adding_to_namespace)
                                            {
                                                Fields.Add(nfe.Name.ToString(), nfe);
                                                nfe.AddedToNamespace = true;
                                            }

                                            offset += bit_length;
                                        }
                                    }

                                    return null;
                                }

                            case 0x86:
                                {
                                    // IndexFieldOp

                                    int cur_offset = CurOffset();
                                    int pkg_length = (int)ReadPkgLength();

                                    NameString IndexName = ReadNameString();
                                    NameString DataName = ReadNameString();
                                    byte FieldFlags = ReadByte();

                                    int offset = 0;
                                    while (CurOffset() < (cur_offset + pkg_length))
                                    {
                                        byte b3 = ReadByte();

                                        if (b3 == 0x00)
                                        {
                                            int bit_length = (int)ReadPkgLength();
                                            offset += bit_length;
                                        }
                                        else if (b3 == 0x01)
                                            not_impl2(b2, cur_offset - 2);
                                        else
                                        {
                                            AdjustOffset(-1);
                                            string name = ReadNameSeg();
                                            NameString Name = new NameString { NameSegs = new List<string> { name }, Scope = new List<string>(Scope) };
                                            int bit_length = (int)ReadPkgLength();

                                            Field.IndexFieldEntry ife = new Field.IndexFieldEntry { DataName = DataName, Flags = FieldFlags, BitIndex = offset, IndexName = IndexName, Name = Name, BitSize = bit_length };
                                            if (adding_to_namespace)
                                            {
                                                Fields.Add(ife.Name.ToString(), ife);
                                                ife.AddedToNamespace = true;
                                            }

                                            offset += bit_length;
                                        }
                                    }

                                    return null;
                                }

                            case 0x01:
                                {
                                    // MutexOp
                                    not_impl2(b2, CurOffset() - 2);
                                    break;
                                }

                            case 0x80:
                                {
                                    // OpRegionOp

                                    OpRegion or = new OpRegion();
                                    or.Name = ReadNameString();
                                    or.RegionSpace = (OpRegion.RegionSpaceType)(int)ReadByte();
                                    or.RegionOffset = parse_item();
                                    or.RegionLen = parse_item();

                                    if (adding_to_namespace)
                                    {
                                        OpRegions.Add(or.Name.ToString(), or);
                                        or.AddedToNamespace = true;
                                    }

                                    return null;
                                }

                            case 0x84:
                                {
                                    // PowerResOp
                                    not_impl2(b2, CurOffset() - 2);
                                    break;
                                }

                            case 0x83:
                                {
                                    // ProcessorOp
                                    Processor d = new Processor();
                                    int cur_offset = CurOffset();
                                    int pkg_length = (int)ReadPkgLength();
                                    d.Name = ReadNameString();
                                    d.ProcessorID = ReadByte();
                                    d.SystemIO = ReadDWord();

                                    int pblk_len = (int)ReadByte();

                                    List<string> old_scope = Scope;
                                    Scope = new List<string>(d.Name.FullName);

                                    while (CurOffset() < (cur_offset + pkg_length))
                                        d.Members.Add(parse_item());

                                    Scope = old_scope;

                                    if (adding_to_namespace)
                                    {
                                        Objects.Add(d.Name.FullNameString, d);
                                        Devices.Add(d.Name.FullNameString, d);
                                        d.AddedToNamespace = true;
                                    }
                                    return d;
                                }

                            case 0x85:
                                {
                                    // ThermalZoneOp
                                    not_impl2(b2, CurOffset() - 2);
                                    break;
                                }

                            case 0x30:
                                return new OpCode { Opcode = OpCode.Opcodes.RevisionOp };

                            default:
                                not_impl2(b2, CurOffset() - 2);
                                break;
                        }
                        break;
                    }

                default:
                    not_impl(b1, CurOffset() - 1);
                    break;
            }

            // Shouldn't get here
            throw new Exception("Reached the end of parse_item without matching an opcode");
        }

        private Item parse_stringconst()
        {
            byte b1 = ReadByte();
            if (b1 != 0x0d)
                throw new Exception("Invalid StringConst");

            StringBuilder sb = new StringBuilder();
            b1 = ReadByte();
            while (b1 != 0x00)
            {
                sb.Append((char)b1);
                b1 = ReadByte();
            }
            return new StringConst { String = sb.ToString() };
        }

        private NameString ReadNameString() { return ReadNameString(true); }
        private NameString ReadNameString(bool use_scope)
        {
            NameString ret = new NameString();

            byte v1 = ReadByte();
            while ((v1 == 0x5c) || (v1 == 0x5e))
            {
                if (v1 == 0x5c)
                    ret.Prefixes.Add('\\');
                else
                    ret.Prefixes.Add('^');

                v1 = ReadByte();
            }

            if(v1 == 0x2e)
            {
                // DualNamePrefix
                ret.NameSegs.Add(ReadNameSeg());
                ret.NameSegs.Add(ReadNameSeg());
            }
            else if(v1 == 0x2f)
            {
                int count = (int)ReadByte();
                for(int i = 0; i < count; i++)
                    ret.NameSegs.Add(ReadNameSeg());
            }
            else if(v1 == 0x00)
                ret.NameSegs.Add(String.Empty);
            else
            {
                AdjustOffset(-1);
                ret.NameSegs.Add(ReadNameSeg());
            }

            if (use_scope)
                ret.Scope = new List<string>(Scope);
            else
                ret.Scope = new List<string>();
            return ret;
        }

        private string ReadNameSeg()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append((char)ReadByte());
            sb.Append((char)ReadByte());
            sb.Append((char)ReadByte());
            sb.Append((char)ReadByte());

            return sb.ToString();
        }

        private uint ReadPkgLength()
        {
            byte b1 = ReadByte();

            int byte_length = (int)(b1 >> 6) & 0x3;
            if (byte_length == 0)
                return (uint)b1;

            int shift_val = 4;
            uint ret = (uint)b1 & 0xf;

            while (byte_length > 0)
            {
                uint next_val = (uint)ReadByte();
                ret |= (next_val << shift_val);
                shift_val += 8;
                byte_length--;
            }

            return ret;
        }

        private DefBlockHeader parse_defblockheader()
        {
            DefBlockHeader ret = new DefBlockHeader();
            ret.TableSignature = ReadDWord();
            ret.TableLength = ReadDWord();
            ret.SpecCompliance = ReadByte();
            ret.CheckSum = ReadByte();
            ret.OemID = ReadByteArray(6);
            ret.OemTableID = ReadByteArray(8);
            ret.OemRevision = ReadDWord();
            ret.CreatorID = ReadDWord();
            ret.CreatorRevision = ReadDWord();

            if (ret.SpecCompliance > 0x1)
                int_is_64_bits = true;
            else
                int_is_64_bits = false;

            return ret;
        }

        internal List<Device> RootDevices
        { get { return GetDevices((string)null); } }

        internal List<Device> GetDevices(Device d)
        {
            if (d == null)
                return GetDevices((string)null);
            else
                return GetDevices(d.Name.FullNameString);
        }

        internal List<Device> GetDevices(string parent)
        {
            string base_name = "\\";
            if (parent != null)
                base_name = parent;

            if (!base_name.EndsWith("\\"))
                base_name += "\\";

            List<Device> ret = new List<Device>();
            foreach (KeyValuePair<string, Device> kvp in Devices)
            {
                if ((kvp.Key.StartsWith(base_name)) && (kvp.Key.Length == base_name.Length + 4))
                    ret.Add(kvp.Value);
            }

            return ret;
        }

        internal List<Object> GetObjects(string parent)
        {
            string base_name = "\\";
            if (parent != null)
                base_name = parent;

            if (!base_name.EndsWith("\\"))
                base_name += "\\";

            List<Object> ret = new List<Object>();
            foreach (KeyValuePair<string, Object> kvp in Objects)
            {
                if ((kvp.Key.StartsWith(base_name)) && (kvp.Key.Length == base_name.Length + 4) && !(kvp.Value is Device))
                    ret.Add(kvp.Value);
            }

            return ret;
        }
    }
}
