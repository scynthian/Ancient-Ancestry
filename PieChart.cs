using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.PieChart;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ancient_Ancestry
{
    public partial class PieChart : Form
    {
        Dictionary<string, int> shared_dna = null;
        public PieChart(Dictionary<string, int> m_shared_dna)
        {
            this.shared_dna = m_shared_dna;
            InitializeComponent();
        }

        private void PieChart_Load(object sender, EventArgs e)
        {
            List<decimal> values = new List<decimal>();
            List<string> text = new List<string>();
            List<string> tooltip = new List<string>();
            List<Color> colors = new List<Color>();

            decimal total = shared_dna.Values.Sum();


            Random randomGen = new Random();
            foreach (string key in shared_dna.Keys)
            {
                values.Add((decimal)shared_dna[key]);
                text.Add(key);
                tooltip.Add(((shared_dna[key] / total) * 100).ToString("#0.00") + " %");
                colors.Add(Color.FromArgb(225, getColor(randomGen)));
            }

            pieChartControl1.RightMargin = 10;
            pieChartControl1.LeftMargin = 10;
            pieChartControl1.TopMargin = 10;
            pieChartControl1.BottomMargin = 10;


            pieChartControl1.Values = values.ToArray();
            pieChartControl1.Texts = text.ToArray();
            pieChartControl1.Colors = colors.ToArray();
            pieChartControl1.ToolTips = tooltip.ToArray();
            pieChartControl1.ShadowStyle = ShadowStyle.GradualShadow;
            pieChartControl1.FitChart = true;
            pieChartControl1.SliceRelativeHeight = 0.25f;            
        }

        private Color getColor(Random rand)
        {           
            KnownColor[] names = (KnownColor[])Enum.GetValues(typeof(KnownColor));
            KnownColor randomColorName = names[rand.Next(names.Length)];
            Color randomColor = Color.FromKnownColor(randomColorName);            
            return randomColor;
        }
    }
}
