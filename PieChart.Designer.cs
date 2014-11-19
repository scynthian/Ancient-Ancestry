namespace Ancient_Ancestry
{
    partial class PieChart
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pieChartControl1 = new System.Drawing.PieChart.PieChartControl();
            this.SuspendLayout();
            // 
            // pieChartControl1
            // 
            this.pieChartControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pieChartControl1.Location = new System.Drawing.Point(0, 0);
            this.pieChartControl1.Name = "pieChartControl1";
            this.pieChartControl1.Size = new System.Drawing.Size(464, 326);
            this.pieChartControl1.TabIndex = 0;
            this.pieChartControl1.ToolTips = null;
            // 
            // PieChart
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 326);
            this.Controls.Add(this.pieChartControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "PieChart";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Pie Chart - Matching Triangulated Segments";
            this.Load += new System.EventHandler(this.PieChart_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Drawing.PieChart.PieChartControl pieChartControl1;

    }
}