using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Reflection;
using Utils;

namespace KanchokuWS.Gui
{
    public partial class DlgSettings
    {
        //-----------------------------------------------------------------------------------
        /// <summary> 開発者用設定</summary>
        void readSettings_tabDevelop()
        {
            // 開発者用
            comboBox_logLevel.SelectedIndex = Settings.GetLogLevel();
            checkBox_loggingDecKeyInfo.Checked = Settings.GetString("loggingDecKeyInfo")._parseBool();
            checkBox_bushuDicLogEnabled.Checked = Settings.BushuDicLogEnabled;
            checkBox_loggingActiveWindowInfo.Checked = Settings.LoggingActiveWindowInfo;
            checkBox_loggingVirtualKeyboardInfo.Checked = Settings.LoggingVirtualKeyboardInfo;
            checkBox_multiAppEnabled.Checked = Settings.MultiAppEnabled;
        }

        private void setDevelopStatusChecker()
        {
            button_developEnter.Enabled = false;
            checkerDevelop.CtlToBeEnabled = button_developEnter;
            checkerDevelop.ControlEnabler = tabDevelopStatusChanged;

            // 開発者用
            checkerDevelop.Add(comboBox_logLevel);
            checkerDevelop.Add(checkBox_loggingDecKeyInfo);
            checkerDevelop.Add(checkBox_bushuDicLogEnabled);
            checkerDevelop.Add(checkBox_loggingActiveWindowInfo);
            checkerDevelop.Add(checkBox_loggingVirtualKeyboardInfo);
            checkerDevelop.Add(checkBox_multiAppEnabled);

            checkerAll.Add(checkerDevelop);
        }

        private void button_developEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            frmMain?.DeactivateDecoder();

            // 開発者用
            Settings.SetUserIni("logLevel", comboBox_logLevel.SelectedIndex);
            Logger.LogLevel = comboBox_logLevel.SelectedIndex;
            Settings.SetUserIni("loggingDecKeyInfo", checkBox_loggingDecKeyInfo.Checked);
            Settings.SetUserIni("bushuDicLogEnabled", checkBox_bushuDicLogEnabled.Checked);
            //Settings.SetUserIni("loggingActiveWindowInfo", checkBox_loggingActiveWindowInfo.Checked);
            Settings.LoggingActiveWindowInfo = checkBox_loggingActiveWindowInfo.Checked;
            Settings.SetUserIni("loggingVirtualKeyboardInfo", checkBox_loggingVirtualKeyboardInfo.Checked);
            Settings.SetUserIni("multiAppEnabled", checkBox_multiAppEnabled.Checked);

            //Settings.ReadIniFile();
            // 各種定義ファイルの再読み込み
            frmMain?.ReloadSettingsAndDefFiles();

            readSettings_tabDevelop();
            checkerDevelop.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            //frmVkb?.SetNormalCellBackColors();
            frmMode?.ShowImmediately();

            // 各種定義ファイルの再読み込み
            //frmMain?.ReloadDefFiles();

            //frmMain?.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            label_okResultDevelop.Show();

            logger.InfoH("LEAVE");

        }

        private void tabDevelopStatusChanged(bool flag)
        {
            button_developClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
            changeCancelButton(flag, button_developClose);
        }

