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
        static void Main(string[] args)
        {
            var ofname = "test.iso";

            var src_dir = "iso_image/iso";

            var d = new System.IO.DirectoryInfo(src_dir);

            var o = new System.IO.BinaryWriter(new System.IO.FileStream(ofname, System.IO.FileMode.Create));


            // Write 32 kiB of zeros to the system area
            o.Write(new byte[32 * 1024]);

            List<VolumeDescriptor> voldescs = new List<VolumeDescriptor>();

            // Add a primary volume descriptor
            var pvd = new PrimaryVolumeDescriptor();
            voldescs.Add(pvd);

            voldescs.Add(new VolumeDescriptorSetTerminator());

            // Generate directory tree
            List<AnnotatedFSO> files, dirs;
            var afso = AnnotatedFSO.BuildAFSOTree(d, out dirs, out files);

            // Allocate space for files + directories
            int cur_lba = 0x10 + voldescs.Count;

            List<AnnotatedFSO> output_order = new List<AnnotatedFSO>();
            foreach(var file in files)
            {
                var fi = file.fsi as FileInfo;
                var l = align(fi.Length);
                var lbal = l / 2048;

                file.lba = cur_lba;
                file.len = (int)fi.Length;
                cur_lba += (int)lbal;

                output_order.Add(file);
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
            pvd.WriteInt("VolumeSequenceNumber", 0);
            pvd.WriteInt("LogicalBlockSize", 2048);
            pvd.WriteInt("PathTableSize", path_table_l.Length);
            pvd.WriteInt("LocTypeLPathTable", path_table_l_lba);
            pvd.WriteInt("LocTypeMPathTable", path_table_b_lba);
            pvd.WriteBytes("RootDir", root_entry);


            // Write out volume descriptors
            foreach (var vd in voldescs)
                vd.Write(o);

            // files
            foreach(var f in output_order)
            {
                o.Seek(f.lba * 2048, SeekOrigin.Begin);
                var fin = new BinaryReader(((FileInfo)f.fsi).OpenRead());
                var b = fin.ReadBytes(f.len);
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

            // Align to sector size
            while ((o.BaseStream.Position % 2048) != 0)
                o.Write((byte)0);

            o.Close();
        }

        private static void insert_bytes(byte[] src, int doffset, List<byte> dest)
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

            cur_afso.len = dir_tree.Count - cur_count;

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
            if(is_root_dot)
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
            // Add SUSP ST
            //rr.Add(0x53); rr.Add(0x54);     // "ST"
            //rr.Add(4);                      // length
            //rr.Add(1);                      // ver

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
            ret.AddRange(short_lsb_msb(0));

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

            var fn_cmp = afn.CompareTo(bfn);
            if (fn_cmp != 0)
                return fn_cmp;

            var ext_len = Math.Max(a.Ext == null ? 0 : a.Ext.Length, b.Ext == null ? 0 : b.Ext.Length);
            var aext = (a.Ext ?? "").PadRight(ext_len);
            var bext = (b.Ext ?? "").PadRight(ext_len);

            return aext.CompareTo(bext);
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
}
