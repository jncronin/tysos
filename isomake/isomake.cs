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

            // Load up all directory infos - this becomes the path table
            List<PathTableEntry> ptes = new List<PathTableEntry>();

            List<PathTableEntry> cur_level = new List<PathTableEntry>();
            cur_level.Add(new PathTableEntry { di = d, parent_idx = 0 });

            Dictionary<DirectoryInfo, int> di_map = new Dictionary<DirectoryInfo, int>();
            di_map[d] = 0;

            // We are required to sort the path table in ascening order of directory level and alphabetically within each level
            while(true)
            {
                cur_level.Sort(DiSorter);

                List<PathTableEntry> next_level = new List<PathTableEntry>();

                foreach(var cl in cur_level)
                {
                    di_map[cl.di] = ptes.Count;
                    cl.cur_idx = ptes.Count;
                    ptes.Add(cl);

                    foreach(var child in cl.di.GetDirectories())
                    {
                        next_level.Add(new PathTableEntry { di = child, parent_idx = cl.cur_idx });
                    }
                }

                if (next_level.Count == 0)
                    break;
                else
                    cur_level = next_level;
            }
        }

        static int DiSorter(PathTableEntry a, PathTableEntry b)
        {
            return a.di.Name.CompareTo(b.di.Name);
        }

        class PathTableEntry
        {
            public DirectoryInfo di;
            public int parent_idx;
            public int cur_idx;
        }
    }
}
