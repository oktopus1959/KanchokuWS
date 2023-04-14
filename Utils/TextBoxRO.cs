using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Utils
{
    public partial class TextBoxRO : TextBox
    {
        public Action<string> actionOnPaste { get; set; }

        public TextBoxRO()
        {
            ContextMenuStrip = new ContextMenuStrip();
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        private void TextBoxRO_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar) {
                case '\u0003':
                    //Ctrl-C
                    if (Text._notEmpty() && SelectionLength <= 0) {
                        Clipboard.SetText(Text);
                        e.Handled = true;
                    }
                    return;
                case '\u0016':
                    //Ctrl-V
                    Text = Clipboard.GetText()._safeSubstring(0, 100);
                    //SelectAll();
                    actionOnPaste?.Invoke(Text);
                    e.Handled = true;
                    return;
                default:
                    e.Handled = true;
                    break;
            }
        }

        private void TextBoxRO_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Left && e.KeyCode != Keys.Right && e.KeyCode != Keys.Home && e.KeyCode != Keys.End) {
                // HOME,END,左右矢印キー以外は無視
                e.Handled = true;
            }
        }
    }
}
