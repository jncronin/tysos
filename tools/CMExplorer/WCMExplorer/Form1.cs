using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WCMExplorer
{
    public partial class Form1 : Form
    {
        CMExpLib.MetadataObject cur_obj = null;
        List<CMExpLib.MetadataObject> history = new List<CMExpLib.MetadataObject>();
        int hx_id = -1;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            load_root();
        }

        private void load_root()
        {
            if (Program.ehdr != null)
            {
                CMExpLib.MetadataObject root = Program.ehdr.GetRoot();
                history.Clear();
                SelectObject(root);
                DisplayObject(root);
                fname_tb.Text = Program.ehdr.FileName;
            }
        }

        private void DisplayObject(CMExpLib.MetadataObject obj)
        {
            cur_obj = obj;

            label1.Text = obj.LayoutName + "@ 0x" + obj.Address.ToString("X16");
            listView1.Items.Clear();
            listBox1.Items.Clear();

            foreach (KeyValuePair<string, object> kvp in obj.Fields)
            {
                string val = kvp.Value.ToString();
                if (kvp.Value.GetType().IsArray)
                    val = "->";
                listView1.Items.Add(new MyListViewItem(new string[] { kvp.Key, val }, kvp.Value));
            }
        }

        void SelectObject(CMExpLib.MetadataObject obj)
        {
            while ((history.Count - 1) > hx_id)
                history.RemoveAt(hx_id + 1);
            history.Add(obj);
            hx_id++;
        }

        void Back()
        {
            if (hx_id > 0)
            {
                hx_id--;
                DisplayObject(history[hx_id]);
            }
        }

        void Forward()
        {
            if (hx_id < (history.Count - 1))
            {
                hx_id++;
                DisplayObject(history[hx_id]);
            }
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected)
                DisplayArray(((MyListViewItem)e.Item).val);
        }

        private void DisplayArray(object p)
        {
            listBox1.Items.Clear();
            if (p.GetType().IsArray)
            {
                System.Type t = p.GetType();
                int len = (int)t.GetProperty("Length").GetValue(p, null);
                for (int i = 0; i < len; i++)
                {
                    object o = t.GetMethod("Get").Invoke(p, new object[] { i });
                    listBox1.Items.Add(o);
                }
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                object o = listBox1.Items[listBox1.SelectedIndex];
                SelectItem(o);
            }
        }

        private void SelectItem(object o)
        {
            if (o.GetType() == typeof(ulong))
            {
                try
                {
                    CMExpLib.MetadataObject mo = CMExpLib.MetadataObject.ReadVaddr((ulong)o, Program.ehdr);
                    SelectObject(mo);
                    DisplayObject(mo);
                }
                catch (Exception)
                { }
            }
            else if (o.GetType() == typeof(CMExpLib.MetadataObject.Reference))
            {
                CMExpLib.MetadataObject mo = CMExpLib.MetadataObject.ReadVaddr(((CMExpLib.MetadataObject.Reference)o).Address, Program.ehdr);
                SelectObject(mo);
                DisplayObject(mo);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Back();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Forward();
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                MyListViewItem lv_item = (MyListViewItem)listView1.SelectedItems[0];
                if (lv_item != null)
                {
                    object o = lv_item.val;
                    SelectItem(o);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CMExpLib.MetadataObject root = Program.ehdr.GetRoot();
            SelectObject(root);
            DisplayObject(root);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CMExpLib.MetadataObject syms = Program.ehdr.GetSymbols();
            SelectObject(syms);
            DisplayObject(syms);
        }

        private void fileload_btn_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            DialogResult dr = ofd.ShowDialog();
            if ((dr == System.Windows.Forms.DialogResult.OK) && (ofd.FileName != null))
            {
                Program.LoadObjectFile(ofd.FileName);
                load_root();
            }
        }
    }

    public class MyListViewItem : ListViewItem
    {
        public object val;
        public MyListViewItem(string[] items, object _val) : base(items) { val = _val; }
    }
}
