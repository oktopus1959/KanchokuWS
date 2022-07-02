using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using KanchokuWS.Handler;
using Utils;

namespace KanchokuWS.Gui
{
    public partial class DlgKeywordSelector : Form
    {
        public string SelectedWord { get; private set; }

        public DlgKeywordSelector()
        {
            InitializeComponent();
            if (Settings.DlgKeywordSelectorWidth > 0) Width = Settings.DlgKeywordSelectorWidth;
            if (Settings.DlgKeywordSelectorHeight > 0) Height = Settings.DlgKeywordSelectorHeight;
            buttonOK.Enabled = false;
            CancelButton = buttonCancel;
        }

        private void DlgKeywordSelector_Load(object sender, EventArgs e)
        {

            setDataGridViewForExtModifier();
        }

        private void setDataGridViewForExtModifier()
        {
            double dpiRate = ScreenInfo.Singleton.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView1;
            dgv._defaultSetup(rowHeight, rowHeight, true);      // ヘッダーあり、行全体の選択
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            int keyCodeWidth = (int)(30 * dpiRate);
            int funcNameWidth = (int)(180 * dpiRate);
            //int funcDescWidth = (int)(dgv.Width - 20 * dpiRate - funcNameWidth);
            int funcDescWidth = (int)(1020 * dpiRate);
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Sortable_Centered("keyCode", "No", keyCodeWidth));
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Sortable("funcName", "キー/機能名", funcNameWidth));
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Sortable("funcDesc", "機能説明", funcDescWidth));

            var list = SpecialKeysAndFunctions.GetAssignableKeyOrFunctions();
            dgv.Rows.Add(list.Length);

            int ridx = 0;
            foreach (var kof in list) {
                if (kof.IsFunction) {
                    dgv.Rows[ridx].Cells[0].Value = ridx;
                    dgv.Rows[ridx].Cells[1].Value = kof.Name;
                    dgv.Rows[ridx].Cells[2].Value = kof.DetailedDesc ?? kof.Description ?? "";
                    ++ridx;
                }
            }
            foreach (var kof in list) {
                if (!kof.IsFunction) {
                    dgv.Rows[ridx].Cells[0].Value = ridx;
                    dgv.Rows[ridx].Cells[1].Value = kof.Name;
                    dgv.Rows[ridx].Cells[2].Value = kof.DetailedDesc ?? kof.Description ?? "";
                    ++ridx;
                }
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool setSelectedWord(int ridx)
        {
            if (ridx >= 0 && ridx < dataGridView1.Rows.Count) {
                SelectedWord = dataGridView1.Rows[ridx].Cells[1].Value?.ToString();
                return true;
            }
            return false;
        }

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            buttonOK.Enabled = true;
            setSelectedWord(e.RowIndex);
        }

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (setSelectedWord(e.RowIndex)) {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void DlgKeywordSelector_Shown(object sender, EventArgs e)
        {
            dataGridView1.CurrentCell = null;
        }
    }
}
