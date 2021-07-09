using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utils;

namespace KanchokuWS
{
    public partial class ForeColorComboBox : ColorComboBox
    {
        public ForeColorComboBox()
        {
            InitializeComponent();
            EditBoxColorName = "Black";
            BackColor = SystemColors.Control;
        }

        private Color getColor(string name)
        {
            var color = Color.FromName(name ?? "");
            return color.IsEmpty ? Color.Black : color;
        }

        protected override Color GetForeColor()
        {
            return getColor(SelectedIndex >= 0 ? Items[SelectedIndex].ToString() : EditBoxColorName);
        }

        protected override Color GetBackColor()
        {
            return BackColor;
        }

        public override void SetText(string name)
        {
            base.SetText(name);
            ForeColor = getColor(name);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        private void ForeColorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ForeColor = GetForeColor();
        }

    }
}
