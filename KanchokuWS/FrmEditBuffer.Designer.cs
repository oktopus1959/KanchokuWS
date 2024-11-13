
namespace KanchokuWS
{
    partial class FrmEditBuffer
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
            this.pictureBox_Main = new System.Windows.Forms.PictureBox();
            this.topTextBox = new Utils.TextBoxRO();
            this.dgvHorizontal = new System.Windows.Forms.DataGridView();
            this.pictureBox_measureFontSize = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Main)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHorizontal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_measureFontSize)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox_Main
            // 
            this.pictureBox_Main.Location = new System.Drawing.Point(1, 19);
            this.pictureBox_Main.Name = "pictureBox_Main";
            this.pictureBox_Main.Size = new System.Drawing.Size(201, 111);
            this.pictureBox_Main.TabIndex = 31;
            this.pictureBox_Main.TabStop = false;
            this.pictureBox_Main.Click += new System.EventHandler(this.pictureBox_Main_Click);
            this.pictureBox_Main.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_Main_MouseDown);
            this.pictureBox_Main.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox_Main_MouseMove);
            // 
            // topTextBox
            // 
            this.topTextBox.actionOnPaste = null;
            this.topTextBox.Location = new System.Drawing.Point(1, 1);
            this.topTextBox.Name = "topTextBox";
            this.topTextBox.Size = new System.Drawing.Size(201, 19);
            this.topTextBox.TabIndex = 29;
            this.topTextBox.TabStop = false;
            // 
            // dgvHorizontal
            // 
            this.dgvHorizontal.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvHorizontal.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHorizontal.Location = new System.Drawing.Point(1, 19);
            this.dgvHorizontal.Name = "dgvHorizontal";
            this.dgvHorizontal.RowTemplate.Height = 21;
            this.dgvHorizontal.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.dgvHorizontal.Size = new System.Drawing.Size(201, 91);
            this.dgvHorizontal.TabIndex = 30;
            this.dgvHorizontal.SelectionChanged += new System.EventHandler(this.dgvHorizontal_SelectionChanged);
            // 
            // pictureBox_measureFontSize
            // 
            this.pictureBox_measureFontSize.Location = new System.Drawing.Point(0, 100);
            this.pictureBox_measureFontSize.Name = "pictureBox_measureFontSize";
            this.pictureBox_measureFontSize.Size = new System.Drawing.Size(202, 23);
            this.pictureBox_measureFontSize.TabIndex = 33;
            this.pictureBox_measureFontSize.TabStop = false;
            this.pictureBox_measureFontSize.Visible = false;
            // 
            // FrmDisplayBuffer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(203, 130);
            this.Controls.Add(this.pictureBox_Main);
            this.Controls.Add(this.topTextBox);
            this.Controls.Add(this.dgvHorizontal);
            this.Controls.Add(this.pictureBox_measureFontSize);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmDisplayBuffer";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "漢直窓 WS";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmDisplayBuffer_FormClosing);
            this.Load += new System.EventHandler(this.FrmDisplayBuffer_Load);
            this.VisibleChanged += new System.EventHandler(this.FrmDisplayBuffer_VisibleChanged);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Main)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHorizontal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_measureFontSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Utils.TextBoxRO topTextBox;
        private System.Windows.Forms.DataGridView dgvHorizontal;
        private System.Windows.Forms.PictureBox pictureBox_Main;
        private System.Windows.Forms.PictureBox pictureBox_measureFontSize;
    }
}
