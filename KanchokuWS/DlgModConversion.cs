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
        private static Logger logger = Logger.GetLogger();

        public class ModifierDef
        {
            public string Name { get; private set; }
            public uint ModKey { get; private set; }
            public string Description { get; private set; }

            public ModifierDef(string name, uint modkey, string desc)
            {
                Name = name;
                ModKey = modkey;
                Description = desc;
            }
        };

        private static ModifierDef[] modifierKeys = new ModifierDef[] {
            new ModifierDef("space", KeyModifiers.MOD_SPACE, "SandS"),
            new ModifierDef("caps", KeyModifiers.MOD_CAPS, "CapsLock"),
            new ModifierDef("alnum", KeyModifiers.MOD_ALNUM, "英数"),
            new ModifierDef("nfer", KeyModifiers.MOD_NFER, "無変換"),
            new ModifierDef("xfer", KeyModifiers.MOD_XFER, "変換"),
            new ModifierDef("rshift", KeyModifiers.MOD_RSHIFT, "右シフト"),
            new ModifierDef("lctrl", KeyModifiers.MOD_LCTRL, "左コントロール"),
            new ModifierDef("rctrl", KeyModifiers.MOD_RCTRL, "右コントロール"),
            new ModifierDef("shift", KeyModifiers.MOD_SHIFT, "シフト"),
        };

        const int PLANE_ASIGNABLE_MOD_KEYS_NUM = 6;

        private static string[] shiftPlaneNames = new string[] {
            "なし",
            "通常シフト",
            "拡張シフトA",
            "拡張シフトB"
        };

        private static string[] normalKeyNames = new string[] {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
            "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
            "A", "S", "D", "F", "G", "H", "J", "K", "L",  ";",
            "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/",
            "Sp", "-", "^", "￥", "@", "[", ":", "]", "＼", ""
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

        public int AssignedKeyOrFuncColWidth {
            get { return dataGridView2.Columns != null && dataGridView2.Columns.Count > 2 ? dataGridView2.Columns[2].Width : 0; }
        }

        public DlgModConversion()
        {
            InitializeComponent();

            if (Settings.DlgModConversionHeight > 0) Height = Settings.DlgModConversionHeight;

            CancelButton = buttonCancel;
        }

        private int defaultModkeyIndex = 0;

        private bool dgv1Locked = true;
        private bool dgv2Locked = true;
        //private bool dgv3Locked = true;

        private void DlgModConversion_Load(object sender, EventArgs e)
        {
            comboBox_modKeys.Visible = true;
            comboBox_shiftPlaneOn.Visible = false;
            comboBox_shiftPlaneOff.Visible = false;
            comboBox_modKeys._setItems(modifierKeys.Select(x => x.Description));
            comboBox_shiftPlaneOn._setItems(shiftPlaneNames);
            comboBox_shiftPlaneOff._setItems(shiftPlaneNames);

            dataGridView1.Visible = false;
            dataGridView2.Visible = true;
            dataGridView3.Visible = false;
            setDataGridViewForShiftPlane();
            setDataGridViewForSingleHit();
            setDataGridViewForExtModifier();

            defaultModkeyIndex = modifierKeys._findIndex(x => x.ModKey == VirtualKeys.DefaultExtModifierKey);
            comboBox_modKeys.SelectedIndex = defaultModkeyIndex;
            radioButton_modKeys.Checked = true;
        }

        private void setDataGridViewForShiftPlane()
        {
            double dpiRate = ScreenInfo.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView3;
            dgv._defaultSetup(rowHeight, rowHeight);
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            int keyCodeWidth = (int)(30 * dpiRate);
            int keyNameWidth = (int)(80 * dpiRate);
            int planeNameOnWidth = (int)(200 * dpiRate);
            int planeNameOffWidth = (int)(dgv.Width - keyCodeWidth - keyNameWidth - planeNameOnWidth - 4 * dpiRate);
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyCode", "No", keyCodeWidth, false, false, DgvHelpers.READONLY_SELECTION_COLOR, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyName", "拡張修飾キー", keyNameWidth, false, false, DgvHelpers.READONLY_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn("planeNameOn", "漢直ON時シフト面", planeNameOnWidth, false, false, DgvHelpers.READONLY_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn("planeNameOff", "漢直OFF時シフト面", planeNameOffWidth, false, false, DgvHelpers.READONLY_SELECTION_COLOR));

            dgv.Rows.Add(PLANE_ASIGNABLE_MOD_KEYS_NUM);

            renewShiftPlaneDgv();
        }

        private void renewShiftPlaneDgv()
        {
            //dgv3Locked = true;
            var dgv = dataGridView3;
            int num = dgv.Rows.Count;

            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i;
                dgv.Rows[i].Cells[1].Value = modifierKeys[i].Description;
                uint modKey = modifierKeys[i].ModKey;
                dgv.Rows[i].Cells[2].Value = shiftPlaneNames._getNth((int)VirtualKeys.ShiftPlaneForShiftModKey._safeGet(modKey)) ?? "";
                dgv.Rows[i].Cells[3].Value = shiftPlaneNames._getNth((int)VirtualKeys.ShiftPlaneForShiftModKeyWhenDecoderOff._safeGet(modKey)) ?? "";
            }
            //dgv3Locked = false;
        }

        private void setDataGridViewForSingleHit()
        {
            double dpiRate = ScreenInfo.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView1;
            dgv._defaultSetup(rowHeight, rowHeight);
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            int keyCodeWidth = (int)(30 * dpiRate);
            int keyNameWidth = (int)(80 * dpiRate);
            int funcNameWidth = (int)(180 * dpiRate);
            int funcDescWidth = (int)(dgv.Width - 20 * dpiRate - keyCodeWidth - keyNameWidth - funcNameWidth);
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyCode", "No", keyCodeWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyName", "単打キー", keyNameWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcName", "割り当てキー/機能名", funcNameWidth, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcDesc", "機能説明", funcDescWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));

            dgv.Rows.Add(singleHitModifiers.Length);

            renewSingleHitDgv();
        }

        private void renewSingleHitDgv()
        {
            dgv1Locked = true;
            var dgv = dataGridView1;
            int num = dgv.Rows.Count;

            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i;
                int deckey = singleHitModifiers[i];
                dgv.Rows[i].Cells[1].Value = SpecialKeysAndFunctions.GetKeyOrFuncByDeckey(deckey)?.Name ?? "";
                string assigned = "";
                string desc = "";
                var target = VirtualKeys.SingleHitDefs._safeGet(deckey);
                if (target._notEmpty()) {
                    var kof = SpecialKeysAndFunctions.GetKeyOrFuncByName(target);
                    assigned = kof != null ? kof.Name : target;
                    desc = kof != null ? kof.Description : "";
                }
                dgv.Rows[i].Cells[2].Value = assigned;
                dgv.Rows[i].Cells[3].Value = desc;
            }
            dgv1Locked = false;
        }

        private void setDataGridViewForExtModifier()
        {
            double dpiRate = ScreenInfo.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView2;
            dgv._defaultSetup(rowHeight, rowHeight);
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            int keyCodeWidth = (int)(30 * dpiRate);
            int keyNameWidth = (int)(80 * dpiRate);
            int funcNameWidth = (int)(Settings.AssignedKeyOrFuncColWidth._gtZeroOr(180) * dpiRate);
            int funcDescWidth = (int)(290 * dpiRate);
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyCode", "No", keyCodeWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyName", "被修飾キー", keyNameWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcName", "割り当てキー/機能名", funcNameWidth, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcDesc", "機能説明", funcDescWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));

            dgv.Rows.Add(normalKeyNames.Length + extModifiees.Length);
        }

        private void renewExtModifierDgv()
        {
            int idx = comboBox_modKeys.SelectedIndex;
            var modKeyDef = modifierKeys._getNth(idx);
            if (modKeyDef == null) return;

            dgv2Locked = true;
            uint modKey = modKeyDef.ModKey;
            var dgv = dataGridView2;
            int num = dgv.Rows.Count;
            var dict = VirtualKeys.ExtModifierKeyDefs._safeGet(modKey);
            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i;
                if (i < normalKeyNames.Length) {
                    dgv.Rows[i].Cells[1].Value = normalKeyNames[i];
                } else {
                    dgv.Rows[i].Cells[1].Value = SpecialKeysAndFunctions.GetKeyOrFuncByDeckey(extModifiees[i - normalKeyNames.Length])?.Name ?? "";
                }
                string assigned = "";
                string desc = "";
                KeyOrFunction kof = null;
                if (dict != null) {
                    int deckey = i < normalKeyNames.Length ? i : extModifiees[i - normalKeyNames.Length];
                    var target = dict._safeGet(deckey);
                    if (target._notEmpty()) {
                        kof = SpecialKeysAndFunctions.GetKeyOrFuncByName(target);
                        if (kof != null) {
                            assigned = kof.Name;
                            desc = kof.Description;
                        } else {
                            assigned = target;
                        }
                    }
                }
                dgv.Rows[i].Cells[2].Value = assigned;
                dgv.Rows[i].Cells[3].Value = desc;
            }
            dgv2Locked = false;
        }

        private void selectModKey()
        {
            int idx = comboBox_modKeys.SelectedIndex;
            var modKeyDef = modifierKeys._getNth(idx);
            uint modKey = modKeyDef?.ModKey ?? 0;
            bool bAssignable = idx >= 0 && idx < PLANE_ASIGNABLE_MOD_KEYS_NUM;
            if (bAssignable) {
                comboBox_shiftPlaneOn.SelectedIndex = (int)VirtualKeys.ShiftPlaneForShiftModKey._safeGet(modKey);
                comboBox_shiftPlaneOff.SelectedIndex = (int)VirtualKeys.ShiftPlaneForShiftModKeyWhenDecoderOff._safeGet(modKey);
            }

            bool shiftPlaneVisible = !radioButton_singleHit.Checked && bAssignable;
            label_shiftPlaneOn.Visible = shiftPlaneVisible;
            comboBox_shiftPlaneOn.Visible = shiftPlaneVisible;
            label_shiftPlaneOff.Visible = shiftPlaneVisible;
            comboBox_shiftPlaneOff.Visible = shiftPlaneVisible;

            renewExtModifierDgv();
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

        private void radioButtonCheckedChanged()
        {
            dataGridView1.Visible = radioButton_singleHit.Checked;
            dataGridView2.Visible = radioButton_modKeys.Checked;
            dataGridView3.Visible = radioButton_shiftPlane.Checked;

            if (!radioButton_singleHit.Checked) {
                int idx = comboBox_modKeys.SelectedIndex;
                int nItems = radioButton_modKeys.Checked ? modifierKeys.Length : PLANE_ASIGNABLE_MOD_KEYS_NUM;
                comboBox_modKeys._setItems(modifierKeys.Take(nItems).Select(x => x.Description));
                comboBox_modKeys.SelectedIndex = idx < nItems ? idx : defaultModkeyIndex;
            }

            bool bModkeysVisible = !radioButton_singleHit.Checked;
            bool bAssignable = comboBox_modKeys.SelectedIndex >= 0 && comboBox_modKeys.SelectedIndex < PLANE_ASIGNABLE_MOD_KEYS_NUM;
            bool bShiftPlaneVisible = bModkeysVisible && bAssignable;
            bool bShiftPlaneEnabled = radioButton_shiftPlane.Checked;

            label_modKeys.Visible = bModkeysVisible;
            comboBox_modKeys.Visible = bModkeysVisible;
            label_shiftPlaneOn.Visible = bShiftPlaneVisible;
            comboBox_shiftPlaneOn.Visible = bShiftPlaneVisible;
            comboBox_shiftPlaneOn.Enabled = bShiftPlaneEnabled;
            label_shiftPlaneOff.Visible = bShiftPlaneVisible;
            comboBox_shiftPlaneOff.Visible = bShiftPlaneVisible;
            comboBox_shiftPlaneOff.Enabled = bShiftPlaneEnabled;
        }

        private void radioButton_modKeys_CheckedChanged(object sender, EventArgs e)
        {
            radioButtonCheckedChanged();
        }

        private void radioButton_singleHit_CheckedChanged(object sender, EventArgs e)
        {
            radioButtonCheckedChanged();
        }

        private void radioButton_shiftPlane_CheckedChanged(object sender, EventArgs e)
        {
            radioButtonCheckedChanged();
        }

        private void comboBox_modKeys_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectModKey();
        }

        private void selectShiftPlane(bool bOn, int idx)
        {
            int modkeyIdx = comboBox_modKeys.SelectedIndex;
            if (modkeyIdx < 0 || modkeyIdx >= PLANE_ASIGNABLE_MOD_KEYS_NUM) return;

            var modKeyDef = modifierKeys._getNth(modkeyIdx);
            if (modKeyDef == null) return;

            uint modKey = modKeyDef.ModKey;
            var plane = VirtualKeys.GetShiftPlane(idx);
            if (bOn) {
                VirtualKeys.ShiftPlaneForShiftModKey[modKey] = plane;
            } else {
                VirtualKeys.ShiftPlaneForShiftModKeyWhenDecoderOff[modKey] = plane;
            }

            renewShiftPlaneDgv();
        }

        private void comboBox_shiftPlaneOn_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectShiftPlane(true, comboBox_shiftPlaneOn.SelectedIndex);
        }

        private void comboBox_shiftPlaneOff_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectShiftPlane(false, comboBox_shiftPlaneOff.SelectedIndex);
        }

        private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv2Locked) return;

            logger.DebugH(() => $"ENTER: row={e.RowIndex}, col={e.ColumnIndex}");

            const int TARGET_COL = 2;

            if (e.ColumnIndex != TARGET_COL) return;

            var dgv = dataGridView2;
            int row = e.RowIndex;
            if (row < 0 || row >= dgv.Rows.Count) return;

            int idx = comboBox_modKeys.SelectedIndex;
            var modKeyDef = modifierKeys._getNth(idx);
            if (modKeyDef == null) return;

            try {
                uint modKey = modKeyDef.ModKey;
                var dict = VirtualKeys.ExtModifierKeyDefs._safeGetOrNewInsert(modKey);

                int deckey = row < normalKeyNames.Length ? row : extModifiees[row - normalKeyNames.Length];
                var target = dgv.Rows[row].Cells[TARGET_COL].Value?.ToString() ?? "";
                if (target._notEmpty()) {
                    dict[deckey] = target;
                } else {
                    if (dict.ContainsKey(deckey)) dict.Remove(deckey);
                }
                renewExtModifierDgv();

            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
            }

            logger.DebugH(() => $"LEAVE");
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv1Locked) return;

            logger.DebugH(() => $"ENTER: row={e.RowIndex}, col={e.ColumnIndex}");

            const int TARGET_COL = 2;

            if (e.ColumnIndex != TARGET_COL) return;

            var dgv = dataGridView1;
            int row = e.RowIndex;
            if (row < 0 || row >= dgv.Rows.Count) return;

            int deckey = singleHitModifiers._getNth(row, -1);
            if (deckey < 0) return;

            try {
                var target = dgv.Rows[row].Cells[TARGET_COL].Value?.ToString() ?? "";
                if (target._notEmpty()) {
                    VirtualKeys.SingleHitDefs[deckey] = target;
                } else {
                    if (VirtualKeys.SingleHitDefs.ContainsKey(deckey)) VirtualKeys.SingleHitDefs.Remove(deckey);
                }
                renewSingleHitDgv();

            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
            }

            logger.DebugH(() => $"LEAVE");
        }

        private void selectKeyOrFuncName(DataGridView dgv, int ridx)
        {
            using (var dlg = new DlgKeywordSelector()) {
                if (dlg.ShowDialog() == DialogResult.OK) {
                    var keyword = dlg.SelectedWord;
                    if (keyword._notEmpty()) {
                        dgv.EndEdit();
                        dgv.Rows[ridx].Cells[2].Value = keyword;
                    }
                }
                if (Settings.DlgKeywordSelectorHeight != dlg.Height) {
                    Settings.SetUserIni("dlgKeywordSelectorHeight", dlg.Height);
                    Settings.DlgKeywordSelectorHeight = dlg.Height;
                }
            }
        }

        private void dgvCellMouseClick(DataGridView dgv, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            int ridx = e.RowIndex;
            if (ridx < 0 || ridx >= dgv.Rows.Count) return;

            selectKeyOrFuncName(dgv, ridx);
        }

        private void dgvCellMouseDoubleClick(DataGridView dgv, DataGridViewCellMouseEventArgs e)
        {
            int ridx = e.RowIndex;
            if (ridx < 0 || ridx >= dgv.Rows.Count) return;

            selectKeyOrFuncName(dgv, ridx);
        }

        private void dgvKeyDown(DataGridView dgv, KeyEventArgs e)
        {
            if (!dgv.IsCurrentCellInEditMode) {
                if (dgv.CurrentCell != null && dgv.CurrentCell.ColumnIndex == 2) {
                    if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete) {
                        dgv.CurrentCell.Value = "";
                    }
                }
            }
        }

        private void dataGridView2_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            dgvCellMouseClick(dataGridView2, e);
        }

        private void dataGridView2_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            dgvCellMouseDoubleClick(dataGridView2, e);
        }

        private void dataGridView2_KeyDown(object sender, KeyEventArgs e)
        {
            dgvKeyDown(dataGridView2, e);
        }

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            dgvCellMouseClick(dataGridView1, e);
        }

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            dgvCellMouseDoubleClick(dataGridView1, e);
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            dgvKeyDown(dataGridView1, e);
        }
    }
}