        private void label_okResultDevelop_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void button_developClose_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            if (button_developClose.Text.StartsWith("閉")) {
                this.Close();
            } else {
                readSettings_tabDevelop();
                checkerDevelop.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)
                logger.InfoH("LEAVE");
            }
        }

        private void button_developTest_Click(object sender, EventArgs e)
        {
            runTest(checkBox_testAll.Checked);
        }

        private void runTest(bool bAll)
        {
            if (frmMain == null) return;

            var lines = readAllLines($@"src\test_script{(bAll ? "_all" : "")}.txt");
            if (lines._isEmpty()) {
                SystemHelper.ShowWarningMessageBox(@"ファイルが見つかりません: bin\test_script.txt");
                return;
            }

            var regex = new Regex(@"^\s*(\w+)\(([^)]+)\)(\s*=\s*([^\s]+))?");
            var sb = new StringBuilder();

            int lineNum = 0;
            int numErrors = 0;
            foreach (var line in lines) {
                if (numErrors >= 10) break;

                ++lineNum;
                void appendError(string msg) { sb.Append(msg).Append($":\n>> ").Append(lineNum).Append(": ").Append(line).Append("\n\n"); }

                var trimmedLine = line.Trim();
                if (trimmedLine._isEmpty() || trimmedLine._startsWith("#") || trimmedLine._startsWith(";")) continue;

                var items = trimmedLine._reScan(regex);
                if (items._safeCount() < 3) {
                    appendError($"Illegal format");
                    ++numErrors;
                    continue;
                }

                var command = items[1];
                var arg = items[2];
                var expected = items._getNth(4);
                logger.DebugH(() => $"command={command}, arg={arg}, expected={(expected._notEmpty() ? expected : "null")}");
                switch (command) {
                    case "loadTable":
                        //Settings.TableFile2 = arg;
                        CombinationKeyStroke.Determiner.Singleton.Initialize(Settings.TableFile, arg);
                        frmMain.ExecCmdDecoder("createStrokeTrees", null); // ストローク木の再構築
                        frmMain.ExecCmdDecoder("useCodeTable2", null);
                        CombinationKeyStroke.Determiner.Singleton.UseSecondaryPool();
                        break;

                    case "convert":
                        if (expected._isEmpty()) {
                            appendError($"Illegal arguments");
                            ++numErrors;
                        } else {
                            var result = convertKeySequence(arg);
                            if (result != expected) {
                                appendError($"Expected={expected}, but Result={result}");
                                ++numErrors;
                            }
                        }
                        break;
                    default:
                        appendError($"Illegal command={command}");
                        ++numErrors;
                        break;
                }
            }
            
            frmMain.ReloadSettingsAndDefFiles();

            if (numErrors == 0) {
                SystemHelper.ShowInfoMessageBox("All tests passed!");
            } else {
                SystemHelper.ShowWarningMessageBox(sb.ToString());
            }
        }

        static Dictionary<char, int> keyToDeckey = new Dictionary<char, int>() {
            {'1', 0 }, {'2', 1 }, {'3', 2 }, {'4', 3 }, {'5', 4 }, {'6', 5 }, {'7', 6 }, {'8', 7 }, {'9', 8 }, {'0', 9 }, 
            {'Q', 10 }, {'W', 11 }, {'E', 12 }, {'R', 13 }, {'T', 14 }, {'Y', 15 }, {'U', 16 }, {'I', 17 }, {'O', 18 }, {'P', 19 }, 
            {'A', 20 }, {'S', 21 }, {'D', 22 }, {'F', 23 }, {'G', 24 }, {'H', 25 }, {'J', 26 }, {'K', 27 }, {'L', 28 }, {';', 29 }, 
            {'Z', 30 }, {'X', 31 }, {'C', 32 }, {'V', 33 }, {'B', 34 }, {'N', 35 }, {'M', 36 }, {',', 37 }, {'.', 38 }, {'/', 39 }, 
            {' ', 40 }, {'-', 41 }, {'^', 42 }, {'\\', 43 }, {'@', 44 }, {'[', 45 }, {':', 46 }, {']', 47 }, 
        };

        string convertKeySequence(string keys)
        {
            int prevLogLevel = Logger.LogLevel;
            if (prevLogLevel < Logger.LogLevelInfoH) Logger.LogLevel = Logger.LogLevelInfoH;

            var sb = new StringBuilder();
            void callDecoder(List<int> list)
            {
                if (list._notEmpty()) {
                    foreach (var dk in list) {
                        sb.Append(frmMain.CallDecoderWithKey(dk, 0));
                    }
                }
            }

            char toUpper(char c) { return (char)(c - 0x20); }

            int keysLen = keys._safeLength();

            int getInt(ref int pos, char ch)
            {
                int start = ++pos;
                while (pos < keysLen) {
                    if (keys[pos] == ch) break;
                    ++pos;
                }
                if (pos > start) return keys._safeSubstring(start, pos - start)._parseInt(0);
                return 0;
            }

            for (int pos = 0; pos < keysLen; ++pos) {
                char k = keys[pos];
                if (k >= 'A' && k <= 'Z') {
                    callDecoder(CombinationKeyStroke.Determiner.Singleton.KeyDown(keyToDeckey._safeGet(k), null));
                } else if (k >= 'a' && k <= 'z') {
                    callDecoder(CombinationKeyStroke.Determiner.Singleton.KeyUp(keyToDeckey._safeGet(toUpper(k))));
                } else if (k == '<') {
                    int ms = getInt(ref pos, '>');
                    if (ms > 0) Helper.WaitMilliSeconds(ms);
                } else if (k == '{') {
                    int dk = getInt(ref pos, '}');
                    callDecoder(CombinationKeyStroke.Determiner.Singleton.KeyDown(dk, null));
                } else if (k == '[') {
                    int dk = getInt(ref pos, ']');
                    callDecoder(CombinationKeyStroke.Determiner.Singleton.KeyUp(dk));
                }
            }

            Logger.LogLevel = prevLogLevel;
            return sb.ToString();
        }

        List<string> readAllLines(string filepath)
        {
            var lines = new List<string>();
            if (filepath._notEmpty()) {
                var absPath = KanchokuIni.Singleton.KanchokuDir._joinPath(filepath);
                logger.DebugH(() => $"ENTER: absFilePath={absPath}");
                var contents = Helper.GetFileContent(absPath, (e) => logger.Error(e._getErrorMsg()));
                if (contents._notEmpty()) {
                    lines.AddRange(contents._safeReplace("\r", "")._split('\n'));
                }
            }
            logger.DebugH(() => $"LEAVE: num of lines={lines.Count}");
            return lines;
        }

        //-----------------------------------------------------------------------------------
        /// <summary>情報設定</summary>
        private void initializeAboutDgv()
        {
            double dpiRate = ScreenInfo.Singleton.PrimaryScreenDpiRate._lowLimit(1.0);
            //if (dpiRate > 1.0) dpiRate *= 1.05;
            int rowHeight = (int)(20 * dpiRate);
            dgvAbout._defaultSetup(0, rowHeight, false, true);  // headerHeight=0 -> ヘッダーを表示しない, 複数セル選択OK
            dgvAbout._setAutoHeightSize(true);                  // 複数行の場合にセルの高さを自動的に変更する
            //dgvAbout._setSelectionColorSmoke();                 // 選択時の色をスモーク色にする
            dgvAbout._setSelectionColorLemon();                 // 選択時の色をレモン色にする
            dgvAbout._setDefaultFont(DgvHelpers.FontYUG9);
            //dgvAbout._setDefaultFont(DgvHelpers.FontMSG8);
            //dgvAbout._disableToolTips();
            const int DGV_COL1_WIDTH = 150;
            const int DGV_COL2_WIDTH = 450;
            int itemWidth = (int)(DGV_COL1_WIDTH * dpiRate);
            dgvAbout.Columns.Add(dgvAbout._makeTextBoxColumn_ReadOnly("itemName", "", itemWidth)._setUnresizable());
            int descWidth = (int)(DGV_COL2_WIDTH * dpiRate);
            dgvAbout.Columns.Add(dgvAbout._makeTextBoxColumn_ReadOnly("description", "", descWidth)._setUnresizable()._setWrapMode());

            int nRow = 10;
            dgvAbout.Height = nRow * rowHeight + (int)(10 * dpiRate) + 19;        // 末尾行が複数行になっていることを考慮
            dgvAbout.Width = itemWidth + descWidth + 1;
            dgvAbout.Rows.Add(nRow);

            int iRow = 0;
            dgvAbout.Rows[iRow].Cells[0].Value = "アプリケーション名／バージョン";
            dgvAbout.Rows[iRow++].Cells[1].Value = $"KanchokuWS Ver.{Settings.Version}";
            dgvAbout.Rows[iRow].Cells[0].Value = "別名";
            dgvAbout.Rows[iRow++].Cells[1].Value = "漢直窓S (KanchokuWin Spoiler / 漢直WS)";
            dgvAbout.Rows[iRow].Cells[0].Value = "プログラムパス";
            dgvAbout.Rows[iRow++].Cells[1].Value = SystemHelper.GetExePath();
            dgvAbout.Rows[iRow].Cells[0].Value = "ルートフォルダ";
            dgvAbout.Rows[iRow++].Cells[1].Value = KanchokuIni.Singleton.KanchokuDir;
            dgvAbout.Rows[iRow].Cells[0].Value = "動作対象環境";
            dgvAbout.Rows[iRow++].Cells[1].Value = "Windows 10 (.NET Framework 4.8)";
            dgvAbout.Rows[iRow].Cells[0].Value = "ドキュメント";
            dgvAbout.Rows[iRow++].Cells[1] = new DataGridViewLinkCell() { Value = Settings.ManualUrl };
            dgvAbout.Rows[iRow].Cells[0].Value = "ビルド日時";
            dgvAbout.Rows[iRow++].Cells[1].Value = Assembly.GetExecutingAssembly()._getLinkerTime().ToString("yyyy/M/d HH:mm:ss");
            dgvAbout.Rows[iRow].Cells[0].Value = "初版公開日";
            dgvAbout.Rows[iRow++].Cells[1].Value = "2021年7月10日";
            dgvAbout.Rows[iRow].Cells[0].Value = "作者";
            dgvAbout.Rows[iRow++].Cells[1].Value = "OKA Toshiyuki (岡 俊行) / @kanchokker(twitter) / @oktopus1959(github)";
            dgvAbout.Rows[iRow].Cells[0].Value = "利用条件と免責";
            dgvAbout.Rows[iRow++].Cells[1].Value =
                "ソースコードとプログラムはフリー； 辞書などのデータは各出典に従ってください。\r\n" +
                "また、当プログラムの利用によるいかなる損害についても作者に責を負わせない\r\nことに同意の上、ご利用ください。";

            dgvAbout.CurrentCell = null;
        }

        private void dgvAbout_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvAbout.RowCount && e.ColumnIndex == 1) {
                var cell = dgvAbout.Rows[e.RowIndex].Cells[1];
                if (cell is DataGridViewLinkCell) {
                    var link = cell.Value.ToString();
                    if (link._reMatchIcase(@"^https?://")) openDocumentUrl(Settings.ManualUrl);
                }
            }
        }

    }
}
