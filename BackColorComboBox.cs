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
    public partial class BackColorComboBox : ColorComboBox
    {
        public BackColorComboBox()
        {
            InitializeComponent();
        }

        private Color getColor(string name)
        {
            var color = Color.FromName(name ?? "");
            return color.IsEmpty ? SystemColors.Window : color;
        }

        protected override Color GetForeColor()
        {
            return Color.Black;
        }

        protected override Color GetBackColor()
        {
            return getColor(SelectedIndex >= 0 ? Items[SelectedIndex].ToString() : EditBoxColorName);
        }

        public override void SetText(string name)
        {
            base.SetText(name);
            BackColor = getColor(name);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        private void BackColorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            BackColor = GetBackColor();
        }

    }
}
