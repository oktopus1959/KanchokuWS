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

namespace KanchokuWS.Gui
{
    public partial class DlgModConversion : Form
    {
        private static Logger logger = Logger.GetLogger();

        private static KeyOrFunction[] modifierKeys;

        private int PLANE_ASIGNABLE_MOD_KEYS_NUM;

        private static string[] shiftPlaneNames = new string[] {
            "なし",
            "通常シフト",
            "拡張シフトA",
            "拡張シフトB",
            "拡張シフトC",
            "拡張シフトD",
            "拡張シフトE",
            "拡張シフトF",
        };

        private static string[] qwertyKeyNames = new string[] {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
            "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
            "A", "S", "D", "F", "G", "H", "J", "K", "L",  ";",
            "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/",
            "Space", "-", "^", "￥", "@", "[", ":", "]", "＼", "N/A"
        };

        private static List<string> normalKeyNames = null;

        private void readCharsDefFile()
        {
            if (normalKeyNames == null) {
                if (Settings.CharsDefFile._notEmpty()) {
                    var lines = Helper.GetFileContent(KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.CharsDefFile))._safeReplace("\r", "")._split('\n');
                    if (lines._notEmpty()) {
                        normalKeyNames = new List<string>();
                        int i = 0;
                        while (i < lines.Length) {
                            var line = lines[i++];
                            if (line._startsWith("## NORMAL")) {
                                bool bSpace = false;
                                while (i < lines.Length) {
                                    line = lines[i++];
                                    if (line._startsWith("## END")) break;
                                    foreach (var ch in line) {
                                        if (ch == ' ') {
                                            if (bSpace) {
                                                normalKeyNames.Add("N/A");
                                            } else {
                                                normalKeyNames.Add("Space");
                                                bSpace = true;
                                            }
                                        } else if (ch == '\\') {
                                            normalKeyNames.Add("＼");
                                        } else {
                                            normalKeyNames.Add(ch.ToString()._toUpper());
                                        }
                                    }
                                }
                            } else if (line._startsWith("## YEN=")) {
                                int yenPos = line._safeSubstring(7)._parseInt(-1);
                                if (yenPos >= 0 && yenPos < normalKeyNames.Count) {
                                    normalKeyNames[yenPos] = "￥";
                                }
                            }
                        }
                    }
                }
                if (normalKeyNames._isEmpty()) {
                    normalKeyNames = qwertyKeyNames.ToList();
                }
            }
        }

        private static KeyOrFunction[] singleHitKeys;

        private static KeyOrFunction[] extModifiees;

        public int AssignedKeyOrFuncColWidth {
            get { return dataGridView2.Columns != null && dataGridView2.Columns.Count > 2 ? dataGridView2.Columns[2].Width : 0; }
        }

        /// <summary>コンストラクタ</summary>
        public DlgModConversion()
        {
            readCharsDefFile();
            modifierKeys = SpecialKeysAndFunctions.GetModifierKeys();
            PLANE_ASIGNABLE_MOD_KEYS_NUM = modifierKeys.Where(x => x.IsExtModifier).Count();
            extModifiees = SpecialKeysAndFunctions.GetModifieeKeys();
            singleHitKeys = SpecialKeysAndFunctions.GetSingleHitKeys();

            InitializeComponent();

            if (Settings.DlgModConversionHeight > 0) Height = Settings.DlgModConversionHeight;
            if (Settings.DlgModConversionWidth > 0) Width = Settings.DlgModConversionWidth;

            //CancelButton = buttonCancel;
            DialogResult = DialogResult.None;
        }

        public static void Initialize()
        {
            normalKeyNames = null;
        }

        private int defaultModkeyIndex = 0;

        private bool dgv1Locked = true;
        private bool dgv2Locked = true;
        //private bool dgv3Locked = true;

