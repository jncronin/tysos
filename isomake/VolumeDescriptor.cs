using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isomake
{
    abstract class VolumeDescriptor
    {
        protected class VDType
        {
            public int Offset;
            public VDTypeCode Code;
            public int Length;

            public enum VDTypeCode
            {
                int8, sint8, int16_LSB, int16_MSB, int16_LSB_MSB, sint16_LSB, sint16_MSB, sint16_LSB_MSB, int32_LSB, int32_MSB, int32_LSB_MSB,
                sint32_LSB, sint32_MSB, sint32_LSB_MSB,
                date_time,
                strA,
                strD,
            }
        }

        protected byte[] d = new byte[2048];

        Dictionary<string, VDType> entries = new Dictionary<string, VDType>();

        int cur_entry = 0;

        public void Write(System.IO.BinaryWriter w)
        {
            w.Write(d);
        }

        protected int ReadInt(int offset, VDType.VDTypeCode c)
        {
            switch (c)
            {
                case VDType.VDTypeCode.int8:
                    return d[offset];
                case VDType.VDTypeCode.sint8:
                    return (sbyte)d[offset];
                case VDType.VDTypeCode.sint16_LSB:
                case VDType.VDTypeCode.sint16_LSB_MSB:
                    return BitConverter.ToInt16(d, offset);
                case VDType.VDTypeCode.sint32_LSB:
                case VDType.VDTypeCode.sint32_LSB_MSB:
                    return BitConverter.ToInt32(d, offset);
                case VDType.VDTypeCode.int16_LSB:
                case VDType.VDTypeCode.int16_LSB_MSB:
                    return BitConverter.ToUInt16(d, offset);
                case VDType.VDTypeCode.int32_LSB:
                case VDType.VDTypeCode.int32_LSB_MSB:
                    return (int)BitConverter.ToUInt32(d, offset);
                default:
                    throw new NotImplementedException();
            }
        }

        protected void WriteInt(int offset, VDType.VDTypeCode c, int v)
        {
            switch (c)
            {
                case VDType.VDTypeCode.int8:
                case VDType.VDTypeCode.sint8:
                    d[offset] = (byte)(v & 0xff);
                    break;
                case VDType.VDTypeCode.int16_LSB:
                case VDType.VDTypeCode.sint16_LSB:
                    d[offset] = (byte)(v & 0xff);
                    d[offset + 1] = (byte)((v >> 8) & 0xff);
                    break;
                case VDType.VDTypeCode.int32_LSB:
                case VDType.VDTypeCode.sint32_LSB:
                    d[offset] = (byte)(v & 0xff);
                    d[offset + 1] = (byte)((v >> 8) & 0xff);
                    d[offset + 2] = (byte)((v >> 16) & 0xff);
                    d[offset + 3] = (byte)((v >> 24) & 0xff);
                    break;
                case VDType.VDTypeCode.int16_MSB:
                case VDType.VDTypeCode.sint16_MSB:
                    d[offset] = (byte)((v >> 8) & 0xff);
                    d[offset + 1] = (byte)(v & 0xff);
                    break;
                case VDType.VDTypeCode.int32_MSB:
                case VDType.VDTypeCode.sint32_MSB:
                    d[offset] = (byte)((v >> 24) & 0xff);
                    d[offset + 1] = (byte)((v >> 16) & 0xff);
                    d[offset + 2] = (byte)((v >> 8) & 0xff);
                    d[offset + 3] = (byte)(v & 0xff);
                    break;
                case VDType.VDTypeCode.int16_LSB_MSB:
                case VDType.VDTypeCode.sint16_LSB_MSB:
                    d[offset] = (byte)(v & 0xff);
                    d[offset + 1] = (byte)((v >> 8) & 0xff);
                    d[offset + 2] = (byte)((v >> 8) & 0xff);
                    d[offset + 3] = (byte)(v & 0xff);
                    break;
                case VDType.VDTypeCode.int32_LSB_MSB:
                case VDType.VDTypeCode.sint32_LSB_MSB:
                    d[offset] = (byte)(v & 0xff);
                    d[offset + 1] = (byte)((v >> 8) & 0xff);
                    d[offset + 2] = (byte)((v >> 16) & 0xff);
                    d[offset + 3] = (byte)((v >> 24) & 0xff);
                    d[offset + 4] = (byte)((v >> 24) & 0xff);
                    d[offset + 5] = (byte)((v >> 16) & 0xff);
                    d[offset + 6] = (byte)((v >> 8) & 0xff);
                    d[offset + 7] = (byte)(v & 0xff);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected void WriteString(int offset, int len, string s)
        {
            for (int i = 0; i < len; i++)
            {
                if (i >= s.Length)
                    d[offset++] = (byte)' ';
                else
                    d[offset++] = (byte)s[i];
            }
        }

        protected string ReadString(int offset, int len)
        {
            var c = new char[len];
            for (int i = 0; i < len; i++)
                c[i] = (char)d[offset++];
            var s = new string(c);
            return s.TrimEnd(' ');
        }

        protected void WriteDateTime(int offset, DateTime d)
        {
            d = d.ToUniversalTime();

            WriteString(offset, 4, d.Year.ToString("D4"));
            WriteString(offset + 4, 2, d.Month.ToString("D2"));
            WriteString(offset + 6, 2, d.Day.ToString("D2"));
            WriteString(offset + 8, 2, d.Hour.ToString("D2"));
            WriteString(offset + 10, 2, d.Minute.ToString("D2"));
            WriteString(offset + 12, 2, d.Second.ToString("D2"));
            WriteString(offset + 14, 2, (d.Millisecond / 10).ToString("D2"));
            WriteInt(offset + 16, VDType.VDTypeCode.int8, 0);
        }

        public string ReadString(string tag) { return ReadString(entries[tag].Offset, entries[tag].Length); }
        public int ReadInt(string tag) { return ReadInt(entries[tag].Offset, entries[tag].Code); }
        public void WriteString(string tag, string s) { WriteString(entries[tag].Offset, entries[tag].Length, s); }
        public void WriteInt(string tag, int v) { WriteInt(entries[tag].Offset, entries[tag].Code, v); }
        public void WriteDateTime(string tag, DateTime d) { WriteDateTime(entries[tag].Offset, d); }

        protected void AddIntEntry(string tag, VDType.VDTypeCode c, int default_val)
        {
            AddIntEntry(tag, c);
            WriteInt(tag, default_val);
        }

        protected void AddIntEntry(string tag, VDType.VDTypeCode c)
        {
            int len = 0;

            switch (c)
            {
                case VDType.VDTypeCode.int8:
                case VDType.VDTypeCode.sint8:
                    len = 1;
                    break;
                case VDType.VDTypeCode.int16_LSB:
                case VDType.VDTypeCode.int16_MSB:
                case VDType.VDTypeCode.sint16_LSB:
                case VDType.VDTypeCode.sint16_MSB:
                    len = 2;
                    break;
                case VDType.VDTypeCode.int32_LSB:
                case VDType.VDTypeCode.int32_MSB:
                case VDType.VDTypeCode.sint32_LSB:
                case VDType.VDTypeCode.sint32_MSB:
                case VDType.VDTypeCode.int16_LSB_MSB:
                case VDType.VDTypeCode.sint16_LSB_MSB:
                    len = 4;
                    break;
                case VDType.VDTypeCode.int32_LSB_MSB:
                case VDType.VDTypeCode.sint32_LSB_MSB:
                    len = 8;
                    break;
                default:
                    throw new NotSupportedException();
            }

            var e = new VDType { Offset = cur_entry, Code = c, Length = len };
            entries[tag] = e;
            cur_entry += len;
        }

        protected void AddStringEntry(string tag, int len, string default_val)
        {
            AddStringEntry(tag, len);
            WriteString(tag, default_val);
        }

        protected void AddStringEntry(string tag, int len)
        {
            var e = new VDType { Offset = cur_entry, Code = VDType.VDTypeCode.strA, Length = len };
            entries[tag] = e;
            cur_entry += len;
        }

        protected void AddDateTimeEntry(string tag, DateTime d)
        {
            AddDateTimeEntry(tag);
            WriteDateTime(tag, d);
        }

        protected void AddDateTimeEntry(string tag)
        {
            var e = new VDType { Offset = cur_entry, Code = VDType.VDTypeCode.date_time, Length = 17 };
            entries[tag] = e;
            cur_entry += 17;
        }

        protected VolumeDescriptor()
        {
            AddIntEntry("Type", VDType.VDTypeCode.int8);
            AddStringEntry("Identifier", 5);
            AddIntEntry("Version", VDType.VDTypeCode.int8);

            WriteInt("Version", 0x01);
            WriteString("Identifier", "CD001");
        }
    }

    class ElToritoBootDescriptor : VolumeDescriptor
    {
        public ElToritoBootDescriptor() : base()
        {
            WriteInt("Type", 0);

            AddStringEntry("BootSystemIdentifier", 32, "EL TORITO SPECIFICATION");
            AddStringEntry("BootIdentifier", 32);
            AddIntEntry("BootCatalogAddress", VDType.VDTypeCode.int32_LSB);
        }

        public int BootCatalogAddress { get { return ReadInt("BootCatalogAddress"); } set { WriteInt("BootCatalogAddress", value); } }
    }

    class PrimaryVolumeDescriptor : VolumeDescriptor
    {
        public PrimaryVolumeDescriptor() : base()
        {
            WriteInt("Type", 0x01);

            AddIntEntry("Unused", VDType.VDTypeCode.int8, 0);
            AddStringEntry("SystemIdentifier", 32);
            AddStringEntry("VolumeIdentifier", 32);
            AddIntEntry("Unused2", VDType.VDTypeCode.sint32_LSB_MSB, 0);
            AddIntEntry("VolumeSpaceSize", VDType.VDTypeCode.int32_LSB_MSB);
            AddStringEntry("Unused3", 32, "");
            AddIntEntry("VolumeSetSize", VDType.VDTypeCode.int16_LSB_MSB);
            AddIntEntry("VolumeSequenceNumber", VDType.VDTypeCode.int16_LSB_MSB);
            AddIntEntry("LogicalBlockSize", VDType.VDTypeCode.int16_LSB_MSB, 2048);
            AddIntEntry("PathTableSize", VDType.VDTypeCode.int32_LSB_MSB);
            AddIntEntry("LocTypeLPathTable", VDType.VDTypeCode.int32_LSB);
            AddIntEntry("LocOptTypeLPathTable", VDType.VDTypeCode.int32_LSB);
            AddIntEntry("LocTypeMPathTable", VDType.VDTypeCode.int32_LSB);
            AddIntEntry("LocOptTypeMPathTable", VDType.VDTypeCode.int32_LSB);

            // TODO
            AddStringEntry("RootDir", 34);

            AddStringEntry("VolumeSetIdentifier", 128, "");
            AddStringEntry("PublisherIdentifier", 128, "");
            AddStringEntry("DataPreparerIdentifier", 128, "");
            AddStringEntry("ApplicationIdentifier", 128, "");
            AddStringEntry("CopyrightFileIdentifier", 38, "");
            AddStringEntry("AbstractFileIdentifier", 36, "");
            AddStringEntry("BibliographicFileIdentifier", 37, "");
            AddDateTimeEntry("CreationDateTime", DateTime.Now);
            AddDateTimeEntry("ModificationDateTime", DateTime.Now);
            AddDateTimeEntry("ExpirationDateTime");
            AddDateTimeEntry("EffectiveDateTime");
            AddIntEntry("FileStructureVersion", VDType.VDTypeCode.int8, 0x01);

        }
    }

    class VolumeDescriptorSetTerminator : VolumeDescriptor
    {
        public VolumeDescriptorSetTerminator() : base()
        {
            WriteInt("Type", 255);
        }
    }
}
