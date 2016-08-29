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
using tysos;
using tysos.lib;
using tysos.Resources;
using vfs;

namespace disk
{
    partial class disk : tysos.lib.VirtualDirectoryServer
    {
        internal disk(tysos.lib.File.Property[] Properties)
        {
            root = new List<tysos.lib.File.Property>(Properties);
        }

        vfs.BlockDevice bdev;

        public override bool InitServer()
        {
            System.Diagnostics.Debugger.Log(0, "disk", "Disk driver started");

            /* Get our ports and interrupt */
            foreach (var r in root)
            {
                if (r.Name == "blockdev" && (r.Value is vfs.BlockDevice))
                {
                    bdev = r.Value as vfs.BlockDevice;
                }
            }

            if (bdev == null)
            {
                System.Diagnostics.Debugger.Log(0, "disk", "No blockdev property found");
                return false;
            }

            /* Get the first sector of the device */
            byte[] sect_1 = new byte[bdev.SectorSize];
            tysos.lib.MonoIOError err;
            long read = bdev.Read(0, 1, sect_1, 0, out err);
            if (err != MonoIOError.ERROR_SUCCESS)
            {
                System.Diagnostics.Debugger.Log(0, "disk", "Attempt to read first sector failed: bytes_read: " + read.ToString() + ", error: " + err.ToString());
                return false;
            }

            /*StringBuilder sb = null;
            for(int i = 0; i < bdev.SectorSize; i++)
            {
                if(i % 8 == 0)
                {
                    if (sb != null)
                        System.Diagnostics.Debugger.Log(0, "disk", sb.ToString());
                    sb = new StringBuilder();
                    sb.Append(i.ToString() + ": ");
                }

                sb.Append(sect_1[i].ToString("X2"));
                sb.Append(" ");
            }

            if (sb != null)
                System.Diagnostics.Debugger.Log(0, "disk", sb.ToString()); */

            /* Try and identify the disk */
            if (bdev.SectorSize < 512 || sect_1[510] != 0x55 || sect_1[511] != 0xaa)
            {
                System.Diagnostics.Debugger.Log(0, "disk", "Treating as one big partition (" +
                    "SectorSize: " + bdev.SectorSize.ToString() +
                    ", signature: " + sect_1[510].ToString("X2") + " " + sect_1[511].ToString("X2") + ")");

                /* Treat as one big partition */
                string part_type = IdentifyPartition(sect_1);

                List<File.Property> props = new List<File.Property>();
                props.Add(new File.Property { Name = "blockdev", Value = bdev });
                if(part_type != null)
                    props.Add(new File.Property { Name = "driver", Value = part_type });
                children.Add("volume", props);
            }
            else
            {
                if(sect_1[0] == 0xeb || sect_1[0] == 0xe9)
                {
                    System.Diagnostics.Debugger.Log(0, "disk", "Treating as VBR");

                    /* VBR signature - treat as one big partition */
                    string part_type = IdentifyPartition(sect_1);

                    List<File.Property> props = new List<File.Property>();
                    props.Add(new File.Property { Name = "blockdev", Value = bdev });
                    if (part_type != null)
                        props.Add(new File.Property { Name = "driver", Value = part_type });
                    children.Add("volume", props);
                }
                else
                {
                    /* Try and treat as a MBR if the partition entries appear valid */
                    bool is_valid = true;
                    bool is_gpt = false;
                    for(int i = 0; i < 4; i++)
                    {
                        int offset = 446 + i * 16;
                        byte ptype = sect_1[offset + 4];
                        uint lba = BitConverter.ToUInt32(sect_1, offset + 8);
                        uint sect_count = BitConverter.ToUInt32(sect_1, offset + 0xc);

                        System.Diagnostics.Debugger.Log(0, "disk", "MBR: partition " + i.ToString() +
                            ": ptype: " + ptype.ToString("X2") +
                            ", lba: " + lba.ToString() +
                            ", sect_count: " + sect_count.ToString());

                        if (ptype == 0)
                            continue;

                        if(ptype == 0xee)
                        {
                            is_gpt = true;
                            break;
                        }

                        if(lba + sect_count > bdev.SectorCount)
                        {
                            is_valid = false;
                            break;
                        }
                        else
                        {
                            /* Load up first sector of partition to identify type */
                            byte[] part_sect_1 = new byte[bdev.SectorSize];
                            read = bdev.Read(lba, 1, part_sect_1, 0, out err);
                            string part_type;
                            if(err != MonoIOError.ERROR_SUCCESS || read == bdev.SectorSize ||
                                (part_type = IdentifyPartition(part_sect_1)) == null)
                            {
                                /* There was an issue loading the first sector - resort to using
                                MBR partition type */
                                part_type = IdentifyPartition(ptype);
                            }

                            /* Create a child object */
                            List<File.Property> props = new List<File.Property>();
                            props.Add(new File.Property { Name = "blockdev", Value = new BlockDevice(bdev, lba, sect_count) });
                            if (part_type != null)
                                props.Add(new File.Property { Name = "driver", Value = part_type });
                            children.Add("volume_" + i.ToString(), props);

                            System.Diagnostics.Debugger.Log(0, "disk", "MBR: partition " + i.ToString() +
                                ": ptype: " + (part_type == null ? "null" : part_type));
                        }
                    }

                    if(is_gpt)
                    {
                        children.Clear();
                        throw new NotImplementedException("GPT not implemented");
                    }

                    if(is_valid == false)
                    {
                        /* The MBR was not valid, treat it as one large partition */
                        System.Diagnostics.Debugger.Log(0, "disk", "Invalid MBR - treating as one big partition");
                        children.Clear();

                        string part_type = IdentifyPartition(sect_1);

                        List<File.Property> props = new List<File.Property>();
                        props.Add(new File.Property { Name = "blockdev", Value = bdev });
                        if (part_type != null)
                            props.Add(new File.Property { Name = "driver", Value = part_type });
                        children.Add("volume", props);
                    }
                }
            }

            /* Dump the identified partitions */
            foreach(var part in children)
            {
                System.Diagnostics.Debugger.Log(0, "disk", part.Key);
                foreach(var prop in part.Value)
                    System.Diagnostics.Debugger.Log(0, "disk", "  " + prop.Name + ": " + prop.Value.ToString());
            }

            root.Add(new File.Property { Name = "class", Value = "block" });
            Tags.Add("class");

            return true;
        }

