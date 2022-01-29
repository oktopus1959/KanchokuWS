
namespace KanchokuWS
{
    partial class ColorTextBox
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ColorTextBox
            // 
            this.BackColor = System.Drawing.Color.White;
            this.ReadOnly = true;
            this.Click += new System.EventHandler(this.ColorTextBox_Click);
            this.TextChanged += new System.EventHandler(this.ColorTextBox_TextChanged);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ColorTextBox_KeyPress);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
