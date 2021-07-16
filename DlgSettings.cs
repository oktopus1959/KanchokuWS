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

namespace KanchokuWS
{
    public partial class DlgSettings : Form
    {
        private static Logger logger = Logger.GetLogger();

        public static DlgSettings ShownDlg { get; private set; } = null;

        private static int SelectedTabIndex = 0;

        public static bool BringTopMostShownDlg()
        {
            if (ShownDlg != null) {
                ShownDlg.TopMost = true;
                ShownDlg.TopMost = false;
                return true;
            }
            return false;
        }

        private FrmKanchoku frmMain;

        private FrmVirtualKeyboard frmVkb;

        private FrmModeMarker frmMode;

        private GuiStatusChecker checkerAll;
        private GuiStatusChecker checkerBasic;
        private GuiStatusChecker checkerFontColor;
        private GuiStatusChecker checkerAdvanced;
        private GuiStatusChecker checkerKeyAssign;
        private GuiStatusChecker checkerCtrlKeys;
        private GuiStatusChecker checkerHistory;

        private const int timerInterval = 200;

        //-----------------------------------------------------------------------------------
        public bool RestartRequired { get; set; } = false;

        public bool NoSave { get; set; } = false;

        //-----------------------------------------------------------------------------------
        public DlgSettings(FrmKanchoku form, FrmVirtualKeyboard vkb, FrmModeMarker mode)
        {
            frmMain = form;
            frmVkb = vkb;
            frmMode = mode;
            InitializeComponent();
        }

        public void ShowInitialMessage()
        {
            label_initialMsg.Show();
        }

        //-----------------------------------------------------------------------------------
        private void DlgSettings_Load(object sender, EventArgs e)
        {
            ShownDlg = this;

            AcceptButton = button_basicEnter;
            CancelButton = button_basicClose;

            tabControl1.SelectedIndex = SelectedTabIndex;
            changeGlobalCtrlKeysCheckBoxState();

            // 情報タブの DataGridView
            initializeAboutDgv();

            // 各タブの状態チェッカーをまとめる
            checkerAll = new GuiStatusChecker("All");
            checkerBasic = new GuiStatusChecker("Basic");
            checkerFontColor = new GuiStatusChecker("FontAndColor");
            checkerAdvanced = new GuiStatusChecker("Advanced");
            checkerKeyAssign = new GuiStatusChecker("KeyAssign");
            checkerCtrlKeys = new GuiStatusChecker("CtrlKeys");
            checkerHistory = new GuiStatusChecker("History");

            readSettings_tabBasic();
            setBasicStatusChecker();

            readSettings_tabFile();
            setFontColortatusChecker();

            readSettings_tabAdvanced();
            setAdvancedStatusChecker();

            readSettings_tabKeyAssign();
            setKeyAssignStatusChecker();

            readSettings_tabCtrlKeys();
            setCtrlKeysStatusChecker();

            readSettings_tabHistory();
            setHistoryStatusChecker();

            checkerAll.Reinitialize();

            // フォームタイマーの開始
            timer1.Interval = timerInterval;
            timer1.Start();
            logger.Info("Timer Started");
        }

        private void DlgSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            logger.InfoH("ENTER");
            timer1.Stop();
            logger.Info("Timer Stopped");
            ShownDlg = null;
            checkerAll.Dispose();
            Helper.WaitMilliSeconds(100);       // 微妙なタイミングで invoke されるのを防ぐ
            logger.InfoH("LEAVE");
        }

