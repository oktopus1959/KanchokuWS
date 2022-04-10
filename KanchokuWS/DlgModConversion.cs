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
    public partial class DlgModConversion : Form
    {
        private static string[] modifierKeys = new string[] {
            "space",
            "caps",
            "alnum",
            "nfer",
            "xfer",
            "rshift",
            "lctrl",
            "rctrl",
            "shift",
        };

        const int PLANE_ASIGNABLE_MOD_KEYS_NUM = 6;

        private static string[] normalKeyNames = new string[] {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
            "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
            "A", "S", "D", "F", "G", "H", "J", "K", "L",  ";",
            "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/",
            "Sp", "-", "^", "￥", "@", "[", ":", "]", "\\", ""
        };

        private static int[] singleHitModifiers = new int[] {
            DecoderKeys.STROKE_SPACE_DECKEY,
            DecoderKeys.ESC_DECKEY,
            DecoderKeys.HANZEN_DECKEY,
            DecoderKeys.TAB_DECKEY,
            DecoderKeys.CAPS_DECKEY,
            DecoderKeys.ALNUM_DECKEY,
            DecoderKeys.NFER_DECKEY,
            DecoderKeys.XFER_DECKEY,
            DecoderKeys.KANA_DECKEY,
            DecoderKeys.BS_DECKEY,
            DecoderKeys.ENTER_DECKEY,
            DecoderKeys.INS_DECKEY,
            DecoderKeys.DEL_DECKEY,
            DecoderKeys.HOME_DECKEY,
            DecoderKeys.END_DECKEY,
            DecoderKeys.PAGE_UP_DECKEY,
            DecoderKeys.PAGE_DOWN_DECKEY,
            DecoderKeys.LEFT_ARROW_DECKEY,
            DecoderKeys.RIGHT_ARROW_DECKEY,
            DecoderKeys.UP_ARROW_DECKEY,
            DecoderKeys.DOWN_ARROW_DECKEY,
            DecoderKeys.RIGHT_SHIFT_DECKEY,
        };

        private static int[] extModifiees = new int[] {
            DecoderKeys.ESC_DECKEY,
            DecoderKeys.TAB_DECKEY,
            DecoderKeys.BS_DECKEY,
            DecoderKeys.ENTER_DECKEY,
            DecoderKeys.INS_DECKEY,
            DecoderKeys.DEL_DECKEY,
            DecoderKeys.HOME_DECKEY,
            DecoderKeys.END_DECKEY,
            DecoderKeys.PAGE_UP_DECKEY,
            DecoderKeys.PAGE_DOWN_DECKEY,
            DecoderKeys.LEFT_ARROW_DECKEY,
            DecoderKeys.RIGHT_ARROW_DECKEY,
            DecoderKeys.UP_ARROW_DECKEY,
            DecoderKeys.DOWN_ARROW_DECKEY,
        };

        public DlgModConversion()
        {
            InitializeComponent();
        }

        private void DlgModConversion_Load(object sender, EventArgs e)
        {
            radioButton_modKeys.Checked = true;
            dataGridView1.Visible = false;
            setDataGridViewForSingleHit();
            setDataGridViewForExtModifier();
            int modIdx = modifierKeys._findIndex((x) => x._equalsTo(VirtualKeys.DefaultExtModifierName));
            if (modIdx >= 0) comboBox_modKeys.SelectedIndex = modIdx;
        }

        private void setDataGridViewForSingleHit()
        {
            double dpiRate = ScreenInfo.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView1;
            dgv._defaultSetup(rowHeight, rowHeight);
            dgv._setSelectionColorLemon();                 // 選択時の色をレモン色にする
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            int keyCodeWidth = (int)(30 * dpiRate);
            int keyNameWidth = (int)(80 * dpiRate);
            int funcNameWidth = (int)(180 * dpiRate);
            int funcDescWidth = (int)(dgv.Width - 20 * dpiRate - keyCodeWidth - keyNameWidth - funcNameWidth);
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyCode", "No", keyCodeWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyName", "キー", keyNameWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcName", "キー/機能名", funcNameWidth, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcDesc", "機能説明", funcDescWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));

            int num = singleHitModifiers.Length;
            dgv.Rows.Add(num);

            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i;
                int deckey = singleHitModifiers[i];
                dgv.Rows[i].Cells[1].Value = SpecialKeysAndFunctions.GetKeyOrFuncByDeckey(deckey)?.Name ?? "";
                var target = VirtualKeys.SingleHitDefs._safeGet(deckey);
                if (target._notEmpty()) {
                    var kof = SpecialKeysAndFunctions.GetKeyOrFuncByName(target);
                    dgv.Rows[i].Cells[2].Value = kof != null ? kof.Name : target;
                    dgv.Rows[i].Cells[3].Value = kof != null ? kof.Description : "";
                }
            }
        }

        private void setDataGridViewForExtModifier()
        {
            double dpiRate = ScreenInfo.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView2;
            dgv._defaultSetup(rowHeight, rowHeight);
            dgv._setSelectionColorLemon();                 // 選択時の色をレモン色にする
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            int keyCodeWidth = (int)(30 * dpiRate);
            int keyNameWidth = (int)(80 * dpiRate);
            int funcNameWidth = (int)(180 * dpiRate);
            int funcDescWidth = (int)(dgv.Width - 20 * dpiRate - keyCodeWidth - keyNameWidth - funcNameWidth);
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyCode", "No", keyCodeWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyName", "キー", keyNameWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcName", "キー/機能名", funcNameWidth, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcDesc", "機能説明", funcDescWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));

            int num = normalKeyNames.Length + extModifiees.Length;
            dgv.Rows.Add(num);

            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i;
                if (i < normalKeyNames.Length) {
                    dgv.Rows[i].Cells[1].Value = normalKeyNames[i];
                } else {
                    dgv.Rows[i].Cells[1].Value = SpecialKeysAndFunctions.GetKeyOrFuncByDeckey(extModifiees[i - normalKeyNames.Length])?.Name ?? "";
                }
            }
        }

        private void setExtModifierDgv(int modDeckey)
        {
            var dgv = dataGridView2;
            int num = dgv.Rows.Count;
            var dict = VirtualKeys.ExtModifierKeyDefs._safeGet(modDeckey);
            for (int i = 0; i < num; ++i) {
                string defStr = "";
                string desc = "";
                KeyOrFunction kof = null;
                if (dict != null) {
                    int deckey = i < normalKeyNames.Length ? i : extModifiees[i - normalKeyNames.Length];
                    var target = dict._safeGet(deckey);
                    if (target._notEmpty()) {
                        kof = SpecialKeysAndFunctions.GetKeyOrFuncByName(target);
                        if (kof != null) {
                            defStr = kof.Name;
                            desc = kof.Description;
                        } else {
                            defStr = target;
                        }
                    }
                }
                dgv.Rows[i].Cells[2].Value = defStr;
                dgv.Rows[i].Cells[3].Value = desc;
            }
        }

        private void selectModKey(int idx)
        {
            string modKeyName = modifierKeys._getNth(idx);
            uint modVKey = VirtualKeys.GetModifierKeyByName(modKeyName);
            bool bAssignable = idx >= 0 && idx < PLANE_ASIGNABLE_MOD_KEYS_NUM;
            if (bAssignable) {
                comboBox_shiftPlaneOn.SelectedIndex = (int)VirtualKeys.ShiftPlaneForShiftModFlag._safeGet(modVKey);
                comboBox_shiftPlaneOff.SelectedIndex = (int)VirtualKeys.ShiftPlaneForShiftModFlagWhenDecoderOff._safeGet(modVKey);
            } 
            comboBox_shiftPlaneOn.Enabled = bAssignable;
            comboBox_shiftPlaneOff.Enabled = bAssignable;

            setExtModifierDgv(SpecialKeysAndFunctions.GetDeckeyByName(modKeyName));
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

        private void radioButton_modKeys_CheckedChanged(object sender, EventArgs e)
        {
            dataGridView1.Visible = !radioButton_modKeys.Checked;
            dataGridView2.Visible = radioButton_modKeys.Checked;
            comboBox_modKeys.Enabled = radioButton_modKeys.Checked;
            comboBox_shiftPlaneOn.Enabled = radioButton_modKeys.Checked;
            comboBox_shiftPlaneOff.Enabled = radioButton_modKeys.Checked;
        }

        private void comboBox_modKeys_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectModKey(comboBox_modKeys.SelectedIndex);
        }
    }
}
