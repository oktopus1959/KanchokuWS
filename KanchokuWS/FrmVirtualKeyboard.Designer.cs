
namespace KanchokuWS
{
    partial class FrmVirtualKeyboard
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
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.設定ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReadDic_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReloadSettings_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReadBushuDic_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReadMazeWikipediaDic_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FollowCaret_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.Restart_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RestartWithSave_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RestartWithDiscard_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.終了ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.pictureBox_Main = new System.Windows.Forms.PictureBox();
            this.topTextBox = new Utils.TextBoxRO();
            this.dgvHorizontal = new System.Windows.Forms.DataGridView();
            this.pictureBox_measureFontSize = new System.Windows.Forms.PictureBox();
            this.ExcangeTable_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Main)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHorizontal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_measureFontSize)).BeginInit();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.設定ToolStripMenuItem,
            this.ReadDic_ToolStripMenuItem,
            this.FollowCaret_ToolStripMenuItem,
            this.toolStripSeparator2,
            this.Restart_ToolStripMenuItem,
            this.toolStripSeparator1,
            this.終了ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(181, 148);
            // 
            // 設定ToolStripMenuItem
            // 
            this.設定ToolStripMenuItem.Name = "設定ToolStripMenuItem";
            this.設定ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.設定ToolStripMenuItem.Text = "設定";
            this.設定ToolStripMenuItem.Click += new System.EventHandler(this.Settings_ToolStripMenuItem_Click);
            // 
            // ReadDic_ToolStripMenuItem
            // 
            this.ReadDic_ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ReloadSettings_ToolStripMenuItem,
            this.ReadBushuDic_ToolStripMenuItem,
            this.ExcangeTable_ToolStripMenuItem,
            this.ReadMazeWikipediaDic_ToolStripMenuItem});
            this.ReadDic_ToolStripMenuItem.Name = "ReadDic_ToolStripMenuItem";
            this.ReadDic_ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.ReadDic_ToolStripMenuItem.Text = "再読込";
            // 
            // ReloadSettings_ToolStripMenuItem
            // 
            this.ReloadSettings_ToolStripMenuItem.Name = "ReloadSettings_ToolStripMenuItem";
            this.ReloadSettings_ToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.ReloadSettings_ToolStripMenuItem.Text = "設定と定義の再読込";
            this.ReloadSettings_ToolStripMenuItem.Click += new System.EventHandler(this.ReloadSettings_ToolStripMenuItem_Click);
            // 
            // ReadBushuDic_ToolStripMenuItem
            // 
            this.ReadBushuDic_ToolStripMenuItem.Name = "ReadBushuDic_ToolStripMenuItem";
            this.ReadBushuDic_ToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.ReadBushuDic_ToolStripMenuItem.Text = "部首合成辞書再読込";
            this.ReadBushuDic_ToolStripMenuItem.Click += new System.EventHandler(this.ReadBushuDic_ToolStripMenuItem_Click);
            // 
            // ReadMazeWikipediaDic_ToolStripMenuItem
            // 
            this.ReadMazeWikipediaDic_ToolStripMenuItem.Name = "ReadMazeWikipediaDic_ToolStripMenuItem";
            this.ReadMazeWikipediaDic_ToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.ReadMazeWikipediaDic_ToolStripMenuItem.Text = "Wikipedia交ぜ書き辞書読込";
            this.ReadMazeWikipediaDic_ToolStripMenuItem.Click += new System.EventHandler(this.ReadMazeWikipediaDic_ToolStripMenuItem_Click);
            // 
            // FollowCaret_ToolStripMenuItem
            // 
            this.FollowCaret_ToolStripMenuItem.Name = "FollowCaret_ToolStripMenuItem";
            this.FollowCaret_ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.FollowCaret_ToolStripMenuItem.Text = "再追従";
            this.FollowCaret_ToolStripMenuItem.Click += new System.EventHandler(this.FollowCaret_ToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(177, 6);
            // 
            // Restart_ToolStripMenuItem
            // 
            this.Restart_ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RestartWithSave_ToolStripMenuItem,
            this.RestartWithDiscard_ToolStripMenuItem});
            this.Restart_ToolStripMenuItem.Name = "Restart_ToolStripMenuItem";
            this.Restart_ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.Restart_ToolStripMenuItem.Text = "再起動";
            // 
            // RestartWithSave_ToolStripMenuItem
            // 
            this.RestartWithSave_ToolStripMenuItem.Name = "RestartWithSave_ToolStripMenuItem";
            this.RestartWithSave_ToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
            this.RestartWithSave_ToolStripMenuItem.Text = "辞書内容を保存して再起動";
            this.RestartWithSave_ToolStripMenuItem.Click += new System.EventHandler(this.RestartWithSave_ToolStripMenuItem_Click);
            // 
            // RestartWithDiscard_ToolStripMenuItem
            // 
            this.RestartWithDiscard_ToolStripMenuItem.Name = "RestartWithDiscard_ToolStripMenuItem";
            this.RestartWithDiscard_ToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
            this.RestartWithDiscard_ToolStripMenuItem.Text = "辞書内容を破棄して再起動";
            this.RestartWithDiscard_ToolStripMenuItem.Click += new System.EventHandler(this.RestartWithDiscard_ToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // 終了ToolStripMenuItem
            // 
            this.終了ToolStripMenuItem.Name = "終了ToolStripMenuItem";
            this.終了ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.終了ToolStripMenuItem.Text = "終了";
            this.終了ToolStripMenuItem.Click += new System.EventHandler(this.Exit_ToolStripMenuItem_Click);
            // 
            // toolTip1
            // 
            this.toolTip1.AutoPopDelay = 32000;
            this.toolTip1.InitialDelay = 100;
            this.toolTip1.ReshowDelay = 100;
            // 
            // pictureBox_Main
            // 
            this.pictureBox_Main.ContextMenuStrip = this.contextMenuStrip1;
            this.pictureBox_Main.Location = new System.Drawing.Point(1, 19);
            this.pictureBox_Main.Name = "pictureBox_Main";
            this.pictureBox_Main.Size = new System.Drawing.Size(201, 111);
            this.pictureBox_Main.TabIndex = 31;
            this.pictureBox_Main.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox_Main, "左クリックするとデコーダをOFFにします。\r\n右クリックでコンテキストメニューを表示します。\r\nまた、Ctrl-T/Ctrl-Shift-Tによりストロークテーブ" +
        "ルが切り替わります。");
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
            this.toolTip1.SetToolTip(this.topTextBox, "ここに文字列をペーストすると以下のアクションを実行します。\r\n　1文字：ストロークヘルプの表示\r\n　■=□□…：■に対して□□…部首連想(エイリアス)を定義\r\n　" +
        "上記以外の文字列：入力履歴への強制登録(削除マークを無視)");
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
            // ExcangeTable_ToolStripMenuItem
            // 
            this.ExcangeTable_ToolStripMenuItem.Name = "ExcangeTable_ToolStripMenuItem";
            this.ExcangeTable_ToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.ExcangeTable_ToolStripMenuItem.Text = "主・副テーブル切り替え";
            this.ExcangeTable_ToolStripMenuItem.Click += new System.EventHandler(this.ExcangeTable_ToolStripMenuItem_Click);
            // 
            // FrmVirtualKeyboard
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
            this.Name = "FrmVirtualKeyboard";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "漢直窓 WS";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmVirtualKeyboard_FormClosing);
            this.Load += new System.EventHandler(this.DlgVirtualKeyboard_Load);
            this.VisibleChanged += new System.EventHandler(this.DlgVirtualKeyboard_VisibleChanged);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Main)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHorizontal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_measureFontSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Utils.TextBoxRO topTextBox;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 終了ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem Restart_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.DataGridView dgvHorizontal;
        private System.Windows.Forms.ToolStripMenuItem 設定ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.PictureBox pictureBox_Main;
        private System.Windows.Forms.PictureBox pictureBox_measureFontSize;
        private System.Windows.Forms.ToolStripMenuItem RestartWithSave_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RestartWithDiscard_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReadDic_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReadBushuDic_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReadMazeWikipediaDic_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReloadSettings_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FollowCaret_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExcangeTable_ToolStripMenuItem;
    }
}
