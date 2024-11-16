
namespace KanchokuWS.Forms
{
    partial class FrmCandidateSelector
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
            if (disposing && (components != null)) {
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
            this.dgvHorizontal = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHorizontal)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvHorizontal
            // 
            this.dgvHorizontal.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvHorizontal.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHorizontal.Location = new System.Drawing.Point(0, 0);
            this.dgvHorizontal.Name = "dgvHorizontal";
            this.dgvHorizontal.RowTemplate.Height = 21;
            this.dgvHorizontal.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.dgvHorizontal.Size = new System.Drawing.Size(201, 111);
            this.dgvHorizontal.TabIndex = 30;
            this.dgvHorizontal.SelectionChanged += new System.EventHandler(this.dgvHorizontal_SelectionChanged);
            // 
            // FrmCandidateSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(201, 111);
            this.Controls.Add(this.dgvHorizontal);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmCandidateSelector";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "漢直窓 WS";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmDisplayBuffer_FormClosing);
            this.Load += new System.EventHandler(this.FrmCandidateSelector_Load);
            this.VisibleChanged += new System.EventHandler(this.FrmDisplayBuffer_VisibleChanged);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHorizontal)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.DataGridView dgvHorizontal;
    }
}
