namespace KanchokuWS
{
    partial class FrmKanchoku
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmKanchoku));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.設定画面ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReadDic_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReadBushuDic_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReadMazeWikipediaDic_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.Restart_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RestartWithSave_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RestartWithDiscard_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.終了ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.ReloadSettings_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "漢直窓WS";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.Click += new System.EventHandler(this.notifyIcon1_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.設定画面ToolStripMenuItem,
            this.ReadDic_ToolStripMenuItem,
            this.toolStripSeparator1,
            this.Restart_ToolStripMenuItem,
            this.toolStripMenuItem1,
            this.終了ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(181, 126);
            // 
            // 設定画面ToolStripMenuItem
            // 
            this.設定画面ToolStripMenuItem.Name = "設定画面ToolStripMenuItem";
            this.設定画面ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.設定画面ToolStripMenuItem.Text = "設定画面";
            this.設定画面ToolStripMenuItem.Click += new System.EventHandler(this.Settings_ToolStripMenuItem_Click);
            // 
            // ReadDic_ToolStripMenuItem
            // 
            this.ReadDic_ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ReloadSettings_ToolStripMenuItem,
            this.ReadBushuDic_ToolStripMenuItem,
            this.ReadMazeWikipediaDic_ToolStripMenuItem});
            this.ReadDic_ToolStripMenuItem.Name = "ReadDic_ToolStripMenuItem";
            this.ReadDic_ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.ReadDic_ToolStripMenuItem.Text = "再読込";
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
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
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
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(177, 6);
            // 
            // 終了ToolStripMenuItem
            // 
            this.終了ToolStripMenuItem.Name = "終了ToolStripMenuItem";
            this.終了ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.終了ToolStripMenuItem.Text = "終了";
            this.終了ToolStripMenuItem.Click += new System.EventHandler(this.Exit_ToolStripMenuItem_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(265, 108);
            this.label1.TabIndex = 1;
            this.label1.Text = "このウィンドウは表示しない。以下のように設定してある。\r\n\r\n   FormBorderStyle = FormBorderStyle.None;\r\n   Wid" +
    "th = 0;\r\n   Height = 0;\r\n   WindowState = FormWindowState.Minimized;\r\n   Opacity" +
    " = 0;\r\n\r\nかわりに FormSplash を表示する。";
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // ReloadSettings_ToolStripMenuItem
            // 
            this.ReloadSettings_ToolStripMenuItem.Name = "ReloadSettings_ToolStripMenuItem";
            this.ReloadSettings_ToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.ReloadSettings_ToolStripMenuItem.Text = "設定と定義の再読込";
            this.ReloadSettings_ToolStripMenuItem.Click += new System.EventHandler(this.ReloadSettings_ToolStripMenuItem_Click);
            // 
            // FrmKanchoku
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(285, 136);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmKanchoku";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "漢直窓 WS";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmKanchoku_FormClosing);
            this.Load += new System.EventHandler(this.FrmKanchoku_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 終了ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 設定画面ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripMenuItem Restart_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripMenuItem RestartWithSave_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RestartWithDiscard_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReadDic_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReadBushuDic_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReadMazeWikipediaDic_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReloadSettings_ToolStripMenuItem;
    }
}

