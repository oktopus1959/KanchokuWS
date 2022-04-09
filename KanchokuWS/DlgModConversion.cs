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
        public DlgModConversion()
        {
            InitializeComponent();
        }

        private void DlgModConversion_Load(object sender, EventArgs e)
        {
            radioButton_modKeys.Checked = true;
            dataGridView1.Visible = false;
            setDataGridView1();
            setDataGridView2();
        }

        private static string[] keyNames = new string[] {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
            "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
            "A", "S", "D", "F", "G", "H", "J", "K", "L",  ";",
            "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/",
            "Sp", "-", "^", "￥", "@", "[", ":", "]", "\\", ""
        };

        private static Dictionary<string, string> specialDecKeysFromName = new Dictionary<string, string>() {
            {"esc", "Esc"},
            {"escape", "Esc"},
            {"zenkaku", "" },
            {"hanzen", "" },
            {"tab", "" },
            {"caps", "" },
            {"capslock", "" },
            {"alnum", "" },
            {"alphanum", "" },
            {"eisu", "" },
            {"nfer", "" },
            {"xfer", "" },
            {"kana", "" },
            {"hiragana", "" },
            {"bs", "" },
            {"back", "" },
            {"backspace", "" },
            {"enter", "" },
            {"ins", "" },
            {"insert", "" },
            {"del", "" },
            {"delete", "" },
            {"home", "" },
            {"end", "" },
            {"pgup", "" },
            {"pageup", "" },
            {"pgdn", "" },
            {"pagedown", "" },
            {"left", "" },
            {"leftarrow", "" },
            {"right", "" },
            {"rightarrow", "" },
            {"up", "" },
            {"uparrow", "" },
            {"down", "" },
            {"downarrow", "" },
            {"rshift", "" },
            {"space", "" },
            {"shiftspace", "" },
            {"modetoggle", "" },
            {"modetogglefollowcaret", "" },
            {"activate", "" },
            {"deactivate", "" },
            {"fullescape", "" },
            {"unblock", "" },
            {"toggleblocker", "" },
            {"blockertoggle", "" },
            {"helprotate", "" },
            {"helpunrotate", "" },
            {"daterotate", "" },
            {"dateunrotate", "" },
            {"histnext", "" },
            {"histprev", "" },
            {"strokehelp", "" },
            {"bushucomphelp", "" },
            {"zenkakuconvert", "" },
            {"zenkakuconversion", "" },
            {"katakanaconvert", "" },
            {"katakanaconversion", "" },
            {"romanstrokeguide", "" },
            {"upperromanstrokeguide", "" },
            {"hiraganastrokeguide", "" },
            {"exchangecodetable", "" },
            {"leftshiftblocker", "" },
            {"rightshiftblocker", "" },
            {"leftshiftmazestartpos", "" },
            {"rightshiftmazestartpos", "" },
            {"copyandregisterselection", "" },
            {"copyselectionandsendtodictionary", "" },
            {"clearstroke", "" },
            //{"^a", DecoderKeys.CTRL_},
        };

        private static string[][] specialDecKeys = new string[][] {
            new string[]{ "Esc", "Escape" },
            new string[]{ "Zenkaku", "全角/半角" },
            new string[]{ "Tab", "Tab" },
            new string[]{ "CapsLock", "Caps Lock" },
            new string[]{ "AlphaNum", "英数" },
            new string[]{ "Nfer", "無変換" },
            new string[]{ "Xfer", "変換" },
            new string[]{ "Hiragana", "ひらがな" },
            new string[]{ "BackSpace", "Back Space" },
            new string[]{ "Enter", "Enter" },
            new string[]{ "Insert", "Insert" },
            new string[]{ "Delete", "Delete" },
            new string[]{ "Home", "Home" },
            new string[]{ "End", "End" },
            new string[]{ "PageUp", "Page Up" },
            new string[]{ "PageDown", "Page Down" },
            new string[]{ "Left", "←" },
            new string[]{ "Right", "→" },
            new string[]{ "Up", "↑" },
            new string[]{ "Down", "↓" },
            new string[]{ "Rshift", "右シフト" },
            new string[]{ "ModeToggle", "漢直モードのトグル" },
            new string[]{ "ModeToggleFollowCaret", "漢直モードのトグル（カレットへの再追従）" },
            new string[]{ "Activate", "漢直モードに入る" },
            new string[]{ "Deactivate", "漢直モードから出る" },
            new string[]{ "ExchangeCodeTable", "主・副テーブルファイルを切り替える" },
            new string[]{ "ClearStroke", "打鍵中のストロークを取り消して、第1打鍵待ちに戻る" },
            new string[]{ "FullEscape", "入力途中状態をクリアし、ミニバッファ末尾にブロッカーを置く" },
            new string[]{ "Unblock", "ミニバッファ末尾のブロッカーを解除する" },
            new string[]{ "BlockerToggle", "ミニバッファ末尾のブロッカーを設定・解除する" },
            new string[]{ "HistNext", "履歴を先頭から選択" },
            new string[]{ "HistPrev", "履歴を末尾から選択" },
            new string[]{ "HelpRotate", "ストロークヘルプの正順回転" },
            new string[]{ "HelpUnRotate", "ストロークヘルプの逆順回転" },
            new string[]{ "DateRotate", "日時変換の入力(フォーマットの正順切替)" },
            new string[]{ "DateUnrotate", "日時変換の入力(フォーマットの逆順切替)" },
            new string[]{ "StrokeHelp", "最後に入力した文字のストロークヘルプ" },
            new string[]{ "BushuCompHelp", "部首合成ヘルプ表示" },
            new string[]{ "RomanStrokeGuide", "ローマ字による読み打鍵ガイドモードのON/OFF" },
            new string[]{ "UpperRomanStrokeGuide", "英大文字ローマ字による読み打鍵ガイドモード" },
            new string[]{ "HiraganaStrokeGuide", "ひながな入力による読み打鍵ガイドモード" },
            new string[]{ "ZenkakuConversion", "全角変換入力モードのON/OFF" },
            new string[]{ "KatakanaConversion", "カタカナ入力モードのON/OFF" },
            new string[]{ "Space", "Space に変換" },
            new string[]{ "ShiftSpace", "Shift+Space に変換" },
            new string[]{ "LeftShiftBlocker", "交ぜ書きブロッカーの左移動" },
            new string[]{ "RightShiftBlocker", "交ぜ書きブロッカーの右移動" },
            new string[]{ "LeftShiftMazeStartPos", "交ぜ書き開始位置の左移動" },
            new string[]{ "RightShiftMazeStartPos", "交ぜ書き開始位置の右移動" },
            new string[]{ "CopyAndRegisterSelection", "選択されている部分をデコーダの辞書に送って登録" },
        };

        private void setDataGridView1()
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
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcName", "機能名", funcNameWidth, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcDesc", "機能説明", funcDescWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));

            int num = 21;
            dgv.Rows.Add(num);

            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i + 1;
                dgv.Rows[i].Cells[1].Value = specialDecKeys[i][0];
                dgv.Rows[i].Cells[2].Value = specialDecKeys[i+21][0];
                dgv.Rows[i].Cells[3].Value = specialDecKeys[i+21][1];
            }

        }

        private void setDataGridView2()
        {
            double dpiRate = ScreenInfo.PrimaryScreenDpiRate._lowLimit(1.0);
            int rowHeight = (int)(20 * dpiRate);

            var dgv = dataGridView2;
            dgv._defaultSetup(rowHeight, rowHeight);
            dgv._setSelectionColorLemon();                 // 選択時の色をレモン色にする
            dgv._setDefaultFont(DgvHelpers.FontYUG9);
            int keyCodeWidth = (int)(40 * dpiRate);
            int keyNameWidth = (int)(40 * dpiRate);
            int funcNameWidth = (int)(180 * dpiRate);
            int funcDescWidth = (int)(dgv.Width - 20 * dpiRate - keyCodeWidth - keyNameWidth - funcNameWidth);
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyCode", "番号", keyCodeWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("keyName", "キー", keyNameWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcName", "機能名", funcNameWidth, true));
            dgv.Columns.Add(dgv._makeTextBoxColumn("funcDesc", "機能説明", funcDescWidth, true, false, DgvHelpers.READONLY_SELECTION_COLOR));

            int num = DecoderKeys.NORMAL_DECKEY_NUM;
            dgv.Rows.Add(num);

            for (int i = 0; i < num; ++i) {
                dgv.Rows[i].Cells[0].Value = i;
                dgv.Rows[i].Cells[1].Value = keyNames[i];
                dgv.Rows[i].Cells[2].Value = specialDecKeys[i][0];
                dgv.Rows[i].Cells[3].Value = specialDecKeys[i][1];
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

        private void radioButton_modKeys_CheckedChanged(object sender, EventArgs e)
        {
            dataGridView1.Visible = !radioButton_modKeys.Checked;
            dataGridView2.Visible = radioButton_modKeys.Checked;
            comboBox_modKeys.Enabled = radioButton_modKeys.Checked;
            comboBox_shiftPlaneOn.Enabled = radioButton_modKeys.Checked;
            comboBox_shiftPlaneOff.Enabled = radioButton_modKeys.Checked;
        }
    }
}