        private void initializeAboutDgv()
        {
            double dpiRate = ScreenInfo.PrimaryScreenDpiRate._lowLimit(1.0);
            //if (dpiRate > 1.0) dpiRate *= 1.05;
            int rowHeight = (int)(20 * dpiRate);
            dgvAbout._defaultSetup(0, rowHeight, false, true);  // headerHeight=0 -> ヘッダーを表示しない, 複数セル選択OK
            dgvAbout._setAutoHeightSize(true);                  // 複数行の場合にセルの高さを自動的に変更する
            //dgvAbout._setSelectionColorSmoke();                 // 選択時の色をスモーク色にする
            dgvAbout._setSelectionColorLemon();                 // 選択時の色をレモン色にする
            dgvAbout._setDefaultFont(DgvHelpers.FontYUG9);
            //dgvAbout._setDefaultFont(DgvHelpers.FontMSG8);
            //dgvAbout._disableToolTips();
            int itemWidth = (int)(150 * dpiRate);
            dgvAbout.Columns.Add(dgvAbout._makeTextBoxColumn_ReadOnly("itemName", "", itemWidth)._setUnresizable());
            int descWidth = (int)(400 * dpiRate);
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
            dgvAbout.Rows[iRow++].Cells[1] = new DataGridViewLinkCell() { Value = Settings.DocumentUrl };
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
                    if (link._reMatchIcase(@"^https?://")) openDocumentUrl();
                }
            }
        }

        private void openDocumentUrl()
        {
            System.Diagnostics.Process.Start(Settings.DocumentUrl);
        }

        //-----------------------------------------------------------------------------------
        /// <summary> 基本設定</summary>
        void readSettings_tabBasic()
        {
            // 漢直モードトグル/OFFキー
            selectModeToggleKeyItem(comboBox_unmodifiedToggleKey, Settings.GetString("unmodifiedHotKey"));
            selectModeToggleKeyItem(comboBox_modifiedToggleKey, Settings.GetString("hotKey"));
            if (comboBox_unmodifiedToggleKey.Text._isEmpty() && comboBox_modifiedToggleKey.Text._isEmpty()) {
                selectModeToggleKeyItem(comboBox_modifiedToggleKey, "dc");
            }
            selectModeToggleKeyItem(comboBox_unmodifiedOffKey, Settings.GetString("unmodifiedOffHotKey"));
            selectModeToggleKeyItem(comboBox_modifiedOffKey, Settings.GetString("offHotKey"));

            // 仮想鍵盤表示
            radioButton_normalVkb.Checked = Settings.VirtualKeyboardShowStrokeCount > 0;
            radioButton_modeMarker.Checked = !radioButton_normalVkb.Checked;
            textBox_vkbShowStrokeCount.Text = $"{Math.Abs(Settings.VirtualKeyboardShowStrokeCount)}";
            //checkBox_hideTopText.Checked = Settings.GetString("topBoxMode")._toLower()._equalsTo("hideonselect");
            //checkBox_hideTopText.Enabled = radioButton_modeMarker.Checked;

            // 開始・終了
            textBox_splashWindowShowDuration.Text = $"{Settings.SplashWindowShowDuration}";
            checkBox_confirmOnClose.Checked = Settings.ConfirmOnClose;

            // ファイル
            textBox_keyboardFile.Text = Settings.KeyboardFile;      // Settings.GetString("keyboard", Settings.KeyboardFile);
            textBox_hotkeyCharsFile.Text = Settings.GetString("charsDefFile");
            textBox_easyCharsFile.Text = Settings.EasyCharsFile;    // Settings.GetString("easyCharsFile");
            textBox_tableFile.Text = Settings.TableFile;            // Settings.GetString("tableFile");
            textBox_strokeHelpFile.Text = Settings.StrokeHelpFile;  // Settings.GetString("strokeHelpFile", Settings.StrokeHelpFile);
            textBox_bushuCompFile.Text = Settings.BushuFile;        // Settings.GetString("bushuFile");
            textBox_bushuAssocFile.Text = Settings.BushuAssocFile;  // Settings.GetString("bushuAssocFile");
            textBox_mazegakiFile.Text = Settings.MazegakiFile;      // Settings.GetString("mazegakiFile");
            textBox_historyFile.Text = Settings.HistoryFile;        // Settings.GetString("historyFile");
        }

        private void selectModeToggleKeyItem(ComboBox cbx, string hex)
        {
            if (hex._notEmpty()) {
                for (int i = 0; i < cbx.Items.Count; ++i) {
                    if (cbx.Items[i].ToString()._startsWith(hex)) {
                        cbx.SelectedIndex = i;
                        return;
                    }
                }
            }
            cbx.Text = hex;
        }

        private void setBasicStatusChecker()
        {
            button_basicEnter.Enabled = false;
            checkerBasic.CtlToBeEnabled = button_basicEnter;
            checkerBasic.ControlEnabler = button_basicClose_textChange;

            // 漢直モードトグルキー
            checkerBasic.Add(comboBox_unmodifiedToggleKey);
            checkerBasic.Add(comboBox_modifiedToggleKey);
            checkerBasic.Add(comboBox_unmodifiedOffKey);
            checkerBasic.Add(comboBox_modifiedOffKey);

            // 仮想鍵盤表示
            checkerBasic.Add(radioButton_normalVkb);
            checkerBasic.Add(radioButton_modeMarker);
            checkerBasic.Add(textBox_vkbShowStrokeCount);
            //checkerBasic.Add(checkBox_hideTopText);

            // 開始・終了
            checkerBasic.Add(textBox_splashWindowShowDuration);
            checkerBasic.Add(checkBox_confirmOnClose);

            // ファイル
            checkerBasic.Add(textBox_keyboardFile);
            checkerBasic.Add(textBox_hotkeyCharsFile);
            checkerBasic.Add(textBox_easyCharsFile);
            checkerBasic.Add(textBox_tableFile);
            checkerBasic.Add(textBox_strokeHelpFile);
            checkerBasic.Add(textBox_bushuCompFile);
            checkerBasic.Add(textBox_bushuAssocFile);
            checkerBasic.Add(textBox_mazegakiFile);
            checkerBasic.Add(textBox_historyFile);

            checkerAll.Add(checkerBasic);
        }

        private void button_basicClose_textChange(bool flag)
        {
            button_basicClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
        }

        private void button_basicEnter_Click(object sender, EventArgs e)
        {
            frmMain.DeactivateDecoder();

            // スプラッシュウィンドウ
            Settings.SetUserIni("splashWindowShowDuration", textBox_splashWindowShowDuration.Text.Trim());
            Settings.SetUserIni("confirmOnClose", checkBox_confirmOnClose.Checked);

            // 漢直モードトグルキー
            Settings.SetUserIni("unmodifiedHotKey", comboBox_unmodifiedToggleKey.Text.Trim()._reReplace(" .*", ""));
            Settings.SetUserIni("hotKey", comboBox_modifiedToggleKey.Text.Trim()._reReplace(" .*", ""));
            Settings.SetUserIni("unmodifiedOffHotKey", comboBox_unmodifiedOffKey.Text.Trim()._reReplace(" .*", ""));
            Settings.SetUserIni("offHotKey", comboBox_modifiedOffKey.Text.Trim()._reReplace(" .*", ""));

            // 仮想鍵盤表示
            Settings.SetUserIni("vkbShowStrokeCount", $"{textBox_vkbShowStrokeCount.Text._parseInt(1)._lowLimit(0) * (radioButton_normalVkb.Checked ? 1 : -1)}");
            //Settings.SetUserIni("topBoxMode", checkBox_hideTopText.Checked ? "hideOnSelect" : "showAlways");

            // ファイル
            Settings.SetUserIni("keyboard", textBox_keyboardFile.Text.Trim());
            Settings.SetUserIni("charsDefFile", textBox_hotkeyCharsFile.Text.Trim());
            Settings.SetUserIni("easyCharsFile", textBox_easyCharsFile.Text.Trim());
            Settings.SetUserIni("tableFile", textBox_tableFile.Text.Trim());
            Settings.SetUserIni("strokeHelpFile", textBox_strokeHelpFile.Text.Trim());
            Settings.SetUserIni("bushuFile", textBox_bushuCompFile.Text.Trim());
            Settings.SetUserIni("bushuAssocFile", textBox_bushuAssocFile.Text.Trim());
            Settings.SetUserIni("mazegakiFile", textBox_mazegakiFile.Text.Trim());
            Settings.SetUserIni("historyFile", textBox_historyFile.Text.Trim());

            Settings.ReadIniFile();

            readSettings_tabBasic();

            checkerBasic.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            frmMain.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            //SystemHelper.ShowInfoMessageBox("設定しました");
            label_okResultBasic.Show();
        }

        /// <summary> INIファイルのリロード </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Reload_Click(object sender, EventArgs e)
        {
            Settings.ReadIniFile();

            readSettings_tabBasic();
            readSettings_tabFile();
            readSettings_tabAdvanced();
            readSettings_tabKeyAssign();
            readSettings_tabCtrlKeys();
            readSettings_tabHistory();

            checkerAll.Reinitialize();

            frmMain.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            //SystemHelper.ShowInfoMessageBox("設定しました");
            label_reloadBasic.Show();
        }

        private void button_basicClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //-----------------------------------------------------------------------------------
        /// <summary>フォント・色設定</summary>
        void readSettings_tabFile()
        {
            // 仮想鍵盤フォント
            textBox_normalFont.Text = Settings.NormalVkbFontSpec;
            textBox_centerFont.Text = Settings.CenterVkbFontSpec;
            textBox_verticalFont.Text = Settings.VerticalVkbFontSpec;
            textBox_horizontalFont.Text = Settings.HorizontalVkbFontSpec;
            textBox_minibufFont.Text = Settings.MiniBufVkbFontSpec;

            // 通常鍵盤背景色
            textBox_topLevelBackColor.Text = Settings.BgColorTopLevelCells;
            textBox_centerSideBackColor.Text = Settings.BgColorCenterSideCells;
            textBox_highLowLevelBackColor.Text = Settings.BgColorHighLowLevelCells;
            textBox_middleLevelBackColor.Text = Settings.BgColorMiddleLevelCells;

            // モード標識文字色
            textBox_modeForeColor.Text = Settings.KanjiModeMarkerForeColor;
            textBox_2ndStrokeForeColor.Text = Settings.KanjiModeMarker2ndForeColor;
            textBox_alphaModeForeColor.Text = Settings.AlphaModeForeColor;

            // 中央鍵盤背景色
            textBox_on2ndStrokeBackColor.Text= Settings.BgColorOnWaiting2ndStroke;
            textBox_onMazegaki.Text= Settings.BgColorForMazegaki;
            textBox_onHistAssoc.Text= Settings.BgColorForHistOrAssoc;

            // 縦列・横列鍵盤背景色
            textBox_firstCandidateBackColor.Text = Settings.BgColorForFirstCandidate;
            textBox_onSelectedBackColor.Text = Settings.BgColorOnSelected;

        }

        private void setFontColortatusChecker()
        {
            button_fontColorEnter.Enabled = false;
            checkerFontColor.CtlToBeEnabled = button_fontColorEnter;
            checkerFontColor.ControlEnabler = button_fileClose_textChange;

            // フォント
            checkerFontColor.Add(textBox_normalFont);
            checkerFontColor.Add(textBox_centerFont);
            checkerFontColor.Add(textBox_verticalFont);
            checkerFontColor.Add(textBox_horizontalFont);
            checkerFontColor.Add(textBox_minibufFont);

            // 通常鍵盤背景色
            checkerFontColor.Add(textBox_topLevelBackColor);
            checkerFontColor.Add(textBox_centerSideBackColor);
            checkerFontColor.Add(textBox_highLowLevelBackColor);
            checkerFontColor.Add(textBox_middleLevelBackColor);

            // モード標識文字色
            checkerFontColor.Add(textBox_modeForeColor);
            checkerFontColor.Add(textBox_2ndStrokeForeColor);
            checkerFontColor.Add(textBox_alphaModeForeColor);

            // 中央鍵盤背景色
            checkerFontColor.Add(textBox_on2ndStrokeBackColor);
            checkerFontColor.Add(textBox_onMazegaki);
            checkerFontColor.Add(textBox_onHistAssoc);

            // 縦列・横列鍵盤背景色
            checkerFontColor.Add(textBox_firstCandidateBackColor);
            checkerFontColor.Add(textBox_onSelectedBackColor);

            checkerAll.Add(checkerFontColor);
        }

        private void button_fileEnter_Click(object sender, EventArgs e)
        {
            frmMain.DeactivateDecoder();

            // フォント
            Settings.SetUserIni("normalFont", textBox_normalFont.Text.Trim());
            Settings.SetUserIni("centerFont", textBox_centerFont.Text.Trim());
            Settings.SetUserIni("verticalFont", textBox_verticalFont.Text.Trim());
            Settings.SetUserIni("horizontalFont", textBox_horizontalFont.Text.Trim());
            Settings.SetUserIni("minibufFont", textBox_minibufFont.Text.Trim());

            // 通常鍵盤背景色
            Settings.SetUserIni("bgColorTopLevelCells", textBox_topLevelBackColor.Text.Trim());
            Settings.SetUserIni("bgColorCenterSideCells", textBox_centerSideBackColor.Text.Trim());
            Settings.SetUserIni("bgColorHighLowLevelCells", textBox_highLowLevelBackColor.Text.Trim());
            Settings.SetUserIni("bgColorMiddleLevelCells", textBox_middleLevelBackColor.Text.Trim());

            // モード標識色
            Settings.SetUserIni("kanjiModeMarkerForeColor", textBox_modeForeColor.Text.Trim());
            Settings.SetUserIni("kanjiModeMarker2ndForeColor", textBox_2ndStrokeForeColor.Text.Trim());
            Settings.SetUserIni("alphaModeForeColor", textBox_alphaModeForeColor.Text.Trim());

            // 中央鍵盤背景色
            Settings.SetUserIni("bgColorOnWaiting2ndStroke", textBox_on2ndStrokeBackColor.Text.Trim());
            Settings.SetUserIni("bgColorForMazegaki", textBox_onMazegaki.Text.Trim());
            Settings.SetUserIni("bgColorForHistOrAssoc", textBox_onHistAssoc.Text.Trim());

            // 縦列・横列鍵盤背景色
            Settings.SetUserIni("bgColorForFirstCandidate", textBox_firstCandidateBackColor.Text.Trim());
            Settings.SetUserIni("bgColorOnSelected", textBox_onSelectedBackColor.Text.Trim());


            Settings.ReadIniFile();

            readSettings_tabFile();

            checkerFontColor.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            frmMain.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            label_okResultFontColor.Show();

        }

        private void button_fileClose_textChange(bool flag)
        {
            button_fontColorClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
        }

        private void button_fileClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //-----------------------------------------------------------------------------------
        /// <summary> 詳細設定</summary>
        void readSettings_tabAdvanced()
        {
            // モード標識
            textBox_vkbOffsetX.Text = $"{Settings.VirtualKeyboardOffsetX}";
            textBox_vkbOffsetY.Text = $"{Settings.VirtualKeyboardOffsetY}";
            //textBox_displayScale.Text = $"{Settings.DisplayScale:f3}";
            textBox_kanjiModeInterval.Text = $"{Settings.KanjiModeMarkerShowIntervalSec}";
            textBox_alphaModeShowTime.Text = $"{Settings.AlphaModeMarkerShowMillisec}";

            // 各種待ち時間
            textBox_ctrlKeyUpGuardMillisec.Text = $"{Settings.CtrlKeyUpGuardMillisec}";
            textBox_ctrlKeyDownGuardMillisec.Text = $"{Settings.CtrlKeyDownGuardMillisec}";
            textBox_preWmCharGuardMillisec.Text = $"{Settings.PreWmCharGuardMillisec}";
            textBox_activeWinInfoIntervalMillisec.Text = $"{Settings.GetActiveWindowInfoIntervalMillisec}";
            textBox_vkbMoveGuardMillisec.Text = $"{Settings.VirtualKeyboardMoveGuardMillisec}";

            // 無限ループ対策
            textBox_hotkeyInfiniteLoopDetectCount.Text = $"{Settings.HotkeyInfiniteLoopDetectCount}";

            // クリップボード経由
            textBox_minLeghthViaClipboard.Text = $"{Settings.MinLeghthViaClipboard}";

            // ファイル保存世代数
            textBox_backFileRotationGeneration.Text = $"{Settings.BackFileRotationGeneration}";

            // 開発者用
            comboBox_logLevel.SelectedIndex = Settings.GetLogLevel();
            checkBox_loggingHotKeyInfo.Checked = Settings.GetString("loggingHotKeyInfo")._parseBool();
            checkBox_bushuDicLogEnabled.Checked = Settings.BushuDicLogEnabled;
            checkBox_loggingActiveWindowInfo.Checked = Settings.LoggingActiveWindowInfo;
            checkBox_delayAfterProcessHotkey.Checked = Settings.DelayAfterProcessHotkey;
            checkBox_multiAppEnabled.Checked = Settings.MultiAppEnabled;

            //checkBox_autoOffWhenBurstKeyIn.Checked = Settings.GetString("autoOffWhenBurstKeyIn")._parseBool();
        }

        private void setAdvancedStatusChecker()
        {
            button_advancedEnter.Enabled = false;
            checkerAdvanced.CtlToBeEnabled = button_advancedEnter;
            checkerAdvanced.ControlEnabler = button_advancedClose_textChange;

            // モード標識
            checkerAdvanced.Add(textBox_vkbOffsetX);
            checkerAdvanced.Add(textBox_vkbOffsetY);
            //checkerAdvanced.Add(textBox_displayScale);
            checkerAdvanced.Add(textBox_kanjiModeInterval);
            checkerAdvanced.Add(textBox_alphaModeShowTime);

            // 各種待ち時間
            checkerAdvanced.Add(textBox_ctrlKeyUpGuardMillisec);
            checkerAdvanced.Add(textBox_ctrlKeyDownGuardMillisec);
            checkerAdvanced.Add(textBox_preWmCharGuardMillisec);
            checkerAdvanced.Add(textBox_activeWinInfoIntervalMillisec);
            checkerAdvanced.Add(textBox_vkbMoveGuardMillisec);

            // 無限ループ対策
            checkerAdvanced.Add(textBox_hotkeyInfiniteLoopDetectCount);

            // クリップボード経由
            checkerAdvanced.Add(textBox_minLeghthViaClipboard);

            // ファイル保存世代数
            checkerAdvanced.Add(textBox_backFileRotationGeneration);

            // 開発者用
            checkerAdvanced.Add(comboBox_logLevel);
            checkerAdvanced.Add(checkBox_loggingHotKeyInfo);
            checkerAdvanced.Add(checkBox_bushuDicLogEnabled);
            checkerAdvanced.Add(checkBox_loggingActiveWindowInfo);
            checkerAdvanced.Add(checkBox_delayAfterProcessHotkey);
            checkerAdvanced.Add(checkBox_multiAppEnabled);

            //checkerAdvanced.Add(checkBox_autoOffWhenBurstKeyIn);

            checkerAll.Add(checkerAdvanced);
        }

        private void button_advancedClose_textChange(bool flag)
        {
            button_advancedClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
        }

        private void button_advancedEnter_Click(object sender, EventArgs e)
        {
            frmMain.DeactivateDecoder();

            // モード標識表示時間
            Settings.SetUserIni("vkbOffsetX", textBox_vkbOffsetX.Text.Trim());
            Settings.SetUserIni("vkbOffsetY", textBox_vkbOffsetY.Text.Trim());
            //Settings.SetUserIni("displayScale", textBox_displayScale.Text.Trim());
            Settings.SetUserIni("kanjiModeMarkerShowIntervalSec", textBox_kanjiModeInterval.Text.Trim());
            Settings.SetUserIni("alphaModeMarkerShowMillisec", textBox_alphaModeShowTime.Text.Trim());

            // 各種待ち時間
            Settings.SetUserIni("ctrlKeyUpGuardMillisec", textBox_ctrlKeyUpGuardMillisec.Text.Trim());
            Settings.SetUserIni("ctrlKeyDownGuardMillisec", textBox_ctrlKeyDownGuardMillisec.Text.Trim());
            Settings.SetUserIni("preWmCharGuardMillisec", textBox_preWmCharGuardMillisec.Text.Trim());
            Settings.SetUserIni("activeWindowInfoIntervalMillisec", textBox_activeWinInfoIntervalMillisec.Text.Trim());
            Settings.SetUserIni("virtualKeyboardMoveGuardMillisec", textBox_vkbMoveGuardMillisec.Text.Trim());

            // 無限ループ対策
            Settings.SetUserIni("hotkeyInfiniteLoopDetectCount", textBox_hotkeyInfiniteLoopDetectCount.Text.Trim());

            // クリップボード経由
            Settings.SetUserIni("minLeghthViaClipboard", textBox_minLeghthViaClipboard.Text.Trim());

            // ファイル保存世代数
            Settings.SetUserIni("backFileRotationGeneration", textBox_backFileRotationGeneration.Text.Trim());

            // 開発者用
            Settings.SetUserIni("logLevel", comboBox_logLevel.SelectedIndex);
            Logger.LogLevel = comboBox_logLevel.SelectedIndex;
            Settings.SetUserIni("loggingHotKeyInfo", checkBox_loggingHotKeyInfo.Checked);
            Settings.SetUserIni("bushuDicLogEnabled", checkBox_bushuDicLogEnabled.Checked);
            //Settings.SetUserIni("loggingActiveWindowInfo", checkBox_loggingActiveWindowInfo.Checked);
            Settings.LoggingActiveWindowInfo = checkBox_loggingActiveWindowInfo.Checked;
            Settings.SetUserIni("delayAfterProcessHotkey", checkBox_delayAfterProcessHotkey.Checked);
            Settings.SetUserIni("multiAppEnabled", checkBox_multiAppEnabled.Checked);

            //Settings.SetUserIni("autoOffWhenBurstKeyIn", checkBox_autoOffWhenBurstKeyIn.Checked);

            Settings.ReadIniFile();

            readSettings_tabAdvanced();
            checkerAdvanced.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            //frmVkb?.SetNormalCellBackColors();
            frmMode?.ShowImmediately();

            frmMain.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            label_okResultAdvanced.Show();
        }

        private void button_advancedClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //-----------------------------------------------------------------------------------
        /// <summary>機能キー割り当て</summary>
        void readSettings_tabKeyAssign()
        {
            textBox_zenkakuModeKeySeq.Text = Settings.ZenkakuModeKeySeq;
            textBox_zenkakuOneCharKeySeq.Text = Settings.ZenkakuOneCharKeySeq;
            textBox_katakanaModeKeySeq.Text = Settings.KatakanaModeKeySeq;
            textBox_nextThroughKeySeq.Text = Settings.NextThroughKeySeq;
            textBox_historyKeySeq.Text = Settings.HistoryKeySeq;
            textBox_historyOneCharKeySeq.Text = Settings.HistoryOneCharKeySeq;
            textBox_mazegakiKeySeq.Text = Settings.MazegakiKeySeq;
            textBox_bushuCompKeySeq.Text = Settings.BushuCompKeySeq;
            textBox_bushuAssocKeySeq.Text = Settings.BushuAssocKeySeq;
            textBox_bushuAssocDirectKeySeq.Text = Settings.BushuAssocDirectKeySeq;
            textBox_katakanaOneShotKeySeq.Text = Settings.KatakanaOneShotKeySeq;
        }

        private void setKeyAssignStatusChecker()
        {
            button_keyAssignEnter.Enabled = false;
            checkerKeyAssign.CtlToBeEnabled = button_keyAssignEnter;
            checkerKeyAssign.ControlEnabler = button_keyAssignClose_textChange;

            checkerKeyAssign.Add(textBox_zenkakuModeKeySeq);
            checkerKeyAssign.Add(textBox_zenkakuOneCharKeySeq);
            checkerKeyAssign.Add(textBox_katakanaModeKeySeq);
            checkerKeyAssign.Add(textBox_nextThroughKeySeq);
            checkerKeyAssign.Add(textBox_historyKeySeq);
            checkerKeyAssign.Add(textBox_historyOneCharKeySeq);
            checkerKeyAssign.Add(textBox_mazegakiKeySeq);
            checkerKeyAssign.Add(textBox_bushuCompKeySeq);
            checkerKeyAssign.Add(textBox_bushuAssocKeySeq);
            checkerKeyAssign.Add(textBox_bushuAssocDirectKeySeq);
            checkerKeyAssign.Add(textBox_katakanaOneShotKeySeq);

            checkerAll.Add(checkerKeyAssign);
        }

        private void button_keyAssignClose_textChange(bool flag)
        {
            button_keyAssignClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
        }

        private void button_keyAssignEnter_Click(object sender, EventArgs e)
        {
            frmMain.DeactivateDecoder();

            Settings.SetUserIni("zenkakuModeKeySeq", textBox_zenkakuModeKeySeq.Text);
            Settings.SetUserIni("zenkakuOneCharKeySeq", textBox_zenkakuOneCharKeySeq.Text);
            Settings.SetUserIni("katakanaModeKeySeq", textBox_katakanaModeKeySeq.Text);
            Settings.SetUserIni("nextThroughKeySeq", textBox_nextThroughKeySeq.Text);
            Settings.SetUserIni("historyKeySeq", textBox_historyKeySeq.Text);
            Settings.SetUserIni("historyOneCharKeySeq", textBox_historyOneCharKeySeq.Text);
            Settings.SetUserIni("mazegakiKeySeq", textBox_mazegakiKeySeq.Text);
            Settings.SetUserIni("bushuCompKeySeq", textBox_bushuCompKeySeq.Text);
            Settings.SetUserIni("bushuAssocKeySeq", textBox_bushuAssocKeySeq.Text);
            Settings.SetUserIni("bushuAssocDirectKeySeq", textBox_bushuAssocDirectKeySeq.Text);
            Settings.SetUserIni("katakanaOneShotKeySeq", textBox_katakanaOneShotKeySeq.Text);

            Settings.ReadIniFile();

            readSettings_tabKeyAssign();
            checkerKeyAssign.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            //frmVkb?.SetNormalCellBackColors();
            frmMode?.ShowImmediately();

            frmMain.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            label_okResultAdvanced.Show();
        }

        private void button_keyAssignClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //-----------------------------------------------------------------------------------
        /// <summary> Ctrlキー変換</summary>
        void readSettings_tabCtrlKeys()
        {
            // Ctrlキー変換
            checkBox_globalCtrlKeysEnabled.Checked = Settings.GlobalCtrlKeysEnabled;

            checkBox_CtrlH.Checked = Settings.ConvertCtrlHtoBackSpace;
            checkBox_CtrlBFNP.Checked = Settings.ConvertCtrlBFNPtoArrowKey;
            checkBox_CtrlA.Checked = Settings.ConvertCtrlAtoHome;
            checkBox_CtrlD.Checked = Settings.ConvertCtrlDtoDelete;
            checkBox_CtrlE.Checked = Settings.ConvertCtrlEtoEnd;
            checkBox_CtrlSemicolon.Checked = Settings.ConvertCtrlSemiColonToDate;
            textBox_dateStringFormat.Text = Settings.DateStringFormat._reReplace(@"\|", "\r\n");

            checkBox_useLeftCtrl.Checked = Settings.UseLeftControlToConversion;
            checkBox_useRightCtrl.Checked = Settings.UseRightControlToConversion;

            radioButton_excludeFollowings.Checked = !Settings.UseClassNameListAsInclusion;
            radioButton_includeFollowings.Checked = Settings.UseClassNameListAsInclusion;

            textBox_targetClassNames.Text = Settings.GetString("ctrlKeyTargetlassNames").Trim()._reReplace(@"\|", "\r\n");

            checkBox_ctrlJasEnter.Checked = Settings.UseCtrlJasEnter;
            checkBox_ctrlMasEnter.Checked = Settings.UseCtrlMasEnter;

            comboBox_ctrlKey_setItems(comboBox_fullEscapeKey);
            comboBox_fullEscapeKey._selectItemStartsWith(Settings.FullEscapeKey);
            comboBox_ctrlKey_setItems(comboBox_strokeHelpRotationKey);
            comboBox_strokeHelpRotationKey._selectItemStartsWith(Settings.StrokeHelpRotationKey);
        }

        private void setCtrlKeysStatusChecker()
        {
            button_ctrlEnter.Enabled = false;
            checkerCtrlKeys.CtlToBeEnabled = button_ctrlEnter;
            checkerCtrlKeys.ControlEnabler = button_ctrlClose_textChange;

            checkerCtrlKeys.Add(checkBox_globalCtrlKeysEnabled);

            checkerCtrlKeys.Add(checkBox_CtrlH);
            checkerCtrlKeys.Add(checkBox_CtrlBFNP);
            checkerCtrlKeys.Add(checkBox_CtrlA);
            checkerCtrlKeys.Add(checkBox_CtrlD);
            checkerCtrlKeys.Add(checkBox_CtrlE);
            checkerCtrlKeys.Add(checkBox_CtrlSemicolon);
            checkerCtrlKeys.Add(textBox_dateStringFormat);

            checkerCtrlKeys.Add(checkBox_useLeftCtrl);
            checkerCtrlKeys.Add(checkBox_useRightCtrl);

            checkerCtrlKeys.Add(radioButton_excludeFollowings);
            checkerCtrlKeys.Add(radioButton_includeFollowings);

            checkerCtrlKeys.Add(textBox_targetClassNames);

            checkerCtrlKeys.Add(checkBox_ctrlJasEnter);
            checkerCtrlKeys.Add(checkBox_ctrlMasEnter);

            checkerCtrlKeys.Add(comboBox_fullEscapeKey);
            checkerCtrlKeys.Add(comboBox_strokeHelpRotationKey);

            checkerAll.Add(checkerCtrlKeys);
        }

        private void button_ctrlClose_textChange(bool flag)
        {
            button_ctrlClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
        }

        private void button_ctrlEnter_Click(object sender, EventArgs e)
        {
            frmMain.DeactivateDecoder();

            Settings.SetUserIni("globalCtrlKeysEnabled", checkBox_globalCtrlKeysEnabled.Checked);

            Settings.SetUserIni("convertCtrlHtoBackSpace", checkBox_CtrlH.Checked);
            Settings.SetUserIni("convertCtrlBFNPtoArrowKey", checkBox_CtrlBFNP.Checked);
            Settings.SetUserIni("convertCtrlAtoHome", checkBox_CtrlA.Checked);
            Settings.SetUserIni("convertCtrlDtoDelete", checkBox_CtrlD.Checked);
            Settings.SetUserIni("convertCtrlEtoEnd", checkBox_CtrlE.Checked);
            Settings.SetUserIni("convertCtrlSemicolonToDate", checkBox_CtrlSemicolon.Checked);
            Settings.SetUserIni("dateStringFormat", textBox_dateStringFormat.Text.Trim()._reReplace(@"[ \r\n]+", "|"));

            Settings.SetUserIni("useLeftControlToConversion", checkBox_useLeftCtrl.Checked);
            Settings.SetUserIni("useRightControlToConversion", checkBox_useRightCtrl.Checked);
            Settings.SetUserIni("useClassNameListAsInclusion", radioButton_includeFollowings.Checked);
            Settings.SetUserIni("ctrlKeyTargetlassNames", textBox_targetClassNames.Text.Trim()._reReplace(@"[ \r\n]+", "|"));

            Settings.SetUserIni("useCtrlJasEnter", checkBox_ctrlJasEnter.Checked);
            Settings.SetUserIni("useCtrlMasEnter", checkBox_ctrlMasEnter.Checked);

            Settings.SetUserIni("fullEscapeKey", comboBox_fullEscapeKey._getSelectedItemSplittedFirst("G"));
            Settings.SetUserIni("strokeHelpRotationKey", comboBox_strokeHelpRotationKey._getSelectedItemSplittedFirst("T"));

            Settings.ReadIniFile();

            readSettings_tabCtrlKeys();
            checkerCtrlKeys.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            frmMain.ReregisterSpecialGlobalHotkeys();

            label_okResultCtrlKeys.Show();
        }

        private void button_ctrlClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //-----------------------------------------------------------------------------------
        /// <summary> 履歴・交ぜ書き・その他変換関連 </summary>
        void readSettings_tabHistory()
        {
            // 履歴関連
            textBox_histKanjiWordMinLength.Text = $"{Settings.HistKanjiWordMinLength}";
            textBox_histKanjiWordMinLengthEx.Text = $"{Settings.HistKanjiWordMinLengthEx}";
            textBox_histKatakanaWordMinLength.Text = $"{Settings.HistKatakanaWordMinLength}";
            textBox_histKanjiKeyLen.Text = $"{Settings.HistKanjiKeyLength}";
            textBox_histKatakanaKeyLen.Text = $"{Settings.HistKatakanaKeyLength}";
            textBox_histHiraganaKeyLen.Text = $"{Settings.HistHiraganaKeyLength}";
            checkBox_autoHistEnabled.Checked = Settings.AutoHistSearchEnabled;
            checkBox_histSearchByCtrlSpace.Checked = Settings.HistSearchByCtrlSpace;
            checkBox_histSearchByShiftSpace.Checked = Settings.HistSearchByShiftSpace;
            checkBox_selectFirstCandByEnter.Checked = Settings.SelectFirstCandByEnter;
            //checkBox_autoHistEnabled_CheckedChanged(null, null);
            checkBox_useArrowKeyToSelectCand.Checked = Settings.UseArrowKeyToSelectCandidate;
            comboBox_histDelHotkeyId.SelectedIndex = Settings.HistDelHotkeyId._lowLimit(41)._highLimit(49) - 41;
            comboBox_histNumHotkeyId.SelectedIndex = Settings.HistNumHotkeyId._lowLimit(41)._highLimit(49) - 41;

            // 交ぜ書き
            textBox_mazeYomiMaxLen.Text = $"{Settings.MazeYomiMaxLen}";
            textBox_mazeGobiMaxLen.Text = $"{Settings.MazeGobiMaxLen}";

            // その他変換
            checkBox_convertShiftedHiraganaToKatakana.Checked = Settings.ConvertShiftedHiraganaToKatakana;
            checkBox_convertJaPeriod.Checked = Settings.ConvertJaPeriod;
            checkBox_convertJaComma.Checked = Settings.ConvertJaComma;
        }

        private void setHistoryStatusChecker()
        {
            // 履歴関連
            button_histEnter.Enabled = false;
            checkerHistory.CtlToBeEnabled = button_histEnter;
            checkerHistory.ControlEnabler = button_histClose_textChange;
            checkerHistory.Add(textBox_histKanjiWordMinLength);
            checkerHistory.Add(textBox_histKanjiWordMinLengthEx);
            checkerHistory.Add(textBox_histKatakanaWordMinLength);
            checkerHistory.Add(textBox_histKanjiKeyLen);
            checkerHistory.Add(textBox_histKatakanaKeyLen);
            checkerHistory.Add(textBox_histHiraganaKeyLen);
            checkerHistory.Add(checkBox_autoHistEnabled);
            checkerHistory.Add(checkBox_histSearchByCtrlSpace);
            checkerHistory.Add(checkBox_histSearchByShiftSpace);
            checkerHistory.Add(checkBox_selectFirstCandByEnter);
            //checkerHistory.Add(checkBox_autoHistEnabled_CheckedChanged);
            checkerHistory.Add(checkBox_useArrowKeyToSelectCand);
            checkerHistory.Add(comboBox_histDelHotkeyId);
            checkerHistory.Add(comboBox_histNumHotkeyId);

            // 交ぜ書き
            checkerHistory.Add(textBox_mazeYomiMaxLen);
            checkerHistory.Add(textBox_mazeGobiMaxLen);

            // その他変換
            checkerHistory.Add(checkBox_convertShiftedHiraganaToKatakana);
            checkerHistory.Add(checkBox_convertJaPeriod);
            checkerHistory.Add(checkBox_convertJaComma);

            checkerAll.Add(checkerHistory);
        }

        private void button_histClose_textChange(bool flag)
        {
            button_histClose.Text = flag ? "キャンセル(&C)" : "閉じる(&C)";
        }

        private void button_histEnter_Click(object sender, EventArgs e)
        {
            frmMain.DeactivateDecoder();

            Settings.SetUserIni("histKatakanaWordMinLength", textBox_histKatakanaWordMinLength.Text.Trim());
            Settings.SetUserIni("histKanjiWordMinLength", textBox_histKanjiWordMinLength.Text.Trim());
            Settings.SetUserIni("histKanjiWordMinLengthEx", textBox_histKanjiWordMinLengthEx.Text.Trim());
            Settings.SetUserIni("histHiraganaKeyLength", textBox_histHiraganaKeyLen.Text.Trim());
            Settings.SetUserIni("histKatakanaKeyLength", textBox_histKatakanaKeyLen.Text.Trim());
            Settings.SetUserIni("histKanjiKeyLength", textBox_histKanjiKeyLen.Text.Trim());
            Settings.SetUserIni("autoHistSearchEnabled", checkBox_autoHistEnabled.Checked);
            Settings.SetUserIni("histSearchByCtrlSpace", checkBox_histSearchByCtrlSpace.Checked);
            Settings.SetUserIni("histSearchByShiftSpace", checkBox_histSearchByShiftSpace.Checked);
            Settings.SetUserIni("selectFirstCandByEnter", checkBox_selectFirstCandByEnter.Checked);
            Settings.SetUserIni("useArrowKeyToSelectCandidate", checkBox_useArrowKeyToSelectCand.Checked);
            Settings.SetUserIni("histDelHotkeyId", comboBox_histDelHotkeyId.Text.Trim()._substring(0, 2));
            Settings.SetUserIni("histNumHotkeyId", comboBox_histNumHotkeyId.Text.Trim()._substring(0, 2));

            Settings.SetUserIni("mazeGobiMaxLen", textBox_mazeGobiMaxLen.Text.Trim());
            Settings.SetUserIni("mazeYomiMaxLen", textBox_mazeYomiMaxLen.Text.Trim());

            Settings.SetUserIni("convertShiftedHiraganaToKatakana", checkBox_convertShiftedHiraganaToKatakana.Checked);
            Settings.SetUserIni("convertJaPeriod", checkBox_convertJaPeriod.Checked);
            Settings.SetUserIni("convertJaComma", checkBox_convertJaComma.Checked);

            Settings.ReadIniFile();

            readSettings_tabHistory();
            checkerHistory.Reinitialize();    // ここの Reinitialize() はタブごとにやる必要がある(まとめてやるとDirty状態の他のタブまでクリーンアップしてしまうため)

            frmMain.ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            label_okResultHist.Show();
        }

        private void button_histClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void checkBox_autoHistEnabled_CheckedChanged(object sender, EventArgs e)
        {
            //checkBox_histSearchByCtrlSpace.Enabled = !checkBox_autoHistEnabled.Checked;
            //checkBox_histSearchByShiftSpace.Enabled = !checkBox_autoHistEnabled.Checked;
        }

        //-----------------------------------------------------------------------------------
        // 一定時間後にOKリザルトラベルを非表示にする
        int okResultCount = 0;

        private const int okResultCountMax = 3 * (1000 / timerInterval);    // 3秒

        private void hideOkResultLabel()
        {
            if (okResultCount > 0) {
                --okResultCount;
                if (okResultCount == 0) {
                    label_okResultBasic.Hide();
                    label_reloadBasic.Hide();
                    label_okResultFontColor.Hide();
                    label_okResultAdvanced.Hide();
                    label_okResultHist.Hide();
                    label_okResultCtrlKeys.Hide();
                    label_execResultFile.Hide();
                }
            }
        }

        private void label_okResultBasic_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_reloadBasic_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultFontColor_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultAdvanced_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultHistory_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        private void label_okResultCtrlKeys_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }
        
        private void label_execResultFile_VisibleChanged(object sender, EventArgs e)
        {
            okResultCount = okResultCountMax;
        }

        //-----------------------------------------------------------------------------------
        /// <summary> 履歴辞書登録 </summary>
        private void button_enterHistory_Click(object sender, EventArgs e)
        {
            var line = textBox_history.Text.Trim().Replace(" ", "");
            if (line._notEmpty()) {
                frmMain.ExecCmdDecoder("addHistEntry", line);
                label_saveHist.Hide();
                label_history.Show();
                dicRegLabelCount = dicRegLabelCountMax;
            }
        }

        private void button_saveHistoryFile_Click(object sender, EventArgs e)
        {
            frmMain.ExecCmdDecoder("saveHistoryDic", null);
            label_history.Hide();
            label_saveHist.Show();
            dicRegLabelCount = dicRegLabelCountMax;
        }

        private void textBox_history_TextChanged(object sender, EventArgs e)
        {
            label_saveHist.Hide();
            label_history.Hide();
        }

        /// <summary> 交ぜ書き辞書登録 </summary>
        private void button_enterMazegaki_Click(object sender, EventArgs e)
        {
            var line = textBox_mazegaki.Text.Trim()._reReplace(@" +", " ");
            if (line._reMatch(@"^[^ ]+ ")) {
                frmMain.ExecCmdDecoder("addMazegakiEntry", line);
                label_saveMaze.Hide();
                label_mazegaki.Show();
                dicRegLabelCount = dicRegLabelCountMax;
            } else {
                SystemHelper.ShowWarningMessageBox("形式が間違っています。\r\n「読み<空白>単語/...」という形式で入力してください。");
            }
        }

        private void button_saveMazegakiFile_Click(object sender, EventArgs e)
        {
            frmMain.ExecCmdDecoder("saveMazegakiDic", null);
            label_mazegaki.Hide();
            label_saveMaze.Show();
            dicRegLabelCount = dicRegLabelCountMax;
        }

        private void textBox_mazegaki_TextChanged(object sender, EventArgs e)
        {

            label_saveMaze.Hide();
            label_mazegaki.Hide();
        }

        /// <summary> 部首連想辞書登録 </summary>
        private void button_enterBushuAssoc_Click(object sender, EventArgs e)
        {
            var line = textBox_bushuAssoc.Text.Trim().Replace(" ", "");
            if (line._reMatch(@"^[^=]=.")) {
                frmMain.ExecCmdDecoder("mergeBushuAssocEntry", line);
                label_saveAssoc.Hide();
                label_bushuAssoc.Show();
                dicRegLabelCount = dicRegLabelCountMax;
            } else {
                SystemHelper.ShowWarningMessageBox("形式が間違っています。\r\n「文字=文字...」という形式で入力してください。");
            }
        }

        private void button_saveBushuAssocFile_Click(object sender, EventArgs e)
        {
            frmMain.ExecCmdDecoder("saveBushuAssocDic", null);
            label_bushuAssoc.Hide();
            label_saveAssoc.Show();
            dicRegLabelCount = dicRegLabelCountMax;
        }

        private void textBox_bushuAssoc_TextChanged(object sender, EventArgs e)
        {
            label_saveAssoc.Hide();
            label_bushuAssoc.Hide();
        }

        /// <summary> 部首合成辞書登録 </summary>
        private void button_enterBushu_Click(object sender, EventArgs e)
        {
            var line = textBox_bushuComp.Text.Trim().Replace(" ", "");
            int n = 0;
            foreach (var ch in line) {
                if (Char.IsHighSurrogate(ch)) continue;
                ++n;
            }
            if (n == 2 || n == 3) {
                frmMain.ExecCmdDecoder("addBushuEntry", line);
                label_saveBushu.Hide();
                label_bushuComp.Show();
                dicRegLabelCount = dicRegLabelCountMax;
            } else {
                SystemHelper.ShowWarningMessageBox("形式が間違っています。\r\n3文字または2文字で入力してください。");
            }
        }

        private void button_saveBushuCompFile_Click(object sender, EventArgs e)
        {
            frmMain.ExecCmdDecoder("saveBushuDic", null);
            label_bushuComp.Hide();
            label_saveBushu.Show();
            dicRegLabelCount = dicRegLabelCountMax;
        }

        private void textBox_bushuComp_TextChanged(object sender, EventArgs e)
        {
            label_saveBushu.Hide();
            label_bushuComp.Hide();
        }

        /// <summary> 閉じる </summary>
        private void button_regClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // 一定時間後にOKリザルトラベルを非表示にする
        int dicRegLabelCount = 0;

        private const int dicRegLabelCountMax = 3 * (1000 / timerInterval);    // 3秒

        private void hideDicRegLabel()
        {
            if (dicRegLabelCount > 0) {
                --dicRegLabelCount;
                if (dicRegLabelCount == 0) {
                    label_saveHist.Hide();
                    label_history.Hide();
                    label_saveMaze.Hide();
                    label_mazegaki.Hide();
                    label_saveAssoc.Hide();
                    label_bushuAssoc.Hide();
                    label_saveBushu.Hide();
                    label_bushuComp.Hide();
                }
            }
        }

        //-----------------------------------------------------------------------------------
        private void button_aboutClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radioButton_modeMarker_CheckedChanged(object sender, EventArgs e)
        {
            //checkBox_hideTopText.Enabled = radioButton_modeMarker.Checked;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedTabIndex = tabControl1.SelectedIndex;

            switch (tabControl1.SelectedTab.Name) {
                case "tabPage_basic":
                    AcceptButton = button_basicEnter;
                    CancelButton = button_basicClose;
                    break;
                case "tabPage_fontColor":
                    AcceptButton = button_fontColorEnter;
                    CancelButton = button_fontColorClose;
                    break;
                case "tabPage_advanced":
                    AcceptButton = button_advancedEnter;
                    CancelButton = button_advancedClose;
                    break;
                case "tabPage_keyAssign":
                    AcceptButton = button_keyAssignEnter;
                    CancelButton = button_keyAssignClose;
                    break;
                case "tabPage_ctrlKeys":
                    AcceptButton = button_ctrlEnter;
                    CancelButton = button_ctrlClose;
                    break;
                case "tabPage_history":
                    AcceptButton = button_histEnter;
                    CancelButton = button_histClose;
                    break;
                case "tabPage_register":
                    AcceptButton = null;
                    CancelButton = button_regClose;
                    break;
                case "tabPage_about":
                    AcceptButton = button_aboutClose;
                    CancelButton = button_aboutClose;
                    break;
            }
        }

        // フォームタイマー処理
        private void timer1_Tick(object sender, EventArgs e)
        {
            checkerAll.CheckStatus();
            hideOkResultLabel();
            hideDicRegLabel();
        }

        private void button_saveAll_Click(object sender, EventArgs e)
        {
            frmMain.SaveAllFiles();
            label_execResultFile.Show();
        }

        private string makeFontSpec(string oldSpec, bool bVertical)
        {
            var result = oldSpec;

            var items = oldSpec._split('|').Select(x => x._strip()).ToArray();

            Font fnt = null;
            var fd = new FontDialog();
            var fontName = items._getNth(0);
            if (fontName._notEmpty()) {
                fnt = new Font(fontName, items._getNth(1)._parseInt(9)._lowLimit(8));
                fd.Font = fnt;
            }
            fd.FontMustExist = true;
            fd.ShowEffects = false;
            if (fd.ShowDialog() == DialogResult.OK) {
                result = $"{(fd.Font.GdiVerticalFont ? "@" : "")}{fd.Font.Name} | {Math.Round(fd.Font.SizeInPoints)}";
                if (bVertical) result = result +$" | {items._getNth(2)._orElse("0")} | {items._getNth(3)._orElse("0")}";
            }
            fd.Dispose();
            fnt?.Dispose();

            return result;
        }

        private void button_normalDlg_Click(object sender, EventArgs e)
        {
            textBox_normalFont.Text = makeFontSpec(textBox_normalFont.Text, true);
        }

        private void button_centerDlg_Click(object sender, EventArgs e)
        {
            textBox_centerFont.Text = makeFontSpec(textBox_centerFont.Text, true);
        }

        private void button_verticalDlg_Click(object sender, EventArgs e)
        {
            textBox_verticalFont.Text = makeFontSpec(textBox_verticalFont.Text, true);
        }

        private void button_horizontalDlg_Click(object sender, EventArgs e)
        {
            textBox_horizontalFont.Text = makeFontSpec(textBox_horizontalFont.Text, false);
        }

        private void button_minibufDlg_Click(object sender, EventArgs e)
        {
            textBox_minibufFont.Text = makeFontSpec(textBox_minibufFont.Text, false);
        }

        private void textBox_dateStringFormat_Enter(object sender, EventArgs e)
        {
            AcceptButton = null;
        }

        private void textBox_dateStringFormat_Leave(object sender, EventArgs e)
        {
            AcceptButton = button_ctrlEnter;
        }

        private void textBox_targetClassNames_Enter(object sender, EventArgs e)
        {
            AcceptButton = null;
        }

        private void textBox_targetClassNames_Leave(object sender, EventArgs e)
        {
            AcceptButton = button_ctrlEnter;
        }

        private void button_restart_Click(object sender, EventArgs e)
        {
            RestartRequired = true;
            Close();
        }

        private void button_restartWithNoSave_Click(object sender, EventArgs e)
        {
            RestartRequired = true;
            NoSave = true;
            Close();
        }

        private void checkBox_globalCtrlKeysEnabled_CheckedChanged(object sender, EventArgs e)
        {
            changeGlobalCtrlKeysCheckBoxState();
        }

        private void changeGlobalCtrlKeysCheckBoxState()
        {
            //groupBox_globalCtrlKeys.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_CtrlH.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_CtrlBFNP.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_CtrlA.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_CtrlD.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_CtrlE.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
            checkBox_CtrlSemicolon.Enabled = checkBox_globalCtrlKeysEnabled.Checked;
        }

        private void button_document_Click(object sender, EventArgs e)
        {
            openDocumentUrl();
        }

        private void radioButton_normalVkb_CheckedChanged(object sender, EventArgs e)
        {
            textBox_vkbShowStrokeCount.Enabled = radioButton_normalVkb.Checked;
        }

        /// <summary>
        /// Ctrlキー割り当てで、ドロップダウンに使われる項目
        /// </summary>
        private string[] ctrlKeyItems = new string[] {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
            "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T",
            "U", "V", "W", "X", "Y", "Z",
            "COLON (:)",    // ba
            "PLUS (+)",     // bb
            "COMMA (,)",    // bc
            "MINUS (-)",    // bd
            "PERIOD (.)",   // be
            "SLASH (/)",    // bf
            "BQUOTE (`)",   // c0/106
            "OEM4 ([)",     // db
            "OEM5 (|)",     // dc
            "OEM6 (]})",    // dd
            "OEM7 (^ ')",   // de
            "OEM102 (\\)",  // e2/106
        };

        private string[] getCtrlKeyItems()
        {
            //if (ctrlKeyItems._isEmpty()) {
            //    var items = new List<string>(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" });
            //    char[] data = frmMain.CallDecoderFunc("getCharsOrderedByHotkey", null);
            //    if (data._notEmpty()) {
            //        for (int i = 0; i < HotKeys.NUM_STROKE_HOTKEY; ++i) {
            //            if ((data[i] > ' ' && data[i] < '0') || (data[i] > '9' && data[i] < 'A') || (data[i] > 'Z' && data[i] < 'a') || data[i] > 'z') {
            //                items.Add($"{data[i]}{data[i+HotKeys.NUM_STROKE_HOTKEY]}");
            //            }
            //        }
            //    }
            //    ctrlKeyItems = items.Select(x => " " + x).ToArray();
            //}
            return ctrlKeyItems;
        }

        private void comboBox_ctrlKey_setItems(ComboBox comboBox)
        {
            if (comboBox.Items.Count == 0) comboBox.Items.AddRange(getCtrlKeyItems());
        }
    }
}

