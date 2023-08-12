
namespace KanchokuWS
{
    partial class FrmSplash
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
            this.buttonOK = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label_version = new System.Windows.Forms.Label();
            this.label_initializing = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.buttonSettings = new System.Windows.Forms.Button();
            this.label_version2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Font = new System.Drawing.Font("BIZ UDゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonOK.Location = new System.Drawing.Point(41, 110);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(50, 21);
            this.buttonOK.TabIndex = 10;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Visible = false;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("BIZ UDゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label4.Location = new System.Drawing.Point(59, 87);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 12);
            this.label4.TabIndex = 9;
            this.label4.Text = "or Spoiler";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("BIZ UDゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label3.Location = new System.Drawing.Point(39, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(143, 12);
            this.label3.TabIndex = 8;
            this.label3.Text = "Shin, Second, Sharp, ..";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("BIZ UDゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label2.Location = new System.Drawing.Point(77, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 12);
            this.label2.TabIndex = 7;
            this.label2.Text = "漢直Win";
            // 
            // label_version
            // 
            this.label_version.AutoSize = true;
            this.label_version.Font = new System.Drawing.Font("BIZ UDゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_version.Location = new System.Drawing.Point(48, 18);
            this.label_version.Name = "label_version";
            this.label_version.Size = new System.Drawing.Size(71, 12);
            this.label_version.TabIndex = 6;
            this.label_version.Text = "漢直WS Ver.";
            // 
            // label_initializing
            // 
            this.label_initializing.AutoSize = true;
            this.label_initializing.Font = new System.Drawing.Font("BIZ UDゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_initializing.Location = new System.Drawing.Point(22, 114);
            this.label_initializing.Name = "label_initializing";
            this.label_initializing.Size = new System.Drawing.Size(167, 12);
            this.label_initializing.TabIndex = 11;
            this.label_initializing.Text = "辞書ファイルを読み込み中...";
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // buttonSettings
            // 
            this.buttonSettings.Font = new System.Drawing.Font("BIZ UDゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.buttonSettings.Location = new System.Drawing.Point(108, 110);
            this.buttonSettings.Name = "buttonSettings";
            this.buttonSettings.Size = new System.Drawing.Size(50, 21);
            this.buttonSettings.TabIndex = 12;
            this.buttonSettings.Text = "設定";
            this.buttonSettings.UseVisualStyleBackColor = true;
            this.buttonSettings.Visible = false;
            this.buttonSettings.Click += new System.EventHandler(this.buttonSettings_Click);
            // 
            // label_version2
            // 
            this.label_version2.AutoSize = true;
            this.label_version2.Font = new System.Drawing.Font("BIZ UDゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_version2.Location = new System.Drawing.Point(75, 35);
            this.label_version2.Name = "label_version2";
            this.label_version2.Size = new System.Drawing.Size(0, 12);
            this.label_version2.TabIndex = 13;
            // 
            // FrmSplash
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(205, 139);
            this.Controls.Add(this.label_version2);
            this.Controls.Add(this.buttonSettings);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label_version);
            this.Controls.Add(this.label_initializing);
            this.Name = "FrmSplash";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FormSplash";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormSplash_FormClosing);
            this.Load += new System.EventHandler(this.FormSplash_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label_version;
        private System.Windows.Forms.Label label_initializing;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button buttonSettings;
        private System.Windows.Forms.Label label_version2;
    }
}