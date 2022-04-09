
namespace KanchokuWS
{
    partial class DlgModConversion
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
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.comboBox_modKeys = new System.Windows.Forms.ComboBox();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.comboBox_shiftPlaneOn = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_shiftPlaneOff = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.radioButton_modKeys = new System.Windows.Forms.RadioButton();
            this.radioButton_singleHit = new System.Windows.Forms.RadioButton();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(537, 415);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 6;
            this.buttonOK.Text = "設定(&O)";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(414, 415);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(76, 23);
            this.buttonCancel.TabIndex = 5;
            this.buttonCancel.Text = "キャンセル(&C)";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 9);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(73, 12);
            this.label7.TabIndex = 2;
            this.label7.Text = "拡張修飾キー";
            // 
            // comboBox_modKeys
            // 
            this.comboBox_modKeys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_modKeys.FormattingEnabled = true;
            this.comboBox_modKeys.Items.AddRange(new object[] {
            "SandS",
            "CapsLock",
            "英数",
            "無変換",
            "変換",
            "右シフト"});
            this.comboBox_modKeys.Location = new System.Drawing.Point(88, 5);
            this.comboBox_modKeys.Name = "comboBox_modKeys";
            this.comboBox_modKeys.Size = new System.Drawing.Size(80, 20);
            this.comboBox_modKeys.TabIndex = 11;
            // 
            // dataGridView2
            // 
            this.dataGridView2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Location = new System.Drawing.Point(13, 31);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.RowTemplate.Height = 21;
            this.dataGridView2.Size = new System.Drawing.Size(599, 378);
            this.dataGridView2.TabIndex = 1;
            // 
            // comboBox_shiftPlaneOn
            // 
            this.comboBox_shiftPlaneOn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_shiftPlaneOn.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_shiftPlaneOn.FormattingEnabled = true;
            this.comboBox_shiftPlaneOn.Items.AddRange(new object[] {
            "なし",
            "通常シフト",
            "拡張シフトA",
            "拡張シフトB"});
            this.comboBox_shiftPlaneOn.Location = new System.Drawing.Point(311, 5);
            this.comboBox_shiftPlaneOn.Name = "comboBox_shiftPlaneOn";
            this.comboBox_shiftPlaneOn.Size = new System.Drawing.Size(88, 20);
            this.comboBox_shiftPlaneOn.TabIndex = 13;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(214, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 12);
            this.label1.TabIndex = 12;
            this.label1.Text = "漢直ON時シフト面";
            // 
            // comboBox_shiftPlaneOff
            // 
            this.comboBox_shiftPlaneOff.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_shiftPlaneOff.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_shiftPlaneOff.FormattingEnabled = true;
            this.comboBox_shiftPlaneOff.Items.AddRange(new object[] {
            "なし",
            "通常シフト",
            "拡張シフトA",
            "拡張シフトB"});
            this.comboBox_shiftPlaneOff.Location = new System.Drawing.Point(524, 5);
            this.comboBox_shiftPlaneOff.Name = "comboBox_shiftPlaneOff";
            this.comboBox_shiftPlaneOff.Size = new System.Drawing.Size(88, 20);
            this.comboBox_shiftPlaneOff.TabIndex = 15;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(421, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 12);
            this.label2.TabIndex = 14;
            this.label2.Text = "漢直OFF時シフト面";
            // 
            // radioButton_modKeys
            // 
            this.radioButton_modKeys.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_modKeys.AutoSize = true;
            this.radioButton_modKeys.Location = new System.Drawing.Point(13, 419);
            this.radioButton_modKeys.Name = "radioButton_modKeys";
            this.radioButton_modKeys.Size = new System.Drawing.Size(91, 16);
            this.radioButton_modKeys.TabIndex = 16;
            this.radioButton_modKeys.TabStop = true;
            this.radioButton_modKeys.Text = "修飾キー設定";
            this.radioButton_modKeys.UseVisualStyleBackColor = true;
            this.radioButton_modKeys.CheckedChanged += new System.EventHandler(this.radioButton_modKeys_CheckedChanged);
            // 
            // radioButton_singleHit
            // 
            this.radioButton_singleHit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_singleHit.AutoSize = true;
            this.radioButton_singleHit.Location = new System.Drawing.Point(121, 419);
            this.radioButton_singleHit.Name = "radioButton_singleHit";
            this.radioButton_singleHit.Size = new System.Drawing.Size(71, 16);
            this.radioButton_singleHit.TabIndex = 17;
            this.radioButton_singleHit.TabStop = true;
            this.radioButton_singleHit.Text = "単打設定";
            this.radioButton_singleHit.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(13, 31);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 21;
            this.dataGridView1.Size = new System.Drawing.Size(599, 378);
            this.dataGridView1.TabIndex = 18;
            // 
            // DlgModConversion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 441);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.radioButton_singleHit);
            this.Controls.Add(this.radioButton_modKeys);
            this.Controls.Add(this.dataGridView2);
            this.Controls.Add(this.comboBox_shiftPlaneOff);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_shiftPlaneOn);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.comboBox_modKeys);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.label7);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(640, 400);
            this.Name = "DlgModConversion";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "拡張修飾キー設定";
            this.Load += new System.EventHandler(this.DlgModConversion_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboBox_modKeys;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.ComboBox comboBox_shiftPlaneOn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_shiftPlaneOff;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton radioButton_modKeys;
        private System.Windows.Forms.RadioButton radioButton_singleHit;
        private System.Windows.Forms.DataGridView dataGridView1;
    }
}