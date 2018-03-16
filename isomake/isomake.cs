using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isomake
{
    class Program
    {
        static bool do_rr = true;

        static List<BootEntry> bootEntries = new List<BootEntry>();

        static string ofname = "output.iso";
        internal static string boot_catalog = "boot.catalog";
        internal static string boot_catalog_d = null;
        internal static string boot_catalog_f = null;
        static string src_dir = null;

        static int rr_ce_lba = 0;
        static List<byte> rr_ce = null;

        static void Main(string[] args)
        {
            if(!parse_args(args))
            {
                show_usage();
                return;
            }

            var d = new System.IO.DirectoryInfo(src_dir);

            var o = new System.IO.BinaryWriter(new System.IO.FileStream(ofname, System.IO.FileMode.Create));


            // Write 32 kiB of zeros to the system area
            o.Write(new byte[32 * 1024]);

            List<VolumeDescriptor> voldescs = new List<VolumeDescriptor>();

            // Add a primary volume descriptor
            var pvd = new PrimaryVolumeDescriptor();
            voldescs.Add(pvd);

            ElToritoBootDescriptor bvd = null;
            if (bootEntries.Count > 0)
            {
                bvd = new ElToritoBootDescriptor();
                voldescs.Add(bvd);
            }

            voldescs.Add(new VolumeDescriptorSetTerminator());

            // Generate directory tree
            List<AnnotatedFSO> files, dirs;
            var afso = AnnotatedFSO.BuildAFSOTree(d, out dirs, out files);

            // Allocate space for files + directories
            int cur_lba = 0x10 + voldescs.Count;

            List<AnnotatedFSO> output_order = new List<AnnotatedFSO>();
            AnnotatedFSO afso_bc = null;
            foreach(var file in files)
            {
                if (file.fsi is BootCatalogFileInfo)
                {
                    afso_bc = file;
                    continue;
                }
                var fi = file.fsi as FileInfo;
                var l = align(fi.Length);
                var lbal = l / 2048;

                file.lba = cur_lba;
                file.len = (int)fi.Length;
                cur_lba += (int)lbal;

                output_order.Add(file);

                foreach(var bfe in bootEntries)
                {
                    if(bfe.ffname != null && bfe.boot_table && bfe.ffname == fi.FullName)
                    {
                        file.needs_boot_table = true;
                        bfe.afso_boot_file = file;
                    }
                }
            }

            // create boot catalog
            List<byte> bc = new List<byte>();
            var bc_lba = cur_lba;
            if (bootEntries.Count > 0)
            {
                // Validation entry
                List<byte> val_ent = new List<byte>();
                val_ent.Add(0x01);
                if (bootEntries[0].type == BootEntry.BootType.BIOS)
                    val_ent.Add(0);
                else
                    val_ent.Add(0xef);
                val_ent.Add(0);
                val_ent.Add(0);
                //val_ent.AddRange(Encoding.ASCII.GetBytes("isomake".PadRight(24)));
                for (int i = 0; i < 24; i++)
                    val_ent.Add(0);
                var cs = elt_checksum(val_ent, 0xaa55);
                val_ent.Add((byte)(cs & 0xff));
                val_ent.Add((byte)((cs >> 8) & 0xff));
                val_ent.Add(0x55);
                val_ent.Add(0xaa);
                bc.AddRange(val_ent);

                // default entry
                List<byte> def_ent = new List<byte>();
                if (bootEntries[0].bootable)
                    def_ent.Add(0x88);
                else
                    def_ent.Add(0x00);
                switch (bootEntries[0].etype)
                {
                    case BootEntry.EmulType.Floppy:
                        def_ent.Add(0x02);
                        break;
                    case BootEntry.EmulType.Hard:
                        def_ent.Add(0x04);
                        break;
                    case BootEntry.EmulType.NoEmul:
                        def_ent.Add(0x00);
                        break;
                    default:
                        throw new NotSupportedException();
                }
                def_ent.AddRange(BitConverter.GetBytes((ushort)bootEntries[0].load_seg));
                def_ent.Add(0x0);
                def_ent.Add(0x0);
                def_ent.AddRange(BitConverter.GetBytes((ushort)bootEntries[0].sector_count));
                if (bootEntries[0].afso_boot_file != null)
                    def_ent.AddRange(BitConverter.GetBytes(bootEntries[0].afso_boot_file.lba));
                else
                    def_ent.AddRange(BitConverter.GetBytes((int)0));
                for (int i = 0; i < 20; i++)
                    def_ent.Add(0);
                bc.AddRange(def_ent);

                for (int idx = 1; idx < bootEntries.Count; idx++)
                {
                    // section header
                    List<byte> sh = new List<byte>();
                    if (idx == bootEntries.Count - 1)
                        sh.Add(0x91);
                    else
                        sh.Add(0x90);
                    if (bootEntries[idx].type == BootEntry.BootType.BIOS)
                        sh.Add(0x0);
                    else
                        sh.Add(0xef);
                    sh.AddRange(BitConverter.GetBytes((ushort)1));
                    for (int i = 0; i < 28; i++)
                        sh.Add(0);
                    //sh.AddRange(Encoding.ASCII.GetBytes(bootEntries[idx].type.ToString().PadRight(28)));
                    bc.AddRange(sh);

                    // section entry
                    List<byte> se = new List<byte>();
                    if (bootEntries[idx].bootable)
                        se.Add(0x88);
                    else
                        se.Add(0x00);
                    switch (bootEntries[idx].etype)
                    {
                        case BootEntry.EmulType.Floppy:
                            se.Add(0x02);
                            break;
                        case BootEntry.EmulType.Hard:
                            se.Add(0x04);
                            break;
                        case BootEntry.EmulType.NoEmul:
                            se.Add(0x00);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    se.AddRange(BitConverter.GetBytes((ushort)bootEntries[idx].load_seg));
                    se.Add(0);
                    se.Add(0);
                    se.AddRange(BitConverter.GetBytes((ushort)bootEntries[idx].sector_count));
                    if (bootEntries[idx].afso_boot_file != null)
                        se.AddRange(BitConverter.GetBytes((int)bootEntries[idx].afso_boot_file.lba));
                    else
                        se.AddRange(BitConverter.GetBytes((int)0));
                    se.Add(0);
                    for (int i = 0; i < 19; i++)
                        se.Add(0);
                    bc.AddRange(se);
                }

                afso_bc.lba = bc_lba;
                afso_bc.len = bc.Count;

                cur_lba += (int)align(bc.Count) / 2048;
            }

            // create root dir first entry continuation area containing rockridge attribute if required
            rr_ce = new List<byte>();
            if(do_rr)
            {
                // Use a spare sector to contain the 'ER' field as is it too big for the system use area
                // This is later referenced in a 'CE' entry
                rr_ce_lba = cur_lba++;

                // Add SUSP ER field to identify RR
                rr_ce.Add(0x45); rr_ce.Add(0x52); // "ER"
                var ext_id = "IEEE_P1282";
                var ext_desc = "THE IEEE P1282 PROTOCOL PROVIDES SUPPORT FOR POSIX FILE SYSTEM SEMANTICS.";
                var ext_src = "PLEASE CONTACT THE IEEE STANDARDS DEPARTMENT, PISCATAWAY, NJ, USA FOR THE P1282 SPECIFICATION.";
                rr_ce.Add((byte)(8 + ext_id.Length + ext_desc.Length + ext_src.Length));       // length
                rr_ce.Add(1);                  // version
                rr_ce.Add((byte)ext_id.Length);    // LEN_ID
                rr_ce.Add((byte)ext_desc.Length);  // LEN_DES
                rr_ce.Add((byte)ext_src.Length);   // LEN_SRC
                rr_ce.Add(1);                  // EXT_VER
                rr_ce.AddRange(Encoding.ASCII.GetBytes(ext_id));
                rr_ce.AddRange(Encoding.ASCII.GetBytes(ext_desc));
                rr_ce.AddRange(Encoding.ASCII.GetBytes(ext_src));
            }

            // build directory tree from most deeply nested to most shallow, so that we automatically patch up
            //  the appropriate child lbas as we go
            List<byte> dir_tree = new List<byte>();
            var dt_lba = cur_lba;
            Dictionary<int, AnnotatedFSO> parent_dir_map = new Dictionary<int, AnnotatedFSO>();
            for(int i = 7; i >= 0; i--)
            {
                var afso_level = afso[i];
                foreach (var cur_afso in afso_level)
                    build_dir_tree(cur_afso, dir_tree, dt_lba, parent_dir_map);
            }
            
            // patch up parent lbas
            foreach(var kvp in parent_dir_map)
            {
                var lba = kvp.Value.lba;
                var len = kvp.Value.len;

                var lba_b = int_lsb_msb(lba);
                var len_b = int_lsb_msb(len);

                insert_bytes(lba_b, kvp.Key + 2, dir_tree);
                insert_bytes(len_b, kvp.Key + 10, dir_tree);
            }

            // And root directory entry
            var root_entry = build_dir_entry("\0", afso[0][0].lba, afso[0][0].len, d);
            cur_lba += dir_tree.Count / 2048;

            // create path table
            var path_table_l_lba = cur_lba;
            byte[] path_table_l = build_path_table(afso, true);
            var path_table_b_lba = path_table_l_lba + (int)align(path_table_l.Length) / 2048;
            byte[] path_table_b = build_path_table(afso, false);

            cur_lba = path_table_b_lba + (int)align(path_table_b.Length) / 2048;

            // Set pvd entries
            pvd.WriteString("VolumeIdentifier", "UNNAMED");
            pvd.WriteInt("VolumeSpaceSize", cur_lba);
            pvd.WriteInt("VolumeSetSize", 1);
            pvd.WriteInt("VolumeSequenceNumber", 1);
            pvd.WriteInt("LogicalBlockSize", 2048);
            pvd.WriteInt("PathTableSize", path_table_l.Length);
            pvd.WriteInt("LocTypeLPathTable", path_table_l_lba);
            pvd.WriteInt("LocTypeMPathTable", path_table_b_lba);
            pvd.WriteBytes("RootDir", root_entry);

            if (bootEntries.Count > 0)
                bvd.BootCatalogAddress = bc_lba;

            // Write out volume descriptors
            foreach (var vd in voldescs)
                vd.Write(o);

            // files
            foreach(var f in output_order)
            {
                o.Seek(f.lba * 2048, SeekOrigin.Begin);
                var fin = new BinaryReader(((FileInfo)f.fsi).OpenRead());

                var b = fin.ReadBytes(f.len);

                if(f.needs_boot_table)
                {
                    // patch in the eltorito boot info table to offset 8

                    // first get 32 bit checksum from offset 64 onwards
                    uint csum = elt_checksum32(b, 64);
                    insert_bytes(BitConverter.GetBytes((int)0x10), 8, b);
                    insert_bytes(BitConverter.GetBytes((int)f.lba), 12, b);
                    insert_bytes(BitConverter.GetBytes((int)f.len), 16, b);
                    insert_bytes(BitConverter.GetBytes(csum), 20, b);
                }

                o.Write(b, 0, f.len);
            }

            // directory records
            o.Seek(dt_lba * 2048, SeekOrigin.Begin);
            o.Write(dir_tree.ToArray(), 0, dir_tree.Count);

            // path tables
            o.Seek(path_table_l_lba * 2048, SeekOrigin.Begin);
            o.Write(path_table_l, 0, path_table_l.Length);
            o.Seek(path_table_b_lba * 2048, SeekOrigin.Begin);
            o.Write(path_table_b, 0, path_table_b.Length);

            // boot catalog
            if(bootEntries.Count > 0)
            {
                o.Seek(bc_lba * 2048, SeekOrigin.Begin);

                o.Write(bc.ToArray(), 0, bc.Count);
            }

            // rr es field continuation area
            if(rr_ce != null)
            {
                o.Seek(rr_ce_lba * 2048, SeekOrigin.Begin);

                o.Write(rr_ce.ToArray(), 0, rr_ce.Count);
            }

            // Align to sector size
            o.Seek(0, SeekOrigin.End);
            while ((o.BaseStream.Position % 2048) != 0)
                o.Write((byte)0);

            o.Close();
        }

        private static bool parse_args(string[] args)
        {
            List<BootEntry> tmp_bootEntries = new List<BootEntry>();
            int cur_boot_entry = 0;
            tmp_bootEntries.Add(new BootEntry());

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var c = args[i];

                    if (c == "-as")
                    {
                        c = args[++i];
                        if (c == "mkisofs")
                        {
                            // pass
                        }
                        else
                        {
                            System.Console.WriteLine("Unknown -as value: " + c);
                            return false;
                        }
                    }
                    else if (c == "-R" || c == "-r")
                        do_rr = true;
                    else if (c == "-f")
                    {
                        // pass
                    }
                    else if (c == "-e" || c == "--efi-boot")
                    {
                        tmp_bootEntries[cur_boot_entry].fname = args[++i];
                        tmp_bootEntries[cur_boot_entry].type = BootEntry.BootType.EFI;
                        tmp_bootEntries[cur_boot_entry].valid = true;
                    }
                    else if (c == "-b")
                    {
                        tmp_bootEntries[cur_boot_entry].fname = args[++i];
                        tmp_bootEntries[cur_boot_entry].type = BootEntry.BootType.BIOS;
                        tmp_bootEntries[cur_boot_entry].valid = true;
                    }
                    else if (c == "-no-emul-boot")
                    {
                        tmp_bootEntries[cur_boot_entry].etype = BootEntry.EmulType.NoEmul;
                    }
                    else if (c == "-hard-disk-boot")
                    {
                        tmp_bootEntries[cur_boot_entry].etype = BootEntry.EmulType.Hard;
                    }
                    else if (c == "-no-boot")
                    {
                        tmp_bootEntries[cur_boot_entry].bootable = false;
                        tmp_bootEntries[cur_boot_entry].valid = true;
                    }
                    else if (c == "-boot-load-seg")
                    {
                        tmp_bootEntries[cur_boot_entry].load_seg = readDecOrHex(args[++i]);
                    }
                    else if (c == "-boot-load-size")
                    {
                        tmp_bootEntries[cur_boot_entry].sector_count = readDecOrHex(args[++i]);
                    }
                    else if (c == "-boot-info-table")
                    {
                        tmp_bootEntries[cur_boot_entry].boot_table = true;
                    }
                    else if (c == "-eltorito-alt-boot")
                    {
                        tmp_bootEntries[cur_boot_entry++] = new BootEntry();
                    }
                    else if (c == "-o")
                    {
                        ofname = args[++i];
                    }
                    else if(c == "-c")
                    {
                        boot_catalog = args[++i];
                    }
                    else if (i == args.Length - 1 && !c.StartsWith("-"))
                    {
                        src_dir = c;
                    }
                    else
                        return false;
                }
            }
            catch(ArgumentOutOfRangeException)
            {
                return false;
            }

            if (src_dir == null)
                return false;

            // sanitize the boot table
            foreach(var tbe in tmp_bootEntries)
            {
                if (tbe.valid)
                {
                    bootEntries.Add(tbe);
                    if (tbe.fname != null)
                    {
                        tbe.ffname = Path.GetFullPath(Path.Combine(src_dir, tbe.fname));
                        var fi = new FileInfo(tbe.ffname);
                        if (fi.Exists == false)
                        {
                            System.Console.WriteLine("Boot file " + tbe.fname + " not found within src_dir");
                            return false;
                        }
                        if(tbe.sector_count == -1)
                        {
                            if (tbe.type == BootEntry.BootType.BIOS)
                                tbe.sector_count = 4;
                            else
                                tbe.sector_count = (int)align(fi.Length) / 2048;
                        }
                    }
                }
            }
            if(bootEntries.Count > 0)
            {
                boot_catalog = Path.GetFullPath(Path.Combine(src_dir, boot_catalog));
                var bcfi = new FileInfo(boot_catalog);
                boot_catalog_d = bcfi.Directory.FullName;
                boot_catalog_f = bcfi.Name;
            }

            return true;
        }

        private static int readDecOrHex(string v)
        {
            if (v.StartsWith("0x") || v.StartsWith("0X") || v.IndexOfAny(new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'a', 'b', 'c', 'd', 'e', 'f' }) != -1)
                return int.Parse(v, System.Globalization.NumberStyles.HexNumber);
            else
                return int.Parse(v);
        }

        private static void show_usage()
        {
            Console.WriteLine("Usage: isomake [-o output_file] [-R] [-r] [-b boot_file] [-e efi_boot_file] [-no-emul-boot] [-boot-load-table] [-boot-load-size] " +
                "[-eltorito-alt-boot] <src_dir>");
            Console.WriteLine("  All arguments as per 'man mkisofs'");
        }

        private static uint elt_checksum32(IList<byte> v, int offset)
        {
            uint val = 0;

            unchecked
            {
                for(int i = offset; i < v.Count; i += 4)
                {
                    uint cv = v[i];
                    if (i + 1 < v.Count)
                        cv |= (uint)v[i + 1] << 8;
                    if (i + 2 < v.Count)
                        cv |= (uint)v[i + 2] << 16;
                    if (i + 3 < v.Count)
                        cv |= (uint)v[i + 3] << 24;

                    val += cv;
                }
            }

            return (uint)((0L - val) & 0xffffffff);
        }

        private static ushort elt_checksum(List<byte> v, params ushort[] extra)
        {
            if ((v.Count % 2) == 1)
                throw new NotSupportedException();

            ushort val = 0;

            unchecked
            {
                for (int i = 0; i < v.Count; i += 2)
                {
                    var cv = v[i] + (v[i + 1] << 8);
                    val += (ushort)cv;
                }

                foreach (var e in extra)
                    val += e;
            }

            return (ushort)((0 - val) & 0xffff);
        }

        private static void insert_bytes(byte[] src, int doffset, IList<byte> dest)
        {
            for (int i = 0; i < src.Length; i++)
                dest[doffset + i] = src[i];
        }

        private static void build_dir_tree(AnnotatedFSO cur_afso, List<byte> dir_tree, int base_lba,
            Dictionary<int, AnnotatedFSO> parent_dir_map)
        {
            int cur_lba = base_lba + dir_tree.Count / 2048;
            cur_afso.lba = cur_lba;

            int cur_count = dir_tree.Count;

            // Add "." and ".." entries, store their location for future patching up
            parent_dir_map[dir_tree.Count] = cur_afso;
            var b1 = build_dir_entry("\0", 0, 0, null, cur_afso.Parent == null);
            add_dir_entry(b1, dir_tree);
            parent_dir_map[dir_tree.Count] = cur_afso.Parent ?? cur_afso;
            var b2 = build_dir_entry("\u0001", 0, 0);
            add_dir_entry(b2, dir_tree);

            cur_afso.Children.Sort(dir_sorter);

            foreach(var c in cur_afso.Children)
            {
                if (c.fsi is FileInfo)
                    add_dir_entry(build_dir_entry(c.Identifier, c.lba, (int)((FileInfo)c.fsi).Length, c.fsi), dir_tree);
                else
                    add_dir_entry(build_dir_entry(c.Identifier, c.lba, c.len, c.fsi), dir_tree);
            }

            cur_afso.len = (int)align(dir_tree.Count - cur_count);

            align(dir_tree);
        }

        private static void add_dir_entry(List<byte> b, List<byte> dir_tree)
        {
            // get the size left in the current sector
            var space_left = 2048 - (dir_tree.Count % 2048);
            if (b.Count > 2048)
                throw new NotSupportedException();

            if (b.Count > space_left)
                align(dir_tree);

            dir_tree.AddRange(b);
        }

        private static List<byte> build_dir_entry(string id, int lba, int len, FileSystemInfo fsi = null, bool is_root_dot = false)
        {
            var ret = new List<byte>();

            var id_len = id.Length;
            var dr_len = align(33 + id_len, 2);

            // build RockRidge extra data here, then paste at the end
            List<byte> rr = new List<byte>();
            if (do_rr)
            {
                if (is_root_dot)
                {
                    // Add SUSP SP field
                    rr.Add(0x53); rr.Add(0x50); // "SP"
                    rr.Add(0x7);                // length
                    rr.Add(1);                  // version
                    rr.Add(0xbe); rr.Add(0xef); // check bytes
                    rr.Add(0);                  // len_skp
                }
                // Add RR PX field
                rr.Add(0x50); rr.Add(0x58);     // "PX"
                rr.Add(44);                     // length
                rr.Add(1);                      // version
                int posix_attrs = 0x1ff;        // permissions
                if (fsi == null || fsi is DirectoryInfo)
                    posix_attrs |= 0x4000;     // dir
                else
                    posix_attrs |= 0x8000;    // regular
                rr.AddRange(int_lsb_msb(posix_attrs));
                rr.AddRange(int_lsb_msb(0));    // links
                rr.AddRange(int_lsb_msb(0));    // uid
                rr.AddRange(int_lsb_msb(0));    // gid
                rr.AddRange(int_lsb_msb(0));    // serial num
                                                
                // Add RR NM entry
                if (!id.Equals("\0") && !id.Equals("\u0001"))
                {
                    rr.Add(0x4e); rr.Add(0x4d);     // "NM"
                                                    // get name len
                    var rr_name_len = (id.Equals("\0") || id.Equals("\u0001") ? 0 : fsi.Name.Length);
                    rr.Add((byte)(5 + rr_name_len));    // length
                    rr.Add(1);                      // version
                    int nm_flags = 0;
                    if (id.Equals("\0")) nm_flags |= 0x2;
                    if (id.Equals("\u0001")) nm_flags |= 0x3;
                    rr.Add((byte)nm_flags);
                    if (rr_name_len > 0) rr.AddRange(Encoding.ASCII.GetBytes(fsi.Name));
                }

                // If '.' in root dir, point to the 'ER' field in the continuation area set above
                if(is_root_dot)
                {
                    rr.Add(0x43); rr.Add(0x45);     // "CE"
                    rr.Add(28);                     // length
                    rr.Add(1);                      // version
                    rr.AddRange(int_lsb_msb(rr_ce_lba));    // block location of continuation area
                    rr.AddRange(int_lsb_msb(0));    // offset within block
                    rr.AddRange(int_lsb_msb(rr_ce.Count));  // length of continuation area
                }
            }
            if ((rr.Count % 2) == 0x1)
                rr.Add(0);

            dr_len += rr.Count;

            if (dr_len > 255)
                throw new NotSupportedException();

            ret.Add((byte)dr_len);
            ret.Add(0);

            ret.AddRange(int_lsb_msb(lba));
            ret.AddRange(int_lsb_msb(len));

            ret.AddRange(dr_date((fsi == null) ? DateTime.UtcNow : fsi.LastWriteTimeUtc));
            int flags = 0;
            if (fsi == null || fsi is DirectoryInfo)
                flags |= 0x2;
            ret.Add((byte)flags);

            ret.Add(0);
            ret.Add(0);
            ret.AddRange(short_lsb_msb(1));     // volume sequence number

            ret.Add((byte)id_len);
            ret.AddRange(Encoding.ASCII.GetBytes(id));

            align(ret, 2);

            ret.AddRange(rr);

            return ret;
        }

        private static byte[] dr_date(DateTime d)
        {
            var ret = new byte[7];
            ret[0] = (byte)(d.Year - 1900);
            ret[1] = (byte)d.Month;
            ret[2] = (byte)d.Day;
            ret[3] = (byte)d.Hour;
            ret[4] = (byte)d.Minute;
            ret[5] = (byte)d.Second;
            ret[6] = 0;
            return ret;
        }

        private static byte[] int_lsb_msb(int v)
        {
            var d = new byte[8];
            d[0] = (byte)(v & 0xff);
            d[1] = (byte)((v >> 8) & 0xff);
            d[2] = (byte)((v >> 16) & 0xff);
            d[3] = (byte)((v >> 24) & 0xff);
            d[4] = (byte)((v >> 24) & 0xff);
            d[5] = (byte)((v >> 16) & 0xff);
            d[6] = (byte)((v >> 8) & 0xff);
            d[7] = (byte)(v & 0xff);
            return d;
        }

        private static byte[] short_lsb_msb(int v)
        {
            var d = new byte[4];
            d[0] = (byte)(v & 0xff);
            d[1] = (byte)((v >> 8) & 0xff);
            d[2] = (byte)((v >> 8) & 0xff);
            d[3] = (byte)(v & 0xff);
            return d;
        }

        static int dir_sorter(AnnotatedFSO a, AnnotatedFSO b)
        {
            var fname_len = Math.Max(a.FName.Length, b.FName.Length);
            var afn = a.FName.PadRight(fname_len);
            var bfn = b.FName.PadRight(fname_len);

            var fn_cmp = string.CompareOrdinal(afn, bfn);
            if (fn_cmp != 0)
                return fn_cmp;

            var ext_len = Math.Max(a.Ext == null ? 0 : a.Ext.Length, b.Ext == null ? 0 : b.Ext.Length);
            var aext = (a.Ext ?? "").PadRight(ext_len);
            var bext = (b.Ext ?? "").PadRight(ext_len);

            return string.CompareOrdinal(aext, bext);
        }

        private static byte[] build_path_table(List<AnnotatedFSO>[] afso, bool lsb)
        {
            List<byte> ret = new List<byte>();

            for(int i = 0; i < afso.Length; i++)
            {
                var cur_afsol = afso[i];
                foreach (var cur_afso in cur_afsol)
                    build_path_table(cur_afso, ret, lsb);
            }

            return ret.ToArray();
        }

        private static void build_path_table(AnnotatedFSO afso, List<byte> ret, bool lsb)
        {
            var id_len = afso.Identifier.Length;
            var pt_len = align(8 + id_len, 2);

            int cur_idx = ret.Count;
            ret.Add((byte)id_len);
            ret.Add(0);

            ret.AddRange(ToByteArray(afso.lba, 4, lsb));
            ret.AddRange(ToByteArray(afso.Parent == null ? 1 : afso.Parent.dir_idx, 2, lsb));

            foreach (var c in afso.Identifier)
                ret.Add((byte)c);

            while (ret.Count < (cur_idx + pt_len))
                ret.Add(0);
        }

        private static IEnumerable<byte> ToByteArray(int v, int bc, bool lsb)
        {
            byte[] ret = new byte[bc];

            if(lsb)
            {
                for (int i = 0; i < bc; i++)
                {
                    ret[i] = (byte)(v & 0xff);
                    v >>= 8;
                }
            }
            else
            {
                for(int i = (bc - 1); i >= 0; i--)
                {
                    ret[i] = (byte)(v & 0xff);
                    v >>= 8;
                }
            }
            return ret;
        }

        static void align(List<byte> v, long align = 2048)
        {
            while ((v.Count % align) != 0)
                v.Add(0);
        }

        static long align(long v, long align=2048)
        {
            var r = v % align;
            if (r == 0)
                return v;
            else
                return v + (align - r);
        }
    }

    class BootEntry
    {
        public int id;
        public enum BootType { BIOS, EFI };
        public string fname, ffname;
        public int sector_count = -1;
        public bool boot_table = false;
        public bool bootable = true;
        public BootType type = BootType.BIOS;
        public enum EmulType { NoEmul, Floppy, Hard };
        public EmulType etype = EmulType.Floppy;
        public int load_seg = 0;
        public bool valid = false;
        public AnnotatedFSO afso_boot_file;
    }
}
