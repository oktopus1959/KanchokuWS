
namespace KanchokuWS
{
    partial class FrmModeMarker
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
            this.label1 = new System.Windows.Forms.Label();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.Settings_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Read_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReadBushuDic_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReadMazeWikipediaDic_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.Restart_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RestartWithSave_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RestartWithDiscard_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.Exit_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.ReloadSettings_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ContextMenuStrip = this.contextMenuStrip1;
            this.label1.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(0, 1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(25, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "漢";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Settings_ToolStripMenuItem,
            this.Read_ToolStripMenuItem,
            this.toolStripMenuItem1,
            this.Restart_ToolStripMenuItem,
            this.toolStripMenuItem2,
            this.Exit_ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(181, 126);
            // 
            // Settings_ToolStripMenuItem
            // 
            this.Settings_ToolStripMenuItem.Name = "Settings_ToolStripMenuItem";
            this.Settings_ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.Settings_ToolStripMenuItem.Text = "設定";
            this.Settings_ToolStripMenuItem.Click += new System.EventHandler(this.Settings_ToolStripMenuItem_Click);
            // 
            // Read_ToolStripMenuItem
            // 
            this.Read_ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ReloadSettings_ToolStripMenuItem,
            this.ReadBushuDic_ToolStripMenuItem,
            this.ReadMazeWikipediaDic_ToolStripMenuItem});
            this.Read_ToolStripMenuItem.Name = "Read_ToolStripMenuItem";
            this.Read_ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.Read_ToolStripMenuItem.Text = "再読込";
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
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(177, 6);
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
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(177, 6);
            // 
            // Exit_ToolStripMenuItem
            // 
            this.Exit_ToolStripMenuItem.Name = "Exit_ToolStripMenuItem";
            this.Exit_ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.Exit_ToolStripMenuItem.Text = "終了";
            this.Exit_ToolStripMenuItem.Click += new System.EventHandler(this.Exit_ToolStripMenuItem_Click);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // ReloadSettings_ToolStripMenuItem
            // 
            this.ReloadSettings_ToolStripMenuItem.Name = "ReloadSettings_ToolStripMenuItem";
            this.ReloadSettings_ToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.ReloadSettings_ToolStripMenuItem.Text = "設定の再読込";
            this.ReloadSettings_ToolStripMenuItem.Click += new System.EventHandler(this.ReloadSettings_ToolStripMenuItem_Click);
            // 
            // FrmModeMarker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(172, 69);
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmModeMarker";
            this.Text = "FrmModeMarker";
            this.Load += new System.EventHandler(this.FrmModeMarker_Load);
            this.VisibleChanged += new System.EventHandler(this.FrmModeMarker_VisibleChanged);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem Settings_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem Exit_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem Restart_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripMenuItem RestartWithSave_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RestartWithDiscard_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem Read_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReadBushuDic_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReadMazeWikipediaDic_ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReloadSettings_ToolStripMenuItem;
    }
}