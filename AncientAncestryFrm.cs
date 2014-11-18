using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Ancient_Ancestry
{
    public partial class AncientAncestryFrm : Form
    {
        //TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        Dictionary<string, XmlNode> kvpair = new Dictionary<string, XmlNode>();
        XmlNode current_node = null;
        Dictionary<string, string> user_gt = new Dictionary<string, string>();
        Dictionary<string, List<string[]>> segment_lookup = new Dictionary<string, List<string[]>>();
        List<string> matching_segments = new List<string>();

        static int quality_threshold = 80;
        string filename = null;

        delegate void expandNode(TreeNode node);

        public AncientAncestryFrm()
        {
            InitializeComponent();
        }

        public Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private void AncientAncestryFrm_Load(object sender, EventArgs e)
        {

            using (Stream s = GenerateStreamFromString(Ancient_Ancestry.Properties.Resources.atree))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(s);
                TreeNode rootnode = new TreeNode("Adam / Eve");
                treeView1.Nodes.Add(rootnode);
                foreach (XmlNode nnode in xmlDoc.ChildNodes[0].ChildNodes)
                    populateLeaf(nnode, rootnode);
                label1.Text = "Adam / Eve";
                treeView1.Nodes[0].Expand();
            }
        }

        private void populateLeaf(XmlNode xmlnode,TreeNode treenode)
        {
            if (xmlnode.Name != "CA")
                return;
            TreeNode currnode = new TreeNode(xmlnode.Attributes["NAME"].Value.Replace("/"," / "));
            currnode.Name = xmlnode.Attributes["NAME"].Value.Replace("/", " / ");
            treenode.Nodes.Add(currnode);
            kvpair.Add(currnode.FullPath, xmlnode);
            if (xmlnode.HasChildNodes)
            {
                foreach(XmlNode xnode in xmlnode.ChildNodes)
                    populateLeaf(xnode, currnode);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void TreeNodeAction(TreeNode node)
        {
            label1.Text = node.Text;
            
            if (kvpair.ContainsKey(node.FullPath))
            {
                current_node = kvpair[node.FullPath];
                string kit = current_node.Attributes["ID"].Value;
                string kits = "";
                matching_sgmts_dgv.Rows.Clear();
                foreach (XmlNode xnode in current_node.SelectSingleNode("KITS").ChildNodes)
                {
                    kits += " " + xnode.Attributes["NAME"].Value.Replace("/", " / ");
                }
                matching_kits_txt.Text = kits.Trim().Replace(" ", ", ");

                //

                DataGridViewCellStyle match_style = new DataGridViewCellStyle();
                match_style.BackColor = Color.Green;
                match_style.ForeColor = Color.White;
                string len_mb = null;
                foreach (XmlNode xnode in current_node.SelectSingleNode("SEGMENTS").ChildNodes)
                {
                    len_mb = ((int.Parse(xnode.Attributes["END"].Value) - int.Parse(xnode.Attributes["START"].Value)) / 1000000.00).ToString("##0.00")+" Mb";
                    matching_sgmts_dgv.Rows.Add(new string[] { xnode.Attributes["CHR"].Value, xnode.Attributes["START"].Value, xnode.Attributes["END"].Value, len_mb });

                    if (matching_segments.Contains(kit + "_" + xnode.Attributes["CHR"].Value + ":" + xnode.Attributes["START"].Value + ":" + xnode.Attributes["END"].Value))
                    {
                        DataGridViewRow row = matching_sgmts_dgv.Rows[matching_sgmts_dgv.Rows.Count - 1];
                        foreach (DataGridViewCell cell in row.Cells)
                            cell.Style = match_style;
                    }
                    
                }
                matching_sgmts_dgv.Sort(matching_sgmts_dgv.Columns[0], ListSortDirection.Ascending);
            }
            else if (node.FullPath == "Adam / Eve")
            {
                matching_kits_txt.Text = "";
                matching_sgmts_dgv.Rows.Clear();
            }
        }

        private void matching_sgmts_dgv_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if (e.Column.Index == 0)
            {
                if (double.Parse(e.CellValue1.ToString()) > double.Parse(e.CellValue2.ToString()))
                {
                    e.SortResult = 1;
                }
                else if (double.Parse(e.CellValue1.ToString()) < double.Parse(e.CellValue2.ToString()))
                {
                    e.SortResult = -1;
                }
                else
                {
                    e.SortResult = 0;
                }
                e.Handled = true;
            }
        }

        private void matching_sgmts_dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (current_node != null && matching_sgmts_dgv.SelectedRows.Count>0)
            {
                DataGridViewCellCollection cells = matching_sgmts_dgv.SelectedRows[0].Cells;
                string chr = cells[0].Value.ToString();
                string start = cells[1].Value.ToString();
                string end = cells[2].Value.ToString();
                string key = current_node.Attributes["ID"].Value + "_" + chr + ":" + start + ":" + end;
                List<string[]> rows=new List<string[]>();
                if (segment_lookup.ContainsKey(key))
                    rows = segment_lookup[key];
                SNPViewFrm frm = new SNPViewFrm(current_node.Attributes["ID"].Value, chr, start, end, rows,matching_segments.Contains(key));
                frm.ShowDialog(this);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if(dlg.ShowDialog(this)==DialogResult.OK)
            {
                filename = dlg.FileName;
                statusLbl.Text = "Processing " + Path.GetFileName(filename);
                if (!backgroundWorker1.IsBusy)
                    backgroundWorker1.RunWorkerAsync(filename);
                    
            }
        }

        private void load(string user_file)
        {
            foreach (string caption in kvpair.Keys)
            {
                TreeNode[] nodes = treeView1.Nodes.Find(kvpair[caption].Attributes["NAME"].Value.Replace("/", " / "), true);
                if (nodes.Length > 0)
                {
                    nodes[0].BackColor = Color.White;
                    nodes[0].ForeColor = Color.Black;
                }
            }

            segment_lookup.Clear();
            user_gt.Clear();
            matching_segments.Clear();

            string[] lines = File.ReadAllLines(user_file);
            string[] data2 = null;
            foreach (string line in lines)
            {
                if (line.StartsWith("RSID") || line.StartsWith("#") || line == "")
                    continue;
                data2 = line.Replace("\"", "").Replace("\t", ",").Split(new char[] { ',' });
                user_gt.Add(data2[0], data2[3]);
            }

            string kit = null;
            string name = null;
            foreach (string caption in kvpair.Keys)
            {
                XmlNode canode = kvpair[caption];
                kit = canode.Attributes["ID"].Value;
                name = canode.Attributes["NAME"].Value.Replace("/", " / ");
                foreach (XmlNode xnode in canode.SelectSingleNode("SEGMENTS").ChildNodes)
                {
                    string chr = xnode.Attributes["CHR"].Value;
                    int start = int.Parse(xnode.Attributes["START"].Value);
                    int end = int.Parse(xnode.Attributes["END"].Value);
                    List<string[]> rows = new List<string[]>();
                    int total_count = 0;
                    int no_call_count = 0;
                    int mismatch_count = 0;
                    using (var fs = new MemoryStream(Ancient_Ancestry.Properties.Resources.ibd))
                    using (var zf = new ZipFile(fs))
                    {
                        var ze = zf.GetEntry(kit);
                        if (ze == null)
                        {
                            throw new ArgumentException(kit, "not found in Zip");
                        }

                        using (var s = zf.GetInputStream(ze))
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                string line = null;
                                string[] data = null;
                                rows.Clear();
                                total_count = 0;
                                no_call_count = 0;
                                mismatch_count = 0;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    data = line.Split(new char[] { '\t' });
                                    if (data[1] == chr && int.Parse(data[2]) >= start && int.Parse(data[2]) <= end)
                                    {
                                        rows.Add(new string[] { data[0], data[1], data[2], data[3], getUserGT(data[0]) });
                                        total_count++;
                                        if (!isGTMatch(data[3], getUserGT(data[0])))
                                            mismatch_count++;
                                        if(isNoCall(getUserGT(data[0])))
                                            no_call_count++;
                                    }
                                }
                                segment_lookup.Add(kit + "_" + chr + ":" + start + ":" + end, rows);
                                double quality = ((total_count - no_call_count - mismatch_count * 2) * 100 / total_count);
                                if (quality >= quality_threshold) // allow a quality
                                {
                                    matching_segments.Add(kit + "_" + chr + ":" + start + ":" + end);
                                    TreeNode[] nodes = treeView1.Nodes.Find(name, true);
                                    if (nodes.Length > 0)
                                    {
                                        nodes[0].BackColor = Color.Green;
                                        nodes[0].ForeColor = Color.White;

                                        expandTreeNode(nodes[0]);

                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        private void expandTreeNode(TreeNode node)
        {
            if (treeView1.InvokeRequired)
                treeView1.Invoke(new expandNode(expandTreeNode), node);
            else
                node.EnsureVisible();

        }

        private bool isNoCall(string p)
        {
            if (p.IndexOf("-") != -1 || p.IndexOf("0") != -1)
                return true;
            return false;
        }

        public static bool isGTMatch(string p1, string p2)
        {
            if (p1.Length == 1)
                p1 = p1 + p1;
            if (p2.Length == 1)
                p2 = p2 + p2;

            if (p1.Length >2)
                p1 = p1.Substring(0,2);
            if (p2.Length >2)
                p2 = p2.Substring(0, 2);

            if (p1 == p2||p1.Reverse().ToString()==p2)
                return true;
            if (p1[0]==p2[0]||p1[0]==p2[1]||p1[1]==p2[0]||p1[1]==p2[1])
                return true;
            if (p1.IndexOf("-") != -1 || p2.IndexOf("-") != -1 || p1.IndexOf("0") != -1 || p2.IndexOf("0") != -1)
                return true;
            return false;
        }

        private string getUserGT(string p)
        {
            if (user_gt.ContainsKey(p))
                return user_gt[p];
            else
                return "-";
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = e.Node;
            TreeNodeAction(node);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            load(e.Argument.ToString());
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusLbl.Text = matching_segments.Count.ToString() + " matching segments found.";
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            quality_threshold = 100;

            toolStripMenuItem4.Checked = false;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = false;
            doProcess();
        }

        private void doProcess()
        {
            if (filename != null)
            {
                statusLbl.Text = "Processing " + Path.GetFileName(filename);
                if (!backgroundWorker1.IsBusy)
                    backgroundWorker1.RunWorkerAsync(filename);
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            quality_threshold = 90;

            toolStripMenuItem5.Checked = false;
            toolStripMenuItem3.Checked = false;
            toolStripMenuItem4.Checked = false;
            doProcess();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            quality_threshold = 80;

            toolStripMenuItem5.Checked = false;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem4.Checked = false;
            doProcess();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            quality_threshold = 75;

            toolStripMenuItem5.Checked = false;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = false;
            doProcess();
        }
    }
}
