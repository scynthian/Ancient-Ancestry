using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Ancient_Ancestry
{
    public partial class SNPViewFrm : Form
    {
        string chr=null;
        string start=null;
        string end = null;
        string ca = null;
        SortedDictionary<string, string> kits = new SortedDictionary<string, string>();
        Dictionary<int, string> pos_rsid_map = new Dictionary<int, string>();
        List<string[]> rows = null;
        bool match = false;
        List<string> kk_list = null;
        List<string> names_list = null;
        Dictionary<string, string> kit_gt = new Dictionary<string, string>();

        public SNPViewFrm(string ca, string mchr, string mstart, string mend, List<string[]> mrows, bool mmatch, List<string> mkk_list, List<string> mnames_list, Dictionary<string, string> mkit_gt)
        {
            this.chr = mchr;
            this.start = mstart;
            this.end = mend;
            this.ca = ca;
            this.rows = mrows;
            this.match = mmatch;
            this.kk_list = mkk_list;
            this.names_list = mnames_list;
            this.kit_gt = mkit_gt;
            InitializeComponent();
        }

        private void dataGridView1_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
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

        private void SNPViewFrm_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }
        
        private void populate(string kit)
        {
            DataGridViewCellStyle mismatch_style = new DataGridViewCellStyle();
            mismatch_style.BackColor = Color.Red;
            mismatch_style.ForeColor = Color.Yellow;
            DataGridViewCellStyle nocall_style = new DataGridViewCellStyle();
            nocall_style.BackColor = Color.LightGray;
            DataGridViewCellStyle default_style = new DataGridViewCellStyle();
            default_style.BackColor = Color.FromArgb(0xfe, 0xfe, 0xfe);

            foreach (string col in kk_list)
                dataGridView1.Columns.Add(col, names_list[kk_list.IndexOf(col)]);

            string[] krow = null;
            if (rows.Count == 0)
            {
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
                            while ((line = sr.ReadLine()) != null)
                            {
                                data = line.Split(new char[] { '\t' });
                                if (data[1] == this.chr && int.Parse(data[2]) >= int.Parse(this.start) && int.Parse(data[2]) <= int.Parse(this.end))
                                {

                                    krow = new string[5 + kk_list.Count];
                                    krow[0] = data[0];
                                    krow[1] = data[1];
                                    krow[2] = data[2];
                                    krow[3] = data[3];
                                    krow[4] = "-";
                                    for (int u = 5; u < 5 + kk_list.Count; u++)
                                        krow[u] = kit_gt[kk_list[u-5] + ":" + data[0]]; // genotype....

                                    dataGridView1.Rows.Add(krow);
                                }
                            }
                        }
                    }
                }
                statusLbl.Text = dataGridView1.Rows.Count + " total SNPs";
            }
            else
            {
                // user data exits.
                int no_call_count =0;
                int mis_count=0;
                foreach (string[] row in rows)
                {
                    dataGridView1.Rows.Add(row);
                    if (!AncientAncestryFrm.isGTMatch(row[3],row[4]))
                    {
                        DataGridViewRow row1 = dataGridView1.Rows[dataGridView1.Rows.Count - 1];
                        row1.Cells[4].Style = mismatch_style;
                        mis_count++;
                    }
                    else if (row[4]=="-")
                    {
                        DataGridViewRow row1 = dataGridView1.Rows[dataGridView1.Rows.Count - 1];
                        row1.Cells[4].Style = nocall_style;
                        no_call_count++;
                    }
                    else
                    {
                        DataGridViewRow row1 = dataGridView1.Rows[dataGridView1.Rows.Count - 1];
                        row1.Cells[4].Style = default_style;
                        
                    }
                       
                }
                string quality = ((rows.Count- no_call_count - mis_count * 2) * 100 / rows.Count).ToString("##0.00") + "%";
                statusLbl.Text = rows.Count + " total SNPs / " + no_call_count + " no-calls / " + mis_count + " mismatches. Quality: " + quality;
            }
            dataGridView1.Sort(dataGridView1.Columns[2], ListSortDirection.Ascending);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            if (match)
                this.Text = "Segment View - Chr" + chr + ": " + start + " - " + end + " [Matching Segment]";
            else
                this.Text = "Segment View - Chr" + chr + ": " + start + " - " + end;
            //
            populate(ca);
            //

        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            string rsid = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
            if(MessageBox.Show("Do you want more information about "+rsid+" from internet? Clicking 'Yes' will open the default web browser to SNPedia entry on "+rsid,"Connect to Internet?",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)
            {
                Process.Start("http://www.snpedia.com/index.php/" + rsid);
            }
        }
    }
}