        private string IdentifyPartition(byte[] sect_1)
        {
            //TODO
            return null;
        }

        private string IdentifyPartition(uint ptype)
        {
            switch(ptype)
            {
                case 1:
                case 4:
                case 6:
                case 0xb:
                case 0xc:
                case 0xe:
                case 0x11:
                case 0x14:
                case 0x1b:
                case 0x1c:
                case 0x1e:
                    return "fat";

                case 0x83:
                    return "ext2";

                default:
                    return null;
            }
        }
    }

    class BlockDevice : vfs.BlockDevice
    {
        vfs.BlockDevice parent;
        long lba;
        long lba_len;

        internal BlockDevice(vfs.BlockDevice Parent, long Lba, long LbaLen)
        {
            parent = Parent;
            lba = Lba;
            lba_len = LbaLen;
        }

        public override long SectorSize
        {
            get
            {
                return parent.SectorSize;
            }
        }

        public override long SectorCount
        {
            get
            {
                return lba_len;
            }
        }

        public override BlockEvent ReadAsync(long sector_idx, long sector_count, byte[] buf, int buf_offset)
        {
            if (sector_idx + sector_count > lba_len)
                sector_count = lba_len - sector_idx;
            if (sector_count < 0)
                sector_count = 0;
            return parent.ReadAsync(sector_idx + lba, sector_count, buf, buf_offset);
        }

        public override BlockEvent WriteAsync(long sector_idx, long sector_count, byte[] buf, int buf_offset)
        {
            if (sector_idx + sector_count > lba_len)
                sector_count = lba_len - sector_idx;
            if (sector_count < 0)
                sector_count = 0;
            return parent.WriteAsync(sector_idx + lba, sector_count, buf, buf_offset);
        }
    }
}
