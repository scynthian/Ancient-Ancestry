using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ancient_Ancestry
{
    public partial class Acknowledgements : Form
    {
        public Acknowledgements()
        {
            InitializeComponent();
        }

        private void Acknowledgements_Load(object sender, EventArgs e)
        {
            textBox1.SelectionStart = 0;
            textBox1.SelectionLength = 0;
        }
    }
}
