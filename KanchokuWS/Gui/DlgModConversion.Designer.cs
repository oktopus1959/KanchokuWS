
namespace KanchokuWS.Gui
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DlgModConversion));
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label_modKeys = new System.Windows.Forms.Label();
            this.comboBox_modKeys = new System.Windows.Forms.ComboBox();
            this.dataGridView_extModifier = new System.Windows.Forms.DataGridView();
            this.comboBox_shiftPlaneOn = new System.Windows.Forms.ComboBox();
            this.label_shiftPlaneOn = new System.Windows.Forms.Label();
            this.comboBox_shiftPlaneOff = new System.Windows.Forms.ComboBox();
            this.label_shiftPlaneOff = new System.Windows.Forms.Label();
            this.radioButton_modKeys = new System.Windows.Forms.RadioButton();
            this.radioButton_singleHit = new System.Windows.Forms.RadioButton();
            this.dataGridView_singleHit = new System.Windows.Forms.DataGridView();
            this.dataGridView_shiftPlane = new System.Windows.Forms.DataGridView();
            this.radioButton_shiftPlane = new System.Windows.Forms.RadioButton();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox_help = new System.Windows.Forms.GroupBox();
            this.panel_shiftPlaneHint = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_extModifier)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_singleHit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_shiftPlane)).BeginInit();
            this.groupBox_help.SuspendLayout();
            this.panel_shiftPlaneHint.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(538, 415);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 10;
            this.buttonOK.Text = "書き出し(&W)";
            this.toolTip1.SetToolTip(this.buttonOK, "設定内容をファイルに書き出して、ダイアログを閉じます。\r\n\r\n変更した設定内容を漢直WSに反映させるには、元の設定画面で\r\n「再読込」を実行してください。");
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(427, 415);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(76, 23);
            this.buttonCancel.TabIndex = 9;
            this.buttonCancel.Text = "閉じる(&C)";
            this.toolTip1.SetToolTip(this.buttonCancel, "ダイアログを閉じます。\r\n\r\nファイルへの書き出しは行いませんが、ダイアログを閉じても修正結果は\r\nメモリ上に残るので、再度開いたときは前回の修正結果が表示されま" +
        "す。\r\n\r\n修正を元に戻したい場合は、ダイアログを閉じた後、元の画面で「再読込」を\r\n実行してください。\r\n");
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // label_modKeys
            // 
            this.label_modKeys.AutoSize = true;
            this.label_modKeys.Location = new System.Drawing.Point(13, 9);
            this.label_modKeys.Name = "label_modKeys";
            this.label_modKeys.Size = new System.Drawing.Size(73, 12);
            this.label_modKeys.TabIndex = 2;
            this.label_modKeys.Text = "拡張修飾キー";
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
            "右シフト",
            "左コントロール",
            "右コントロール",
            "シフト"});
            this.comboBox_modKeys.Location = new System.Drawing.Point(88, 5);
            this.comboBox_modKeys.Name = "comboBox_modKeys";
            this.comboBox_modKeys.Size = new System.Drawing.Size(114, 20);
            this.comboBox_modKeys.TabIndex = 0;
            this.toolTip1.SetToolTip(this.comboBox_modKeys, "表示または変更対象となる拡張修飾キーを選択します。\r\n\r\nキー名の末尾に (＊) が付いているものは、\r\n何らかの被修飾キーが定義されていることを\r\n示しています" +
        "。");
            this.comboBox_modKeys.SelectedIndexChanged += new System.EventHandler(this.comboBox_modKeys_SelectedIndexChanged);
            // 
            // dataGridView_extModifier
            // 
            this.dataGridView_extModifier.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView_extModifier.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_extModifier.Location = new System.Drawing.Point(13, 31);
            this.dataGridView_extModifier.Name = "dataGridView_extModifier";
            this.dataGridView_extModifier.RowTemplate.Height = 21;
            this.dataGridView_extModifier.Size = new System.Drawing.Size(600, 378);
            this.dataGridView_extModifier.TabIndex = 4;
            this.dataGridView_extModifier.TabStop = false;
            this.dataGridView_extModifier.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_extModifier_CellMouseClick);
            this.dataGridView_extModifier.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_extModifier_CellMouseDoubleClick);
            this.dataGridView_extModifier.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_extModifier_CellValueChanged);
            this.dataGridView_extModifier.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.dataGridView_extModifier_ColumnWidthChanged);
            this.dataGridView_extModifier.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridView_extModifier_KeyDown);
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
            this.comboBox_shiftPlaneOn.Location = new System.Drawing.Point(319, 5);
            this.comboBox_shiftPlaneOn.Name = "comboBox_shiftPlaneOn";
            this.comboBox_shiftPlaneOn.Size = new System.Drawing.Size(88, 20);
            this.comboBox_shiftPlaneOn.TabIndex = 1;
            this.toolTip1.SetToolTip(this.comboBox_shiftPlaneOn, "選択されている拡張修飾キーに対して、\r\n漢直ON時に割り当てるシフト面を選択します。");
            this.comboBox_shiftPlaneOn.SelectedIndexChanged += new System.EventHandler(this.comboBox_shiftPlaneOn_SelectedIndexChanged);
            // 
            // label_shiftPlaneOn
            // 
            this.label_shiftPlaneOn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label_shiftPlaneOn.AutoSize = true;
            this.label_shiftPlaneOn.Location = new System.Drawing.Point(222, 9);
            this.label_shiftPlaneOn.Name = "label_shiftPlaneOn";
            this.label_shiftPlaneOn.Size = new System.Drawing.Size(95, 12);
            this.label_shiftPlaneOn.TabIndex = 12;
            this.label_shiftPlaneOn.Text = "漢直ON時シフト面";
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
            this.comboBox_shiftPlaneOff.Location = new System.Drawing.Point(525, 5);
            this.comboBox_shiftPlaneOff.Name = "comboBox_shiftPlaneOff";
            this.comboBox_shiftPlaneOff.Size = new System.Drawing.Size(88, 20);
            this.comboBox_shiftPlaneOff.TabIndex = 2;
            this.toolTip1.SetToolTip(this.comboBox_shiftPlaneOff, "選択されている拡張修飾キーに対して、\r\n漢直OFF時に割り当てるシフト面を選択します。\r\n");
            this.comboBox_shiftPlaneOff.SelectedIndexChanged += new System.EventHandler(this.comboBox_shiftPlaneOff_SelectedIndexChanged);
            // 
            // label_shiftPlaneOff
            // 
            this.label_shiftPlaneOff.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label_shiftPlaneOff.AutoSize = true;
            this.label_shiftPlaneOff.Location = new System.Drawing.Point(422, 9);
            this.label_shiftPlaneOff.Name = "label_shiftPlaneOff";
            this.label_shiftPlaneOff.Size = new System.Drawing.Size(101, 12);
            this.label_shiftPlaneOff.TabIndex = 14;
            this.label_shiftPlaneOff.Text = "漢直OFF時シフト面";
            // 
            // radioButton_modKeys
            // 
            this.radioButton_modKeys.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_modKeys.AutoSize = true;
            this.radioButton_modKeys.Location = new System.Drawing.Point(111, 419);
            this.radioButton_modKeys.Name = "radioButton_modKeys";
            this.radioButton_modKeys.Size = new System.Drawing.Size(91, 16);
            this.radioButton_modKeys.TabIndex = 7;
            this.radioButton_modKeys.TabStop = true;
            this.radioButton_modKeys.Text = "修飾キー設定";
            this.toolTip1.SetToolTip(this.radioButton_modKeys, "拡張修飾キーによるキー設定画面に切り替えます。\r\n\r\n選択した拡張修飾キーごとに、被修飾キーに対して割り当てる\r\n特殊キーや機能を設定します。");
            this.radioButton_modKeys.UseVisualStyleBackColor = true;
            this.radioButton_modKeys.CheckedChanged += new System.EventHandler(this.radioButton_modKeys_CheckedChanged);
            // 
            // radioButton_singleHit
            // 
            this.radioButton_singleHit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_singleHit.AutoSize = true;
            this.radioButton_singleHit.Location = new System.Drawing.Point(216, 419);
            this.radioButton_singleHit.Name = "radioButton_singleHit";
            this.radioButton_singleHit.Size = new System.Drawing.Size(71, 16);
            this.radioButton_singleHit.TabIndex = 8;
            this.radioButton_singleHit.TabStop = true;
            this.radioButton_singleHit.Text = "単打設定";
            this.toolTip1.SetToolTip(this.radioButton_singleHit, "単打キー設定画面に切り替えます。\r\n\r\n拡張修飾キーや特殊キーの単打(押してすぐ離すこと)に対して\r\nキーや機能を設定することができます。");
            this.radioButton_singleHit.UseVisualStyleBackColor = true;
            this.radioButton_singleHit.CheckedChanged += new System.EventHandler(this.radioButton_singleHit_CheckedChanged);
            // 
            // dataGridView_singleHit
            // 
            this.dataGridView_singleHit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView_singleHit.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_singleHit.Location = new System.Drawing.Point(13, 31);
            this.dataGridView_singleHit.Name = "dataGridView_singleHit";
            this.dataGridView_singleHit.RowTemplate.Height = 21;
            this.dataGridView_singleHit.Size = new System.Drawing.Size(600, 378);
            this.dataGridView_singleHit.TabIndex = 3;
            this.dataGridView_singleHit.TabStop = false;
            this.dataGridView_singleHit.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_singleHit_CellMouseClick);
            this.dataGridView_singleHit.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_singleHit_CellMouseDoubleClick);
            this.dataGridView_singleHit.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_singleHit_CellValueChanged);
            this.dataGridView_singleHit.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.dataGridView_singleHit_ColumnWidthChanged);
            this.dataGridView_singleHit.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridView_singleHit_KeyDown);
            // 
            // dataGridView_shiftPlane
            // 
            this.dataGridView_shiftPlane.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView_shiftPlane.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_shiftPlane.Location = new System.Drawing.Point(13, 31);
            this.dataGridView_shiftPlane.Name = "dataGridView_shiftPlane";
            this.dataGridView_shiftPlane.RowTemplate.Height = 21;
            this.dataGridView_shiftPlane.Size = new System.Drawing.Size(600, 378);
            this.dataGridView_shiftPlane.TabIndex = 5;
            this.dataGridView_shiftPlane.TabStop = false;
            this.dataGridView_shiftPlane.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_shiftPlane_CellMouseClick);
            this.dataGridView_shiftPlane.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_shiftPlane_CellMouseDoubleClick);
            // 
            // radioButton_shiftPlane
            // 
            this.radioButton_shiftPlane.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_shiftPlane.AutoSize = true;
            this.radioButton_shiftPlane.Location = new System.Drawing.Point(15, 419);
            this.radioButton_shiftPlane.Name = "radioButton_shiftPlane";
            this.radioButton_shiftPlane.Size = new System.Drawing.Size(85, 16);
            this.radioButton_shiftPlane.TabIndex = 6;
            this.radioButton_shiftPlane.TabStop = true;
            this.radioButton_shiftPlane.Text = "シフト面設定";
            this.toolTip1.SetToolTip(this.radioButton_shiftPlane, "シフト面設定画面に切り替えます。\r\n\r\n選択した拡張修飾キーに対して、漢直ON時とOFF時の\r\nシフト面を設定できます。");
            this.radioButton_shiftPlane.UseVisualStyleBackColor = true;
            this.radioButton_shiftPlane.CheckedChanged += new System.EventHandler(this.radioButton_shiftPlane_CheckedChanged);
            // 
            // toolTip1
            // 
            this.toolTip1.AutoPopDelay = 32000;
            this.toolTip1.InitialDelay = 100;
            this.toolTip1.ReshowDelay = 100;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "  説明  ";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // groupBox_help
            // 
            this.groupBox_help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox_help.Controls.Add(this.label1);
            this.groupBox_help.Location = new System.Drawing.Point(322, 408);
            this.groupBox_help.Name = "groupBox_help";
            this.groupBox_help.Size = new System.Drawing.Size(52, 30);
            this.groupBox_help.TabIndex = 15;
            this.groupBox_help.TabStop = false;
            // 
            // panel_shiftPlaneHint
            // 
            this.panel_shiftPlaneHint.Controls.Add(this.label3);
            this.panel_shiftPlaneHint.Location = new System.Drawing.Point(40, 270);
            this.panel_shiftPlaneHint.Name = "panel_shiftPlaneHint";
            this.panel_shiftPlaneHint.Size = new System.Drawing.Size(222, 58);
            this.panel_shiftPlaneHint.TabIndex = 17;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(207, 36);
            this.label3.TabIndex = 1;
            this.label3.Text = "キー名の末尾に (＊) が付いているものは、\r\n何らかの被修飾キーが定義されていることを\r\n示しています。";
            // 
            // DlgModConversion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 441);
            this.Controls.Add(this.panel_shiftPlaneHint);
            this.Controls.Add(this.radioButton_shiftPlane);
            this.Controls.Add(this.dataGridView_singleHit);
            this.Controls.Add(this.radioButton_singleHit);
            this.Controls.Add(this.radioButton_modKeys);
            this.Controls.Add(this.dataGridView_extModifier);
            this.Controls.Add(this.comboBox_shiftPlaneOff);
            this.Controls.Add(this.label_shiftPlaneOff);
            this.Controls.Add(this.comboBox_shiftPlaneOn);
            this.Controls.Add(this.label_shiftPlaneOn);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.comboBox_modKeys);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.label_modKeys);
            this.Controls.Add(this.dataGridView_shiftPlane);
            this.Controls.Add(this.groupBox_help);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(640, 400);
            this.Name = "DlgModConversion";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "拡張修飾キー設定";
            this.Load += new System.EventHandler(this.DlgModConversion_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_extModifier)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_singleHit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_shiftPlane)).EndInit();
            this.groupBox_help.ResumeLayout(false);
            this.groupBox_help.PerformLayout();
            this.panel_shiftPlaneHint.ResumeLayout(false);
            this.panel_shiftPlaneHint.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label label_modKeys;
        private System.Windows.Forms.ComboBox comboBox_modKeys;
        private System.Windows.Forms.DataGridView dataGridView_extModifier;
        private System.Windows.Forms.ComboBox comboBox_shiftPlaneOn;
        private System.Windows.Forms.Label label_shiftPlaneOn;
        private System.Windows.Forms.ComboBox comboBox_shiftPlaneOff;
        private System.Windows.Forms.Label label_shiftPlaneOff;
        private System.Windows.Forms.RadioButton radioButton_modKeys;
        private System.Windows.Forms.RadioButton radioButton_singleHit;
        private System.Windows.Forms.DataGridView dataGridView_singleHit;
        private System.Windows.Forms.DataGridView dataGridView_shiftPlane;
        private System.Windows.Forms.RadioButton radioButton_shiftPlane;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.GroupBox groupBox_help;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel_shiftPlaneHint;
        private System.Windows.Forms.Label label3;
    }
}