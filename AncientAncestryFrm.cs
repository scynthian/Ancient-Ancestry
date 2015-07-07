using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
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
        static public Dictionary<string, string> kit_gt = new Dictionary<string, string>();
        Dictionary<string, string> id_name = new Dictionary<string, string>();
        static int quality_threshold = 80;
        string filename = null;

        Dictionary<string,int> shared_dna = new Dictionary<string,int>();

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
                id_name.Clear();
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(s);
                TreeNode rootnode = new TreeNode("Adam / Eve");
                treeView1.Nodes.Add(rootnode);
                foreach (XmlNode nnode in xmlDoc.ChildNodes[0].ChildNodes)
                    populateLeaf(nnode, rootnode);
                label1.Text = "Adam / Eve";
                treeView1.Nodes[0].Expand();
            }
            if (!backgroundWorker2.IsBusy)
                backgroundWorker2.RunWorkerAsync();
        }

        private void populateLeaf(XmlNode xmlnode,TreeNode treenode)
        {
            if (xmlnode.Name != "CA")
                return;
            TreeNode currnode = new TreeNode(xmlnode.Attributes["NAME"].Value.Replace("/"," / "));
            currnode.Name = xmlnode.Attributes["NAME"].Value.Replace("/", " / ");

            //id_name
            foreach (XmlNode kit in xmlnode.SelectSingleNode("KITS").ChildNodes)
            {
                if (!id_name.ContainsKey(kit.Attributes["ID"].Value))
                    id_name.Add(kit.Attributes["ID"].Value, kit.Attributes["NAME"].Value);
            }

            treenode.Nodes.Add(currnode);
            if (!kvpair.ContainsKey(currnode.FullPath))
            {
                kvpair.Add(currnode.FullPath, xmlnode);
                if (xmlnode.HasChildNodes)
                {
                    foreach (XmlNode xnode in xmlnode.ChildNodes)
                        populateLeaf(xnode, currnode);
                }
            }
            else
                MessageBox.Show("Error: Check the XML for '"+currnode.FullPath+"' - duplicate entry.");
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

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if(dlg.ShowDialog(this)==DialogResult.OK)
            {
                filename = dlg.FileName;
                doProcess();
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

            shared_dna.Clear();



            string[] lines = null;
            
            if(user_file.EndsWith(".gz"))
            {
                String line1 = null;
                List<string> list1 = new List<string>();
                StringReader reader = new StringReader(Encoding.UTF8.GetString(Unzip(File.ReadAllBytes(user_file))));
                while ((line1 = reader.ReadLine()) != null)
                {
                    list1.Add(line1);
                }
                reader.Close();
                lines = list1.ToArray();
            }
            else if (user_file.EndsWith(".zip"))
            {
                List<string> list1 = new List<string>();
                using (var fs = new MemoryStream(File.ReadAllBytes(user_file)))
                using (var zf = new ZipFile(fs))
                {
                    var ze = zf[0];
                    if (ze == null)
                    {
                        throw new ArgumentException("file not found in Zip");
                    }
                    using (var s = zf.GetInputStream(ze))
                    {
                        using (StreamReader sr = new StreamReader(s))
                        {
                            string line = null;
                            while ((line = sr.ReadLine()) != null)
                            {
                                list1.Add(line);
                            }
                        }
                    }
                }
                lines = list1.ToArray();
            }
            else
                lines = File.ReadAllLines(user_file);

            string[] data2 = null;
            foreach (string line in lines)
            {
                if (line.ToUpper().StartsWith("RSID") || line.StartsWith("#") || line == "")
                    continue;
                data2 = line.Replace("\"", "").Replace("\t", ",").Split(new char[] { ',' });
                if (!user_gt.ContainsKey(data2[0]))
                {
                    if (data2.Length == 5)//ancestry ..
                    {
                        user_gt.Add(data2[0], data2[3] + data2[4]);
                    }
                    else
                    {
                        user_gt.Add(data2[0], data2[3]);
                    }
                }
            }

            string kit = null;
            string name = null;
            foreach (string caption in kvpair.Keys)
            {
                XmlNode canode = kvpair[caption];
                kit = canode.Attributes["ID"].Value;
                name = canode.Attributes["NAME"].Value.Replace("/", " / ");
                string kk = null;
                List<string> kk_list = new List<string>();
                foreach (XmlNode xnode in canode.SelectSingleNode("KITS").ChildNodes)
                {
                    kk = xnode.Attributes["ID"].Value;
                    kk_list.Add(kk);
                }

                foreach (XmlNode xnode in canode.SelectSingleNode("SEGMENTS").ChildNodes)
                {
                    string chr = xnode.Attributes["CHR"].Value;
                    int start = int.Parse(xnode.Attributes["START"].Value);
                    int end = int.Parse(xnode.Attributes["END"].Value);
                    List<string[]> rows = new List<string[]>();
                    int total_count = 0;
                    int no_call_count = 0;
                    int mismatch_count = 0;
                    string[] krow = null;
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
                                        krow = new string[5 + kk_list.Count];
                                        krow[0]=data[0];
                                        krow[1]=data[1];
                                        krow[2]=data[2];
                                        krow[3]=data[3];
                                        krow[4] = getUserGT(data[0]);
                                        for (int u = 5; u < 5 + kk_list.Count; u++)
                                            krow[u] = kit_gt[ kk_list[u - 5]+":"+data[0]];
                                            rows.Add(krow);
                                        total_count++;
                                        if (!isGTMatch(data[3], getUserGT(data[0])))
                                            mismatch_count++;
                                        if(isNoCall(getUserGT(data[0])))
                                            no_call_count++;
                                    }
                                }
                                segment_lookup.Add(kit + "_" + chr + ":" + start + ":" + end, rows);
                                double quality = ((total_count - no_call_count - mismatch_count * 2) * 100 / total_count);
                                int tmp = 0;
                                if (quality >= quality_threshold) // allow a quality
                                {
                                    foreach (string n in name.Split(new char[] { '/' }))
                                    {
                                        if (shared_dna.ContainsKey(n.Trim()))
                                        {
                                            tmp = shared_dna[n.Trim()];
                                            shared_dna.Remove(n.Trim());
                                        }
                                        else
                                            tmp = 0;

                                        tmp += (end - start);
                                        shared_dna.Add(n.Trim(), tmp);
                                    }

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
            reportToolStripMenuItem.Enabled = true;
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
                reportToolStripMenuItem.Enabled = false;
                statusLbl.Text = "Processing " + Path.GetFileName(filename)+" ...";
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

        public static byte[] Zip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static byte[] Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return mso.ToArray();
            }
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            kit_gt.Clear();
            StringReader reader = new StringReader(Encoding.UTF8.GetString( Unzip( Ancient_Ancestry.Properties.Resources.snps_list_txt)));
            string line = null;
            string[] data = null;
            while((line = reader.ReadLine())!=null)
            {
                if (line.Trim() == "")
                    continue;
                //999918:rs12464023,CC
                data = line.Split(new char[] { ',' });
                kit_gt.Add(data[0], data[1]);
            }
        }

        private void pieChartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (shared_dna.Count > 0)
            {
                PieChart chart = new PieChart(shared_dna);
                chart.ShowDialog(this);
            }
            else
            {
                if (filename==null)
                    MessageBox.Show("Please open an autosomal file first.");
                else
                    MessageBox.Show("No shared segments found!");
                return;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog(this);
        }

        private void licenseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            License lic = new License();
            lic.ShowDialog(this);
        }

        private void ancientDNAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.y-str.org/p/ancient-dna.html");
        }

        private void acknowledgementsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Acknowledgements ack = new Acknowledgements();
            ack.ShowDialog(this);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (shared_dna.Count > 0)
            {
                SaveFileDialog save = new SaveFileDialog();
                if (save.ShowDialog(this) == DialogResult.OK)
                {

                    string folder = Path.GetDirectoryName(save.FileName) + "\\" + Path.GetFileNameWithoutExtension(save.FileName);
                    folder = folder.Replace("\\\\", "\\");
                    if (!Directory.Exists(folder))
                    {
                        statusLbl.Text = "Please wait ...";
                        if (!backgroundWorker3.IsBusy)
                            backgroundWorker3.RunWorkerAsync(folder);
                    }
                    else
                        MessageBox.Show("Folder by name " + Path.GetFileNameWithoutExtension(save.FileName) + " already exists!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                if (filename == null)
                    MessageBox.Show("Please open an autosomal file first.");
                else
                    MessageBox.Show("No shared segments to save!");
                return;
            }
        }

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            string folder = e.Argument.ToString();
            Directory.CreateDirectory(folder);
            string[] tmp = null;
            StringBuilder sb = new StringBuilder();
            foreach (string kit in segment_lookup.Keys)
            {
                sb.Length = 0;
                tmp = kit.Split(new char[] { '_' });
                sb.Append("RSID,Chromosome,Position,Your Genotype,");
                foreach (string kt in tmp[0].Split(new char[] { '-' }))
                {
                    sb.Append(id_name[kt]);
                    sb.Append(",");
                }
                sb.Append("\r\n");
                foreach (string[] row in segment_lookup[kit])
                {
                    for (int i = 0; i < row.Length; i++)
                    {
                        if (i == 3)
                            continue;
                        sb.Append(row[i] + ",");
                    }
                    sb.Append("\r\n");
                }
                File.WriteAllText(folder + "\\Chr" + tmp[1].Replace(':', '_') + ".csv", sb.ToString().Replace(",\r\n", "\r\n"));
            }
            FastZip zip = new FastZip();
            zip.CreateZip(folder + ".zip", folder, false, null);
            Directory.Delete(folder, true);
            e.Result = folder;
        }

        private void backgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusLbl.Text = Path.GetFileName(e.Result.ToString()) + ".zip successfully saved.";
        }

        private void matching_sgmts_dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (current_node != null && matching_sgmts_dgv.SelectedRows.Count > 0)
            {
                DataGridViewCellCollection cells = matching_sgmts_dgv.SelectedRows[0].Cells;
                string chr = cells[0].Value.ToString();
                string start = cells[1].Value.ToString();
                string end = cells[2].Value.ToString();
                string key = current_node.Attributes["ID"].Value + "_" + chr + ":" + start + ":" + end;
                List<string[]> rows = new List<string[]>();
                if (segment_lookup.ContainsKey(key))
                    rows = segment_lookup[key];

                string kk = null;
                List<string> kk_list = new List<string>();
                List<string> kk_name = new List<string>();
                foreach (XmlNode xnode in current_node.SelectSingleNode("KITS").ChildNodes)
                {
                    kk = xnode.Attributes["ID"].Value;
                    kk_list.Add(kk);
                    kk_name.Add(xnode.Attributes["NAME"].Value);
                }

                SNPViewFrm frm = new SNPViewFrm(current_node.Attributes["ID"].Value, chr, start, end, rows, matching_segments.Contains(key), kk_list, kk_name, kit_gt);
                frm.ShowDialog(this);
            }
        }

    }
}