        private void DlgModConversion_Load(object sender, EventArgs e)
        {
            panel_shiftPlaneHint.Visible = false;
            comboBox_modKeys.Visible = true;
            comboBox_shiftPlaneOn.Visible = false;
            comboBox_shiftPlaneOff.Visible = false;
            comboBox_modKeys._setItems(modifierKeys.Select(x => getModifiedDescription(x)));
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

        private string getModifiedDescription(KeyOrFunction modDef)
        {
            if (modDef == null) return "";

            string marker = "";
            var dict = VirtualKeys.ExtModifierKeyDefs._safeGet(modDef.ModKey);
            if (dict != null) {
                int normalKeysNum = normalKeyNames._safeCount();
                int num = normalKeysNum  + extModifiees.Length;
                for (int i = 0; i < num; ++i) {
                    int deckey = i < normalKeysNum ? i : extModifiees[i - normalKeysNum].DecKey;
                    if (dict._safeGet(deckey)._notEmpty()) {
                        marker = " (＊)";
                        break;
                    }
                }
            }
            return modDef.ModName + marker;
        }

        private void setDataGridViewForShiftPlane()
        {
            double dpiRate = ScreenInfo.Singleton.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView3;
            dgv._defaultSetup(rowHeight, rowHeight);
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            dgv._setSelectionColorReadOnly();
            int keyCodeWidth = (int)(30 * dpiRate);
            int keyNameWidth = (int)(100 * dpiRate);
            int planeNameOnWidth = (int)(200 * dpiRate);
            int planeNameOffWidth = (int)(dgv.Width - keyCodeWidth - keyNameWidth - planeNameOnWidth - 4 * dpiRate);
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Centered("keyCode", "No", keyCodeWidth));
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly("keyName", "拡張修飾キー", keyNameWidth));
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly("planeNameOn", "漢直ON時シフト面", planeNameOnWidth));
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly("planeNameOff", "漢直OFF時シフト面", planeNameOffWidth));

            dgv.Rows.Add(modifierKeys.Length);

            renewShiftPlaneDgv();
        }

        private void renewShiftPlaneDgv()
        {
            //dgv3Locked = true;
            var dgv = dataGridView3;
            int num = dgv.Rows.Count._highLimit(modifierKeys.Length);

            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i;
                dgv.Rows[i].Cells[1].Value = getModifiedDescription(modifierKeys[i]);
                if (i < PLANE_ASIGNABLE_MOD_KEYS_NUM) {
                    uint modKey = modifierKeys[i].ModKey;
                    dgv.Rows[i].Cells[2].Value = shiftPlaneNames._getNth((int)VirtualKeys.ShiftPlaneForShiftModKey._safeGet(modKey)) ?? "";
                    dgv.Rows[i].Cells[3].Value = shiftPlaneNames._getNth((int)VirtualKeys.ShiftPlaneForShiftModKeyWhenDecoderOff._safeGet(modKey)) ?? "";
                } else if (modifierKeys._getNth(i)?.ModKey == KeyModifiers.MOD_SHIFT) {
                    dgv.Rows[i].Cells[2].Value = "通常シフト";
                    dgv.Rows[i].Cells[3].Value = "通常シフト";
                } else {
                    dgv.Rows[i].Cells[2].Value = "なし（割り当て不可）";
                    dgv.Rows[i].Cells[3].Value = "なし（割り当て不可）";
                }
            }
            //dgv3Locked = false;
        }

        private void setDataGridViewForSingleHit()
        {
            double dpiRate = ScreenInfo.Singleton.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView1;
            dgv._defaultSetup(rowHeight, rowHeight);
            dgv._setSelectionColorReadOnly();
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            int keyCodeWidth = (int)(30 * dpiRate);
            int keyNameWidth = (int)(80 * dpiRate);
            int funcNameWidth = (int)(180 * dpiRate);
            int funcDescWidth = (int)(dgv.Width - 20 * dpiRate - keyCodeWidth - keyNameWidth - funcNameWidth);
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Sortable_Centered("keyCode", "No", keyCodeWidth, DgvHelpers.READONLY_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Sortable("keyName", "単打キー", keyNameWidth, DgvHelpers.READONLY_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn_Sortable("funcName", "割り当てキー/機能名", funcNameWidth, DgvHelpers.HIGHLIGHT_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Sortable("funcDesc", "機能説明", funcDescWidth, DgvHelpers.READONLY_SELECTION_COLOR));

            dgv.Rows.Add(singleHitKeys.Length);

            renewSingleHitDgv();
        }

        private void renewSingleHitDgv()
        {
            dgv1Locked = true;
            var dgv = dataGridView1;
            int num = dgv.Rows.Count;

            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i;
                var kof = singleHitKeys[i];
                int deckey = kof.DecKey;
                dgv.Rows[i].Cells[1].Value = kof.ModName._orElse(kof.Name);
                string assigned = "";
                string desc = "";
                var target = VirtualKeys.SingleHitDefs._safeGet(kof.DecKey);
                if (target._notEmpty()) {
                    kof = SpecialKeysAndFunctions.GetKeyOrFuncByName(target);
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
            double dpiRate = ScreenInfo.Singleton.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView2;
            dgv._defaultSetup(rowHeight, rowHeight);
            dgv._setSelectionColorReadOnly();
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            int keyCodeWidth = (int)(30 * dpiRate);
            int keyNameWidth = (int)(80 * dpiRate);
            int funcNameWidth = (int)(Settings.AssignedKeyOrFuncColWidth._gtZeroOr(180) * dpiRate);
            int funcDescWidth = (int)(290 * dpiRate);
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Sortable_Centered("keyCode", "No", keyCodeWidth));
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Sortable("keyName", "被修飾キー", keyNameWidth));
            dgv.Columns.Add(dgv._makeTextBoxColumn_Sortable("funcName", "割り当てキー/機能名", funcNameWidth, DgvHelpers.HIGHLIGHT_SELECTION_COLOR));
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly_Sortable("funcDesc", "機能説明", funcDescWidth));

            dgv.Rows.Add(normalKeyNames._safeCount() + extModifiees.Length);
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
            int normalKeysNum = normalKeyNames._safeCount();
            var dict = VirtualKeys.ExtModifierKeyDefs._safeGet(modKey);
            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i;
                if (i < normalKeysNum) {
                    dgv.Rows[i].Cells[1].Value = normalKeyNames[i];
                } else {
                    dgv.Rows[i].Cells[1].Value = extModifiees[i - normalKeysNum].Name;
                }
                string assigned = "";
                string desc = "";
                KeyOrFunction kof = null;
                if (dict != null) {
                    int deckey = i < normalKeysNum ? i : extModifiees[i - normalKeysNum].DecKey;
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
            try {
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
            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
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

        private void radioButtonCheckedChanged()
        {
            try {
                dataGridView1.Visible = radioButton_singleHit.Checked;
                dataGridView2.Visible = radioButton_modKeys.Checked;
                dataGridView3.Visible = radioButton_shiftPlane.Checked;
                panel_shiftPlaneHint.Visible = radioButton_shiftPlane.Checked;

                if (!radioButton_singleHit.Checked) {
                    int idx = comboBox_modKeys.SelectedIndex;
                    int nItems = radioButton_modKeys.Checked ? modifierKeys.Length : PLANE_ASIGNABLE_MOD_KEYS_NUM;
                    comboBox_modKeys._setItems(modifierKeys.Take(nItems).Select(x => getModifiedDescription(x)));
                    comboBox_modKeys.SelectedIndex = idx < nItems ? idx : (defaultModkeyIndex < PLANE_ASIGNABLE_MOD_KEYS_NUM ? defaultModkeyIndex : 0);
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
            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
            }
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
            try {
                int modkeyIdx = comboBox_modKeys.SelectedIndex;
                if (modkeyIdx < 0 || modkeyIdx >= PLANE_ASIGNABLE_MOD_KEYS_NUM) return;

                var modKeyDef = modifierKeys._getNth(modkeyIdx);
                if (modKeyDef == null) return;

                if (idx < VirtualKeys.ShiftPlane_NUM) {
                    uint modKey = modKeyDef.ModKey;
                    if (bOn) {
                        VirtualKeys.ShiftPlaneForShiftModKey[modKey] = idx;
                    } else {
                        VirtualKeys.ShiftPlaneForShiftModKeyWhenDecoderOff[modKey] = idx;
                    }
                }

                renewShiftPlaneDgv();
            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
            }
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

                int normalKeysNum = normalKeyNames._safeCount();
                int deckey = row < normalKeysNum ? row : extModifiees[row - normalKeysNum].DecKey;
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

            int deckey = singleHitKeys._getNth(row)?.DecKey ?? -1;
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
            int no = dgv.Rows[ridx].Cells[0].Value.ToString()._parseInt(0);     // 元の順の番号を取得しておく
            using (var dlg = new DlgKeywordSelector()) {
                try {
                    if (dlg.ShowDialog() == DialogResult.OK) {
                        var keyword = dlg.SelectedWord;
                        if (keyword._notEmpty()) {
                            dgv.EndEdit();                              // 編集中だったセルの場合に、いったんそれをコミットする必要がある
                            dgv.Rows[no].Cells[2].Value = keyword;      // セルの値を変更しようとすると、元の順に並びが戻るので注意
                            if (no != ridx) {
                                dgv.Rows[no].Cells[2].Selected = true;
                            }
                        }
                    }
                    if (Settings.DlgKeywordSelectorHeight != dlg.Height) {
                        Settings.SetUserIni("dlgKeywordSelectorHeight", dlg.Height);
                        Settings.DlgKeywordSelectorHeight = dlg.Height;
                    }
                } catch (Exception ex) {
                    logger.Error(ex._getErrorMsg());
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
