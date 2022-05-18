using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using Utils;

namespace KanchokuWS.Gui
{
    public partial class ColorComboBox : ComboBox
    {
        private static string[] allColorNames = null;

        protected static string[] getAllColorNames()
        {
            if (allColorNames == null) {
                allColorNames = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name).Skip(1).ToArray();   // Skip(1)で先頭の Transparent をスキップ
            }
            return allColorNames;
        }

        protected virtual Color GetForeColor() { return Color.Black; }
        protected virtual Color GetBackColor() { return SystemColors.Window; }

        protected virtual Color GetItemForeColor(string colorName) { return Color.Black; }
        protected virtual Color GetItemBackColor(string colorName) { return Color.FromName(colorName); }

        protected string EditBoxColorName = "";

        public ColorComboBox()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        public virtual void SetText(string name)
        {
            if (Items.Count < 2) {
                Items.Clear();
                Items.Add(name);
                SelectedIndex = 0;
            }
            EditBoxColorName = name;
        }

        private void ColorComboBox_DropDown(object sender, EventArgs e)
        {
            if (Items.Count < 2) {
                Items.Clear();
                Items.AddRange(getAllColorNames());
                SelectedIndex = getAllColorNames()._findIndex(name => name._equalsTo(EditBoxColorName));
            }
            ForeColor = Color.Black;
            BackColor = SystemColors.Window;
        }

        private void ColorComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            string colorName = e.Index > -1 ? cmb.Items[e.Index].ToString() : EditBoxColorName;

            bool isEditBox = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
            Color fgColor = isEditBox ? GetForeColor() : e.ForeColor;
            Color bgColor = isEditBox ? GetBackColor() : e.BackColor;
            //Color fgColor = isEditBox ? GetForeColor() : GetItemForeColor(colorName);
            //Color bgColor = isEditBox ? GetBackColor() : GetItemBackColor(colorName);

            //背景を描画する
            //項目が選択されている時は強調表示される
            using (Brush bgBrush = new SolidBrush(bgColor)) {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            //文字列を描画する
            using (Brush fgBrush = new SolidBrush(fgColor)) {
                e.Graphics.DrawString(colorName, this.Font, fgBrush, e.Bounds.X, e.Bounds.Y);
            }

            //フォーカスを示す四角形を描画
            if ((e.State & DrawItemState.Focus) != 0) ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds);
        }
    }
}
