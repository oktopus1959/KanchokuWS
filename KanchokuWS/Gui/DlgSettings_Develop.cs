using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

using KanchokuWS.CombinationKeyStroke.DeterminerLib;
using KanchokuWS.TableParser;
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
            checkBox_outputDebugTableFiles.Checked = Settings.OutputDebugTableFiles;
            checkBox_showHiddleFolder.Checked = Settings.ShowHiddleFolder;
            checkBox_multiAppEnabled.Checked = Settings.MultiAppEnabled;
            textBox_warnThresholdKeyQueueCount.Text = Settings.WarnThresholdKeyQueueCount.ToString();
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
            checkerDevelop.Add(checkBox_outputDebugTableFiles);
            checkerDevelop.Add(checkBox_showHiddleFolder);
            checkerDevelop.Add(checkBox_multiAppEnabled);
            checkerDevelop.Add(textBox_warnThresholdKeyQueueCount);

            checkerAll.Add(checkerDevelop);
        }

        private void button_developEnter_Click(object sender, EventArgs e)
        {
            logger.InfoH("ENTER");
            frmMain?.DeactivateDecoderWithModifiersOff();

            // 開発者用
            Settings.SetUserIni("logLevel", comboBox_logLevel.SelectedIndex);
            Logger.LogLevel = comboBox_logLevel.SelectedIndex;
            Settings.SetUserIni("loggingDecKeyInfo", checkBox_loggingDecKeyInfo.Checked);
            Settings.SetUserIni("bushuDicLogEnabled", checkBox_bushuDicLogEnabled.Checked);
            //Settings.SetUserIni("loggingActiveWindowInfo", checkBox_loggingActiveWindowInfo.Checked);
            Settings.LoggingActiveWindowInfo = checkBox_loggingActiveWindowInfo.Checked;
            Settings.SetUserIni("loggingVirtualKeyboardInfo", checkBox_loggingVirtualKeyboardInfo.Checked);
            Settings.SetUserIni("outputDebugTableFiles", checkBox_outputDebugTableFiles.Checked);
            Settings.SetUserIni("showHiddleFolder", checkBox_showHiddleFolder.Checked);
            Settings.SetUserIni("multiAppEnabled", checkBox_multiAppEnabled.Checked);
            Settings.SetUserIni("warnThresholdKeyQueueCount", textBox_warnThresholdKeyQueueCount.Text);

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

        private void button_developReload_Click(object sender, EventArgs e)
        {
            reloadIniFileAndDefFiles();
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
            if (frmMain != null) {
                label_testCount.Text = "0/0件";
                label_testCount.Visible = true;
                new TestRunner(frmMain).RunTest(checkBox_testAll.Checked, displayTestCount);
                label_testCount.Visible = false;
            }
        }

        private void displayTestCount(string msg)
        {
            label_testCount.Text = msg;
        }

        /// <summary>ファイル出力(OBSOLETE)</summary>
        private void button_developSaveDebugTableFile_Click(object sender, EventArgs e)
        {
            //frmMain?.ExecCmdDecoder("SaveDumpTable", null);    // tmp/dump-table[13].txt (Decoder が実際に保持しているテーブルの内容をダンプしたもの)

            //KeyCombinationPool.SingletonK1?.DebugPrintFile("tmp/key-combination1.txt");
            //KeyCombinationPool.SingletonK2?.DebugPrintFile("tmp/key-combination2.txt");

            //if (Settings.TableFile._notEmpty()) new TableFileParser().ParseTableFile(Settings.TableFile, "tmp/tableFile1.tbl", KeyCombinationPool.Singleton1, true, true, false);
            //if (Settings.TableFile2._notEmpty()) new TableFileParser().ParseTableFile(Settings.TableFile2, "tmp/tableFile2.tbl", KeyCombinationPool.Singleton2, false, true, false);
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
