using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KanchokuWS
{
    public partial class ColorTextBox : TextBox
    {
        public bool ForBackColor { get; set; } = true;

        public ColorTextBox()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        private void ColorTextBox_TextChanged(object sender, EventArgs e)
        {
            if (ForBackColor)
                BackColor = Color.FromName(Text);
            else
                ForeColor = Color.FromName(Text);
        }

        private void ColorTextBox_Click(object sender, EventArgs e)
        {
            invokeColorSelector();
        }

        private void ColorTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ') invokeColorSelector();
            e.Handled = true;
        }

        private void invokeColorSelector()
        {
            var dlg = new DlgColorSelector(Text);
            if (dlg.ShowDialog() == DialogResult.OK) {
                Text = dlg.ColorName;
            }
            dlg.Dispose();
            //Parent?.Focus();
        }
    }
}
