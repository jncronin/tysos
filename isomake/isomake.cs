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
                cur_lba += (int)lbal;

                output_order.Add(file);
            }
            foreach(var dir in dirs)
            {
                dir.lba = cur_lba++;
                output_order.Add(dir);
            }

            // create path table
            var path_table_l_lba = cur_lba;
            byte[] path_table_l = build_path_table(afso, false);
            var path_table_b_lba = path_table_l_lba + (int)align(path_table_l.Length) / 2048;
            byte[] path_table_b = build_path_table(afso, true);

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
            ret.Add((byte)(8 + id_len));
            ret.Add(0);

            ret.AddRange(ToByteArray(afso.lba, 4, lsb));
            ret.AddRange(ToByteArray(afso.Parent == null ? 0 : afso.Parent.dir_idx, 2, lsb));

            foreach (var c in afso.Identifier)
                ret.Add((byte)c);

            if ((id_len % 1) != 0)
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
